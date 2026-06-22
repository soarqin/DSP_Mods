using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Patches.Factory;

internal class NoConditionBuild : PatchImpl<NoConditionBuild>
{
    protected override void OnEnable()
    {
        GameMain.data?.warningSystem?.UpdateCriticalWarningText();
    }

    protected override void OnDisable()
    {
        GameMain.data?.warningSystem?.UpdateCriticalWarningText();
    }

    [HarmonyTranspiler, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
    [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
    private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
        yield return new CodeInstruction(OpCodes.Ret);
    }

    [HarmonyTranspiler, HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
    private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        matcher.Start().InsertAndAdvance(
            new CodeInstruction(OpCodes.Br, label1)
        );
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.buildPreviews))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<BuildPreview>), nameof(List<BuildPreview>.Count))),
            new CodeMatch(ci => ci.IsStloc())
        );
        matcher.Labels.Add(label1);
        matcher.Advance(4).InsertAndAdvance(
            new CodeInstruction(OpCodes.Br, label2)
        );
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.Branches(out _)),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.waitForConfirm))),
            new CodeMatch(ci => ci.Branches(out _))
        );
        var operand = matcher.Operand;
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(label2),
            new CodeInstruction(OpCodes.Stloc_S, operand)
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
    [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
    private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.Start().InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NoConditionBuild), nameof(CheckForMiner))),
            new CodeInstruction(OpCodes.Brfalse_S, label1),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    public static bool CheckForMiner(BuildTool tool)
    {
        var previews = tool.buildPreviews;
        foreach (var preview in previews)
        {
            var desc = preview?.item?.prefabDesc;
            if (desc == null) continue;
            if (desc.veinMiner || desc.oilMiner) return false;
        }

        return true;
    }
}
