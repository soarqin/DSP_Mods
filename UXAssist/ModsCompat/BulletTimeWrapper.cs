using BepInEx.Bootstrap;
using HarmonyLib;

namespace UXAssist.ModsCompat;

public static class BulletTimeWrapper
{
    private const string BulletTimeGuid = "com.starfi5h.plugin.BulletTime";
    public static bool HasBulletTime;

    public static void Start(Harmony _)
    {
        HasBulletTime = Chainloader.PluginInfos.TryGetValue(BulletTimeGuid, out var _);
    }
}
