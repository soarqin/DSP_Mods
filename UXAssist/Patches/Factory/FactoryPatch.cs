using System;
using System.IO;
using BepInEx.Configuration;
using CommonAPI.Systems;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

[ModFeature("Factory", Order = 10)]
public static class FactoryPatch
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
    public static ConfigEntry<bool> AutoConstructButtonEnabled;
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

    internal static PressKeyBind _doNotRenderEntitiesKey;
    internal static PressKeyBind _offgridfForPathsKey;
    internal static PressKeyBind _cutConveyorBeltKey;
    internal static PressKeyBind _dismantleBlueprintSelectionKey;
    internal static PressKeyBind _selectAllBuildingsInBlueprintCopyKey;

    internal static int _tankFastFillInAndTakeOutMultiplierRealValue = 2;

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

        BeltSignalPatch.InitPersist();
        VeinProtectionPatch.InitConfig();
        UnlimitInteractiveEnabled.SettingChanged += (_, _) => ArchitectModePatch.UnlimitInteractive.Enable(UnlimitInteractiveEnabled.Value);
        RemoveSomeConditionEnabled.SettingChanged += (_, _) => ArchitectModePatch.RemoveSomeConditionBuild.Enable(RemoveSomeConditionEnabled.Value);
        NightLightEnabled.SettingChanged += (_, _) => RenderingPatch.NightLight.Enable(NightLightEnabled.Value);
        NightLightAngleX.SettingChanged += (_, _) => RenderingPatch.NightLight.UpdateSunlightAngle();
        NightLightAngleY.SettingChanged += (_, _) => RenderingPatch.NightLight.UpdateSunlightAngle();
        RemoveBuildRangeLimitEnabled.SettingChanged += (_, _) => ArchitectModePatch.RemoveBuildRangeLimit.Enable(RemoveBuildRangeLimitEnabled.Value);
        LargerAreaForUpgradeAndDismantleEnabled.SettingChanged += (_, _) => ArchitectModePatch.LargerAreaForUpgradeAndDismantle.Enable(LargerAreaForUpgradeAndDismantleEnabled.Value);
        LargerAreaForTerraformEnabled.SettingChanged += (_, _) => ArchitectModePatch.LargerAreaForTerraform.Enable(LargerAreaForTerraformEnabled.Value);
        OffGridBuildingEnabled.SettingChanged += (_, _) => BuildToolPatch.OffGridBuilding.Enable(OffGridBuildingEnabled.Value);
        TreatStackingAsSingleEnabled.SettingChanged += (_, _) => BuildToolPatch.TreatStackingAsSingle.Enable(TreatStackingAsSingleEnabled.Value);
        QuickBuildAndDismantleLabsEnabled.SettingChanged += (_, _) => ImmediateBuildPatch.QuickBuildAndDismantleLab.Enable(QuickBuildAndDismantleLabsEnabled.Value);
        ProtectVeinsFromExhaustionEnabled.SettingChanged += (_, _) => VeinProtectionPatch.ProtectVeinsFromExhaustion.Enable(ProtectVeinsFromExhaustionEnabled.Value);
        DoNotRenderEntitiesEnabled.SettingChanged += (_, _) => RenderingPatch.DoNotRenderEntities.Enable(DoNotRenderEntitiesEnabled.Value);
        DragBuildPowerPolesEnabled.SettingChanged += (_, _) => BuildToolPatch.DragBuildPowerPoles.Enable(DragBuildPowerPolesEnabled.Value);
        DragBuildPowerPolesAlternatelyEnabled.SettingChanged += (_, _) => BuildToolPatch.DragBuildPowerPoles.AlternatelyChanged();
        AutoConstructButtonEnabled.SettingChanged += (_, _) => Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        AutoConstructEnabled.SettingChanged += (_, _) => Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        BeltSignalsForBuyOutEnabled.SettingChanged += (_, _) => BeltSignalPatch.BeltSignalsForBuyOut.Enable(BeltSignalsForBuyOutEnabled.Value);
        TankFastFillInAndTakeOutEnabled.SettingChanged += (_, _) => BuildToolPatch.TankFastFillInAndTakeOut.Enable(TankFastFillInAndTakeOutEnabled.Value);
        TankFastFillInAndTakeOutMultiplier.SettingChanged += (_, _) => UpdateTankFastFillInAndTakeOutMultiplierRealValue();
        TweakBuildingBufferEnabled.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.Enable(TweakBuildingBufferEnabled.Value);
        AssemblerBufferTimeMultiplier.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshAssemblerBufferMultipliers();
        AssemblerBufferMininumMultiplier.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshAssemblerBufferMultipliers();
        LabBufferMaxCountForAssemble.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshLabBufferMaxCountForAssemble();
        LabBufferExtraCountForAdvancedAssemble.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshLabBufferMaxCountForAssemble();
        LabBufferMaxCountForResearch.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshLabBufferMaxCountForResearch();
        ReceiverBufferCount.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshReceiverBufferCount();
        EjectorBufferCount.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshEjectorBufferCount();
        SiloBufferCount.SettingChanged += (_, _) => BuildingBufferPatch.TweakBuildingBuffer.RefreshSiloBufferCount();
        PressShiftToTakeWholeBeltItemsEnabled.SettingChanged += (_, _) => BuildToolPatch.PressShiftToTakeWholeBeltItems.Enable(PressShiftToTakeWholeBeltItemsEnabled.Value);

        UXAssist.RegisterExporter(Export);
        UXAssist.RegisterImporter((r, version) => Import(r, version));
    }

    public static void Start()
    {
        ArchitectModePatch.UnlimitInteractive.Enable(UnlimitInteractiveEnabled.Value);
        ArchitectModePatch.RemoveSomeConditionBuild.Enable(RemoveSomeConditionEnabled.Value);
        RenderingPatch.NightLight.Enable(NightLightEnabled.Value);
        ArchitectModePatch.RemoveBuildRangeLimit.Enable(RemoveBuildRangeLimitEnabled.Value);
        ArchitectModePatch.LargerAreaForUpgradeAndDismantle.Enable(LargerAreaForUpgradeAndDismantleEnabled.Value);
        ArchitectModePatch.LargerAreaForTerraform.Enable(LargerAreaForTerraformEnabled.Value);
        BuildToolPatch.OffGridBuilding.Enable(OffGridBuildingEnabled.Value);
        BuildToolPatch.TreatStackingAsSingle.Enable(TreatStackingAsSingleEnabled.Value);
        ImmediateBuildPatch.QuickBuildAndDismantleLab.Enable(QuickBuildAndDismantleLabsEnabled.Value);
        VeinProtectionPatch.ProtectVeinsFromExhaustion.Enable(ProtectVeinsFromExhaustionEnabled.Value);
        RenderingPatch.DoNotRenderEntities.Enable(DoNotRenderEntitiesEnabled.Value);
        BuildToolPatch.DragBuildPowerPoles.Enable(DragBuildPowerPolesEnabled.Value);
        ImmediateBuildPatch.AutoConstructButton.Enable(AutoConstructButtonEnabled.Value);
        BeltSignalPatch.BeltSignalsForBuyOut.Enable(BeltSignalsForBuyOutEnabled.Value);
        BuildToolPatch.TankFastFillInAndTakeOut.Enable(TankFastFillInAndTakeOutEnabled.Value);
        BuildingBufferPatch.TweakBuildingBuffer.Enable(TweakBuildingBufferEnabled.Value);
        BuildToolPatch.PressShiftToTakeWholeBeltItems.Enable(PressShiftToTakeWholeBeltItemsEnabled.Value);

        BuildToolPatch.Enable(true);
        UpdateTankFastFillInAndTakeOutMultiplierRealValue();
    }

    public static void Uninit()
    {
        BuildToolPatch.Enable(false);

        BuildToolPatch.PressShiftToTakeWholeBeltItems.Enable(false);
        BuildingBufferPatch.TweakBuildingBuffer.Enable(false);
        BuildToolPatch.TankFastFillInAndTakeOut.Enable(false);
        BeltSignalPatch.BeltSignalsForBuyOut.Enable(false);
        ImmediateBuildPatch.AutoConstructButton.Enable(false);
        BuildToolPatch.DragBuildPowerPoles.Enable(false);
        RenderingPatch.DoNotRenderEntities.Enable(false);
        VeinProtectionPatch.ProtectVeinsFromExhaustion.Enable(false);
        ImmediateBuildPatch.QuickBuildAndDismantleLab.Enable(false);
        BuildToolPatch.TreatStackingAsSingle.Enable(false);
        BuildToolPatch.OffGridBuilding.Enable(false);
        ArchitectModePatch.LargerAreaForTerraform.Enable(false);
        ArchitectModePatch.LargerAreaForUpgradeAndDismantle.Enable(false);
        ArchitectModePatch.RemoveBuildRangeLimit.Enable(false);
        RenderingPatch.NightLight.Enable(false);
        ArchitectModePatch.RemoveSomeConditionBuild.Enable(false);
        ArchitectModePatch.UnlimitInteractive.Enable(false);

        BeltSignalPatch.UninitPersist();
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
        BeltSignalPatch.Export(w);
    }

    public static void Import(BinaryReader r, ushort version)
    {
        BeltSignalPatch.Import(r);
    }
}
