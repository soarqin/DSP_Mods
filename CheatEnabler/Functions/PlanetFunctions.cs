using Random = UnityEngine.Random;

namespace CheatEnabler.Functions;

public static class PlanetFunctions
{
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
            if (colliderId <= 0) continue;
            var chunkIdx = colliderId >> 20;
            var poolIdx = colliderId & 0xFFFFF;
            if (chunkIdx >= physics.colChunks.Length) continue;
            var chunk = physics.colChunks[chunkIdx];
            if (chunk == null || poolIdx >= chunk.colliderPool.Length) continue;
            var colliderData = physics.GetColliderData(colliderId);
            var vector = colliderData.pos.normalized * (height + 0.4f);
            chunk.colliderPool[poolIdx].pos = vector;
            array[m].pos = pos.normalized * height;
            var quaternion = Maths.SphericalRotation(array[m].pos, Random.value * 360f);
            physics.SetPlanetPhysicsColliderDirty();
            GameMain.gpuiManager.AlterModel(array[m].modelIndex, array[m].modelId, m, array[m].pos, quaternion, false);
        }
        GameMain.gpuiManager.SyncAllGPUBuffer();
    }
}