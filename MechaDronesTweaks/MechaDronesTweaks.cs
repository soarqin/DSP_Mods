using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

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
    public static bool UseFixedSpeed;
    public static bool SkipStage1;
    public static bool RemoveSpeedLimitForStage1 = true;
    public static float FixedSpeed = 300f;
    public static float SpeedMultiplier = 4f;
    public static float EnergyMultiplier = 0.1f;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConstructionSystem), nameof(ConstructionSystem.UpdateDrones))]
    [HarmonyPatch(typeof(UIMechaWindow), nameof(UIMechaWindow.UpdateProps))]
    [HarmonyPatch(typeof(UITechTree), nameof(UITechTree.RefreshDataValueText))]
    private static IEnumerable<CodeInstruction> UITechTreeRefreshDataValueText_Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.constructionDroneSpeed)))
        );
        if (UseFixedSpeed)
        {
            matcher.Repeat(m => m.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_R4, FixedSpeed)
            ));
        }
        else
        {
            matcher.Repeat(m => m.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_R4, SpeedMultiplier),
                new CodeInstruction(OpCodes.Mul)
            ));
        }
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.EjectBaseDrone))]
    [HarmonyPatch(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.EjectMechaDrone))]
    private static IEnumerable<CodeInstruction> MechaUpdateTargets_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        if (!SkipStage1)
            return matcher.InstructionEnumeration();
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4_1),
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DroneComponent), nameof(DroneComponent.stage)))
        ).Operand = 2;
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(ConstructionSystem), nameof(ConstructionSystem.UpdateDrones))]
    private static IEnumerable<CodeInstruction> MechaUpdateDrones_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        if (EnergyMultiplier >= 1f)
            return matcher.InstructionEnumeration();
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ModeConfig), nameof(ModeConfig.droneEnergyPerMeter)))
        ).Advance(1).Insert(
            new CodeInstruction(OpCodes.Ldc_R8, (double)EnergyMultiplier),
            new CodeInstruction(OpCodes.Mul)
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    // ref CraftData craft, PlanetFactory factory, ref Vector3 ejectPos, float droneSpeed, float dt, ref double mechaEnergy, ref double mechaEnergyChange, double flyEnergyRate, double repairEnergyCost, out float energyRatio
    [HarmonyPatch(typeof(DroneComponent), nameof(DroneComponent.InternalUpdate), new[] { typeof(CraftData), typeof(PlanetFactory), typeof(Vector3), typeof(float), typeof(float), typeof(double), typeof(double), typeof(double), typeof(double), typeof(float) }, new[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    // ref CraftData craft, PlanetFactory factory, Vector3 ejectPos, float droneSpeed, float dt, ref long energy, double flyEnergyRate, double repairEnergyCost, out float energyRatio
    [HarmonyPatch(typeof(DroneComponent), nameof(DroneComponent.InternalUpdate), new[] { typeof(CraftData), typeof(PlanetFactory), typeof(Vector3), typeof(float), typeof(float), typeof(long), typeof(double), typeof(double), typeof(float) }, new[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    private static IEnumerable<CodeInstruction> MechaDroneUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);

        if (RemoveSpeedLimitForStage1)
        {
            matcher.MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldc_R4 && instr.OperandIs(1f)),
                new CodeMatch(instr => instr.opcode == OpCodes.Ldc_R4 && instr.OperandIs(3f))
            );
            matcher.Advance(1).Operand = 10000f;
        }

        if (!UseFixedSpeed && Math.Abs(SpeedMultiplier - 1.0f) < 0.01f)
            return matcher.InstructionEnumeration();

        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldc_R4),
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DroneComponent), nameof(DroneComponent.progress)))
        );
        matcher.Repeat(m =>
            {
                if (m.InstructionAt(1).OperandIs(0f))
                {
                    m.InstructionAt(3).labels = m.Labels;
                    m.RemoveInstructions(3);
                }
                else if (m.InstructionAt(1).OperandIs(1f))
                {
                    m.Advance(1).Operand = 0f;
                    m.Advance(2);
                }
            }
        );
        matcher.Start().MatchForward(false,
            new CodeMatch(instr => instr.opcode == OpCodes.Ldc_R4 && instr.OperandIs(0.5f))
        );
        matcher.Repeat(m =>
            {
                if (UseFixedSpeed)
                {
                    if (FixedSpeed > 75f)
                    {
                        m.Operand = 0.5f * FixedSpeed / 75f;
                    }
                }
                else
                {
                    m.Operand = 0.5f * SpeedMultiplier;
                }
            }
        );
        return matcher.InstructionEnumeration();
    }
}