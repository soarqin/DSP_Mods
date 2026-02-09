using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UXAssist.Common;
using Object = UnityEngine.Object;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches;

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
        LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);
        RealtimeLogisticsInfoPanel.Enable(RealtimeLogisticsInfoPanelEnabled.Value);
        RealtimeLogisticsInfoPanel.EnableBars(RealtimeLogisticsInfoPanelBarsEnabled.Value);

        GameLogicProc.OnGameBegin += RealtimeLogisticsInfoPanel.OnGameBegin;
        GameLogicProc.OnDataLoaded += RealtimeLogisticsInfoPanel.OnDataLoaded;
    }

    public static void Uninit()
    {
        GameLogicProc.OnDataLoaded -= RealtimeLogisticsInfoPanel.OnDataLoaded;
        GameLogicProc.OnGameBegin -= RealtimeLogisticsInfoPanel.OnGameBegin;

        AutoConfigLogistics.Enable(false);
        AutoConfigLogisticsSetDefaultRemoteLogicToStorage.Enable(false);
        LogisticsCapacityTweaks.Enable(false);
        AllowOverflowInLogistics.Enable(false);
        LogisticsConstrolPanelImprovement.Enable(false);
        RealtimeLogisticsInfoPanel.Enable(false);
    }

    public static void OnUpdate()
    {
        if (RealtimeLogisticsInfoPanelEnabled.Value)
        {
            RealtimeLogisticsInfoPanel.StationInfoPanelsUpdate();
        }
    }

    public static void OnInputUpdate()
    {
        if (VFInput.onGUI && LogisticsCapacityTweaksEnabled.Value)
        {
            LogisticsCapacityTweaks.UpdateInput();
        }
    }

    private class AutoConfigLogistics : PatchImpl<AutoConfigLogistics>
    {
        enum KnownItemId : int
        {
            Drone = 5001,
            Ship = 5002,
            Bot = 5003,
            Warper = 1210,
        }

        protected override void OnEnable()
        {
            ToggleLimitAutoReplenishCount();
        }

        protected override void OnDisable()
        {
            ToggleLimitAutoReplenishCount();
        }

        public static void ToggleLimitAutoReplenishCount()
        {
            LimitAutoReplenishCount.Enable(AutoConfigLogisticsEnabled.Value && AutoConfigLimitAutoReplenishCount.Value);
        }

        private class LimitAutoReplenishCount : PatchImpl<LimitAutoReplenishCount>
        {
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityAutoReplenishIfNeeded))]
            [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.StationAutoReplenishIfNeeded))]
            private static IEnumerable<CodeInstruction> PlanetFactory_StationAutoReplenishIfNeeded_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var matcher = new CodeMatcher(instructions, generator);
                // Patch dispenser courier count
                matcher.Start().MatchForward(false,
                    new CodeMatch(ci => ci.IsLdloc()),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DispenserComponent), nameof(DispenserComponent.workCourierDatas))),
                    new CodeMatch(OpCodes.Ldlen),
                    new CodeMatch(OpCodes.Conv_I4)
                );
                matcher.Repeat(m => m.Advance(4).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(LogisticsPatch), nameof(AutoConfigDispenserCourierCount))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)]))
                ));

                // Patch PLS/ILS drone count
                matcher.Start().MatchForward(false,
                    new CodeMatch(ci => ci.IsLdloc()),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.workDroneDatas))),
                    new CodeMatch(OpCodes.Ldlen),
                    new CodeMatch(OpCodes.Conv_I4)
                );
                matcher.Repeat(m =>
                {
                    var instr = m.Instruction;
                    m.Advance(4).InsertAndAdvance(
                        instr,
                        Transpilers.EmitDelegate((int x, StationComponent station)
                            => Math.Min(x, station.isStellar ? AutoConfigILSDroneCount.Value : AutoConfigPLSDroneCount.Value))
                    );
                });

                // Patch ILS ship count
                matcher.Start().MatchForward(false,
                    new CodeMatch(ci => ci.IsLdloc()),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.workShipDatas))),
                    new CodeMatch(OpCodes.Ldlen),
                    new CodeMatch(OpCodes.Conv_I4)
                );
                matcher.Repeat(m => m.Advance(4).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(LogisticsPatch), nameof(AutoConfigILSShipCount))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)]))
                ));
                return matcher.InstructionEnumeration();
            }
        }

        private static void DoConfigStation(PlanetFactory factory, StationComponent station)
        {
            if (station.isCollector) return;
            if (station.isVeinCollector)
            {
                /* station.minerId is not set at this point, so we need to fetch the minerId from the EntityData */
                ref var entity = ref factory.entityPool[station.entityId];
                if (entity.id != station.entityId || entity.minerId <= 0 || entity.minerId >= factory.factorySystem.minerCursor) return;
                factory.factorySystem.minerPool[entity.minerId].speed = 10000 + AutoConfigVeinCollectorHarvestSpeed.Value * 1000;
                station.pilerCount = AutoConfigVeinCollectorMinPilerValue.Value;
                return;
            }
            int toFill;
            if (!station.isStellar)
            {
                factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = (long)(50000.0 * AutoConfigPLSChargePower.Value + 0.5);
                station.tripRangeDrones = Math.Cos(AutoConfigPLSMaxTripDrone.Value / 180.0 * Math.PI);
                station.deliveryDrones = AutoConfigPLSDroneMinDeliver.Value switch { 0 => 1, _ => AutoConfigPLSDroneMinDeliver.Value * 10 };
                station.pilerCount = AutoConfigPLSMinPilerValue.Value;
                toFill = Math.Max(0, AutoConfigPLSDroneCount.Value - station.idleDroneCount - station.workDroneCount);
                if (toFill > 0) station.idleDroneCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Drone, toFill, out _);
                return;
            }
            factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = (long)(250000.0 * AutoConfigILSChargePower.Value + 0.5);
            station.tripRangeDrones = Math.Cos(AutoConfigILSMaxTripDrone.Value / 180.0 * Math.PI);
            station.tripRangeShips = AutoConfigILSMaxTripShip.Value switch
            {
                <= 20 => AutoConfigILSMaxTripShip.Value,
                <= 40 => AutoConfigILSMaxTripShip.Value * 2 - 20,
                _ => 10000,
            } * 2400000.0;
            station.warpEnableDist = AutoConfigILSWarperDistance.Value switch
            {
                <= 7 => AutoConfigILSWarperDistance.Value * 0.5 - 0.5,
                <= 16 => AutoConfigILSWarperDistance.Value - 4.0,
                <= 20 => AutoConfigILSWarperDistance.Value * 2 - 20.0,
                _ => 60.0,
            } * 40000.0;
            station.deliveryDrones = AutoConfigILSDroneMinDeliver.Value switch { 0 => 1, _ => AutoConfigILSDroneMinDeliver.Value * 10 };
            station.deliveryShips = AutoConfigILSShipMinDeliver.Value switch { 0 => 1, _ => AutoConfigILSShipMinDeliver.Value * 10 };
            station.pilerCount = AutoConfigILSMinPilerValue.Value;
            station.includeOrbitCollector = AutoConfigILSIncludeOrbitCollector.Value;
            station.warperNecessary = AutoConfigILSWarperNecessary.Value;
            toFill = Math.Max(0, AutoConfigILSDroneCount.Value - station.idleDroneCount - station.workDroneCount);
            if (toFill > 0) station.idleDroneCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Drone, toFill, out _);
            toFill = Math.Max(0, AutoConfigILSShipCount.Value - station.idleShipCount - station.workShipCount);
            if (toFill > 0) station.idleShipCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Ship, toFill, out _);
        }

        private static void DoConfigDispenser(PlanetFactory factory, DispenserComponent dispenser)
        {
            var toFill = Math.Max(0, AutoConfigDispenserCourierCount.Value - dispenser.idleCourierCount - dispenser.workCourierCount);
            if (toFill > 0) dispenser.idleCourierCount += GameMain.data.mainPlayer.package.TakeItem((int)KnownItemId.Bot, toFill, out _);
        }

        private static void DoConfigBattleBase(PlanetFactory factory, BattleBaseComponent battleBase)
        {
            factory.powerSystem.consumerPool[battleBase.pcId].workEnergyPerTick = (long)(5000.0 * AutoConfigBattleBaseChargePower.Value + 0.5);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.SetDefaultParams))]
        private static void BuildTool_Addon_SetDefaultParams_Postfix(BuildTool_Addon __instance, int bpIndex)
        {
            if (__instance.handPrefabDesc.isDispenser)
            {
                __instance.handBpParams[bpIndex][2] = (int)(long)(5000.0 * AutoConfigDispenserChargePower.Value + 0.5);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewStationComponent))]
        private static void PlanetTransport_NewStationComponent_Postfix(PlanetTransport __instance, StationComponent __result)
        {
            DoConfigStation(__instance.factory, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.NewBattleBaseComponent))]
        private static void DefenseSystem_NewBattleBaseComponent_Postfix(DefenseSystem __instance, int __result)
        {
            if (__result <= 0) return;
            DoConfigBattleBase(__instance.factory, __instance.battleBases[__result]);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewDispenserComponent))]
        private static void PlanetTransport_NewDispenserComponent_Postfix(PlanetTransport __instance, int __result)
        {
            if (__result <= 0) return;
            DoConfigDispenser(__instance.factory, __instance.dispenserPool[__result]);
        }
    }

    public class AutoConfigLogisticsSetDefaultRemoteLogicToStorage : PatchImpl<AutoConfigLogisticsSetDefaultRemoteLogicToStorage>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnItemPickerReturn))]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnItemPickerReturn))]
        private static IEnumerable<CodeInstruction> UIStationStorage_OnItemPickerReturn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.isStellar))),
                new CodeMatch(ci => ci.opcode == OpCodes.Brtrue_S || ci.opcode == OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.opcode == OpCodes.Br_S || ci.opcode == OpCodes.Br),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer)))
            );
            if (matcher.IsValid)
            {
                matcher.RemoveInstructions(7).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_I4_0)
                );
            }
            return matcher.InstructionEnumeration();
        }
    }

    public class LogisticsCapacityTweaks : PatchImpl<LogisticsCapacityTweaks>
    {
        private static KeyCode _lastKey = KeyCode.None;
        private static long _nextKeyTick;
        private static bool _skipNextUIStationStorageEvent;
        private static bool _skipNextUIControlPanelStationStorageEvent;

        private static bool UpdateKeyPressed(KeyCode code)
        {
            if (!Input.GetKey(code))
                return false;
            if (code != _lastKey)
            {
                _lastKey = code;
                _nextKeyTick = GameMain.instance.timei + 30;
                return true;
            }

            var currTick = GameMain.instance.timei;
            if (_nextKeyTick > currTick) return false;
            _nextKeyTick = currTick + 4;
            return true;
        }

        public static void UpdateInput()
        {
            if (_lastKey != KeyCode.None && Input.GetKeyUp(_lastKey))
            {
                _lastKey = KeyCode.None;
            }

            if (VFInput.shift) return;
            var ctrl = VFInput.control;
            var alt = VFInput.alt;
            if (ctrl && alt) return;
            int delta;
            if (UpdateKeyPressed(KeyCode.LeftArrow))
            {
                if (ctrl)
                    delta = -100000;
                else if (alt)
                    delta = -1000;
                else
                    delta = -10;
            }
            else if (UpdateKeyPressed(KeyCode.RightArrow))
            {
                if (ctrl)
                    delta = 100000;
                else if (alt)
                    delta = 1000;
                else
                    delta = 10;
            }
            else if (UpdateKeyPressed(KeyCode.DownArrow))
            {
                if (ctrl)
                    delta = -1000000;
                else if (alt)
                    delta = -10000;
                else
                    delta = -100;
            }
            else if (UpdateKeyPressed(KeyCode.UpArrow))
            {
                if (ctrl)
                    delta = 1000000;
                else if (alt)
                    delta = 10000;
                else
                    delta = 100;
            }
            else
            {
                return;
            }

            var targets = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Input.mousePosition }, targets);
            foreach (var target in targets)
            {
                StationComponent station = null;
                int index = -1;
                PlanetFactory planetFactory = null;
                var stationStorage = target.gameObject.GetComponentInParent<UIStationStorage>();
                bool isControlPanelStationStorage = false;
                if (stationStorage != null)
                {
                    station = stationStorage.station;
                    index = stationStorage.index;
                    planetFactory = stationStorage.stationWindow?.factory;
                }
                else
                {
                    var controlPanelStationStorage = target.gameObject.GetComponentInParent<UIControlPanelStationStorage>();
                    if (controlPanelStationStorage == null) continue;
                    station = controlPanelStationStorage.station;
                    index = controlPanelStationStorage.index;
                    planetFactory = controlPanelStationStorage.factory;
                    isControlPanelStationStorage = true;
                }
                if (station?.storage is null) continue;
                ref var storage = ref station.storage[index];
                var oldMax = storage.max;
                var newMax = oldMax + delta;
                if (newMax < 0)
                {
                    newMax = 0;
                }
                else
                {
                    int itemCountMax;
                    if (AllowOverflowInLogisticsEnabled.Value)
                    {
                        itemCountMax = 90000000;
                    }
                    else
                    {
                        if (planetFactory == null || station.entityId <= 0 || station.entityId >= planetFactory.entityCursor) continue;
                        var modelProto = LDB.models.Select(planetFactory.entityPool[station.entityId].modelIndex);
                        itemCountMax = modelProto == null ? 0 : modelProto.prefabDesc.stationMaxItemCount;
                        itemCountMax += station.isStellar && !station.isCollector ? GameMain.history.remoteStationExtraStorage : GameMain.history.localStationExtraStorage;
                    }

                    if (newMax > itemCountMax)
                    {
                        newMax = itemCountMax;
                    }
                }

                storage.max = newMax;
                if (isControlPanelStationStorage)
                {
                    _skipNextUIControlPanelStationStorageEvent = oldMax / 100 != newMax / 100;
                }
                else
                {
                    _skipNextUIStationStorageEvent = oldMax / 100 != newMax / 100;
                }
                break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnMaxSliderValueChange))]
        private static bool UIStationStorage_OnMaxSliderValueChange_Prefix()
        {
            if (!_skipNextUIStationStorageEvent) return true;
            _skipNextUIStationStorageEvent = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnMaxSliderValueChange))]
        private static bool UIControlPanelStationStorage_OnMaxSliderValueChange_Prefix()
        {
            if (!_skipNextUIControlPanelStationStorageEvent) return true;
            _skipNextUIControlPanelStationStorageEvent = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.OnTechFunctionUnlocked))]
        private static bool PlanetTransport_OnTechFunctionUnlocked_Prefix(PlanetTransport __instance, int _funcId, double _valuelf, int _level)
        {
            switch (_funcId)
            {
                case 30:
                    {
                        var stationPool = __instance.stationPool;
                        var factory = __instance.factory;
                        var history = GameMain.history;
                        for (var i = __instance.stationCursor - 1; i > 0; i--)
                        {
                            if (stationPool[i] == null || stationPool[i].id != i || (stationPool[i].isStellar && !stationPool[i].isCollector && !stationPool[i].isVeinCollector)) continue;
                            var modelIndex = factory.entityPool[stationPool[i].entityId].modelIndex;
                            var maxCount = LDB.models.Select(modelIndex).prefabDesc.stationMaxItemCount;
                            var oldMaxCount = maxCount + history.localStationExtraStorage - _valuelf;
                            var intOldMaxCount = (int)Math.Round(oldMaxCount);
                            var intNewMaxCount = maxCount + history.localStationExtraStorage;
                            var ratio = intNewMaxCount / oldMaxCount;
                            var storage = stationPool[i].storage;
                            for (var j = storage.Length - 1; j >= 0; j--)
                            {
                                var max = storage[j].max;
                                if (max + 10 < intOldMaxCount || max >= intNewMaxCount) continue;
                                storage[j].max = Mathf.RoundToInt((float)(max * ratio / 50.0)) * 50;
                            }
                        }

                        break;
                    }
                case 31:
                    {
                        var stationPool = __instance.stationPool;
                        var factory = __instance.factory;
                        var history = GameMain.history;
                        for (var i = __instance.stationCursor - 1; i > 0; i--)
                        {
                            if (stationPool[i] == null || stationPool[i].id != i || !stationPool[i].isStellar || stationPool[i].isCollector || stationPool[i].isVeinCollector) continue;
                            var modelIndex = factory.entityPool[stationPool[i].entityId].modelIndex;
                            var maxCount = LDB.models.Select(modelIndex).prefabDesc.stationMaxItemCount;
                            var oldMaxCount = maxCount + history.remoteStationExtraStorage - _valuelf;
                            var intOldMaxCount = (int)Math.Round(oldMaxCount);
                            var intNewMaxCount = maxCount + history.remoteStationExtraStorage;
                            var ratio = intNewMaxCount / oldMaxCount;
                            var storage = stationPool[i].storage;
                            for (var j = storage.Length - 1; j >= 0; j--)
                            {
                                var max = storage[j].max;
                                if (max + 10 < intOldMaxCount || max >= intNewMaxCount) continue;
                                storage[j].max = Mathf.RoundToInt((float)(max * ratio / 100.0)) * 100;
                            }
                        }

                        break;
                    }
            }

            return false;
        }
    }

    private class AllowOverflowInLogistics : PatchImpl<AllowOverflowInLogistics>
    {
        private static bool _blueprintPasting;

        // Do not check for overflow when try to send hand items into storages
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnItemIconMouseDown))]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnItemIconMouseDown))]
        private static IEnumerable<CodeInstruction> UIStationStorage_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(LDB), nameof(LDB.items))),
                new CodeMatch(OpCodes.Ldarg_0)
            );
            var pos = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Stloc_S)
            );
            var inst = matcher.InstructionAt(1).Clone();
            var pos2 = matcher.Pos + 2;
            matcher.Start().Advance(pos);
            var labels = matcher.Labels;
            matcher.RemoveInstructions(pos2 - pos).Insert(
                new CodeInstruction(OpCodes.Ldloc_1).WithLabels(labels),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.inhandItemCount))),
                inst
            );
            return matcher.InstructionEnumeration();
        }

        // Remove storage limit check
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        private static IEnumerable<CodeInstruction> PlanetTransport_SetStationStorage_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(ci => ci.IsStarg())
            );
            var label = generator.DefineLabel();
            var oldLabels = matcher.Labels;
            matcher.Labels = [];
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(AllowOverflowInLogistics), nameof(_blueprintPasting))).WithLabels(oldLabels),
                new CodeInstruction(OpCodes.Brfalse, label)
            );
            matcher.Advance(9).Labels.Add(label);
            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        private static void BuildTool_BlueprintPaste_CreatePrebuilds_Prefix()
        {
            _blueprintPasting = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        private static void BuildTool_BlueprintPaste_CreatePrebuilds_Postfix()
        {
            _blueprintPasting = false;
        }
    }

    private class LogisticsConstrolPanelImprovement : PatchImpl<LogisticsConstrolPanelImprovement>
    {
        private static int ItemIdHintUnderMouse()
        {
            var itemId = GameMain.data.mainPlayer.inhandItemId;
            if (itemId > 0) return itemId;
            List<RaycastResult> targets = [];
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            EventSystem.current.RaycastAll(pointer, targets);
            foreach (var target in targets)
            {
                var btn = target.gameObject.GetComponentInParent<UIButton>();
                if (btn?.tips is { itemId: > 0 })
                {
                    return btn.tips.itemId;
                }

                var repWin = target.gameObject.GetComponentInParent<UIReplicatorWindow>();
                if (repWin != null)
                {
                    var mouseRecipeIndex = repWin.mouseRecipeIndex;
                    var recipeProtoArray = repWin.recipeProtoArray;
                    if (mouseRecipeIndex < 0)
                    {
                        return 0;
                    }

                    var recipeProto = recipeProtoArray[mouseRecipeIndex];
                    return recipeProto != null ? recipeProto.Results[0] : 0;
                }

                var grid = target.gameObject.GetComponentInParent<UIStorageGrid>();
                if (grid != null)
                {
                    var storage = grid.storage;
                    if (storage == null) return 0;
                    var mouseOnX = grid.mouseOnX;
                    var mouseOnY = grid.mouseOnY;
                    if (mouseOnX < 0 || mouseOnY < 0) return 0;
                    var gridIndex = mouseOnX + mouseOnY * grid.colCount;
                    return storage.grids[gridIndex].itemId;
                }

                var productEntry = target.gameObject.GetComponentInParent<UIProductEntry>();
                if (productEntry == null) continue;
                if (!productEntry.productionStatWindow.isProductionTab) return 0;
                return productEntry.entryData?.itemId ?? 0;
            }

            return 0;
        }

        private static bool SetFilterItemId(UIControlPanelFilterPanel filterPanel, int itemId)
        {
            var filter = filterPanel.GetCurrentFilter();
            if (filter.itemsFilter is { Length: 1 } && filter.itemsFilter[0] == itemId) return false;
            filter.itemsFilter = [itemId];
            filterPanel.SetNewFilter(filter);
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame.On_I_Switch))]
        private static IEnumerable<CodeInstruction> UIGame_On_I_Switch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.End().MatchBack(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UIGame), nameof(UIGame.ShutAllFunctionWindow)))
            );
            if (matcher.IsInvalid)
            {
                UXAssist.Logger.LogWarning("Failed to patch UIGame.On_I_Switch()");
                return matcher.InstructionEnumeration();
            }

            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                Transpilers.EmitDelegate((UIGame uiGame) =>
                {
                    var itemId = ItemIdHintUnderMouse();
                    if (itemId <= 0) return;
                    SetFilterItemId(uiGame.controlPanelWindow.filterPanel, itemId);
                })
            );
            return matcher.InstructionEnumeration();
        }

        private static void OnStationEntryItemIconRightClick(UIControlPanelStationEntry stationEntry, int slot)
        {
            var storage = stationEntry.station?.storage;
            if (storage == null) return;
            var itemId = storage.Length > slot ? storage[slot].itemId : 0;
            var uiRoot = UIRoot.instance;
            if (!uiRoot) return;
            var controlPanelWindow = uiRoot.uiGame?.controlPanelWindow;
            if (controlPanelWindow == null) return;
            var filterPanel = controlPanelWindow.filterPanel;
            if (filterPanel == null) return;
            if (!SetFilterItemId(filterPanel, itemId)) return;
            filterPanel.RefreshFilterUI();
            controlPanelWindow.DetermineFilterResults();
        }

        private static readonly Action<int>[] OnStationEntryItemIconRightClickActions = new Action<int>[5];

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationEntry), nameof(UIControlPanelStationEntry._OnRegEvent))]
        private static void UIControlPanelStationEntry__OnRegEvent_Postfix(UIControlPanelStationEntry __instance)
        {
            OnStationEntryItemIconRightClickActions[0] = _ => OnStationEntryItemIconRightClick(__instance, 0);
            OnStationEntryItemIconRightClickActions[1] = _ => OnStationEntryItemIconRightClick(__instance, 1);
            OnStationEntryItemIconRightClickActions[2] = _ => OnStationEntryItemIconRightClick(__instance, 2);
            OnStationEntryItemIconRightClickActions[3] = _ => OnStationEntryItemIconRightClick(__instance, 3);
            OnStationEntryItemIconRightClickActions[4] = _ => OnStationEntryItemIconRightClick(__instance, 4);
            __instance.storageItem0.itemButton.onRightClick += OnStationEntryItemIconRightClickActions[0];
            __instance.storageItem1.itemButton.onRightClick += OnStationEntryItemIconRightClickActions[1];
            __instance.storageItem2.itemButton.onRightClick += OnStationEntryItemIconRightClickActions[2];
            __instance.storageItem3.itemButton.onRightClick += OnStationEntryItemIconRightClickActions[3];
            __instance.storageItem4.itemButton.onRightClick += OnStationEntryItemIconRightClickActions[4];
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationEntry), nameof(UIControlPanelStationEntry._OnUnregEvent))]
        private static void UIControlPanelStationEntry__OnUnregEvent_Postfix(UIControlPanelStationEntry __instance)
        {
            __instance.storageItem0.itemButton.onRightClick -= OnStationEntryItemIconRightClickActions[0];
            __instance.storageItem1.itemButton.onRightClick -= OnStationEntryItemIconRightClickActions[1];
            __instance.storageItem2.itemButton.onRightClick -= OnStationEntryItemIconRightClickActions[2];
            __instance.storageItem3.itemButton.onRightClick -= OnStationEntryItemIconRightClickActions[3];
            __instance.storageItem4.itemButton.onRightClick -= OnStationEntryItemIconRightClickActions[4];
            for (var i = 0; i < 5; i++)
            {
                OnStationEntryItemIconRightClickActions[i] = null;
            }
        }
    }

    private static class RealtimeLogisticsInfoPanel
    {
        private static StationTip[] _stationTips = new StationTip[16];
        private static readonly StationTip[] StationTipsRecycle = new StationTip[128];
        private static readonly Sprite[] StateSprite = [null, null, null];
        private static int _stationTipsRecycleCount;
        private static GameObject _stationTipsRoot;
        private static GameObject _tipPrefab;
        private static readonly Color DemandColor = new(253f / 255, 150f / 255, 94f / 255);
        private static readonly Color SupplyColor = new(97f / 255, 216f / 255, 255f / 255);
        private static readonly Color OrderInColor = new(108f / 255, 187f / 255, 214f / 255);
        private static readonly Color OrderOutColor = new(255f / 255, 161f / 255, 109.5f / 255);

        private static int _lastPlanetId;

        private static int _localStorageMax = 5000;
        private static int _remoteStorageMax = 10000;
        private static int _localStorageExtra;
        private static int _remoteStorageExtra;
        private static int _localStorageMaxTotal = _localStorageMax;
        private static int _remoteStorageMaxTotal = _remoteStorageMax;
        private static float _localStoragePixelPerItem = StorageSliderWidth / _localStorageMaxTotal;
        private static float _remoteStoragePixelPerItem = StorageSliderWidth / _remoteStorageMaxTotal;

        private static int _storageMaxSlotCount = 5;
        private const int CarrierSlotCount = 3;
        private const float StorageSliderWidth = 70f;
        private const float StorageSliderHeight = 5f;

        private static bool UpdateStorageMax()
        {
            var history = GameMain.history;
            if (history == null) return false;
            if (_remoteStorageExtra == history.remoteStationExtraStorage) return false;
            _localStorageExtra = history.localStationExtraStorage;
            _remoteStorageExtra = history.remoteStationExtraStorage;
            _localStorageMaxTotal = _localStorageMax + _localStorageExtra;
            _remoteStorageMaxTotal = _remoteStorageMax + _remoteStorageExtra;
            _localStoragePixelPerItem = StorageSliderWidth / _localStorageMaxTotal;
            _remoteStoragePixelPerItem = StorageSliderWidth / _remoteStorageMaxTotal;
            return true;
        }

        private static readonly Sprite[] LogisticsExtraItemSprites =
        [
            Resources.Load<Sprite>("Icons/ItemRecipe/logistic-drone"),
            Resources.Load<Sprite>("Icons/ItemRecipe/logistic-vessel"),
            Resources.Load<Sprite>("Icons/ItemRecipe/space-warper")
        ];

        public static void Enable(bool on)
        {
            if (_stationTipsRoot == null) return;
            if (!on)
            {
                RecycleStationTips();
                _lastPlanetId = 0;
                _stationTipsRoot.SetActive(false);
                return;
            }
            if (DSPGame.IsMenuDemo || !GameMain.isRunning)
            {
                _lastPlanetId = 0;
                _stationTipsRoot.SetActive(false);
                return;
            }
            _lastPlanetId = GameMain.data?.localPlanet?.id ?? 0;
            _stationTipsRoot.SetActive(_lastPlanetId != 0);
        }

        public static void EnableBars(bool on)
        {
            foreach (var stationTip in _stationTips)
            {
                if (stationTip == null) continue;
                stationTip.SetBarsVisible(on);
            }
        }

        public static void OnGameBegin()
        {
            RecycleStationTips();
            _lastPlanetId = 0;
            _stationTipsRoot?.SetActive(false);
        }

        public static void OnDataLoaded()
        {
            _storageMaxSlotCount = 5;
            _localStorageMax = 5000;
            _remoteStorageMax = 10000;
            foreach (var model in LDB.models.dataArray)
            {
                var prefabDesc = model?.prefabDesc;
                if (prefabDesc == null) continue;
                if (prefabDesc.isStation)
                {
                    _storageMaxSlotCount = Math.Max(_storageMaxSlotCount, prefabDesc.stationMaxItemKinds);
                    if (prefabDesc.isStellarStation)
                    {
                        if (!prefabDesc.isCollectStation)
                        {
                            _remoteStorageMax = prefabDesc.stationMaxItemCount;
                        }
                    }
                    else
                    {
                        if (!prefabDesc.isVeinCollector)
                        {
                            _localStorageMax = prefabDesc.stationMaxItemCount;
                        }
                    }
                }
            }
            _localStorageExtra = -1;
            _remoteStorageExtra = -1;
            _localStoragePixelPerItem = 0f;
            _remoteStoragePixelPerItem = 0f;
            UpdateStorageMax();
            RealtimeLogisticsInfoPanel.InitGUI();
        }

        public static void InitGUI()
        {
            if (_stationTipsRoot != null) return;
            _stationTipsRoot = new GameObject("station-tips-root");
            var rtrans = _stationTipsRoot.AddComponent<RectTransform>();
            _stationTipsRoot.transform.SetParent(GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs").transform);
            rtrans.sizeDelta = new Vector2(0f, 0f);
            rtrans.localScale = new Vector3(1f, 1f, 1f);
            rtrans.anchorMax = new Vector2(1f, 1f);
            rtrans.anchorMin = new Vector2(0f, 0f);
            rtrans.pivot = new Vector2(0f, 0f);
            rtrans.anchoredPosition3D = new Vector3(0, 0, 0f);

            var sliderBgPrefab = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0/slider-bg");

            _tipPrefab = Object.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs/Vein Marks/vein-tips/vein-tip-prefab"), _stationTipsRoot.transform);
            _tipPrefab.name = "tipPrefab";
            Object.Destroy(_tipPrefab.GetComponent<UIVeinDetailNode>());
            var image = _tipPrefab.GetComponent<Image>();
            image.sprite = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Key Tips/tip-prefab").GetComponent<Image>().sprite;
            image.color = new Color(0, 0, 0, 0.8f);
            image.enabled = true;
            var rectTrans = (RectTransform)_tipPrefab.transform;
            rectTrans.localPosition = new Vector3(200f, 800f, 0);
            rectTrans.sizeDelta = new Vector2(150f, 160f);
            rectTrans.pivot = new Vector2(0.5f, 0.5f);

            var infoText = _tipPrefab.transform.Find("info-text").gameObject;
            var tipIconPrefab = _tipPrefab.transform.Find("icon");

            for (var index = 0; index < _storageMaxSlotCount; ++index)
            {
                var y = -5f - 35f * index;
                var itemIcon = Object.Instantiate(tipIconPrefab.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                itemIcon.name = "icon" + index;
                rectTrans = (RectTransform)itemIcon.transform;
                rectTrans.sizeDelta = new Vector2(30f, 30f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(0, y, 0);

                var sliderBg = Object.Instantiate(sliderBgPrefab.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                sliderBg.name = "sliderBg" + index;
                rectTrans = (RectTransform)sliderBg.transform;
                rectTrans.sizeDelta = new Vector2(StorageSliderWidth, StorageSliderHeight);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(30f, y - 22f, 0f);
                rectTrans = (RectTransform)sliderBg.transform.Find("current-fg").transform;
                rectTrans.sizeDelta = new Vector2(0f, StorageSliderHeight);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.localPosition = new Vector3(0f, 0f, 0f);
                rectTrans = (RectTransform)sliderBg.transform.Find("ordered-fg").transform;
                rectTrans.sizeDelta = new Vector2(0f, StorageSliderHeight);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.localPosition = new Vector3(0f, 0f, 0f);
                rectTrans = (RectTransform)sliderBg.transform.Find("max-fg").transform;
                rectTrans.sizeDelta = new Vector2(StorageSliderWidth, StorageSliderHeight);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.localPosition = new Vector3(0f, 0f, 0f);
                image = rectTrans.GetComponent<Image>();
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0.15f);
                Object.Destroy(sliderBg.GetComponent<Slider>());
                Object.Destroy(sliderBg.transform.Find("thumb").gameObject);
                Object.Destroy(sliderBg.transform.Find("speed-text").gameObject);
                sliderBg.gameObject.SetActive(false);

                var countText = Object.Instantiate(infoText, Vector3.zero, Quaternion.identity, _tipPrefab.transform);
                countText.name = "countText" + index;
                var text = countText.GetComponent<Text>();
                text.fontSize = 18;
                text.text = "";
                text.alignment = TextAnchor.UpperRight;
                rectTrans = (RectTransform)countText.transform;
                rectTrans.sizeDelta = new Vector2(70f, 20f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(30f, y, 0);
                Object.Destroy(countText.GetComponent<Shadow>());

                var stateLocal = Object.Instantiate(tipIconPrefab.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                stateLocal.name = "iconLocal" + index;
                stateLocal.GetComponent<Image>().material = null;
                rectTrans = (RectTransform)stateLocal.transform;
                rectTrans.sizeDelta = new Vector2(16f, 16f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(102f, y, 0);
                var stateRemote = Object.Instantiate(tipIconPrefab.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                stateRemote.name = "iconRemote" + index;
                stateRemote.GetComponent<Image>().material = null;
                rectTrans = (RectTransform)stateRemote.transform;
                rectTrans.sizeDelta = new Vector2(20f, 20f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(100f, y - 12f, 0);
            }

            var iconPrefab = Object.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Entity Briefs/brief-info-top/brief-info/content/icons/icon"), _tipPrefab.transform);
            Object.Destroy(iconPrefab.transform.Find("count-text").gameObject);
            Object.Destroy(iconPrefab.transform.Find("bg").gameObject);
            Object.Destroy(iconPrefab.transform.Find("inc").gameObject);
            Object.Destroy(iconPrefab.GetComponent<UIIconCountInc>());
            iconPrefab.SetActive(false);

            for (var i = 0; i < CarrierSlotCount; i++)
            {
                var iconObj = Object.Instantiate(iconPrefab, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);

                iconObj.name = "carrierIcon" + i;
                iconObj.GetComponent<Image>().sprite = LogisticsExtraItemSprites[i];
                rectTrans = (RectTransform)iconObj.transform;
                rectTrans.localScale = new Vector3(0.7f, 0.7f, 1f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(0f, -180f, 0);
                iconObj.SetActive(true);

                var countText = Object.Instantiate(infoText, Vector3.zero, Quaternion.identity, iconObj.transform);
                Object.Destroy(countText.GetComponent<Shadow>());
                countText.name = "carrierTotalCountText";
                var text = countText.GetComponent<Text>();
                text.fontSize = 22;
                text.text = "100";
                text.alignment = TextAnchor.MiddleRight;
                text.color = Color.white;
                rectTrans = (RectTransform)countText.transform;
                rectTrans.sizeDelta = new Vector2(40f, 30f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.localPosition = new Vector3(0f, -18f, 0f);

                if (i >= CarrierSlotCount - 1) continue;

                countText = Object.Instantiate(infoText, Vector3.zero, Quaternion.identity, iconObj.transform);
                Object.Destroy(countText.GetComponent<Shadow>());

                countText.name = "carrierIdleCountText";
                text = countText.GetComponent<Text>();
                text.fontSize = 22;
                text.text = "100";
                text.alignment = TextAnchor.MiddleRight;
                text.color = Color.white;
                rectTrans = (RectTransform)countText.transform;
                rectTrans.sizeDelta = new Vector2(40f, 30f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.localPosition = new Vector3(0f, 8f, 0f);
            }

            tipIconPrefab.gameObject.SetActive(false);
            _tipPrefab.SetActive(false);
            _stationTipsRoot.SetActive(false);

            StateSprite[0] = Util.LoadEmbeddedSprite("assets/icon/keep.png");
            StateSprite[1] = Util.LoadEmbeddedSprite("assets/icon/out.png");
            StateSprite[2] = Util.LoadEmbeddedSprite("assets/icon/in.png");
        }

        private static void RecycleStationTips()
        {
            foreach (var stationTip in _stationTips)
            {
                if (!stationTip) continue;
                if (_stationTipsRecycleCount < 128)
                {
                    stationTip.ResetStationTip();
                    stationTip.gameObject.SetActive(false);
                    StationTipsRecycle[_stationTipsRecycleCount] = stationTip;
                    _stationTipsRecycleCount++;
                }
                else
                {
                    Object.Destroy(stationTip);
                }
            }
            _stationTips = new StationTip[16];
        }

        private static void RecycleStationTip(int index)
        {
            var stationTip = _stationTips[index];
            if (!stationTip) return;
            if (_stationTipsRecycleCount < 128)
            {
                stationTip.ResetStationTip();
                stationTip.gameObject.SetActive(false);
                StationTipsRecycle[_stationTipsRecycleCount++] = stationTip;
            }
            else
            {
                Object.Destroy(stationTip);
            }
            _stationTips[index] = null;
        }

        private static StationTip AllocateStationTip()
        {
            if (_stationTipsRecycleCount > 0)
            {
                _stationTipsRecycleCount--;
                var result = StationTipsRecycle[_stationTipsRecycleCount];
                StationTipsRecycle[_stationTipsRecycleCount] = null;
                return result;
            }

            var tempTip = Object.Instantiate(_tipPrefab, _stationTipsRoot.transform);
            var stationTip = tempTip.AddComponent<StationTip>();
            stationTip.InitStationTip();
            return stationTip;
        }

        public static void StationInfoPanelsUpdate()
        {
            if (DSPGame.IsMenuDemo || !GameMain.isRunning) return;
            var localPlanet = GameMain.data?.localPlanet;
            if (localPlanet == null || !localPlanet.factoryLoaded)
            {
                _stationTipsRoot.SetActive(false);
                if (_lastPlanetId == 0) return;
                RecycleStationTips();
                _lastPlanetId = 0;
                return;
            }

            if (_lastPlanetId != localPlanet.id)
            {
                RecycleStationTips();
                _lastPlanetId = localPlanet.id;
            }

            var factory = localPlanet.factory;
            var transport = factory?.transport;
            if (transport is not { stationCursor: > 1 } || (UIGame.viewMode != EViewMode.Normal && UIGame.viewMode != EViewMode.Globe))
            {
                _stationTipsRoot.SetActive(false);
                return;
            }

            _stationTipsRoot.SetActive(true);
            if (UpdateStorageMax())
            {
                foreach (var tip in _stationTips)
                {
                    tip?.ResetStorageSlider();
                }
            }

            var localPosition = GameCamera.main.transform.localPosition;
            var forward = GameCamera.main.transform.forward;
            var realRadius = localPlanet.realRadius;

            var stationCount = transport.stationCursor;
            if (stationCount > _stationTips.Length)
            {
                var newSize = stationCount - 1;
                newSize |= newSize >> 1;
                newSize |= newSize >> 2;
                newSize |= newSize >> 4;
                newSize |= newSize >> 8;
                newSize |= newSize >> 16;
                newSize++;
                Array.Resize(ref _stationTips, newSize);
            }

            for (var i = stationCount - 1; i > 0; i--)
            {
                var stationComponent = transport.stationPool[i];
                if (stationComponent == null || i != stationComponent.id)
                {
                    RecycleStationTip(i);
                    continue;
                }
                var storageArray = stationComponent.storage;
                if (storageArray == null)
                {
                    RecycleStationTip(i);
                    continue;
                }

                var stationTip = _stationTips[i];
                if (!stationTip)
                {
                    stationTip = AllocateStationTip();
                    _stationTips[i] = stationTip;
                }

                var position = factory.entityPool[stationComponent.entityId].pos.normalized;
                var radius = realRadius;
                if (stationComponent.isCollector)
                {
                    radius += 35f;
                }
                else if (stationComponent.isStellar)
                {
                    radius += 20f;
                }
                else if (stationComponent.isVeinCollector)
                {
                    radius += 8f;
                }
                else
                {
                    radius += 15f;
                }

                position *= radius;

                var vec = position - localPosition;
                var magnitude = vec.magnitude;
                if (magnitude < 1.0
                    || Vector3.Dot(forward, vec) < 1.0
                    || !UIRoot.ScreenPointIntoRect(GameCamera.main.WorldToScreenPoint(position), (RectTransform)_stationTipsRoot.transform, out var rectPoint)
                    || rectPoint.x is < -4096f or > 4096f
                    || rectPoint.y is < -4096f or > 4096f
                    || Phys.RayCastSphere(localPosition, vec / magnitude, magnitude, Vector3.zero, realRadius, out _)
                    || storageArray.Select(x => x.itemId).All(x => x == 0))
                {
                    stationTip.gameObject.SetActive(false);
                    continue;
                }

                stationTip.gameObject.SetActive(true);

                var localScaleMultiple = magnitude switch
                {
                    < 50f => 1.5f,
                    < 250f => 1.75f - magnitude * 0.005f,
                    _ => 0.5f
                };
                /*
                rectPoint.x = Mathf.Round(rectPoint.x);
                rectPoint.y = Mathf.Round(rectPoint.y);
                */
                stationTip.rectTransform.anchoredPosition = rectPoint;
                stationTip.transform.localScale = Vector3.one * localScaleMultiple;

                stationTip.UpdateStationInfo(stationComponent);
            }
        }

        public class StationTip : MonoBehaviour
        {
            [FormerlySerializedAs("RectTransform")]
            public RectTransform rectTransform;

            private Transform[] _icons;
            private Transform[] _iconLocals;
            private Transform[] _iconRemotes;
            private Transform[] _countTexts;
            private Transform[] _sliderBg;
            private Transform[] _sliderMax;
            private Transform[] _sliderCurrent;
            private Transform[] _sliderOrdered;
            private Image[] _sliderOrderedImage;

            private Image[] _iconsImage;
            private Image[] _iconLocalsImage;
            private Image[] _iconRemotesImage;
            private Text[] _countTextsText;

            private Transform[] _carrierIcons;
            private Text[] _carrierTotalCountText;
            private Text[] _carrierIdleCountText;

            private GameObject _infoText;
            private EStationTipLayout _layout = EStationTipLayout.None;
            private int _storageNum;
            private float _pixelPerItem;

            private StorageItemData[] _storageItems;
            private static readonly Dictionary<int, Sprite> ItemSprites = [];
            private static readonly Color[] StateColor = [Color.gray, SupplyColor, DemandColor];

            private struct StorageItemData
            {
                public int ItemId;
                public int ItemCount;
                public int ItemOrdered;
                public int ItemMax;
                public ELogisticStorage LocalState;
                public ELogisticStorage RemoteState;
            }

            private enum EStationTipLayout
            {
                None,
                Collector,
                VeinCollector,
                PlanetaryLogistics,
                InterstellarLogistics
            }

            public void InitStationTip()
            {
                _storageItems = new StorageItemData[_storageMaxSlotCount];
                rectTransform = (RectTransform)transform;
                _icons = new Transform[_storageMaxSlotCount];
                _iconLocals = new Transform[_storageMaxSlotCount];
                _iconRemotes = new Transform[_storageMaxSlotCount];
                _iconsImage = new Image[_storageMaxSlotCount];
                _iconLocalsImage = new Image[_storageMaxSlotCount];
                _iconRemotesImage = new Image[_storageMaxSlotCount];
                _countTexts = new Transform[_storageMaxSlotCount];
                _countTextsText = new Text[_storageMaxSlotCount];
                _sliderBg = new Transform[_storageMaxSlotCount];
                _sliderMax = new Transform[_storageMaxSlotCount];
                _sliderCurrent = new Transform[_storageMaxSlotCount];
                _sliderOrdered = new Transform[_storageMaxSlotCount];
                _sliderOrderedImage = new Image[_storageMaxSlotCount];
                _carrierIcons = new Transform[3];
                _carrierTotalCountText = new Text[3];
                _carrierIdleCountText = new Text[2];

                _infoText = transform.Find("info-text").gameObject;
                for (var i = CarrierSlotCount - 1; i >= 0; i--)
                {
                    _carrierIcons[i] = transform.Find("carrierIcon" + i);
                    _carrierIcons[i].gameObject.SetActive(false);
                    _carrierTotalCountText[i] = _carrierIcons[i].Find("carrierTotalCountText").GetComponent<Text>();
                    if (i >= CarrierSlotCount - 1) continue;
                    _carrierIdleCountText[i] = _carrierIcons[i].Find("carrierIdleCountText").GetComponent<Text>();
                }

                for (var i = _storageMaxSlotCount - 1; i >= 0; i--)
                {
                    _countTexts[i] = transform.Find("countText" + i);
                    _countTextsText[i] = _countTexts[i].GetComponent<Text>();
                    _sliderBg[i] = transform.Find("sliderBg" + i);
                    _sliderMax[i] = _sliderBg[i].Find("max-fg");
                    _sliderCurrent[i] = _sliderBg[i].Find("current-fg");
                    _sliderOrdered[i] = _sliderBg[i].Find("ordered-fg");
                    _sliderOrderedImage[i] = _sliderOrdered[i].GetComponent<Image>();
                    _icons[i] = transform.Find("icon" + i);
                    _iconsImage[i] = _icons[i].GetComponent<Image>();
                    _iconLocals[i] = transform.Find("iconLocal" + i);
                    _iconRemotes[i] = transform.Find("iconRemote" + i);
                    _iconLocalsImage[i] = _iconLocals[i].GetComponent<Image>();
                    _iconRemotesImage[i] = _iconRemotes[i].GetComponent<Image>();
                    _countTexts[i].gameObject.SetActive(false);
                    _sliderBg[i].gameObject.SetActive(false);
                    _icons[i].gameObject.SetActive(false);
                    _iconLocals[i].gameObject.SetActive(false);
                    _iconRemotes[i].gameObject.SetActive(false);
                    _storageItems[i] = new StorageItemData
                    {
                        ItemId = -1,
                        ItemCount = -1,
                        ItemOrdered = -1,
                        ItemMax = -1,
                        LocalState = ELogisticStorage.None,
                        RemoteState = ELogisticStorage.None
                    };
                }

                _infoText.SetActive(false);
            }

            public void ResetStationTip()
            {
                _layout = EStationTipLayout.None;
                for (var i = _storageMaxSlotCount - 1; i >= 0; i--)
                {
                    _countTexts[i].gameObject.SetActive(false);
                    _sliderBg[i].gameObject.SetActive(false);
                    _icons[i].gameObject.SetActive(false);
                    _iconLocals[i].gameObject.SetActive(false);
                    _iconRemotes[i].gameObject.SetActive(false);
                    _countTextsText[i].color = StateColor[0];
                    _iconLocalsImage[i].color = StateColor[0];
                    _iconRemotesImage[i].color = StateColor[0];

                    ref var storageItem = ref _storageItems[i];
                    storageItem.ItemId = -1;
                    storageItem.ItemCount = -1;
                    storageItem.ItemOrdered = -1;
                    storageItem.ItemMax = -1;
                    storageItem.LocalState = ELogisticStorage.None;
                    storageItem.RemoteState = ELogisticStorage.None;
                }

                for (var i = CarrierSlotCount - 1; i >= 0; i--)
                {
                    _carrierIcons[i].gameObject.SetActive(false);
                }
            }

            public void ResetStorageSlider()
            {
                for (var i = _storageMaxSlotCount - 1; i >= 0; i--)
                {
                    ref var storageItem = ref _storageItems[i];
                    storageItem.ItemId = -1;
                    storageItem.ItemCount = -1;
                    storageItem.ItemOrdered = -1;
                    storageItem.ItemMax = -1;
                }
                _pixelPerItem = _layout == EStationTipLayout.InterstellarLogistics ? _remoteStoragePixelPerItem : _localStoragePixelPerItem;
            }

            private static Sprite GetItemSprite(int itemId)
            {
                if (ItemSprites.TryGetValue(itemId, out var sprite))
                    return sprite;
                sprite = LDB.items.Select(itemId)?.iconSprite;
                ItemSprites[itemId] = sprite;
                return sprite;
            }

            public void SetItem(int i, StationStore storage, bool barEnabled)
            {
                ref var storageState = ref _storageItems[i];
                var countUIText = _countTextsText[i];
                var itemId = storage.itemId;
                if (itemId != storageState.ItemId)
                {
                    var icon = _icons[i];
                    storageState.ItemId = itemId;
                    if (itemId <= 0)
                    {
                        icon.gameObject.SetActive(false);
                        _iconLocals[i].gameObject.SetActive(false);
                        _iconRemotes[i].gameObject.SetActive(false);
                        _sliderBg[i].gameObject.SetActive(false);

                        countUIText.color = StateColor[0];
                        countUIText.text = "—  ";
                        return;
                    }
                    storage.count = -1;
                    icon.gameObject.SetActive(true);
                    _iconsImage[i].sprite = GetItemSprite(itemId);
                    _iconLocals[i].gameObject.SetActive(CarrierEnabled[(int)_layout][0]);
                    _iconLocalsImage[i].sprite = StateSprite[(int)storageState.LocalState];
                    _iconRemotes[i].gameObject.SetActive(CarrierEnabled[(int)_layout][1]);
                    _iconRemotesImage[i].sprite = StateSprite[(int)storageState.RemoteState];
                    _sliderBg[i].gameObject.SetActive(barEnabled);
                    switch (_layout)
                    {
                        case EStationTipLayout.InterstellarLogistics:
                            {
                                countUIText.color = _iconRemotesImage[i].color = StateColor[(int)storageState.RemoteState];
                                break;
                            }
                        case EStationTipLayout.VeinCollector:
                        case EStationTipLayout.PlanetaryLogistics:
                            {
                                countUIText.color = _iconLocalsImage[i].color = StateColor[(int)storageState.LocalState];
                                break;
                            }
                        case EStationTipLayout.None:
                        case EStationTipLayout.Collector:
                        default:
                            break;
                    }
                }
                else if (itemId <= 0) return;

                var itemCount = storage.count;
                var itemLimit = _layout == EStationTipLayout.InterstellarLogistics ? _remoteStorageMaxTotal : _localStorageMaxTotal;
                var barPositionChanged = false;
                if (storageState.ItemCount != itemCount)
                {
                    storageState.ItemCount = itemCount;
                    countUIText.text = itemCount.ToString();
                    if (itemCount > itemLimit) itemCount = itemLimit;
                    if (barEnabled)
                    {
                        if (itemCount == 0)
                        {
                            _sliderCurrent[i].gameObject.SetActive(false);
                        }
                        else
                        {
                            ((RectTransform)_sliderCurrent[i].transform).sizeDelta = new Vector2(
                                _pixelPerItem * itemCount,
                                StorageSliderHeight
                            );
                            _sliderCurrent[i].gameObject.SetActive(true);
                        }
                        barPositionChanged = true;
                    }
                }
                else
                {
                    if (itemCount > itemLimit) itemCount = itemLimit;
                }

                if (barEnabled)
                {
                    var itemOrdered = storage.totalOrdered;
                    if (storageState.ItemOrdered != itemOrdered)
                    {
                        storageState.ItemOrdered = itemOrdered;
                        barPositionChanged = true;
                    }
                    if (barPositionChanged)
                    {
                        switch (itemOrdered)
                        {
                            case > 0:
                                if (itemOrdered + itemCount > itemLimit) itemOrdered = itemLimit - itemCount;
                                _sliderOrderedImage[i].color = OrderInColor;
                                var rectTrans = (RectTransform)_sliderOrdered[i].transform;
                                rectTrans.localPosition = new Vector3(
                                    _pixelPerItem * itemCount,
                                    0f, 0f
                                );
                                rectTrans.sizeDelta = new Vector2(
                                    _pixelPerItem * itemOrdered + 0.49f,
                                    StorageSliderHeight
                                );
                                break;
                            case < 0:
                                if (itemOrdered + itemCount < 0) itemOrdered = -itemCount;
                                _sliderOrderedImage[i].color = OrderOutColor;
                                rectTrans = (RectTransform)_sliderOrdered[i].transform;
                                rectTrans.localPosition = new Vector3(
                                    _pixelPerItem * (itemCount + itemOrdered),
                                    0f, 0f
                                );
                                rectTrans.sizeDelta = new Vector2(
                                    _pixelPerItem * -itemOrdered + 0.49f,
                                    StorageSliderHeight
                                );
                                break;
                        }

                        _sliderOrdered[i].gameObject.SetActive(itemOrdered != 0);
                    }

                    var itemMax = storage.max;
                    if (storageState.ItemMax != itemMax)
                    {
                        storageState.ItemMax = itemMax;
                        _sliderBg[i].gameObject.SetActive(itemMax > 0);
                        if (itemMax > itemLimit) itemMax = itemLimit;
                        ((RectTransform)_sliderMax[i].transform).sizeDelta = new Vector2(
                            _pixelPerItem * itemMax,
                            StorageSliderHeight
                        );
                    }
                }

                switch (_layout)
                {
                    case EStationTipLayout.InterstellarLogistics:
                        {
                            var localLogic = storage.localLogic;
                            if (storageState.LocalState != localLogic)
                            {
                                storageState.LocalState = localLogic;
                                var iconLocalImage = _iconLocalsImage[i];
                                iconLocalImage.sprite = StateSprite[(int)localLogic];
                                iconLocalImage.color = StateColor[(int)localLogic];
                            }
                            var remoteLogic = storage.remoteLogic;
                            if (storageState.RemoteState != remoteLogic)
                            {
                                storageState.RemoteState = remoteLogic;
                                var iconRemoteImage = _iconRemotesImage[i];
                                iconRemoteImage.sprite = StateSprite[(int)remoteLogic];
                                countUIText.color = iconRemoteImage.color = StateColor[(int)remoteLogic];
                            }

                            break;
                        }
                    case EStationTipLayout.VeinCollector:
                    case EStationTipLayout.PlanetaryLogistics:
                        {
                            var localLogic = storage.localLogic;
                            if (storageState.LocalState != localLogic)
                            {
                                storageState.LocalState = localLogic;
                                var iconLocalImage = _iconLocalsImage[i];
                                iconLocalImage.sprite = StateSprite[(int)localLogic];
                                countUIText.color = iconLocalImage.color = StateColor[(int)localLogic];
                            }
                            break;
                        }
                    case EStationTipLayout.None:
                    case EStationTipLayout.Collector:
                    default:
                        break;
                }
            }

            private static readonly bool[][] CarrierEnabled = [
                [false, false, false],
                [false, false, false],
                [false, false, false],
                [true, false, false],
                [true, true, true],
            ];

            private static readonly float[] TipWindowWidths = [0f, 100f, 120f, 120f, 120f];
            private static readonly float[] TipWindowExtraHeights = [0f, 5f, 5f, 40f, 40f];
            private static readonly float[] CarrierPositionX = [5f, 35f, 85f];

            public void UpdateStationInfo(StationComponent stationComponent)
            {
                var layout = stationComponent.isCollector ? EStationTipLayout.Collector :
                    stationComponent.isVeinCollector ? EStationTipLayout.VeinCollector :
                    stationComponent.isStellar ? EStationTipLayout.InterstellarLogistics : EStationTipLayout.PlanetaryLogistics;

                if (_layout != layout)
                {
                    _layout = layout;
                    for (var i = _storageMaxSlotCount - 1; i >= 0; i--)
                    {
                        _iconLocals[i].gameObject.SetActive(false);
                        _iconRemotes[i].gameObject.SetActive(false);
                        _icons[i].gameObject.SetActive(false);
                        switch (layout)
                        {
                            case EStationTipLayout.PlanetaryLogistics:
                                var rectTrans = (RectTransform)_iconLocals[i].transform;
                                rectTrans.sizeDelta = new Vector2(20f, 20f);
                                rectTrans.anchoredPosition3D = new Vector3(100f, -5f - 35f * i - 5f, 0);
                                break;
                            case EStationTipLayout.InterstellarLogistics:
                                rectTrans = (RectTransform)_iconLocals[i].transform;
                                rectTrans.sizeDelta = new Vector2(16f, 16f);
                                rectTrans.anchoredPosition3D = new Vector3(102f, -5f - 35f * i, 0);
                                break;
                        }
                    }
                    for (var i = _storageNum; i < _storageMaxSlotCount; i++)
                    {
                        _iconLocals[i].gameObject.SetActive(false);
                        _iconRemotes[i].gameObject.SetActive(false);
                        _icons[i].gameObject.SetActive(false);
                        _countTexts[i].gameObject.SetActive(false);
                        _sliderBg[i].gameObject.SetActive(false);
                    }

                    _storageNum = Math.Min(_storageMaxSlotCount, stationComponent.storage.Length);
                    rectTransform.sizeDelta = new Vector2(TipWindowWidths[(int)layout], TipWindowExtraHeights[(int)layout] + 35f * _storageNum);
                    for (var i = _storageMaxSlotCount - 1; i >= 0; i--)
                    {
                        _countTexts[i].gameObject.SetActive(i < _storageNum);
                    }

                    for (var i = CarrierSlotCount - 1; i >= 0; i--)
                    {
                        var active = CarrierEnabled[(int)layout][i];
                        _carrierIcons[i].gameObject.SetActive(active);
                        if (!active) continue;
                        var rectTrans = (RectTransform)_carrierIcons[i].transform;
                        rectTrans.anchoredPosition3D = new Vector3(CarrierPositionX[i], -5f - 35f * _storageNum, 0);
                    }

                    _pixelPerItem = _layout == EStationTipLayout.InterstellarLogistics ? _remoteStoragePixelPerItem : _localStoragePixelPerItem;
                }

                if (_storageNum > 0)
                {
                    var storageArray = stationComponent.storage;
                    var barEnabled = RealtimeLogisticsInfoPanelBarsEnabled.Value;
                    for (var j = _storageNum - 1; j >= 0; j--)
                    {
                        var storage = storageArray[j];
                        SetItem(j, storage, barEnabled);
                    }
                }

                switch (_layout)
                {
                    case EStationTipLayout.PlanetaryLogistics:
                        var totalCount = stationComponent.idleDroneCount + stationComponent.workDroneCount;
                        var currentCount = stationComponent.idleDroneCount;
                        _carrierIdleCountText[0].text = currentCount.ToString();
                        _carrierTotalCountText[0].text = totalCount.ToString();
                        break;
                    case EStationTipLayout.InterstellarLogistics:
                        totalCount = stationComponent.idleDroneCount + stationComponent.workDroneCount;
                        currentCount = stationComponent.idleDroneCount;
                        _carrierIdleCountText[0].text = currentCount.ToString();
                        _carrierTotalCountText[0].text = totalCount.ToString();
                        totalCount = stationComponent.idleShipCount + stationComponent.workShipCount;
                        currentCount = stationComponent.idleShipCount;
                        _carrierIdleCountText[1].text = currentCount.ToString();
                        _carrierTotalCountText[1].text = totalCount.ToString();
                        currentCount = stationComponent.warperCount;
                        _carrierTotalCountText[2].text = currentCount.ToString();
                        break;
                    case EStationTipLayout.None:
                    case EStationTipLayout.Collector:
                    case EStationTipLayout.VeinCollector:
                    default:
                        break;
                }
            }

            public void SetBarsVisible(bool on)
            {
                for (var i = _storageNum - 1; i >= 0; i--)
                {
                    _sliderBg[i].gameObject.SetActive(on && _storageItems[i].ItemId > 0);
                }
                for (var i = _storageNum; i < _storageMaxSlotCount; i++)
                {
                    _sliderBg[i].gameObject.SetActive(false);
                }
            }
        }
    }
}