using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches;

public class FactoryPatch : PatchImpl<FactoryPatch>
{
    public static ConfigEntry<bool> UnlimitInteractiveEnabled;
    public static ConfigEntry<bool> RemoveSomeConditionEnabled;
    public static ConfigEntry<bool> NightLightEnabled;
    public static ConfigEntry<float> NightLightAngleX;
    public static ConfigEntry<float> NightLightAngleY;
    public static ConfigEntry<bool> RemoveBuildRangeLimitEnabled;
    public static ConfigEntry<bool> LargerAreaForUpgradeAndDismantleEnabled;
    public static ConfigEntry<bool> LargerAreaForTerraformEnabled;
    public static ConfigEntry<bool> OffGridBuildingEnabled;
    public static ConfigEntry<bool> TreatStackingAsSingleEnabled;
    public static ConfigEntry<bool> QuickBuildAndDismantleLabsEnabled;
    public static ConfigEntry<bool> ProtectVeinsFromExhaustionEnabled;
    public static ConfigEntry<bool> DoNotRenderEntitiesEnabled;
    public static ConfigEntry<bool> DragBuildPowerPolesEnabled;
    public static ConfigEntry<bool> DragBuildPowerPolesAlternatelyEnabled;
    public static ConfigEntry<bool> AutoConstructEnabled;
    public static ConfigEntry<bool> BeltSignalsForBuyOutEnabled;
    public static ConfigEntry<bool> TankFastFillInAndTakeOutEnabled;
    public static ConfigEntry<int> TankFastFillInAndTakeOutMultiplier;
    public static ConfigEntry<bool> CutConveyorBeltEnabled;
    public static ConfigEntry<bool> TweakBuildingBufferEnabled;
    public static ConfigEntry<int> AssemblerBufferTimeMultiplier;
    public static ConfigEntry<int> AssemblerBufferMininumMultiplier;
    public static ConfigEntry<int> LabBufferMaxCountForAssemble;
    public static ConfigEntry<int> LabBufferExtraCountForAdvancedAssemble;
    public static ConfigEntry<int> LabBufferMaxCountForResearch;
    public static ConfigEntry<int> ReceiverBufferCount;
    public static ConfigEntry<int> EjectorBufferCount;
    public static ConfigEntry<int> SiloBufferCount;
    public static ConfigEntry<bool> ShortcutKeysForBlueprintCopyEnabled;
    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsEnabled;
    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeBranches;
    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeInserters;

    private static PressKeyBind _doNotRenderEntitiesKey;
    private static PressKeyBind _offgridfForPathsKey;
    private static PressKeyBind _cutConveyorBeltKey;
    private static PressKeyBind _dismantleBlueprintSelectionKey;
    private static PressKeyBind _selectAllBuildingsInBlueprintCopyKey;

    private static int _tankFastFillInAndTakeOutMultiplierRealValue = 2;

    public static void Init()
    {
        _doNotRenderEntitiesKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleDoNotRenderEntities",
            canOverride = true
        }
        );
        I18N.Add("KEYToggleDoNotRenderEntities", "[UXA] Toggle Do Not Render Factory Entities", "[UXA] 切换不渲染工厂建筑实体");
        _offgridfForPathsKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.UI | KeyBindConflict.FLYING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "OffgridForPaths",
            canOverride = true
        }
        );
        I18N.Add("KEYOffgridForPaths", "[UXA] Build belts offgrid", "[UXA] 脱离网格建造传送带");
        _cutConveyorBeltKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.X, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "CutConveyorBelt",
            canOverride = true
        }
        );
        I18N.Add("KEYCutConveyorBelt", "[UXA] Cut conveyor belt", "[UXA] 切割传送带");
        _dismantleBlueprintSelectionKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.X, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.KEYBOARD_KEYBIND,
            name = "DismantleBlueprintSelection",
            canOverride = true
        }
        );
        I18N.Add("KEYDismantleBlueprintSelection", "[UXA] Dismantle blueprint selected buildings", "[UXA] 拆除蓝图选中的建筑");
        _selectAllBuildingsInBlueprintCopyKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.A, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.KEYBOARD_KEYBIND,
            name = "SelectAllBuildingsInBlueprintCopy",
            canOverride = true
        }
        );
        I18N.Add("KEYSelectAllBuildingsInBlueprintCopy", "[UXA] Select all buildings in Blueprint Copy Mode", "[UXA] 蓝图复制时选择所有建筑");

        BeltSignalsForBuyOut.InitPersist();
        ProtectVeinsFromExhaustion.InitConfig();
        UnlimitInteractiveEnabled.SettingChanged += (_, _) => UnlimitInteractive.Enable(UnlimitInteractiveEnabled.Value);
        RemoveSomeConditionEnabled.SettingChanged += (_, _) => RemoveSomeConditionBuild.Enable(RemoveSomeConditionEnabled.Value);
        NightLightEnabled.SettingChanged += (_, _) => NightLight.Enable(NightLightEnabled.Value);
        NightLightAngleX.SettingChanged += (_, _) => NightLight.UpdateSunlightAngle();
        NightLightAngleY.SettingChanged += (_, _) => NightLight.UpdateSunlightAngle();
        RemoveBuildRangeLimitEnabled.SettingChanged += (_, _) => RemoveBuildRangeLimit.Enable(RemoveBuildRangeLimitEnabled.Value);
        LargerAreaForUpgradeAndDismantleEnabled.SettingChanged += (_, _) => LargerAreaForUpgradeAndDismantle.Enable(LargerAreaForUpgradeAndDismantleEnabled.Value);
        LargerAreaForTerraformEnabled.SettingChanged += (_, _) => LargerAreaForTerraform.Enable(LargerAreaForTerraformEnabled.Value);
        OffGridBuildingEnabled.SettingChanged += (_, _) => OffGridBuilding.Enable(OffGridBuildingEnabled.Value);
        TreatStackingAsSingleEnabled.SettingChanged += (_, _) => TreatStackingAsSingle.Enable(TreatStackingAsSingleEnabled.Value);
        QuickBuildAndDismantleLabsEnabled.SettingChanged += (_, _) => QuickBuildAndDismantleLab.Enable(QuickBuildAndDismantleLabsEnabled.Value);
        ProtectVeinsFromExhaustionEnabled.SettingChanged += (_, _) => ProtectVeinsFromExhaustion.Enable(ProtectVeinsFromExhaustionEnabled.Value);
        DoNotRenderEntitiesEnabled.SettingChanged += (_, _) => DoNotRenderEntities.Enable(DoNotRenderEntitiesEnabled.Value);
        DragBuildPowerPolesEnabled.SettingChanged += (_, _) => DragBuildPowerPoles.Enable(DragBuildPowerPolesEnabled.Value);
        DragBuildPowerPolesAlternatelyEnabled.SettingChanged += (_, _) => DragBuildPowerPoles.AlternatelyChanged();
        AutoConstructEnabled.SettingChanged += (_, _) => Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        BeltSignalsForBuyOutEnabled.SettingChanged += (_, _) => BeltSignalsForBuyOut.Enable(BeltSignalsForBuyOutEnabled.Value);
        TankFastFillInAndTakeOutEnabled.SettingChanged += (_, _) => TankFastFillInAndTakeOut.Enable(TankFastFillInAndTakeOutEnabled.Value);
        TankFastFillInAndTakeOutMultiplier.SettingChanged += (_, _) => UpdateTankFastFillInAndTakeOutMultiplierRealValue();
        TweakBuildingBufferEnabled.SettingChanged += (_, _) => TweakBuildingBuffer.Enable(TweakBuildingBufferEnabled.Value);
        AssemblerBufferTimeMultiplier.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshAssemblerBufferMultipliers();
        AssemblerBufferMininumMultiplier.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshAssemblerBufferMultipliers();
        LabBufferMaxCountForAssemble.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshLabBufferMaxCountForAssemble();
        LabBufferExtraCountForAdvancedAssemble.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshLabBufferMaxCountForAssemble();
        LabBufferMaxCountForResearch.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshLabBufferMaxCountForResearch();
        ReceiverBufferCount.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshReceiverBufferCount();
        EjectorBufferCount.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshEjectorBufferCount();
        SiloBufferCount.SettingChanged += (_, _) => TweakBuildingBuffer.RefreshSiloBufferCount();
        PressShiftToTakeWholeBeltItemsEnabled.SettingChanged += (_, _) => PressShiftToTakeWholeBeltItems.Enable(PressShiftToTakeWholeBeltItemsEnabled.Value);
    }

    public static void Start()
    {
        UnlimitInteractive.Enable(UnlimitInteractiveEnabled.Value);
        RemoveSomeConditionBuild.Enable(RemoveSomeConditionEnabled.Value);
        NightLight.Enable(NightLightEnabled.Value);
        RemoveBuildRangeLimit.Enable(RemoveBuildRangeLimitEnabled.Value);
        LargerAreaForUpgradeAndDismantle.Enable(LargerAreaForUpgradeAndDismantleEnabled.Value);
        LargerAreaForTerraform.Enable(LargerAreaForTerraformEnabled.Value);
        OffGridBuilding.Enable(OffGridBuildingEnabled.Value);
        TreatStackingAsSingle.Enable(TreatStackingAsSingleEnabled.Value);
        QuickBuildAndDismantleLab.Enable(QuickBuildAndDismantleLabsEnabled.Value);
        ProtectVeinsFromExhaustion.Enable(ProtectVeinsFromExhaustionEnabled.Value);
        DoNotRenderEntities.Enable(DoNotRenderEntitiesEnabled.Value);
        DragBuildPowerPoles.Enable(DragBuildPowerPolesEnabled.Value);
        BeltSignalsForBuyOut.Enable(BeltSignalsForBuyOutEnabled.Value);
        TankFastFillInAndTakeOut.Enable(TankFastFillInAndTakeOutEnabled.Value);
        TweakBuildingBuffer.Enable(TweakBuildingBufferEnabled.Value);
        PressShiftToTakeWholeBeltItems.Enable(PressShiftToTakeWholeBeltItemsEnabled.Value);

        Enable(true);
        UpdateTankFastFillInAndTakeOutMultiplierRealValue();
    }

    public static void Uninit()
    {
        Enable(false);

        PressShiftToTakeWholeBeltItems.Enable(false);
        TweakBuildingBuffer.Enable(false);
        TankFastFillInAndTakeOut.Enable(false);
        BeltSignalsForBuyOut.Enable(false);
        DragBuildPowerPoles.Enable(false);
        DoNotRenderEntities.Enable(false);
        ProtectVeinsFromExhaustion.Enable(false);
        QuickBuildAndDismantleLab.Enable(false);
        TreatStackingAsSingle.Enable(false);
        OffGridBuilding.Enable(false);
        LargerAreaForTerraform.Enable(false);
        LargerAreaForUpgradeAndDismantle.Enable(false);
        RemoveBuildRangeLimit.Enable(false);
        NightLight.Enable(false);
        RemoveSomeConditionBuild.Enable(false);
        UnlimitInteractive.Enable(false);

        BeltSignalsForBuyOut.UninitPersist();
    }

    private static void UpdateTankFastFillInAndTakeOutMultiplierRealValue()
    {
        _tankFastFillInAndTakeOutMultiplierRealValue = Mathf.Max(1, TankFastFillInAndTakeOutMultiplier.Value) * 2;
    }

    public static void OnInputUpdate()
    {
        if (_doNotRenderEntitiesKey.keyValue)
            DoNotRenderEntitiesEnabled.Value = !DoNotRenderEntitiesEnabled.Value;
        if (CutConveyorBeltEnabled.Value && _cutConveyorBeltKey.keyValue)
        {
            var raycast = GameMain.mainPlayer.controller?.cmd.raycast;
            int beltId;
            if (raycast != null && raycast.castEntity.id > 0 && (beltId = raycast.castEntity.beltId) > 0)
            {
                var cargoTraffic = raycast.planet.factory.cargoTraffic;
                Functions.FactoryFunctions.CutConveyorBelt(cargoTraffic, beltId);
            }
        }
        if (ShortcutKeysForBlueprintCopyEnabled.Value)
        {
            if (_dismantleBlueprintSelectionKey.keyValue)
                Functions.FactoryFunctions.DismantleBlueprintSelectedBuildings();
            if (_selectAllBuildingsInBlueprintCopyKey.keyValue)
                Functions.FactoryFunctions.SelectAllBuildingsInBlueprintCopy();
        }
    }

    public static void Export(BinaryWriter w)
    {
        var storage = BeltSignalsForBuyOut.DarkFogItemsInVoid;
        for (var i = 0; i < 6; i++)
            w.Write(storage[i]);
    }

    public static void Import(BinaryReader r)
    {
        var storage = BeltSignalsForBuyOut.DarkFogItemsInVoid;
        for (var i = 0; i < 6; i++)
            storage[i] = r.ReadInt32();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConnGizmoGraph), MethodType.Constructor)]
    private static IEnumerable<CodeInstruction> ConnGizmoGraph_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(256))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConnGizmoGraph), nameof(ConnGizmoGraph.SetPointCount))]
    private static IEnumerable<CodeInstruction> ConnGizmoGraph_SetPointCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(256))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path._OnInit))]
    private static IEnumerable<CodeInstruction> BuildTool_Path__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(160))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), MethodType.Constructor)]
    private static IEnumerable<CodeInstruction> BuildTool_Reform_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(100))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 900));
        return matcher.InstructionEnumeration();
    }

    #region Auto Construct
    private static int _lastPrebuildCount = -1;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.NotifyFactoryLoaded))]
    private static void PlanetData_NotifyFactoryLoaded_Postfix()
    {
        Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        _lastPrebuildCount = -1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.UnloadFactory))]
    private static void PlanetData_UnloadFactory_Postfix()
    {
        Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        _lastPrebuildCount = -1;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
    private static void PlayerAction_Rts_GameTick_Prefix(PlayerAction_Rts __instance, long timei)
    {
        if (timei % 60L != 0) return;
        var planet = GameMain.localPlanet;
        if (planet == null || !planet.factoryLoaded) return;
        var factory = planet.factory;
        var prebuildCount = factory.prebuildCount;
        if (_lastPrebuildCount != prebuildCount)
        {
            if (_lastPrebuildCount <= 0 || prebuildCount == 0)
            {
                Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            }
            _lastPrebuildCount = prebuildCount;
            Functions.UIFunctions.UpdateConstructCountText(prebuildCount);
        }
        if (prebuildCount <= 0) return;
        if (!AutoConstructEnabled.Value) return;
        var player = __instance.player;
        if (prebuildCount <= player.mecha.constructionModule.buildTargetTotalCount) return;
        if (player.orders.orderCount > 0) return;
        var prebuilds = factory.prebuildPool;
        var minDist = float.MaxValue;
        var minIndex = 0;
        var playerPos = player.position;
        for (var i = factory.prebuildCursor - 1; i > 0; i--)
        {
            ref var prebuild = ref prebuilds[i];
            if (prebuild.id != i || prebuild.isDestroyed) continue;
            if (prebuild.itemRequired > 0)
            {
                if (player.package.GetItemCount(prebuild.protoId) < prebuild.itemRequired) continue;
            }
            var dist = (prebuild.pos - playerPos).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                minIndex = i;
            }
        }
        if (minIndex == 0) return;
        if ((prebuilds[minIndex].pos - playerPos).sqrMagnitude < 400f) return;
        if (player.movementState == EMovementState.Walk && player.mecha.thrusterLevel >= 1)
        {
            player.controller.actionWalk.SwitchToFly();
            return;
        }
        player.Order(OrderNode.MoveTo(prebuilds[minIndex].pos), false);
    }
    #endregion

    public class NightLight : PatchImpl<NightLight>
    {
        private static bool _nightlightInitialized;
        private static bool _mechaOnEarth;
        private static AnimationState _sail;
        private static Light _sunlight;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameEnd += OnGameEnd;
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameEnd -= OnGameEnd;
            if (_sunlight)
            {
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
            }
            _sail = null;
            _mechaOnEarth = false;
            _nightlightInitialized = false;
        }

        private static void OnGameEnd()
        {
            if (_sunlight)
            {
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
            }
            _sail = null;
            _mechaOnEarth = false;
            _nightlightInitialized = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateSunlightAngle()
        {
            if (!_sunlight) return;
            _sunlight.transform.rotation =
                Quaternion.LookRotation(-GameMain.mainPlayer.transform.up + GameMain.mainPlayer.transform.forward * NightLightAngleX.Value / 10f +
                                        GameMain.mainPlayer.transform.right * NightLightAngleY.Value / 10f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.LateUpdate))]
        public static void GameMain_LateUpdate_Postfix(GameMain __instance)
        {
            if (__instance.isMenuDemo || !__instance._running) return;

            if (!_nightlightInitialized)
            {
                if (!GameMain.mainPlayer.controller.model.gameObject.activeInHierarchy) return;
                if (_sail == null) _sail = GameMain.mainPlayer.animator.sails[GameMain.mainPlayer.animator.sailAnimIndex];
                _nightlightInitialized = true;
            }

            var sailing = _sail && _sail.enabled;
            if (_mechaOnEarth)
            {
                if (!sailing)
                {
                    UpdateSunlightAngle();
                    return;
                }
                _mechaOnEarth = false;
                if (!_sunlight) return;
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
                return;
            }

            if (sailing) return;
            _mechaOnEarth = true;
            if (_sunlight == null)
            {
                _sunlight = GameMain.universeSimulator?.LocalStarSimulator()?.sunLight;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StarSimulator), nameof(StarSimulator.LateUpdate))]
        private static IEnumerable<CodeInstruction> StarSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform)))
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NightLight), nameof(_mechaOnEarth))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
                new CodeInstruction(OpCodes.Stloc_0),
                new CodeInstruction(OpCodes.Br_S, label2)
            );
            matcher.Labels.Add(label1);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_0)
            ).Advance(1).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetSimulator), nameof(PlanetSimulator.LateRefresh))]    
        private static IEnumerable<CodeInstruction> PlanetSimulator_LateRefresh_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // var vec = (NightlightEnabled ? GameMain.mainPlayer.transform.up : (Quaternion.Inverse(localPlanet.runtimeRotation) * (__instance.planetData.star.uPosition - __instance.planetData.uPosition).normalized));
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NightLight), nameof(_mechaOnEarth))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
                new CodeInstruction(OpCodes.Stloc_2),
                new CodeInstruction(OpCodes.Br_S, label2)
            );
            matcher.Labels.Add(label1);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryModel), nameof(FactoryModel.whiteMode0)))
            ).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }
    }

    private class UnlimitInteractive : PatchImpl<UnlimitInteractive>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GetObjectSelectDistance))]
        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GetObjectSelectDistance_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_R4, 10000f);
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    private class RemoveSomeConditionBuild : PatchImpl<RemoveSomeConditionBuild>
    {
        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* search for:
             *  ldloc.s	V_8 (8)
             *  ldfld	class PrefabDesc BuildPreview::desc
             *  ldfld	bool PrefabDesc::isInserter
             *  brtrue	2358 (1C12) ldloc.s V_8 (8)
             *  ldloca.s	V_10 (10)
             *  call	instance float32 [UnityEngine.CoreModule]UnityEngine.Vector3::get_magnitude()
             */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isInserter))),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldloca_S),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.magnitude)))
            );
            /* Change to:
             *  Ldloc.s	V_8 (8)
             *  ldfld	class PrefabDesc BuildPreview::desc
             *  ldfld	bool PrefabDesc::isEjector
             *  brfalse	2358 (1C12) ldloc.s V_8 (8)
             */
            matcher.Advance(2);
            matcher.Operand = AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isEjector));
            matcher.Advance(1);
            matcher.Opcode = OpCodes.Brfalse;
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* search for:
             * ldloc.s	V_88 (88)
             * ldloc.s	V_120 (120)
             * brtrue.s	2054 (173A) ldc.i4.s 17
             * ldc.i4.s	EBuildCondition.JointCannotLift (19)
             * br.s	2055 (173C) stfld valuetype EBuildCondition BuildPreview::condition
             * ldc.i4.s	EBuildCondition.TooBendToLift (18)
             * stfld	valuetype EBuildCondition BuildPreview::condition
             */
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs((int)EBuildCondition.JointCannotLift)),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs((int)EBuildCondition.TooBendToLift)),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
            );
            if (matcher.IsValid)
            {
                // Remove 7 instructions, if the following instruction is br/br.s, remove it as well
                var labels = matcher.Labels;
                matcher.Labels = [];
                matcher.RemoveInstructions(7);
                var opcode = matcher.Opcode;
                if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                    matcher.RemoveInstruction();
                matcher.Labels.AddRange(labels);
            }

            /* search for:
             * ldloc.s	V_88 (88)
             * ldc.i4.s	EBuildCondition.TooSteep(16)-EBuildCondition.InputConflict(20)
             * stfld	valuetype EBuildCondition BuildPreview::condition
             */
            matcher.Start().MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldloc_S || instr.opcode == OpCodes.Ldloc),
                new CodeMatch(instr =>
                    (instr.opcode == OpCodes.Ldc_I4_S || instr.opcode == OpCodes.Ldc_I4) &&
                    Convert.ToInt64(instr.operand) is >= (int)EBuildCondition.TooSteep and <= (int)EBuildCondition.InputConflict),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
            );
            if (matcher.IsValid)
            {
                // Remove 3 instructions, if the following instruction is br/br.s, remove it as well
                matcher.Repeat(codeMatcher =>
                {
                    var labels = codeMatcher.Labels;
                    codeMatcher.Labels = [];
                    codeMatcher.RemoveInstructions(3);
                    var opcode = codeMatcher.Opcode;
                    if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                        codeMatcher.RemoveInstruction();
                    codeMatcher.Labels.AddRange(labels);
                });
            }

            return matcher.InstructionEnumeration();
        }
    }

    private class RemoveBuildRangeLimit : PatchImpl<RemoveBuildRangeLimit>
    {
        protected override void OnEnable()
        {
            var controller = GameMain.mainPlayer?.controller;
            if (controller == null) return;
            controller.actionBuild?.clickTool?._OnInit();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnInit))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(15))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 512));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DetermineMoreChainTargets))]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DetermineMoreChainTargets))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildAreaLimitRemoval_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Patch (player.mecha.buildArea * player.mecha.buildArea) to 100000000 */
            matcher.MatchForward(false,
                new CodeMatch(),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.buildArea))),
                new CodeMatch(),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.buildArea))),
                new CodeMatch(OpCodes.Mul)
            );
            matcher.Repeat(m => m.RemoveInstructions(9).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, 100000000.0f)));
            return matcher.InstructionEnumeration();
        }
    }

    private class LargerAreaForUpgradeAndDismantle : PatchImpl<LargerAreaForUpgradeAndDismantle>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTools_CursorSizePatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(11))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4_S, 31));
            return matcher.InstructionEnumeration();
        }
    }

    private class LargerAreaForTerraform : PatchImpl<LargerAreaForTerraform>
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.brushSize))),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.Advance(1).SetAndAdvance(OpCodes.Ldc_I4_S, 30));
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10)),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.brushSize)))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4_S, 30));
            return matcher.InstructionEnumeration();
        }
    }

    public class OffGridBuilding : PatchImpl<OffGridBuilding>
    {
        // private const float SteppedRotationDegrees = 15f;

        private static bool _initialized;

        private static void SetupRichTextSupport()
        {
            if (_initialized) return;
            UIGeneralTips.instance.buildCursorTextComp.supportRichText = true;
            UIGeneralTips.instance.entityBriefInfo.entityNameText.supportRichText = true;
            _initialized = true;
        }

        private static void CalculateGridOffset(PlanetData planet, Vector3 pos, out float x, out float y, out float z)
        {
            var npos = pos.normalized;
            var segment = planet.aux.activeGrid?.segment ?? 200;
            var latitudeRadPerGrid = BlueprintUtils.GetLatitudeRadPerGrid(segment);
            var longitudeSegmentCount = BlueprintUtils.GetLongitudeSegmentCount(npos, segment);
            var longitudeRadPerGrid = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount, segment);
            var latitudeRad = BlueprintUtils.GetLatitudeRad(npos);
            var longitudeRad = BlueprintUtils.GetLongitudeRad(npos);
            x = longitudeRad / longitudeRadPerGrid;
            y = latitudeRad / latitudeRadPerGrid;
            z = (pos.magnitude - planet.realRadius - 0.2f) / 1.3333333f;
        }

        private static string FormatOffsetFloat(float f)
        {
            return f.ToString("0.0000").TrimEnd('0').TrimEnd('.');
        }

        private static PlanetData _lastPlanet;
        private static Vector3 _lastPos;
        private static string _lastOffsetText;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static void BuildTool_Click_CheckBuildConditions_Postfix(BuildTool __instance)
        {
            var cnt = __instance.buildPreviews.Count;
            if (cnt == 0) return;
            var preview = __instance.buildPreviews[cnt - 1];
            if (preview.desc.isInserter) return;
            var planet = __instance.planet;
            if (_lastPlanet != planet || _lastPos != preview.lpos)
            {
                SetupRichTextSupport();
                CalculateGridOffset(__instance.planet, preview.lpos, out var x, out var y, out var z);
                _lastPlanet = planet;
                _lastPos = preview.lpos;
                _lastOffsetText = z is < 0.001f and > -0.001f
                    ? $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>"
                    : $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>,<color=#bfbfffff>{FormatOffsetFloat(z)}</color>";
            }

            __instance.actionBuild.model.cursorText = $"({_lastOffsetText})\n" + __instance.actionBuild.model.cursorText;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIEntityBriefInfo__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo.entityNameText))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Text), nameof(Text.preferredWidth)))
            );
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((UIEntityBriefInfo entityBriefInfo) =>
                    {
                        var entity = entityBriefInfo.factory.entityPool[entityBriefInfo.entityId];
                        if (entity.inserterId > 0) return;
                        var planet = entityBriefInfo.factory.planet;
                        if (_lastPlanet != planet || _lastPos != entity.pos)
                        {
                            SetupRichTextSupport();
                            CalculateGridOffset(planet, entity.pos, out var x, out var y, out var z);
                            _lastPlanet = planet;
                            _lastPos = entity.pos;
                            _lastOffsetText = $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>,<color=#bfbfffff>{FormatOffsetFloat(z)}</color>";
                        }

                        entityBriefInfo.entityNameText.text += $" ({_lastOffsetText})";
                    }
                )
            );
            return matcher.InstructionEnumeration();
        }

        private static void MatchIgnoreGridAndCheckIfRotatable(CodeMatcher matcher, out Label? ifBlockEntryLabel, out Label? elseBlockEntryLabel)
        {
            Label? thisIfBlockEntryLabel = null;
            Label? thisElseBlockEntryLabel = null;

            matcher.MatchForward(false,
                new CodeMatch(ci => ci.Calls(AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._switchGridSnap)))),
                new CodeMatch(ci => ci.Branches(out thisElseBlockEntryLabel)),
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(ci => ci.LoadsConstant(EMinerType.Vein)),
                new CodeMatch(ci => ci.Branches(out thisIfBlockEntryLabel)),
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld)
            );

            ifBlockEntryLabel = thisIfBlockEntryLabel;
            elseBlockEntryLabel = thisElseBlockEntryLabel;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.UpdateRaycast))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> AllowOffGridConstruction(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            MatchIgnoreGridAndCheckIfRotatable(matcher, out var entryLabel, out _);

            if (matcher.IsInvalid)
                return instructions;

            matcher.Advance(2);
            matcher.Insert(new CodeInstruction(OpCodes.Br, entryLabel.Value));

            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> PreventDraggingWhenOffGrid(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            Label? exitLabel = null;

            matcher.MatchForward(false,
                new CodeMatch(ci => ci.Branches(out exitLabel)),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(ci => ci.LoadsConstant(1)),
                new CodeMatch(ci => ci.StoresField(AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.isDragging))))
            );

            if (matcher.IsInvalid)
                return instructions;

            matcher.Advance(1);
            matcher.Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._switchGridSnap))),
                new CodeInstruction(OpCodes.Brtrue, exitLabel)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateRaycast))]
        public static IEnumerable<CodeInstruction> AllowOffGridConstructionForPath(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.actionBuild))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerAction_Build), nameof(PlayerAction_Build.planetAux))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPos))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castTerrain))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.Snap), [typeof(Vector3), typeof(bool)])),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPosSnapped)))
            );

            if (matcher.IsInvalid)
                return matcher.InstructionEnumeration();

            var jmp0 = generator.DefineLabel();
            var jmp1 = generator.DefineLabel();
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(_offgridfForPathsKey))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                new CodeInstruction(OpCodes.Brfalse, jmp0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPos))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.normalized))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.planet))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetData), nameof(PlanetData.realRadius))),
                new CodeInstruction(OpCodes.Ldc_R4, 0.2f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", [typeof(Vector3), typeof(float)])),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPosSnapped))),
                new CodeInstruction(OpCodes.Br, jmp1)
            ).Labels.Add(jmp0);
            matcher.Advance(10).Labels.Add(jmp1);

            return matcher.InstructionEnumeration();
        }

        /*
        public static IEnumerable<CodeInstruction> PatchToPerformSteppedRotate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            MatchIgnoreGridAndCheckIfRotatable(matcher, out var ifBlockEntryLabel, out var elseBlockEntryLabel);

            if (matcher.IsInvalid)
                return instructions;

            while (!matcher.Labels.Contains(elseBlockEntryLabel.Value))
                matcher.Advance(1);

            Label? ifBlockExitLabel = null;

            matcher.MatchBack(false, new CodeMatch(ci => ci.Branches(out ifBlockExitLabel)));

            if (matcher.IsInvalid)
                return instructions;

            while (!matcher.Labels.Contains(ifBlockEntryLabel.Value))
                matcher.Advance(-1);

            var instructionToClone = matcher.Instruction.Clone();
            var overwriteWith = CodeInstruction.LoadField(typeof(VFInput), nameof(VFInput.control));

            matcher.SetAndAdvance(overwriteWith.opcode, overwriteWith.operand);
            matcher.Insert(instructionToClone);
            matcher.CreateLabel(out var existingEntryLabel);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Brfalse, existingEntryLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(OffGridBuilding), nameof(RotateStepped)),
                new CodeInstruction(OpCodes.Br, ifBlockExitLabel)
            );

            return matcher.InstructionEnumeration();
        }

        public static void RotateStepped(BuildTool_Click instance)
        {
            if (VFInput._rotate.onDown)
            {
                instance.yaw += SteppedRotationDegrees;
                instance.yaw = Mathf.Repeat(instance.yaw, 360f);
                instance.yaw = Mathf.Round(instance.yaw / SteppedRotationDegrees) * SteppedRotationDegrees;
            }

            if (VFInput._counterRotate.onDown)
            {
                instance.yaw -= SteppedRotationDegrees;
                instance.yaw = Mathf.Repeat(instance.yaw, 360f);
                instance.yaw = Mathf.Round(instance.yaw / SteppedRotationDegrees) * SteppedRotationDegrees;
            }
        }
        */
    }

    public class TreatStackingAsSingle : PatchImpl<TreatStackingAsSingle>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MonitorComponent), nameof(MonitorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> MonitorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MonitorComponent), nameof(MonitorComponent.GetCargoAtIndexByFilter)))
            );
            matcher.Advance(-3);
            var localVar = matcher.Operand;
            matcher.Advance(4).Insert(
                new CodeInstruction(OpCodes.Ldloca, localVar),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Cargo), nameof(Cargo.stack)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    private class QuickBuildAndDismantleLab : PatchImpl<QuickBuildAndDismantleLab>
    {
        private static bool DetermineMoreLabsForDismantle(BuildTool dismantle, int id)
        {
            if (!VFInput._chainReaction) return true;
            var factory = dismantle.factory;
            var proto = dismantle.GetItemProto(id);
            var protoId = proto.ID;
            var prefDesc = proto.prefabDesc;
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return true;
            factory.ReadObjectConn(id, 14, out _, out var nextId, out _);
            /* We keep last lab if selected lab is not the ground one */
            if (nextId > 0)
            {
                while (true)
                {
                    factory.ReadObjectConn(nextId, 14, out _, out var nextNextId, out _);
                    if (nextNextId <= 0) break;
                    var itemProto = dismantle.GetItemProto(nextId);
                    if (itemProto.ID != protoId) break;
                    var desc = itemProto.prefabDesc;
                    var pose = dismantle.GetObjectPose(nextId);
                    var preview = new BuildPreview
                    {
                        item = itemProto,
                        desc = desc,
                        lpos = pose.position,
                        lrot = pose.rotation,
                        lpos2 = pose.position,
                        lrot2 = pose.rotation,
                        objId = nextId,
                        needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                        isConnNode = true
                    };
                    dismantle.buildPreviews.Add(preview);
                    nextId = nextNextId;
                }
            }

            nextId = id;
            while (true)
            {
                factory.ReadObjectConn(nextId, 15, out _, out var nextId2, out _);
                if (nextId2 <= 0)
                {
                    factory.ReadObjectConn(nextId, 13, out _, out nextId2, out _);
                    if (nextId2 <= 0) break;
                }

                nextId = nextId2;
                var itemProto = dismantle.GetItemProto(nextId);
                var desc = itemProto.prefabDesc;
                var pose = dismantle.GetObjectPose(nextId);
                var preview = new BuildPreview
                {
                    item = itemProto,
                    desc = desc,
                    lpos = pose.position,
                    lrot = pose.rotation,
                    lpos2 = pose.position,
                    lrot2 = pose.rotation,
                    objId = nextId,
                    needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                    isConnNode = true
                };
                dismantle.buildPreviews.Add(preview);
            }

            return false;
        }

        private static void BuildLabsToTop(BuildTool_Click click)
        {
            if (!click.multiLevelCovering || !VFInput._chainReaction) return;
            var prefDesc = click.GetPrefabDesc(click.castObjectId);
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return;
            var levelMax = prefDesc.isLab ? GameMain.history.labLevel : GameMain.history.storageLevel;
            var factory = click.factory;
            var currLevel = 2;
            var nid = click.castObjectId;
            do
            {
                factory.ReadObjectConn(nid, 14, out _, out nid, out _);
                if (nid <= 0) break;
                currLevel++;
            } while (true);

            while (currLevel < levelMax)
            {
                click.UpdateRaycast();
                click.DeterminePreviews();
                click.UpdateCollidersForCursor();
                click.UpdateCollidersForGiantBp();
                var model = click.actionBuild.model;
                click.UpdatePreviewModels(model);
                if (!click.CheckBuildConditions())
                {
                    model.ClearAllPreviewsModels();
                    model.EarlyGameTickIgnoreActive();
                    return;
                }

                click.UpdatePreviewModelConditions(model);
                click.UpdateGizmos(model);
                click.CreatePrebuilds();
                currLevel++;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isBattleBase))),
                new CodeMatch(OpCodes.Brfalse)
            ).Advance(-1);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.objId))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(DetermineMoreLabsForDismantle))),
                new CodeInstruction(OpCodes.And)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnTick))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds)))
            ).Advance(2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(BuildLabsToTop)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    public class ProtectVeinsFromExhaustion : PatchImpl<ProtectVeinsFromExhaustion>
    {
        public static int KeepVeinAmount = 100;
        public static float KeepOilSpeed = 1f;
        private static int _keepOilAmount;

        public static void InitConfig()
        {
            _keepOilAmount = Math.Max((int)(KeepOilSpeed / 0.00004f + 0.5f), 2500);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
        private static bool MinerComponent_InternalUpdate_Prefix(PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister,
            ref MinerComponent __instance, out uint __result)
        {
            if (power < 0.1f)
            {
                __result = 0U;
                return false;
            }

            var res = 0U;
            int veinId;
            int times;
            switch (__instance.type)
            {
                case EMinerType.Vein:
                    var veinCount = __instance.veinCount;
                    if (veinCount <= 0)
                        break;

                    if (__instance.time <= __instance.period)
                    {
                        __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * veinCount);
                        res = 1U;
                    }

                    if (__instance.time < __instance.period)
                    {
                        break;
                    }

                    var currentVeinIndex = __instance.currentVeinIndex;
                    veinId = __instance.veins[currentVeinIndex];
                    lock (veinPool)
                    {
                        if (veinPool[veinId].id == 0)
                        {
                            __instance.RemoveVeinFromArray(currentVeinIndex);
                            __instance.GetMinimumVeinAmount(factory, veinPool);
                            veinCount = __instance.veinCount;
                            __instance.currentVeinIndex = veinCount > 1 ? currentVeinIndex % veinCount : 0;
                            __result = 0U;
                            return false;
                        }

                        if (__instance.productCount < 50 && (__instance.productId == 0 || __instance.productId == veinPool[veinId].productId))
                        {
                            __instance.productId = veinPool[veinId].productId;
                            times = __instance.time / __instance.period;
                            var outputCount = 0;
                            var amount = veinPool[veinId].amount;
                            if (miningRate > 0f)
                            {
                                if (amount > KeepVeinAmount)
                                {
                                    var usedCount = 0;
                                    var maxAllowed = amount - KeepVeinAmount;
                                    var add = miningRate * (double)times;
                                    __instance.costFrac += add;
                                    var estimateUses = (int)__instance.costFrac;
                                    if (estimateUses < maxAllowed)
                                    {
                                        outputCount = times;
                                        usedCount = estimateUses;
                                        __instance.costFrac -= estimateUses;
                                    }
                                    else
                                    {
                                        usedCount = maxAllowed;
                                        var oldFrac = __instance.costFrac - add;
                                        var ratio = (usedCount - oldFrac) / add;
                                        var realCost = times * ratio;
                                        outputCount = (int)(Math.Ceiling(realCost) + 0.01);
                                        __instance.costFrac = miningRate * (outputCount - realCost);
                                    }
                                    if (usedCount > 0)
                                    {
                                        var groupIndex = (int)veinPool[veinId].groupIndex;
                                        amount -= usedCount;
                                        veinPool[veinId].amount = amount;
                                        if (amount < __instance.minimumVeinAmount)
                                        {
                                            __instance.minimumVeinAmount = amount;
                                        }

                                        factory.veinGroups[groupIndex].amount -= usedCount;
                                        factory.veinAnimPool[veinId].time = amount >= 20000 ? 0f : 1f - 0.00005f;
                                        if (amount <= 0)
                                        {
                                            var venType = (int)veinPool[veinId].type;
                                            var pos = veinPool[veinId].pos;
                                            factory.RemoveVeinWithComponents(veinId);
                                            factory.RecalculateVeinGroup(groupIndex);
                                            factory.NotifyVeinExhausted(venType, groupIndex, pos);
                                            veinCount = __instance.veinCount;
                                        }
                                        else
                                        {
                                            currentVeinIndex++;
                                        }
                                    }
                                }
                                else
                                {
                                    if (amount <= 0)
                                    {
                                        __instance.RemoveVeinFromArray(currentVeinIndex);
                                        __instance.GetMinimumVeinAmount(factory, veinPool);
                                        veinCount = __instance.veinCount;
                                    }
                                    else
                                    {
                                        currentVeinIndex++;
                                    }
                                    __instance.currentVeinIndex = veinCount > 1 ? currentVeinIndex % veinCount : 0;
                                    __instance.time -= __instance.period * times;
                                    break;
                                }
                            }
                            else
                            {
                                outputCount = times;
                            }
                            __instance.productCount += outputCount;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += outputCount;
                                factory.AddMiningFlagUnsafe(veinPool[veinId].type);
                                factory.AddVeinMiningFlagUnsafe(veinPool[veinId].type);
                            }
                            __instance.time -= __instance.period * outputCount;
                            __instance.currentVeinIndex = veinCount > 1 ? currentVeinIndex % veinCount : 0;
                        }
                    }

                    break;
                case EMinerType.Oil:
                    if (__instance.veinCount <= 0)
                        break;

                    veinId = __instance.veins[0];
                    lock (veinPool)
                    {
                        var amount = veinPool[veinId].amount;
                        var workCount = amount * VeinData.oilSpeedMultiplier;
                        if (__instance.time < __instance.period)
                        {
                            __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * workCount + 0.5f);
                            res = 1U;
                        }

                        if (__instance.time >= __instance.period && __instance.productCount < 50)
                        {
                            __instance.productId = veinPool[veinId].productId;
                            times = __instance.time / __instance.period;
                            if (times <= 0) break;
                            var outputCount = 0;
                            if (miningRate > 0f)
                            {
                                if (amount > _keepOilAmount)
                                {
                                    var usedCount = 0;
                                    var maxAllowed = amount - _keepOilAmount;
                                    var add = miningRate * (double)times;
                                    __instance.costFrac += add;
                                    var estimateUses = (int)__instance.costFrac;
                                    if (estimateUses < maxAllowed)
                                    {
                                        outputCount = times;
                                        usedCount = estimateUses;
                                        __instance.costFrac -= estimateUses;
                                    }
                                    else
                                    {
                                        usedCount = maxAllowed;
                                        var oldFrac = __instance.costFrac - add;
                                        var ratio = (usedCount - oldFrac) / add;
                                        var realCost = times * ratio;
                                        outputCount = (int)(Math.Ceiling(realCost) + 0.01);
                                        __instance.costFrac = miningRate * (outputCount - realCost);
                                    }
                                    if (usedCount > 0)
                                    {
                                        if (usedCount > maxAllowed)
                                        {
                                            usedCount = maxAllowed;
                                        }

                                        amount -= usedCount;
                                        veinPool[veinId].amount = amount;
                                        var groupIndex = veinPool[veinId].groupIndex;
                                        factory.veinGroups[groupIndex].amount -= usedCount;
                                        factory.veinAnimPool[veinId].time = amount >= 25000 ? 0f : 1f - amount * VeinData.oilSpeedMultiplier;
                                        if (amount <= 2500)
                                        {
                                            factory.NotifyVeinExhausted((int)veinPool[veinId].type, groupIndex, veinPool[veinId].pos);
                                        }
                                    }
                                }
                                else if (_keepOilAmount <= 2500)
                                {
                                    outputCount = times;
                                }
                                else
                                {
                                    __instance.time -= __instance.period * times;
                                    break;
                                }
                            }
                            else
                            {
                                outputCount = times;
                            }

                            __instance.productCount += outputCount;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += outputCount;
                            }

                            __instance.time -= __instance.period * outputCount;
                        }
                    }

                    break;

                case EMinerType.Water:
                    if (__instance.time < __instance.period)
                    {
                        __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed);
                        res = 1U;
                    }

                    if (__instance.time < __instance.period) break;
                    times = __instance.time / __instance.period;
                    if (__instance.productCount >= 50) break;
                    __instance.productId = factory.planet.waterItemId;
                    do
                    {
                        if (__instance.productId > 0)
                        {
                            __instance.productCount += times;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += times;
                                break;
                            }
                        }

                        __instance.productId = 0;
                    } while (false);

                    __instance.time -= __instance.period * times;
                    break;
            }

            if (__instance is { productCount: > 0, insertTarget: > 0, productId: > 0 })
            {
                var multiplier = 36000000.0 / __instance.period * miningSpeed;
                if (__instance.type == EMinerType.Vein)
                {
                    multiplier *= __instance.veinCount;
                }
                else if (__instance.type == EMinerType.Oil)
                {
                    multiplier *= veinPool[__instance.veins[0]].amount * VeinData.oilSpeedMultiplier;
                }

                var count = (int)(multiplier - 0.01) / 1800 + 1;
                count = count < 4 ? count < 1 ? 1 : count : 4;
                var stack = __instance.productCount < count ? __instance.productCount : count;
                var outputCount = factory.InsertInto(__instance.insertTarget, 0, __instance.productId, (byte)stack, 0, out _);
                __instance.productCount -= outputCount;
                if (__instance is { productCount: 0, type: EMinerType.Vein })
                {
                    __instance.productId = 0;
                }
            }

            __result = res;
            return false;
        }
    }

    private class DoNotRenderEntities : PatchImpl<DoNotRenderEntities>
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ObjectRenderer), nameof(ObjectRenderer.Render))]
        [HarmonyPatch(typeof(DynamicRenderer), nameof(DynamicRenderer.Render))]
        private static bool ObjectRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabRenderer), nameof(LabRenderer.Render))]
        private static bool LabRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GPUInstancingManager), nameof(GPUInstancingManager.Render))]
        private static void FactoryModel_DrawInstancedBatches_Postfix(GPUInstancingManager __instance)
        {
            __instance.renderEntity = true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(RaycastLogic), nameof(RaycastLogic.GameTick))]
        private static IEnumerable<CodeInstruction> RaycastLogic_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Brtrue));
            var branch1 = (Label)matcher.Advance(1).Operand;
            var branch2 = generator.DefineLabel();
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldarg_0)
            ).Advance(8).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, 45),
                new CodeInstruction(OpCodes.Brtrue, branch2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, 33),
                new CodeInstruction(OpCodes.Ldloc_S, 35),
                Transpilers.EmitDelegate((RaycastLogic l, ColliderData[] colliderPool, int j) => l.factory.entityPool[colliderPool[j].objId].inserterId > 0),
                new CodeInstruction(OpCodes.Brfalse, branch1)
            );
            matcher.Labels.Add(branch2);
            return matcher.InstructionEnumeration();
        }
    }

    private class DragBuildPowerPoles : PatchImpl<DragBuildPowerPoles>
    {
        private static readonly List<bool> OldDragBuild = [];
        private static readonly List<Vector2> OldDragBuildDist = [];
        private static readonly int[] PowerPoleIds = [2202, 2212];

        protected override void OnEnable()
        {
            GameLogicProc.OnGameBegin += OnGameBegin;
            GameLogicProc.OnGameEnd += OnGameEnd;
            FixProto();
        }

        protected override void OnDisable()
        {
            UnfixProto();
            GameLogicProc.OnGameEnd -= OnGameEnd;
            GameLogicProc.OnGameBegin -= OnGameBegin;
        }

        public static void AlternatelyChanged()
        {
            UnfixProto();
            FixProto();
        }

        private static bool IsPowerPole(int id)
        {
            return PowerPoleIds.Contains(id);
        }

        private static void FixProto()
        {
            if (DSPGame.IsMenuDemo) return;
            OldDragBuild.Clear();
            OldDragBuildDist.Clear();
            foreach (var id in PowerPoleIds)
            {
                var prefabDesc = LDB.items.Select(id)?.prefabDesc;
                if (prefabDesc == null) return;
                OldDragBuild.Add(prefabDesc.dragBuild);
                OldDragBuildDist.Add(prefabDesc.dragBuildDist);
                prefabDesc.dragBuild = true;
                var distance = prefabDesc.powerConnectDistance - 0.72f;
                prefabDesc.dragBuildDist = new Vector2(distance, distance);
            }
        }

        private static void UnfixProto()
        {
            if (GetHarmony() == null || OldDragBuild.Count < 3 || DSPGame.IsMenuDemo) return;
            var i = 0;
            foreach (var id in PowerPoleIds)
            {
                var powerPole = LDB.items.Select(id);
                if (powerPole?.prefabDesc != null)
                {
                    powerPole.prefabDesc.dragBuild = OldDragBuild[i];
                    powerPole.prefabDesc.dragBuildDist = OldDragBuildDist[i];
                }

                i++;
            }

            OldDragBuild.Clear();
            OldDragBuildDist.Clear();
        }

        private static void OnGameBegin()
        {
            FixProto();
        }

        private static void OnGameEnd()
        {
            UnfixProto();
        }

        private static int PlanetGridSnapDotsNonAllocNotAligned(PlanetGrid planetGrid, Vector3 begin, Vector3 end, Vector2 interval, float yaw, float planetRadius, float gap, Vector3[] snaps)
        {
            begin = begin.normalized;
            end = end.normalized;
            var finalCount = 1;
            var ignoreGrid = VFInput._switchGridSnap;
            if (ignoreGrid)
                snaps[0] = begin;
            else
                snaps[0] = planetGrid.SnapTo(begin);
            var dot = Vector3.Dot(begin, end);
            if (dot is > 0.999999f or < -0.999999f)
                return 1;
            var distTotal = Mathf.Acos(dot) * planetRadius;

            var intervalAll = interval.x;
            var maxT = 1f - intervalAll * 0.5f / distTotal;
            if (maxT < 0f)
                return 1;
            var maxCount = snaps.Length;
            while (finalCount < maxCount)
            {
                var t = finalCount * intervalAll / distTotal;
                if (ignoreGrid)
                    snaps[finalCount] = Vector3.Slerp(begin, end, t);
                else
                    snaps[finalCount] = planetGrid.SnapTo(Vector3.Slerp(begin, end, t));
                finalCount++;
                if (t > maxT) break;
            }

            return finalCount;
        }

        private static int PlanetAuxDataSnapDotsNonAllocNotAligned(PlanetAuxData aux, Vector3 begin, Vector3 end, Vector2 interval, float height, float yaw, float gap, Vector3[] snaps)
        {
            var num = 0;
            var magnitude = begin.magnitude;
            if (aux.activeGrid != null)
            {
                num = PlanetGridSnapDotsNonAllocNotAligned(aux.activeGrid, begin, end, interval, yaw, aux.planet.realRadius + height, gap, snaps);
                for (var i = 0; i < num; i++)
                {
                    snaps[i] *= magnitude;
                }
            }
            else
            {
                snaps[num++] = aux.Snap(begin, false);
            }

            return num;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.SnapDotsNonAlloc)))
            );
            matcher.Labels.Add(label1);
            matcher.InstructionAt(1).labels.Add(label2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.control))),
                new CodeInstruction(OpCodes.Brtrue, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handItem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DragBuildPowerPoles), nameof(IsPowerPole))),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DragBuildPowerPoles), nameof(PlanetAuxDataSnapDotsNonAllocNotAligned))),
                new CodeInstruction(OpCodes.Br, label2)
            ).Advance(1).MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handItem))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.item))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handPrefabDesc))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc)))
            );
            var pos = matcher.Pos;
            matcher.MatchBack(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Add)
            );
            var operand = matcher.Operand;
            matcher.Start().Advance(pos);
            matcher.Advance(2).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, operand)).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildTool_Click click, int i) =>
            {
                if (!DragBuildPowerPolesAlternatelyEnabled.Value || (i & 1) == 0) return click.handItem;
                var id = click.handItem.ID;
                if (id != 2202) return click.handItem;
                return LDB.items.Select(id ^ 3);
            })).Advance(3).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, operand)).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildTool_Click click, int i) =>
            {
                if (!DragBuildPowerPolesAlternatelyEnabled.Value || (i & 1) == 0) return click.handPrefabDesc;
                var id = click.handItem.ID;
                if (id != 2202) return click.handPrefabDesc;
                return LDB.items.Select(id ^ 3).prefabDesc;
            }));
            return matcher.InstructionEnumeration();
        }
    }

    private class BeltSignalsForBuyOut : PatchImpl<BeltSignalsForBuyOut>
    {
        private static bool _initialized;
        private static bool _loaded;
        private static long _clusterSeedKey;
        private static readonly int[] DarkFogItemIds = [5201, 5206, 5202, 5204, 5203, 5205];
        private static readonly int[] DarkFogItemExchangeRate = [20, 60, 30, 30, 30, 10];
        public static readonly int[] DarkFogItemsInVoid = [0, 0, 0, 0, 0, 0];
        private static Dictionary<int, uint>[] _signalBelts = new Dictionary<int, uint>[64];
        private static readonly HashSet<int> SignalBeltFactoryIndices = [];

        public static void InitPersist()
        {
            Persist.Enable(true);
        }

        public static void UninitPersist()
        {
            Persist.Enable(false);
        }

        private static void AddBeltSignalProtos()
        {
            if (!_initialized || _loaded) return;
            var assembly = Assembly.GetExecutingAssembly();
            var signals = LDB._signals;
            SignalProto[] protos =
            [
                new SignalProto
                {
                    ID = 301,
                    Name = "存储单元",
                    GridIndex = 3601,
                    IconPath = "assets/signal/memory.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/memory.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 302,
                    Name = "能量碎片",
                    GridIndex = 3602,
                    IconPath = "assets/signal/energy-fragment.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/energy-fragment.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 303,
                    Name = "硅基神经元",
                    GridIndex = 3603,
                    IconPath = "assets/signal/silicon-neuron.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/silicon-neuron.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 304,
                    Name = "负熵奇点",
                    GridIndex = 3604,
                    IconPath = "assets/signal/negentropy.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/negentropy.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 305,
                    Name = "物质重组器",
                    GridIndex = 3605,
                    IconPath = "assets/signal/reassembler.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/reassembler.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 306,
                    Name = "虚粒子",
                    GridIndex = 3606,
                    IconPath = "assets/signal/virtual-particle.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/virtual-particle.png", assembly),
                    SID = ""
                },
            ];
            foreach (var proto in protos)
            {
                proto.name = proto.Name.Translate();
            }

            var index = signals.dataArray.Length;
            signals.dataArray = signals.dataArray.Concat(protos).ToArray();
            foreach (var proto in protos)
            {
                signals.dataIndices[proto.ID] = index;
                index++;
            }

            _loaded = true;
        }

        private static void RemoveBeltSignalProtos()
        {
            if (!_initialized || !_loaded) return;
            var signals = LDB._signals;
            if (signals.dataIndices.TryGetValue(301, out var index))
            {
                signals.dataArray = signals.dataArray.Take(index).Concat(signals.dataArray.Skip(index + 6)).ToArray();
                for (var id = 301; id <= 306; id++)
                    signals.dataIndices.Remove(id);
                var len = signals.dataArray.Length;
                for (; index < len; index++)
                    signals.dataIndices[signals.dataArray[index].ID] = index;
            }

            _loaded = false;
        }

        private static void InitSignalBelts()
        {
            if (!GameMain.isRunning) return;

            var factories = GameMain.data?.factories;
            if (factories == null) return;
            foreach (var factory in factories)
            {
                var entitySignPool = factory?.entitySignPool;
                if (entitySignPool == null) continue;
                var cargoTraffic = factory.cargoTraffic;
                var beltPool = cargoTraffic.beltPool;
                for (var i = cargoTraffic.beltCursor - 1; i > 0; i--)
                {
                    if (beltPool[i].id != i) continue;
                    ref var signal = ref entitySignPool[beltPool[i].entityId];
                    var signalId = signal.iconId0;
                    if (signalId is < 301U or > 306U) continue;
                    SetSignalBelt(factory.index, i, signalId - 301U);
                }
            }
        }

        private static void SetSignalBelt(int factory, int beltId, uint signal)
        {
            var signalBelts = GetOrCreateSignalBelts(factory);
            if (signalBelts.Count == 0)
                SignalBeltFactoryIndices.Add(factory);
            signalBelts[beltId] = signal;
        }

        private static Dictionary<int, uint> GetOrCreateSignalBelts(int index)
        {
            Dictionary<int, uint> obj;
            if (index < 0) return null;
            if (index >= _signalBelts.Length)
            {
                Array.Resize(ref _signalBelts, index * 2);
            }
            else
            {
                obj = _signalBelts[index];
                if (obj != null) return obj;
            }

            obj = [];
            _signalBelts[index] = obj;
            return obj;
        }

        private static Dictionary<int, uint> GetSignalBelts(int index)
        {
            return index >= 0 && index < _signalBelts.Length ? _signalBelts[index] : null;
        }

        private static void RemoveSignalBelt(int factory, int beltId)
        {
            var signalBelts = GetSignalBelts(factory);
            if (signalBelts == null) return;
            signalBelts.Remove(beltId);
            if (signalBelts.Count == 0)
                SignalBeltFactoryIndices.Remove(factory);
        }

        private static void RemovePlanetSignalBelts(int factory)
        {
            var signalBelts = GetSignalBelts(factory);
            if (signalBelts == null) return;
            signalBelts.Clear();
            SignalBeltFactoryIndices.Remove(factory);
        }

        private class Persist : PatchImpl<Persist>
        {
            protected override void OnEnable()
            {
                AddBeltSignalProtos();
                GameLogicProc.OnDataLoaded += VFPreload_InvokeOnLoadWorkEnded_Postfix;
                GameLogicProc.OnGameBegin += OnGameBegin;
            }

            protected override void OnDisable()
            {
                GameLogicProc.OnGameBegin -= OnGameBegin;
                GameLogicProc.OnDataLoaded -= VFPreload_InvokeOnLoadWorkEnded_Postfix;
            }

            private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
            {
                if (_initialized) return;
                _initialized = true;
                AddBeltSignalProtos();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DigitalSystem), MethodType.Constructor, typeof(PlanetData))]
            private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
            {
                var player = GameMain.mainPlayer;
                if (player == null) return;
                var factory = _planet?.factory;
                if (factory == null) return;
                RemovePlanetSignalBelts(factory.index);
            }

            private static void OnGameBegin()
            {
                _clusterSeedKey = GameMain.data.GetClusterSeedKey();
                InitSignalBelts();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
            public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
            {
                var planet = GameMain.localPlanet;
                if (planet == null) return;
                RemoveSignalBelt(planet.factoryIndex, id);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalIcon))]
            public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int entityId, int signalId)
            {
                var planet = GameMain.localPlanet;
                if (planet == null) return;
                var factory = __instance.factory;
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                if (signalId is < 301 or > 306)
                {
                    RemoveSignalBelt(factoryIndex, beltId);
                }
                else
                {
                    SetSignalBelt(factoryIndex, beltId, (uint)signalId - 301U);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.OnFactoryFrameBegin))]
        public static void GameLogic_OnFactoryFrameBegin_Postfix()
        {
            var factories = GameMain.data?.factories;
            if (factories == null) return;
            var factoriesCount = factories.Length;
            var propertySystem = DSPGame.propertySystem;
            List<int> factoriesToRemove = null;
            foreach (var factoryIndex in SignalBeltFactoryIndices)
            {
                if (factoryIndex >= factoriesCount)
                {
                    if (factoriesToRemove == null)
                        factoriesToRemove = [factoryIndex];
                    else
                        factoriesToRemove.Add(factoryIndex);
                    continue;
                }
                var signalBelts = GetSignalBelts(factoryIndex);
                if (signalBelts == null) continue;
                var factory = factories[factoryIndex];
                if (factory == null) continue;
                var cargoTraffic = factory.cargoTraffic;
                var beltCount = cargoTraffic.beltCursor;
                List<int> beltsToRemove = null;
                foreach (var kvp in signalBelts)
                {
                    if (kvp.Key >= beltCount)
                    {
                        if (beltsToRemove == null)
                            beltsToRemove = [kvp.Key];
                        else
                            beltsToRemove.Add(kvp.Key);
                        continue;
                    }
                    ref var belt = ref cargoTraffic.beltPool[kvp.Key];
                    var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                    var itemIdx = kvp.Value;
                    if (cargoPath == null) continue;
                    var itemId = DarkFogItemIds[itemIdx];
                    var consume = (byte)Math.Min(DarkFogItemsInVoid[itemIdx], 4);
                    if (consume < 4)
                    {
                        var metaverse = propertySystem.GetItemAvaliableProperty(_clusterSeedKey, 6006);
                        if (metaverse > 0)
                        {
                            if (metaverse > 10)
                                metaverse = 10;
                            propertySystem.AddItemConsumption(_clusterSeedKey, 6006, metaverse);
                            var mainPlayer = GameMain.mainPlayer;
                            GameMain.history.AddPropertyItemConsumption(6006, metaverse, true);
                            var count = DarkFogItemExchangeRate[itemIdx] * metaverse;
                            DarkFogItemsInVoid[itemIdx] += count;
                            consume = (byte)Math.Min(DarkFogItemsInVoid[itemIdx], 4);
                            mainPlayer.mecha.AddProductionStat(itemId, count, mainPlayer.nearestFactory);
                        }
                    }

                    if (consume > 0 && cargoPath.TryInsertItem(belt.segIndex + belt.segPivotOffset, itemId, consume, 0))
                        DarkFogItemsInVoid[itemIdx] -= consume;
                }
                if (beltsToRemove == null) continue;
                foreach (var beltId in beltsToRemove)
                    signalBelts.Remove(beltId);
                if (signalBelts.Count > 0) continue;
                if (factoriesToRemove == null)
                    factoriesToRemove = [factoryIndex];
                else
                    factoriesToRemove.Add(factoryIndex);
            }
            if (factoriesToRemove == null) return;
            foreach (var factoryIndex in factoriesToRemove)
            {
                RemovePlanetSignalBelts(factoryIndex);
            }
        }
    }

    private class TankFastFillInAndTakeOut : PatchImpl<TankFastFillInAndTakeOut>
    {
        private static readonly CodeInstruction[] MultiplierWithCountCheck = [
            new(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(_tankFastFillInAndTakeOutMultiplierRealValue))),
            new(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)]))
        ];
        private static readonly CodeInstruction GetRealCount = new(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(_tankFastFillInAndTakeOutMultiplierRealValue)));

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
        private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastFillIn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsStloc())
            ).Advance(1).RemoveInstruction().InsertAndAdvance(GetRealCount).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsStloc())
            ).RemoveInstructions(5).Insert(MultiplierWithCountCheck);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastTakeOut))]
        private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastTakeOut_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloca || ci.opcode == OpCodes.Ldloca_S)
            ).Advance(1).RemoveInstruction().InsertAndAdvance(GetRealCount).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Bgt || ci.opcode == OpCodes.Bgt_S),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsLdloc())
            ).RemoveInstructions(5).Insert(MultiplierWithCountCheck);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITankWindow), nameof(UITankWindow._OnUpdate))]
        private static IEnumerable<CodeInstruction> UITankWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Bgt || ci.opcode == OpCodes.Bgt_S),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsStloc())
            );
            matcher.Repeat(m => m.RemoveInstructions(5).InsertAndAdvance(MultiplierWithCountCheck));
            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TankComponent), nameof(TankComponent.TickOutput))]
        private static bool TankComponent_TickOutput_Prefix(ref TankComponent __instance, PlanetFactory factory)
        {
            if (!__instance.outputSwitch || __instance.fluidCount <= 0)
                return false;
            var lastTankId = __instance.lastTankId;
            if (lastTankId <= 0)
                return false;
            var factoryStorage = factory.factoryStorage;
            ref var tankComponent = ref factoryStorage.tankPool[lastTankId];
            if (!tankComponent.inputSwitch || (tankComponent.fluidId > 0 && tankComponent.fluidId != __instance.fluidId))
                return false;
            var left = tankComponent.fluidCapacity - tankComponent.fluidCount;
            if (left <= 0)
                return false;
            if (tankComponent.fluidId == 0)
                tankComponent.fluidId = __instance.fluidId;
            var takeOut = Math.Min(left, _tankFastFillInAndTakeOutMultiplierRealValue);
            if (takeOut >= __instance.fluidCount)
            {
                tankComponent.fluidCount += __instance.fluidCount;
                tankComponent.fluidInc += __instance.fluidInc;
                __instance.fluidId = 0;
                __instance.fluidCount = 0;
                __instance.fluidInc = 0;
            }
            else
            {
                var takeInc = __instance.split_inc(ref __instance.fluidCount, ref __instance.fluidInc, takeOut);
                tankComponent.fluidCount += takeOut;
                tankComponent.fluidInc += takeInc;
            }
            return false;
        }
    }

    private class TweakBuildingBuffer : PatchImpl<TweakBuildingBuffer>
    {
        public static void RefreshAssemblerBufferMultipliers()
        {
            if (!TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(AssemblerComponent_UpdateNeeds_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(AssemblerComponent_UpdateNeeds_Transpiler)));
        }

        public static void RefreshLabBufferMaxCountForAssemble()
        {
            if (!TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsAssemble_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsAssemble_Transpiler)));
        }

        public static void RefreshLabBufferMaxCountForResearch()
        {
            if (!TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsResearch_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsResearch_Transpiler)));
        }

        public static void RefreshReceiverBufferCount()
        {
            if (!TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(PowerGeneratorComponent_GameTick_Gamma_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(PowerGeneratorComponent_GameTick_Gamma_Transpiler)));
        }

        public static void RefreshEjectorBufferCount()
        {
            if (!TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(EjectorComponent_InternalUpdate_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(EjectorComponent_InternalUpdate_Transpiler)));
        }

        public static void RefreshSiloBufferCount()
        {
            if (!TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(SiloComponent_InternalUpdate_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(SiloComponent_InternalUpdate_Transpiler)));
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma))]
        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_GameTick_Gamma_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  bool flag3 = keyFrame && useIon && (float)this.catalystPoint < 72000f;
             * To:
             *  bool flag3 = keyFrame && useIon && this.catalystPoint < 3600 * ReceiverBufferCount.Value;
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.catalystPoint))),
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Ldc_R4, 72000f),
                new CodeMatch(OpCodes.Clt)
            );
            matcher.Advance(2).RemoveInstructions(2).Insert(new CodeInstruction(OpCodes.Ldc_I4, ReceiverBufferCount.Value * 3600));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds))]
        private static IEnumerable<CodeInstruction> AssemblerComponent_UpdateNeeds_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  int num2 = this.speedOverride * 180 / this.timeSpend + 1;
             *  if (num2 < 2)
             *  {
             *      num2 = 2;
             *  }
             * To:
             *  int num2 = this.speedOverride * 60 * (AssemblerBufferTimeMultiplier.Value - 1) * 60 / this.timeSpend + 1;
             *  if (num2 < AssemblerBufferMininumMultiplier.Value)
             *  {
             *      num2 = AssemblerBufferMininumMultiplier.Value;
             *  }
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(AssemblerComponent), nameof(AssemblerComponent.speedOverride))),
                new CodeMatch(OpCodes.Ldc_I4, 180),
                new CodeMatch(OpCodes.Mul)
            );
            matcher.Advance(2).Operand = (AssemblerBufferTimeMultiplier.Value - 1) * 60;
            matcher.Advance(2).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.opcode == OpCodes.Bge_S || ci.opcode == OpCodes.Bge),
                new CodeMatch(OpCodes.Ldc_I4_2)
            );
            matcher.Operand = AssemblerBufferMininumMultiplier.Value;
            matcher.Advance(2).Operand = AssemblerBufferMininumMultiplier.Value;
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble))]
        private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsAssemble_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  int num2 = ((this.timeSpend > 5400000) ? 6 :
             *             (3 * ((this.speedOverride + 5001) / 10000) + 3));
             * To:
             *  int num2 = ((this.timeSpend > 5400000) ? LabBufferMaxCountForAssemble.Value :
             *             (LabBufferExtraCountForAdvancedAssemble.Value * ((this.speedOverride + 5001) / 10000) + (LabBufferMaxCountForAssemble.Value - LabBufferExtraCountForAdvancedAssemble.Value)));
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.recipeExecuteData))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(RecipeExecuteData), nameof(RecipeExecuteData.timeSpend))),
                new CodeMatch(OpCodes.Ldc_I4, 5400000),
                new CodeMatch(ci => ci.opcode == OpCodes.Bgt_S || ci.opcode == OpCodes.Bgt),
                new CodeMatch(OpCodes.Ldc_I4_3)
            );
            var extraCount = LabBufferExtraCountForAdvancedAssemble.Value;
            matcher.Advance(5).SetAndAdvance(OpCodes.Ldc_I4, extraCount);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Ldc_I4_3),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S),
                new CodeMatch(OpCodes.Ldc_I4_6),
                new CodeMatch(ci => ci.IsStloc())
            );
            var maxCount = LabBufferMaxCountForAssemble.Value;
            matcher.Advance(2).SetAndAdvance(OpCodes.Ldc_I4, maxCount > extraCount ? maxCount - extraCount : 2);
            matcher.Advance(2).SetAndAdvance(OpCodes.Ldc_I4, maxCount);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch))]
        private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsResearch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  this.needs[0] = ((this.matrixServed[0] < 36000) ? 6001 : 0);
             *  this.needs[1] = ((this.matrixServed[1] < 36000) ? 6002 : 0);
             *  this.needs[2] = ((this.matrixServed[2] < 36000) ? 6003 : 0);
             *  this.needs[3] = ((this.matrixServed[3] < 36000) ? 6004 : 0);
             *  this.needs[4] = ((this.matrixServed[4] < 36000) ? 6005 : 0);
             *  this.needs[5] = ((this.matrixServed[5] < 36000) ? 6006 : 0);
             * To:
             *  this.needs[0] = ((this.matrixServed[0] < LabBufferMaxCountForResearch.Value * 3600) ? 6001 : 0);
             *  this.needs[1] = ((this.matrixServed[1] < LabBufferMaxCountForResearch.Value * 3600) ? 6002 : 0);
             *  this.needs[2] = ((this.matrixServed[2] < LabBufferMaxCountForResearch.Value * 3600) ? 6003 : 0);
             *  this.needs[3] = ((this.matrixServed[3] < LabBufferMaxCountForResearch.Value * 3600) ? 6004 : 0);
             *  this.needs[4] = ((this.matrixServed[4] < LabBufferMaxCountForResearch.Value * 3600) ? 6005 : 0);
             *  this.needs[5] = ((this.matrixServed[5] < LabBufferMaxCountForResearch.Value * 3600) ? 6006 : 0);
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4, 36000)
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, LabBufferMaxCountForResearch.Value * 3600));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.bulletCount))),
                new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4 || ci.opcode == OpCodes.Ldc_I4_S) && ci.OperandIs(20))
            );
            matcher.Advance(2).Set(OpCodes.Ldc_I4, EjectorBufferCount.Value);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> SiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SiloComponent), nameof(SiloComponent.bulletCount))),
                new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4 || ci.opcode == OpCodes.Ldc_I4_S) && ci.OperandIs(20))
            );
            matcher.Advance(2).Operand = SiloBufferCount.Value;
            return matcher.InstructionEnumeration();
        }
    }

    private class PressShiftToTakeWholeBeltItems : PatchImpl<PressShiftToTakeWholeBeltItems>
    {
        private static long nextTimei = 0;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameBegin += OnGameBegin;
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameBegin -= OnGameBegin;
        }

        private static void OnGameBegin()
        {
            nextTimei = 0;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._fastTransferWithEntityDown), MethodType.Getter)]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._fastTransferWithEntityPress), MethodType.Getter)]
        private static IEnumerable<CodeInstruction> VFInput_fastTransferWithEntityDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))),
                new CodeMatch(ci => ci.opcode == OpCodes.Brtrue || ci.opcode == OpCodes.Brtrue_S)
            );
            var lables = matcher.Labels;
            matcher.RemoveInstructions(2);
            matcher.Labels.AddRange(lables);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.fastFillIn)))
            );
            matcher.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))).Insert(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq)
            );

            var label0 = generator.DefineLabel();
            var label1 = generator.DefineLabel();
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastTakeOut)))
            ).Advance(8).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))),
                new CodeInstruction(OpCodes.Brfalse_S, label0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PressShiftToTakeWholeBeltItems), nameof(EntityFastTakeOutAlt))),
                new CodeInstruction(OpCodes.Br, label1)
            ).Labels.Add(label0);
            matcher.Advance(1).Labels.Add(label1);
            return matcher.InstructionEnumeration();
        }

        private static void EntityFastTakeOutAlt(PlanetFactory factory, int entityId, bool toPackage, out ItemBundle itemBundle, out bool full)
        {
            if (factory._tmp_items == null)
            {
                factory._tmp_items = new ItemBundle();
            }
            else
            {
                factory._tmp_items.Clear();
            }
            itemBundle = factory._tmp_items;
            full = false;
            if (entityId == 0 || factory.entityPool[entityId].id != entityId)
            {
                return;
            }
            if (GameMain.instance.timei < nextTimei) return;
            nextTimei = GameMain.instance.timei + 12;

            ref var entityData = ref factory.entityPool[entityId];
            if (entityData.beltId <= 0) return;
            var cargoTraffic = factory.cargoTraffic;
            ref var belt = ref cargoTraffic.beltPool[entityData.beltId];
            if (belt.id != entityData.beltId) return;

            HashSet<int> pathIds = [belt.segPathId];
            HashSet<int> inserterIds = [];
            var includeBranches = PressShiftToTakeWholeBeltItemsIncludeBranches.Value;
            var includeInserters = PressShiftToTakeWholeBeltItemsIncludeInserters.Value;
            List<int> pendingPathIds = [belt.segPathId];
            Dictionary<int, long> takeOutItems = [];
            var factorySystem = factory.factorySystem;
            while (pendingPathIds.Count > 0)
            {
                var lastIndex = pendingPathIds.Count - 1;
                var thisPathId = pendingPathIds[lastIndex];
                pendingPathIds.RemoveAt(lastIndex);
                var path = cargoTraffic.GetCargoPath(thisPathId);
                if (path == null) continue;
                if (includeInserters)
                {
                    foreach (var beltId in path.belts)
                    {
                        ref var b = ref cargoTraffic.beltPool[beltId];
                        if (b.id != beltId) return;
                        // From WriteObjectConn: Only slot 4 to 11 is used for belt <-> inserter connections (method argument slot/otherSlot is -1 there)
                        for (int cidx = 4; cidx < 12; cidx++)
                        {
                            factory.ReadObjectConn(b.entityId, cidx, out var isOutput, out var otherObjId, out var otherSlot);
                            if (otherObjId <= 0) continue;
                            var inserterId = factory.entityPool[otherObjId].inserterId;
                            if (inserterId <= 0) continue;
                            ref var inserter = ref factorySystem.inserterPool[inserterId];
                            if (inserter.id != inserterId) continue;
                            inserterIds.Add(inserterId);
                            if (includeBranches)
                            {
                                var pickTargetId = inserter.pickTarget;
                                if (pickTargetId > 0)
                                {
                                    ref var pickTarget = ref factory.entityPool[pickTargetId];
                                    if (pickTarget.id == pickTargetId && pickTarget.beltId > 0)
                                    {
                                        ref var pickTargetBelt = ref cargoTraffic.beltPool[pickTarget.beltId];
                                        if (pickTargetBelt.id == pickTarget.beltId && !pathIds.Contains(pickTargetBelt.segPathId))
                                        {
                                            pathIds.Add(pickTargetBelt.segPathId);
                                            pendingPathIds.Add(pickTargetBelt.segPathId);
                                        }
                                    }
                                }
                                var insertTargetId = inserter.insertTarget;
                                if (insertTargetId > 0)
                                {
                                    ref var insertTarget = ref factory.entityPool[insertTargetId];
                                    if (insertTarget.id == insertTargetId && insertTarget.beltId > 0)
                                    {
                                        ref var insertTargetBelt = ref cargoTraffic.beltPool[insertTarget.beltId];
                                        if (insertTargetBelt.id == insertTarget.beltId && !pathIds.Contains(insertTargetBelt.segPathId))
                                        {
                                            pathIds.Add(insertTargetBelt.segPathId);
                                            pendingPathIds.Add(insertTargetBelt.segPathId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (!includeBranches) continue;
                foreach (var inputPathId in path.inputPaths)
                {
                    if (pathIds.Contains(inputPathId)) continue;
                    pathIds.Add(inputPathId);
                    pendingPathIds.Add(inputPathId);
                }
                if (path.outputPath == null) continue;
                var outputPathId = path.outputPath.id;
                if (pathIds.Contains(outputPathId)) continue;
                pathIds.Add(outputPathId);
                pendingPathIds.Add(outputPathId);
            }

            var mainPlayer = factory.gameData.mainPlayer;
            foreach (var pathId in pathIds)
            {
                var cargoPath = cargoTraffic.GetCargoPath(pathId);
                if (cargoPath == null) continue;
                var end = cargoPath.bufferLength - 5;
                var buffer = cargoPath.buffer;
                for (var i = 0; i <= end;)
                {
                    if (buffer[i] >= 246)
                    {
                        i += 250 - buffer[i];
                        var index = buffer[i + 1] - 1 + (buffer[i + 2] - 1) * 100 + (buffer[i + 3] - 1) * 10000 + (buffer[i + 4] - 1) * 1000000;
                        ref var cargo = ref cargoPath.cargoContainer.cargoPool[index];
                        var item = cargo.item;
                        var stack = cargo.stack;
                        var inc = cargo.inc;
                        takeOutItems[item] = (takeOutItems.TryGetValue(item, out var value) ? value : 0)
                            + ((long)stack | ((long)inc << 32));
                        Array.Clear(buffer, i - 4, 10);
                        i += 6;
                        if (cargoPath.updateLen < i) cargoPath.updateLen = i;
                        i += 4;
                        cargoPath.cargoContainer.RemoveCargo(index);
                    }
                    else
                    {
                        i += 5;
                        if (i > end && i < end + 5)
                        {
                            i = end;
                        }
                    }
                }
            }
            foreach (var inserterId in inserterIds)
            {
                ref var inserter = ref factorySystem.inserterPool[inserterId];
                if (inserter.itemId > 0 && inserter.stackCount > 0)
                {
                    takeOutItems[inserter.itemId] = (takeOutItems.TryGetValue(inserter.itemId, out var value) ? value : 0)
                            + ((long)inserter.itemCount | ((long)inserter.itemInc << 32));
                    inserter.itemId = 0;
                    inserter.stackCount = 0;
                    inserter.itemCount = 0;
                    inserter.itemInc = 0;
                }
            }
            foreach (var kvp in takeOutItems)
            {
                var added = mainPlayer.TryAddItemToPackage(kvp.Key, (int)(kvp.Value & 0xFFFFFFFF), (int)(kvp.Value >> 32), true, entityId);
                if (added > 0)
                {
                    UIItemup.Up(kvp.Key, added);
                }
            }
        }
    }
}
