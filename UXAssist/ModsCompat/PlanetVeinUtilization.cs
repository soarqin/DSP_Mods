using HarmonyLib;
using UXAssist.Common.ModCompat;

namespace UXAssist.ModsCompat;

class PlanetVeinUtilization
{
    public const string PlanetVeinUtilizationGuid = "testpostpleaseignore.dsp.planet_vein_utilization";

    public static bool Run(Harmony harmony)
    {
        if (!ModCompatHelper.TryGetLoadedPluginInfo(PlanetVeinUtilizationGuid, out var pluginInfo)) return false;
        if (!ModCompatHelper.TryGetPluginType(pluginInfo, "PlanetVeinUtilization.PlanetVeinUtilization", out var classType)) return false;
        harmony.Patch(AccessTools.Method(classType, "Awake"),
            new HarmonyMethod(typeof(PlanetVeinUtilization).GetMethod("PatchPlanetVeinUtilizationAwake")));
        return true;
    }

    public static bool PatchPlanetVeinUtilizationAwake()
    {
        return false;
    }
}
