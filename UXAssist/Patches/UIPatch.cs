namespace UXAssist.Patches;

using Common;
using HarmonyLib;
using UnityEngine;

[PatchGuid(PluginInfo.PLUGIN_GUID)]
public class UIPatch: PatchImpl<UIPatch>
{
    public static void Start()
    {
        Enable(true);
        Functions.UIFunctions.InitMenuButtons();
    }

    // Add config button to main menu
    [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot._OnOpen))]
    public static void UIRoot__OnOpen_Postfix()
    {
        Functions.UIFunctions.InitMenuButtons();
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
}
