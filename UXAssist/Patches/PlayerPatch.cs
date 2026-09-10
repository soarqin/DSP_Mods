using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Common.Patching;
using Object = UnityEngine.Object;

namespace UXAssist.Patches;

public class PlayerPatch : PatchImpl<PlayerPatch>
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

	public static ConfigEntry<bool>   UseNewNavigationAlgorithm { get; set; } = null!;
	public static ConfigEntry<bool>   StopOnArrivalAndInput     { get; set; } = null!;
	public static ConfigEntry<bool>   UseWarper                 { get; set; } = null!;
	public static ConfigEntry<double> UseWarperMinimalEnergy    { get; set; } = null!;
	public static ConfigEntry<double> UseWarperDistance         { get; set; } = null!;
	public static ConfigEntry<bool>   UseSpeedUp                { get; set; } = null!;
	public static ConfigEntry<double> UseSpeedUpMinimalEnergy   { get; set; } = null!;
	public static ConfigEntry<double> DFHiveFollowDistance      { get; set; } = null!;
	public static ConfigEntry<double> DFCarrierFollowDistance   { get; set; } = null!;

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
                _toggleAllStarsNameKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.Tab, 0, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.UI | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleAllStarsName",
            canOverride = true
        }
        );
                _autoDriveKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.K, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleAutoCruise",
            canOverride = true
        });
                                EnhancedMechaForgeCountControlEnabled.SettingChanged += (_, _) => EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChangesEnabled.SettingChanged += (_, _) => HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        ShortcutKeysForStarsNameEnabled.SettingChanged += (_, _) => ShortcutKeysForStarsName.Enable(ShortcutKeysForStarsNameEnabled.Value);
        AutoNavigationEnabled.SettingChanged += (_, _) => AutoNavigation.Enable(AutoNavigationEnabled.Value);
        AutoNavigationEnabled.SettingChanged += (_, _) => Functions.UIFunctions.UpdateToggleAutoCruiseCheckButtonVisiblility();
        AutoCruiseEnabled.SettingChanged += (_, _) => Functions.UIFunctions.UpdateToggleAutoCruiseCheckButtonVisiblility();
        AutoNavigationG.Init();
    }

    public static void Start()
    {
        EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        ShortcutKeysForStarsName.Enable(ShortcutKeysForStarsNameEnabled.Value);
        AutoNavigation.Enable(AutoNavigationEnabled.Value);
		AutoNavigationG.Enable(true);
        Enable(true);
    }

    public static void OnInputUpdate()
    {
        ShortcutKeysForStarsName.OnInputUpdate();
        if (_autoDriveKey.keyValue)
        {
            if (UseNewNavigationAlgorithm.Value)
                AutoNavigationG.Toggle();
            else
                AutoNavigation.ToggleAutoCruise();
        }
    }

    public static void Uninit()
    {
        Enable(false);
        EnhancedMechaForgeCountControl.Enable(false);
        HideTipsForSandsChanges.Enable(false);
        ShortcutKeysForStarsName.Enable(false);
        AutoNavigation.Enable(false);
		AutoNavigationG.Enable(false);
    }
    // Harmony transpiler: UIStarmapStar__OnLateUpdate_Transpiler
    // Target: UIStarmapStar._OnLateUpdate
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIStarmapStar), nameof(UIStarmapStar._OnLateUpdate))]
    private static IEnumerable<CodeInstruction> UIStarmapStar__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        Label? jumpPos = null;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapStar), nameof(UIStarmapStar.projectedCoord))),
            new CodeMatch(OpCodes.Ldarg_0)
        ).Advance(2).MatchForward(false,
            new CodeMatch(ci => ci.IsStloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.Branches(out jumpPos))
        ).Advance(3);
        var labels = matcher.Labels;
        matcher.Labels = [];
        matcher.CreateLabel(out var jumpPos2);
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(ShortcutKeysForStarsName.ShowAllStarsNameStatus))).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ceq),
            new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch.ShortcutKeysForStarsName), nameof(ShortcutKeysForStarsName.ForceShowAllStarsName))),
            new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Functions.UI.StarmapFilterUI), nameof(Functions.UI.StarmapFilterUI.ShowStarName))),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapStar), nameof(UIStarmapStar.star))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StarData), nameof(StarData.index))),
            new CodeInstruction(OpCodes.Ldelem_I1),
            new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(ShortcutKeysForStarsName.ShowAllStarsNameStatus))),
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Ceq),
            new CodeInstruction(OpCodes.Brfalse, jumpPos2),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Stloc_1),
            new CodeInstruction(OpCodes.Br, jumpPos.Value)
        );
        return matcher.InstructionEnumeration();
    }


    private class EnhancedMechaForgeCountControl : PatchImpl<EnhancedMechaForgeCountControl>
    {
        // Harmony transpiler: UIReplicatorWindow_OnOkButtonClick_Transpiler
        // Target: UIReplicatorWindow.OnOkButtonClick
        // Fallback: None — patch will fail loudly if the target method body changes.
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
        // Harmony transpiler: UIReplicatorWindow_OnPlusButtonClick_Transpiler
        // Target: UIReplicatorWindow.OnPlusButtonClick, UIReplicatorWindow.OnMinusButtonClick
        // Fallback: None — patch will fail loudly if the target method body changes.
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

    private class HideTipsForSandsChanges : PatchImpl<HideTipsForSandsChanges>
    {
        // Harmony transpiler: Player_SetSandCount_Transpiler
        // Target: Player.SetSandCount
        // Fallback: None — patch will fail loudly if the target method body changes.
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

    public class ShortcutKeysForStarsName : PatchImpl<ShortcutKeysForStarsName>
    {
        public static int ShowAllStarsNameStatus;
        public static bool ForceShowAllStarsName;
        public static bool ForceShowAllStarsNameExternal;

        public static void ToggleAllStarsName()
        {
            ShowAllStarsNameStatus = (ShowAllStarsNameStatus + 1) % 3;
        }

        public static void OnInputUpdate()
        {
            if (!UIRoot.instance.uiGame.starmap.active) return;
            var enabled = ShortcutKeysForStarsNameEnabled.Value;
            if (!enabled)
            {
                ForceShowAllStarsName = ForceShowAllStarsNameExternal;
                return;
            }
            if (_toggleAllStarsNameKey.keyValue)
            {
                ToggleAllStarsName();
            }
            ForceShowAllStarsName = ForceShowAllStarsNameExternal || _showAllStarsNameKey.IsKeyPressing();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnOpen))]
        private static void UIStarmap__OnOpen_Prefix()
        {
            ShowAllStarsNameStatus = 0;
        }
        /*
                // Harmony transpiler: UIStarmapPlanet__OnLateUpdate_Transpiler
                // Target: UIStarmapPlanet._OnLateUpdate
                // Fallback: None — patch will fail loudly if the target method body changes.
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
                    matcher.Labels = [];
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
                // Harmony transpiler: UIStarmapDFHive__OnLateUpdate_Transpiler
                // Target: UIStarmapDFHive._OnLateUpdate
                // Fallback: None — patch will fail loudly if the target method body changes.
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
                    matcher.Labels = [];
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

    public class AutoNavigation : PatchImpl<AutoNavigation>
    {
        private static bool _canUseWarper;
        private static int _indicatorAstroId;
        private static bool _speedUp;
        private static Vector3 _direction;
        private static EMovementState _movementState = EMovementState.Walk;

        public static int IndicatorAstroId => _indicatorAstroId;

        protected override void OnEnable()
        {
            _canUseWarper = false;
            _indicatorAstroId = 0;
            _speedUp = false;
            _direction = Vector3.zero;
            _movementState = EMovementState.Walk;
        }

        public static void ToggleAutoCruise()
        {
			if(UseNewNavigationAlgorithm.Value)
				return;
            AutoCruiseEnabled.Value = !AutoCruiseEnabled.Value;
            if (!DSPGame.IsMenuDemo && GameMain.isRunning)
            {
                UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead((AutoCruiseEnabled.Value ? I18NKeys.AutoCruiseOn : I18NKeys.AutoCruiseOff).Translate());
            }
        }

        private static bool UpdateMovementState(PlayerController controller)
        {
            var movementStateChanged = controller.movementStateInFrame != _movementState;
            if (movementStateChanged)
            {
                _movementState = controller.movementStateInFrame;
            }
            return movementStateChanged;
        }
        // Harmony transpiler: PlayerController_GameTick_Transpiler
        // Target: PlayerController.GameTick
        // Fallback: None — patch will fail loudly if the target method body changes.
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
					if (UseNewNavigationAlgorithm.Value)
						return;
                    /* Update target astro if changed */
                    _speedUp = false;
                    var player = controller.player;
                    if (player.mecha.thrusterLevel < 2) return;
                    var navi = player.navigation;
                    var astroChanged = navi.indicatorAstroId != _indicatorAstroId;
                    if (astroChanged)
                    {
                        _indicatorAstroId = navi.indicatorAstroId;
                        Functions.UIFunctions.UpdateToggleAutoCruiseCheckButtonVisiblility();
                    }
                    if (_indicatorAstroId == 0) return;
                    switch (controller.movementStateInFrame)
                    {
                        case EMovementState.Walk:
                        case EMovementState.Drift:
                            if (!AutoCruiseEnabled.Value) return;
                            if (GameMain.localStar?.astroId == _indicatorAstroId) return;
                            UpdateMovementState(controller);
                            /* Press jump key to fly */
                            controller.input0.z = 1f;
                            break;
                        case EMovementState.Fly:
                            if (!AutoCruiseEnabled.Value) return;
                            if (GameMain.localStar?.astroId == _indicatorAstroId) return;
                            UpdateMovementState(controller);
                            /* Keep pressing jump and pullup key to sail */
                            controller.input0.y = -1f;
                            controller.input1.y = 1f;
                            break;
                        case EMovementState.Sail:
                            if (VFInput._pullUp.pressing || VFInput._pushDown.pressing || VFInput._moveLeft.pressing || VFInput._moveRight.pressing ||
                                (!player.warping && UIRoot.instance.uiGame.disableLockCursor && (VFInput._moveForward.pressing || VFInput._moveBackward.pressing)))
                                return;
                            var movementStateChanged = UpdateMovementState(controller);
                            var playerPos = player.uPosition;
                            var isHive = _indicatorAstroId > 1000000;
                            ref var astro = ref isHive ? ref GameMain.spaceSector.astros[_indicatorAstroId - 1000000] : ref GameMain.galaxy.astrosData[_indicatorAstroId];
                            var astroVec = astro.uPos - playerPos;
                            var distance = astroVec.magnitude;
                            astroVec = astroVec.normalized;
                            if (astroChanged || movementStateChanged)
                            {
                                controller.actionSail.sailPoser.targetURotWanted = Quaternion.LookRotation(astroVec);
                            }
                            if (distance < astro.type switch
                            {
                                EAstroType.Planet => 800.0 + astro.uRadius,
                                EAstroType.Star => 4000.0 + astro.uRadius,
                                EAstroType.EnemyHive => 800.0,
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
                                _direction = astroVec;

                                /* Check nearest astroes, try to bypass them */
                                var localStar = GameMain.localStar;
                                _canUseWarper = autoCruise && player.mecha.thrusterLevel >= 3 && !player.warping && player.mecha.HasWarper();
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
                                                /* Divide by 36, so that the real range is 6 times of the calculated range,
                                                   which means the minimal range allowed is 12000 */
                                                var range = (playerPos - hiveAstro.uPos).sqrMagnitude / 36.0;
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
                                if (autoCruise)
                                {
                                    var actionSail = controller.actionSail;
                                    _speedUp = actionSail.currentWarpSpeed < actionSail.maxWarpSpeed;
                                    /* Speed down if too close */
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
                                            _speedUp = false;
                                        }
                                    }
                                }
                                else
                                {
                                    _speedUp = false;
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
                                    controller.actionSail.sailPoser.targetURotWanted = Quaternion.LookRotation(astroVec);
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
        // Harmony transpiler: VFInput_sailSpeedUp_Transpiler
        // Target: VFInput._sailSpeedUp (getter)
        // Fallback: None — patch will fail loudly if the target method body changes.
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
            UIRoot.instance.uiGame.disableLockCursor = true;
        }
        */
    }

	#region New navigation algorithm

	public class AutoNavigationG : PatchImpl<AutoNavigationG>
	{
		private const double Epsilon                     = 1e-18;
		private const int    DarkFogAstroIdStart         = 1000000;
		private const int    StarArriveThresholdRadius   = 4000;
		private const int    PlanetArriveThresholdRadius = 100;
		private const double StarSafeRedis               = 2000;
		private const double PlanetSafeRedis             = 2000;
		private const double HiveSafeRedis               = GalaxyData.AU * 0.5;

		private const double FollowModeUseWarpDistance       = GalaxyData.AU * 1.5;
		private const double ResidualCompensationMaxDistance = GalaxyData.AU * 0.5;
		private const double ResidualCompensationSpeedRatio  = 0.5;
		private const double ResidualCompensationMinSpeed    = 100;

		private static bool EnableTag { get; set; }
		private static bool PauseTag  { get; set; }
		private static Text UiTipText { get; set; }

		private static int             TargetId                    { get; set; }
		private static VectorLF3       TargetUniversePosition      { get; set; }
		private static VectorLF3       TargetUniverseVelocity      { get; set; }
		private static ESpaceGuideType TargetType                  { get; set; }
		private static double          TargetArriveThresholdRadius { get; set; }

		private static List<SpaceObject> SpaceObjects      { get; set; } = new(64);
		private static EMovementState    LastMovementState { get; set; }
		private static bool              EscapeMode        { get; set; }
		private static int               EscapeAstroId     { get; set; }
		private static bool              FollowModeLock    { get; set; }

		private static bool _normalizingNavigationMode;

		protected override void OnEnable( )
		{
			StartG(  );
			global::UXAssist.Common.GameLogic.OnGameBegin += StartG;
			global::UXAssist.Common.GameLogic.OnGameEnd += ResetState;
		}

		protected override void OnDisable()
		{
			global::UXAssist.Common.GameLogic.OnGameBegin -= StartG;
			global::UXAssist.Common.GameLogic.OnGameEnd -= ResetState;
			ResetState();
			if (UiTipText != null)
			{
				Object.Destroy(UiTipText.gameObject);
				UiTipText = null;
			}
		}

		public static void Awake(ConfigFile config)
		{
			UseNewNavigationAlgorithm = config.Bind(
				"Player",
				nameof(UseNewNavigationAlgorithm),
				true,
				"Use the new navigation algorithm");
			StopOnArrivalAndInput = config.Bind("Player", nameof(StopOnArrivalAndInput), true, "Stop auto-navigation on arrival or manual input");
			UseWarper             = config.Bind("Player", nameof(UseWarper),             true, "Use warp during auto-navigation");
			UseWarperMinimalEnergy = config.Bind<double>(
				"Player",
				nameof(UseWarperMinimalEnergy),
				800,
				new ConfigDescription("Minimum energy required to use warp (MJ)", new AcceptableValueRange<double>(50, 1000)));
			UseWarperDistance = config.Bind("Player", nameof(UseWarperDistance), 2.0,
				new ConfigDescription("Minimum distance to use warp (AU)", new AcceptableValueRange<double>(0.5, 20)));
			UseSpeedUp        = config.Bind("Player", nameof(UseSpeedUp), true, "Use automatic acceleration");
			UseSpeedUpMinimalEnergy = config.Bind<double>(
				"Player",
				nameof(UseSpeedUpMinimalEnergy),
				100,
				new ConfigDescription("Minimum energy required for automatic acceleration (MJ)", new AcceptableValueRange<double>(50, 1000)));
			DFHiveFollowDistance = config.Bind(
				"Player",
				nameof(DFHiveFollowDistance),
				0.5,
				new ConfigDescription("Dark Fog Hive follow distance (AU)", new AcceptableValueRange<double>(0.1, 5)));
			DFCarrierFollowDistance = config.Bind<double>(
				"Player",
				nameof(DFCarrierFollowDistance),
				1000,
				new ConfigDescription("Dark Fog Carrier follow distance (m)", new AcceptableValueRange<double>(100, 3000)));
		}

		public static void Init()
		{
			AutoNavigationEnabled.SettingChanged += NavigationModeChanged;
			UseNewNavigationAlgorithm.SettingChanged += NavigationModeChanged;
			NormalizeNavigationMode();
		}

		public static void StartG( )
		{
			if (UiTipText != null || UIRoot.instance?.uiGame?.generalTips?.modeText == null)
				return;
			Text originText = UIRoot.instance.uiGame.generalTips.modeText;
			UiTipText = Object.Instantiate(originText, originText.transform.parent);
			UiTipText.gameObject.SetActive(false);
			UiTipText.rectTransform.anchoredPosition = new Vector2(0f, 160f);
			UiTipText.fontStyle = FontStyle.Normal;
			UiTipText.text                           = I18NKeys.AutoNavigationActive.Translate();
		}

		public static void Toggle()
		{
			if (EnableTag)
				StopAutoNavigation();
			else
				StartAutoNavigation( );
		}

		private static void StartAutoNavigation( )
		{
			if (DSPGame.IsMenuDemo || ! GameMain.isRunning)
				return;

			EnableTag = true;
			Reset( );
			UIRealtimeTip.Popup(I18NKeys.AutoNavigationStarted.Translate(), sound: false);
			UiTipText?.gameObject.SetActive(true);
		}

		private static void StopAutoNavigation(bool showTip = true)
		{
			bool wasEnabled = EnableTag;
			EnableTag = false;
			PauseTag = false;
			if (wasEnabled && showTip && !DSPGame.IsMenuDemo && GameMain.isRunning)
				UIRealtimeTip.Popup(I18NKeys.AutoNavigationStopped.Translate(), sound: false);
			UiTipText?.gameObject.SetActive(false);
		}

		private static void Reset( )
		{
			PauseTag          = false;
			TargetId          = 0;
			LastMovementState = EMovementState.Walk;
			EscapeMode        = false;
			FollowModeLock    = false;

			EscapeAstroId = 0;
			TargetUniversePosition = default;
			TargetUniverseVelocity = default;
			TargetType = default;
			TargetArriveThresholdRadius = 0;
			SpaceObjects.Clear();
		}

		private static void ResetState()
		{
			StopAutoNavigation(false);
			Reset();
		}

		private static void NavigationModeChanged(object sender, EventArgs args) => NormalizeNavigationMode();

		private static void NormalizeNavigationMode()
		{
			if (_normalizingNavigationMode || !AutoNavigationEnabled.Value || !UseNewNavigationAlgorithm.Value)
				return;
			_normalizingNavigationMode = true;
			UseNewNavigationAlgorithm.Value = false;
			_normalizingNavigationMode = false;
			StopAutoNavigation(false);
		}

		private static void Pause( ) => PauseTag = true;

		private static void Resume( ) => PauseTag = false;

		private static bool IsEnable => EnableTag && ! PauseTag;

		/// <summary>
		/// Performs precondition checks and refreshes the current navigation target.
		/// </summary>
		private static bool PreCheckAndRefreshTarget(Player player)
		{
			int previousTargetId = TargetId;
			ESpaceGuideType previousTargetType = TargetType;
			if (! UseNewNavigationAlgorithm.Value)
			{
				StopAutoNavigation( );
				return true;
			}
			if (AutoNavigationEnabled.Value)
			{
				StopAutoNavigation( );
				return true;
			}
			if (IsEnable is not true)
				return true;
			if (player.mecha.thrusterLevel < 2)
			{
				StopAutoNavigation( );
				return true;
			}
			if (VFInput._pullUp.pressing
				|| VFInput._pushDown.pressing
				|| VFInput._moveLeft.pressing
				|| VFInput._moveRight.pressing
				|| VFInput._moveForward.pressing
				|| VFInput._moveBackward.pressing)
			{
				if (StopOnArrivalAndInput.Value)
					StopAutoNavigation( );
				return true;
			}

			PlayerNavigation navigation       = player.navigation;
			int              indicatorAstroId = navigation.indicatorAstroId;
			if (indicatorAstroId != 0)
			{
				TargetId = indicatorAstroId;
				if (indicatorAstroId > DarkFogAstroIdStart)
				{
					TargetType = ESpaceGuideType.DFHive;
					int astroIndex = indicatorAstroId - DarkFogAstroIdStart;
					if (astroIndex < 0 || astroIndex >= GameMain.spaceSector.astros.Length)
					{
						StopAutoNavigation();
						return true;
					}
					AstroData astro = GameMain.spaceSector.astros[astroIndex];
					VectorLF3 lPos  = default;
					astro.VelocityU(ref lPos, out Vector3 velocity);
					TargetUniverseVelocity      = velocity;
					TargetUniversePosition      = astro.uPos;
					TargetArriveThresholdRadius = DFHiveFollowDistance.Value * GalaxyData.AU;
				} else
				{
					bool isStar = indicatorAstroId % 100 == 0;
					TargetType = isStar ? ESpaceGuideType.Star : ESpaceGuideType.Planet;
					if (isStar)
					{
						StarData star = GameMain.galaxy.StarById(indicatorAstroId / 100);
						if (star == null)
						{
							StopAutoNavigation();
							return true;
						}
						TargetUniversePosition      = star.uPosition;
						TargetArriveThresholdRadius = GameMain.galaxy.astrosData[indicatorAstroId].uRadius + StarArriveThresholdRadius;
					} else
					{
						PlanetData planet = GameMain.galaxy.PlanetById(indicatorAstroId);
						if (planet == null)
						{
							StopAutoNavigation();
							return true;
						}
						TargetUniversePosition      = planet.uPosition;
						TargetArriveThresholdRadius = planet.realRadius + PlanetArriveThresholdRadius;
					}
				}
			} else if (navigation.indicatorEnemyId != 0)
			{
				TargetId   = navigation.indicatorEnemyId;
				TargetType = ESpaceGuideType.DFCarrier;
				if (navigation.indicatorEnemyId < 0 || navigation.indicatorEnemyId >= GameMain.data.spaceSector.enemyPool.Length)
				{
					StopAutoNavigation();
					return true;
				}
				EnemyData ptr = GameMain.data.spaceSector.enemyPool[navigation.indicatorEnemyId];
				if (ptr.id != navigation.indicatorEnemyId)
				{
					StopAutoNavigation();
					return true;
				}
				GameMain.data.spaceSector.TransformFromAstro_ref(ptr.astroId, out VectorLF3 uPosition, ref ptr.pos);
				TargetUniversePosition      = uPosition;
				TargetUniverseVelocity      = ptr.vel;
				TargetArriveThresholdRadius = DFCarrierFollowDistance.Value;
			} else
			{
				StopAutoNavigation( );
				return true;
			}
			if (TargetId != previousTargetId || TargetType != previousTargetType)
			{
				EscapeMode = false;
				EscapeAstroId = 0;
				FollowModeLock = false;
			}
			return false;
		}

		/// <summary>
		/// Steers toward the target while avoiding nearby obstacles.
		/// </summary>
		private static void SailToTarget(PlayerController controller, double distance)
		{
			if (controller.movementStateInFrame != EMovementState.Sail)
				return;

			StarData localStar = GameMain.localStar;
			SpaceObjects.Clear( );
			if (localStar != null)
			{
				if (localStar.astroId != TargetId)
				{
					SpaceObjects.Add(
						new SpaceObject(
							astroId: localStar.astroId,
							position: localStar.uPosition,
							radius: GameMain.galaxy.astrosData[localStar.astroId].uRadius,
							radiusOffset: StarSafeRedis));
				}
				foreach (PlanetData planet in localStar.planets)
				{
					if (planet.astroId == TargetId)
						continue;
					SpaceObjects.Add(
						new SpaceObject(
							astroId: planet.astroId,
							position: planet.uPosition,
							radius: planet.realRadius,
							radiusOffset: PlanetSafeRedis));
				}
				if (TargetType is not ESpaceGuideType.DFHive)
				{
					EnemyDFHiveSystem hiveSys = GameMain.spaceSector.dfHives[localStar.index];
					while (hiveSys != null)
					{
						if (hiveSys is { realized: true, hiveAstroId: > DarkFogAstroIdStart }
							&& hiveSys.hiveAstroId != TargetId)
						{
							SpaceObjects.Add(
								new SpaceObject(
									astroId: hiveSys.hiveAstroId,
									position: GameMain.spaceSector.astros[hiveSys.hiveAstroId - DarkFogAstroIdStart].uPos,
									radius: 0,
									radiusOffset: HiveSafeRedis));
						}
						hiveSys = hiveSys.nextSibling;
					}
				}
			}

			Player    player       = controller.player;
			double    currentSpeed = player.uVelocity.magnitude;
			VectorLF3 targetDir    = ComputeDirection(player.uPosition, SpaceObjects);
			VectorLF3 currentDir   = SafeNorm(player.uVelocity, targetDir);
			if (player.warping)
				UpdateSailVelocityAndRotation(controller, currentDir, currentSpeed, targetDir, currentSpeed);
			else
			{
				Mecha mecha = player.mecha;
				bool canWarp = UseWarper.Value
							   && mecha.coreEnergy           > UseWarperMinimalEnergy.Value * 1000 * 1000
							   && mecha.coreEnergy           > mecha.warpStartPowerPerSpeed * controller.actionSail.maxWarpSpeed
							   && player.mecha.thrusterLevel >= 3
							   && player.mecha.HasWarper( )
							   && GameMain.localPlanet == null
							   && distance             > GalaxyData.AU * UseWarperDistance.Value;
				if (canWarp && player.mecha.UseWarper( ))
				{
					UpdateSailVelocityAndRotation(controller, currentDir, currentSpeed, targetDir, currentSpeed);
					player.warpCommand = true;
					VFAudio.Create("warp-begin", player.transform, Vector3.zero, true);
				} else
					UpdateSailVelocityAndRotation(controller, currentDir, currentSpeed, targetDir, mecha.maxSailSpeed);
			}
		}

		/// <summary>
		/// Computes a target direction with obstacle avoidance.
		/// </summary>
		/// <param name="playerPos">Player position.</param>
		/// <param name="spaceObjects">Nearby obstacles.</param>
		/// <returns></returns>
		private static VectorLF3 ComputeDirection(VectorLF3 playerPos, List<SpaceObject> spaceObjects)
		{
			const double lookForward = GalaxyData.AU * 1;

			VectorLF3 toTarget    = TargetUniversePosition - playerPos;
			double    toTargetSqr = toTarget.sqrMagnitude;
			if (toTargetSqr < Epsilon)
				return default;
			VectorLF3 baseDir = toTarget.normalized;
			if (spaceObjects.Count == 0)
				return baseDir;

			var       upRef   = VectorLF3.unit_y;
			VectorLF3 avoid   = default;
			VectorLF3 liftRaw = default;
			double    sumW    = 0;
			foreach (SpaceObject spaceObject in spaceObjects)
			{
				const double enterEscapeAltitude = 200;
				double       quitEscapeAltitude  = Math.Max(600, spaceObject.Radius * 1.5);

				double safeR = spaceObject.Radius + spaceObject.RadiusOffset;
				if (safeR <= 0.0)
					continue;
				VectorLF3 toObject    = spaceObject.Position - playerPos;
				double    toObjectSqr = toObject.sqrMagnitude;
				if (toObjectSqr > toTargetSqr)
					continue;

				double toObjectMagnitude = Math.Sqrt(toObjectSqr);
				if (toObjectMagnitude > lookForward)
					continue;

				double    toObjectAltitude = toObjectMagnitude - spaceObject.Radius;
				double    forward          = VectorLF3.Dot(toObject, baseDir);
				VectorLF3 pushVector       = baseDir * forward - toObject;

				// Escape from an obstacle before resuming normal avoidance.
				if (EscapeMode is not true && toObjectAltitude < enterEscapeAltitude)
				{
					EscapeMode    = true;
					EscapeAstroId = spaceObject.AstroId;
				}
				if (EscapeMode)
				{
					if (spaceObject.AstroId != EscapeAstroId)
						continue;
					if (toObjectAltitude > quitEscapeAltitude)
					{
						EscapeMode    = false;
						EscapeAstroId = 0;
					} else
					{
						VectorLF3 escapeDirection = - toObject / toObjectMagnitude;
						VectorLF3 cDirection = VectorLF3.Dot(escapeDirection, baseDir) < - 0.5 ?
							pushVector.sqrMagnitude < Epsilon ? default : pushVector.normalized
							: baseDir;
						double bias = (quitEscapeAltitude - toObjectAltitude) / (quitEscapeAltitude - enterEscapeAltitude);
						bias = bias switch
						{
							< 0.0 => 0.0,
							> 1.0 => 1.0,
							_     => bias
						};
						VectorLF3 direction = escapeDirection * bias + cDirection * (1 - bias);
						return direction.sqrMagnitude < Epsilon ? escapeDirection : direction.normalized;
					}
				}

				// Only consider obstacles in front of the player.
				if (forward <= 0.0)
					continue;
				double pushSqr       = pushVector.sqrMagnitude;
				double pushMagnitude = Math.Sqrt(pushSqr);
				if (pushSqr < Epsilon)
					continue;
				if (pushMagnitude >= safeR)
					continue;

				// Weight and combine avoidance forces by distance.
				double pushW = (safeR - pushMagnitude) / safeR;
				pushW = Clamp(pushW, 0, 1);
				double forwardW = 1 - (forward - safeR) / (lookForward - safeR);
				forwardW = Clamp(forwardW, 0, 1);
				double    weight  = pushW      * pushW * (3.0 - 2.0 * pushW) * forwardW * forwardW;
				VectorLF3 pushDir = pushVector / pushMagnitude;
				avoid += pushDir * weight;
				sumW  += weight;

				// Add lift to avoid cancellation from symmetric obstacles.
				VectorLF3 n = VectorLF3.Cross(baseDir, pushDir);
				if (VectorLF3.Dot(n, upRef) < 0)
					n = - n;
				liftRaw += n * weight;
			}

			double avoidSqr = avoid.sqrMagnitude;
			if (avoidSqr < Epsilon)
				return baseDir;

			// Apply lift when the combined lateral avoidance is insufficient.
			double needLift = 0;
			if (sumW > Epsilon)
			{
				double avoidMagnitude = Math.Sqrt(avoidSqr);
				double ratio          = avoidMagnitude / sumW;
				needLift = 1 - Clamp(ratio, 0, 1);
			}
			const double needLiftMin = 0.4;
			needLift = (needLift - needLiftMin) / (1 - needLiftMin);
			needLift = Clamp(needLift, 0, 1);
			VectorLF3 lift = default;
			if (needLift > 0)
			{
				const double liftScale = 0.25;
				const double liftMax   = 0.6;
				lift = liftRaw * needLift * liftScale;
				if (lift.sqrMagnitude > liftMax * liftMax)
					lift = lift.normalized * liftMax;
			}

			VectorLF3 mixed    = baseDir + avoid + lift;
			double    mixedSqr = mixed.sqrMagnitude;
			return mixedSqr < Epsilon ? baseDir : mixed.normalized;
		}

		/// <summary>
		/// Steers toward and follows a moving target without avoidance after close-range lock.
		/// <param name="distance">Distance to the target.</param>
		/// </summary>
		private static void FollowMovingTarget(PlayerController controller, double distance)
		{
			double arriveRadius            = TargetArriveThresholdRadius;
			double exitLockRadius          = arriveRadius * 1.05;
			double axialTolerance          = arriveRadius * 0.8;
			double lateralTolerance        = arriveRadius * 0.8;
			double axialSlowDownDistance   = arriveRadius * 10;
			double lateralSlowDownDistance = arriveRadius * 5;

			Player    player            = controller.player;
			double    mechaMaxSailSpeed = player.mecha.maxSailSpeed;
			VectorLF3 playerPos         = player.uPosition;
			VectorLF3 playerVelocity    = player.uVelocity;
			double    playerSpeed       = playerVelocity.magnitude;
			VectorLF3 targetPos         = TargetUniversePosition;
			VectorLF3 targetVelocity    = TargetUniverseVelocity;
			VectorLF3 toTarget          = targetPos - playerPos;
			if (distance < Epsilon)
				return;
			VectorLF3 toTargetDir       = toTarget / distance;
			VectorLF3 targetVelocityDir = SafeNorm(targetVelocity, toTargetDir);

			if (distance > FollowModeUseWarpDistance)
			{
				SailToTarget(controller, distance);
				return;
			}
			if (player.warping)
				player.warpCommand = false;

			switch (distance)
			{
				case >= FollowModeUseWarpDistance:
					SailToTarget(controller, distance);
					return;
				case < FollowModeUseWarpDistance when player.warping:
					player.warpCommand = false;
					return;
				case < FollowModeUseWarpDistance when distance > exitLockRadius:
					FollowModeLock = false;
					break;
				case < FollowModeUseWarpDistance when distance <= arriveRadius:
					FollowModeLock = true;
					break;
			}
			// Match the target velocity after entering the follow sphere.
			if (FollowModeLock)
			{
				UpdateSailVelocityAndRotation(
					controller: controller,
					currentDir: SafeNorm(playerVelocity, targetVelocityDir),
					currentSpeed: playerSpeed,
					targetDir: targetVelocityDir,
					targetSpeed: targetVelocity.magnitude,
					forceSpeedUp: false);
				return;
			}

			// Resolve the position error into axial and lateral components.
			double    axialError    = VectorLF3.Dot(toTarget, targetVelocityDir);
			VectorLF3 lateralVector = toTarget - targetVelocityDir * axialError;
			double    lateralError  = lateralVector.magnitude;
			VectorLF3 lateralDir    = lateralError > Epsilon ? lateralVector / lateralError : default;

			// Axial approach.
			double axialEffectiveError = Math.Max(Math.Abs(axialError) - axialTolerance, 0.0);
			double axialApproachSpeed = mechaMaxSailSpeed * axialEffectiveError / (axialEffectiveError + axialSlowDownDistance);
			VectorLF3 axialDir = axialError >= 0 ? targetVelocityDir : - targetVelocityDir;

			// Lateral approach.
			double lateralEffectiveError = Math.Max(lateralError - lateralTolerance, 0.0);
			double lateralApproachSpeed =
				mechaMaxSailSpeed * lateralEffectiveError / (lateralEffectiveError + lateralSlowDownDistance);

			VectorLF3 relativeVelocityDesired = axialDir * axialApproachSpeed + lateralDir * lateralApproachSpeed;

			// Residual velocity compensation.
			if (distance > arriveRadius)
			{
				double cWeight = (distance - arriveRadius) / ResidualCompensationMaxDistance;
				cWeight = Clamp(cWeight, 0, 1);
				cWeight = cWeight * cWeight * (3.0 - 2.0 * cWeight);
				double cSpeed = player.mecha.maxSailSpeed * cWeight * ResidualCompensationSpeedRatio
								+ ResidualCompensationMinSpeed;
				double dot = VectorLF3.Dot(relativeVelocityDesired, toTargetDir);
				if (dot < cSpeed)
					relativeVelocityDesired += toTargetDir * cSpeed;
			}

			// Combine target and approach velocities.
			VectorLF3 desiredVelocity  = targetVelocity + relativeVelocityDesired;
			double    desiredSpeed     = Math.Min(desiredVelocity.magnitude, mechaMaxSailSpeed);
			VectorLF3 desiredDirection = SafeNorm(desiredVelocity, toTargetDir);
			VectorLF3 playerDir        = SafeNorm(playerVelocity,  desiredDirection);
			UpdateSailVelocityAndRotation(
				controller: controller,
				currentDir: playerDir,
				currentSpeed: playerSpeed,
				targetDir: desiredDirection,
				targetSpeed: desiredSpeed,
				forceSpeedUp: false);
		}

		private static VectorLF3 SafeNorm(VectorLF3 vector, VectorLF3 fallback, double sqr = double.NaN)
		{
			sqr = double.IsNaN(sqr) ? vector.sqrMagnitude : sqr;
			return sqr < Epsilon ? fallback : vector.normalized;
		}

		/// <summary>
		/// Updates sail acceleration, steering, rotation, and visual velocity.
		/// </summary>
		private static void UpdateSailVelocityAndRotation(
			PlayerController controller,
			VectorLF3        currentDir,
			double           currentSpeed,
			VectorLF3        targetDir,
			double           targetSpeed,
			bool             forceSpeedUp = false)
		{
			PlayerMove_Sail sail         = controller.actionSail;
			Player          player       = controller.player;
			Mecha           mecha        = player.mecha;
			VectorLF3       currentVel   = currentDir * currentSpeed;
			double          desiredSpeed = Math.Min(targetSpeed, mecha.maxSailSpeed);
			double          stepSpeed    = currentSpeed;

			// Acceleration and braking.
			bool speedUp = forceSpeedUp || (UseSpeedUp.Value && mecha.coreEnergy > UseSpeedUpMinimalEnergy.Value * 1000 * 1000);
			if (speedUp && desiredSpeed > currentSpeed)
			{
				double dSpeed = Clamp(currentSpeed * 0.02, 7.0, sail.max_acc);
				dSpeed = Math.Min(dSpeed, desiredSpeed       - currentSpeed);
				dSpeed = Math.Min(dSpeed, mecha.maxSailSpeed - currentSpeed);
				if (dSpeed > 0)
					stepSpeed = currentSpeed + dSpeed * sail.UseSailEnergy(dSpeed);
			} else if (desiredSpeed < currentSpeed)
			{
				VectorLF3 dVelocityBrake = currentVel * 0.008;
				sail.UseSailEnergy(ref dVelocityBrake, 1.5);
				stepSpeed = Math.Max(0, (currentVel - dVelocityBrake).magnitude);
			}

			// Smooth velocity changes.
			VectorLF3 targetVelocity = targetDir * stepSpeed;
			float     angle          = Vector3.Angle(targetVelocity, currentVel);
			var       t              = (float)(1.6 / Mathf.Max(10, angle));
			VectorLF3 dVelocity      = (VectorLF3)Vector3.Slerp(currentVel, targetVelocity, t) - currentVel;
			sail.UseSailEnergy(ref dVelocity, 0.36);
			VectorLF3 newVelocity = currentVel + dVelocity;

			sail.input_aff_1 = 1.0;
			player.uVelocity = newVelocity;
			UpdateRotation(controller: controller);
		}

		/// <summary>
		/// Updates player orientation.
		/// </summary>
		private static void UpdateRotation(PlayerController controller)
		{
			Player          player      = controller.player;
			PlayerMove_Sail sail        = controller.actionSail;
			VectorLF3       newVelocity = player.uVelocity;
			// Use the camera up vector to stabilize orientation.
			if (newVelocity.magnitude > Epsilon)
			{
				VectorLF3 forward       = newVelocity.normalized;
				Vector3   cameraUp      = controller.actionSail.sailPoser.targetURot * Vector3.up;
				VectorLF3 cameraUpWorld = cameraUp;
				double    dot           = VectorLF3.Dot(cameraUpWorld, forward);
				VectorLF3 projectedUp   = cameraUpWorld - forward * dot;
				Vector3   newUp;
				if (projectedUp.sqrMagnitude < Epsilon)
				{
					Vector3 cameraRight = controller.actionSail.sailPoser.targetURot * Vector3.right;
					newUp = Vector3.Cross(cameraRight, forward);
					if (newUp.sqrMagnitude < Epsilon)
						newUp = Vector3.up;
					else
						newUp.Normalize( );
				} else
					newUp = ((Vector3)projectedUp).normalized;
				Quaternion targetRot = Quaternion.LookRotation(forward, newUp);
				float      rotAngle  = Quaternion.Angle(player.uRotation, targetRot);
				float      rotT      = Mathf.Min(0.15f, 10f / Mathf.Max(10f, rotAngle));
				player.uRotation = Quaternion.Slerp(player.uRotation, targetRot, rotT);
			}
			// Keep visual velocity consistent with universal velocity.
			PlanetData localPlanet = GameMain.localPlanet;
			if (localPlanet != null)
			{
				VectorLF3 planetVel = localPlanet.GetUniversalVelocityAtLocalPoint(GameMain.gameTime, player.position);
				sail.visual_uvel = newVelocity - planetVel;
			} else
				sail.visual_uvel = newVelocity;
		}

		private static bool UpdateMovementState(EMovementState state)
		{
			if (LastMovementState == state)
				return false;
			LastMovementState = state;
			return true;
		}

		private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

		[HarmonyPatch(typeof(GameMain), nameof(GameMain.Pause)), HarmonyPrefix]
		private static void PausePrefix( ) => Pause( );

		[HarmonyPatch(typeof(GameMain), nameof(GameMain.Resume)), HarmonyPrefix]
		private static void ResumePrefix( ) => Resume( );

		// Harmony transpiler: PlayerController_GameTick_Transpiler
		// Target: PlayerController.GameTick
		// Fallback: Return the original instructions if the insertion point cannot be found.
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GameTick))]
		private static IEnumerable<CodeInstruction> PlayerController_GameTick_Transpiler(
			IEnumerable<CodeInstruction> instructions,
			ILGenerator                  generator)
		{
			var original = new List<CodeInstruction>(instructions);
			var matcher = new CodeMatcher(original, generator);
			matcher.MatchForward(
				false,
				new CodeMatch(
					OpCodes.Callvirt,
					AccessTools.Method(typeof(BuildModel), nameof(BuildModel.EarlyGameTickIgnoreActive)))
			).Advance(1).InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldarg_0),
				Transpilers.EmitDelegate((PlayerController controller) =>
				{
					switch (controller.movementStateInFrame)
					{
						case EMovementState.Walk:
						case EMovementState.Drift:
							if (PreCheckAndRefreshTarget(controller.player) || DetermineTargetLocal( ))
								return;
							controller.input0.z = 1f;
							break;
						case EMovementState.Fly:
							if (PreCheckAndRefreshTarget(controller.player) || DetermineTargetLocal( ))
								return;
							controller.input1.y = 1f;
							controller.input0.y = 1f;
							break;
					}
				})
			);
			return matcher.Finish(original, UXAssist.Logger, nameof(PlayerController_GameTick_Transpiler));

			// Detect arrival while walking or flying.
			static bool DetermineTargetLocal( )
			{
				if (TargetId != 0 && (TargetType != ESpaceGuideType.Planet || GameMain.localPlanet?.astroId != TargetId))
					return false;
				StopAutoNavigation( );
				return true;
			}
		}

		[HarmonyPatch(typeof(PlayerMove_Sail), nameof(PlayerMove_Sail.GameTick)), HarmonyPostfix]
		// ReSharper disable once InconsistentNaming
		private static void PlayerMoveSailPostfix(PlayerMove_Sail __instance)
		{
			PlayerController controller = __instance.controller;
			Player           player     = __instance.player;
			if (PreCheckAndRefreshTarget(player))
				return;
			bool      isStateChanged = UpdateMovementState(controller.movementStateInFrame);
			VectorLF3 playerPos      = player.uPosition;
			VectorLF3 targetVector   = TargetUniversePosition - playerPos;
			double    distance       = targetVector.magnitude;
			if (isStateChanged && targetVector.sqrMagnitude >= Epsilon)
				__instance.sailPoser.targetURotWanted = Quaternion.LookRotation(targetVector);

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (TargetType)
			{
				case ESpaceGuideType.Star:
				case ESpaceGuideType.Planet:
					// Stop after reaching a fixed target.
					if (distance < TargetArriveThresholdRadius)
					{
						if (player.warping)
							player.warpCommand = false;
						StopAutoNavigation( );
						return;
					}
					SailToTarget(controller, distance);
					break;
				case ESpaceGuideType.DFHive:
				case ESpaceGuideType.DFCarrier:
					FollowMovingTarget(controller, distance);
					break;
			}
		}
	}

	public readonly struct SpaceObject(int astroId, VectorLF3 position, double radius, double radiusOffset)
	{
		public int       AstroId      { get; } = astroId;
		public VectorLF3 Position     { get; } = position;
		public double    Radius       { get; } = radius;
		public double    RadiusOffset { get; } = radiusOffset;
	}

	#endregion
}
