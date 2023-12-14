using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace OverclockEverything;

[HarmonyPatch]
public static class BeltFix
{
    private static readonly CodeInstruction[] LdcInstrs = {
        new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ldc_I4_2),
        new CodeInstruction(OpCodes.Ldc_I4_3), new CodeInstruction(OpCodes.Ldc_I4_4), new CodeInstruction(OpCodes.Ldc_I4_5),
        new CodeInstruction(OpCodes.Ldc_I4_6), new CodeInstruction(OpCodes.Ldc_I4_7), new CodeInstruction(OpCodes.Ldc_I4_8),
        new CodeInstruction(OpCodes.Ldc_I4_S, 9), new CodeInstruction(OpCodes.Ldc_I4_S, 10)
    };
    [HarmonyTranspiler, HarmonyPatch(typeof(CargoTraffic), "AlterBeltRenderer")]
    public static IEnumerable<CodeInstruction> CargoTraffic_AlterBeltRenderer_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var lastIsSpeed = false;
        foreach (var instr in instructions)
        {
            if (lastIsSpeed)
            {
                lastIsSpeed = false;
                if (instr.opcode == OpCodes.Ldc_I4_1)
                {
                    yield return LdcInstrs[Patch.Cfg.BeltSpeed[0]];
                }
                else if (instr.opcode == OpCodes.Ldc_I4_2)
                {
                    yield return LdcInstrs[Patch.Cfg.BeltSpeed[1]];
                }
                else
                {
                    yield return instr;
                }
            }
            else
            {
                lastIsSpeed = instr.opcode == OpCodes.Ldfld &&
                              instr.OperandIs(AccessTools.Field(typeof(BeltComponent), nameof(BeltComponent.speed)));
                yield return instr;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ConnGizmoRenderer), "AddBlueprintBeltMajorPoint")]
    [HarmonyPatch(typeof(ConnGizmoRenderer), "AddBlueprintBeltPoint")]
    [HarmonyPatch(typeof(ConnGizmoRenderer), "AddBlueprintBeltConn")]
    public static void ConnGizmoRenderer_AddBlueprintBelt_Prefix(ref ConnGizmoRenderer __instance, ref uint color)
    {
        var bspeed = Patch.Cfg.BeltSpeed;
        color = color >= bspeed[2] ? 3u : color >= bspeed[1] ? 2u : 1u;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConnGizmoRenderer), "Update")]
    public static IEnumerable<CodeInstruction> ConnGizmoRenderer_Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var lastIsLdcI4_3 = false;
        foreach (var instr in instructions)
        {
            if (lastIsLdcI4_3)
            {
                lastIsLdcI4_3 = false;
                yield return instr;
                if (instr.opcode == OpCodes.Stfld && instr.OperandIs(AccessTools.Field(typeof(ConnGizmoObj), nameof(ConnGizmoObj.color))))
                {
                    var label1 = generator.DefineLabel();
                    var label2 = generator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return LdcInstrs[Patch.Cfg.BeltSpeed[1]];
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, label1);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    yield return new CodeInstruction(OpCodes.Stfld,
                        AccessTools.Field(typeof(ConnGizmoObj), nameof(ConnGizmoObj.color)));
                    yield return new CodeInstruction(OpCodes.Br, label2);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6).WithLabels(label1);
                    yield return LdcInstrs[Patch.Cfg.BeltSpeed[0]];
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, label2);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Stfld,
                        AccessTools.Field(typeof(ConnGizmoObj), nameof(ConnGizmoObj.color)));
                    yield return new CodeInstruction(OpCodes.Nop).WithLabels(label2);
                }
            }
            else
            {
                lastIsLdcI4_3 = instr.opcode == OpCodes.Ldc_I4_3;
                yield return instr;
            }
        }
    }
}
