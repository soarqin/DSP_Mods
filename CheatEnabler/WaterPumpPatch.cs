using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;
public static class WaterPumperPatch
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
                _patch.UnpatchSelf();
                _patch = null;
            }

            _patch = Harmony.CreateAndPatchAll(typeof(WaterPumperPatch));
        }
        else if (_patch != null)
        {
            _patch.UnpatchSelf();
            _patch = null;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
    [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
    private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(instr => instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(22))
        ).Advance(1).MatchForward(false,
            new CodeMatch(instr => instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(22))
        );
        matcher.Repeat(codeMatcher =>
        {
            codeMatcher.SetAndAdvance(OpCodes.Ldc_I4_S, 0);
        });
        return matcher.InstructionEnumeration();
    }
}