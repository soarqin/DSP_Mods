using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Functions;

public static class FactoryFunctions
{
    public static void CutConveyorBelt(CargoTraffic cargoTraffic, int beltId)
    {
        ref var belt = ref cargoTraffic.beltPool[beltId];
        if (belt.id != beltId || belt.outputId <= 0) return;

        // Clear entity connection
        var factory = cargoTraffic.factory;
        factory.ReadObjectConn(belt.entityId, 0, out var isOutput, out var otherObjId, out var otherSlot);
        if (isOutput && factory.entityPool[otherObjId].beltId == belt.outputId)
        {
            factory.ClearObjectConnDirect(belt.entityId, 0);
            factory.ClearObjectConnDirect(otherObjId, otherSlot);
        }
        // Alter belt connections
        var (i0, i1, i2) = (belt.rightInputId, belt.backInputId, belt.leftInputId);
        cargoTraffic._arrInputs(ref i0, ref i1, ref i2);
        cargoTraffic.AlterBeltConnections(beltId, 0, i0, i1, i2);
    }

    public static bool ObjectIsBeltOrInserter(PlanetFactory factory, int objId)
    {
		if (objId == 0) return false;
		ItemProto proto = LDB.items.Select(objId > 0 ? factory.entityPool[objId].protoId : factory.prebuildPool[-objId].protoId);
		return proto != null && (proto.prefabDesc.isBelt || proto.prefabDesc.isInserter);
    }

    public static void DismantleBlueprintSelectedBuildings()
    {
        var player = GameMain.mainPlayer;
        var build = player?.controller?.actionBuild;
        if (build == null) return;
        var blueprintCopyTool = build.blueprintCopyTool;
        if (blueprintCopyTool == null || !blueprintCopyTool.active) return;
        var factory = build.factory;
        List<int> buildPreviewsToRemove = [];
        foreach (var buildPreview in blueprintCopyTool.bpPool)
        {
            if (buildPreview?.item == null) continue;
            var objId = buildPreview.objId;
            if (objId == 0) continue;
            int index;
            if ((index = buildPreviewsToRemove.BinarySearch(objId)) < 0)
                buildPreviewsToRemove.Insert(~index, objId);
            var isBelt = buildPreview.desc.isBelt;
            var isInserter = buildPreview.desc.isInserter;
            if (isInserter) continue;
            if (isBelt)
            {
                var needCheck = false;
                for (var j = 0; j < 2; j++)
                {
                    factory.ReadObjectConn(objId, j, out _, out var connObjId, out _);
                    if (connObjId == 0 || ObjectIsBeltOrInserter(factory, connObjId)) continue;
                    needCheck = true;
                    break;
                }
                if (needCheck)
                {
                    for (var k = 0; k < 16; k++)
                    {
                        factory.ReadObjectConn(objId, k, out _, out var connObjId, out _);
                        if (connObjId != 0 && (index = buildPreviewsToRemove.BinarySearch(connObjId)) < 0 && ObjectIsBeltOrInserter(factory, connObjId))
                            buildPreviewsToRemove.Insert(~index, connObjId);
                    }
                }
                for (var m = 0; m < 4; m++)
                {
                    factory.ReadObjectConn(objId, m, out _, out var connObjId, out _);
                    if (connObjId == 0 || !factory.ObjectIsBelt(connObjId) || buildPreviewsToRemove.BinarySearch(connObjId) >= 0) continue;
                    for (var j = 0; j < 2; j++)
                    {
                        factory.ReadObjectConn(connObjId, j, out _, out var connObjId2, out _);
                        if (connObjId2 == 0 || (index = buildPreviewsToRemove.BinarySearch(connObjId2)) >= 0 || ObjectIsBeltOrInserter(factory, connObjId2)) continue;
                        buildPreviewsToRemove.Insert(~index, connObjId);
                        break;
                    }
                }
                continue;
            }
            if (buildPreview.desc.addonType == EAddonType.Belt) continue;
            for (var j = 0; j < 16; j++)
            {
                factory.ReadObjectConn(objId, j, out _, out var connObjId, out _);
                if (connObjId != 0 && (index = buildPreviewsToRemove.BinarySearch(connObjId)) < 0 && ObjectIsBeltOrInserter(factory, connObjId))
                    buildPreviewsToRemove.Insert(~index, connObjId);
            }
        }
        var entityPool = factory.entityPool;
        var stationPool = factory.transport.stationPool;
        foreach (var objId in buildPreviewsToRemove)
        {
            if (objId > 0)
            {
                int stationId = entityPool[objId].stationId;
                if (stationId > 0)
                {
                    StationComponent sc = stationPool[stationId];
                    if (sc.id != stationId) continue;
                    for (int i = 0; i < sc.storage.Length; i++)
                    {
                        int package = player.TryAddItemToPackage(sc.storage[i].itemId, sc.storage[i].count, sc.storage[i].inc, true, objId);
                        UIItemup.Up(sc.storage[i].itemId, package);
                    }
                    sc.storage = new StationStore[sc.storage.Length];
                    sc.needs = new int[sc.needs.Length];
                }
            }
            build.DoDismantleObject(objId);
        }
        blueprintCopyTool.ClearSelection();
        blueprintCopyTool.ClearPreSelection();
        blueprintCopyTool.ResetBlueprint();
        blueprintCopyTool.ResetBuildPreviews();
        blueprintCopyTool.RefreshBlueprintData();
    }

    public static void SelectAllBuildingsInBlueprintCopy()
    {
        var localFactory = GameMain.localPlanet?.factory;
        if (localFactory == null) return;
        var blueprintCopyTool = GameMain.mainPlayer?.controller?.actionBuild?.blueprintCopyTool;
        if (blueprintCopyTool == null || !blueprintCopyTool.active) return;
        var entityPool = localFactory.entityPool;
        foreach (var entity in entityPool)
        {
            if (entity.id == 0) continue;
            blueprintCopyTool.preSelectObjIds.Add(entity.id);
            blueprintCopyTool.selectedObjIds.Add(entity.id);
        }
        var prebuildPool = localFactory.prebuildPool;
        foreach (var prebuild in prebuildPool)
        {
            if (prebuild.id == 0) continue;
            blueprintCopyTool.preSelectObjIds.Add(-prebuild.id);
            blueprintCopyTool.selectedObjIds.Add(-prebuild.id);
        }
        blueprintCopyTool.RefreshBlueprintData();
        blueprintCopyTool.DeterminePreviews();
    }

    private struct BPBuildingData
    {
        public BlueprintBuilding building;
        public int itemType;
        public double offset;
    }

    private struct BPBeltData
    {
        public BlueprintBuilding building;
        public double offset;
    }

    private static HashSet<int> _itemIsBelt = null;
    private static Dictionary<int, int> _upgradeTypes = null;

    public static void SortBlueprintData(BlueprintData blueprintData)
    {
        // Initialize itemIsBelt and upgradeTypes
        if (_itemIsBelt == null)
        {
            _itemIsBelt = [];
            _upgradeTypes = [];
            foreach (var proto in LDB.items.dataArray)
            {
                if (proto.prefabDesc?.isBelt ?? false)
                {
                    _itemIsBelt.Add(proto.ID);
                    continue;
                }
                if (proto.Upgrades != null && proto.Upgrades.Length > 0)
                {
                    var minUpgrade = proto.Upgrades.Min(u => u);
                    if (minUpgrade != 0 && minUpgrade != proto.ID)
                    {
                        _upgradeTypes.Add(proto.ID, minUpgrade);
                    }
                }
            }
        }

        // Separate belt and non-belt buildings
        List<BPBuildingData> bpBuildings = [];
        Dictionary<BlueprintBuilding, BPBeltData> bpBelts = [];
        foreach (var building in blueprintData.buildings)
        {
            var offset = building.areaIndex * 1073741824.0 + building.localOffset_y * 262144.0 + building.localOffset_x * 1024.0 + building.localOffset_z;
            if (_itemIsBelt.Contains(building.itemId))
            {
                bpBelts.Add(building, new BPBeltData { building = building, offset = offset });
            }
            else
            {
                var itemType = _upgradeTypes.TryGetValue(building.itemId, out var upgradeType) ? upgradeType : building.itemId;
                bpBuildings.Add(new BPBuildingData { building = building, itemType = itemType, offset = offset });
            }
        }
        HashSet<BlueprintBuilding> beltsWithInput = [.. bpBelts.Select(pair => pair.Value.building.outputObj)];
        var beltHeads = bpBelts.Where(pair => !beltsWithInput.Contains(pair.Value.building)).ToDictionary(pair => pair.Key, pair => pair.Value);
        // Sort belt buildings
        List<BlueprintBuilding> sortedBpBelts = [];
        // Deal with non-cycle belt paths
        foreach (var pair in beltHeads.OrderByDescending(pair => pair.Value.offset))
        {
            var building = pair.Key;
            while (building != null)
            {
                if (!bpBelts.Remove(building)) break;
                sortedBpBelts.Add(building);
                building = building.outputObj;
            }
        }
        // Deal with cycle belt paths
        foreach (var pair in bpBelts.OrderByDescending(pair => pair.Value.offset))
        {
            var building = pair.Key;
            while (building != null)
            {
                if (!bpBelts.Remove(building)) break;
                sortedBpBelts.Add(building);
                building = building.outputObj;
            }
        }

        // Sort non-belt buildings
        bpBuildings.Sort((a, b) =>
        {
            var sign = b.itemType.CompareTo(a.itemType);
            if (sign != 0) return sign;

            sign = b.building.modelIndex.CompareTo(a.building.modelIndex);
            if (sign != 0) return sign;

            sign = b.building.recipeId.CompareTo(a.building.recipeId);
            if (sign != 0) return sign;

            sign = a.building.areaIndex.CompareTo(b.building.areaIndex);
            if (sign != 0) return sign;

            return b.offset.CompareTo(a.offset);
        });

        // Concatenate sorted belts and non-belt buildings
        sortedBpBelts.Reverse();
        blueprintData.buildings = [.. bpBuildings.Select(b => b.building), .. sortedBpBelts];
        var buildings = blueprintData.buildings;

        for (var i = buildings.Length - 1; i >= 0; i--)
        {
            buildings[i].index = i;
        }
    }
}
