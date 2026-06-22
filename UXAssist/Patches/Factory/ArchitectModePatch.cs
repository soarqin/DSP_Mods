using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist.Patches.Factory;

internal static class ArchitectModePatch
{
    public static void Enable(bool enable)
    {
        UnlimitInteractive.Enable(enable);
        RemoveSomeConditionBuild.Enable(enable);
        RemoveBuildRangeLimit.Enable(enable);
        LargerAreaForUpgradeAndDismantle.Enable(enable);
        LargerAreaForTerraform.Enable(enable);
    }

    internal class UnlimitInteractive : PatchImpl<UnlimitInteractive>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GetObjectSelectDistance))]
        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GetObjectSelectDistance_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_R4, 10000f);
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    internal class RemoveSomeConditionBuild : PatchImpl<RemoveSomeConditionBuild>
    {
        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* search for:
             *  ldloc.s	V_8 (8)
             *  ldfld	class PrefabDesc BuildPreview::desc
             *  ldfld	bool PrefabDesc::isInserter
             *  brtrue	2358 (1C12) ldloc.s V_8 (8)
             *  ldloca.s	V_10 (10)
             *  call	instance float32 [UnityEngine.CoreModule]UnityEngine.Vector3::get_magnitude()
             */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isInserter))),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldloca_S),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.magnitude)))
            );
            /* Change to:
             *  Ldloc.s	V_8 (8)
             *  ldfld	class PrefabDesc BuildPreview::desc
             *  ldfld	bool PrefabDesc::isEjector
             *  brfalse	2358 (1C12) ldloc.s V_8 (8)
             */
            matcher.Advance(2);
            matcher.Operand = AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isEjector));
            matcher.Advance(1);
            matcher.Opcode = OpCodes.Brfalse;
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* search for:
             * ldloc.s	V_88 (88)
             * ldloc.s	V_120 (120)
             * brtrue.s	2054 (173A) ldc.i4.s 17
             * ldc.i4.s	EBuildCondition.JointCannotLift (19)
             * br.s	2055 (173C) stfld valuetype EBuildCondition BuildPreview::condition
             * ldc.i4.s	EBuildCondition.TooBendToLift (18)
             * stfld	valuetype EBuildCondition BuildPreview::condition
             */
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs((int)EBuildCondition.JointCannotLift)),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs((int)EBuildCondition.TooBendToLift)),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
            );
            if (matcher.IsValid)
            {
                // Remove 7 instructions, if the following instruction is br/br.s, remove it as well
                var labels = matcher.Labels;
                matcher.Labels = [];
                matcher.RemoveInstructions(7);
                var opcode = matcher.Opcode;
                if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                    matcher.RemoveInstruction();
                matcher.Labels.AddRange(labels);
            }

            /* search for:
             * ldloc.s	V_88 (88)
             * ldc.i4.s	EBuildCondition.TooSteep(16)-EBuildCondition.InputConflict(20)
             * stfld	valuetype EBuildCondition BuildPreview::condition
             */
            matcher.Start().MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldloc_S || instr.opcode == OpCodes.Ldloc),
                new CodeMatch(instr =>
                    (instr.opcode == OpCodes.Ldc_I4_S || instr.opcode == OpCodes.Ldc_I4) &&
                    Convert.ToInt64(instr.operand) is >= (int)EBuildCondition.TooSteep and <= (int)EBuildCondition.InputConflict),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
            );
            if (matcher.IsValid)
            {
                // Remove 3 instructions, if the following instruction is br/br.s, remove it as well
                matcher.Repeat(codeMatcher =>
                {
                    var labels = codeMatcher.Labels;
                    codeMatcher.Labels = [];
                    codeMatcher.RemoveInstructions(3);
                    var opcode = codeMatcher.Opcode;
                    if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                        codeMatcher.RemoveInstruction();
                    codeMatcher.Labels.AddRange(labels);
                });
            }

            return matcher.InstructionEnumeration();
        }
    }

    internal class RemoveBuildRangeLimit : PatchImpl<RemoveBuildRangeLimit>
    {
        protected override void OnEnable()
        {
            var controller = GameMain.mainPlayer?.controller;
            if (controller == null) return;
            controller.actionBuild?.clickTool?._OnInit();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnInit))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(15))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 512));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DetermineMoreChainTargets))]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DetermineMoreChainTargets))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildAreaLimitRemoval_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Patch (player.mecha.buildArea * player.mecha.buildArea) to 100000000 */
            matcher.MatchForward(false,
                new CodeMatch(),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.buildArea))),
                new CodeMatch(),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.buildArea))),
                new CodeMatch(OpCodes.Mul)
            );
            matcher.Repeat(m => m.RemoveInstructions(9).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, 100000000.0f)));
            return matcher.InstructionEnumeration();
        }
    }

    internal class LargerAreaForUpgradeAndDismantle : PatchImpl<LargerAreaForUpgradeAndDismantle>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTools_CursorSizePatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(11))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4_S, 31));
            return matcher.InstructionEnumeration();
        }
    }

    internal class LargerAreaForTerraform : PatchImpl<LargerAreaForTerraform>
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.brushSize))),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.Advance(1).SetAndAdvance(OpCodes.Ldc_I4_S, 30));
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10)),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.brushSize)))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4_S, 30));
            return matcher.InstructionEnumeration();
        }
    }
}
