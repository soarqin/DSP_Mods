using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Logistics;

[ModFeature("Logistics", Order = 11)]
public static class LogisticsPatch
{
    public static ConfigEntry<bool> AutoConfigLogisticsEnabled;
    public static ConfigEntry<bool> AutoConfigLimitAutoReplenishCount;
    // Dispenser config
    public static ConfigEntry<int> AutoConfigDispenserChargePower; // 3~30, display as 300000.0 * value
    public static ConfigEntry<int> AutoConfigDispenserCourierCount; // 0~10
    // Battlefield Analysis Base
    public static ConfigEntry<int> AutoConfigBattleBaseChargePower; // 4~40, display as 300000.0 * value
    // PLS config
    public static ConfigEntry<int> AutoConfigPLSChargePower; // 2~20, display as 3000000.0 * value
    public static ConfigEntry<int> AutoConfigPLSMaxTripDrone; // 1~180, by degress
    public static ConfigEntry<int> AutoConfigPLSDroneMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value
    public static ConfigEntry<int> AutoConfigPLSMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count
    public static ConfigEntry<int> AutoConfigPLSDroneCount; // 0~50
    // ILS config
    public static ConfigEntry<bool> SetDefaultRemoteLogicToStorage;
    public static ConfigEntry<int> AutoConfigILSChargePower; // 2~20, display as 15000000.0 * value
    public static ConfigEntry<int> AutoConfigILSMaxTripDrone; // 1~180, by degress
    public static ConfigEntry<int> AutoConfigILSMaxTripShip; // 1~41; 1~20 = value LY, 21-40 = 2*value-20LY, 41 = Unlimited
    public static ConfigEntry<int> AutoConfigILSWarperDistance; // 2~21; 2~7 = value * 0.5 - 0.5AU, 8~16 = value - 4AU, 17~20 = value * 2 - 20AU, 21 = 60AU
    public static ConfigEntry<int> AutoConfigILSDroneMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value
    public static ConfigEntry<int> AutoConfigILSShipMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value
    public static ConfigEntry<int> AutoConfigILSMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count
    public static ConfigEntry<bool> AutoConfigILSIncludeOrbitCollector;
    public static ConfigEntry<bool> AutoConfigILSWarperNecessary;
    public static ConfigEntry<int> AutoConfigILSDroneCount; // 0~100
    public static ConfigEntry<int> AutoConfigILSShipCount; // 0~10
    // Vein collector config
    public static ConfigEntry<int> AutoConfigVeinCollectorHarvestSpeed; // 0-20, 100% + 10% * value
    public static ConfigEntry<int> AutoConfigVeinCollectorMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count

    public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled;
    public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled;
    public static ConfigEntry<bool> GreaterPowerUsageInLogisticsEnabled;
    public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled;
    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelEnabled;
    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelBarsEnabled;

    public static void Init()
    {
        AutoConfigLogisticsEnabled.SettingChanged += (_, _) => AutoConfigLogistics.Enable(AutoConfigLogisticsEnabled.Value);
        AutoConfigLimitAutoReplenishCount.SettingChanged += (_, _) => AutoConfigLogistics.ToggleLimitAutoReplenishCount();
        SetDefaultRemoteLogicToStorage.SettingChanged += (_, _) => AutoConfigLogisticsSetDefaultRemoteLogicToStorage.Enable(SetDefaultRemoteLogicToStorage.Value);
        LogisticsCapacityTweaksEnabled.SettingChanged += (_, _) => LogisticsCapacityTweaks.Enable(LogisticsCapacityTweaksEnabled.Value);
        AllowOverflowInLogisticsEnabled.SettingChanged += (_, _) => AllowOverflowInLogistics.Enable(AllowOverflowInLogisticsEnabled.Value);
        GreaterPowerUsageInLogisticsEnabled.SettingChanged += (_, _) => GreaterPowerUsageInLogistics.Enable(GreaterPowerUsageInLogisticsEnabled.Value);
        LogisticsConstrolPanelImprovementEnabled.SettingChanged += (_, _) => LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);
        RealtimeLogisticsInfoPanelEnabled.SettingChanged += (_, _) => RealtimeLogisticsInfoPanel.Enable(RealtimeLogisticsInfoPanelEnabled.Value);
        RealtimeLogisticsInfoPanelBarsEnabled.SettingChanged += (_, _) => RealtimeLogisticsInfoPanel.EnableBars(RealtimeLogisticsInfoPanelBarsEnabled.Value);
    }

    public static void Start()
    {
        AutoConfigLogistics.Enable(AutoConfigLogisticsEnabled.Value);
        AutoConfigLogisticsSetDefaultRemoteLogicToStorage.Enable(SetDefaultRemoteLogicToStorage.Value);
        LogisticsCapacityTweaks.Enable(LogisticsCapacityTweaksEnabled.Value);
        AllowOverflowInLogistics.Enable(AllowOverflowInLogisticsEnabled.Value);
        GreaterPowerUsageInLogistics.Enable(GreaterPowerUsageInLogisticsEnabled.Value);
        LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);
        RealtimeLogisticsInfoPanel.Enable(RealtimeLogisticsInfoPanelEnabled.Value);
        RealtimeLogisticsInfoPanel.EnableBars(RealtimeLogisticsInfoPanelBarsEnabled.Value);

        GameLogicProc.OnGameBegin += RealtimeLogisticsInfoPanel.OnGameBegin;
        GameLogicProc.OnGameEnd += RealtimeLogisticsInfoPanel.OnGameEnd;
        GameLogicProc.OnDataLoaded += RealtimeLogisticsInfoPanel.OnDataLoaded;
    }

    public static void Uninit()
    {
        GameLogicProc.OnDataLoaded -= RealtimeLogisticsInfoPanel.OnDataLoaded;
        GameLogicProc.OnGameEnd -= RealtimeLogisticsInfoPanel.OnGameEnd;
        GameLogicProc.OnGameBegin -= RealtimeLogisticsInfoPanel.OnGameBegin;

        AutoConfigLogistics.Enable(false);
        AutoConfigLogisticsSetDefaultRemoteLogicToStorage.Enable(false);
        LogisticsCapacityTweaks.Enable(false);
        AllowOverflowInLogistics.Enable(false);
        GreaterPowerUsageInLogistics.Enable(false);
        LogisticsConstrolPanelImprovement.Enable(false);
        RealtimeLogisticsInfoPanel.Enable(false);
    }

    public static void OnUpdate() => RealtimeInfoPanelPatch.OnUpdate();

    public static void OnInputUpdate()
    {
        if (DSPGame.IsMenuDemo) return;
        if (VFInput.onGUI && LogisticsCapacityTweaksEnabled.Value)
        {
            LogisticsCapacityTweaks.UpdateInput();
        }
    }

    #region Apply auto-config values to existing facilities on the current planet

    private enum StationKind
    {
        Pls,
        Ils,
        VeinCollector
    }

    // === Per-field setters (single source of truth, shared by auto-config-on-build and apply-to-planet) ===

    internal static void StationSetChargePower(PlanetFactory factory, StationComponent station) =>
        factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick =
            (long)((station.isStellar ? 250000.0 * AutoConfigILSChargePower.Value : 50000.0 * AutoConfigPLSChargePower.Value) + 0.5);

    internal static void StationSetTripRangeDrones(PlanetFactory factory, StationComponent station) =>
        station.tripRangeDrones = Math.Cos((station.isStellar ? AutoConfigILSMaxTripDrone.Value : AutoConfigPLSMaxTripDrone.Value) / 180.0 * Math.PI);

    internal static void StationSetDeliveryDrones(PlanetFactory factory, StationComponent station)
    {
        var v = station.isStellar ? AutoConfigILSDroneMinDeliver.Value : AutoConfigPLSDroneMinDeliver.Value;
        station.deliveryDrones = v == 0 ? 1 : v * 10;
    }

    internal static void StationSetPilerCount(PlanetFactory factory, StationComponent station) =>
        station.pilerCount = station.isStellar ? AutoConfigILSMinPilerValue.Value : AutoConfigPLSMinPilerValue.Value;

    internal static void StationFillDrones(PlanetFactory factory, StationComponent station)
    {
        var target = station.isStellar ? AutoConfigILSDroneCount.Value : AutoConfigPLSDroneCount.Value;
        var toFill = Math.Max(0, target - station.idleDroneCount - station.workDroneCount);
        if (toFill > 0) station.idleDroneCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Drone, toFill, out _);
    }

    internal static void StationSetTripRangeShips(PlanetFactory factory, StationComponent station) =>
        station.tripRangeShips = AutoConfigILSMaxTripShip.Value switch
        {
            <= 20 => AutoConfigILSMaxTripShip.Value,
            <= 40 => AutoConfigILSMaxTripShip.Value * 2 - 20,
            _ => 10000,
        } * 2400000.0;

    internal static void StationSetWarpDistance(PlanetFactory factory, StationComponent station) =>
        station.warpEnableDist = AutoConfigILSWarperDistance.Value switch
        {
            <= 7 => AutoConfigILSWarperDistance.Value * 0.5 - 0.5,
            <= 16 => AutoConfigILSWarperDistance.Value - 4.0,
            <= 20 => AutoConfigILSWarperDistance.Value * 2 - 20.0,
            _ => 60.0,
        } * 40000.0;

    internal static void StationSetDeliveryShips(PlanetFactory factory, StationComponent station)
    {
        var v = AutoConfigILSShipMinDeliver.Value;
        station.deliveryShips = v == 0 ? 1 : v * 10;
    }

    internal static void StationFillShips(PlanetFactory factory, StationComponent station)
    {
        var toFill = Math.Max(0, AutoConfigILSShipCount.Value - station.idleShipCount - station.workShipCount);
        if (toFill > 0) station.idleShipCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Ship, toFill, out _);
    }

    internal static void StationSetIncludeOrbitCollector(PlanetFactory factory, StationComponent station) =>
        station.includeOrbitCollector = AutoConfigILSIncludeOrbitCollector.Value;

    internal static void StationSetWarperNecessary(PlanetFactory factory, StationComponent station) =>
        station.warperNecessary = AutoConfigILSWarperNecessary.Value;

    /* station.minerId may not be set yet on freshly built collectors, so resolve the minerId from the EntityData. */
    internal static bool VeinCollectorSetHarvestSpeed(PlanetFactory factory, StationComponent station)
    {
        ref var entity = ref factory.entityPool[station.entityId];
        if (entity.id != station.entityId || entity.minerId <= 0 || entity.minerId >= factory.factorySystem.minerCursor) return false;
        factory.factorySystem.minerPool[entity.minerId].speed = 10000 + AutoConfigVeinCollectorHarvestSpeed.Value * 1000;
        return true;
    }

    internal static void VeinCollectorSetPilerCount(PlanetFactory factory, StationComponent station) =>
        station.pilerCount = AutoConfigVeinCollectorMinPilerValue.Value;

    internal static void DispenserSetChargePower(PlanetFactory factory, DispenserComponent dispenser) =>
        factory.powerSystem.consumerPool[dispenser.pcId].workEnergyPerTick = (long)(5000.0 * AutoConfigDispenserChargePower.Value + 0.5);

    internal static void DispenserFillCouriers(PlanetFactory factory, DispenserComponent dispenser)
    {
        var toFill = Math.Max(0, AutoConfigDispenserCourierCount.Value - dispenser.idleCourierCount - dispenser.workCourierCount);
        if (toFill > 0) dispenser.idleCourierCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Bot, toFill, out _);
    }

    internal static void BattleBaseSetChargePower(PlanetFactory factory, BattleBaseComponent battleBase) =>
        factory.powerSystem.consumerPool[battleBase.pcId].workEnergyPerTick = (long)(5000.0 * AutoConfigBattleBaseChargePower.Value + 0.5);

    // === Per-facility "apply all settings" (also used as auto-config-on-build entry point) ===

    internal static void DoConfigStation(PlanetFactory factory, StationComponent station)
    {
        if (station.isCollector) return;
        if (station.isVeinCollector)
        {
            if (VeinCollectorSetHarvestSpeed(factory, station))
                VeinCollectorSetPilerCount(factory, station);
            return;
        }
        if (!station.isStellar)
        {
            StationSetChargePower(factory, station);
            StationSetTripRangeDrones(factory, station);
            StationSetDeliveryDrones(factory, station);
            StationSetPilerCount(factory, station);
            StationFillDrones(factory, station);
            return;
        }
        StationSetChargePower(factory, station);
        StationSetTripRangeDrones(factory, station);
        StationSetTripRangeShips(factory, station);
        StationSetWarpDistance(factory, station);
        StationSetDeliveryDrones(factory, station);
        StationSetDeliveryShips(factory, station);
        StationSetPilerCount(factory, station);
        StationSetIncludeOrbitCollector(factory, station);
        StationSetWarperNecessary(factory, station);
        StationFillDrones(factory, station);
        StationFillShips(factory, station);
    }

    // === Iterate over the current planet's facilities of a given kind ===

    private static void ForEachStation(StationKind kind, Action<PlanetFactory, StationComponent> action)
    {
        var factory = GameMain.localPlanet?.factory;
        var transport = factory?.transport;
        var stationPool = transport?.stationPool;
        if (stationPool == null) return;
        for (var i = transport.stationCursor - 1; i > 0; i--)
        {
            var station = stationPool[i];
            if (station == null || station.id != i || station.isCollector) continue;
            var skip = kind switch
            {
                StationKind.VeinCollector => !station.isVeinCollector,
                StationKind.Ils => station.isVeinCollector || !station.isStellar,
                StationKind.Pls => station.isVeinCollector || station.isStellar,
                _ => true
            };
            if (skip) continue;
            action(factory, station);
        }
        RefreshOpenLogisticsWindows();
    }

    private static void ForEachDispenser(Action<PlanetFactory, DispenserComponent> action)
    {
        var factory = GameMain.localPlanet?.factory;
        var transport = factory?.transport;
        var dispenserPool = transport?.dispenserPool;
        if (dispenserPool == null) return;
        for (var i = transport.dispenserCursor - 1; i > 0; i--)
        {
            var dispenser = dispenserPool[i];
            if (dispenser == null || dispenser.id != i) continue;
            action(factory, dispenser);
        }
        RefreshOpenLogisticsWindows();
    }

    private static void ForEachBattleBase(Action<PlanetFactory, BattleBaseComponent> action)
    {
        var factory = GameMain.localPlanet?.factory;
        var battleBases = factory?.defenseSystem?.battleBases;
        if (battleBases?.buffer == null) return;
        for (var i = battleBases.cursor - 1; i > 0; i--)
        {
            var battleBase = battleBases.buffer[i];
            if (battleBase == null || battleBase.id != i) continue;
            action(factory, battleBase);
        }
        RefreshOpenLogisticsWindows();
    }

    // Re-populate any open logistic facility detail window so applied values show immediately.
    internal static void RefreshOpenLogisticsWindows()
    {
        var uiRoot = UIRoot.instance;
        if (!uiRoot) return;
        var uiGame = uiRoot.uiGame;
        if (!uiGame) return;
        var stationWindow = uiGame.stationWindow;
        if (stationWindow && stationWindow.active) stationWindow.OnStationIdChange();
        var dispenserWindow = uiGame.dispenserWindow;
        if (dispenserWindow && dispenserWindow.active) dispenserWindow.OnDispenserIdChange();
        var battleBaseWindow = uiGame.battleBaseWindow;
        if (battleBaseWindow && battleBaseWindow.active) battleBaseWindow.OnBattleBaseIdChange();
    }

    // === Public entry points invoked by the config panel buttons ===

    // Dispenser
    public static void ApplyDispenserChargePower() => ForEachDispenser(DispenserSetChargePower);
    public static void ApplyDispenserCourierCount() => ForEachDispenser(DispenserFillCouriers);
    public static void ApplyAllDispenser() => ForEachDispenser((f, d) => { DispenserSetChargePower(f, d); DispenserFillCouriers(f, d); });

    // Battlefield Analysis Base
    public static void ApplyBattleBaseChargePower() => ForEachBattleBase(BattleBaseSetChargePower);
    public static void ApplyAllBattleBase() => ForEachBattleBase(BattleBaseSetChargePower);

    // PLS
    public static void ApplyPLSChargePower() => ForEachStation(StationKind.Pls, StationSetChargePower);
    public static void ApplyPLSTripRangeDrones() => ForEachStation(StationKind.Pls, StationSetTripRangeDrones);
    public static void ApplyPLSDroneMinDeliver() => ForEachStation(StationKind.Pls, StationSetDeliveryDrones);
    public static void ApplyPLSMinPilerValue() => ForEachStation(StationKind.Pls, StationSetPilerCount);
    public static void ApplyPLSDroneCount() => ForEachStation(StationKind.Pls, StationFillDrones);
    public static void ApplyAllPLS() => ForEachStation(StationKind.Pls, DoConfigStation);

    // ILS
    public static void ApplyILSChargePower() => ForEachStation(StationKind.Ils, StationSetChargePower);
    public static void ApplyILSTripRangeDrones() => ForEachStation(StationKind.Ils, StationSetTripRangeDrones);
    public static void ApplyILSTripRangeShips() => ForEachStation(StationKind.Ils, StationSetTripRangeShips);
    public static void ApplyILSWarpDistance() => ForEachStation(StationKind.Ils, StationSetWarpDistance);
    public static void ApplyILSDroneMinDeliver() => ForEachStation(StationKind.Ils, StationSetDeliveryDrones);
    public static void ApplyILSShipMinDeliver() => ForEachStation(StationKind.Ils, StationSetDeliveryShips);
    public static void ApplyILSMinPilerValue() => ForEachStation(StationKind.Ils, StationSetPilerCount);
    public static void ApplyILSDroneCount() => ForEachStation(StationKind.Ils, StationFillDrones);
    public static void ApplyILSShipCount() => ForEachStation(StationKind.Ils, StationFillShips);
    public static void ApplyILSIncludeOrbitCollector() => ForEachStation(StationKind.Ils, StationSetIncludeOrbitCollector);
    public static void ApplyILSWarperNecessary() => ForEachStation(StationKind.Ils, StationSetWarperNecessary);
    public static void ApplyAllILS() => ForEachStation(StationKind.Ils, DoConfigStation);

    // Vein Collector (Advanced Mining Machine)
    public static void ApplyVeinCollectorHarvestSpeed() => ForEachStation(StationKind.VeinCollector, (f, s) => VeinCollectorSetHarvestSpeed(f, s));
    public static void ApplyVeinCollectorMinPilerValue() => ForEachStation(StationKind.VeinCollector, VeinCollectorSetPilerCount);
    public static void ApplyAllVeinCollector() => ForEachStation(StationKind.VeinCollector, DoConfigStation);

    #endregion
}
