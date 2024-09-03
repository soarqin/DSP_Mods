using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UXAssist.Common;

namespace UXAssist;

public static class LogisticsPatch
{
    public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled;
    public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled;
    public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled;
    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelEnabled;

    public static void Init()
    {
        LogisticsCapacityTweaksEnabled.SettingChanged += (_, _) => LogisticsCapacityTweaks.Enable(LogisticsCapacityTweaksEnabled.Value);
        AllowOverflowInLogisticsEnabled.SettingChanged += (_, _) => AllowOverflowInLogistics.Enable(AllowOverflowInLogisticsEnabled.Value);
        LogisticsConstrolPanelImprovementEnabled.SettingChanged += (_, _) => LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);
        RealtimeLogisticsInfoPanelEnabled.SettingChanged += (_, _) => RealtimeLogisticsInfoPanel.Enable(RealtimeLogisticsInfoPanelEnabled.Value);
        LogisticsCapacityTweaks.Enable(LogisticsCapacityTweaksEnabled.Value);
        AllowOverflowInLogistics.Enable(AllowOverflowInLogisticsEnabled.Value);
        LogisticsConstrolPanelImprovement.Enable(LogisticsConstrolPanelImprovementEnabled.Value);

        GameLogic.OnGameBegin += RealtimeLogisticsInfoPanel.OnGameBegin;
    }

    public static void Uninit()
    {
        GameLogic.OnGameBegin -= RealtimeLogisticsInfoPanel.OnGameBegin;

        LogisticsCapacityTweaks.Enable(false);
        AllowOverflowInLogistics.Enable(false);
        LogisticsConstrolPanelImprovement.Enable(false);
        RealtimeLogisticsInfoPanel.Enable(false);
    }

    public static void Start()
    {
        RealtimeLogisticsInfoPanel.InitGUI();
    }

    public static void OnUpdate()
    {
        if (RealtimeLogisticsInfoPanelEnabled.Value)
        {
            RealtimeLogisticsInfoPanel.StationInfoPanelsUpdate();
        }
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
        private static int _stationTipsRecycleCount;
        private static GameObject _stationTipRoot;
        private static GameObject _tipPrefab;
        private static readonly Color DemandColor = new(223.7f / 255, 139.6f / 255, 94f / 255);
        private static readonly Color SupplyColor = new(90f / 255, 208.5f / 255, 249f / 255);

        private static PlanetData _lastPlanet;

        private static readonly Sprite[] LogisticsExtraItemSprites =
        [
            Resources.Load<Sprite>("Icons/ItemRecipe/logistic-drone"),
            Resources.Load<Sprite>("Icons/ItemRecipe/logistic-vessel"),
            Resources.Load<Sprite>("Icons/ItemRecipe/space-warper")
        ];

        public static void Enable(bool on)
        {
            _stationTipRoot?.SetActive(on);
        }

        public static void OnGameBegin()
        {
            _lastPlanet = null;
        }

        public static void InitGUI()
        {
            _stationTipRoot = UnityEngine.Object.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs/Vein Marks"),
                GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs").transform);
            _stationTipRoot.name = "stationTip";
            UnityEngine.Object.Destroy(_stationTipRoot.GetComponent<UIVeinDetail>());
            _tipPrefab = UnityEngine.Object.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs/Vein Marks/vein-tip-prefab"), _stationTipRoot.transform);
            _tipPrefab.name = "tipPrefab";
            var image = _tipPrefab.GetComponent<Image>();
            image.sprite = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Key Tips/tip-prefab").GetComponent<Image>().sprite;
            image.color = new Color(0, 0, 0, 0.8f);
            image.enabled = true;
            var rectTrans = (RectTransform)_tipPrefab.transform;
            rectTrans.localPosition = new Vector3(200f, 800f, 0);
            rectTrans.sizeDelta = new Vector2(150, 160f);
            rectTrans.pivot = new Vector2(0.5f, 0.5f);
            UnityEngine.Object.Destroy(_tipPrefab.GetComponent<UIVeinDetailNode>());
            var infoText = _tipPrefab.transform.Find("info-text").gameObject;

            for (var index = 0; index < 5; ++index)
            {
                var countText = UnityEngine.Object.Instantiate(infoText, Vector3.zero, Quaternion.identity, _tipPrefab.transform);
                countText.name = "countText" + index;
                var y = -5f - 35f * index;
                var text = countText.GetComponent<Text>();
                text.fontSize = 20;
                text.text = "99999";
                text.alignment = TextAnchor.MiddleRight;
                rectTrans = (RectTransform)countText.transform;
                rectTrans.sizeDelta = new Vector2(70f, 30f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(30f, y, 0);
                UnityEngine.Object.Destroy(countText.GetComponent<Shadow>());

                var iconTrans = _tipPrefab.transform.Find("icon");
                var itemIcon = UnityEngine.Object.Instantiate(iconTrans.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                itemIcon.name = "icon" + index;
                rectTrans = (RectTransform)itemIcon.transform;
                rectTrans.sizeDelta = new Vector2(30f, 30f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(0, y, 0);
                var stateLocal = UnityEngine.Object.Instantiate(iconTrans.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                stateLocal.name = "iconLocal" + index;
                stateLocal.GetComponent<Image>().material = null;
                rectTrans = (RectTransform)stateLocal.transform;
                rectTrans.sizeDelta = new Vector2(20f, 20f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(100f, y - 5f, 0);
                var stateRemote = UnityEngine.Object.Instantiate(iconTrans.gameObject, new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                stateRemote.name = "iconRemote" + index;
                stateRemote.GetComponent<Image>().material = null;
                rectTrans = (RectTransform)stateRemote.transform;
                rectTrans.sizeDelta = new Vector2(30f, 30f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(115f, y, 0);
            }

            for (var i = 0; i < 3; i++)
            {
                var iconObj = UnityEngine.Object.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Entity Briefs/brief-info-top/brief-info/content/icons/icon"),
                    new Vector3(0, 0, 0), Quaternion.identity, _tipPrefab.transform);
                UnityEngine.Object.Destroy(iconObj.transform.Find("count-text").gameObject);
                UnityEngine.Object.Destroy(iconObj.transform.Find("bg").gameObject);
                UnityEngine.Object.Destroy(iconObj.transform.Find("inc").gameObject);
                UnityEngine.Object.Destroy(iconObj.GetComponent<UIIconCountInc>());

                iconObj.name = "carrierIcon" + i;
                iconObj.GetComponent<Image>().sprite = LogisticsExtraItemSprites[i];
                rectTrans = (RectTransform)iconObj.transform;
                rectTrans.localScale = new Vector3(0.7f, 0.7f, 1f);
                rectTrans.anchorMax = new Vector2(0f, 1f);
                rectTrans.anchorMin = new Vector2(0f, 1f);
                rectTrans.pivot = new Vector2(0f, 1f);
                rectTrans.anchoredPosition3D = new Vector3(17f + i * 40f, -180f, 0);

                var countText = UnityEngine.Object.Instantiate(infoText, Vector3.zero, Quaternion.identity, iconObj.transform);
                UnityEngine.Object.Destroy(countText.GetComponent<Shadow>());
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

                if (i >= 2) continue;

                countText = UnityEngine.Object.Instantiate(infoText, Vector3.zero, Quaternion.identity, iconObj.transform);
                UnityEngine.Object.Destroy(countText.GetComponent<Shadow>());

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

            _tipPrefab.transform.Find("icon").gameObject.SetActive(false);
            _tipPrefab.SetActive(false);
        }

        public class StationTip : MonoBehaviour
        {
            [FormerlySerializedAs("RectTransform")]
            public RectTransform rectTransform;

            private Transform[] _icons;
            private Transform[] _iconLocals;
            private Transform[] _iconRemotes;
            private Transform[] _countTexts;

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
            private static readonly Dictionary<int, Sprite> ItemSprites = new();

            private static readonly Color[] StateColor = [Color.gray, SupplyColor, DemandColor];

            private static readonly Sprite[] StateSprite =
            [
                Util.LoadEmbeddedSprite("assets/icon/keep.png"),
                Util.LoadEmbeddedSprite("assets/icon/out.png"),
                Util.LoadEmbeddedSprite("assets/icon/in.png")
            ];

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
                rectTransform = (RectTransform)transform;
                _icons = new Transform[5];
                _iconLocals = new Transform[5];
                _iconRemotes = new Transform[5];
                _iconsImage = new Image[5];
                _iconLocalsImage = new Image[5];
                _iconRemotesImage = new Image[5];
                _countTexts = new Transform[5];
                _countTextsText = new Text[5];
                _carrierIcons = new Transform[3];
                _carrierTotalCountText = new Text[3];
                _carrierIdleCountText = new Text[2];

                _infoText = transform.Find("info-text").gameObject;
                for (var i = 0; i < 3; i++)
                {
                    _carrierIcons[i] = transform.Find("carrierIcon" + i);
                    _carrierTotalCountText[i] = _carrierIcons[i].Find("carrierTotalCountText").GetComponent<Text>();
                    if (i >= 2) continue;
                    _carrierIdleCountText[i] = _carrierIcons[i].Find("carrierIdleCountText").GetComponent<Text>();
                }

                for (var i = 0; i < 5; i++)
                {
                    _countTexts[i] = transform.Find("countText" + i);
                    _countTextsText[i] = _countTexts[i].GetComponent<Text>();
                    _icons[i] = transform.Find("icon" + i);
                    _iconsImage[i] = _icons[i].GetComponent<Image>();
                    _iconLocals[i] = transform.Find("iconLocal" + i);
                    _iconRemotes[i] = transform.Find("iconRemote" + i);
                    _iconLocalsImage[i] = _iconLocals[i].GetComponent<Image>();
                    _iconRemotesImage[i] = _iconRemotes[i].GetComponent<Image>();
                    _countTexts[i].gameObject.SetActive(false);
                    _icons[i].gameObject.SetActive(false);
                    _iconLocals[i].gameObject.SetActive(false);
                    _iconRemotes[i].gameObject.SetActive(false);
                }

                _infoText.SetActive(false);
            }

            private static Sprite GetItemSprite(int itemId)
            {
                if (ItemSprites.TryGetValue(itemId, out var sprite))
                    return sprite;
                sprite = LDB.items.Select(itemId)?.iconSprite;
                ItemSprites[itemId] = sprite;
                return sprite;
            }

            public void SetItem(int i, StationStore storage, bool isStellar, bool isCollector)
            {
                var itemId = storage.itemId;
                var icon = _icons[i];
                var iconLocal = _iconLocals[i];
                var iconRemote = _iconRemotes[i];
                var countUIText = _countTextsText[i];
                if (itemId <= 0)
                {
                    icon.gameObject.SetActive(false);
                    iconLocal.gameObject.SetActive(false);
                    iconRemote.gameObject.SetActive(false);
                    countUIText.color = Color.gray;
                    countUIText.text = "—  ";
                    return;
                }

                _iconsImage[i].sprite = GetItemSprite(itemId);
                icon.gameObject.SetActive(true);
                countUIText.text = storage.count.ToString(CultureInfo.CurrentCulture);
                if (isCollector) return;
                var iconLocalImage = _iconLocalsImage[i];
                var localLogic = storage.localLogic;
                iconLocalImage.sprite = StateSprite[(int)localLogic];
                iconLocalImage.color = StateColor[(int)localLogic];
                iconLocal.gameObject.SetActive(true);
                if (isStellar)
                {
                    var iconRemoteImage = _iconRemotesImage[i];
                    var remoteLogic = storage.remoteLogic;
                    iconRemoteImage.sprite = StateSprite[(int)remoteLogic];
                    iconRemoteImage.color = StateColor[(int)remoteLogic];
                    iconRemote.gameObject.SetActive(true);
                    countUIText.color = iconRemoteImage.color;
                }
                else
                {
                    iconRemote.gameObject.SetActive(false);
                    countUIText.color = iconLocalImage.color;
                }
            }

            public void UpdateStationInfo(StationComponent stationComponent, PlanetFactory factory)
            {
                var storageArray = stationComponent.storage;
                EStationTipLayout layout;
                if (stationComponent.isCollector)
                {
                    layout = EStationTipLayout.Collector;
                }
                else if (!stationComponent.isStellar)
                {
                    layout = stationComponent.isVeinCollector ? EStationTipLayout.VeinCollector : EStationTipLayout.PlanetaryLogistics;
                }
                else
                {
                    layout = EStationTipLayout.InterstellarLogistics;
                }

                if (layout != _layout)
                {
                    _layout = layout;
                    for (var i = 5 - 1; i >= 0; i--)
                    {
                        _countTexts[i].gameObject.SetActive(false);
                        _iconLocals[i].gameObject.SetActive(false);
                        _iconRemotes[i].gameObject.SetActive(false);
                        _icons[i].gameObject.SetActive(false);
                    }

                    for (var i = 2; i >= 0; i--)
                    {
                        _carrierIcons[i].gameObject.SetActive(false);
                        _carrierTotalCountText[i].text = "";
                        if (i < 2) _carrierIdleCountText[i].text = "";
                    }

                    var tipWindowWidth = 143f;
                    var tipWindowHeight = 5f;
                    switch (layout)
                    {
                        case EStationTipLayout.Collector:
                            _storageNum = 2;
                            tipWindowWidth = 100f;
                            _carrierIcons[0].gameObject.SetActive(false);
                            _carrierIcons[1].gameObject.SetActive(false);
                            _carrierIcons[2].gameObject.SetActive(false);
                            break;
                        case EStationTipLayout.VeinCollector:
                            _storageNum = 1;
                            tipWindowWidth = 120f;
                            _carrierIcons[0].gameObject.SetActive(false);
                            _carrierIcons[1].gameObject.SetActive(false);
                            _carrierIcons[2].gameObject.SetActive(false);
                            break;
                        case EStationTipLayout.PlanetaryLogistics:
                            _storageNum = Math.Min(storageArray.Length, 4);
                            tipWindowHeight += 35f;
                            tipWindowWidth = 120f;
                            _carrierIcons[0].gameObject.SetActive(true);
                            _carrierIcons[1].gameObject.SetActive(false);
                            _carrierIcons[2].gameObject.SetActive(false);
                            break;
                        case EStationTipLayout.InterstellarLogistics:
                            _storageNum = Math.Min(storageArray.Length, 5);
                            tipWindowHeight += 35f;
                            _carrierIcons[0].gameObject.SetActive(true);
                            _carrierIcons[1].gameObject.SetActive(true);
                            _carrierIcons[2].gameObject.SetActive(true);
                            break;
                    }

                    for (var i = _storageNum - 1; i >= 0; i--)
                    {
                        _countTexts[i].gameObject.SetActive(true);
                    }

                    rectTransform.sizeDelta = new Vector2(tipWindowWidth, tipWindowHeight + 35f * _storageNum);

                    for (var i = 0; i < 3; i++)
                    {
                        var rectTrans = (RectTransform)_carrierIcons[i].transform;
                        rectTrans.anchoredPosition3D = new Vector3(rectTrans.anchoredPosition3D.x, -5f - 35f * _storageNum, 0);
                    }
                }

                var isStellar = stationComponent.isStellar;
                var isCollector = stationComponent.isCollector;
                for (var j = _storageNum - 1; j >= 0; j--)
                {
                    var storage = storageArray[j];
                    SetItem(j, storage, isStellar, isCollector);
                }

                int currentCount, totalCount;
                switch (layout)
                {
                    case EStationTipLayout.PlanetaryLogistics:
                        totalCount = stationComponent.idleDroneCount + stationComponent.workDroneCount;
                        currentCount = stationComponent.idleDroneCount;
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
                }
            }
        }

        private static void RecycleStationTips()
        {
            foreach (var stationTip in _stationTips)
            {
                if (!stationTip) continue;
                stationTip.gameObject.SetActive(false);
                if (_stationTipsRecycleCount < 128)
                    StationTipsRecycle[_stationTipsRecycleCount++] = stationTip;
            }

            Array.Clear(_stationTips, 0, _stationTips.Length);
        }

        private static StationTip AllocateStationTip()
        {
            if (_stationTipsRecycleCount > 0)
            {
                var result = StationTipsRecycle[--_stationTipsRecycleCount];
                StationTipsRecycle[_stationTipsRecycleCount] = null;
                return result;
            }

            var tempTip = UnityEngine.Object.Instantiate(_tipPrefab, _stationTipRoot.transform);
            var stationTip = tempTip.AddComponent<StationTip>();
            stationTip.InitStationTip();
            return stationTip;
        }

        public static void StationInfoPanelsUpdate()
        {
            var localPlanet = GameMain.localPlanet;
            if (localPlanet == null)
            {
                if (_lastPlanet == null) return;
                _lastPlanet = null;
                _stationTipRoot.SetActive(false);
                return;
            }

            if (_lastPlanet != localPlanet)
            {
                RecycleStationTips();
                _lastPlanet = localPlanet;
            }

            var factory = localPlanet.factory;
            var transport = factory?.transport;
            if (transport == null || transport.stationCursor == 0 || (UIGame.viewMode != EViewMode.Normal && UIGame.viewMode != EViewMode.Globe))
            {
                if (_stationTipRoot.activeSelf)
                {
                    _stationTipRoot.SetActive(false);
                }

                return;
            }

            _stationTipRoot.SetActive(true);
            var localPosition = GameCamera.main.transform.localPosition;
            var forward = GameCamera.main.transform.forward;
            var realRadius = localPlanet.realRadius;

            var stationCount = transport.stationCursor;
            if (stationCount >= _stationTips.Length)
            {
                var newSize = stationCount;
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
                var storageArray = stationComponent?.storage;
                if (storageArray == null)
                {
                    _stationTips[i]?.gameObject.SetActive(false);
                    continue;
                }

#if DEBUG
                if (i != stationComponent.id)
                {
                    UXAssist.Logger.LogWarning($"Station index mismatch: {i} != {stationComponent.id}");
                    _stationTips[i]?.gameObject.SetActive(false);
                    continue;
                }
#endif

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
                    || !UIRoot.ScreenPointIntoRect(GameCamera.main.WorldToScreenPoint(position), (RectTransform)_stationTipRoot.transform, out var rectPoint)
                    || rectPoint.x is < -4096f or > 4096f
                    || rectPoint.y is < -4096f or > 4096f
                    || Phys.RayCastSphere(localPosition, vec / magnitude, magnitude, Vector3.zero, realRadius, out _)
                    || storageArray.Select(x => x.itemId).All(x => x == 0))
                {
                    _stationTips[i]?.gameObject.SetActive(false);
                    continue;
                }

                var stationTip = _stationTips[i];
                if (!stationTip)
                {
                    stationTip = AllocateStationTip();
                    _stationTips[i] = stationTip;
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

                stationTip.UpdateStationInfo(stationComponent, factory);
            }
        }
    }
}