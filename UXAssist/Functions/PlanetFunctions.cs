using System;
using System.IO;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace UXAssist.Functions;

public static class PlanetFunctions
{
    public static ConfigEntry<int> OrbitalCollectorMaxBuildCount;
    public static ConfigEntry<bool> ReturnBuildingsOnInitializeEnabled;
    public static ConfigEntry<bool> ReturnLogisticStorageItemsOnInitializeEnabled;
    public static ConfigEntry<bool> ReturnBeltAFactoryItemsOnInitializeEnabled;

    private const int OrbitalCollectorItemId = 2105;

    public static void DismantleAll(bool toBag)
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        foreach (var etd in factory.entityPool)
        {
            var stationId = etd.stationId;
            if (stationId > 0)
            {
                var sc = GameMain.localPlanet.factory.transport.stationPool[stationId];
                if (sc is null || sc.id != stationId) continue;
                if (toBag)
                {
                    for (var i = sc.storage.Length - 1; i >= 0; i--)
                    {
                        var added = player.TryAddItemToPackage(sc.storage[i].itemId, sc.storage[i].count, sc.storage[i].inc, true, etd.id);
                        if (added > 0) UIItemup.Up(sc.storage[i].itemId, added);
                        sc.storage[i].count = 0;
                    }
                }
                else
                {
                    for (var i = sc.storage.Length - 1; i >= 0; i--)
                    {
                        sc.storage[i].count = 0;
                    }
                }
            }
            if (toBag)
            {
                int protoId = 0;
                factory.DismantleFinally(player, etd.id, ref protoId);
            }
            else
            {
                var objId = etd.id;
                factory.BeforeDismantleObject(objId);
                factory.RemoveEntityWithComponents(objId, false);
                factory.OnDismantleObject(objId);
            }
        }
    }

    public static void RecreatePlanet(bool revertReform)
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        var uiGame = UIRoot.instance.uiGame;
        if (uiGame)
        {
            uiGame.ShutAllFunctionWindow();
            uiGame.ShutAllFullScreens();
        }
        player.controller.actionBuild.Close();

        var groundCombatModule = player.mecha.groundCombatModule;
        for (var i = groundCombatModule.moduleFleets.Length - 1; i >= 0; i--)
        {
            var entry = groundCombatModule.moduleFleets[i];
            if (entry.fleetId <= 0 || !entry.fleetEnabled) continue;
            entry.fleetEnabled = false;
            groundCombatModule.RemoveFleetDirectly(i);
        }
        var constructionSystem = factory.constructionSystem;
        var constructionModule = player.mecha.constructionModule;
        for (var i = constructionSystem.drones.cursor - 1; i > 0; i--)
        {
            ref var drone = ref constructionSystem.drones.buffer[i];
            if (drone.id <= 0) continue;
            var owner = drone.owner;
            constructionSystem.ResetDroneTargets(ref drone);
            if (owner == 0)
            {
                factory.RemoveCraftWithComponents(drone.craftId);
                constructionModule.droneIdleCount++;
            }
            else
            {
                drone.movement = factory.gameData.history.constructionDroneMovement;
                factory.craftPool[drone.craftId].pos = constructionSystem.constructionModules[owner].baseEjectPos;
                constructionSystem.constructionModules[owner].droneIdleCount++;
            }
        }
        if (constructionSystem.constructStats?.buffer != null)
        {
            for (var i = constructionSystem.constructStats.cursor - 1; i > 0; i++)
            {
                ref var constructStat = ref constructionSystem.constructStats.buffer[i];
                if (constructStat.id <= 0) continue;
                constructionSystem.RemoveConstructStat(constructStat.id);
            }
        }
        constructionModule.autoReconstructTargetTotalCount = 0;
        constructionModule.buildTargetTotalCount = 0;
        constructionModule.repairTargetTotalCount = 0;
        constructionModule.autoReconstructTargets = null;
        constructionModule.buildTargets = null;
        constructionModule.repairTargets = null;
        constructionModule.tmpTargets = null;
        constructionModule.tmpTargetsHash = null;
        constructionModule.checkItemCursor = 0;

        var gameData = GameMain.data;

        var returnBuildings = ReturnBuildingsOnInitializeEnabled.Value;
        var returnLogisticStorageItems = ReturnLogisticStorageItemsOnInitializeEnabled.Value;
        var returnBeltAFactoryItems = ReturnBeltAFactoryItemsOnInitializeEnabled.Value;
        var returnedItems = new Dictionary<int, (long count, int inc)>();

        static void AddReturnedItem(int itemId, int count, int inc, Dictionary<int, (long count, int inc)> returnedItems)
        {
            if (returnedItems.TryGetValue(itemId, out var value))
            {
                returnedItems[itemId] = (value.count + count, value.inc + inc);
            }
            else
            {
                returnedItems[itemId] = (count, inc);
            }
        }

        if (returnBuildings || returnBeltAFactoryItems)
        {
            var factoryStorage = factory.factoryStorage;
            var factorySystem = factory.factorySystem;
            var planetTransport = factory.transport;
            var defenseSystem = factory.defenseSystem;
            var powerSystem = factory.powerSystem;
            var cargoTraffic = factory.cargoTraffic;
            for (var i = factory.entityCursor - 1; i > 0; i--)
            {
                ref var ed = ref factory.entityPool[i];
                if (ed.id != i) continue;
                if (ed.protoId <= 0) continue;
                if (returnBuildings) AddReturnedItem(ed.protoId, 1, 0, returnedItems);
                if (!returnBeltAFactoryItems) continue;
                #region Storage Items
                var storageId = ed.storageId;
                if (storageId > 0)
                {
                    var storage = factoryStorage.storagePool[storageId];
                    if (storage != null)
                    {
                        for (var j = storage.size - 1; j >= 0; j--)
                        {
                            var count = storage.grids[j].count;
                            if (count <= 0) continue;
                            AddReturnedItem(storage.grids[j].itemId, count, storage.grids[j].inc, returnedItems);
                        }
                    }
                }
                #endregion
                #region Tank Items
                var tankId = ed.tankId;
                if (tankId > 0)
                {
                    ref var tank = ref factoryStorage.tankPool[tankId];
                    var fluidId = tank.fluidId;
                    var fluidCount = tank.fluidCount;
                    if (fluidId > 0 && fluidCount > 0) AddReturnedItem(fluidId, fluidCount, tank.fluidInc, returnedItems);
                }
                #endregion
                #region Miner Items
                var minerId = ed.minerId;
                if (minerId > 0)
                {
                    ref var miner = ref factorySystem.minerPool[minerId];
                    var productId = miner.productId;
                    var productCount = miner.productCount;
                    if (productId > 0 && productCount > 0) AddReturnedItem(productId, productCount, 0, returnedItems);
                }
                #endregion
                #region Inserter Items
                var inserterId = ed.inserterId;
                if (inserterId > 0)
                {
                    ref var inserter = ref factorySystem.inserterPool[inserterId];
                    var itemId = inserter.itemId;
                    var itemCount = inserter.itemCount;
                    if (itemId > 0 && itemCount > 0) AddReturnedItem(itemId, itemCount, inserter.itemInc, returnedItems);
                }
                #endregion
                #region Assembler Items
                var assemblerId = ed.assemblerId;
                if (assemblerId > 0)
                {
                    ref var assembler = ref factorySystem.assemblerPool[assemblerId];
                    if (assembler.recipeId > 0)
                    {
                        var products = assembler.recipeExecuteData.products;
                        var requires = assembler.recipeExecuteData.requires;
                        var requireCounts = assembler.recipeExecuteData.requireCounts;
                        for (var j = products.Length - 1; j >= 0; j--)
                        {
                            var product = products[j];
                            var produced = assembler.produced[j];
                            if (produced > 0) AddReturnedItem(product, produced, 0, returnedItems);
                        }
                        for (var j = requires.Length - 1; j >= 0; j--)
                        {
                            var require = requires[j];
                            var served = assembler.served[j];
                            var incServed = assembler.incServed[j];
                            if (incServed > served * 10) incServed = served * 10;
                            if (assembler.replicating) served += requireCounts[j];
                            if (served > 0) AddReturnedItem(require, served, incServed, returnedItems);
                        }
                    }
                }
                #endregion
                #region Fractionator Items
                var fractionatorId = ed.fractionatorId;
                if (fractionatorId > 0)
                {
                    ref var fractionator = ref factorySystem.fractionatorPool[fractionatorId];
                    var fluidId = fractionator.fluidId;
                    var fluidInputCount = fractionator.fluidInputCount;
                    if (fluidInputCount > 0) AddReturnedItem(fluidId, fluidInputCount, fractionator.fluidInputInc, returnedItems);
                    var fluidOutputCount = fractionator.fluidOutputCount;
                    if (fluidOutputCount > 0) AddReturnedItem(fluidId, fluidOutputCount, fractionator.fluidOutputInc, returnedItems);
                    var productOutputCount = fractionator.productOutputCount;
                    if (productOutputCount > 0) AddReturnedItem(fractionator.productId, productOutputCount, 0, returnedItems);
                }
                #endregion
                #region Ejector Items
                var ejectorId = ed.ejectorId;
                if (ejectorId > 0)
                {
                    ref var ejector = ref factorySystem.ejectorPool[ejectorId];
                    var bulletCount = ejector.bulletCount;
                    if (bulletCount > 0) AddReturnedItem(ejector.bulletId, bulletCount, ejector.bulletInc, returnedItems);
                }
                #endregion
                #region Silo Items
                var siloId = ed.siloId;
                if (siloId > 0)
                {
                    ref var silo = ref factorySystem.siloPool[siloId];
                    var itemCount = silo.bulletCount;
                    if (itemCount > 0) AddReturnedItem(silo.bulletId, itemCount, silo.bulletInc, returnedItems);
                }
                #endregion
                #region Lab Items
                var labId = ed.labId;
                if (labId > 0)
                {
                    ref var lab = ref factorySystem.labPool[labId];
                    if (lab.recipeId > 0)
                    {
                        var products = lab.recipeExecuteData.products;
                        var requires = lab.recipeExecuteData.requires;
                        var requireCounts = lab.recipeExecuteData.requireCounts;
                        for (var j = products.Length - 1; j >= 0; j--)
                        {
                            var produced = lab.produced[j];
                            if (produced > 0) AddReturnedItem(products[j], produced, 0, returnedItems);
                        }
                        for (var j = requires.Length - 1; j >= 0; j--)
                        {
                            var served = lab.served[j];
                            var incServed = lab.incServed[j];
                            if (incServed > served * 10) incServed = served * 10;
                            if (lab.replicating) served += requireCounts[j];
                            if (served > 0) AddReturnedItem(requires[j], served, incServed, returnedItems);
                        }
                    }
                    if (lab.researchMode && lab.matrixServed != null)
                    {
                        for (var j = lab.matrixServed.Length - 1; j >= 0; j--)
                        {
                            var served = lab.matrixServed[j] / 3600;
                            if (served > 0) AddReturnedItem(LabComponent.matrixIds[j], served, lab.matrixIncServed[j] / 3600, returnedItems);
                        }
                    }
                }
                #endregion
                #region Dispenser Items
                var dispenserId = ed.dispenserId;
                if (dispenserId > 0)
                {
                    var dispenser = planetTransport.dispenserPool[dispenserId];
                    var holdupPackage = dispenser.holdupPackage;
                    for (var j = holdupPackage.Length - 1; j >= 0; j--)
                    {
                        var count = holdupPackage[j].count;
                        if (count > 0) AddReturnedItem(holdupPackage[j].itemId, count, holdupPackage[j].inc, returnedItems);
                    }
                    var courierCount = dispenser.idleCourierCount + dispenser.workCourierCount;
                    if (courierCount > 0) AddReturnedItem((int)Common.KnownItemId.Bot, courierCount, 0, returnedItems);
                }
                #endregion
                #region Turret Items
                var turretId = ed.turretId;
                if (turretId > 0)
                {
                    ref var turret = ref defenseSystem.turrets.buffer[turretId];
                    var itemCount = turret.itemCount;
                    if (itemCount > 0) AddReturnedItem(turret.itemId, itemCount, turret.itemInc, returnedItems);
                }
                #endregion
                #region Battle Base Items
                var battleBaseId = ed.battleBaseId;
                if (battleBaseId > 0)
                {
                    ref var battleBase = ref defenseSystem.battleBases.buffer[battleBaseId];
                    var combatModule = battleBase.combatModule;
                    if (combatModule != null && combatModule.moduleFleets[0].fleetId <= 0)
                    {
                        var fighters = combatModule.moduleFleets[0].fighters;
                        for (var j = fighters.Length - 1; j >= 0; j--)
                        {
                            var fighterItemId = fighters[j].itemId;
                            var fighterCount = fighters[j].count;
                            if (fighterItemId > 0 && fighterCount > 0) AddReturnedItem(fighterItemId, fighterCount, 0, returnedItems);
                        }
                    }
                }
                #endregion
                #region Power Generator Items
                var powerGeneratorId = ed.powerGenId;
                if (powerGeneratorId > 0)
                {
                    ref var powerGen = ref powerSystem.genPool[powerGeneratorId];
                    if (powerGen.fuelId > 0 && powerGen.fuelCount > 0) AddReturnedItem(powerGen.fuelId, powerGen.fuelCount, powerGen.fuelInc, returnedItems);
                    if (powerGen.gamma)
                    {
                        var productId = powerGen.productId;
                        var productCount = (int)powerGen.productCount;
                        if (productId != 0 && productCount > 0) AddReturnedItem(productId, productCount, 0, returnedItems);
                        int catalystId = powerGen.catalystId;
                        var catalystPointDiv = powerGen.catalystPoint / 3600;
                        if (catalystId != 0 && catalystPointDiv > 0) AddReturnedItem(catalystId, catalystPointDiv, powerGen.catalystIncPoint / 3600, returnedItems);
                    }
                }
                #endregion
                #region Power Exchanger Items
                var powerExchangerId = ed.powerExcId;
                if (powerExchangerId > 0)
                {
                    ref var powerExchanger = ref powerSystem.excPool[powerExchangerId];
                    var emptyCount = (int)powerExchanger.emptyCount;
                    if (emptyCount > 0) AddReturnedItem(powerExchanger.emptyId, emptyCount, powerExchanger.emptyInc, returnedItems);
                    var fullCount = (int)powerExchanger.fullCount;
                    if (fullCount > 0) AddReturnedItem(powerExchanger.fullId, fullCount, powerExchanger.fullInc, returnedItems);
                }
                #endregion
                #region Spraycoater Items
                var spraycoaterId = ed.spraycoaterId;
                if (spraycoaterId > 0)
                {
                    ref var spraycoater = ref cargoTraffic.spraycoaterPool[spraycoaterId];
                    if (spraycoater.incItemId != 0 && spraycoater.incCount != 0)
                    {
                        var itemProto = LDB.items.Select(spraycoater.incItemId);
                        var count = spraycoater.incCount / itemProto.HpMax;
                        if (count != 0) AddReturnedItem(spraycoater.incItemId, count, 0, returnedItems);
                    }
                }
                #endregion
                #region Piler Items
                var pilerId = ed.pilerId;
                if (pilerId > 0)
                {
                    ref var piler = ref cargoTraffic.pilerPool[pilerId];
                    var cacheCargoStack = (int)piler.cacheCargoStack1;
                    if (cacheCargoStack > 0) AddReturnedItem(piler.cacheItemId1, cacheCargoStack, piler.cacheCargoInc1, returnedItems);
                    var cacheCargoStack2 = (int)piler.cacheCargoStack2;
                    if (cacheCargoStack2 > 0) AddReturnedItem(piler.cacheItemId2, cacheCargoStack2, piler.cacheCargoInc2, returnedItems);
                }
                #endregion
            }
            if (returnBeltAFactoryItems)
            {
                #region Belt Items
                for (var i = cargoTraffic.pathCursor - 1; i > 0; i--)
                {
                    var cargoPath = cargoTraffic.pathPool[i];
                    if (cargoPath == null) continue;
                    var end = cargoPath.bufferLength - 5;
                    var buffer = cargoPath.buffer;
                    for (var j = 0; j <= end;)
                    {
                        if (buffer[j] >= 246)
                        {
                            j += 250 - buffer[j];
                            var bufferIndex = buffer[j + 1] - 1 + (buffer[j + 2] - 1) * 100 + (buffer[j + 3] - 1) * 10000 + (buffer[j + 4] - 1) * 1000000;
                            ref var cargo = ref cargoPath.cargoContainer.cargoPool[bufferIndex];
                            var stack = cargo.stack;
                            if (stack > 0) AddReturnedItem(cargo.item, stack, cargo.inc, returnedItems);
                            j += 10;
                        }
                        else
                        {
                            j += 5;
                            if (j > end && j < end + 5)
                            {
                                j = end;
                            }
                        }
                    }
                }
                #endregion
            }
        }

        var stationPool = factory.transport?.stationPool;
        if (stationPool != null)
        {
            var galacticTransport = gameData.galacticTransport;
            for (var i = factory.transport.stationCursor - 1; i > 0; i--)
            {
                var sc = stationPool[i];
                if (sc == null || sc.id != i) continue;
                if (returnLogisticStorageItems)
                {
                    for (var j = sc.storage.Length - 1; j >= 0; j--)
                    {
                        var count = sc.storage[j].count;
                        if (count > 0) AddReturnedItem(sc.storage[j].itemId, count, sc.storage[j].inc, returnedItems);
                    }
                    var droneCount = sc.idleDroneCount + sc.workDroneCount;
                    if (droneCount > 0) AddReturnedItem((int)Common.KnownItemId.Drone, droneCount, 0, returnedItems);
                    var shipCount = sc.idleShipCount + sc.workShipCount;
                    if (shipCount > 0) AddReturnedItem((int)Common.KnownItemId.Ship, shipCount, 0, returnedItems);
                    var warperCount = sc.warperCount;
                    if (warperCount > 0) AddReturnedItem((int)Common.KnownItemId.Warper, warperCount, 0, returnedItems);
                }
                var gid = sc.gid;
                if (galacticTransport.stationPool[gid] != null)
                {
                    galacticTransport.stationPool[gid] = null;
                    int cursor = galacticTransport.stationRecycleCursor;
                    galacticTransport.stationRecycleCursor = cursor + 1;
                    galacticTransport.stationRecycle[cursor] = gid;
                }
                galacticTransport.RemoveStation2StationRoute(gid);
                galacticTransport.RefreshTraffic(gid);
                sc.Reset();
            }
            if (galacticTransport.OnStellarStationRemoved != null)
            {
                galacticTransport.OnStellarStationRemoved();
            }
        }

        var physics = planet.physics;
        var gpuiManager = GameMain.gpuiManager;
        var blockContainer = factory.blockContainer;
        var audio = planet.audio;
        for (var id = factory.entityCursor - 1; id > 0; id--)
        {
            ref var ed = ref factory.entityPool[id];
            if (ed.id != id) continue;

            factory.BeforeDismantleObject(id);

            if (ed.colliderId != 0)
            {
                physics.RemoveLinkedColliderData(ed.colliderId);
                physics.NotifyObjectRemove(EObjectType.Entity, ed.id);
            }

            if (ed.modelId != 0)
            {
                gpuiManager.RemoveModel(ed.modelIndex, ed.modelId);
            }

            if (ed.mmblockId != 0)
            {
                blockContainer.RemoveMiniBlock(ed.mmblockId);
            }

            if (ed.audioId != 0)
            {
                audio.RemoveAudioData(ed.audioId);
            }

            factory.OnDismantleObject(id);
        }

        for (var id = factory.prebuildCursor - 1; id > 0; id--)
        {
            ref var pb = ref factory.prebuildPool[id];
            if (pb.id != id) continue;
            if (pb.colliderId != 0)
            {
                physics.RemoveLinkedColliderData(pb.colliderId);
            }
            if (pb.modelId != 0)
            {
                gpuiManager.RemovePrebuildModel(pb.modelIndex, pb.modelId);
            }
        }

        var hives = GameMain.spaceSector?.dfHives;
        if (hives != null)
        {
            var hive = hives[planet.star.index];
            var relays = hive?.relays?.buffer;
            if (relays != null)
            {
                var astroId = planet.astroId;
                for (var i = relays.Length - 1; i >= 0; i--)
                {
                    var relay = relays[i];
                    if (relay == null || relay.id != i) continue;
                    if (relay.targetAstroId != astroId && relay.searchAstroId != astroId) continue;
                    relay.targetAstroId = 0;
                    relay.searchAstroId = 0;
                    if (relay.baseId > 0)
                        hive.relayNeutralizedCounter++;
                    relay.LeaveBase();
                }
            }
        }

        if (factory.enemyPool != null)
        {
            for (var i = factory.enemyCursor - 1; i > 0; i--)
            {
                ref var enemyData = ref factory.enemyPool[i];
                if (enemyData.id != i) continue;
                var combatStatId = enemyData.combatStatId;
                factory.skillSystem.OnRemovingSkillTarget(combatStatId, factory.skillSystem.combatStats.buffer[combatStatId].originAstroId, ETargetType.CombatStat);
                factory.skillSystem.combatStats.Remove(combatStatId);
                factory.KillEnemyFinally(i, ref CombatStat.empty);
            }
            factory.enemySystem.Free();
            UIRoot.instance.uiGame.dfAssaultTip.ClearAllSpots();
        }

        var planetId = planet.id;
        var gameScenario = GameMain.gameScenario;
        if (gameScenario != null)
        {
            var genPool = factory.powerSystem?.genPool;
            if (genPool != null)
            {
                foreach (var pgc in genPool)
                {
                    if (pgc.id <= 0) continue;
                    int protoId = factory.entityPool[pgc.entityId].protoId;
                    gameScenario.achievementLogic.NotifyBeforeDismantleEntity(planetId, protoId, pgc.entityId);
                    gameScenario.NotifyOnDismantleEntity(planetId, protoId, pgc.entityId);
                }
            }
        }

        if (revertReform)
        {
            factory.PlanetReformRevert();
        }

        gameData.LeavePlanet();
        var index = factory.index;
        var warningSystem = gameData.warningSystem;
        var warningPool = warningSystem.warningPool;
        for (var i = warningSystem.warningCursor - 1; i >= 0; i--)
        {
            ref var warning = ref warningPool[i];
            if (warning.id != i) continue;
            switch (warning.factoryId)
            {
                case -4:
                    if (warning.astroId == planetId)
                        warningSystem.RemoveWarningData(i);
                    break;
                case >= 0:
                    if (warning.factoryId == index)
                        warningSystem.RemoveWarningData(i);
                    break;
                default:
                    break;
            }
        }
        var isCombatMode = factory.gameData.gameDesc.isCombatMode;
        factory.entityCursor = 1;
        factory.entityRecycleCursor = 0;
        factory.entityCapacity = 0;
        factory.SetEntityCapacity(1024);
        factory.craftCursor = 1;
        factory.craftRecycleCursor = 0;
        factory.craftCapacity = 0;
        factory.SetCraftCapacity(128);
        factory.prebuildCursor = 1;
        factory.prebuildRecycleCursor = 0;
        factory.prebuildCapacity = 0;
        factory.SetPrebuildCapacity(256);
        factory.enemyCursor = 1;
        factory.enemyRecycleCursor = 0;
        factory.enemyCapacity = 0;
        factory.SetEnemyCapacity(isCombatMode ? 1024 : 32);
        factory.hashSystemDynamic = new HashSystem();
        factory.hashSystemStatic = new HashSystem();
        factory.cargoContainer = new CargoContainer();
        factory.cargoTraffic = new CargoTraffic(planet);
        factory.blockContainer = new MiniBlockContainer();
        factory.factoryStorage = new FactoryStorage(planet);
        factory.powerSystem = new PowerSystem(planet);
        factory.constructionSystem = new ConstructionSystem(planet);
        if (factory.veinPool != null)
        {
            for (var i = factory.veinPool.Length - 1; i >= 0; i--)
            {
                ref var vein = ref factory.veinPool[i];
                if (vein.id != i) continue;
                vein.minerCount = 0;
                vein.minerId0 = 0;
                vein.minerId1 = 0;
                vein.minerId2 = 0;
                vein.minerId3 = 0;
            }
        }
        factory.InitVeinHashAddress();
        factory.RecalculateAllVeinGroups();
        factory.InitVegeHashAddress();
        factory.ruinCursor = 1;
        factory.ruinRecycleCursor = 0;
        factory.ruinCapacity = 0;
        factory.SetRuinCapacity(isCombatMode ? 1024 : 32);
        factory.factorySystem = new FactorySystem(planet);
        factory.enemySystem = new EnemyDFGroundSystem(planet);
        factory.combatGroundSystem = new CombatGroundSystem(planet);
        factory.defenseSystem = new DefenseSystem(planet);
        factory.planetATField = new PlanetATField(planet);
        factory.transport = new PlanetTransport(gameData, planet);
        factory.transport.Init();
        factory.RefreshHashSystems();
        var mem = new MemoryStream();
        var writer = new BinaryWriter(mem);
        factory.platformSystem.Export(writer);
        factory.platformSystem = new PlatformSystem(planet, true);
        mem.Position = 0;
        var reader = new BinaryReader(mem);
        factory.platformSystem.Import(reader);
        mem.Close();
        mem.Dispose();
        factory.digitalSystem = new DigitalSystem(planet);

        //GameMain.data.statistics.production.CreateFactoryStat(index);
        gameData.ArrivePlanet(planet);

        foreach (var kvp in returnedItems)
        {
            var added = player.TryAddItemToPackage(kvp.Key, (int)kvp.Value.count, kvp.Value.inc, true);
            if (added > 0) UIItemup.Up(kvp.Key, added);
        }
    }

    public static void BuildOrbitalCollectors()
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        if (planet is not { type: EPlanetType.Gas }) return;
        var countToBuild = OrbitalCollectorMaxBuildCount.Value;
        if (countToBuild == 0) countToBuild = -1;

        var factory = planet.factory;
        var stationPool = factory.transport.stationPool;
        var stationCursor = factory.transport.stationCursor;
        var entityPool = factory.entityPool;
        var pos = new Vector3(0f, 0f, planet.realRadius * 1.025f + 0.2f);
        var found = false;
        for (var i = stationCursor - 1; i > 0; i--)
        {
            if (stationPool[i] == null || stationPool[i].id != i) continue;
            ref var entity = ref entityPool[stationPool[i].entityId];
            pos = entity.pos;
            found = true;
            break;
        }
        var prebuildCursor = factory.prebuildCursor;
        var prebuildPool = factory.prebuildPool;
        if (!found)
        {
            for (var i = prebuildCursor - 1; i > 0; i--)
            {
                if (prebuildPool[i].id != i) continue;
                pos = prebuildPool[i].pos;
                break;
            }
        }

        var testPos = pos;
        var cellCount = PlanetGrid.DetermineLongitudeSegmentCount(0, factory.planet.aux.mainGrid.segment) * 5;
        var cellRad = Math.PI / cellCount;
        var distRadCount = 1;
        for (var i = 1; i <= cellCount; i++)
        {
            testPos = Maths.RotateLF(0.0, 1.0, 0.0, cellRad, testPos);
            if ((testPos - pos).sqrMagnitude < 14297f) continue;
            distRadCount = i;
            break;
        }
        for (var i = 0; i < cellCount && countToBuild != 0;)
        {
            /* Check for collision */
            var collide = false;
            for (var j = stationCursor - 1; j > 0; j--)
            {
                if (stationPool[j] == null || stationPool[j].id != j) continue;
                if ((entityPool[stationPool[j].entityId].pos - pos).sqrMagnitude >= 14297f) continue;
                collide = true;
                break;
            }
            for (var j = 1; j < prebuildCursor; j++)
            {
                if (prebuildPool[j].id != j) continue;
                if ((prebuildPool[j].pos - pos).sqrMagnitude >= 14297f) continue;
                collide = true;
                break;
            }
            if (collide)
            {
                /* rotate for a small cell on sphere */
                pos = Maths.RotateLF(0.0, 1.0, 0.0, cellRad, pos);
                i++;
                continue;
            }

            if (player.inhandItemId == OrbitalCollectorItemId && player.inhandItemCount > 0)
            {
                player.UseHandItems(1, out var _);
            }
            else
            {
                var count = 1;
                var itemId = OrbitalCollectorItemId;
                player.package.TakeTailItems(ref itemId, ref count, out var _);
                if (count == 0) break;
            }

            var rot = Maths.SphericalRotation(pos, 0f);
            var prebuild = new PrebuildData
            {
                protoId = 2105,
                modelIndex = 117,
                pos = pos,
                pos2 = pos,
                rot = rot,
                rot2 = rot,
                pickOffset = 0,
                insertOffset = 0,
                recipeId = 0,
                filterId = 0,
                paramCount = 0
            };
            factory.AddPrebuildDataWithComponents(prebuild);
            prebuildCursor = factory.prebuildCursor;
            if (countToBuild > 0) countToBuild--;
            /* rotate for minimal distance for next OC on sphere */
            pos = Maths.RotateLF(0.0, 1.0, 0.0, cellRad * distRadCount, pos);
            i += distRadCount;
        }
    }
}
