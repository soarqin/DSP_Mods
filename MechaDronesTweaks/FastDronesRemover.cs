using HarmonyLib;

namespace MechaDronesTweaks;

class FastDronesRemover
{
    public const string FastDronesGuid = "com.dkoppstein.plugin.DSP.FastDrones";
    private const string FastDronesVersion = "0.0.5";

    public static bool Run(Harmony harmony)
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(FastDronesGuid, out var pluginInfo) ||
            pluginInfo.Metadata.Version.ToString() != FastDronesVersion) return false;
        var assembly = pluginInfo.Instance.GetType().Assembly;
        var classType = assembly.GetType("com.dkoppstein.plugin.DSP.FastDrones.FastDronesPlugin");
        harmony.Patch(AccessTools.Method(classType, "Start"),
            new HarmonyMethod(typeof(FastDronesRemover).GetMethod("PatchFastDronesStart")));
        return true;
    }

    public static bool PatchFastDronesStart()
    {
        return false;
    }
}