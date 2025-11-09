using BepInEx.Bootstrap;
using CommonAPI;
using HarmonyLib;

namespace UXAssist.ModsCompat;

public static class CommonAPIWrapper
{
    public static void Run(Harmony harmony)
    {
        if (!Chainloader.PluginInfos.TryGetValue(CommonAPIPlugin.GUID, out var commonAPIPlugin) ||
            commonAPIPlugin.Metadata.Version > new System.Version(1, 6, 7, 0)) return;
        harmony.Patch(AccessTools.Method(typeof(GameOption), nameof(GameOption.InitKeys)), new HarmonyMethod(typeof(CommonAPIWrapper), nameof(PatchInitKeys)));
    }

    public static bool PatchInitKeys(GameOption __instance)
    {
        return __instance.overrideKeys == null;
    }
}
