using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist.Patches;

public static class PlayerPatch
{
    public static ConfigEntry<bool> EnhancedMechaForgeCountControlEnabled;
    public static ConfigEntry<bool> HideTipsForSandsChangesEnabled;
    public static ConfigEntry<bool> ShortcutKeysForStarsNameEnabled;
    public static ConfigEntry<bool> AutoNavigationEnabled;
    public static ConfigEntry<bool> AutoCruiseEnabled;
    public static ConfigEntry<bool> AutoBoostEnabled;
    public static ConfigEntry<double> DistanceToWarp;
    private static PressKeyBind _showAllStarsNameKey;
    private static PressKeyBind _toggleAllStarsNameKey;
    private static PressKeyBind _autoDriveKey;

    public static void Init()
    {
        _showAllStarsNameKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.UI | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ShowAllStarsName",
            canOverride = true
        }
        );
        I18N.Add("KEYShowAllStarsName", "Keep pressing to show all Stars' name", "按住显示所有星系名称");

        _toggleAllStarsNameKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.Tab, 0, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.UI | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleAllStarsName",
            canOverride = true
        }
        );
        I18N.Add("KEYToggleAllStarsName", "Toggle display of all Stars' name", "切换所有星系名称显示状态");

        _autoDriveKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleAutoCruise",
            canOverride = true
        });
        I18N.Add("KEYToggleAutoCruise", "Toggle auto-cruise", "切换自动巡航");
        I18N.Add("AutoCruiseOn", "Auto-cruise enabled", "已启用自动巡航");
        I18N.Add("AutoCruiseOff", "Auto-cruise disabled", "已禁用自动巡航");

        EnhancedMechaForgeCountControlEnabled.SettingChanged += (_, _) => EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChangesEnabled.SettingChanged += (_, _) => HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        ShortcutKeysForStarsNameEnabled.SettingChanged += (_, _) => ShortcutKeysForStarsName.Enable(ShortcutKeysForStarsNameEnabled.Value);
        AutoNavigationEnabled.SettingChanged += (_, _) => AutoNavigation.Enable(AutoNavigationEnabled.Value);
    }

    public static void Start()
    {
        EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        ShortcutKeysForStarsName.Enable(ShortcutKeysForStarsNameEnabled.Value);
        AutoNavigation.Enable(AutoNavigationEnabled.Value);
    }

    public static void OnUpdate()
    {
        if (_toggleAllStarsNameKey.keyValue)
        {
            ShortcutKeysForStarsName.ToggleAllStarsName();
        }
        if (_autoDriveKey.keyValue)
        {
            AutoNavigation.ToggleAutoCruise();
        }
    }

    public static void Uninit()
    {
        EnhancedMechaForgeCountControl.Enable(false);
        HideTipsForSandsChanges.Enable(false);
        ShortcutKeysForStarsName.Enable(false);
        AutoNavigation.Enable(false);
    }

    private class EnhancedMechaForgeCountControl: PatchImpl<EnhancedMechaForgeCountControl>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnOkButtonClick))]
        private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnOkButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 1000));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnPlusButtonClick))]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnMinusButtonClick))]
        private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnPlusButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            var label3 = generator.DefineLabel();
            var label4 = generator.DefineLabel();
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(o => o.opcode == OpCodes.Add || o.opcode == OpCodes.Sub)
            ).Advance(1).RemoveInstruction().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.control))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Ldc_I4_S, 10),
                new CodeInstruction(OpCodes.Br_S, label4),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))).WithLabels(label1),
                new CodeInstruction(OpCodes.Brfalse_S, label2),
                new CodeInstruction(OpCodes.Ldc_I4_S, 100),
                new CodeInstruction(OpCodes.Br_S, label4),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.alt))).WithLabels(label2),
                new CodeInstruction(OpCodes.Brfalse_S, label3),
                new CodeInstruction(OpCodes.Ldc_I4, 1000),
                new CodeInstruction(OpCodes.Br_S, label4),
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(label3)
            ).Labels.Add(label4);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 1000));
            return matcher.InstructionEnumeration();
        }
    }

    private class HideTipsForSandsChanges: PatchImpl<HideTipsForSandsChanges>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.SetSandCount))]
        private static IEnumerable<CodeInstruction> Player_SetSandCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(Player), nameof(Player.sandCount)))
            ).Advance(1).Insert(new CodeInstruction(OpCodes.Ret));
            return matcher.InstructionEnumeration();
        }
    }

    public class ShortcutKeysForStarsName: PatchImpl<ShortcutKeysForStarsName>
    {
        private static int _showAllStarsNameStatus;

        public static void ToggleAllStarsName()
        {
            _showAllStarsNameStatus = (_showAllStarsNameStatus + 1) % 3;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnClose))]
        private static void UIStarmap__OnClose_Postfix()
        {
            _showAllStarsNameStatus = 0;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIStarmapStar), nameof(UIStarmapStar._OnLateUpdate))]
        private static IEnumerable<CodeInstruction> UIStarmapStar__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            Label? jumpPos = null;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapStar), nameof(UIStarmapStar.projectedCoord))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out jumpPos))
            );
            matcher.Advance(3);
            var labels = matcher.Labels;
            matcher.Labels = [];
            matcher.CreateLabel(out var jumpPos2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch), nameof(_showAllStarsNameKey))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brfalse, jumpPos2),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc_1),
                new CodeInstruction(OpCodes.Br, jumpPos.Value)
            );
            return matcher.InstructionEnumeration();
        }
/*
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet._OnLateUpdate))]
        private static IEnumerable<CodeInstruction> UIStarmapPlanet__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet.projected)))
            );
            matcher.Advance(3);
            matcher.CreateLabel(out var jumpPos1);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brfalse, jumpPos1),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc_3)
            );
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet.gameHistory))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet.planet))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.id))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.GetPlanetPin))),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.Branches(out _))
            );
            matcher.CreateLabelAt(matcher.Pos + 8, out var jumpPos);
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brtrue, jumpPos),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch), nameof(_showAllStarsNameKey))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                new CodeInstruction(OpCodes.Brtrue, jumpPos)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive._OnLateUpdate))]
        private static IEnumerable<CodeInstruction> UIStarmapDFHive__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive.projected)))
            );
            matcher.Advance(3);
            matcher.CreateLabel(out var jumpPos1);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brfalse, jumpPos1),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc_S, 4)
            );
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive.gameHistory))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive.hive))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.hiveStarId))),
                new CodeMatch(OpCodes.Ldc_I4, 1000000),
                new CodeMatch(OpCodes.Sub),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.GetHivePin))),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.Branches(out _))
            );
            matcher.CreateLabelAt(matcher.Pos + 10, out var jumpPos);
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brtrue, jumpPos),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch), nameof(_showAllStarsNameKey))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                new CodeInstruction(OpCodes.Brtrue, jumpPos)
            );
            return matcher.InstructionEnumeration();
        }
*/
    }

    public class AutoNavigation: PatchImpl<AutoNavigation>
    {
        private static bool _canUseWarper;
        private static int _indicatorAstroId;
        private static bool _speedUp;
        private static Vector3 _direction;

        public static void ToggleAutoCruise()
        {
            AutoCruiseEnabled.Value = !AutoCruiseEnabled.Value;
            if (!DSPGame.IsMenuDemo && GameMain.isRunning)
            {
                UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead((AutoCruiseEnabled.Value ? "AutoCruiseOn" : "AutoCruiseOff").Translate());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerController_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BuildModel), nameof(BuildModel.EarlyGameTickIgnoreActive)))
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((PlayerController controller) =>
                {
                    /* Update target astro if changed */
                    _speedUp = false;
                    var player = controller.player;
                    var navi = player.navigation;
                    if (navi.indicatorAstroId != _indicatorAstroId)
                    {
                        _indicatorAstroId = navi.indicatorAstroId;
                        if (_indicatorAstroId == 0) return;
                    }
                    else if (_indicatorAstroId == 0) return;
                    switch (controller.movementStateInFrame)
                    {
                        case EMovementState.Walk:
                        case EMovementState.Drift:
                            if (!AutoCruiseEnabled.Value) return;
                            if (GameMain.localStar?.astroId == _indicatorAstroId) return;
                            /* Press jump key to fly */
                            controller.input0.z = 1f;
                            break;
                        case EMovementState.Fly:
                            if (!AutoCruiseEnabled.Value) return;
                            if (GameMain.localStar?.astroId == _indicatorAstroId) return;
                            /* Keep pressing jump and pullup key to sail */
                            controller.input0.y = 1f;
                            controller.input1.y = 1f;
                            break;
                        case EMovementState.Sail:
                            if (VFInput._pullUp.pressing || VFInput._pushDown.pressing || VFInput._moveLeft.pressing || VFInput._moveRight.pressing ||
                                (!player.warping && UIRoot.instance.uiGame.disableLockCursor && (VFInput._moveForward.pressing || VFInput._moveBackward.pressing)))
                                return;
                            var playerPos = player.uPosition;
                            var isHive = _indicatorAstroId > 1000000;
                            ref var astro = ref isHive ? ref GameMain.spaceSector.astros[_indicatorAstroId - 1000000] : ref GameMain.galaxy.astrosData[_indicatorAstroId];
                            var astroVec = astro.uPos - playerPos;
                            var distance = astroVec.magnitude;
                            if (distance < astro.type switch
                                {
                                    EAstroType.Planet => 800.0 + astro.uRadius,
                                    EAstroType.Star => 4000.0 + astro.uRadius,
                                    EAstroType.EnemyHive => 400.0,
                                    _ => 2000.0 + astro.uRadius
                                })
                            {
                                if (isHive)
                                {
                                    player.uVelocity = Vector3.zero;
                                }
                                return;
                            }
                            var autoCruise = AutoCruiseEnabled.Value;
                            if (GameMain.instance.timei % 6 == 0 || _direction == Vector3.zero)
                            {
                                _direction = astroVec.normalized;

                                /* Check nearest astroes, try to bypass them */
                                var localStar = GameMain.localStar;
                                _canUseWarper = autoCruise && !player.warping && player.mecha.warpStorage.GetItemCount(1210) > 0;
                                if (localStar != null)
                                {
                                    var nearestRange = (playerPos - localStar.uPosition).sqrMagnitude;
                                    var nearestPos = localStar.uPosition;
                                    var nearestAstroId = localStar.astroId;
                                    foreach (var p in localStar.planets)
                                    {
                                        var range = (playerPos - p.uPosition).sqrMagnitude;
                                        if (range >= nearestRange) continue;
                                        nearestRange = range;
                                        nearestPos = p.uPosition;
                                        nearestAstroId = p.astroId;
                                    }

                                    /* If targeting hives, do not bypass them */
                                    if (!isHive)
                                    {
                                        var hiveSys = GameMain.spaceSector.dfHives[localStar.index];
                                        while (hiveSys != null)
                                        {
                                            if (hiveSys.realized && hiveSys.hiveAstroId > 1000000)
                                            {
                                                ref var hiveAstro = ref GameMain.spaceSector.astros[hiveSys.hiveAstroId - 1000000];
                                                /* Divide by 4, so that the real range is 2 times of the calculated range,
                                                   which means the minimal range allowed is 4000 */
                                                var range = (playerPos - hiveAstro.uPos).sqrMagnitude / 4.0;
                                                if (range < nearestRange)
                                                {
                                                    nearestRange = range;
                                                    nearestPos = hiveAstro.uPos;
                                                    nearestAstroId = hiveSys.hiveAstroId;
                                                }
                                            }

                                            hiveSys = hiveSys.nextSibling;
                                        }
                                    }

                                    if (nearestAstroId != _indicatorAstroId && nearestRange < 2000.0 * 2000.0)
                                    {
                                        Vector3 leavingDirection = (playerPos - nearestPos).normalized;
                                        var dot = Vector3.Dot(leavingDirection, _direction);
                                        if (dot < 0)
                                        {
                                            var cross = Vector3.Cross(_direction, leavingDirection);
                                            _direction = Vector3.Cross(leavingDirection, cross).normalized;
                                        }
                                        else
                                        {
                                            _direction = leavingDirection;
                                        }
                                    }
                                }
                            }

                            Vector3 uVel = player.uVelocity;
                            var speed = uVel.magnitude;
                            if (player.warping)
                            {
                                _speedUp = false;
                                if (autoCruise)
                                {
                                    /* Speed down if too close */
                                    var actionSail = controller.actionSail;
                                    if (distance < GalaxyData.LY * 1.5)
                                    {
                                        if (distance < actionSail.currentWarpSpeed * distance switch
                                            {
                                                > GalaxyData.LY * 0.6 => 0.33,
                                                > GalaxyData.LY * 0.3 => 0.5,
                                                > GalaxyData.LY * 0.1 => 0.66,
                                                _ => 1.0
                                            })
                                        {
                                            controller.input0.y = -1f;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var mecha = player.mecha;
                                var energyRatio = mecha.coreEnergy / mecha.coreEnergyCap;
                                if (_canUseWarper && GameMain.localPlanet == null && distance > GalaxyData.AU * DistanceToWarp.Value && energyRatio >= 0.8 && player.mecha.UseWarper())
                                {
                                    player.warpCommand = true;
                                    VFAudio.Create("warp-begin", player.transform, Vector3.zero, true);
                                }
                                else
                                {
                                    /* Speed up if needed */
                                    _speedUp = autoCruise && AutoBoostEnabled.Value && speed + 0.2f < player.mecha.maxSailSpeed && energyRatio >= 0.1;
                                }
                            }

                            /* Update direction, gracefully rotate for 2 degrees for each frame */
                            var angle = Vector3.Angle(uVel, _direction);
                            if (angle < 2f)
                            {
                                player.uVelocity = _direction * speed;
                            }
                            else
                            {
                                player.uVelocity = Vector3.Slerp(uVel, _direction * speed, 2f / angle);
                            }
                            break;
                        default:
                            _speedUp = false;
                            break;
                    }
                })
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._sailSpeedUp), MethodType.Getter)]
        private static IEnumerable<CodeInstruction> VFInput_sailSpeedUp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ret)
            );
            matcher.Repeat(m => m.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(AutoNavigation), nameof(_speedUp))),
                new CodeInstruction(OpCodes.Or)
            ).Advance(1));
            return matcher.InstructionEnumeration();
        }

        /* Disable Lock Cursor Mode on entering sail panel
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UISailPanel), nameof(UISailPanel._OnOpen))]
        public static void OnOpen_Prefix()
        {
            if (_aimingEnabled)
            {
                UIRoot.instance.uiGame.disableLockCursor = true;
            }
        }
        */
    }
}
