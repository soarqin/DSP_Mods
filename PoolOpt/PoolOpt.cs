using System;
using System.Runtime.CompilerServices;
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
        if (DSPGame.IsMenuDemo) return;
        foreach (var planet in GameMain.data.factories)
        {
            if (planet == null) continue;
            var oldLen = planet.entityPool.Length;
            if (OptimizePool((in EntityData n) => n.id, 1024,
                    ref planet.entityPool, ref planet.entityCursor, ref planet.entityCapacity,
                    ref planet.entityRecycle, ref planet.entityRecycleCursor))
            {
                if (oldLen != planet.entityPool.Length)
                {
                    var newLen = planet.entityPool.Length;
                    planet.entityAnimPool = new AnimData[newLen];
                    planet.entitySignPool = new SignData[newLen];
                    planet.entityConnPool = new int[newLen];
                    planet.entityMutexs = new Mutex[newLen];
                    planet.entityNeeds = new int[newLen][];
                }
                Logger.LogDebug($"Optimized `{nameof(planet.entityPool)}` on Planet {planet.planetId}");
            }

            oldLen = planet.prebuildPool.Length;
            if (OptimizePool((in PrebuildData n) => n.id, 256,
                    ref planet.prebuildPool, ref planet.prebuildCursor, ref planet.prebuildCapacity,
                    ref planet.prebuildRecycle, ref planet.prebuildRecycleCursor))
            {
                Logger.LogDebug($"Optimized `{nameof(planet.prebuildPool)}` on Planet {planet.planetId}");
                if (oldLen != planet.prebuildPool.Length)
                {
                    var newLen = planet.prebuildPool.Length;
                    planet.prebuildConnPool = new int[newLen];
                }
            }

            var cargoTraffic = planet.cargoTraffic;
            if (cargoTraffic != null)
            {
                if (OptimizePool((in BeltComponent n) => n.id, 16,
                        ref cargoTraffic.beltPool, ref cargoTraffic.beltCursor, ref cargoTraffic.beltCapacity,
                        ref cargoTraffic.beltRecycle, ref cargoTraffic.beltRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.beltPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in CargoPath n) => n?.id ?? 0, 16,
                        ref cargoTraffic.pathPool, ref cargoTraffic.pathCursor, ref cargoTraffic.pathCapacity,
                        ref cargoTraffic.pathRecycle, ref cargoTraffic.pathRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.pathPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in SplitterComponent n) => n.id, 16,
                        ref cargoTraffic.splitterPool, ref cargoTraffic.splitterCursor, ref cargoTraffic.splitterCapacity,
                        ref cargoTraffic.splitterRecycle, ref cargoTraffic.splitterRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.splitterPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in MonitorComponent n) => n.id, 16,
                        ref cargoTraffic.monitorPool, ref cargoTraffic.monitorCursor, ref cargoTraffic.monitorCapacity,
                        ref cargoTraffic.monitorRecycle, ref cargoTraffic.monitorRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.monitorPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in SpraycoaterComponent n) => n.id, 16,
                        ref cargoTraffic.spraycoaterPool, ref cargoTraffic.spraycoaterCursor, ref cargoTraffic.spraycoaterCapacity,
                        ref cargoTraffic.spraycoaterRecycle, ref cargoTraffic.spraycoaterRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.spraycoaterPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PilerComponent n) => n.id, 16,
                        ref cargoTraffic.pilerPool, ref cargoTraffic.pilerCursor, ref cargoTraffic.pilerCapacity,
                        ref cargoTraffic.pilerRecycle, ref cargoTraffic.pilerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.pilerPool)}` on Planet {planet.planetId}");
            }
            var factoryStorage = planet.factoryStorage;
            if (factoryStorage != null)
            {
                if (OptimizePool((in StorageComponent n) => n?.id ?? n.id, 64,
                        ref factoryStorage.storagePool, ref factoryStorage.storageCursor, ref factoryStorage.storageCapacity,
                        ref factoryStorage.storageRecycle, ref factoryStorage.storageRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factoryStorage.storagePool)}` on Planet {planet.planetId}");
                if (OptimizePool((in TankComponent n) => n.id, 256,
                        ref factoryStorage.tankPool, ref factoryStorage.tankCursor, ref factoryStorage.tankCapacity,
                        ref factoryStorage.tankRecycle, ref factoryStorage.tankRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factoryStorage.tankPool)}` on Planet {planet.planetId}");
            }
            var factorySystem = planet.factorySystem;
            if (factorySystem != null)
            {
                if (OptimizePool((in MinerComponent n) => n.id, 256,
                        ref factorySystem.minerPool, ref factorySystem.minerCursor, ref factorySystem.minerCapacity,
                        ref factorySystem.minerRecycle, ref factorySystem.minerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.minerPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in InserterComponent n) => n.id, 256,
                        ref factorySystem.inserterPool, ref factorySystem.inserterCursor, ref factorySystem.inserterCapacity,
                        ref factorySystem.inserterRecycle, ref factorySystem.inserterRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.inserterPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in AssemblerComponent n) => n.id, 256,
                        ref factorySystem.assemblerPool, ref factorySystem.assemblerCursor, ref factorySystem.assemblerCapacity,
                        ref factorySystem.assemblerRecycle, ref factorySystem.assemblerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.assemblerPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in FractionatorComponent n) => n.id, 32,
                        ref factorySystem.fractionatorPool, ref factorySystem.fractionatorCursor, ref factorySystem.fractionatorCapacity,
                        ref factorySystem.fractionatorRecycle, ref factorySystem.fractionatorRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.fractionatorPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in EjectorComponent n) => n.id, 32,
                        ref factorySystem.ejectorPool, ref factorySystem.ejectorCursor, ref factorySystem.ejectorCapacity,
                        ref factorySystem.ejectorRecycle, ref factorySystem.ejectorRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.ejectorPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in SiloComponent n) => n.id, 32,
                        ref factorySystem.siloPool, ref factorySystem.siloCursor, ref factorySystem.siloCapacity,
                        ref factorySystem.siloRecycle, ref factorySystem.siloRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.siloPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in LabComponent n) => n.id, 256,
                        ref factorySystem.labPool, ref factorySystem.labCursor, ref factorySystem.labCapacity,
                        ref factorySystem.labRecycle, ref factorySystem.labRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.labPool)}` on Planet {planet.planetId}");
            }
            var transport = planet.transport;
            if (transport != null)
            {
                if (OptimizePool((in StationComponent n) => n?.id ?? 0, 16,
                        ref transport.stationPool, ref transport.stationCursor, ref transport.stationCapacity,
                        ref transport.stationRecycle, ref transport.stationRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(transport.stationPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in DispenserComponent n) => n?.id ?? 0, 8,
                        ref transport.dispenserPool, ref transport.dispenserCursor, ref transport.dispenserCapacity,
                        ref transport.dispenserRecycle, ref transport.dispenserRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(transport.dispenserPool)}` on Planet {planet.planetId}");
            }

            var powerSystem = planet.powerSystem;
            if (powerSystem != null)
            {
                if (OptimizePool((in PowerGeneratorComponent n) => n.id, 8,
                        ref powerSystem.genPool, ref powerSystem.genCursor, ref powerSystem.genCapacity,
                        ref powerSystem.genRecycle, ref powerSystem.genRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.genPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerNodeComponent n) => n.id, 8,
                        ref powerSystem.nodePool, ref powerSystem.nodeCursor, ref powerSystem.nodeCapacity,
                        ref powerSystem.nodeRecycle, ref powerSystem.nodeRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.nodePool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerConsumerComponent n) => n.id, 8,
                        ref powerSystem.consumerPool, ref powerSystem.consumerCursor, ref powerSystem.consumerCapacity,
                        ref powerSystem.consumerRecycle, ref powerSystem.consumerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.consumerPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerAccumulatorComponent n) => n.id, 8,
                        ref powerSystem.accPool, ref powerSystem.accCursor, ref powerSystem.accCapacity,
                        ref powerSystem.accRecycle, ref powerSystem.accRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.accPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerExchangerComponent n) => n.id, 8,
                        ref powerSystem.excPool, ref powerSystem.excCursor, ref powerSystem.excCapacity,
                        ref powerSystem.excRecycle, ref powerSystem.excRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.excPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerNetwork n) => n?.id ?? 0, 8,
                        ref powerSystem.netPool, ref powerSystem.netCursor, ref powerSystem.netCapacity,
                        ref powerSystem.netRecycle, ref powerSystem.netRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.netPool)}` on Planet {planet.planetId}");
            }
        }
        var trashContainer = GameMain.data.trashSystem?.container;
        if (trashContainer != null)
        {
            var oldLen = trashContainer.trashObjPool.Length;
            if (OptimizePool((in TrashObject n) => n.item == 0, 16,
                    ref trashContainer.trashObjPool, ref trashContainer.trashCursor, ref trashContainer.trashCapacity,
                    ref trashContainer.trashRecycle, ref trashContainer.trashRecycleCursor))
            {
                if (oldLen != trashContainer.trashObjPool.Length)
                {
                    var newLen = trashContainer.trashObjPool.Length;
                    trashContainer.trashDataPool = new TrashData[newLen];
                }
                Logger.LogDebug($"Optimized `{nameof(trashContainer.trashObjPool)}`");
            }
        }
        var warningSystem = GameMain.data.warningSystem;
        if (warningSystem != null)
        {
            if (OptimizePool((in WarningData n) => n.id, 32,
                    ref warningSystem.warningPool, ref warningSystem.warningCursor, ref warningSystem.warningCapacity,
                    ref warningSystem.warningRecycle, ref warningSystem.warningRecycleCursor))
                Logger.LogDebug($"Optimized `{nameof(warningSystem.warningPool)}`");
        }
        var dysonSpheres = GameMain.data.dysonSpheres;
        if (dysonSpheres != null)
        {
            foreach (var dysonSphere in dysonSpheres)
            {
                if (dysonSphere == null) continue;
                if (OptimizePool((in DysonRocket n) => n.id, 256,
                        ref dysonSphere.rocketPool, ref dysonSphere.rocketCursor, ref dysonSphere.rocketCapacity,
                        ref dysonSphere.rocketRecycle, ref dysonSphere.rocketRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(dysonSphere.rocketPool)}` on Star {dysonSphere.starData.id}");
                if (OptimizePool((in DysonNodeRData n) => n.id, 128,
                        ref dysonSphere.nrdPool, ref dysonSphere.nrdCursor, ref dysonSphere.nrdCapacity,
                        ref dysonSphere.nrdRecycle, ref dysonSphere.nrdRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(dysonSphere.nrdPool)}` on Star {dysonSphere.starData.id}");
                if (dysonSphere.layersIdBased == null) continue;
                foreach (var layer in dysonSphere.layersSorted)
                {
                    if (layer == null) continue;
                    if (OptimizePool((in DysonNode n) => n?.id ?? 0, 64,
                            ref layer.nodePool, ref layer.nodeCursor, ref layer.nodeCapacity,
                            ref layer.nodeRecycle, ref layer.nodeRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(layer.nodePool)}` on Star {dysonSphere.starData.id} layer {layer.id}");
                    if (OptimizePool((in DysonFrame n) => n?.id ?? 0, 64,
                            ref layer.framePool, ref layer.frameCursor, ref layer.frameCapacity,
                            ref layer.frameRecycle, ref layer.frameRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(layer.framePool)}` on Star {dysonSphere.starData.id} layer {layer.id}");
                    if (OptimizePool((in DysonShell n) => n?.id ?? 0, 64,
                            ref layer.shellPool, ref layer.shellCursor, ref layer.shellCapacity,
                            ref layer.shellRecycle, ref layer.shellRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(layer.shellPool)}` on Star {dysonSphere.starData.id} layer {layer.id}");
                }

                var swarm = dysonSphere.swarm;
                if (swarm != null)
                {
                    if (OptimizePool((in SailBullet n) => n.id, 128,
                            ref swarm.bulletPool, ref swarm.bulletCursor, ref swarm.bulletCapacity,
                            ref swarm.bulletRecycle, ref swarm.bulletRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(swarm.bulletPool)}` on Star {dysonSphere.starData.id}");
                }
            }
        }

        GC.Collect();
    }

    private delegate int GetId<T>(in T s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OptimizePool<T>(GetId<T> getter, int initCapacity, ref T[] pool, ref int cursor, ref int capacity, ref int[] recycle, ref int recycleCursor)
    {
        if (cursor <= 1) return false;
        var pos = cursor - 1;
        while (pos > 0)
        {
            if (getter(pool[pos]) == pos) break;
            pos--;
        }

        if (pos + 1 == cursor) return false;
        if (pos == 0)
        {
            cursor = 1;
            capacity = initCapacity;
            pool = new T[initCapacity];
            recycle = new int[initCapacity];
            recycleCursor = 0;
            Logger.LogDebug("Resetted pool to initial status");
            return true;
        }

        Logger.LogDebug($"Old size = {cursor}. Old recycle size = {recycleCursor}");
        cursor = pos + 1;
        Array.Sort(recycle, 0, recycleCursor);
        var idx = Array.BinarySearch(recycle, 0, recycleCursor, pos + 1);
        recycleCursor = idx < 0 ? ~idx : idx;
        Logger.LogDebug($"Shrinked pool size to {cursor} and recycle size to {recycleCursor}");
        return true;
    }

    private delegate bool IsEmpty<T>(in T s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OptimizePool<T>(IsEmpty<T> checker, int initCapacity, ref T[] pool, ref int cursor, ref int capacity, ref int[] recycle, ref int recycleCursor)
    {
        if (cursor <= 1) return false;
        var pos = cursor;
        while (pos > 0)
        {
            if (!checker(pool[pos])) break;
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

        cursor = pos;
        Array.Sort(recycle, 0, recycleCursor);
        var idx = Array.BinarySearch(recycle, 0, recycleCursor, pos + 1);
        recycleCursor = idx < 0 ? ~idx : idx;
        return true;
    }
}