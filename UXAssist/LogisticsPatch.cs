using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UXAssist;

public static class LogisticsPatch
{
    public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled;
    public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled;
    public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled;

    public static void Init()
    {
        LogisticsCapacityTweaksEnabled.SettingChanged += (_, _) => LogisticsCapacityTweaks.Enable(LogisticsCapacityTweaksEnabled.Value);
        AllowOverflowInLogisticsEnabled.SettingChanged += (_, _) => AllowOverflowInLogistics.Enable(AllowOverflowInLogisticsEnabled.Value);
        LogisticsConstrolPanelImprovementEnabled.SettingChanged += (_, _) => LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);
        LogisticsCapacityTweaks.Enable(LogisticsCapacityTweaksEnabled.Value);
        AllowOverflowInLogistics.Enable(AllowOverflowInLogisticsEnabled.Value);
        LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);
    }

    public static void Uninit()
    {
        LogisticsCapacityTweaks.Enable(false);
        AllowOverflowInLogistics.Enable(false);
        LogisticsConstrolPanelImprovement.Enable(false);
    }

    public static class LogisticsCapacityTweaks
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(LogisticsCapacityTweaks));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static KeyCode _lastKey = KeyCode.None;
        private static long _nextKeyTick;
        private static bool _skipNextEvent;

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
                var stationStorage = target.gameObject.GetComponentInParent<UIStationStorage>();
                if (stationStorage is null) continue;
                var station = stationStorage.station;
                ref var storage = ref station.storage[stationStorage.index];
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
                        var modelProto = LDB.models.Select(stationStorage.stationWindow.factory.entityPool[station.entityId].modelIndex);
                        itemCountMax = 0;
                        if (modelProto != null)
                        {
                            itemCountMax = modelProto.prefabDesc.stationMaxItemCount;
                        }

                        itemCountMax += station.isStellar ? GameMain.history.remoteStationExtraStorage : GameMain.history.localStationExtraStorage;
                    }

                    if (newMax > itemCountMax)
                    {
                        newMax = itemCountMax;
                    }
                }

                storage.max = newMax;
                _skipNextEvent = oldMax / 100 != newMax / 100;
                break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnMaxSliderValueChange))]
        private static bool UIStationStorage_OnMaxSliderValueChange_Prefix()
        {
            if (!_skipNextEvent) return true;
            _skipNextEvent = false;
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
                        var ratio = (maxCount + history.localStationExtraStorage) / oldMaxCount;
                        var storage = stationPool[i].storage;
                        for (var j = storage.Length - 1; j >= 0; j--)
                        {
                            if (storage[j].max + 10 < intOldMaxCount) continue;
                            storage[j].max = Mathf.RoundToInt((float)(storage[j].max * ratio / 50.0)) * 50;
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
                        var ratio = (maxCount + history.remoteStationExtraStorage) / oldMaxCount;
                        var storage = stationPool[i].storage;
                        for (var j = storage.Length - 1; j >= 0; j--)
                        {
                            if (storage[j].max + 10 < intOldMaxCount) continue;
                            storage[j].max = Mathf.RoundToInt((float)(storage[j].max * ratio / 100.0)) * 100;
                        }
                    }

                    break;
                }
            }

            return false;
        }
    }

    private static class AllowOverflowInLogistics
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(AllowOverflowInLogistics));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        // Do not check for overflow when try to send hand items into storages
        [HarmonyTranspiler]
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
            var labels = matcher.Labels;
            matcher.RemoveInstructions(9).Labels.AddRange(labels);
            return matcher.InstructionEnumeration();
        }
    }

    private static class LogisticsConstrolPanelImprovement
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(LogisticsConstrolPanelImprovement));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static int ItemIdHintUnderMouse()
        {
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
            var controlPanelWindow = UIRoot.instance?.uiGame?.controlPanelWindow;
            if (controlPanelWindow == null) return;
            var filterPanel = controlPanelWindow.filterPanel;
            if (filterPanel == null) return;
            if (!SetFilterItemId(filterPanel, itemId)) return;
            filterPanel.RefreshFilterUI();
            controlPanelWindow.DetermineFilterResults();
        }

        private static Action<int>[] _onStationEntryItemIconRightClickActions = new Action<int>[5];
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationEntry), nameof(UIControlPanelStationEntry._OnRegEvent))]
        private static void UIControlPanelStationEntry__OnRegEvent_Postfix(UIControlPanelStationEntry __instance)
        {
            _onStationEntryItemIconRightClickActions[0] = _ => OnStationEntryItemIconRightClick(__instance, 0);
            _onStationEntryItemIconRightClickActions[1] = _ => OnStationEntryItemIconRightClick(__instance, 1);
            _onStationEntryItemIconRightClickActions[2] = _ => OnStationEntryItemIconRightClick(__instance, 2);
            _onStationEntryItemIconRightClickActions[3] = _ => OnStationEntryItemIconRightClick(__instance, 3);
            _onStationEntryItemIconRightClickActions[4] = _ => OnStationEntryItemIconRightClick(__instance, 4);
            __instance.storageItem0.itemButton.onRightClick += _onStationEntryItemIconRightClickActions[0];
            __instance.storageItem1.itemButton.onRightClick += _onStationEntryItemIconRightClickActions[1];
            __instance.storageItem2.itemButton.onRightClick += _onStationEntryItemIconRightClickActions[2];
            __instance.storageItem3.itemButton.onRightClick += _onStationEntryItemIconRightClickActions[3];
            __instance.storageItem4.itemButton.onRightClick += _onStationEntryItemIconRightClickActions[4];
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationEntry), nameof(UIControlPanelStationEntry._OnUnregEvent))]
        private static void UIControlPanelStationEntry__OnUnregEvent_Postfix(UIControlPanelStationEntry __instance)
        {
            __instance.storageItem0.itemButton.onRightClick -= _onStationEntryItemIconRightClickActions[0];
            __instance.storageItem1.itemButton.onRightClick -= _onStationEntryItemIconRightClickActions[1];
            __instance.storageItem2.itemButton.onRightClick -= _onStationEntryItemIconRightClickActions[2];
            __instance.storageItem3.itemButton.onRightClick -= _onStationEntryItemIconRightClickActions[3];
            __instance.storageItem4.itemButton.onRightClick -= _onStationEntryItemIconRightClickActions[4];
            for (var i = 0; i < 5; i++)
            {
                _onStationEntryItemIconRightClickActions[i] = null;
            }
        }
    }
}