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
            if (OptimizePool((in EntityData n, int index) => n.id == index, () =>
                    {
                        planet.entityCapacity = 0;
                        planet.SetEntityCapacity(1024);
                    },
                    ref planet.entityPool, ref planet.entityCursor,
                    ref planet.entityRecycle, ref planet.entityRecycleCursor))
            {
                Logger.LogDebug($"Optimized `{nameof(planet.entityPool)}` on Planet {planet.planetId}");
            }

            if (OptimizePool((in PrebuildData n, int index) => n.id == index, () =>
                    {
                        planet.prebuildCapacity = 0;
                        planet.SetPrebuildCapacity(256);
                    },
                    ref planet.prebuildPool, ref planet.prebuildCursor,
                    ref planet.prebuildRecycle, ref planet.prebuildRecycleCursor))
            {
                Logger.LogDebug($"Optimized `{nameof(planet.prebuildPool)}` on Planet {planet.planetId}");
            }

            var cargoTraffic = planet.cargoTraffic;
            if (cargoTraffic != null)
            {
                if (OptimizePool((in BeltComponent n, int index) => n.id == index, () =>
                        {
                            cargoTraffic.beltCapacity = 0;
                            cargoTraffic.SetBeltCapacity(16);
                        },
                        ref cargoTraffic.beltPool, ref cargoTraffic.beltCursor,
                        ref cargoTraffic.beltRecycle, ref cargoTraffic.beltRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.beltPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in CargoPath n, int index) => (n?.id ?? 0) == index, () =>
                        {
                            cargoTraffic.pathCapacity = 0;
                            cargoTraffic.SetPathCapacity(16);
                        },
                        ref cargoTraffic.pathPool, ref cargoTraffic.pathCursor,
                        ref cargoTraffic.pathRecycle, ref cargoTraffic.pathRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.pathPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in SplitterComponent n, int index) => n.id == index, () =>
                        {
                            cargoTraffic.splitterCapacity = 0;
                            cargoTraffic.SetSplitterCapacity(16);
                        },
                        ref cargoTraffic.splitterPool, ref cargoTraffic.splitterCursor,
                        ref cargoTraffic.splitterRecycle, ref cargoTraffic.splitterRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.splitterPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in MonitorComponent n, int index) => n.id == index, () =>
                        {
                            cargoTraffic.monitorCapacity = 0;
                            cargoTraffic.SetMonitorCapacity(16);
                        },
                        ref cargoTraffic.monitorPool, ref cargoTraffic.monitorCursor,
                        ref cargoTraffic.monitorRecycle, ref cargoTraffic.monitorRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.monitorPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in SpraycoaterComponent n, int index) => n.id == index, () =>
                        {
                            cargoTraffic.spraycoaterCapacity = 0;
                            cargoTraffic.SetSpraycoaterCapacity(16);
                        },
                        ref cargoTraffic.spraycoaterPool, ref cargoTraffic.spraycoaterCursor,
                        ref cargoTraffic.spraycoaterRecycle, ref cargoTraffic.spraycoaterRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.spraycoaterPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PilerComponent n, int index) => n.id == index, () =>
                        {
                            cargoTraffic.pilerCapacity = 0;
                            cargoTraffic.SetPilerCapacity(16);
                        },
                        ref cargoTraffic.pilerPool, ref cargoTraffic.pilerCursor,
                        ref cargoTraffic.pilerRecycle, ref cargoTraffic.pilerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(cargoTraffic.pilerPool)}` on Planet {planet.planetId}");
            }
            var factoryStorage = planet.factoryStorage;
            if (factoryStorage != null)
            {
                if (OptimizePool((in StorageComponent n, int index) => (n?.id ?? 0) == index, () =>
                        {
                            factoryStorage.storageCapacity = 0;
                            factoryStorage.SetStorageCapacity(64);
                        },
                        ref factoryStorage.storagePool, ref factoryStorage.storageCursor,
                        ref factoryStorage.storageRecycle, ref factoryStorage.storageRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factoryStorage.storagePool)}` on Planet {planet.planetId}");
                if (OptimizePool((in TankComponent n, int index) => n.id == index, () =>
                        {
                            factoryStorage.tankCapacity = 0;
                            factoryStorage.SetTankCapacity(256);
                        },
                        ref factoryStorage.tankPool, ref factoryStorage.tankCursor,
                        ref factoryStorage.tankRecycle, ref factoryStorage.tankRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factoryStorage.tankPool)}` on Planet {planet.planetId}");
            }
            var factorySystem = planet.factorySystem;
            if (factorySystem != null)
            {
                if (OptimizePool((in MinerComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.minerCapacity = 0;
                            factorySystem.SetMinerCapacity(256);
                        },
                        ref factorySystem.minerPool, ref factorySystem.minerCursor,
                        ref factorySystem.minerRecycle, ref factorySystem.minerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.minerPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in InserterComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.inserterCapacity = 0;
                            factorySystem.SetInserterCapacity(256);
                        },
                        ref factorySystem.inserterPool, ref factorySystem.inserterCursor,
                        ref factorySystem.inserterRecycle, ref factorySystem.inserterRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.inserterPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in AssemblerComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.assemblerCapacity = 0;
                            factorySystem.SetAssemblerCapacity(256);
                        },
                        ref factorySystem.assemblerPool, ref factorySystem.assemblerCursor,
                        ref factorySystem.assemblerRecycle, ref factorySystem.assemblerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.assemblerPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in FractionatorComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.fractionatorCapacity = 0;
                            factorySystem.SetFractionatorCapacity(32);
                        },
                        ref factorySystem.fractionatorPool, ref factorySystem.fractionatorCursor,
                        ref factorySystem.fractionatorRecycle, ref factorySystem.fractionatorRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.fractionatorPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in EjectorComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.ejectorCapacity = 0;
                            factorySystem.SetEjectorCapacity(32);
                        },
                        ref factorySystem.ejectorPool, ref factorySystem.ejectorCursor,
                        ref factorySystem.ejectorRecycle, ref factorySystem.ejectorRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.ejectorPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in SiloComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.siloCapacity = 0;
                            factorySystem.SetSiloCapacity(32);
                        },
                        ref factorySystem.siloPool, ref factorySystem.siloCursor,
                        ref factorySystem.siloRecycle, ref factorySystem.siloRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.siloPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in LabComponent n, int index) => n.id == index, () =>
                        {
                            factorySystem.labCapacity = 0;
                            factorySystem.SetLabCapacity(256);
                        },
                        ref factorySystem.labPool, ref factorySystem.labCursor,
                        ref factorySystem.labRecycle, ref factorySystem.labRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(factorySystem.labPool)}` on Planet {planet.planetId}");
            }
            var transport = planet.transport;
            if (transport != null)
            {
                if (OptimizePool((in StationComponent n, int index) => (n?.id ?? 0) == index, () =>
                        {
                            transport.stationCapacity = 0;
                            transport.SetStationCapacity(16);
                        },
                        ref transport.stationPool, ref transport.stationCursor,
                        ref transport.stationRecycle, ref transport.stationRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(transport.stationPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in DispenserComponent n, int index) => (n?.id ?? 0) == index, () =>
                        {
                            transport.dispenserCapacity = 0;
                            transport.SetDispenserCapacity(8);
                        },
                        ref transport.dispenserPool, ref transport.dispenserCursor,
                        ref transport.dispenserRecycle, ref transport.dispenserRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(transport.dispenserPool)}` on Planet {planet.planetId}");
            }

            var powerSystem = planet.powerSystem;
            if (powerSystem != null)
            {
                if (OptimizePool((in PowerGeneratorComponent n, int index) => n.id == index, () =>
                        {
                            powerSystem.genCapacity = 0;
                            powerSystem.SetGeneratorCapacity(8);
                        },
                        ref powerSystem.genPool, ref powerSystem.genCursor,
                        ref powerSystem.genRecycle, ref powerSystem.genRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.genPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerNodeComponent n, int index) => n.id == index, () =>
                        {
                            powerSystem.nodeCapacity = 0;
                            powerSystem.SetNodeCapacity(8);
                        },
                        ref powerSystem.nodePool, ref powerSystem.nodeCursor,
                        ref powerSystem.nodeRecycle, ref powerSystem.nodeRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.nodePool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerConsumerComponent n, int index) => n.id == index, () =>
                        {
                            powerSystem.consumerCapacity = 0;
                            powerSystem.SetConsumerCapacity(8);
                        },
                        ref powerSystem.consumerPool, ref powerSystem.consumerCursor,
                        ref powerSystem.consumerRecycle, ref powerSystem.consumerRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.consumerPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerAccumulatorComponent n, int index) => n.id == index, () =>
                        {
                            powerSystem.accCapacity = 0;
                            powerSystem.SetAccumulatorCapacity(8);
                        },
                        ref powerSystem.accPool, ref powerSystem.accCursor,
                        ref powerSystem.accRecycle, ref powerSystem.accRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.accPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerExchangerComponent n, int index) => n.id == index, () =>
                        {
                            powerSystem.excCapacity = 0;
                            powerSystem.SetExchangerCapacity(8);
                        },
                        ref powerSystem.excPool, ref powerSystem.excCursor,
                        ref powerSystem.excRecycle, ref powerSystem.excRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.excPool)}` on Planet {planet.planetId}");
                if (OptimizePool((in PowerNetwork n, int index) => (n?.id ?? 0) == index, () =>
                        {
                            powerSystem.netCapacity = 0;
                            powerSystem.SetNetworkCapacity(8);
                            powerSystem.netPool[0] = new PowerNetwork();
                            powerSystem.netPool[0].Reset(0);
                        },
                        ref powerSystem.netPool, ref powerSystem.netCursor,
                        ref powerSystem.netRecycle, ref powerSystem.netRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(powerSystem.netPool)}` on Planet {planet.planetId}");
            }
        }
        var trashContainer = GameMain.data.trashSystem?.container;
        if (trashContainer != null)
        {
            if (OptimizePool((in TrashObject n, int index) => n.item != 0, () =>
                    {
                        trashContainer.trashCapacity = 0;
                        trashContainer.SetTrashCapacity(16);
                    },
                    ref trashContainer.trashObjPool, ref trashContainer.trashCursor,
                    ref trashContainer.trashRecycle, ref trashContainer.trashRecycleCursor))
            {
                Logger.LogDebug($"Optimized `{nameof(trashContainer.trashObjPool)}`");
            }
        }
        var warningSystem = GameMain.data.warningSystem;
        if (warningSystem != null)
        {
            if (OptimizePool((in WarningData n, int index) => n.id == index, () =>
                    {
                        warningSystem.warningCapacity = 0;
                        warningSystem.SetWarningCapacity(32);
                    },
                    ref warningSystem.warningPool, ref warningSystem.warningCursor,
                    ref warningSystem.warningRecycle, ref warningSystem.warningRecycleCursor))
                Logger.LogDebug($"Optimized `{nameof(warningSystem.warningPool)}`");
        }
        var dysonSpheres = GameMain.data.dysonSpheres;
        if (dysonSpheres != null)
        {
            foreach (var dysonSphere in dysonSpheres)
            {
                if (dysonSphere == null) continue;
                if (OptimizePool((in DysonRocket n, int index) => n.id == index, () =>
                        {
                            dysonSphere.rocketCapacity = 0;
                            dysonSphere.SetRocketCapacity(256);
                        },
                        ref dysonSphere.rocketPool, ref dysonSphere.rocketCursor,
                        ref dysonSphere.rocketRecycle, ref dysonSphere.rocketRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(dysonSphere.rocketPool)}` on Star {dysonSphere.starData.id}");
                if (OptimizePool((in DysonNodeRData n, int index) => n.id == index, () =>
                        {
                            dysonSphere.nrdCapacity = 0;
                            dysonSphere.SetNrdCapacity(128);
                        },
                        ref dysonSphere.nrdPool, ref dysonSphere.nrdCursor,
                        ref dysonSphere.nrdRecycle, ref dysonSphere.nrdRecycleCursor))
                    Logger.LogDebug($"Optimized `{nameof(dysonSphere.nrdPool)}` on Star {dysonSphere.starData.id}");
                if (dysonSphere.layersIdBased == null) continue;
                foreach (var layer in dysonSphere.layersSorted)
                {
                    if (layer == null) continue;
                    if (OptimizePool((in DysonNode n, int index) => (n?.id ?? 0) == index, () =>
                            {
                                layer.nodeCapacity = 64;
                                layer.SetNodeCapacity(64);
                            },
                            ref layer.nodePool, ref layer.nodeCursor,
                            ref layer.nodeRecycle, ref layer.nodeRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(layer.nodePool)}` on Star {dysonSphere.starData.id} layer {layer.id}");
                    if (OptimizePool((in DysonFrame n, int index) => (n?.id ?? 0) == index, () =>
                            {
                                layer.frameCapacity = 64;
                                layer.SetFrameCapacity(64);
                            },
                            ref layer.framePool, ref layer.frameCursor,
                            ref layer.frameRecycle, ref layer.frameRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(layer.framePool)}` on Star {dysonSphere.starData.id} layer {layer.id}");
                    if (OptimizePool((in DysonShell n, int index) => (n?.id ?? 0) == index, () =>
                            {
                                layer.shellCapacity = 64;
                                layer.SetShellCapacity(64);
                            },
                            ref layer.shellPool, ref layer.shellCursor,
                            ref layer.shellRecycle, ref layer.shellRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(layer.shellPool)}` on Star {dysonSphere.starData.id} layer {layer.id}");
                }

                var swarm = dysonSphere.swarm;
                if (swarm != null)
                {
                    if (OptimizePool((in SailBullet n, int index) => n.id == index, () =>
                            {
                                swarm.bulletCapacity = 0;
                                swarm.SetBulletCapacity(128);
                            },
                            ref swarm.bulletPool, ref swarm.bulletCursor,
                            ref swarm.bulletRecycle, ref swarm.bulletRecycleCursor))
                        Logger.LogDebug($"Optimized `{nameof(swarm.bulletPool)}` on Star {dysonSphere.starData.id}");
                }
            }
        }

        GC.Collect();
    }

    private delegate bool IsOccupied<T>(in T s, int index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OptimizePool<T>(IsOccupied<T> isOccupied, Action initCapacity, ref T[] pool, ref int cursor, ref int[] recycle, ref int recycleCursor)
    {
        if (cursor <= 1) return false;
        if (recycleCursor == cursor - 1)
        {
            cursor = 1;
            recycleCursor = 0;
            initCapacity();
            Logger.LogDebug("Resetted pool to initial status");
            return true;
        }
        var pos = cursor - 1;
        while (pos > 0)
        {
            if (isOccupied(pool[pos], pos)) break;
            pos--;
        }

        if (pos + 1 == cursor) return false;
        if (pos == 0)
        {
            cursor = 1;
            recycleCursor = 0;
            initCapacity();
            Logger.LogDebug("Resetted pool to initial status");
            return true;
        }

        cursor = pos + 1;
        Array.Sort(recycle, 0, recycleCursor);
        var idx = Array.BinarySearch(recycle, 0, recycleCursor, pos + 1);
        recycleCursor = idx < 0 ? ~idx : idx;
        Logger.LogDebug($"Shrinked pool size to {cursor} and recycle size to {recycleCursor}");
        return true;
    }

}
