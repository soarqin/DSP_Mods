using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;
public static class TerraformPatch
{
    public static ConfigEntry<bool> Enabled;
    private static Harmony _patch;

    public static void Init()
    {
        Enabled.SettingChanged += (_, _) => ValueChanged();
        ValueChanged();
    }

    public static void Uninit()
    {
        if (_patch == null) return;
        _patch.UnpatchSelf();
        _patch = null;
    }

    private static void ValueChanged()
    {
        if (Enabled.Value)
        {
            if (_patch != null)
            {
                return;
            }

            _patch = Harmony.CreateAndPatchAll(typeof(TerraformPatch));
        }
        else if (_patch != null)
        {
            _patch.UnpatchSelf();
            _patch = null;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
    private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), "get_sandCount"))
        ).Advance(3).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), "Max", new[] { typeof(int), typeof(int) }))
        ).Advance(1).RemoveInstructions(3);
        return matcher.InstructionEnumeration();
    }
}