using System;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;
using Box = UXAssist.Functions.Factory.MinerPlacementGeometry.Box;
using Candidate = UXAssist.Functions.Factory.MinerPlacementSolver.Candidate;

namespace UXAssist.Functions.Factory;

// Use the same serializer in game and offline checks. Unity's JsonUtility omits fields whose
// types are internal, including arrays of these nested records, without reporting an error.
internal static class MinerPlacementDebug
{
    [Serializable]
    internal sealed class Snapshot
    {
        public int schema = 4;
        public string algorithm = "parallel-repack-v5-pose-exploration-min3-unlimited-stock-target-group";
        public int minimumCoverage = MinerPlacementSolver.MinimumCoverage;
        public string version, gameVersion, startedUtc, outcome, error;
        public int planetId, veinType, veinGroup, initialStock, placedCount, totalCoverage, buriedCount;
        public int logicalProcessors, workerCount;
        public float planetRadius, buildRange;
        public bool unlimitedRange, allowVeinReform;
        public long elapsedMilliseconds;
        public float[] playerPosition, center, right, forward;
        public Node[] nodes;
        public Collider[] buildColliders, minerColliders, obstacles;
        public ExistingMiner[] existingMiners;
        public CandidateData[] candidates;
        public Phase[] phases;
        public Placement[] placements;
        public Rejections rejections = new();
    }

    [Serializable]
    internal sealed class Node
    {
        public int id, type;
        public bool target, visible, canBury;
        public float[] originalPosition, position;
        public Collider collider;
    }

    [Serializable]
    internal sealed class Collider
    {
        public int idType, link;
        public float[] position, extents, rotation;
        public float radius;

        public static Collider Capture(ColliderData value) => new()
        {
            idType = value.idType, link = value.link, position = Vector(value.pos),
            extents = Vector(value.ext), rotation = Rotation(value.q), radius = value.radius
        };

        public ColliderData Restore() => new()
        {
            idType = idType, link = link, pos = ReadVector(position), ext = ReadVector(extents),
            q = ReadRotation(rotation), radius = radius
        };
    }

    [Serializable]
    internal sealed class ExistingMiner
    {
        public int entityId, minerId;
        public bool prebuild;
        public float[] position, rotation;
        public int[] veins;
    }

    [Serializable]
    internal sealed class CandidateData
    {
        public float x, z, yaw, radius;
        public float[] position, rotation;
        public int[] covered, buried;

        public static CandidateData Capture(Candidate value) => new()
        {
            x = value.X, z = value.Z, yaw = value.Yaw, radius = value.Radius,
            position = Vector(value.Position), rotation = Rotation(value.Rotation),
            covered = value.Covered, buried = value.Buried
        };

        public Candidate Restore(Collider[] build, Collider[] obstacles)
        {
            var result = new Candidate
            {
                X = x, Z = z, Yaw = yaw, Radius = radius, Position = ReadVector(position), Rotation = ReadRotation(rotation),
                Covered = covered, Buried = buried, BuildBoxes = new Box[build.Length], ObstacleBoxes = new Box[obstacles.Length]
            };
            for (var i = 0; i < build.Length; i++)
                result.BuildBoxes[i] = new Box(build[i].Restore().BindToObject(0, 0, EObjectType.None, result.Position, result.Rotation));
            for (var i = 0; i < obstacles.Length; i++)
                result.ObstacleBoxes[i] = new Box(obstacles[i].Restore().BindToObject(0, 0, EObjectType.None, result.Position, result.Rotation));
            return result;
        }
    }

    [Serializable]
    internal sealed class Phase
    {
        public string name;
        public int candidateCount, passes, score;
        public long elapsedMilliseconds, searchMilliseconds;
        public int[] selected;
    }

    [Serializable]
    internal sealed class Placement
    {
        public int candidate, prebuildId, coverObjectId, itemRequired;
        public string outcome, nativeCondition;
        public int[] nativeVeins, plannedVeins, finalVeins;
    }

    [Serializable]
    internal sealed class Rejections
    {
        public int attempted, range, clearance, mixedType, noCoverage, veinCollision, buildingCollision, ground;
    }

    public static float[] Vector(Vector3 value) => [value.x, value.y, value.z];
    public static float[] Rotation(Quaternion value) => [value.x, value.y, value.z, value.w];

    internal static void Write(string path, Snapshot snapshot)
    {
        using var stream = File.Create(path);
        var serializer = new DataContractJsonSerializer(typeof(Snapshot), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = int.MaxValue });
        serializer.WriteObject(stream, snapshot);
    }

    private static Vector3 ReadVector(float[] value) => new(value[0], value[1], value[2]);
    private static Quaternion ReadRotation(float[] value) => new(value[0], value[1], value[2], value[3]);
}
