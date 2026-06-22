using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UXAssist.Common;

namespace UXAssist.Patches.Factory;

internal static class BuildingBufferPatch
{
    public static void Enable(bool enable)
    {
        TweakBuildingBuffer.Enable(enable);
    }

    internal class TweakBuildingBuffer : PatchImpl<TweakBuildingBuffer>
    {
        public static void RefreshAssemblerBufferMultipliers()
        {
            if (!FactoryPatch.TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(AssemblerComponent_UpdateNeeds_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(AssemblerComponent_UpdateNeeds_Transpiler)));
        }

        public static void RefreshLabBufferMaxCountForAssemble()
        {
            if (!FactoryPatch.TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsAssemble_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsAssemble_Transpiler)));
        }

        public static void RefreshLabBufferMaxCountForResearch()
        {
            if (!FactoryPatch.TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsResearch_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(LabComponent_UpdateNeedsResearch_Transpiler)));
        }

        public static void RefreshReceiverBufferCount()
        {
            if (!FactoryPatch.TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(PowerGeneratorComponent_GameTick_Gamma_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(PowerGeneratorComponent_GameTick_Gamma_Transpiler)));
        }

        public static void RefreshEjectorBufferCount()
        {
            if (!FactoryPatch.TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(EjectorComponent_InternalUpdate_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(EjectorComponent_InternalUpdate_Transpiler)));
        }

        public static void RefreshSiloBufferCount()
        {
            if (!FactoryPatch.TweakBuildingBufferEnabled.Value) return;
            /* re-patch to use new value */
            var patch = Instance._patch;
            patch.Unpatch(AccessTools.Method(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate)), AccessTools.Method(typeof(TweakBuildingBuffer), nameof(SiloComponent_InternalUpdate_Transpiler)));
            patch.Patch(AccessTools.Method(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate)), null, null, new HarmonyMethod(typeof(TweakBuildingBuffer), nameof(SiloComponent_InternalUpdate_Transpiler)));
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma))]
        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_GameTick_Gamma_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  bool flag3 = keyFrame && useIon && (float)this.catalystPoint < 72000f;
             * To:
             *  bool flag3 = keyFrame && useIon && this.catalystPoint < 3600 * ReceiverBufferCount.Value;
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.catalystPoint))),
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Ldc_R4, 72000f),
                new CodeMatch(OpCodes.Clt)
            );
            matcher.Advance(2).RemoveInstructions(2).Insert(new CodeInstruction(OpCodes.Ldc_I4, FactoryPatch.ReceiverBufferCount.Value * 3600));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds))]
        private static IEnumerable<CodeInstruction> AssemblerComponent_UpdateNeeds_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  int num2 = this.speedOverride * 180 / this.timeSpend + 1;
             *  if (num2 < 2)
             *  {
             *      num2 = 2;
             *  }
             * To:
             *  int num2 = this.speedOverride * 60 * (AssemblerBufferTimeMultiplier.Value - 1) * 60 / this.timeSpend + 1;
             *  if (num2 < AssemblerBufferMininumMultiplier.Value)
             *  {
             *      num2 = AssemblerBufferMininumMultiplier.Value;
             *  }
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(AssemblerComponent), nameof(AssemblerComponent.speedOverride))),
                new CodeMatch(OpCodes.Ldc_I4, 180),
                new CodeMatch(OpCodes.Mul)
            );
            matcher.Advance(2).Operand = (FactoryPatch.AssemblerBufferTimeMultiplier.Value - 1) * 60;
            matcher.Advance(2).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.opcode == OpCodes.Bge_S || ci.opcode == OpCodes.Bge),
                new CodeMatch(OpCodes.Ldc_I4_2)
            );
            matcher.Operand = FactoryPatch.AssemblerBufferMininumMultiplier.Value;
            matcher.Advance(2).Operand = FactoryPatch.AssemblerBufferMininumMultiplier.Value;
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble))]
        private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsAssemble_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  int num2 = ((this.timeSpend > 5400000) ? 6 :
             *             (3 * ((this.speedOverride + 5001) / 10000) + 3));
             * To:
             *  int num2 = ((this.timeSpend > 5400000) ? LabBufferMaxCountForAssemble.Value :
             *             (LabBufferExtraCountForAdvancedAssemble.Value * ((this.speedOverride + 5001) / 10000) + (LabBufferMaxCountForAssemble.Value - LabBufferExtraCountForAdvancedAssemble.Value)));
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.recipeExecuteData))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(RecipeExecuteData), nameof(RecipeExecuteData.timeSpend))),
                new CodeMatch(OpCodes.Ldc_I4, 5400000),
                new CodeMatch(ci => ci.opcode == OpCodes.Bgt_S || ci.opcode == OpCodes.Bgt),
                new CodeMatch(OpCodes.Ldc_I4_3)
            );
            var extraCount = FactoryPatch.LabBufferExtraCountForAdvancedAssemble.Value;
            matcher.Advance(5).SetAndAdvance(OpCodes.Ldc_I4, extraCount);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Ldc_I4_3),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S),
                new CodeMatch(OpCodes.Ldc_I4_6),
                new CodeMatch(ci => ci.IsStloc())
            );
            var maxCount = FactoryPatch.LabBufferMaxCountForAssemble.Value;
            matcher.Advance(2).SetAndAdvance(OpCodes.Ldc_I4, maxCount > extraCount ? maxCount - extraCount : 2);
            matcher.Advance(2).SetAndAdvance(OpCodes.Ldc_I4, maxCount);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch))]
        private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsResearch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            /*
             * Patch:
             *  this.needs[0] = ((this.matrixServed[0] < 36000) ? 6001 : 0);
             *  ...
             * To:
             *  this.needs[0] = ((this.matrixServed[0] < LabBufferMaxCountForResearch.Value * 3600) ? 6001 : 0);
             *  ...
             */
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4, 36000)
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, FactoryPatch.LabBufferMaxCountForResearch.Value * 3600));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.bulletCount))),
                new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4 || ci.opcode == OpCodes.Ldc_I4_S) && ci.OperandIs(20))
            );
            matcher.Advance(2).Set(OpCodes.Ldc_I4, FactoryPatch.EjectorBufferCount.Value);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> SiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SiloComponent), nameof(SiloComponent.bulletCount))),
                new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4 || ci.opcode == OpCodes.Ldc_I4_S) && ci.OperandIs(20))
            );
            matcher.Advance(2).Operand = FactoryPatch.SiloBufferCount.Value;
            return matcher.InstructionEnumeration();
        }
    }
}
