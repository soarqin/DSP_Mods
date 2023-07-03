using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace MechaDronesTweaks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(FastDronesRemover.FastDronesGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class MechaDronesTweaksPlugin : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

    public MechaDronesTweaksPlugin()
    {
        /* Remove FastDrones MOD if loaded */
        try
        {
            if (FastDronesRemover.Run(_harmony))
            {
                Logger.LogInfo("Unpatch FastDrones - OK");
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning($"Failed to unpatch FastDrones: {e}");
        }
    }

    public void Awake()
    {
        MechaDronesTweaks.UseFixedSpeed = Config.Bind("MechaDrones", "UseFixedSpeed", MechaDronesTweaks.UseFixedSpeed,
            "Use fixed speed for mecha drones").Value;
        MechaDronesTweaks.SkipStage1 = Config.Bind("MechaDrones", "SkipStage1",
            MechaDronesTweaks.SkipStage1,
            "Skip 1st stage of working mecha drones (flying away from mecha in ~1/3 speed for several frames)").Value;
        MechaDronesTweaks.RemoveSpeedLimitForStage1 = Config.Bind("MechaDrones", "RemoveSpeedLimitForStage1",
            MechaDronesTweaks.RemoveSpeedLimitForStage1,
            "Remove speed limit for 1st stage (has a speed limit @ ~10m/s originally)").Value;
        MechaDronesTweaks.FixedSpeed = Config.Bind("MechaDrones", "FixedSpeed", MechaDronesTweaks.FixedSpeed,
            new ConfigDescription("Fixed speed for mecha drones, working only when UseFixedSpeed is enabled",
                new AcceptableValueRange<float>(6f, 1000f))).Value;
        MechaDronesTweaks.SpeedMultiplier = Config.Bind("MechaDrones", "SpeedMultiplier",
            MechaDronesTweaks.SpeedMultiplier,
            new ConfigDescription("Speed multiplier for mecha drones, working only when UseFixedSpeed is disabled",
                new AcceptableValueRange<float>(1f, 10f))).Value;
        MechaDronesTweaks.EnergyMultiplier = Config.Bind("MechaDrones", "EnergyMultiplier",
            MechaDronesTweaks.EnergyMultiplier,
            new ConfigDescription("Energy consumption multiplier for mecha drones",
                new AcceptableValueRange<float>(0f, 1f))).Value;
        MechaDronesTweaks.EnergyMultiplier = Config.Bind("MechaDrones", "EnergyMultiplier",
            MechaDronesTweaks.EnergyMultiplier,
            new ConfigDescription("Energy consumption multiplier for mecha drones",
                new AcceptableValueRange<float>(0f, 1f))).Value;

        _harmony.PatchAll(typeof(MechaDronesTweaks));
    }
}

public static class MechaDronesTweaks
{
    public static bool UseFixedSpeed = false;
    public static bool SkipStage1 = false;
    public static bool RemoveSpeedLimitForStage1 = true;
    public static float FixedSpeed = 300f;
    public static float SpeedMultiplier = 4f;
    public static float EnergyMultiplier = 0.1f;

    [HarmonyTranspiler, HarmonyPatch(typeof(UITechTree), "RefreshDataValueText")]
    private static IEnumerable<CodeInstruction> UITechTreeRefreshDataValueText_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Callvirt &&
                instr.OperandIs(AccessTools.Method(typeof(Mecha), "get_droneSpeed")))
            {
                if (UseFixedSpeed)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, FixedSpeed);
                }
                else
                {
                    yield return instr;
                    yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedMultiplier);
                    yield return new CodeInstruction(OpCodes.Mul);
                }
            }
            else
            {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaDroneLogic), "UpdateTargets")]
    private static IEnumerable<CodeInstruction> MechaUpdateTargets_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (SkipStage1)
        {
            var ilist = instructions.ToList();
            for (var i = 0; i < ilist.Count; i++)
            {
                var instr = ilist[i];
                if (instr.opcode == OpCodes.Ldc_I4_1)
                {
                    var instrNext = ilist[i + 1];
                    if (instrNext.opcode == OpCodes.Stfld &&
                        instrNext.OperandIs(AccessTools.Field(typeof(MechaDrone), "stage")))
                    {
                        instr.opcode = OpCodes.Ldc_I4_2;
                    }
                }

                yield return instr;
            }
        }
        else
        {
            foreach (var instr in instructions)
            {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaDroneLogic), "UpdateDrones")]
    private static IEnumerable<CodeInstruction> MechaUpdateDrones_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (EnergyMultiplier >= 1f)
        {
            foreach (var instr in instructions)
            {
                yield return instr;
            }
        }
        else
        {
            foreach (var instr in instructions)
            {
                yield return instr;
                if (instr.opcode != OpCodes.Ldfld ||
                    !instr.OperandIs(AccessTools.Field(typeof(Mecha), "droneEnergyPerMeter"))) continue;
                yield return new CodeInstruction(OpCodes.Ldc_R8, (double)EnergyMultiplier);
                yield return new CodeInstruction(OpCodes.Mul);
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaDrone), "Update")]
    private static IEnumerable<CodeInstruction> MechaDroneUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var ilist = instructions.ToList();
        for (var i = 0; i < ilist.Count; i++)
        {
            var instr = ilist[i];
            if (instr.opcode == OpCodes.Ldarg_0)
            {
                var instrNext = ilist[i + 1];
                if (instrNext.opcode == OpCodes.Ldfld &&
                    instrNext.OperandIs(AccessTools.Field(typeof(MechaDrone), "speed")))
                {
                    if (UseFixedSpeed)
                    {
                        var newInstr = new CodeInstruction(instr)
                        {
                            opcode = OpCodes.Ldc_R4,
                            operand = FixedSpeed
                        };
                        yield return newInstr;
                    }
                    else
                    {
                        yield return instr;
                        yield return instrNext;
                        yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedMultiplier);
                        yield return new CodeInstruction(OpCodes.Mul);
                    }

                    i++;
                    continue;
                }

                if (instrNext.opcode == OpCodes.Ldc_R4)
                {
                    if (instrNext.OperandIs(0f))
                    {
                        var instrNext2 = ilist[i + 2];
                        if (instrNext2.opcode == OpCodes.Stfld &&
                            instrNext2.OperandIs(AccessTools.Field(typeof(MechaDrone), "progress")))
                        {
                            ilist[i + 3].labels = instr.labels;
                            i += 2;
                            continue;
                        }
                    }
                    else if (instrNext.OperandIs(1f))
                    {
                        var instrNext2 = ilist[i + 2];
                        if (instrNext2.opcode == OpCodes.Stfld &&
                            instrNext2.OperandIs(AccessTools.Field(typeof(MechaDrone), "progress")))
                        {
                            instrNext.operand = 0f;
                            yield return instr;
                            yield return instrNext;
                            yield return instrNext2;
                            i += 2;
                            continue;
                        }
                    }
                }
            }
            else if (instr.opcode == OpCodes.Ldc_R4)
            {
                if (instr.OperandIs(0.5f))
                {
                    if (UseFixedSpeed)
                    {
                        if (FixedSpeed > 75f)
                        {
                            instr.operand = 0.5f * FixedSpeed / 75f;
                        }
                    }
                    else
                    {
                        instr.operand = 0.5f * SpeedMultiplier;
                    }
                }
                else if (instr.OperandIs(3f))
                {
                    if (RemoveSpeedLimitForStage1)
                    {
                        instr.operand = 10000f;
                    }
                }
            }

            yield return instr;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
    [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
    private static IEnumerable<CodeInstruction> BuildTools_CursorSizePatch_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(11))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 31);
            }
            else
            {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
    private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var ilist = instructions.ToList();
        for (var i = 0; i < ilist.Count; i++)
        {
            var instr = ilist[i];
            if (instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(10) &&
                (ilist[i - 1].opcode == OpCodes.Ldfld &&
                 ilist[i - 1].OperandIs(AccessTools.Field(typeof(BuildTool_Reform), "brushSize"))
                 ||
                 ilist[i + 1].opcode == OpCodes.Stfld &&
                 ilist[i + 1].OperandIs(AccessTools.Field(typeof(BuildTool_Reform), "brushSize")))
               )
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 30);
            }
            else
            {
                yield return instr;
            }
        }
    }
   
    
    [HarmonyTranspiler, HarmonyPatch(typeof(ConnGizmoGraph), MethodType.Constructor)]
    private static IEnumerable<CodeInstruction> ConnGizmoGraph_Constructor_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_I4 && instr.OperandIs(256))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, 2048);
            }
            else
            {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(ConnGizmoGraph), nameof(ConnGizmoGraph.SetPointCount))]
    private static IEnumerable<CodeInstruction> ConnGizmoGraph_SetPointCount_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_I4 && instr.OperandIs(256))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, 2048);
            }
            else
            {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path._OnInit))]
    private static IEnumerable<CodeInstruction> BuildTool_Path__OnInit_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_I4 && instr.OperandIs(160))
            {
                instr.operand = 2048;
            }

            yield return instr;
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnInit))]
    private static IEnumerable<CodeInstruction> BuildTool_Click__OnInit_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(15))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, 512);
            }
            else
            {
                yield return instr;
            }
        }
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
    private static IEnumerable<CodeInstruction> BuildAreaElimination_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        /* Patch (player.mecha.buildArea * player.mecha.buildArea) to 100000000 */
        var ilist = instructions.ToList();
        var count = ilist.Count - 8;
        int i;
        for (i = 0; i < count; i++)
        {
            var found = false;
            while (true)
            {
                var instr = ilist[i + 1];
                if (instr.opcode != OpCodes.Call ||
                    !instr.OperandIs(AccessTools.Method(typeof(BuildTool), "get_player")))
                {
                    break;
                }

                instr = ilist[i + 2];
                if (instr.opcode != OpCodes.Callvirt ||
                    !instr.OperandIs(AccessTools.Method(typeof(Player), "get_mecha")))
                {
                    break;
                }

                instr = ilist[i + 3];
                if (instr.opcode != OpCodes.Ldfld || !instr.OperandIs(AccessTools.Field(typeof(Mecha), "buildArea")))
                {
                    break;
                }

                instr = ilist[i + 5];
                if (instr.opcode != OpCodes.Call ||
                    !instr.OperandIs(AccessTools.Method(typeof(BuildTool), "get_player")))
                {
                    break;
                }

                instr = ilist[i + 6];
                if (instr.opcode != OpCodes.Callvirt ||
                    !instr.OperandIs(AccessTools.Method(typeof(Player), "get_mecha")))
                {
                    break;
                }

                instr = ilist[i + 7];
                if (instr.opcode != OpCodes.Ldfld || !instr.OperandIs(AccessTools.Field(typeof(Mecha), "buildArea")))
                {
                    break;
                }

                instr = ilist[i + 8];
                if (instr.opcode != OpCodes.Mul)
                {
                    break;
                }

                found = true;
                yield return new CodeInstruction(OpCodes.Ldc_R4, 100000000.0f);
                break;
            }

            if (found)
            {
                i += 8;
            }
            else
            {
                yield return ilist[i];
            }
        }

        for (; i < ilist.Count; i++)
        {
            yield return ilist[i];
        }
    }
}