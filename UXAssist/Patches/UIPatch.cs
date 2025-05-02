namespace UXAssist.Patches;

using Common;
using HarmonyLib;

[PatchGuid(PluginInfo.PLUGIN_GUID)]
public class UIPatch: PatchImpl<UIPatch>
{
    public static void Start()
    {
        Enable(true);
        Functions.UIFunctions.InitMenuButtons();
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
