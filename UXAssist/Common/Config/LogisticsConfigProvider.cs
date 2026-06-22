using BepInEx.Configuration;
using UXAssist.Patches.Logistics;

namespace UXAssist.Common.Config;

public static class LogisticsConfigProvider
{
    public static ConfigEntry<bool> AutoConfigLogisticsEnabled => LogisticsPatch.AutoConfigLogisticsEnabled;
    public static ConfigEntry<bool> AutoConfigLimitAutoReplenishCount => LogisticsPatch.AutoConfigLimitAutoReplenishCount;
    public static ConfigEntry<int> AutoConfigDispenserChargePower => LogisticsPatch.AutoConfigDispenserChargePower;
    public static ConfigEntry<int> AutoConfigDispenserCourierCount => LogisticsPatch.AutoConfigDispenserCourierCount;
    public static ConfigEntry<int> AutoConfigBattleBaseChargePower => LogisticsPatch.AutoConfigBattleBaseChargePower;
    public static ConfigEntry<int> AutoConfigPLSChargePower => LogisticsPatch.AutoConfigPLSChargePower;
    public static ConfigEntry<int> AutoConfigPLSMaxTripDrone => LogisticsPatch.AutoConfigPLSMaxTripDrone;
    public static ConfigEntry<int> AutoConfigPLSDroneMinDeliver => LogisticsPatch.AutoConfigPLSDroneMinDeliver;
    public static ConfigEntry<int> AutoConfigPLSMinPilerValue => LogisticsPatch.AutoConfigPLSMinPilerValue;
    public static ConfigEntry<int> AutoConfigPLSDroneCount => LogisticsPatch.AutoConfigPLSDroneCount;
    public static ConfigEntry<bool> SetDefaultRemoteLogicToStorage => LogisticsPatch.SetDefaultRemoteLogicToStorage;
    public static ConfigEntry<int> AutoConfigILSChargePower => LogisticsPatch.AutoConfigILSChargePower;
    public static ConfigEntry<int> AutoConfigILSMaxTripDrone => LogisticsPatch.AutoConfigILSMaxTripDrone;
    public static ConfigEntry<int> AutoConfigILSMaxTripShip => LogisticsPatch.AutoConfigILSMaxTripShip;
    public static ConfigEntry<int> AutoConfigILSWarperDistance => LogisticsPatch.AutoConfigILSWarperDistance;
    public static ConfigEntry<int> AutoConfigILSDroneMinDeliver => LogisticsPatch.AutoConfigILSDroneMinDeliver;
    public static ConfigEntry<int> AutoConfigILSShipMinDeliver => LogisticsPatch.AutoConfigILSShipMinDeliver;
    public static ConfigEntry<int> AutoConfigILSMinPilerValue => LogisticsPatch.AutoConfigILSMinPilerValue;
    public static ConfigEntry<bool> AutoConfigILSIncludeOrbitCollector => LogisticsPatch.AutoConfigILSIncludeOrbitCollector;
    public static ConfigEntry<bool> AutoConfigILSWarperNecessary => LogisticsPatch.AutoConfigILSWarperNecessary;
    public static ConfigEntry<int> AutoConfigILSDroneCount => LogisticsPatch.AutoConfigILSDroneCount;
    public static ConfigEntry<int> AutoConfigILSShipCount => LogisticsPatch.AutoConfigILSShipCount;
    public static ConfigEntry<int> AutoConfigVeinCollectorHarvestSpeed => LogisticsPatch.AutoConfigVeinCollectorHarvestSpeed;
    public static ConfigEntry<int> AutoConfigVeinCollectorMinPilerValue => LogisticsPatch.AutoConfigVeinCollectorMinPilerValue;
    public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled => LogisticsPatch.LogisticsCapacityTweaksEnabled;
    public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled => LogisticsPatch.AllowOverflowInLogisticsEnabled;
    public static ConfigEntry<bool> GreaterPowerUsageInLogisticsEnabled => LogisticsPatch.GreaterPowerUsageInLogisticsEnabled;
    public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled => LogisticsPatch.LogisticsConstrolPanelImprovementEnabled;
    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelEnabled => LogisticsPatch.RealtimeLogisticsInfoPanelEnabled;
    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelBarsEnabled => LogisticsPatch.RealtimeLogisticsInfoPanelBarsEnabled;
}
