using System;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common.ModCompat;
using UXAssist.Patches;

namespace UXAssist.ModsCompat;

public static class AuxilaryfunctionWrapper
{
    private const string AuxilaryfunctionGuid = "cn.blacksnipe.dsp.Auxilaryfunction";
    public static ConfigEntry<bool> ShowStationInfo;

    public static void Start(Harmony harmony)
    {
        if (!ModCompatHelper.TryGetLoadedPluginInfo(AuxilaryfunctionGuid, out var pluginInfo)) return;
        if (!ModCompatHelper.TryGetPluginType(pluginInfo, "Auxilaryfunction.Auxilaryfunction", out var classType))
        {
            UXAssist.Logger.LogWarning("Failed to locate Auxilaryfunction main type");
            return;
        }
        if (!ModCompatHelper.TryGetFieldValue<ConfigEntry<bool>>(classType, "ShowStationInfo", pluginInfo.Instance, out ShowStationInfo))
        {
            UXAssist.Logger.LogWarning("Failed to get ShowStationInfo from Auxilaryfunction");
        }
        if (!ModCompatHelper.TryGetPluginType(pluginInfo, "Auxilaryfunction.Patch.SpeedUpPatch", out var speedUpPatchType))
        {
            UXAssist.Logger.LogWarning("Failed to locate Auxilaryfunction SpeedUpPatch");
            return;
        }
        if (!ModCompatHelper.TryGetPropertySetter(speedUpPatchType, "Enable", out var setter))
        {
            UXAssist.Logger.LogWarning("Failed to resolve SpeedUpPatch.set_Enable() from Auxilaryfunction");
            return;
        }
        harmony.Patch(setter,
            new HarmonyMethod(AccessTools.Method(typeof(AuxilaryfunctionWrapper), nameof(PatchSpeedUpPatchEnable))));
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