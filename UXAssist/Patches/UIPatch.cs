namespace UXAssist.Patches;

using System.Linq;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Configuration;
using Common;
using GameLogicProc = Common.GameLogic;

[PatchGuid(PluginInfo.PLUGIN_GUID)]
public class UIPatch : PatchImpl<UIPatch>
{
    public static ConfigEntry<bool> PlanetVeinUtilizationEnabled;

    public static void Init()
    {
        PlanetVeinUtilizationEnabled.SettingChanged += (_, _) => PlanetVeinUtilization.Enable(PlanetVeinUtilizationEnabled.Value);
    }

    public static void Start()
    {
        GameLogicProc.OnGameBegin += PlanetVeinUtilization.OnGameBegin;
        Enable(true);
        Functions.UIFunctions.InitMenuButtons();
        PlanetVeinUtilization.Enable(PlanetVeinUtilizationEnabled.Value);
    }

    public static void Uninit()
    {
        PlanetVeinUtilization.Enable(false);
        Enable(false);
        GameLogicProc.OnGameBegin -= PlanetVeinUtilization.OnGameBegin;
    }

    private class PlanetVeinUtilization : PatchImpl<PlanetVeinUtilization>
    {
        private static VeinTypeInfo[] planetVeinCount = null;
        private static VeinTypeInfo[] starVeinCount = null;
        private static readonly Dictionary<int, bool> tmpGroups = [];

        public static void OnGameBegin()
        {
            if (planetVeinCount != null)
            {
                foreach (VeinTypeInfo vti in planetVeinCount)
                {
                    if (vti.textCtrl != null)
                    {
                        Object.Destroy(vti.textCtrl.gameObject);
                    }
                }
                planetVeinCount = null;
            }
            if (starVeinCount != null)
            {
                foreach (VeinTypeInfo vti in starVeinCount)
                {
                    if (vti.textCtrl != null)
                    {
                        Object.Destroy(vti.textCtrl.gameObject);
                    }
                }
                starVeinCount = null;
            }
            var maxVeinId = LDB.veins.dataArray.Max(vein => vein.ID);
            planetVeinCount = new VeinTypeInfo[maxVeinId + 1];
            starVeinCount = new VeinTypeInfo[maxVeinId + 1];
            InitializeVeinCountArray(planetVeinCount);
            InitializeVeinCountArray(starVeinCount);
        }

        protected override void OnEnable()
        {
            if (planetVeinCount != null)
            {
                foreach (VeinTypeInfo vti in planetVeinCount)
                {
                    vti.Reset();
                    vti.textCtrl?.gameObject.SetActive(true);
                }
                UIPlanetDetail_RefreshDynamicProperties_Postfix(UIRoot.instance.uiGame.planetDetail);
            }
            if (starVeinCount != null)
            {
                foreach (VeinTypeInfo vti in starVeinCount)
                {
                    vti.Reset();
                    vti.textCtrl?.gameObject.SetActive(true);
                }
                UIStarDetail_RefreshDynamicProperties_Postfix(UIRoot.instance.uiGame.starDetail);
            }
        }

        private static Vector2 GetAdjustedSizeDelta(Vector2 origSizeDelta)
        {
            return new Vector2(origSizeDelta.x + 40f, origSizeDelta.y);
        }

        protected override void OnDisable()
        {
            if (planetVeinCount != null)
            {
                foreach (VeinTypeInfo vti in planetVeinCount)
                {
                    vti.Reset();
                    vti.textCtrl?.gameObject.SetActive(false);
                }
            }
            if (starVeinCount != null)
            {
                foreach (VeinTypeInfo vti in starVeinCount)
                {
                    vti.Reset();
                    vti.textCtrl?.gameObject.SetActive(false);
                }
            }
        }

        #region Helper functions
        private static void ProcessVeinData(VeinTypeInfo[] veinCount, VeinData[] veinPool)
        {
            lock (veinPool)
            {
                foreach (VeinData veinData in veinPool)
                {
                    if (veinData.groupIndex == 0 || veinData.amount == 0) continue;
                    if (tmpGroups.TryGetValue(veinData.groupIndex, out bool hasMiner))
                    {
                        if (hasMiner) continue;
                        hasMiner = veinData.minerCount > 0;
                        if (!hasMiner) continue;
                        tmpGroups[veinData.groupIndex] = true;
                        VeinTypeInfo vti = veinCount[(int)veinData.type];
                        vti.numVeinGroupsWithCollector++;
                    }
                    else
                    {
                        hasMiner = veinData.minerCount > 0;
                        tmpGroups.Add(veinData.groupIndex, hasMiner);
                        VeinTypeInfo vti = veinCount[(int)veinData.type];
                        vti.numVeinGroups++;
                        if (hasMiner)
                        {
                            vti.numVeinGroupsWithCollector++;
                        }
                    }
                }
            }
            tmpGroups.Clear();
        }

        private static void FormatResource(int refId, UIResAmountEntry uiresAmountEntry, VeinTypeInfo vt)
        {
            if (vt.textCtrl == null)
            {
                var parent = uiresAmountEntry.labelText.transform.parent;
                vt.textCtrl = Object.Instantiate(uiresAmountEntry.valueText, parent);
                vt.textCtrl.font = uiresAmountEntry.labelText.font;
                RectTransform trans = vt.textCtrl.rectTransform;
                var pos = uiresAmountEntry.rectTrans.localPosition;
                pos.x = pos.x + uiresAmountEntry.iconImage.rectTransform.localPosition.x - 25f;
                trans.localPosition = pos;
                Vector2 size = trans.sizeDelta;
                size.x = 40f;
                trans.sizeDelta = size;
            }
            else
            {
                RectTransform trans = vt.textCtrl.rectTransform;
                Vector3 pos = trans.localPosition;
                pos.y = uiresAmountEntry.rectTrans.localPosition.y;
                trans.localPosition = pos;
            }
            vt.textCtrl.text = $"{vt.numVeinGroupsWithCollector}/{vt.numVeinGroups}";
        }

        private static void InitializeVeinCountArray(VeinTypeInfo[] veinCountArray)
        {
            for (int i = 0; i < veinCountArray.Length; i++)
            {
                veinCountArray[i] = new VeinTypeInfo();
            }
        }
        #endregion

        #region UIPlanetDetail patches
        [HarmonyPrefix, HarmonyPatch(typeof(UIPlanetDetail), nameof(UIPlanetDetail.OnPlanetDataSet))]
        public static void UIPlanetDetail_OnPlanetDataSet_Prefix(UIPlanetDetail __instance)
        {
            foreach (VeinTypeInfo vti in planetVeinCount)
            {
                vti.Reset();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), nameof(UIPlanetDetail.RefreshDynamicProperties))]
        public static void UIPlanetDetail_RefreshDynamicProperties_Postfix(UIPlanetDetail __instance)
        {
            PlanetData planet = __instance.planet;
            if (planet == null || planet.runtimeVeinGroups == null || __instance.tabIndex != 0) { return; }

            int observeLevelCheck = __instance.planet == GameMain.localPlanet ? 1 : 2;
            if (GameMain.history.universeObserveLevel < observeLevelCheck) { return; }

            foreach (VeinTypeInfo vti in planetVeinCount)
            {
                vti.numVeinGroups = 0;
                vti.numVeinGroupsWithCollector = 0;
            }
            // count up the total number of vein groups per resource type, as well as the total number of groups that have a miner attached
            PlanetFactory factory = planet.factory;
            if (factory != null)
            {
                ProcessVeinData(planetVeinCount, factory.veinPool);
            }
            else
            {
                VeinGroup[] veinGroups = planet.runtimeVeinGroups;
                lock (planet.veinGroupsLock)
                {
                    for (int i = 1; i < veinGroups.Length; i++)
                    {
                        planetVeinCount[(int)veinGroups[i].type].numVeinGroups++;
                    }
                }
            }

            // update each resource to show the following vein group info:
            //     Iron:  <number of vein groups with miners> / <total number of vein groups>
            foreach (UIResAmountEntry uiresAmountEntry in __instance.entries)
            {
                int refId = uiresAmountEntry.refId;
                if (refId > 0 && refId < (int)EVeinType.Max)
                {
                    var vt = planetVeinCount[refId];
                    if (vt.numVeinGroups > 0)
                    {
                        FormatResource(refId, uiresAmountEntry, vt);
                    }
                    else if (vt.textCtrl != null)
                    {
                        vt.textCtrl.text = "";
                    }
                }
            }
        }
        #endregion

        #region UIStarDetail patches
        [HarmonyPrefix, HarmonyPatch(typeof(UIStarDetail), nameof(UIStarDetail.OnStarDataSet))]
        public static void UIStaretail_OnStarDataSet_Prefix(UIStarDetail __instance)
        {
            foreach (VeinTypeInfo vti in starVeinCount)
            {
                vti.Reset();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStarDetail), nameof(UIStarDetail.RefreshDynamicProperties))]
        public static void UIStarDetail_RefreshDynamicProperties_Postfix(UIStarDetail __instance)
        {
            if (__instance.star == null || __instance.tabIndex != 0) { return; }
            if (GameMain.history.universeObserveLevel < 2) { return; }

            foreach (VeinTypeInfo vti in starVeinCount)
            {
                vti.numVeinGroups = 0;
                vti.numVeinGroupsWithCollector = 0;
            }
            foreach (PlanetData planet in __instance.star.planets)
            {
                if (planet.runtimeVeinGroups == null) { continue; }
                PlanetFactory factory = planet.factory;
                if (factory != null)
                {
                    ProcessVeinData(starVeinCount, factory.veinPool);
                }
                else
                {
                    VeinGroup[] veinGroups = planet.runtimeVeinGroups;
                    lock (planet.veinGroupsLock)
                    {
                        for (int i = 1; i < veinGroups.Length; i++)
                        {
                            starVeinCount[(int)veinGroups[i].type].numVeinGroups++;
                        }
                    }
                }
            }
            // update each resource to show the following vein group info:
            //     Iron:  <number of vein groups with miners> / <total number of vein groups>
            foreach (UIResAmountEntry uiresAmountEntry in __instance.entries)
            {
                int refId = uiresAmountEntry.refId;
                if (refId > 0 && refId < (int)EVeinType.Max)
                {
                    var vt = starVeinCount[refId];
                    if (vt.numVeinGroups > 0)
                    {
                        FormatResource(refId, uiresAmountEntry, vt);
                    }
                    else if (vt.textCtrl != null)
                    {
                        vt.textCtrl.text = "";
                    }
                }
            }
        }
        #endregion
    }

    public class VeinTypeInfo
    {
        public int numVeinGroups;
        public int numVeinGroupsWithCollector;
        public Text textCtrl;

        public void Reset()
        {
            numVeinGroups = 0;
            numVeinGroupsWithCollector = 0;
            if (textCtrl != null)
            {
                textCtrl.text = "";
            }
        }
    }

    // Add config button to main menu
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIRoot), nameof(UIRoot._OnOpen))]
    public static void UIRoot__OnOpen_Postfix()
    {
        Functions.UIFunctions.InitMenuButtons();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.NotifyScanEnded))]
    private static void PlanetData_NotifyScanEnded_Postfix(PlanetData __instance)
    {
        if (PlanetModelingManager.scnPlanetReqList.Count > 0) return;
        BepInEx.ThreadingHelper.Instance.StartSyncInvoke(Functions.UIFunctions.OnPlanetScanEnded);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIPlanetGlobe), nameof(UIPlanetGlobe.DistributeButtons))]
    private static void UIPlanetGlobe_DistributeButtons_Postfix(UIPlanetGlobe __instance)
    {
        Functions.UIFunctions.UpdateGlobeButtonPosition(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIStarmapStar), nameof(UIStarmapStar.OnStarDisplayNameChange))]
    private static bool UIStarmapStar_OnStarDisplayNameChange_Prefix()
    {
        return Functions.UIFunctions.CornerComboBoxIndex == 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStarmapStar), nameof(UIStarmapStar._OnClose))]
    private static void UIStarmapStar__OnClose_Postfix(UIStarmapStar __instance)
    {
        Functions.UIFunctions.StarmapFilterToggler?.SetCheckedWithEvent(false);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIMechaLab), nameof(UIMechaLab.DetermineVisible))]
    private static bool UIMechaLab_DetermineVisible_Prefix(UIMechaLab __instance, ref bool __result)
    {
        if (!UIRoot.instance.uiGame.starmap.active || !Functions.UIFunctions.StarmapFilterToggler.Checked)
        {
            return true;
        }
        __instance._Close();
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGoalPanel), nameof(UIGoalPanel.DetermineVisiable))]
    private static bool UIGoalPanel_DetermineVisiable_Prefix(UIGoalPanel __instance)
    {
        if (!UIRoot.instance.uiGame.starmap.active || !Functions.UIFunctions.StarmapFilterToggler.Checked)
        {
            return true;
        }
        __instance.isUseOverwrittenState = true;
        if (__instance.stateOverwritten == EUIGoalPanelState.None)
        {
            __instance.stateOverwritten = EUIGoalPanelState.Collapse;
        }
        return false;
    }
}
