using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UXAssist.Common;

namespace UXAssist.ModsCompat;

public static class BulletTimeWrapper
{
    private const string BulletTimeGuid = "com.starfi5h.plugin.BulletTime";
    public static bool HasBulletTime;

    public static void Start(Harmony _)
    {
        HasBulletTime = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(BulletTimeGuid, out var _);
    }
}
