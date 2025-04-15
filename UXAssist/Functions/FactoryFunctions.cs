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
}
