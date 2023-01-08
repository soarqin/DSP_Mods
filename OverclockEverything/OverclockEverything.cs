using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OverclockEverything;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Patch : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    public static uint[] beltSpeed = {
        2, 5, 10
    };

    private static bool initialized = false;
    private static int sorterSpeedMultiplier = 2;
    private static int sorterPowerConsumptionMultiplier = 2;
    private static int assembleSpeedMultiplier = 2;
    private static int assemblePowerConsumptionMultiplier = 2;
    private static int researchSpeedMultiplier = 2;
    private static int labPowerConsumptionMultiplier = 2;
    private static int minerSpeedMultiplier = 2;
    private static int minerPowerConsumptionMultiplier = 2;
    private static long powerGenerationMultiplier = 4;
    private static long powerFuelConsumptionMultiplier = 1;
    private static long powerSupplyAreaMultiplier = 2;
    private static int ejectMultiplier = 2;
    private static int siloMultiplier = 2;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        if (!_cfgEnabled) return;
        beltSpeed[0] = Config.Bind("Belt", "MkI_Speed", beltSpeed[0],
            new ConfigDescription("Speed for Belt Mk.I.\n  1: 6/s\n  2: 12/s\n  3: 15/s(Displayed as 18/s)\n  4: 20/s(Displayed as 24/s)\n  5+: 6*n/s", new AcceptableValueRange<uint>(1, 10))).Value;
        beltSpeed[1] = Config.Bind("Belt", "MkII_Speed", beltSpeed[1],
            new ConfigDescription("Speed for Belt Mk.II", new AcceptableValueRange<uint>(1, 10))).Value;
        beltSpeed[2] = Config.Bind("Belt", "MkIII_Speed", beltSpeed[2],
            new ConfigDescription("Speed for Belt Mk.III", new AcceptableValueRange<uint>(1, 10))).Value;
        sorterSpeedMultiplier = Config.Bind("Sorter", "SpeedMultiplier", sorterSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Sorters", new AcceptableValueRange<int>(1, 5))).Value;
        sorterPowerConsumptionMultiplier = Config.Bind("Sorter", "PowerConsumptionMultiplier", sorterPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        assembleSpeedMultiplier = Config.Bind("Assemble", "SpeedMultiplier", assembleSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters, Assembling Machines and Lab Matrices", new AcceptableValueRange<int>(1, 10))).Value;
        assemblePowerConsumptionMultiplier = Config.Bind("Assemble", "PowerConsumptionMultiplier", assemblePowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        researchSpeedMultiplier = Config.Bind("Lab", "SpeedMultiplier", researchSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Lab Researches", new AcceptableValueRange<int>(1, 10))).Value;
        labPowerConsumptionMultiplier = Config.Bind("Lab", "PowerConsumptionMultiplier", labPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Labs", new AcceptableValueRange<int>(1, 100))).Value;
        minerSpeedMultiplier = Config.Bind("Miner", "SpeedMultiplier", minerSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 10))).Value;
        minerPowerConsumptionMultiplier = Config.Bind("Miner", "PowerConsumptionMultiplier", minerPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        powerGenerationMultiplier = Config.Bind("Power", "GenerationMultiplier", powerGenerationMultiplier,
            new ConfigDescription("Power generation multiplier for all power providers", new AcceptableValueRange<long>(1, 100))).Value;
        powerFuelConsumptionMultiplier = Config.Bind("Power", "FuelConsumptionMultiplier", powerFuelConsumptionMultiplier,
            new ConfigDescription("Fuel consumption multiplier for all fuel-consuming power providers", new AcceptableValueRange<long>(1, 10))).Value;
        powerSupplyAreaMultiplier = Config.Bind("Power", "SupplyAreaMultiplier", powerSupplyAreaMultiplier,
            new ConfigDescription("Connection length and supply area radius multiplier for power providers", new AcceptableValueRange<long>(1, 10))).Value;
        ejectMultiplier = Config.Bind("DysonSphere", "EjectMultiplier", ejectMultiplier,
            new ConfigDescription("Speed multiplier for EM-Rail Ejectors", new AcceptableValueRange<int>(1, 10))).Value;
        siloMultiplier = Config.Bind("DysonSphere", "SiloMultiplier", siloMultiplier,
            new ConfigDescription("Speed multiplier for Rocket Silos", new AcceptableValueRange<int>(1, 10))).Value;
        Harmony.CreateAndPatchAll(typeof(Patch));
        Harmony.CreateAndPatchAll(typeof(BeltFix));
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(LabComponent), "SetFunction")]
    private static IEnumerable<CodeInstruction> LabComponent_SetFunction_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var lastIsLdc10000 = false;
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_I4 && instr.OperandIs(10000))
            {
                lastIsLdc10000 = true;
            }
            else
            {
                if (lastIsLdc10000)
                {
                    lastIsLdc10000 = false;
                    if (instr.opcode == OpCodes.Stfld)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4, assembleSpeedMultiplier);
                        yield return new CodeInstruction(OpCodes.Mul);
                    }
                }
            }
            yield return instr;
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaForge), "GameTick")]
    private static IEnumerable<CodeInstruction> MechaForge_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_R4 && instr.OperandIs(10000f))
            {
                yield return new CodeInstruction(OpCodes.Ldc_R4, 10000f * assembleSpeedMultiplier);
            }
            else
            {
                yield return instr;
            }
        }
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        if (initialized) return;
        initialized = true;
        // Belts
        LDB.items.Select(2001).prefabDesc.beltSpeed = (int)beltSpeed[0];
        LDB.items.Select(2002).prefabDesc.beltSpeed = (int)beltSpeed[1];
        LDB.items.Select(2003).prefabDesc.beltSpeed = (int)beltSpeed[2];
        foreach (var proto in LDB.recipes.dataArray)
        {
            if (proto.Type == ERecipeType.Fractionate)
            {
                for (int i = 0; i < proto.ItemCounts.Length; i++)
                {
                    proto.ItemCounts[i] *= assembleSpeedMultiplier;
                }
            }
        }

        foreach (var proto in LDB.items.dataArray)
        {
            var prefabDesc = proto.prefabDesc;
            if (prefabDesc.isInserter)
            {
                prefabDesc.inserterSTT /= sorterSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= sorterPowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= sorterPowerConsumptionMultiplier;
            }
            if (prefabDesc.isLab)
            {
                prefabDesc.labAssembleSpeed *= assembleSpeedMultiplier;
                prefabDesc.labResearchSpeed *= researchSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= labPowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= labPowerConsumptionMultiplier;
            }
            if (prefabDesc.isAssembler)
            {
                prefabDesc.assemblerSpeed *= assembleSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= assemblePowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= assemblePowerConsumptionMultiplier;
            }
            if (prefabDesc.minerType != EMinerType.None)
            {
                prefabDesc.minerPeriod /= minerSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= minerPowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= minerPowerConsumptionMultiplier;
            }
            if (prefabDesc.isPowerGen || prefabDesc.isPowerExchanger || prefabDesc.isAccumulator)
            {
                prefabDesc.genEnergyPerTick *= powerGenerationMultiplier;
                prefabDesc.useFuelPerTick *= powerFuelConsumptionMultiplier;
                if (prefabDesc.isPowerConsumer)
                {
                    prefabDesc.idleEnergyPerTick *= powerGenerationMultiplier;
                    prefabDesc.workEnergyPerTick *= powerGenerationMultiplier;
                }
                if (prefabDesc.isPowerExchanger)
                {
                    prefabDesc.exchangeEnergyPerTick *= powerFuelConsumptionMultiplier;
                }
                if (prefabDesc.isAccumulator)
                {
                    prefabDesc.maxAcuEnergy *= powerGenerationMultiplier;
                    prefabDesc.inputEnergyPerTick *= powerGenerationMultiplier;
                    prefabDesc.outputEnergyPerTick *= powerGenerationMultiplier;
                }
            }
            if (prefabDesc.isPowerNode)
            {
                var ival = Mathf.Floor(prefabDesc.powerConnectDistance);
                prefabDesc.powerConnectDistance =
                    ival * powerSupplyAreaMultiplier + (prefabDesc.powerConnectDistance - ival);
                ival = Mathf.Floor(prefabDesc.powerCoverRadius);
                prefabDesc.powerCoverRadius =
                    ival * powerSupplyAreaMultiplier + (prefabDesc.powerCoverRadius - ival);
            }
            if (prefabDesc.isEjector)
            {
                prefabDesc.ejectorChargeFrame /= ejectMultiplier;
                prefabDesc.ejectorColdFrame /= ejectMultiplier;
            }
            if (prefabDesc.isSilo)
            {
                prefabDesc.siloChargeFrame /= siloMultiplier;
                prefabDesc.siloColdFrame /= siloMultiplier;
            }
        }
    }
}
