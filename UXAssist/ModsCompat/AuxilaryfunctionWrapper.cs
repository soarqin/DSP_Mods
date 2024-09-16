using System;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Patches;

namespace UXAssist.ModsCompat;

public static class AuxilaryfunctionWrapper
{
    private const string AuxilaryfunctionGuid = "cn.blacksnipe.dsp.Auxilaryfunction";
    public static ConfigEntry<bool> ShowStationInfo;

    public static void Init(Harmony harmony)
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(AuxilaryfunctionGuid, out var pluginInfo)) return;
        var assembly = pluginInfo.Instance.GetType().Assembly;
        try
        {
            var classType = assembly.GetType("Auxilaryfunction.Auxilaryfunction");
            ShowStationInfo = (ConfigEntry<bool>)AccessTools.Field(classType, "ShowStationInfo").GetValue(pluginInfo.Instance);
        }
        catch
        {
            UXAssist.Logger.LogWarning("Failed to get ShowStationInfo from Auxilaryfunction");
        }
        try
        {
            var classType = assembly.GetType("Auxilaryfunction.Patch.SpeedUpPatch");
            harmony.Patch(AccessTools.PropertySetter(classType, "Enable"),
                new HarmonyMethod(AccessTools.Method(typeof(AuxilaryfunctionWrapper), nameof(PatchSpeedUpPatchEnable))));
        }
        catch
        {
            UXAssist.Logger.LogWarning("Failed to patch SpeedUpPatch.set_Enable() from Auxilaryfunction");
        }
    }

    public static void PatchSpeedUpPatchEnable(bool value)
    {
        if (!value)
        {
            GamePatch.EnableGameUpsFactor = true;
            return;
        }
        if (Math.Abs(GamePatch.GameUpsFactor.Value - 1.0) < 0.001) return;
        GamePatch.EnableGameUpsFactor = false;
        UXAssist.Logger.LogInfo("Game UPS changing is disabled when using Auxilaryfunction's speed up feature");
    }
}