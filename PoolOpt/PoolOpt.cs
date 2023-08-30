using System;
using BepInEx;
using HarmonyLib;

namespace PoolOpt;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class PoolOptPatch : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(PoolOptPatch));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame))]
    private static void GameSave_LoadCurrentGame_Postfix()
    {
        DebugOutput();
        foreach (var planet in GameMain.data.factories)
        {
            if (planet == null) continue;
            var factorySystem = planet.factorySystem;
            if (factorySystem != null)
            {
                OptimizePool((in MinerComponent n) => n.id, 256,
                    ref factorySystem.minerPool, ref factorySystem.minerCursor, ref factorySystem.minerCapacity,
                    ref factorySystem.minerRecycle, ref factorySystem.minerRecycleCursor);
            }
        }
        DebugOutput();
    }

    private delegate int GetId<T>(in T s) where T : struct;

    private static bool OptimizePool<T>(GetId<T> getter, int initCapacity, ref T[] pool, ref int cursor, ref int capacity, ref int[] recycle, ref int recycleCursor) where T : struct
    {
        if (cursor <= 1) return false;
        var pos = cursor;
        while (pos > 0)
        {
            if (getter(pool[pos]) == pos) break;
            pos--;
        }

        if (pos == cursor) return false;
        if (pos == 0)
        {
            cursor = 1;
            capacity = initCapacity;
            pool = new T[initCapacity];
            recycle = new int[initCapacity];
            recycleCursor = 0;
            return true;
        }

        cursor = pos + 1;
        Array.Sort(recycle);
        var idx = Array.BinarySearch(recycle, 0, recycleCursor, pos);
        recycleCursor = idx < 0 ? ~idx : idx + 1;
        return true;
    }

    private static void DebugOutput()
    {
        foreach (var planet in GameMain.data.factories)
        {
            if (planet == null) continue;
            Logger.LogDebug($"Planet: {planet.planetId}");
            var cargoTraffic = planet.cargoTraffic;
            if (cargoTraffic != null)
            {
                Logger.LogDebug($"  Cargo: Belt=[{cargoTraffic.beltCursor},{cargoTraffic.beltCapacity},{cargoTraffic.beltRecycleCursor}]");
                Logger.LogDebug($"         Path=[{cargoTraffic.pathCursor},{cargoTraffic.pathCapacity},{cargoTraffic.pathRecycleCursor}]");
                Logger.LogDebug($"         Splitter=[{cargoTraffic.splitterCursor},{cargoTraffic.splitterCapacity},{cargoTraffic.splitterRecycleCursor}]");
                Logger.LogDebug($"         Monitor=[{cargoTraffic.monitorCursor},{cargoTraffic.monitorCapacity},{cargoTraffic.monitorRecycleCursor}]");
                Logger.LogDebug($"         Spraycoater=[{cargoTraffic.spraycoaterCursor},{cargoTraffic.spraycoaterCapacity},{cargoTraffic.spraycoaterRecycleCursor}]");
                Logger.LogDebug($"         Piler=[{cargoTraffic.pilerCursor},{cargoTraffic.pilerCapacity},{cargoTraffic.pilerRecycleCursor}]");
            }

            var factoryStorage = planet.factoryStorage;
            if (factoryStorage != null)
            {
                Logger.LogDebug($"  Storage: Storage=[{factoryStorage.storageCursor},{factoryStorage.storageCapacity},{factoryStorage.storageRecycleCursor}]");
                Logger.LogDebug($"           Tank=[{factoryStorage.tankCursor},{factoryStorage.tankCapacity},{factoryStorage.tankRecycleCursor}]");
            }

            var factorySystem = planet.factorySystem;
            if (factorySystem != null)
            {
                Logger.LogDebug($"  Factory: Storage=[{factorySystem.minerCursor},{factorySystem.minerCapacity},{factorySystem.minerRecycleCursor}]");
                Logger.LogDebug($"           Inserter=[{factorySystem.inserterCursor},{factorySystem.inserterCapacity},{factorySystem.inserterRecycleCursor}]");
                Logger.LogDebug($"           Assembler=[{factorySystem.assemblerCursor},{factorySystem.assemblerCapacity},{factorySystem.assemblerRecycleCursor}]");
                Logger.LogDebug($"           Fractionator=[{factorySystem.fractionatorCursor},{factorySystem.fractionatorCapacity},{factorySystem.fractionatorRecycleCursor}]");
                Logger.LogDebug($"           Ejector=[{factorySystem.ejectorCursor},{factorySystem.ejectorCapacity},{factorySystem.ejectorRecycleCursor}]");
                Logger.LogDebug($"           Silo=[{factorySystem.siloCursor},{factorySystem.siloCapacity},{factorySystem.siloRecycleCursor}]");
                Logger.LogDebug($"           Lab=[{factorySystem.labCursor},{factorySystem.labCapacity},{factorySystem.labRecycleCursor}]");
            }

            var transport = planet.transport;
            if (transport != null)
            {
                Logger.LogDebug($"  Transport: Station=[{transport.stationCursor},{transport.stationCapacity},{transport.stationRecycleCursor}]");
                Logger.LogDebug($"             Dispenser=[{transport.dispenserCursor},{transport.dispenserCapacity},{transport.dispenserRecycleCursor}]");
            }

            var powerSystem = planet.powerSystem;
            if (powerSystem != null)
            {
                Logger.LogDebug($"  Power: Gen=[{powerSystem.genCursor},{powerSystem.genCapacity},{powerSystem.genRecycleCursor}]");
                Logger.LogDebug($"         Node=[{powerSystem.nodeCursor},{powerSystem.nodeCapacity},{powerSystem.nodeRecycleCursor}]");
                Logger.LogDebug($"         consumer=[{powerSystem.consumerCursor},{powerSystem.consumerCapacity},{powerSystem.consumerRecycleCursor}]");
                Logger.LogDebug($"         acc=[{powerSystem.accCursor},{powerSystem.accCapacity},{powerSystem.accRecycleCursor}]");
                Logger.LogDebug($"         exc=[{powerSystem.excCursor},{powerSystem.excCapacity},{powerSystem.excRecycleCursor}]");
                Logger.LogDebug($"         net=[{powerSystem.netCursor},{powerSystem.netCapacity},{powerSystem.netRecycleCursor}]");
            }
        }
    }
}