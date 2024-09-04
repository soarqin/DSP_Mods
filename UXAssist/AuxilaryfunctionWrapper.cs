using BepInEx.Configuration;
using HarmonyLib;

namespace UXAssist;

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
    }
}