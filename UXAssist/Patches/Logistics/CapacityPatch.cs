using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Common.GameConstants;

namespace UXAssist.Patches.Logistics;

internal class LogisticsCapacityTweaks : PatchImpl<LogisticsCapacityTweaks>
{
    private static KeyCode _lastKey = KeyCode.None;
    private static long _nextKeyTick;
    private static bool _skipNextUIStationStorageEvent;
    private static bool _skipNextUIControlPanelStationStorageEvent;
    private static bool _refreshingUIStationStorage;
    private static bool _refreshingUIControlPanelStationStorage;

    private static bool UpdateKeyPressed(KeyCode code)
    {
        if (!Input.GetKey(code))
            return false;
        var main = GameMain.instance;
        if (main == null)
            return false;
        if (code != _lastKey)
        {
            _lastKey = code;
            _nextKeyTick = main.timei + LogisticsConstants.KeyRepeatInitialDelay;
            return true;
        }

        var currTick = main.timei;
        if (_nextKeyTick > currTick) return false;
        _nextKeyTick = currTick + LogisticsConstants.KeyRepeatInterval;
        return true;
    }

    internal static void UpdateInput()
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
                delta = -LogisticsConstants.MassiveAdjustment;
            else if (alt)
                delta = -LogisticsConstants.LargeAdjustment;
            else
                delta = -LogisticsConstants.SmallAdjustment;
        }
        else if (UpdateKeyPressed(KeyCode.RightArrow))
        {
            if (ctrl)
                delta = LogisticsConstants.MassiveAdjustment;
            else if (alt)
                delta = LogisticsConstants.LargeAdjustment;
            else
                delta = LogisticsConstants.SmallAdjustment;
        }
        else if (UpdateKeyPressed(KeyCode.DownArrow))
        {
            if (ctrl)
                delta = -LogisticsConstants.GargantuanAdjustment;
            else if (alt)
                delta = -LogisticsConstants.HugeAdjustment;
            else
                delta = -LogisticsConstants.MediumAdjustment;
        }
        else if (UpdateKeyPressed(KeyCode.UpArrow))
        {
            if (ctrl)
                delta = LogisticsConstants.GargantuanAdjustment;
            else if (alt)
                delta = LogisticsConstants.HugeAdjustment;
            else
                delta = LogisticsConstants.MediumAdjustment;
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
                if (LogisticsPatch.AllowOverflowInLogisticsEnabled.Value)
                {
                    itemCountMax = LogisticsConstants.OverflowStorageMax;
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
    [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
    private static void UIStationStorage_RefreshValues_Prefix()
    {
        _refreshingUIStationStorage = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
    private static void UIStationStorage_RefreshValues_Postfix()
    {
        _refreshingUIStationStorage = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), "RefreshValues")]
    private static void UIControlPanelStationStorage_RefreshValues_Prefix()
    {
        _refreshingUIControlPanelStationStorage = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), "RefreshValues")]
    private static void UIControlPanelStationStorage_RefreshValues_Postfix()
    {
        _refreshingUIControlPanelStationStorage = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnMaxSliderValueChange))]
    private static bool UIStationStorage_OnMaxSliderValueChange_Prefix()
    {
        if (!_refreshingUIStationStorage && !_skipNextUIStationStorageEvent) return true;
        _skipNextUIStationStorageEvent = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnMaxSliderValueChange))]
    private static bool UIControlPanelStationStorage_OnMaxSliderValueChange_Prefix()
    {
        if (!_refreshingUIControlPanelStationStorage && !_skipNextUIControlPanelStationStorageEvent) return true;
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
                        if (oldMaxCount < 1.0) continue;
                        var intOldMaxCount = (int)Math.Round(oldMaxCount);
                        var intNewMaxCount = maxCount + history.localStationExtraStorage;
                        var ratio = intNewMaxCount / oldMaxCount;
                        var storage = stationPool[i].storage;
                        for (var j = storage.Length - 1; j >= 0; j--)
                        {
                            var max = storage[j].max;
                            if (max + 10 < intOldMaxCount || max >= intNewMaxCount) continue;
                            storage[j].max = Mathf.RoundToInt((float)(max * ratio / LogisticsConstants.LocalStorageRounding)) * LogisticsConstants.LocalStorageRounding;
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
                        if (oldMaxCount < 1.0) continue;
                        var intOldMaxCount = (int)Math.Round(oldMaxCount);
                        var intNewMaxCount = maxCount + history.remoteStationExtraStorage;
                        var ratio = intNewMaxCount / oldMaxCount;
                        var storage = stationPool[i].storage;
                        for (var j = storage.Length - 1; j >= 0; j--)
                        {
                            var max = storage[j].max;
                            if (max + 10 < intOldMaxCount || max >= intNewMaxCount) continue;
                            storage[j].max = Mathf.RoundToInt((float)(max * ratio / LogisticsConstants.RemoteStorageRounding)) * LogisticsConstants.RemoteStorageRounding;
                        }
                    }

                    break;
                }
        }

        return false;
    }
}

internal class GreaterPowerUsageInLogistics : PatchImpl<GreaterPowerUsageInLogistics>
{
    protected override void OnEnable()
    {
        var window = UIRoot.instance?.uiGame?.stationWindow;
        if (window == null) return;
        window._Close();
        window.maxMiningSpeedSlider.maxValue = LogisticsConstants.MiningSpeedSliderMaxExtended;
    }

    protected override void OnDisable()
    {
        var window = UIRoot.instance?.uiGame?.stationWindow;
        if (window == null) return;
        window._Close();
        window.maxMiningSpeedSlider.maxValue = LogisticsConstants.MiningSpeedSliderMaxDefault;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnStationIdChange))]
    private static IEnumerable<CodeInstruction> UIStationWindow_OnStationIdChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Start().Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            Transpilers.EmitDelegate((UIStationWindow window) =>
            {
                window.maxMiningSpeedSlider.maxValue = LogisticsConstants.MiningSpeedSliderMaxExtended;
            })
        ).MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.maxChargePowerSlider))),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(LogisticsConstants.ChargePowerSliderScale)),
            new CodeMatch(OpCodes.Conv_I8)
        );
        var pos = matcher.Pos + 1;
        matcher.Advance(5).MatchForward(false,
            new CodeMatch(OpCodes.Conv_R4),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Slider), nameof(Slider.value)))
        );
        var pos2 = matcher.Pos + 2;
        matcher.Start().Advance(pos);
        var ldvar = matcher.InstructionAt(1).Clone();
        var locWorkEnergyPerTick = matcher.InstructionAt(-2).operand;
        matcher.RemoveInstructions(pos2 - pos).InsertAndAdvance(
            ldvar,
            new CodeInstruction(OpCodes.Ldloc_S, locWorkEnergyPerTick),
            Transpilers.EmitDelegate((UIStationWindow window, long maxWorkEnergy, long workEnergyPerTick) =>
            {
                var maxSliderValue = maxWorkEnergy / LogisticsConstants.ChargePowerSliderScale;
                window.maxChargePowerSlider.maxValue = maxSliderValue + 9;
                window.maxChargePowerSlider.minValue = maxWorkEnergy / LogisticsConstants.ChargePowerSliderMinScale;
                if (workEnergyPerTick <= maxWorkEnergy)
                    window.maxChargePowerSlider.Set(workEnergyPerTick / LogisticsConstants.ChargePowerSliderScale, false);
                else
                    window.maxChargePowerSlider.Set(maxSliderValue + (workEnergyPerTick - 1) / maxWorkEnergy + 1, false);
            })
        );

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.maxMiningSpeedSlider))),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.factorySystem))),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.minerPool))),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.minerId))),
            new CodeMatch(OpCodes.Ldelema, typeof(MinerComponent)),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MinerComponent), nameof(MinerComponent.speed)))
        );
        pos = matcher.Pos + 9;
        matcher.Advance(5).MatchForward(false,
            new CodeMatch(OpCodes.Conv_R4),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Slider), nameof(Slider.value)))
        );
        pos2 = matcher.Pos;
        matcher.Start().Advance(pos).RemoveInstructions(pos2 - pos).Insert(
            Transpilers.EmitDelegate((int speed) =>
            {
                if (speed <= LogisticsConstants.MaxMiningSpeedBase)
                    return (speed - LogisticsConstants.MinMiningSpeedBase) / LogisticsConstants.MiningSpeedFineStep;
                return (speed - LogisticsConstants.MaxMiningSpeedBase) / LogisticsConstants.MiningSpeedCoarseStep + LogisticsConstants.MiningSpeedSliderMaxDefault;
            })
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnMaxMiningSpeedChange))]
    private static IEnumerable<CodeInstruction> UIStationWindow_OnMaxMiningSpeedChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(LogisticsConstants.MinMiningSpeedBase)),
            new CodeMatch(OpCodes.Ldarg_1)
        );
        var pos = matcher.Pos;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Stloc_1)
        );
        var pos2 = matcher.Pos;
        matcher.Start().Advance(pos);
        var labels = matcher.Labels;
        matcher.RemoveInstructions(pos2 - pos);
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
            Transpilers.EmitDelegate((float value) =>
            {
                var intval = (int)(value + 0.5f);
                if (intval <= LogisticsConstants.MiningSpeedSliderMaxDefault)
                    return intval * LogisticsConstants.MiningSpeedFineStep + LogisticsConstants.MinMiningSpeedBase;
                return (intval - LogisticsConstants.MiningSpeedSliderMaxDefault) * LogisticsConstants.MiningSpeedCoarseStep + LogisticsConstants.MaxMiningSpeedBase;
            })
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnMaxChargePowerSliderValueChange))]
    private static IEnumerable<CodeInstruction> UIStationWindow_OnMaxChargePowerSliderValueChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.factory))),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.powerSystem))),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PowerSystem), nameof(PowerSystem.consumerPool)))
        );
        var labels = matcher.Labels;
        matcher.Labels = null;
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldarg_1),
            Transpilers.EmitDelegate((UIStationWindow window, float value) =>
            {
                float prevMax = window.workEnergyPrefab * 5L / 50000L;
                if (value <= prevMax)
                {
                    return value;
                }

                return prevMax * (value - prevMax + 1);
            }),
            new CodeInstruction(OpCodes.Starg_S, 1)
        );
        return matcher.InstructionEnumeration();
    }
}
