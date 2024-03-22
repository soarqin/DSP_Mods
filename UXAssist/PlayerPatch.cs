using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace UXAssist;

public static class PlayerPatch
{
    public static ConfigEntry<bool> EnhancedMechaForgeCountControlEnabled;
    public static ConfigEntry<bool> HideTipsForSandsChangesEnabled;
    public static ConfigEntry<bool> AutoNavigationEnabled;
    
    public static void Init()
    {
        EnhancedMechaForgeCountControlEnabled.SettingChanged += (_, _) => EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChangesEnabled.SettingChanged += (_, _) => HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        AutoNavigationEnabled.SettingChanged += (_, _) => AutoNavigation.Enable(AutoNavigationEnabled.Value);
        EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        AutoNavigation.Enable(AutoNavigationEnabled.Value);
    }
    
    public static void Uninit()
    {
        EnhancedMechaForgeCountControl.Enable(false);
        HideTipsForSandsChanges.Enable(false);
        AutoNavigation.Enable(false);
    }

    private static class EnhancedMechaForgeCountControl
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(EnhancedMechaForgeCountControl));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

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

    private static class HideTipsForSandsChanges
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(HideTipsForSandsChanges));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

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

    private static class AutoNavigation
    {
        private static Harmony _patch;

        private static int _indicatorAstroId;
        private static bool _speedUp;
        /*
        private static bool _aimingEnabled;
        */
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(AutoNavigation));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerController_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BuildModel), nameof(BuildModel.LateGameTickIgnoreActive)))
            );
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                Transpilers.EmitDelegate((PlayerController controller) =>
                {
                    _speedUp = false;
                    if (controller.movementStateInFrame != EMovementState.Sail) return;
                    var navi = controller.player.navigation;
                    if (navi.indicatorAstroId != _indicatorAstroId)
                    {
                        _indicatorAstroId = navi.indicatorAstroId;
                        if (_indicatorAstroId == 0) return;
                    }
                    else if (_indicatorAstroId == 0) return;
                    var player = controller.player;
                    var playerPos = player.uPosition;
                    ref var astro = ref GameMain.galaxy.astrosData[_indicatorAstroId];
                    var vec = astro.uPos - playerPos;
                    if (vec.magnitude - astro.uRadius < 800.0) return;
                    var direction = vec.normalized;
                    var localStar = GameMain.localStar;
                    if (localStar != null)
                    {
                        var nearestRange = (playerPos - localStar.uPosition).sqrMagnitude;
                        var nearestPos = localStar.uPosition;
                        var nearestAstroId = localStar.id;
                        foreach (var p in localStar.planets)
                        {
                            var range = (playerPos - p.uPosition).sqrMagnitude;
                            if (range >= nearestRange) continue;
                            nearestRange = range;
                            nearestPos = p.uPosition;
                            nearestAstroId = p.id;
                        }

                        if (nearestAstroId != _indicatorAstroId && nearestRange < 2000.0 * 2000.0)
                        {
                            var vec2 = (playerPos - nearestPos).normalized;
                            var dot = Vector3.Dot(vec2, direction);
                            if (dot >= 0)
                            {
                                direction = vec2;
                            }
                            else
                            {
                                var cross = Vector3.Cross(direction, vec2);
                                direction = -Vector3.Cross(cross, vec2).normalized;
                            }
                        }
                    }
                    var uVel = player.uVelocity;
                    var speed = uVel.magnitude;
                    _speedUp = !player.warping && speed < 2000.0 - 0.1;
                    player.uVelocity = direction * speed;
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

        /*
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
