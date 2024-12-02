using System;
using System.Collections.Generic;
using HarmonyLib;
using UXAssist.Common;

namespace LogisticHub.Module;

public class PlanetStations
{
    private readonly List<int>[][] _stations = [null, null];

    public void AddStationStorage(bool demand, int id, int stationId, int storageIdx)
    {
        var stations = _stations[demand ? 1 : 0];
        if (stations == null || id >= stations.Length)
            Array.Resize(ref stations, id + 1);
        var list = stations[id];
        if (list == null)
        {
            list = [];
            stations[id] = list;
        }

        var value = stationId * 100 + storageIdx;
        var index = list.BinarySearch(value);
        if (index < 0)
            list.Insert(~index, value);
    }

    public void RemoveStationStorage(bool demand, int id, int stationId, int storageIdx)
    {
        var stations = _stations[demand ? 1 : 0];
        if (stations == null || id >= stations.Length)
            return;
        var list = stations[id];
        if (list == null)
            return;

        var value = stationId * 100 + storageIdx;
        var index = list.BinarySearch(value);
        if (index >= 0)
            list.RemoveAt(index);
    }
}

public class StationManager: PatchImpl<StationManager>
{
    private static PlanetStations[] _stations;

    public void Init()
    {
        Enable(true);
    }

    public void Uninit()
    {
        Enable(false);
    }
    
    private static PlanetStations StationsByPlanet(int planetId)
    {
        if (_stations == null || _stations.Length <= planetId)
            Array.Resize(ref _stations, planetId + 1);
        var stations = _stations[planetId];
        if (stations != null) return stations;
        stations = new PlanetStations();
        _stations[planetId] = stations;
        return stations;
    }

    private static void UpdateStationInfo(PlanetStations stations, StationComponent station, int storageIdx = -1)
    {
        var storage = station.storage;
        var stationId = station.id;
        if (storageIdx >= 0)
        {
            if (storageIdx >= storage.Length) return;
            var itemId = storage[storageIdx].itemId;
            if (itemId <= 0) return;
            var logic = storage[storageIdx].localLogic;
            switch (logic)
            {
                case ELogisticStorage.Demand:
                    stations.AddStationStorage(true, itemId, stationId, storageIdx);
                    break;
                case ELogisticStorage.Supply:
                    stations.AddStationStorage(false, itemId, stationId, storageIdx);
                    break;
                case ELogisticStorage.None:
                default:
                    break;
            }
            return;
        }
        for (var i = storage.Length - 1; i >= 0; i--)
        {
            var itemId = storage[i].itemId;
            if (itemId <= 0) continue;
            var logic = storage[i].localLogic;
            switch (logic)
            {
                case ELogisticStorage.Demand:
                    stations.AddStationStorage(true, itemId, stationId, i);
                    break;
                case ELogisticStorage.Supply:
                    stations.AddStationStorage(false, itemId, stationId, i);
                    break;
                case ELogisticStorage.None:
                default:
                    break;
            }
        }
    }

    private static void RemoveStationInfo(PlanetStations stations, StationComponent station, int storageIdx = -1)
    {
        var storage = station.storage;
        var stationId = station.id;
        if (storageIdx >= 0)
        {
            var itemId = storage[storageIdx].itemId;
            if (itemId <= 0) return;
            var logic = storage[storageIdx].localLogic;
            switch (logic)
            {
                case ELogisticStorage.Demand:
                    stations.RemoveStationStorage(true, itemId, stationId, storageIdx);
                    break;
                case ELogisticStorage.Supply:
                    stations.RemoveStationStorage(false, itemId, stationId, storageIdx);
                    break;
                case ELogisticStorage.None:
                default:
                    break;
            }
        }
        for (var i = storage.Length - 1; i >= 0; i--)
        {
            var itemId = storage[i].itemId;
            if (itemId <= 0) continue;
            var logic = storage[i].localLogic;
            switch (logic)
            {
                case ELogisticStorage.Demand:
                    stations.RemoveStationStorage(true, itemId, stationId, i);
                    break;
                case ELogisticStorage.Supply:
                    stations.RemoveStationStorage(false, itemId, stationId, i);
                    break;
                case ELogisticStorage.None:
                default:
                    break;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildingParameters), nameof(BuildingParameters.ApplyPrebuildParametersToEntity))]
    private static void BuildingParameters_ApplyPrebuildParametersToEntity_Postfix(int entityId, PlanetFactory factory)
    {
        if (entityId <= 0) return;
        ref var entity = ref factory.entityPool[entityId];
        var battleBaseId = entity.battleBaseId;
        var stationId = entity.stationId;
        if (stationId <= 0) return;
        var station = factory.transport.stationPool[stationId];
        if (station == null || station.id != stationId || station.isCollector || station.isVeinCollector) return;
        UpdateStationInfo(StationsByPlanet(factory.planetId), station);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.Import))]
    private static void PlanetTransport_Import_Postfix(PlanetTransport __instance)
    {
        var pool = __instance.stationPool;
        var stations = StationsByPlanet(__instance.planet.id);
        for (var i = __instance.stationCursor - 1; i > 0; i--)
        {
            var station = pool[i];
            if (station == null || station.id != i || station.isCollector || station.isVeinCollector) return;
            UpdateStationInfo(stations, station);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.RemoveStationComponent))]
    private static void PlanetTransport_RemoveStationComponent_Prefix(PlanetTransport __instance, int id)
    {
        if (id <= 0) return;
        var station = __instance.stationPool[id];
        if (station == null || station.id != id || station.isCollector || station.isVeinCollector) return;
        RemoveStationInfo(StationsByPlanet(__instance.planet.id), station);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
    private static void PlanetTransport_SetStationStorage_Prefix(PlanetTransport __instance, int stationId, int storageIdx, int itemId, ELogisticStorage localLogic, out bool __state)
    {
        var station = __instance.stationPool[stationId];
        if (station == null || station.id != stationId || station.isCollector || station.isVeinCollector || storageIdx < 0 || storageIdx >= station.storage.Length)
        {
            __state = false;
            return;
        }
        ref var storage = ref station.storage[storageIdx];
        if (localLogic == storage.localLogic && itemId == storage.itemId)
        {
            __state = false;
            return;
        }
        RemoveStationInfo(StationsByPlanet(__instance.planet.id), station, storageIdx);
        __state = localLogic != ELogisticStorage.None;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
    private static void PlanetTransport_SetStationStorage_Postfix(PlanetTransport __instance, int stationId, int storageIdx, bool __state)
    {
        if (!__state) return;
        var station = __instance.stationPool[stationId];
        UpdateStationInfo(StationsByPlanet(__instance.planet.id), station, storageIdx);
    }
}
