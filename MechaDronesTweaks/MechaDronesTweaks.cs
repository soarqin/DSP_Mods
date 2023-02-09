using System;
using System.Collections.Generic;
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
        MechaDronesTweaks.RemoveSpeedLimitForStage1 = Config.Bind("MechaDrones", "RemoveSpeedLimitForStage1",
            MechaDronesTweaks.RemoveSpeedLimitForStage1,
            "Remove speed limit for 1st stage (flying away from mecha in ~1/3 speed for several frames)").Value;
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
                new AcceptableValueRange<float>(0.01f, 1f))).Value;

        _harmony.PatchAll(typeof(MechaDronesTweaks));
    }
}

[HarmonyPatch]
public static class MechaDronesTweaks
{
    public static bool UseFixedSpeed = false;
    public static bool RemoveSpeedLimitForStage1 = true;
    public static float FixedSpeed = 500f;
    public static float SpeedMultiplier = 5f;
    public static float EnergyMultiplier = 0.1f;

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaDroneLogic), "UpdateDrones")]
    private static IEnumerable<CodeInstruction> MechaUpdateDrones_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            yield return instr;
            if (instr.opcode == OpCodes.Ldfld && instr.OperandIs(AccessTools.Field(typeof(Mecha), "droneEnergyPerMeter")))
            {
                yield return new CodeInstruction(OpCodes.Ldc_R8, (double)EnergyMultiplier);
                yield return new CodeInstruction(OpCodes.Mul);
            }
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaDrone), "Update")]
    private static IEnumerable<CodeInstruction> MechaDroneUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var lastIsLdarg0 = false;
        CodeInstruction lastInstr = null;
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldarg_0)
            {
                if (lastIsLdarg0)
                {
                    yield return lastInstr;
                    continue;
                }

                lastIsLdarg0 = true;
                lastInstr = instr;
                continue;
            }

            if (lastIsLdarg0)
            {
                lastIsLdarg0 = false;
                if (instr.opcode == OpCodes.Ldfld &&
                    instr.OperandIs(AccessTools.Field(typeof(MechaDrone), "speed")))
                {
                    if (UseFixedSpeed)
                    {
                        lastInstr.opcode = OpCodes.Ldc_R4;
                        lastInstr.operand = FixedSpeed;
                        yield return lastInstr;
                    }
                    else
                    {
                        yield return lastInstr;
                        yield return instr;
                        yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedMultiplier);
                        yield return new CodeInstruction(OpCodes.Mul);
                    }

                    continue;
                }

                yield return lastInstr;
                yield return instr;
                continue;
            }

            if (instr.opcode == OpCodes.Ldc_R4)
            {
                if (instr.OperandIs(0.5f))
                {
                    if (FixedSpeed > 59f)
                    {
                        instr.operand = 0.5f * FixedSpeed / 50f;
                        yield return instr;
                        continue;
                    }
                }
                else if (instr.OperandIs(3f))
                {
                    if (RemoveSpeedLimitForStage1)
                    {
                        instr.operand = 10000f;
                    }

                    yield return instr;
                    continue;
                }
            }

            yield return instr;
        }
    }
}