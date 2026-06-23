using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;

namespace CheatEnabler.Patches;

[ModFeature("Planet")]
public static class PlanetPatch
{
    public static ConfigEntry<bool> WaterPumpAnywhereEnabled;
    public static ConfigEntry<bool> TerraformAnywayEnabled;

    public static void Init()
    {
        WaterPumpAnywhereEnabled.SettingChanged += (_, _) => WaterPumperPatch.Enable(WaterPumpAnywhereEnabled.Value);
        TerraformAnywayEnabled.SettingChanged += (_, _) => TerraformAnyway.Enable(TerraformAnywayEnabled.Value);
    }

    public static void Start()
    {
        WaterPumperPatch.Enable(WaterPumpAnywhereEnabled.Value);
        TerraformAnyway.Enable(TerraformAnywayEnabled.Value);
    }

    public static void Uninit()
    {
        WaterPumperPatch.Enable(false);
        TerraformAnyway.Enable(false);
    }

    private class WaterPumperPatch : PatchImpl<WaterPumperPatch>
    {
        // Harmony transpiler: BuildTool_CheckBuildConditions_Transpiler
        // Target: BuildTool_BlueprintPaste.CheckBuildConditions, BuildTool_Click.CheckBuildConditions
        // Fallback: None — patch will fail loudly if the target method body changes.
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

    private class TerraformAnyway : PatchImpl<TerraformAnyway>
    {
        // Harmony transpiler: BuildTool_BlueprintPaste_DetermineReforms_Patch
        // Target: BuildTool_BlueprintPaste.DetermineReforms
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.DetermineReforms))]
        private static IEnumerable<CodeInstruction> BuildTool_BlueprintPaste_DetermineReforms_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.sandCount))),
                new CodeMatch(ci => ci.IsStloc())
            ).Advance(4).MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Conv_I8),
                new CodeMatch(ci => ci.opcode == OpCodes.Bge || ci.opcode == OpCodes.Bge_S)
            );
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.RemoveInstructions(3);
            matcher.Labels.AddRange(labels);
            matcher.Opcode = OpCodes.Br;
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: BuildTool_Reform_RemoveBasePit_Patch
        // Target: BuildTool_Reform.RemoveBasePit
        // Fallback: Checks CodeMatcher.IsInvalid/IsValid and returns original instructions on mismatch.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.RemoveBasePit))]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_RemoveBasePit_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.circlePointCount))),
                new CodeMatch(ci => ci.opcode == OpCodes.Blt || ci.opcode == OpCodes.Blt_S)
            ).RemoveInstructions(2).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_0)
            ).MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.sandCount))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Conv_I8),
                new CodeMatch(OpCodes.Sub)
            ).Advance(6).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I8, 0L),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Max), [typeof(long), typeof(long)]))
            );
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: UIRemoveBasePitButton_OnRemoveButtonClick_Patch
        // Target: UIRemoveBasePitButton.OnRemoveButtonClick, UIRemoveBasePitButton._OnUpdate
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIRemoveBasePitButton), nameof(UIRemoveBasePitButton.OnRemoveButtonClick))]
        [HarmonyPatch(typeof(UIRemoveBasePitButton), nameof(UIRemoveBasePitButton._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIRemoveBasePitButton_OnRemoveButtonClick_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIRemoveBasePitButton), nameof(UIRemoveBasePitButton.pointCount))),
                new CodeMatch(OpCodes.Sub),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Clt)
            );
            if (matcher.IsValid)
            {
                matcher.RemoveInstructions(2).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_I4_0)
                );
            }
            else
            {
                matcher.Start();
            }
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIRemoveBasePitButton), nameof(UIRemoveBasePitButton.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.sandCount))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Conv_I8),
                new CodeMatch(OpCodes.Sub)
            ).Advance(6).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I8, 0L),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Max), [typeof(long), typeof(long)]))
            );
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: BuildTool_Reform_ReformAction_Patch
        // Target: BuildTool_Reform.ReformAction
        // Fallback: None — patch will fail loudly if the target method body changes.
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
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.sandCount)))
            ).Advance(1).MatchForward(false,
                new CodeMatch(OpCodes.Conv_I8),
                new CodeMatch(OpCodes.Sub)
            ).Advance(2).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I8, 0L),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Max), [typeof(long), typeof(long)]))
            );
            return matcher.InstructionEnumeration();
        }
    }
}