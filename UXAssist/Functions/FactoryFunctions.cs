using System.Collections.Generic;

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
        if (isOutput && factory.entityPool[otherObjId].beltId == belt.outputId) {
            factory.ClearObjectConnDirect(belt.entityId, 0);
            factory.ClearObjectConnDirect(otherObjId, otherSlot);
        }
        // Alter belt connections
        var (i0, i1, i2) = (belt.rightInputId, belt.backInputId, belt.leftInputId);
        cargoTraffic._arrInputs(ref i0, ref i1, ref i2);
        cargoTraffic.AlterBeltConnections(beltId, 0, i0, i1, i2);
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
            if (buildPreview?.item == null || buildPreview.objId <= 0) continue;
            int index;
            if ((index = buildPreviewsToRemove.BinarySearch(buildPreview.objId)) < 0)
                buildPreviewsToRemove.Insert(~index, buildPreview.objId);
            var isBelt = buildPreview.desc.isBelt;
            var isInserter = buildPreview.desc.isInserter;
            if (isInserter) continue;
            var objId = buildPreview.objId;
            if (isBelt)
            {
                var needCheck = false;
                for (var j = 0; j < 2; j++)
                {
                    factory.ReadObjectConn(objId, j, out _, out var connObjId, out _);
                    if (connObjId == 0 || factory.ObjectIsBelt(connObjId) || blueprintCopyTool.ObjectIsInserter(connObjId)) continue;
                    needCheck = true;
                    break;
                }
                if (needCheck)
                {
                    for (var k = 0; k < 16; k++)
                    {
                        factory.ReadObjectConn(objId, k, out _, out var connObjId, out _);
                        if (connObjId != 0 && (index = buildPreviewsToRemove.BinarySearch(connObjId)) < 0 && (factory.ObjectIsBelt(connObjId) || blueprintCopyTool.ObjectIsInserter(connObjId)))
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
                        if (connObjId2 == 0 || (index = buildPreviewsToRemove.BinarySearch(connObjId2)) >= 0 || factory.ObjectIsBelt(connObjId2) || blueprintCopyTool.ObjectIsInserter(connObjId2)) continue;
                        buildPreviewsToRemove.Insert(~index, connObjId2);
                        break;
                    }
                }
                continue;
            }
            if (buildPreview.desc.addonType == EAddonType.Belt) continue;
            for (var j = 0; j < 16; j++)
            {
                factory.ReadObjectConn(objId, j, out _, out var connObjId, out _);
                if (connObjId != 0 && (index = buildPreviewsToRemove.BinarySearch(connObjId)) < 0 && (factory.ObjectIsBelt(connObjId) || blueprintCopyTool.ObjectIsInserter(connObjId)))
                    buildPreviewsToRemove.Insert(~index, connObjId);
            }
        }
        foreach (var objId in buildPreviewsToRemove)
        {
            build.DoDismantleObject(objId);
        }
        buildPreviewsToRemove = null;
        blueprintCopyTool.ClearSelection();
        blueprintCopyTool.ClearPreSelection();
        blueprintCopyTool.ResetBlueprint();
        blueprintCopyTool.ResetBuildPreviews();
        blueprintCopyTool.RefreshBlueprintData();
    }
}
