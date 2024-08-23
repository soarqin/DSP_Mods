using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;
public static class PlanetPatch
{
    public static ConfigEntry<bool> WaterPumpAnywhereEnabled;
    public static ConfigEntry<bool> TerraformAnywayEnabled;

    public static void Init()
    {
        WaterPumpAnywhereEnabled.SettingChanged += (_, _) => WaterPumperPatch.Enable(WaterPumpAnywhereEnabled.Value);
        TerraformAnywayEnabled.SettingChanged += (_, _) => TerraformAnyway.Enable(TerraformAnywayEnabled.Value);
        WaterPumperPatch.Enable(WaterPumpAnywhereEnabled.Value);
        TerraformAnyway.Enable(TerraformAnywayEnabled.Value);
    }

    public static void Uninit()
    {
        WaterPumperPatch.Enable(false);
        TerraformAnyway.Enable(false);
    }

    private static class WaterPumperPatch
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(WaterPumperPatch));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs((int)EBuildCondition.NeedWater))
            ).Advance(1).MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs((int)EBuildCondition.NeedWater))
            );
            matcher.Repeat(codeMatcher =>
            {
                codeMatcher.SetAndAdvance(OpCodes.Ldc_I4_S, 0);
            });
            return matcher.InstructionEnumeration();
        }
    }

    private static class TerraformAnyway
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(TerraformAnyway));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.cursorPointCount))),
                new CodeMatch(ci => ci.opcode == OpCodes.Blt || ci.opcode == OpCodes.Blt_S)
            ).RemoveInstructions(2).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_0)
            ).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), "get_sandCount"))
            ).Advance(1).MatchForward(false,
                    new CodeMatch(OpCodes.Conv_I8),
                    new CodeMatch(OpCodes.Sub)
            ).Advance(2).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I8, 0L),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), "Max", new[] { typeof(long), typeof(long) }))
            );
            return matcher.InstructionEnumeration();
        }
    }
}