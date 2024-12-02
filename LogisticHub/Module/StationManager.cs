using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UXAssist.Common;

namespace LogisticHub.Module;

public class PlanetStations
{
    public readonly List<int>[][] StorageIndices = [null, null];

    public void AddStationStorage(bool demand, int id, int stationId, int storageIdx)
    {
        var stations = StorageIndices[demand ? 1 : 0];
        if (stations == null || id >= stations.Length)
            Array.Resize(ref stations, AuxData.AlignUpToPowerOf2(id + 1));
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
        var stations = StorageIndices[demand ? 1 : 0];
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

public class StationManager : PatchImpl<StationManager>
{
    private static PlanetStations[] _stations;

    public void Init()
    {
        GameLogic.OnGameBegin += () =>
        {
            _stations = null;
            var data = GameMain.data;
            for (var index = data.factoryCount - 1; index >= 0; index--)
            {
                var factory = data.factories[index];
                if (factory == null || factory.index != index) continue;
                var planetIndex = factory.index;
                var stations = StationsByPlanet(planetIndex);
                var transport = factory.transport;
                var pool = transport.stationPool;
                for (var i = transport.stationCursor - 1; i > 0; i--)
                {
                    var station = pool[i];
                    if (station == null || station.id != i || station.isCollector || station.isVeinCollector) continue;
                    UpdateStationInfo(stations, station);
                }
            }
        };
        Enable(true);
    }

    public void Uninit()
    {
        Enable(false);
    }

    private static int ItemIdToIndex(int itemId)
    {
        return itemId switch
        {
            >= 1000 and < 2000 => itemId - 1000,
            >= 5000 and < 5050 => itemId - 5000 + 900,
            >= 6000 and < 6050 => itemId - 6000 + 950,
            _ => -1
        };
    }

    public static int IndexToItemId(int index)
    {
        return index switch
        {
            < 900 => index + 1000,
            < 950 => index - 900 + 5000,
            < 1000 => index - 950 + 6000,
            _ => -1
        };
    }

    public static PlanetStations GetStations(int planetIndex)
    {
        return _stations != null && planetIndex < _stations.Length ? _stations[planetIndex] : null;
    }

    private static PlanetStations StationsByPlanet(int planetIndex)
    {
        if (_stations == null || _stations.Length <= planetIndex)
            Array.Resize(ref _stations, AuxData.AlignUpToPowerOf2(planetIndex + 1));
        var stations = _stations[planetIndex];
        if (stations != null) return stations;
        stations = new PlanetStations();
        _stations[planetIndex] = stations;
        return stations;
    }

    private static void UpdateStationInfo(PlanetStations stations, StationComponent station, int storageIdx = -1)
    {
        var storage = station.storage;
        var stationId = station.id;
        if (storageIdx >= 0)
        {
            if (storageIdx >= storage.Length) return;
            var itemId = ItemIdToIndex(storage[storageIdx].itemId);
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
            var itemId = ItemIdToIndex(storage[i].itemId);
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
            var itemId = ItemIdToIndex(storage[storageIdx].itemId);
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

            return;
        }

        for (var i = storage.Length - 1; i >= 0; i--)
        {
            var itemId = ItemIdToIndex(storage[i].itemId);
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
        var stationId = entity.stationId;
        if (stationId <= 0) return;
        var station = factory.transport.stationPool[stationId];
        if (station == null || station.id != stationId || station.isCollector || station.isVeinCollector) return;
        UpdateStationInfo(StationsByPlanet(factory.index), station);
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