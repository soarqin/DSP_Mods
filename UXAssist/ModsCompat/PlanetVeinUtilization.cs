using BepInEx.Bootstrap;
using HarmonyLib;

namespace UXAssist.ModsCompat;

class PlanetVeinUtilization
{
    public const string PlanetVeinUtilizationGuid = "testpostpleaseignore.dsp.planet_vein_utilization";

    public static bool Run(Harmony harmony)
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(PlanetVeinUtilizationGuid, out var pluginInfo)) return false;
        var assembly = pluginInfo.Instance.GetType().Assembly;
        var classType = assembly.GetType("PlanetVeinUtilization.PlanetVeinUtilization");
        harmony.Patch(AccessTools.Method(classType, "Awake"),
            new HarmonyMethod(typeof(PlanetVeinUtilization).GetMethod("PatchPlanetVeinUtilizationAwake")));
        return true;
    }

    public static bool PatchPlanetVeinUtilizationAwake()
    {
        return false;
    }
}
