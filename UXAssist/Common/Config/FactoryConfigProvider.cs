using BepInEx.Configuration;
using UXAssist.Patches.Factory;

namespace UXAssist.Common.Config;

public static class FactoryConfigProvider
{
    public static ConfigEntry<bool> UnlimitInteractiveEnabled => FactoryPatch.UnlimitInteractiveEnabled;
    public static ConfigEntry<bool> RemoveSomeConditionEnabled => FactoryPatch.RemoveSomeConditionEnabled;
    public static ConfigEntry<bool> NightLightEnabled => FactoryPatch.NightLightEnabled;
    public static ConfigEntry<float> NightLightAngleX => FactoryPatch.NightLightAngleX;
    public static ConfigEntry<float> NightLightAngleY => FactoryPatch.NightLightAngleY;
    public static ConfigEntry<bool> RemoveBuildRangeLimitEnabled => FactoryPatch.RemoveBuildRangeLimitEnabled;
    public static ConfigEntry<bool> LargerAreaForUpgradeAndDismantleEnabled => FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled;
    public static ConfigEntry<bool> LargerAreaForTerraformEnabled => FactoryPatch.LargerAreaForTerraformEnabled;
    public static ConfigEntry<bool> OffGridBuildingEnabled => FactoryPatch.OffGridBuildingEnabled;
    public static ConfigEntry<bool> TreatStackingAsSingleEnabled => FactoryPatch.TreatStackingAsSingleEnabled;
    public static ConfigEntry<bool> QuickBuildAndDismantleLabsEnabled => FactoryPatch.QuickBuildAndDismantleLabsEnabled;
    public static ConfigEntry<bool> ProtectVeinsFromExhaustionEnabled => FactoryPatch.ProtectVeinsFromExhaustionEnabled;
    public static ConfigEntry<bool> DoNotRenderEntitiesEnabled => FactoryPatch.DoNotRenderEntitiesEnabled;
    public static ConfigEntry<bool> DragBuildPowerPolesEnabled => FactoryPatch.DragBuildPowerPolesEnabled;
    public static ConfigEntry<bool> DragBuildPowerPolesAlternatelyEnabled => FactoryPatch.DragBuildPowerPolesAlternatelyEnabled;
    public static ConfigEntry<bool> AutoConstructButtonEnabled => FactoryPatch.AutoConstructButtonEnabled;
    public static ConfigEntry<bool> BeltSignalsForBuyOutEnabled => FactoryPatch.BeltSignalsForBuyOutEnabled;
    public static ConfigEntry<bool> TankFastFillInAndTakeOutEnabled => FactoryPatch.TankFastFillInAndTakeOutEnabled;
    public static ConfigEntry<int> TankFastFillInAndTakeOutMultiplier => FactoryPatch.TankFastFillInAndTakeOutMultiplier;
    public static ConfigEntry<bool> CutConveyorBeltEnabled => FactoryPatch.CutConveyorBeltEnabled;
    public static ConfigEntry<bool> TweakBuildingBufferEnabled => FactoryPatch.TweakBuildingBufferEnabled;
    public static ConfigEntry<int> AssemblerBufferTimeMultiplier => FactoryPatch.AssemblerBufferTimeMultiplier;
    public static ConfigEntry<int> AssemblerBufferMininumMultiplier => FactoryPatch.AssemblerBufferMininumMultiplier;
    public static ConfigEntry<int> LabBufferMaxCountForAssemble => FactoryPatch.LabBufferMaxCountForAssemble;
    public static ConfigEntry<int> LabBufferExtraCountForAdvancedAssemble => FactoryPatch.LabBufferExtraCountForAdvancedAssemble;
    public static ConfigEntry<int> LabBufferMaxCountForResearch => FactoryPatch.LabBufferMaxCountForResearch;
    public static ConfigEntry<int> ReceiverBufferCount => FactoryPatch.ReceiverBufferCount;
    public static ConfigEntry<int> EjectorBufferCount => FactoryPatch.EjectorBufferCount;
    public static ConfigEntry<int> SiloBufferCount => FactoryPatch.SiloBufferCount;
    public static ConfigEntry<bool> ShortcutKeysForBlueprintCopyEnabled => FactoryPatch.ShortcutKeysForBlueprintCopyEnabled;
    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsEnabled => FactoryPatch.PressShiftToTakeWholeBeltItemsEnabled;
    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeBranches => FactoryPatch.PressShiftToTakeWholeBeltItemsIncludeBranches;
    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeInserters => FactoryPatch.PressShiftToTakeWholeBeltItemsIncludeInserters;
}
