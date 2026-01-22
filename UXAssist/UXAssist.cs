using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using crecheng.DSPModSave;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Functions;
using UXAssist.Patches;
using UXAssist.UI;
using Util = UXAssist.Common.Util;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist;

[BepInDependency(CommonAPIPlugin.GUID)]
[BepInDependency(DSPModSavePlugin.MODGUID)]
[CommonAPISubmoduleDependency(nameof(CustomKeyBindSystem))]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(ModsCompat.PlanetVeinUtilization.PlanetVeinUtilizationGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class UXAssist : BaseUnityPlugin, IModCanSave
{
    public new static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private static ConfigFile _dummyConfig;
    private Type[] _patches, _compats;
    private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

    #region IModCanSave
    private const ushort ModSaveVersion = 2;

    public void Export(BinaryWriter w)
    {
        w.Write(ModSaveVersion);
        FactoryPatch.Export(w);
        UIFunctions.ExportClusterUploadResults(w);
    }

    public void Import(BinaryReader r)
    {
        var version = r.ReadUInt16();
        if (version <= 0) return;
        FactoryPatch.Import(r);
        if (version > 1)
        {
            UIFunctions.ImportClusterUploadResults(r);
        }
        else
        {
            UIFunctions.ClearClusterUploadResults();
        }
    }

    public void IntoOtherSave()
    {
    }
    #endregion

    UXAssist()
    {
        ModsCompat.PlanetVeinUtilization.Run(_harmony);
        ModsCompat.BlueprintTweaks.Run(_harmony);
        ModsCompat.CommonAPIWrapper.Run(_harmony);
    }

    private void Awake()
    {
        _dummyConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_GUID + "_dummy.cfg"), false)
        {
            SaveOnConfigSet = false
        };
        GamePatch.EnableWindowResizeEnabled = Config.Bind("Game", "EnableWindowResize", false,
            "Enable game window resize (maximum box and thick frame)");
        GamePatch.LoadLastWindowRectEnabled = Config.Bind("Game", "LoadLastWindowRect", false,
            "Load last window position and size when game starts");
        GamePatch.LastWindowRect = Config.Bind("Game", "LastWindowRect", new Vector4(0f, 0f, 0f, 0f),
            "Last window position and size");
        GamePatch.ProfileBasedSaveFolderEnabled = Config.Bind("Game", "ProfileBasedSaveFolder", false,
            "Profile-based save folder");
        GamePatch.ProfileBasedOptionEnabled = Config.Bind("Game", "ProfileBasedOption", false,
            "Profile-based option");
        GamePatch.DefaultProfileName = Config.Bind("Game", "DefaultProfileName", "Default",
            "Default profile name, used when profile-based save folder is enabled. Use original game save folder if matched");
        /*
        GamePatch.AutoSaveOptEnabled = Config.Bind("Game", "AutoSaveOpt", false,
            "Better auto-save mechanism");
        */
        GamePatch.ConvertSavesFromPeaceEnabled = Config.Bind("Game", "ConvertSavesFromPeace", false,
            "Convert saves from Peace mode to Combat mode on save loading");
        GamePatch.GameUpsFactor = _dummyConfig.Bind("Game", "GameUpsFactor", 1.0,
            "Game UPS factor (1.0 for normal speed)");
        WindowFunctions.ProcessPriority = Config.Bind("Game", "ProcessPriority", 2,
            new ConfigDescription("Game process priority\n  0: High  1: Above Normal  2: Normal  3: Below Normal  4: Idle", new AcceptableValueRange<int>(0, 4)));

        FactoryPatch.UnlimitInteractiveEnabled = Config.Bind("Factory", "UnlimitInteractive", false,
            "Unlimit interactive range");
        FactoryPatch.RemoveSomeConditionEnabled = Config.Bind("Factory", "RemoveSomeBuildConditionCheck", false,
            "Remove part of build condition checks that does not affect game logic");
        FactoryPatch.NightLightEnabled = Config.Bind("Factory", "NightLight", false,
            "Night light");
        FactoryPatch.NightLightAngleX = Config.Bind("Factory", "NightLightAngleX", -8f, "Night light angle X");
        FactoryPatch.NightLightAngleY = Config.Bind("Factory", "NightLightAngleY", -2f, "Night light angle Y");
        PlanetPatch.PlayerActionsInGlobeViewEnabled = Config.Bind("Planet", "PlayerActionsInGlobeView", false,
            "Enable player actions in globe view");
        FactoryPatch.RemoveBuildRangeLimitEnabled = Config.Bind("Factory", "RemoveBuildRangeLimit", false,
                "Remove limit for build range and maximum count of drag building belts/buildings\nNote: this does not affect range limit for mecha drones' action");
        FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled = Config.Bind("Factory", "LargerAreaForUpgradeAndDismantle", false,
            "Increase maximum area size for upgrade and dismantle to 31x31 (from 11x11)");
        FactoryPatch.LargerAreaForTerraformEnabled = Config.Bind("Factory", "LargerAreaForTerraform", false,
                "Increase maximum area size for terraform to 30x30 (from 10x10)\nNote: this may impact game performance while using large area");
        FactoryPatch.OffGridBuildingEnabled = Config.Bind("Factory", "OffGridBuilding", false,
            "Enable off grid building and stepped rotation");
        FactoryPatch.TreatStackingAsSingleEnabled = Config.Bind("Factory", "TreatStackingAsSingle", false,
            "Treat stack items as single in monitor components");
        FactoryPatch.QuickBuildAndDismantleLabsEnabled = Config.Bind("Factory", "QuickBuildAndDismantleLab", false,
            "Quick build and dismantle stacking labs");
        FactoryPatch.ProtectVeinsFromExhaustionEnabled = Config.Bind("Factory", "ProtectVeinsFromExhaustion", false,
            "Protect veins from exhaustion");
        FactoryPatch.ProtectVeinsFromExhaustion.KeepVeinAmount = Config.Bind("Factory", "KeepVeinAmount", 1000, new ConfigDescription("Keep veins amount (0 to disable)", new AcceptableValueRange<int>(0, 10000))).Value;
        FactoryPatch.ProtectVeinsFromExhaustion.KeepOilSpeed = Config.Bind("Factory", "KeepOilSpeed", 1.0f, new ConfigDescription("Keep minimal oil speed (< 0.1 to disable)", new AcceptableValueRange<float>(0.0f, 10.0f))).Value;
        FactoryPatch.DoNotRenderEntitiesEnabled = Config.Bind("Factory", "DoNotRenderEntities", false,
            "Do not render factory entities");
        FactoryPatch.DragBuildPowerPolesEnabled = Config.Bind("Factory", "DragBuildPowerPoles", false,
            "Drag building power poles in maximum connection range");
        FactoryPatch.DragBuildPowerPolesAlternatelyEnabled = Config.Bind("Factory", "DragBuildPowerPolesAlternately", true,
            "Build Tesla Tower and Wireless Power Tower alternately");
        FactoryPatch.BeltSignalsForBuyOutEnabled = Config.Bind("Factory", "BeltSignalsForBuyOut", false,
            "Belt signals for buy out dark fog items automatically");
        FactoryPatch.TankFastFillInAndTakeOutEnabled = Config.Bind("Factory", "TankFastFillInAndTakeOut", false,
            "Fast fill in to and take out from tanks");
        FactoryPatch.TankFastFillInAndTakeOutMultiplier = Config.Bind("Factory", "TankFastFillInAndTakeOutMultiplier", 1000, "Speed multiplier for fast filling in to and takeing out from tanks");
        FactoryPatch.CutConveyorBeltEnabled = Config.Bind("Factory", "CutConveyorBeltShortcut", false,
            "Cut conveyor belt (with shortcut key)");
        FactoryPatch.TweakBuildingBufferEnabled = Config.Bind("Factory", "TweakBuildingBuffer", false,
            "Tweak buffer count for assemblers and power generators");
        FactoryPatch.AssemblerBufferTimeMultiplier = Config.Bind("Factory", "AssemblerBufferTimeMultiplier", 4, new ConfigDescription("Assembler buffer time multiplier in seconds", new AcceptableValueRange<int>(2, 10)));
        FactoryPatch.AssemblerBufferMininumMultiplier = Config.Bind("Factory", "AssemblerBufferMininumMultiplier", 4, new ConfigDescription("Assembler buffer minimum multiplier", new AcceptableValueRange<int>(2, 10)));
        FactoryPatch.LabBufferMaxCountForAssemble = Config.Bind("Factory", "LabBufferMaxCountForAssemble", 6, new ConfigDescription("Buffer count for assembling in labs", new AcceptableValueRange<int>(2, 20)));
        FactoryPatch.LabBufferExtraCountForAdvancedAssemble = Config.Bind("Factory", "LabBufferExtraCountForAdvancedAssemble", 3, new ConfigDescription("Extra buffer count for Self-evolution Labs", new AcceptableValueRange<int>(1, 10)));
        FactoryPatch.LabBufferMaxCountForResearch = Config.Bind("Factory", "LabBufferMaxCountForResearch", 10, new ConfigDescription("Buffer count for researching in labs", new AcceptableValueRange<int>(2, 20)));
        FactoryPatch.ReceiverBufferCount = Config.Bind("Factory", "ReceiverBufferCount", 1, new ConfigDescription("Ray Receiver Graviton Lens buffer count", new AcceptableValueRange<int>(1, 20)));
        FactoryPatch.EjectorBufferCount = Config.Bind("Factory", "EjectorBufferCount", 1, new ConfigDescription("Ejector buffer count", new AcceptableValueRange<int>(5, 400)));
        FactoryPatch.SiloBufferCount = Config.Bind("Factory", "SiloBufferCount", 1, new ConfigDescription("Silo buffer count", new AcceptableValueRange<int>(1, 40)));
        FactoryPatch.ShortcutKeysForBlueprintCopyEnabled = Config.Bind("Factory", "DismantleBlueprintSelection", false,
            "Dismantle blueprint selected buildings");
        FactoryPatch.PressShiftToTakeWholeBeltItemsEnabled = Config.Bind("Factory", "PressShiftToTakeWholeBeltItems", false,
            "Ctrl+Shift+Click to pick items from whole belts");
        FactoryPatch.PressShiftToTakeWholeBeltItemsIncludeBranches = Config.Bind("Factory", "PressShiftToTakeWholeBeltItemsIncludeBranches", false,
            "Include branches of belts");
        FactoryPatch.PressShiftToTakeWholeBeltItemsIncludeInserters = Config.Bind("Factory", "PressShiftToTakeWholeBeltItemsIncludeInserters", false,
            "Include connected inserters");
        LogisticsPatch.AutoConfigLogisticsEnabled = Config.Bind("Factory", "AutoConfigLogistics", false,
            "Auto-config logistic stations");
        LogisticsPatch.AutoConfigLimitAutoReplenishCount = Config.Bind("Factory", "AutoConfigLimitAutoReplenishCount", false,
            "Limit auto-replenish count to config values");
        LogisticsPatch.SetDefaultRemoteLogicToStorage = Config.Bind("Factory", "AutoConfigSetDefaultRemoteLogicToStorage", false,
            "Set default remote logic to storage");
        LogisticsPatch.AutoConfigDispenserChargePower = Config.Bind("Factory", "AutoConfigDispenserChargePower", 30, new ConfigDescription("LD: Max. Charging Power", new AcceptableValueRange<int>(3, 30)));
        LogisticsPatch.AutoConfigDispenserCourierCount = Config.Bind("Factory", "AutoConfigDispenserCourierCount", 10, new ConfigDescription("LD: Count of Bots filled", new AcceptableValueRange<int>(0, 10)));
        LogisticsPatch.AutoConfigBattleBaseChargePower = Config.Bind("Factory", "AutoConfigBattleBaseChargePower", 8, new ConfigDescription("Battlefield Analysis Base: Max. Charging Power", new AcceptableValueRange<int>(4, 40)));
        LogisticsPatch.AutoConfigPLSChargePower = Config.Bind("Factory", "AutoConfigPLSChargePower", 4, new ConfigDescription("PLS: Max. Charging Power", new AcceptableValueRange<int>(2, 20)));
        LogisticsPatch.AutoConfigPLSMaxTripDrone = Config.Bind("Factory", "AutoConfigPLSMaxTripDrone", 180, new ConfigDescription("PLS: Drone transport range", new AcceptableValueRange<int>(1, 180)));
        LogisticsPatch.AutoConfigPLSDroneMinDeliver = Config.Bind("Factory", "AutoConfigPLSDroneMinDeliver", 10, new ConfigDescription("PLS: Min. Load of Drones", new AcceptableValueRange<int>(0, 10)));
        LogisticsPatch.AutoConfigPLSMinPilerValue = Config.Bind("Factory", "AutoConfigPLSMinPilerValue", 0, new ConfigDescription("PLS: Outgoing integration count", new AcceptableValueRange<int>(0, 4)));
        LogisticsPatch.AutoConfigPLSDroneCount = Config.Bind("Factory", "AutoConfigPLSDroneCount", 10, new ConfigDescription("PLS: Count of Drones filled", new AcceptableValueRange<int>(0, 50)));
        LogisticsPatch.AutoConfigILSChargePower = Config.Bind("Factory", "AutoConfigILSChargePower", 4, new ConfigDescription("ILS: Max. Charging Power", new AcceptableValueRange<int>(2, 20)));
        LogisticsPatch.AutoConfigILSMaxTripDrone = Config.Bind("Factory", "AutoConfigILSMaxTripDrone", 180, new ConfigDescription("ILS: Drone transport range", new AcceptableValueRange<int>(1, 180)));
        LogisticsPatch.AutoConfigILSMaxTripShip = Config.Bind("Factory", "AutoConfigILSMaxTripShip", 41, new ConfigDescription("ILS: Vessel transport range", new AcceptableValueRange<int>(1, 41)));
        LogisticsPatch.AutoConfigILSWarperDistance = Config.Bind("Factory", "AutoConfigILSWarperDistance", 16, new ConfigDescription("ILS: Warp distance", new AcceptableValueRange<int>(2, 21)));
        LogisticsPatch.AutoConfigILSDroneMinDeliver = Config.Bind("Factory", "AutoConfigILSDroneMinDeliver", 10, new ConfigDescription("ILS: Min. Load of Drones", new AcceptableValueRange<int>(0, 10)));
        LogisticsPatch.AutoConfigILSShipMinDeliver = Config.Bind("Factory", "AutoConfigILSShipMinDeliver", 10, new ConfigDescription("ILS: Min. Load of Vessels", new AcceptableValueRange<int>(0, 10)));
        LogisticsPatch.AutoConfigILSMinPilerValue = Config.Bind("Factory", "AutoConfigILSMinPilerValue", 0, new ConfigDescription("ILS: Outgoing integration count", new AcceptableValueRange<int>(0, 4)));
        LogisticsPatch.AutoConfigILSIncludeOrbitCollector = Config.Bind("Factory", "AutoConfigILSIncludeOrbitCollector", true,
            "ILS: Include Orbital Collector");
        LogisticsPatch.AutoConfigILSWarperNecessary = Config.Bind("Factory", "AutoConfigILSWarperNecessary", true,
            "ILS: Warpers required");
        LogisticsPatch.AutoConfigILSDroneCount = Config.Bind("Factory", "AutoConfigILSDroneCount", 20, new ConfigDescription("ILS: Count of Drones filled", new AcceptableValueRange<int>(0, 100)));
        LogisticsPatch.AutoConfigILSShipCount = Config.Bind("Factory", "AutoConfigILSShipCount", 10, new ConfigDescription("ILS: Count of Vessels filled", new AcceptableValueRange<int>(0, 10)));
        LogisticsPatch.AutoConfigVeinCollectorHarvestSpeed = Config.Bind("Factory", "AutoConfigVeinCollectorHarvestSpeed", 20, new ConfigDescription("AMM: Collecting Speed", new AcceptableValueRange<int>(0, 20)));
        LogisticsPatch.AutoConfigVeinCollectorMinPilerValue = Config.Bind("Factory", "AutoConfigVeinCollectorMinPilerValue", 0, new ConfigDescription("AMM: Outgoing integration count", new AcceptableValueRange<int>(0, 4)));
        LogisticsPatch.LogisticsCapacityTweaksEnabled = Config.Bind("Factory", "LogisticsCapacityTweaks", true,
            "Logistics capacity related tweaks");
        LogisticsPatch.AllowOverflowInLogisticsEnabled = Config.Bind("Factory", "AllowOverflowInLogistics", false,
            "Allow overflow in logistic stations");
        LogisticsPatch.LogisticsConstrolPanelImprovementEnabled = Config.Bind("Factory", "LogisticsConstrolPanelImprovement", false,
            "Logistics control panel improvement");
        LogisticsPatch.RealtimeLogisticsInfoPanelEnabled = Config.Bind("Factory", "RealtimeLogisticsInfoPanel", false,
            "Realtime logistics info panel");
        LogisticsPatch.RealtimeLogisticsInfoPanelBarsEnabled = Config.Bind("Factory", "RealtimeLogisticsInfoPanelBars", false,
            "Realtime logistics info panel - Show status bars for storage item");
        PlanetFunctions.OrbitalCollectorMaxBuildCount = Config.Bind("Factory", "OCMaxBuildCount", 0, "Maximum Orbital Collectors to build once, set to 0 to build as many as possible");
        PlayerPatch.EnhancedMechaForgeCountControlEnabled = Config.Bind("Player", "EnhancedMechaForgeCountControl", false,
            "Enhanced count control for hand-make, increases maximum of count to 1000, and you can hold Ctrl/Shift/Alt to change the count rapidly");
        PlayerPatch.HideTipsForSandsChangesEnabled = Config.Bind("Player", "HideTipsForGettingSands", false,
            "Hide tips for getting soil piles");
        PlayerPatch.ShortcutKeysForStarsNameEnabled = Config.Bind("Player", "ShortcutKeysForStarsName", false,
            "Shortcut keys for showing stars' name");
        PlayerPatch.AutoNavigationEnabled = Config.Bind("Player", "AutoNavigation", false,
            "Auto navigation");
        PlayerPatch.AutoCruiseEnabled = Config.Bind("Player", "AutoCruise", false,
            "Auto-cruise enabled");
        PlayerPatch.AutoBoostEnabled = Config.Bind("Player", "AutoBoost", false,
            "Auto boost speed with auto-cruise enabled");
        PlayerPatch.DistanceToWarp = Config.Bind("Player", "DistanceToWarp", 5.0, "Distance to warp (in AU)");
        TechPatch.SorterCargoStackingEnabled = Config.Bind("Tech", "SorterCargoStacking", false,
            "Restore upgrades of `Sorter Cargo Stacking` on panel");
        TechPatch.DisableBattleRelatedTechsInPeaceModeEnabled = Config.Bind("Tech", "DisableBattleRelatedTechsInPeaceMode", false,
            "Disable battle-related techs in Peace mode");
        TechPatch.BatchBuyoutTechEnabled = Config.Bind("Tech", "BatchBuyoutTech", false,
            "Can buy out techs with their prerequisites");
        DysonSpherePatch.StopEjectOnNodeCompleteEnabled = Config.Bind("DysonSphere", "StopEjectOnNodeComplete", false,
            "Stop ejectors when available nodes are all filled up");
        DysonSpherePatch.OnlyConstructNodesEnabled = Config.Bind("DysonSphere", "OnlyConstructNodes", false,
            "Construct only nodes but frames");
        DysonSpherePatch.AutoConstructMultiplier = Config.Bind("DysonSphere", "AutoConstructMultiplier", 1, "Dyson Sphere auto-construct speed multiplier");
        UIPatch.PlanetVeinUtilizationEnabled = Config.Bind("UI", "PlanetVeinUtilization", false,
            "Planet vein utilization");

        I18N.Init();
        I18N.Add("UXAssist Config", "UXAssist Config", "UX助手设置");

        // UI Patches
        GameLogicProc.Enable(true);

        UIConfigWindow.Init();

        _patches = Util.GetTypesFiltered(Assembly.GetExecutingAssembly(),
            t => string.Equals(t.Namespace, "UXAssist.Patches", StringComparison.Ordinal) || string.Equals(t.Namespace, "UXAssist.Functions", StringComparison.Ordinal));
        _patches?.Do(type => type.GetMethod("Init")?.Invoke(null, null));
        _compats = Util.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "UXAssist.ModsCompat");
        object[] parameters = [_harmony];
        _compats?.Do(type => type.GetMethod("Init")?.Invoke(null, parameters));

        I18N.Apply();
    }

    private void Start()
    {
        MyWindowManager.InitBaseObjects();
        MyWindowManager.Enable(true);

        _patches?.Do(type => type.GetMethod("Start")?.Invoke(null, null));

        object[] parameters = [_harmony];
        _compats?.Do(type => type.GetMethod("Start")?.Invoke(null, parameters));
    }

    private void OnDestroy()
    {
        _patches?.Do(type => type.GetMethod("Uninit")?.Invoke(null, null));

        MyWindowManager.Enable(false);
        GameLogicProc.Enable(false);
    }

    private void Update()
    {
        if (VFInput.inputing) return;
        if (DSPGame.IsMenuDemo)
        {
            UIFunctions.OnInputUpdate();
            return;
        }
        LogisticsPatch.OnInputUpdate();
        UIFunctions.OnInputUpdate();
        GamePatch.OnInputUpdate();
        FactoryPatch.OnInputUpdate();
        PlayerPatch.OnInputUpdate();

        LogisticsPatch.OnUpdate();
    }
}
