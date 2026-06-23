using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using Object = UnityEngine.Object;

namespace UXAssist.Patches.Logistics;

internal static class RealtimeInfoPanelPatch
{
    public static void OnUpdate()
    {
        if (LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.Value)
        {
            RealtimeLogisticsInfoPanel.StationInfoPanelsUpdate();
        }
    }
}

internal class LogisticsConstrolPanelImprovement : PatchImpl<LogisticsConstrolPanelImprovement>
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
    // Harmony transpiler: UIGame_On_I_Switch_Transpiler
    // Target: UIGame.On_I_Switch
    // Fallback: None — patch will fail loudly if the target method body changes.
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
        matcher.Labels = [];
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

    private static readonly Dictionary<UIControlPanelStationEntry, Action<int>[]> OnStationEntryItemIconRightClickActionsMap = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIControlPanelStationEntry), nameof(UIControlPanelStationEntry._OnRegEvent))]
    private static void UIControlPanelStationEntry__OnRegEvent_Postfix(UIControlPanelStationEntry __instance)
    {
        var actions = new Action<int>[5];
        actions[0] = _ => OnStationEntryItemIconRightClick(__instance, 0);
        actions[1] = _ => OnStationEntryItemIconRightClick(__instance, 1);
        actions[2] = _ => OnStationEntryItemIconRightClick(__instance, 2);
        actions[3] = _ => OnStationEntryItemIconRightClick(__instance, 3);
        actions[4] = _ => OnStationEntryItemIconRightClick(__instance, 4);
        OnStationEntryItemIconRightClickActionsMap[__instance] = actions;
        __instance.storageItem0.itemButton.onRightClick += actions[0];
        __instance.storageItem1.itemButton.onRightClick += actions[1];
        __instance.storageItem2.itemButton.onRightClick += actions[2];
        __instance.storageItem3.itemButton.onRightClick += actions[3];
        __instance.storageItem4.itemButton.onRightClick += actions[4];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIControlPanelStationEntry), nameof(UIControlPanelStationEntry._OnUnregEvent))]
    private static void UIControlPanelStationEntry__OnUnregEvent_Postfix(UIControlPanelStationEntry __instance)
    {
        if (!OnStationEntryItemIconRightClickActionsMap.TryGetValue(__instance, out var actions)) return;
        __instance.storageItem0.itemButton.onRightClick -= actions[0];
        __instance.storageItem1.itemButton.onRightClick -= actions[1];
        __instance.storageItem2.itemButton.onRightClick -= actions[2];
        __instance.storageItem3.itemButton.onRightClick -= actions[3];
        __instance.storageItem4.itemButton.onRightClick -= actions[4];
        OnStationEntryItemIconRightClickActionsMap.Remove(__instance);
    }
}

internal static class RealtimeLogisticsInfoPanel
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

    private static int _localStorageMax = LogisticsConstants.DefaultLocalStorageMax;
    private static int _remoteStorageMax = LogisticsConstants.DefaultRemoteStorageMax;
    private static int _localStorageExtra;
    private static int _remoteStorageExtra;
    private static int _localStorageMaxTotal = _localStorageMax;
    private static int _remoteStorageMaxTotal = _remoteStorageMax;
    private static float _localStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _localStorageMaxTotal;
    private static float _remoteStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _remoteStorageMaxTotal;

    private static int _storageMaxSlotCount = LogisticsConstants.DefaultStorageSlotCount;
    private const int CarrierSlotCount = 3;


    private static bool UpdateStorageMax()
    {
        var history = GameMain.history;
        if (history == null) return false;
        if (_remoteStorageExtra == history.remoteStationExtraStorage && _localStorageExtra == history.localStationExtraStorage) return false;
        _localStorageExtra = history.localStationExtraStorage;
        _remoteStorageExtra = history.remoteStationExtraStorage;
        _localStorageMaxTotal = _localStorageMax + _localStorageExtra;
        _remoteStorageMaxTotal = _remoteStorageMax + _remoteStorageExtra;
        _localStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _localStorageMaxTotal;
        _remoteStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _remoteStorageMaxTotal;
        return true;
    }

    private static readonly Sprite[] LogisticsExtraItemSprites =
    [
        Resources.Load<Sprite>("Icons/ItemRecipe/logistic-drone"),
        Resources.Load<Sprite>("Icons/ItemRecipe/logistic-vessel"),
        Resources.Load<Sprite>("Icons/ItemRecipe/space-warper")
    ];

    internal static void Enable(bool on)
    {
        // Toggle the defensive localPlanet setter hook regardless of whether the GUI
        // root has been initialized yet (InitGUI may run later via OnDataLoaded).
        LocalPlanetWatcher.Enable(on);
        if (_stationTipsRoot == null) return;
        if (!on)
        {
            HideAndRecycleStationTips();
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

    internal static void EnableBars(bool on)
    {
        foreach (var stationTip in _stationTips)
        {
            if (stationTip == null) continue;
            stationTip.SetBarsVisible(on);
        }
    }

    internal static void OnGameBegin()
    {
        HideAndRecycleStationTips();
    }

    internal static void OnGameEnd()
    {
        HideAndRecycleStationTips();
    }

    internal static void OnDataLoaded()
    {
        _storageMaxSlotCount = LogisticsConstants.DefaultStorageSlotCount;
        _localStorageMax = LogisticsConstants.DefaultLocalStorageMax;
        _remoteStorageMax = LogisticsConstants.DefaultRemoteStorageMax;
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
                        _remoteStorageMax = Math.Max(_remoteStorageMax, prefabDesc.stationMaxItemCount);
                    }
                }
                else
                {
                    if (!prefabDesc.isVeinCollector)
                    {
                        _localStorageMax = Math.Max(_localStorageMax, prefabDesc.stationMaxItemCount);
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

    internal static void InitGUI()
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
        if (sliderBgPrefab == null)
        {
            UXAssist.Logger.LogWarning("RealtimeLogisticsInfoPanel.InitGUI: slider-bg prefab not found, aborting GUI init");
            Object.Destroy(_stationTipsRoot);
            _stationTipsRoot = null;
            return;
        }

        var veinTipPrefabGo = GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs/Vein Marks/vein-tips/vein-tip-prefab");
        if (veinTipPrefabGo == null)
        {
            UXAssist.Logger.LogWarning("RealtimeLogisticsInfoPanel.InitGUI: vein-tip-prefab not found, aborting GUI init");
            Object.Destroy(_stationTipsRoot);
            _stationTipsRoot = null;
            return;
        }
        var keyTipPrefabGo = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Key Tips/tip-prefab");
        if (keyTipPrefabGo == null)
        {
            UXAssist.Logger.LogWarning("RealtimeLogisticsInfoPanel.InitGUI: key tip-prefab not found, aborting GUI init");
            Object.Destroy(_stationTipsRoot);
            _stationTipsRoot = null;
            return;
        }

        _tipPrefab = Object.Instantiate(veinTipPrefabGo, _stationTipsRoot.transform);
        _tipPrefab.name = "tipPrefab";
        Object.Destroy(_tipPrefab.GetComponent<UIVeinDetailNode>());
        var image = _tipPrefab.GetComponent<Image>();
        image.sprite = keyTipPrefabGo.GetComponent<Image>().sprite;
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
            rectTrans.sizeDelta = new Vector2(LogisticsConstants.StorageSliderWidth, LogisticsConstants.StorageSliderHeight);
            rectTrans.anchorMax = new Vector2(0f, 1f);
            rectTrans.anchorMin = new Vector2(0f, 1f);
            rectTrans.pivot = new Vector2(0f, 1f);
            rectTrans.anchoredPosition3D = new Vector3(30f, y - 22f, 0f);
            rectTrans = (RectTransform)sliderBg.transform.Find("current-fg").transform;
            rectTrans.sizeDelta = new Vector2(0f, LogisticsConstants.StorageSliderHeight);
            rectTrans.anchorMax = new Vector2(0f, 1f);
            rectTrans.anchorMin = new Vector2(0f, 1f);
            rectTrans.pivot = new Vector2(0f, 1f);
            rectTrans.localPosition = new Vector3(0f, 0f, 0f);
            rectTrans = (RectTransform)sliderBg.transform.Find("ordered-fg").transform;
            rectTrans.sizeDelta = new Vector2(0f, LogisticsConstants.StorageSliderHeight);
            rectTrans.anchorMax = new Vector2(0f, 1f);
            rectTrans.anchorMin = new Vector2(0f, 1f);
            rectTrans.pivot = new Vector2(0f, 1f);
            rectTrans.localPosition = new Vector3(0f, 0f, 0f);
            rectTrans = (RectTransform)sliderBg.transform.Find("max-fg").transform;
            rectTrans.sizeDelta = new Vector2(LogisticsConstants.StorageSliderWidth, LogisticsConstants.StorageSliderHeight);
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

    private static void ReleaseStationTip(StationTip stationTip)
    {
        if (!stationTip) return;
        var go = stationTip.gameObject;
        if (_stationTipsRecycleCount < StationTipsRecycle.Length)
        {
            stationTip.ResetStationTip();
            go.SetActive(false);
            StationTipsRecycle[_stationTipsRecycleCount++] = stationTip;
        }
        else
        {
            // Recycle pool is full: destroy the cloned UI GameObject (not just the
            // StationTip MonoBehaviour). Destroying only the component would leave the
            // GameObject and all its UI children orphaned under _stationTipsRoot,
            // causing "ghost" tips to remain visible whenever the root is reactivated.
            go.SetActive(false);
            Object.Destroy(go);
        }
    }

    private static void RecycleStationTips()
    {
        foreach (var stationTip in _stationTips)
        {
            ReleaseStationTip(stationTip);
        }
        _stationTips = new StationTip[16];
    }

    private static void RecycleStationTip(int index)
    {
        var stationTip = _stationTips[index];
        if (!stationTip) return;
        ReleaseStationTip(stationTip);
        _stationTips[index] = null;
    }

    private static void HideAndRecycleStationTips()
    {
        _stationTipsRoot?.SetActive(false);
        RecycleStationTips();
        _lastPlanetId = 0;
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

    internal static void StationInfoPanelsUpdate()
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

    // Defensive Harmony hook on GameData.localPlanet setter.
    // It guarantees tips are hidden and recycled when the local planet id changes,
    // even if UXAssist.Update() is skipped that frame (e.g. by VFInput.inputing
    // while the player is typing while transitioning between planets, or by a brief
    // !GameMain.isRunning state during loading). _lastPlanetId is reset to 0 here so
    // the next StationInfoPanelsUpdate() re-validates factoryLoaded/transport/viewMode
    // and re-allocates fresh tips for the new planet.
    private class LocalPlanetWatcher : PatchImpl<LocalPlanetWatcher>
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.localPlanet), MethodType.Setter)]
        private static void GameData_localPlanet_Setter_Prefix(GameData __instance, PlanetData value)
        {
            var oldId = __instance.localPlanet?.id ?? 0;
            var newId = value?.id ?? 0;
            if (oldId == newId) return;
            HideAndRecycleStationTips();
        }
    }

    internal class StationTip : MonoBehaviour
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

        internal void InitStationTip()
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

        internal void ResetStationTip()
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

        internal void ResetStorageSlider()
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

        internal void SetItem(int i, StationStore storage, bool barEnabled)
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
                            LogisticsConstants.StorageSliderHeight
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
                                LogisticsConstants.StorageSliderHeight
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
                                LogisticsConstants.StorageSliderHeight
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
                        LogisticsConstants.StorageSliderHeight
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

        internal void UpdateStationInfo(StationComponent stationComponent)
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
                var barEnabled = LogisticsPatch.RealtimeLogisticsInfoPanelBarsEnabled.Value;
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

        internal void SetBarsVisible(bool on)
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
