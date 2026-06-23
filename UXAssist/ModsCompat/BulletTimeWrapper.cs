using HarmonyLib;
using UXAssist.Common.ModCompat;

namespace UXAssist.ModsCompat;

public static class BulletTimeWrapper
{
    private const string BulletTimeGuid = "com.starfi5h.plugin.BulletTime";
    public static bool HasBulletTime;

    public static void Start(Harmony _)
    {
        HasBulletTime = ModCompatHelper.TryGetLoadedPluginInfo(BulletTimeGuid, out var _);
    }
}
