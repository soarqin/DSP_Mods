using UnityEngine;

namespace CheatEnabler;
public static class PlanetFunctions {
    public static void DismantleAll(bool toBag)
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        if (planet == null || planet.factory == null) return;
        foreach (var etd in GameMain.localPlanet.factory.entityPool)
        {
            var stationId = etd.stationId;
            if (stationId > 0)
            {
                var sc = GameMain.localPlanet.factory.transport.stationPool[stationId];
                if (toBag)
                {
                    for (var i = 0; i < sc.storage.Length; i++)
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
                GameMain.localPlanet.factory.RemoveEntityWithComponents(etd.id);
            }
        }
    }

    public static void BuryAllVeins(bool bury)
    {
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        var physics = planet.physics;
        var height = bury ? planet.realRadius - 50f : planet.realRadius + 0.07f;
        var array = factory.veinPool;
        var num = factory.veinCursor;
        for (var m = 1; m < num; m++)
        {
            var pos = array[m].pos;
            var colliderId = array[m].colliderId;
            var colliderData = physics.GetColliderData(colliderId);
            var vector = colliderData.pos.normalized * (height + 0.4f);
            physics.colChunks[colliderId >> 20].colliderPool[colliderId & 0xFFFFF].pos = vector;
            array[m].pos = pos.normalized * height;
            var quaternion = Maths.SphericalRotation(array[m].pos, Random.value * 360f);
            physics.SetPlanetPhysicsColliderDirty();
            GameMain.gpuiManager.AlterModel(array[m].modelIndex, array[m].modelId, m, array[m].pos, quaternion, false);
        }
        GameMain.gpuiManager.SyncAllGPUBuffer();
    }

    public static void RecreatePlanet(bool revertReform)
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        //planet.data = new PlanetRawData(planet.precision);
        //planet.data.CalcVerts();
        for (var id = 1; id < factory.entityCursor; id++)
        {
            var ed = factory.entityPool[id];
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

            if (ed.audioId != 0)
            {
                if (planet.audio != null)
                {
                    planet.audio.RemoveAudioData(ed.audioId);
                }
            }
        }

        if (factory.transport != null && factory.transport.stationPool != null)
        {
            foreach (var sc in factory.transport.stationPool)
            {
                if (sc == null || sc.id <= 0) continue;
                sc.storage = new StationStore[sc.storage.Length];
                sc.needs = new int[sc.needs.Length];
                int protoId = factory.entityPool[sc.entityId].protoId;
                factory.DismantleFinally(player, sc.entityId, ref protoId);
            }
        }

        if (GameMain.gameScenario != null)
        {
            if (factory.powerSystem != null && factory.powerSystem.genPool != null)
            {
                foreach (var pgc in factory.powerSystem.genPool)
                {
                    if (pgc.id <= 0) continue;
                    int protoId = factory.entityPool[pgc.entityId].protoId;
                    GameMain.gameScenario.achievementLogic.NotifyBeforeDismantleEntity(planet.id, protoId, pgc.entityId);
                    GameMain.gameScenario.NotifyOnDismantleEntity(planet.id, protoId, pgc.entityId);
                }
            }
        }

        if (revertReform)
        {
            factory.PlanetReformRevert();
        }

        planet.UnloadFactory();
        var index = factory.index;
        for (var i = 1; i < GameMain.data.warningSystem.warningCursor; i++)
        {
            if (GameMain.data.warningSystem.warningPool[i].factoryId == index)
                GameMain.data.warningSystem.RemoveWarningData(GameMain.data.warningSystem.warningPool[i].id);
        }

        factory.entityCursor = 1;
        factory.entityRecycleCursor = 0;
        factory.SetEntityCapacity(1024);
        factory.prebuildCursor = 1;
        factory.prebuildRecycleCursor = 0;
        factory.SetPrebuildCapacity(256);
        factory.cargoContainer = new CargoContainer();
        factory.cargoTraffic = new CargoTraffic(planet);
        factory.blockContainer = new MiniBlockContainer();
        factory.factoryStorage = new FactoryStorage(planet);
        factory.powerSystem = new PowerSystem(planet);
        factory.factorySystem = new FactorySystem(planet);
        factory.transport = new PlanetTransport(GameMain.data, planet);
        factory.transport.Init();
        factory.digitalSystem = new DigitalSystem(planet);
        //GameMain.data.statistics.production.CreateFactoryStat(index);
        planet.LoadFactory();
    }
}