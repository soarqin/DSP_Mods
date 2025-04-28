using System;
using System.IO;
using System.Threading;
using BepInEx.Configuration;
using UnityEngine;

namespace UXAssist.Functions;

public static class PlanetFunctions
{
    public static ConfigEntry<int> OrbitalCollectorMaxBuildCount;
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
                if (toBag)
                {
                    for (var i = sc.storage.Length - 1; i >= 0; i--)
                    {
                        var package = player.TryAddItemToPackage(sc.storage[i].itemId, sc.storage[i].count, 0, true, etd.id);
                        UIItemup.Up(sc.storage[i].itemId, package);
                    }
                }
                sc.storage = new StationStore[sc.storage.Length];
                sc.needs = new int[sc.needs.Length];
            }
            if (toBag)
            {
                player.controller.actionBuild.DoDismantleObject(etd.id);
            }
            else
            {
                factory.RemoveEntityWithComponents(etd.id, false);
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
        for (var i = 0; i < groundCombatModule.moduleFleets.Length; i++)
        {
            var entry = groundCombatModule.moduleFleets[i];
            if (entry.fleetId <= 0 || !entry.fleetEnabled) continue;
            entry.fleetEnabled = false;
            groundCombatModule.RemoveFleetDirectly(i);
        }
        //planet.data = new PlanetRawData(planet.precision);
        //planet.data.CalcVerts();
        for (var id = factory.entityCursor - 1; id > 0; id--)
        {
            ref var ed = ref factory.entityPool[id];
            if (ed.id != id) continue;
            if (ed.colliderId != 0)
            {
                planet.physics.RemoveLinkedColliderData(ed.colliderId);
                planet.physics.NotifyObjectRemove(EObjectType.Entity, ed.id);
            }

            if (ed.modelId != 0)
            {
                GameMain.gpuiManager.RemoveModel(ed.modelIndex, ed.modelId);
            }

            if (ed.mmblockId != 0)
            {
                factory.blockContainer.RemoveMiniBlock(ed.mmblockId);
            }

            if (ed.audioId == 0) continue;
            planet.audio?.RemoveAudioData(ed.audioId);
        }

        if (planet.factory.enemyPool != null)
        {
            for (var i = planet.factory.enemyCursor - 1; i > 0; i--)
            {
                ref var enemyData = ref planet.factory.enemyPool[i];
                if (enemyData.id != i) continue;
                var combatStatId = enemyData.combatStatId;
                planet.factory.skillSystem.OnRemovingSkillTarget(combatStatId, planet.factory.skillSystem.combatStats.buffer[combatStatId].originAstroId, ETargetType.CombatStat);
                planet.factory.skillSystem.combatStats.Remove(combatStatId);
                planet.factory.KillEnemyFinally(player, i, ref CombatStat.empty);
            }
            planet.factory.enemySystem.Free();
            UIRoot.instance.uiGame.dfAssaultTip.ClearAllSpots();
        }

        var stationPool = factory.transport?.stationPool;
        if (stationPool != null)
        {
            foreach (var sc in stationPool)
            {
                if (sc is not { id: > 0 }) continue;
                sc.storage = new StationStore[sc.storage.Length];
                sc.needs = new int[sc.needs.Length];
                int protoId = factory.entityPool[sc.entityId].protoId;
                factory.DismantleFinally(player, sc.entityId, ref protoId);
            }
        }

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
                    gameScenario.achievementLogic.NotifyBeforeDismantleEntity(planet.id, protoId, pgc.entityId);
                    gameScenario.NotifyOnDismantleEntity(planet.id, protoId, pgc.entityId);
                }
            }
        }

        if (revertReform)
        {
            factory.PlanetReformRevert();
        }

        planet.UnloadFactory();
        var index = factory.index;
        var warningSystem = GameMain.data.warningSystem;
        var warningPool = warningSystem.warningPool;
        for (var i = warningSystem.warningCursor - 1; i > 0; i--)
        {
            if (warningPool[i].id == i && warningPool[i].factoryId == index)
                warningSystem.RemoveWarningData(warningPool[i].id);
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
                    if (relay != null && relay.id != i) continue;
                    if (relay.targetAstroId != astroId && relay.searchAstroId != astroId) continue;
                    relay.targetAstroId = 0;
                    relay.searchAstroId = 0;
                    if (relay.baseId > 0)
                        hive.relayNeutralizedCounter++;
                    relay.LeaveBase();
                }
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
        factory.transport = new PlanetTransport(GameMain.data, planet);
        factory.transport.Init();
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
        planet.LoadFactory();
        while (!planet.factoryLoaded)
        {
            PlanetModelingManager.Update();
            Thread.Sleep(0);
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
        for (var i = 1; i < stationCursor; i++)
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
            for (var i = 1; i < prebuildCursor; i++)
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
            for (var j = 1; j < stationCursor; j++)
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
