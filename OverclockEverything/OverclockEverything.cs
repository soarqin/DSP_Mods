using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using crecheng.DSPModSave;
using HarmonyLib;
using UnityEngine;

namespace OverclockEverything;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(DSPModSavePlugin.MODGUID)]
public class Patch : BaseUnityPlugin, IModCanSave
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private const ushort SaveVersion = 1;
    private static bool _initialized;
    public static Cfg Cfg = new();
    private static Cfg _oldCfg;

    private void Awake()
    {
        var cfgEnabled = Config.Bind("General", "Enabled", true, "Enable/Disable this plugin").Value;
        if (!cfgEnabled) return;
        Cfg.BeltSpeed[0] = Config.Bind("Belt", "MkI_Speed", Cfg.BeltSpeed[0],
            new ConfigDescription("Speed for Belt Mk.I.\n  1: 6/s\n  2: 12/s\n  3: 15/s(Displayed as 18/s)\n  4: 20/s(Displayed as 24/s)\n  5+: 6*n/s", new AcceptableValueRange<uint>(1, 10))).Value;
        Cfg.BeltSpeed[1] = Config.Bind("Belt", "MkII_Speed", Cfg.BeltSpeed[1],
            new ConfigDescription("Speed for Belt Mk.II", new AcceptableValueRange<uint>(1, 10))).Value;
        Cfg.BeltSpeed[2] = Config.Bind("Belt", "MkIII_Speed", Cfg.BeltSpeed[2],
            new ConfigDescription("Speed for Belt Mk.III", new AcceptableValueRange<uint>(1, 10))).Value;
        Cfg.SorterSpeedMultiplier = Config.Bind("Sorter", "SpeedMultiplier", Cfg.SorterSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Sorters", new AcceptableValueRange<int>(1, 5))).Value;
        Cfg.SorterPowerConsumptionMultiplier = Config.Bind("Sorter", "PowerConsumptionMultiplier", Cfg.SorterPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        Cfg.AssembleSpeedMultiplier = Config.Bind("Assemble", "SpeedMultiplier", Cfg.AssembleSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters, Assembling Machines and Lab Matrices", new AcceptableValueRange<int>(1, 10))).Value;
        Cfg.AssemblePowerConsumptionMultiplier = Config.Bind("Assemble", "PowerConsumptionMultiplier", Cfg.AssemblePowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        Cfg.ResearchSpeedMultiplier = Config.Bind("Lab", "SpeedMultiplier", Cfg.ResearchSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Lab Researches", new AcceptableValueRange<int>(1, 10))).Value;
        Cfg.LabPowerConsumptionMultiplier = Config.Bind("Lab", "PowerConsumptionMultiplier", Cfg.LabPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Labs", new AcceptableValueRange<int>(1, 100))).Value;
        Cfg.MinerSpeedMultiplier = Config.Bind("Miner", "SpeedMultiplier", Cfg.MinerSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 10))).Value;
        Cfg.MinerPowerConsumptionMultiplier = Config.Bind("Miner", "PowerConsumptionMultiplier", Cfg.MinerPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        Cfg.PowerGenerationMultiplier = Config.Bind("Power", "GenerationMultiplier", Cfg.PowerGenerationMultiplier,
            new ConfigDescription("Power generation multiplier for all power providers", new AcceptableValueRange<long>(1, 100))).Value;
        Cfg.PowerFuelConsumptionMultiplier = Config.Bind("Power", "FuelConsumptionMultiplier", Cfg.PowerFuelConsumptionMultiplier,
            new ConfigDescription("Fuel consumption multiplier for all fuel-consuming power providers", new AcceptableValueRange<long>(1, 10))).Value;
        Cfg.PowerSupplyAreaMultiplier = Config.Bind("Power", "SupplyAreaMultiplier", Cfg.PowerSupplyAreaMultiplier,
            new ConfigDescription("Connection length and supply area radius multiplier for power providers", new AcceptableValueRange<long>(1, 10))).Value;
        Cfg.EjectMultiplier = Config.Bind("DysonSphere", "EjectMultiplier", Cfg.EjectMultiplier,
            new ConfigDescription("Speed multiplier for EM-Rail Ejectors", new AcceptableValueRange<int>(1, 10))).Value;
        Cfg.SiloMultiplier = Config.Bind("DysonSphere", "SiloMultiplier", Cfg.SiloMultiplier,
            new ConfigDescription("Speed multiplier for Rocket Silos", new AcceptableValueRange<int>(1, 10))).Value;
        Harmony.CreateAndPatchAll(typeof(Patch));
        Harmony.CreateAndPatchAll(typeof(BeltFix));
    }

    public void Export(BinaryWriter w)
    {
        w.Write(SaveVersion);
        w.Write(Cfg.SorterSpeedMultiplier);
        w.Write(Cfg.SorterPowerConsumptionMultiplier);
        w.Write(Cfg.AssembleSpeedMultiplier);
        w.Write(Cfg.AssemblePowerConsumptionMultiplier);
        w.Write(Cfg.ResearchSpeedMultiplier);
        w.Write(Cfg.LabPowerConsumptionMultiplier);
        w.Write(Cfg.MinerSpeedMultiplier);
        w.Write(Cfg.MinerPowerConsumptionMultiplier);
        w.Write(Cfg.PowerGenerationMultiplier);
        w.Write(Cfg.PowerFuelConsumptionMultiplier);
        w.Write(Cfg.PowerSupplyAreaMultiplier);
        w.Write(Cfg.EjectMultiplier);
        w.Write(Cfg.SiloMultiplier);
    }

    public void Import(BinaryReader r)
    {
        var version = r.ReadUInt16();
        if (version <= 0) return;

        _oldCfg.SorterSpeedMultiplier = r.ReadInt32();
        _oldCfg.SorterPowerConsumptionMultiplier = r.ReadInt32();
        _oldCfg.AssembleSpeedMultiplier = r.ReadInt32();
        _oldCfg.AssemblePowerConsumptionMultiplier = r.ReadInt32();
        _oldCfg.ResearchSpeedMultiplier = r.ReadInt32();
        _oldCfg.LabPowerConsumptionMultiplier = r.ReadInt32();
        _oldCfg.MinerSpeedMultiplier = r.ReadInt32();
        _oldCfg.MinerPowerConsumptionMultiplier = r.ReadInt32();
        _oldCfg.PowerGenerationMultiplier = r.ReadInt64();
        _oldCfg.PowerFuelConsumptionMultiplier = r.ReadInt64();
        _oldCfg.PowerSupplyAreaMultiplier = r.ReadInt64();
        _oldCfg.EjectMultiplier = r.ReadInt32();
        _oldCfg.SiloMultiplier = r.ReadInt32();
    }

    public void IntoOtherSave()
    {
    }

    /* Belt fix for GalacticScale old versions, should be of no use now.
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
    */

    [HarmonyPostfix, HarmonyPatch(typeof(LabComponent), "SetFunction")]
    private static void LabComponent_SetFunction_Postfix(ref LabComponent __instance)
    {
        if (__instance.researchMode) return;
        __instance.speed *= Cfg.AssembleSpeedMultiplier;
        __instance.speedOverride *= Cfg.AssembleSpeedMultiplier;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MechaForge), "GameTick")]
    private static IEnumerable<CodeInstruction> MechaForge_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ldc_R4 && instr.OperandIs(10000f))
            {
                yield return new CodeInstruction(OpCodes.Ldc_R4, 10000f * Cfg.AssembleSpeedMultiplier);
            }
            else
            {
                yield return instr;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame))]
    private static void GameSave_LoadCurrentGame_Prefix()
    {
        if (DSPGame.IsMenuDemo) return;
        _oldCfg = new Cfg();
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame))]
    private static void GameSave_LoadCurrentGame_Postfix()
    {
        if (DSPGame.IsMenuDemo) return;
        var needFix = _oldCfg.SorterSpeedMultiplier != Cfg.SorterSpeedMultiplier || _oldCfg.SorterPowerConsumptionMultiplier != Cfg.SorterPowerConsumptionMultiplier ||
                      _oldCfg.AssembleSpeedMultiplier != Cfg.AssembleSpeedMultiplier || _oldCfg.AssemblePowerConsumptionMultiplier != Cfg.AssemblePowerConsumptionMultiplier ||
                      _oldCfg.ResearchSpeedMultiplier != Cfg.ResearchSpeedMultiplier || _oldCfg.LabPowerConsumptionMultiplier != Cfg.LabPowerConsumptionMultiplier ||
                      _oldCfg.MinerSpeedMultiplier != Cfg.MinerSpeedMultiplier || _oldCfg.MinerPowerConsumptionMultiplier != Cfg.MinerPowerConsumptionMultiplier ||
                      _oldCfg.PowerGenerationMultiplier != Cfg.PowerGenerationMultiplier || _oldCfg.PowerFuelConsumptionMultiplier != Cfg.PowerFuelConsumptionMultiplier ||
                      _oldCfg.PowerSupplyAreaMultiplier != Cfg.PowerSupplyAreaMultiplier || _oldCfg.EjectMultiplier != Cfg.EjectMultiplier || _oldCfg.SiloMultiplier != Cfg.SiloMultiplier;
        if (!needFix) return;
        Logger.LogInfo("Config changed, fixing parameters for builds...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach (var factory in GameMain.data.factories)
        {
            var factorySystem = factory?.factorySystem;
            if (factorySystem == null) continue;
            var powerSystem = factory.powerSystem;
            if (_oldCfg.SorterSpeedMultiplier != Cfg.SorterSpeedMultiplier)
            {
                var om = _oldCfg.SorterSpeedMultiplier;
                var nm = Cfg.SorterSpeedMultiplier;
                var om2 = _oldCfg.SorterPowerConsumptionMultiplier;
                var nm2 = Cfg.SorterPowerConsumptionMultiplier;
                for (var i = 1; i < factorySystem.inserterCursor; i++)
                {
                    ref var sorter = ref factorySystem.inserterPool[i];
                    if (sorter.id != i) continue;
                    sorter.stt = sorter.stt * om / nm;
                    if (sorter.time > sorter.stt)
                    {
                        sorter.time = sorter.stt - sorter.speed;
                    }

                    if (powerSystem == null) continue;
                    ref var entity = ref factory.entityPool[sorter.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }
            }

            if (_oldCfg.AssembleSpeedMultiplier != Cfg.AssembleSpeedMultiplier || _oldCfg.LabPowerConsumptionMultiplier != Cfg.LabPowerConsumptionMultiplier)
            {
                var om = _oldCfg.AssembleSpeedMultiplier;
                var nm = Cfg.AssembleSpeedMultiplier;
                var om2 = _oldCfg.LabPowerConsumptionMultiplier;
                var nm2 = Cfg.LabPowerConsumptionMultiplier;
                for (var i = 1; i < factorySystem.labCursor; i++)
                {
                    ref var lab = ref factorySystem.labPool[i];
                    if (lab.id != i) continue;
                    if (!lab.researchMode)
                    {
                        lab.speed = lab.speed / om * nm;
                        lab.speedOverride = lab.speedOverride / om * nm;
                    }

                    if (powerSystem == null) continue;
                    ref var entity = ref factory.entityPool[lab.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }
            }

            if (_oldCfg.AssembleSpeedMultiplier != Cfg.AssembleSpeedMultiplier || _oldCfg.AssemblePowerConsumptionMultiplier != Cfg.AssemblePowerConsumptionMultiplier)
            {
                var om = _oldCfg.AssembleSpeedMultiplier;
                var nm = Cfg.AssembleSpeedMultiplier;
                var om2 = _oldCfg.AssemblePowerConsumptionMultiplier;
                var nm2 = Cfg.AssemblePowerConsumptionMultiplier;
                for (var i = 1; i < factorySystem.assemblerCursor; i++)
                {
                    ref var assembler = ref factorySystem.assemblerPool[i];
                    if (assembler.id != i) continue;
                    assembler.speed = assembler.speed / om * nm;
                    assembler.speedOverride = assembler.speedOverride / om * nm;

                    if (powerSystem == null) continue;
                    ref var entity = ref factory.entityPool[assembler.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }
            }

            if (_oldCfg.MinerSpeedMultiplier != Cfg.MinerSpeedMultiplier || _oldCfg.MinerPowerConsumptionMultiplier != Cfg.MinerPowerConsumptionMultiplier)
            {
                var om = _oldCfg.MinerSpeedMultiplier;
                var nm = Cfg.MinerSpeedMultiplier;
                var om2 = _oldCfg.MinerPowerConsumptionMultiplier;
                var nm2 = Cfg.MinerPowerConsumptionMultiplier;
                for (var i = 1; i < factorySystem.minerCursor; i++)
                {
                    ref var miner = ref factorySystem.minerPool[i];
                    if (miner.id != i) continue;
                    miner.speed = miner.speed / om * nm;

                    if (powerSystem == null) continue;
                    ref var entity = ref factory.entityPool[miner.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }
            }

            if (powerSystem != null && (_oldCfg.PowerGenerationMultiplier != Cfg.PowerGenerationMultiplier || _oldCfg.PowerFuelConsumptionMultiplier != Cfg.PowerFuelConsumptionMultiplier))
            {
                var om = _oldCfg.PowerFuelConsumptionMultiplier;
                var nm = Cfg.PowerFuelConsumptionMultiplier;
                var om2 = _oldCfg.PowerGenerationMultiplier;
                var nm2 = Cfg.PowerGenerationMultiplier;
                Logger.LogInfo($"{om2} => {nm2}   {om} => {nm}");
                for (var i = 1; i < powerSystem.genCursor; i++)
                {
                    ref var gen = ref powerSystem.genPool[i];
                    if (gen.id != i) continue;
                    gen.genEnergyPerTick = gen.genEnergyPerTick / om2 * nm2;
                    gen.useFuelPerTick = gen.useFuelPerTick / om * nm;
                    ref var entity = ref factory.entityPool[gen.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }

                for (var i = 1; i < powerSystem.excCursor; i++)
                {
                    ref var exc = ref powerSystem.excPool[i];
                    if (exc.id != i) continue;
                    exc.energyPerTick = exc.energyPerTick / om * nm;
                    ref var entity = ref factory.entityPool[exc.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }

                for (var i = 1; i < powerSystem.accCursor; i++)
                {
                    ref var acc = ref powerSystem.accPool[i];
                    if (acc.id != i) continue;
                    acc.maxEnergy = acc.maxEnergy / om2 * nm2;
                    acc.inputEnergyPerTick = acc.inputEnergyPerTick / om2 * nm2;
                    acc.outputEnergyPerTick = acc.outputEnergyPerTick / om2 * nm2;
                    ref var entity = ref factory.entityPool[acc.entityId];
                    if (entity.powerConId <= 0) continue;
                    ref var consumer = ref powerSystem.consumerPool[entity.powerConId];
                    consumer.idleEnergyPerTick = consumer.idleEnergyPerTick / om2 * nm2;
                    consumer.workEnergyPerTick = consumer.workEnergyPerTick / om2 * nm2;
                }
            }

            if (powerSystem != null && _oldCfg.PowerSupplyAreaMultiplier != Cfg.PowerSupplyAreaMultiplier)
            {
                for (var i = 1; i < powerSystem.nodeCursor; i++)
                {
                    ref var node = ref powerSystem.nodePool[i];
                    if (node.id != i) continue;
                    ref var entity = ref factory.entityPool[node.entityId];
                    var prefabDesc = LDB.items.Select(entity.protoId).prefabDesc;
                    node.connectDistance = prefabDesc.powerConnectDistance;
                    node.coverRadius = prefabDesc.powerCoverRadius;
                }
            }

            if (_oldCfg.EjectMultiplier != Cfg.EjectMultiplier)
            {
                for (var i = 1; i < factorySystem.ejectorCursor; i++)
                {
                    ref var ejector = ref factorySystem.ejectorPool[i];
                    if (ejector.id != i) continue;
                    ref var entity = ref factory.entityPool[ejector.entityId];
                    var prefabDesc = LDB.items.Select(entity.protoId).prefabDesc;
                    ejector.chargeSpend = prefabDesc.ejectorChargeFrame * 10000;
                    ejector.coldSpend = prefabDesc.ejectorColdFrame * 10000;
                }
            }

            if (_oldCfg.SiloMultiplier != Cfg.SiloMultiplier)
            {
                for (var i = 1; i < factorySystem.siloCursor; i++)
                {
                    ref var silo = ref factorySystem.siloPool[i];
                    if (silo.id != i) continue;
                    ref var entity = ref factory.entityPool[silo.entityId];
                    var prefabDesc = LDB.items.Select(entity.protoId).prefabDesc;
                    silo.chargeSpend = prefabDesc.siloChargeFrame * 10000;
                    silo.coldSpend = prefabDesc.siloColdFrame * 10000;
                }
            }
        }

        stopwatch.Stop();
        Logger.LogInfo($"Finished in {stopwatch.ElapsedMilliseconds}ms.");
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        if (_initialized) return;
        _initialized = true;
        // Belts
        LDB.items.Select(2001).prefabDesc.beltSpeed = (int)Cfg.BeltSpeed[0];
        LDB.items.Select(2002).prefabDesc.beltSpeed = (int)Cfg.BeltSpeed[1];
        LDB.items.Select(2003).prefabDesc.beltSpeed = (int)Cfg.BeltSpeed[2];
        foreach (var proto in LDB.recipes.dataArray)
        {
            if (proto.Type == ERecipeType.Fractionate)
            {
                for (int i = 0; i < proto.ItemCounts.Length; i++)
                {
                    proto.ItemCounts[i] *= Cfg.AssembleSpeedMultiplier;
                }
            }
        }

        foreach (var proto in LDB.items.dataArray)
        {
            var prefabDesc = proto.prefabDesc;
            /* Fix collision sizes, for GalacticScale old versions, should be of no use now.
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
            */

            if (prefabDesc.isInserter)
            {
                prefabDesc.inserterSTT /= Cfg.SorterSpeedMultiplier;
                prefabDesc.inserterDelay /= Cfg.SorterSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= Cfg.SorterPowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= Cfg.SorterPowerConsumptionMultiplier;
            }

            if (prefabDesc.isLab)
            {
                prefabDesc.labAssembleSpeed *= Cfg.AssembleSpeedMultiplier;
                prefabDesc.labResearchSpeed *= Cfg.ResearchSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= Cfg.LabPowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= Cfg.LabPowerConsumptionMultiplier;
            }

            if (prefabDesc.isAssembler)
            {
                prefabDesc.assemblerSpeed *= Cfg.AssembleSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= Cfg.AssemblePowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= Cfg.AssemblePowerConsumptionMultiplier;
            }

            if (prefabDesc.minerType != EMinerType.None)
            {
                prefabDesc.minerPeriod /= Cfg.MinerSpeedMultiplier;
                prefabDesc.idleEnergyPerTick *= Cfg.MinerPowerConsumptionMultiplier;
                prefabDesc.workEnergyPerTick *= Cfg.MinerPowerConsumptionMultiplier;
            }

            if (prefabDesc.isPowerGen || prefabDesc.isPowerExchanger || prefabDesc.isAccumulator)
            {
                prefabDesc.genEnergyPerTick *= Cfg.PowerGenerationMultiplier;
                prefabDesc.useFuelPerTick *= Cfg.PowerFuelConsumptionMultiplier;
                if (prefabDesc.isPowerConsumer)
                {
                    prefabDesc.idleEnergyPerTick *= Cfg.PowerGenerationMultiplier;
                    prefabDesc.workEnergyPerTick *= Cfg.PowerGenerationMultiplier;
                }

                if (prefabDesc.isPowerExchanger)
                {
                    prefabDesc.exchangeEnergyPerTick *= Cfg.PowerFuelConsumptionMultiplier;
                }

                if (prefabDesc.isAccumulator)
                {
                    prefabDesc.maxAcuEnergy *= Cfg.PowerGenerationMultiplier;
                    prefabDesc.inputEnergyPerTick *= Cfg.PowerGenerationMultiplier;
                    prefabDesc.outputEnergyPerTick *= Cfg.PowerGenerationMultiplier;
                }
            }

            if (prefabDesc.isPowerNode)
            {
                var ival = Mathf.Floor(prefabDesc.powerConnectDistance);
                prefabDesc.powerConnectDistance =
                    ival * Cfg.PowerSupplyAreaMultiplier + (prefabDesc.powerConnectDistance - ival);
                ival = Mathf.Floor(prefabDesc.powerCoverRadius);
                prefabDesc.powerCoverRadius =
                    ival * Cfg.PowerSupplyAreaMultiplier + (prefabDesc.powerCoverRadius - ival);
            }

            if (prefabDesc.isEjector)
            {
                prefabDesc.ejectorChargeFrame /= Cfg.EjectMultiplier;
                prefabDesc.ejectorColdFrame /= Cfg.EjectMultiplier;
            }

            if (prefabDesc.isSilo)
            {
                prefabDesc.siloChargeFrame /= Cfg.SiloMultiplier;
                prefabDesc.siloColdFrame /= Cfg.SiloMultiplier;
            }
        }
    }

    /*
    private static void FixExtValue(ref float v)
    {
        if (v == 0f)
        {
            return;
        }

        var b = Math.Abs(v);
        v = (v - b) * 0.75f + b;
    }
    */
}