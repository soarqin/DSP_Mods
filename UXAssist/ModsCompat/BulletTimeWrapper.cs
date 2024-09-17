using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace UXAssist.ModsCompat;

public static class BulletTimeWrapper
{
    private const string BulletTimeGuid = "com.starfi5h.plugin.BulletTime";
    public static bool HasBulletTime;

    public static void Init(Harmony harmony)
    {
        HasBulletTime = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(BulletTimeGuid, out var pluginInfo);
        if (!HasBulletTime) return;
        var assembly = pluginInfo.Instance.GetType().Assembly;
        try
        {
            var classType = assembly.GetType("BulletTime.IngameUI");
            harmony.Patch(AccessTools.Method(classType, "Init"),
                null, null, new HarmonyMethod(AccessTools.Method(typeof(BulletTimeWrapper), nameof(IngameUI_Init_Transpiler))));
            harmony.Patch(AccessTools.Method(classType, "OnSpeedButtonClick"),
                null, null, new HarmonyMethod(AccessTools.Method(typeof(BulletTimeWrapper), nameof(IngameUI_OnSpeedButtonClick_Transpiler))));
        }
        catch
        {
            UXAssist.Logger.LogWarning("Failed to patch BulletTime functions()");
        }
    }

    private static IEnumerable<CodeInstruction> IngameUI_Init_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldstr, "Increase game speed (max 4x)")
        ).Set(OpCodes.Ldstr, "Increase game speed (max 10x)");
        return matcher.InstructionEnumeration();
    }

    private static IEnumerable<CodeInstruction> IngameUI_OnSpeedButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_R8, 240.0)
        ).Set(OpCodes.Ldc_R8, 600.0);
        return matcher.InstructionEnumeration();
    }

}