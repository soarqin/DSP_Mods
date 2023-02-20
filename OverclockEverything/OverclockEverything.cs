using System;
using System.Collections.Generic;
using System.Linq;
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
    private static int inventoryStackMultiplier = 5;

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
        inventoryStackMultiplier = Config.Bind("Inventory", "StackMultiplier", inventoryStackMultiplier,
            new ConfigDescription("Stack count multiplier for inventory items", new AcceptableValueRange<int>(1, 10))).Value;
        Harmony.CreateAndPatchAll(typeof(Patch));
        Harmony.CreateAndPatchAll(typeof(BeltFix));
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Path), "CheckBuildConditions")]
    private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_R4 && instr.OperandIs(0.28f))
            {
                instr.operand = 0.21f;
            }
            yield return instr;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIReplicatorWindow), "OnPlusButtonClick")]
    [HarmonyPatch(typeof(UIReplicatorWindow), "OnMinusButtonClick")]
    private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnPlusButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var ilist = instructions.ToList();
        for (var i = 0; i < ilist.Count; i++)
        {
            var instr = ilist[i];
            if (instr.opcode == OpCodes.Ldloc_0 && ilist[i + 1].opcode == OpCodes.Ldc_I4_1 && (ilist[i + 2].opcode == OpCodes.Add || ilist[i + 2].opcode == OpCodes.Sub))
            {
                var label1 = generator.DefineLabel();
                var label2 = generator.DefineLabel();
                var label3 = generator.DefineLabel();
                var label4 = generator.DefineLabel();
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), "control"));
                yield return new CodeInstruction(OpCodes.Brfalse_S, label1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 10);
                yield return new CodeInstruction(OpCodes.Br_S, label4);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), "shift")).WithLabels(label1);
                yield return new CodeInstruction(OpCodes.Brfalse_S, label2);
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 100);
                yield return new CodeInstruction(OpCodes.Br_S, label4);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), "alt")).WithLabels(label2);
                yield return new CodeInstruction(OpCodes.Brfalse_S, label3);
                yield return new CodeInstruction(OpCodes.Ldc_I4, 1000);
                yield return new CodeInstruction(OpCodes.Br_S, label4);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(label3);
                ilist[i + 2] = ilist[i + 2].WithLabels(label4);
                i++;
                continue;
            }

            if (instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(10))
            {
                instr.opcode = OpCodes.Ldc_I4;
                instr.operand = 1000;
            }
            yield return instr;
        }
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

    [HarmonyPrefix, HarmonyPatch(typeof(StorageComponent), "LoadStatic")]
    private static bool StorageComponent_LoadStatic_Prefix()
    {
        if (StorageComponent.staticLoaded) return false;
        foreach (var proto in LDB.items.dataArray)
        {
            proto.StackSize *= inventoryStackMultiplier;
        }
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StorageComponent), "Import")]
    private static void StorageComponent_Import_Postfix(StorageComponent __instance)
    {
        if (inventoryStackMultiplier <= 1) return;
        var size = __instance.size;
        for (var i = 0; i < size; i++)
        {
            var itemId = __instance.grids[i].itemId;
            if (itemId != 0)
            {
                __instance.grids[i].stackSize = StorageComponent.itemStackCount[itemId];
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
            FixExtValue(ref prefabDesc.buildCollider.ext.x);
            FixExtValue(ref prefabDesc.buildCollider.ext.y);
            FixExtValue(ref prefabDesc.buildCollider.ext.z);
            if (prefabDesc.buildColliders != null)
            {
                for (var i = 0; i < prefabDesc.buildColliders.Length; i++)
                {
                    FixExtValue(ref prefabDesc.buildColliders[i].ext.x);
                    FixExtValue(ref prefabDesc.buildColliders[i].ext.y);
                    FixExtValue(ref prefabDesc.buildColliders[i].ext.z);
                }
            }
            if (prefabDesc.isInserter)
            {
                prefabDesc.inserterSTT /= sorterSpeedMultiplier;
                prefabDesc.inserterDelay /= sorterSpeedMultiplier;
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

    private static void FixExtValue(ref float v)
    {
        if (v == 0f)
        {
            return;
        }
        var b = Math.Abs(v);
        v = (v - b) * 0.75f + b;
    }
}
