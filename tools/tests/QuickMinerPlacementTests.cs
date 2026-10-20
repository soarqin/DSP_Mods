using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using UnityEngine;
using UXAssist.Functions.Factory;
using Candidate = UXAssist.Functions.Factory.MinerPlacementSolver.Candidate;
using Box = UXAssist.Functions.Factory.MinerPlacementGeometry.Box;

internal static class QuickMinerPlacementTests
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    private static Quaternion Yaw(float degrees)
    {
        var radians = degrees * Math.PI / 360;
        return new Quaternion(0, (float)Math.Sin(radians), 0, (float)Math.Cos(radians));
    }

    private static ColliderData Collider(Vector3 center, Vector3 halfSize, Quaternion rotation) => new()
    {
        idType = 3 << 24 | 1 << 29, pos = center, ext = halfSize, q = rotation
    };

    private static Candidate Placement(float x, int[] covered, int[] buried = null, float halfWidth = 0.4f) => new()
    {
        X = x, Z = 0, Yaw = 0, Rotation = Quaternion.identity,
        Position = new Vector3(x, 0, 0), Radius = halfWidth * 2,
        Covered = covered, Buried = buried ?? [],
        BuildBoxes = [new Box(Collider(new Vector3(x, 0, 0), new Vector3(halfWidth, 0.4f, 0.4f), Quaternion.identity))],
        ObstacleBoxes = [new Box(Collider(new Vector3(x, 0, 0), new Vector3(halfWidth, 0.4f, 0.4f), Quaternion.identity))]
    };

    private static MinerPlacementSolver Solve(List<Candidate> candidates, int nodes, int passes = 12, bool allowVeinReform = true)
    {
        var solver = new MinerPlacementSolver(candidates, nodes, allowVeinReform);
        var search = solver.Search(passes);
        var steps = 0;
        while (search.MoveNext()) if (++steps > 2000000) throw new InvalidOperationException("Search did not terminate.");
        if (MinerPlacementSolver.WorkerCount > 0)
        {
            var parallel = new MinerPlacementSolver(candidates, nodes, allowVeinReform);
            parallel.SearchAsync(passes, CancellationToken.None).GetAwaiter().GetResult();
            Check(parallel.BestScore == solver.BestScore && parallel.Best.SequenceEqual(solver.Best),
                "Parallel scheduling must preserve the exact sequential layout, including ties.");
        }
        var buried = solver.Best.SelectMany(c => c.Buried).ToHashSet();
        var expected = solver.Best.Sum(c => c.Covered.Count(n => !buried.Contains(n)));
        Check(solver.BestScore == expected, "Score does not match the final burial union.");
        for (var i = 0; i < solver.Best.Count; i++)
        {
            Check(solver.Best[i].Covered.Count(n => !buried.Contains(n)) >= MinerPlacementSolver.MinimumCoverage,
                "Placed a miner below the minimum after the full burial union.");
            Check(MinerPlacementSolver.Fits(solver.Best[i], solver.Best.Take(i).ToList()), "Plan contains colliding miners.");
        }
        var canImprove = candidates.Any(candidate =>
        {
            if (candidate.Covered.Length < MinerPlacementSolver.MinimumCoverage || !MinerPlacementSolver.Fits(candidate, solver.Best)) return false;
            var trialBurial = buried.Concat(candidate.Buried).ToHashSet();
            var counts = solver.Best.Append(candidate).Select(c => c.Covered.Count(n => !trialBurial.Contains(n))).ToArray();
            return counts.All(count => count >= MinerPlacementSolver.MinimumCoverage) && counts.Sum() > expected;
        });
        Check(!canImprove, "Search left a legal positive-gain miner unplaced in its candidate set.");
        if (allowVeinReform && candidates.All(candidate => candidate.Buried.Length == 0))
        {
            var withoutReform = Solve(candidates, nodes, passes, false);
            Check(withoutReform.BestScore == solver.BestScore && withoutReform.Best.SequenceEqual(solver.Best),
                "Skipping reform calculations must preserve the exact layout for burial-free candidates.");
        }
        return solver;
    }

    private static void SolverChecks()
    {
        for (var processors = 1; processors <= 64; processors++)
            Check(MinerPlacementSolver.GetWorkerCount(processors) <= processors / 2 && MinerPlacementSolver.GetWorkerCount(processors) <= 4,
                "Workers must not exceed four or half of the available logical processors.");
        Check(MinerPlacementSolver.GetWorkerCount(1) == 0, "Single-core machines must use the main-thread fallback.");
        var shared = new List<Candidate> { Placement(0, [0, 1, 2]), Placement(2, [0, 1, 2]), Placement(4, [0, 1, 2]) };
        Check(Solve(shared, 3).BestScore == 9, "Planning must include every compatible miner and count shared nodes per miner.");
        if (MinerPlacementSolver.WorkerCount > 0)
        {
            var oneWorker = new MinerPlacementSolver(shared, 3);
            oneWorker.SearchAsync(12, CancellationToken.None, 1).GetAwaiter().GetResult();
            Check(oneWorker.BestScore == 9, "A two-logical-processor machine must work with just one background worker.");
        }

        var dense = new List<Candidate> { Placement(0, [0, 1, 2, 3, 4], halfWidth: 1.1f), Placement(-1.4f, [0, 1, 2]), Placement(1.4f, [2, 3, 4]) };
        Check(Solve(dense, 5).BestScore == 6, "Planning must prefer two smaller miners regardless of carried stock.");

        var costlyBurial = new List<Candidate> { Placement(0, [0, 1, 2]), Placement(2, [0, 1, 2]), Placement(4, [1, 2, 3], [0]) };
        Check(Solve(costlyBurial, 4).BestScore == 6, "A higher total must not justify reducing existing miners from three nodes to two.");
        Check(Solve(costlyBurial, 4).Best.All(c => c.Buried.Length == 0), "Keep the valid burial-free layout.");
        var goodBurial = new List<Candidate> { Placement(0, [0, 1, 2, 3]), Placement(2, [1, 2, 3, 4], [0]) };
        Check(Solve(goodBurial, 5).BestScore == 7, "A beneficial burial must remain possible when every miner retains three nodes.");
        var lowAfterBurial = new List<Candidate> { Placement(0, [0, 1, 2]), Placement(2, [3, 4, 5, 6], [0]) };
        Check(Solve(lowAfterBurial, 7).Best.Count == 1, "Replace a miner instead of leaving it with only two nodes.");
        var manyNodes = new List<Candidate> { Placement(0, [0, 64, 100, 129]), Placement(2, [0, 100, 129], [64]) };
        Check(Solve(manyNodes, 130).BestScore == 6, "Coverage and burial must support groups larger than 64 nodes.");
        var manyMiners = Enumerable.Range(0, 65).Select(index => Placement(index * 2, [0, 1, 2])).ToList();
        Check(Solve(manyMiners, 3).BestScore == 195, "The collision cache must support more than 64 selected miners.");
        Check(Solve([], 5).BestScore == 0, "An obstructed patch should produce an empty plan.");
        Check(Solve([Placement(0, [0]), Placement(2, [0, 1])], 2).Best.Count == 0,
            "One- and two-node miners must be invalid even when stock is abundant.");
        var valid = Placement(4, [0, 1, 2]);
        Check(Solve([Placement(0, [0]), Placement(2, [0, 1]), valid], 3).Best.SequenceEqual([valid]),
            "Do not append marginal miners to an otherwise valid layout.");
        var burialCycle = new List<Candidate>
        {
            Placement(0, [0, 1, 2], [6]), Placement(2, [3, 4, 5], [0]), Placement(4, [6, 7, 8], [3])
        };
        Check(Solve(burialCycle, 9).BestScore == 3,
            "Reject cyclic burials that would repeatedly invalidate an earlier miner.");

        // Three columns all block three alternative rows. Duplicated high-scoring column
        // alternatives prevent randomized greedy orders from accidentally solving this trap.
        // Replacing any single column still leaves the other two blocking every better row.
        Candidate Bar(float x, float z, float width, float depth, int count)
        {
            var candidate = Placement(x, Enumerable.Range(0, count).ToArray(), halfWidth: width);
            candidate.Z = z;
            candidate.Position = new Vector3(x, 0, z);
            candidate.Radius = 10;
            candidate.Yaw = width > depth ? 90 : 0;
            candidate.Rotation = Yaw(candidate.Yaw);
            candidate.BuildBoxes = [new Box(Collider(candidate.Position, new Vector3(Math.Min(width, depth), 0.4f, Math.Max(width, depth)), candidate.Rotation))];
            candidate.ObstacleBoxes = candidate.BuildBoxes;
            return candidate;
        }
        var coordinated = Enumerable.Range(0, 96).Select(_ => Bar(-2, 0, 0.4f, 4, 9)).ToList();
        coordinated.AddRange([Bar(0, 0, 0.4f, 4, 6), Bar(2, 0, 0.4f, 4, 6)]);
        coordinated.AddRange([Bar(0, -2, 4, 0.4f, 8), Bar(0, 0, 4, 0.4f, 8), Bar(0, 2, 4, 0.4f, 8)]);
        Check(Solve(coordinated, 9).BestScore == 24, "Must repack all three miners together to escape the 21-point layout.");

        // Four columns block all four better rows, beyond the three-miner repair radius.
        // Repeated poses must not bias every exploratory pass toward the strongest column.
        var duplicated = Enumerable.Range(0, 96).Select(_ => Bar(-3, 0, 0.4f, 5, 9)).ToList();
        duplicated.AddRange([Bar(-1, 0, 0.4f, 5, 6), Bar(1, 0, 0.4f, 5, 6), Bar(3, 0, 0.4f, 5, 6)]);
        duplicated.AddRange([Bar(0, -3, 5, 0.4f, 8), Bar(0, -1, 5, 0.4f, 8), Bar(0, 1, 5, 0.4f, 8), Bar(0, 3, 5, 0.4f, 8)]);
        Check(Solve(duplicated, 9).BestScore == 32, "Pose-based starts must escape the duplicated 27-point column layout.");
        duplicated.InsertRange(0, Enumerable.Range(0, 4).Select(index => Placement(100 + index * 2, [0])));
        Check(Solve(duplicated, 9).BestScore == 32, "Filtering unrelated invalid poses must not destroy the better exploration path.");

        // Local repacking can free a distant spot outside every six-meter neighborhood.
        // The final layout must refill that spot instead of stopping after a local repair.
        var refill = Enumerable.Range(0, 96).Select(_ => Bar(0, 0, 12, 0.4f, 200)).ToList();
        refill.AddRange([Placement(-2, Enumerable.Range(0, 100).ToArray()), Placement(0, Enumerable.Range(0, 100).ToArray()),
            Placement(2, Enumerable.Range(0, 100).ToArray()), Placement(10, [100, 101, 102])]);
        Check(Solve(refill, 200, passes: 2).BestScore == 303, "Refill a valid miner outside the repacked neighborhood after freeing its footprint.");

        // A burial can be individually harmful but worthwhile when two replacements share it.
        var sharedBurial = new List<Candidate> { Placement(-4, Enumerable.Range(0, 8).ToArray()) };
        sharedBurial.AddRange([Placement(0, [8, 9, 10, 11, 12], [0, 1, 2, 3, 4, 5, 6, 7]),
            Placement(2, [8, 9, 10, 11, 12], [0, 1, 2, 3, 4, 5, 6, 7])]);
        Check(Solve(sharedBurial, 13).BestScore == 10, "Joint search must replace the invalidated miner and charge shared burial only once.");
    }

    private static void WithoutReformChecks()
    {
        var candidates = new List<Candidate>
        {
            Placement(0, [0, 1, 2, 3, 4], halfWidth: 1.1f), Placement(-1.4f, [0, 1, 2]),
            Placement(1.4f, [2, 3, 4]), Placement(4, [0, 1]), Placement(6, [0, 1, 2])
        };
        var expected = Solve(candidates, 5);
        // No burial metadata or per-node state is needed when reform is disabled.
        foreach (var candidate in candidates) candidate.Buried = null;
        var solver = new MinerPlacementSolver(candidates, 0, false);
        var search = solver.Search(12);
        while (search.MoveNext()) { }
        Check(solver.BestScore == expected.BestScore && solver.Best.SequenceEqual(expected.Best),
            "Disabled reform must work without burial metadata or per-node accounting.");
        if (MinerPlacementSolver.WorkerCount == 0) return;
        var parallel = new MinerPlacementSolver(candidates, 0, false);
        parallel.SearchAsync(12, CancellationToken.None).GetAwaiter().GetResult();
        Check(parallel.BestScore == solver.BestScore && parallel.Best.SequenceEqual(solver.Best),
            "Workers must also skip all burial calculations when reform is disabled.");
    }

    private static bool CornerOracle(ColliderData a, ColliderData b)
    {
        var axesA = new[] { a.q * Vector3.right, a.q * Vector3.up, a.q * Vector3.forward };
        var axesB = new[] { b.q * Vector3.right, b.q * Vector3.up, b.q * Vector3.forward };
        var axes = axesA.Concat(axesB).Concat(axesA.SelectMany(x => axesB.Select(y => Vector3.Cross(x, y))));
        var cornersA = Corners(a);
        var cornersB = Corners(b);
        foreach (var axis in axes)
        {
            if (axis.sqrMagnitude < 1e-10f) continue;
            var first = cornersA.Select(p => Vector3.Dot(p, axis)).ToArray();
            var second = cornersB.Select(p => Vector3.Dot(p, axis)).ToArray();
            if (first.Max() < second.Min() || second.Max() < first.Min()) return false;
        }
        return true;
    }

    private static Vector3[] Corners(ColliderData collider)
    {
        var points = new List<Vector3>();
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
            points.Add(collider.pos + collider.q * new Vector3(x * collider.ext.x, y * collider.ext.y, z * collider.ext.z));
        return points.ToArray();
    }

    private static void GeometryChecks()
    {
        var unit = new Box(Collider(Vector3.zero, Vector3.one, Quaternion.identity));
        Check(MinerPlacementGeometry.Intersects(unit, new Box(Collider(new Vector3(1.9f, 0, 0), Vector3.one, Yaw(0)))), "Overlapping boxes missed.");
        Check(!MinerPlacementGeometry.Intersects(unit, new Box(Collider(new Vector3(2.01f, 0, 0), Vector3.one, Yaw(0)))), "Separated boxes collide.");
        Check(!MinerPlacementGeometry.Intersects(unit, new Box(Collider(new Vector3(0, 3, 0), Vector3.one, Yaw(60)))), "Height separation must be respected.");
        var capsule = new ColliderData { idType = 2 << 24, pos = new Vector3(1.4f, 0, 0), ext = new Vector3(0, 3, 0), radius = 0.5f };
        Check(MinerPlacementGeometry.Intersects(unit, capsule), "Capsule side intersection missed.");
        capsule.pos.x = 1.6f;
        Check(!MinerPlacementGeometry.Intersects(unit, capsule), "Separated capsule intersects.");
        Check(Math.Abs(MinerPlacementGeometry.SegmentDistanceSquared(new Vector3(2, 2, 2), new Vector3(2, 2, 2), Vector3.one) - 3) < 1e-5, "Zero-length segment distance.");
        Check(MinerPlacementGeometry.SegmentDistanceSquared(new Vector3(-3, 0, 0), new Vector3(3, 0, 0), Vector3.one) == 0, "Segment through box distance.");

        // Native ordinary miner prefab: body and two foot-end triggers. PrefabDesc reduces
        // the final encompassing trigger to a small marker, excluding it from buildColliders.
        var native = NativeMiner(0);
        Check(MinerPlacementSolver.Fits(NativeMiner(3.1f), [native]), "Native miner feet must be allowed to interleave.");
        Check(MinerPlacementSolver.Fits(NativeMiner(3.05f), [native]), "Body-width neighbor sampling must reach the narrow interleaving interval.");
        Check(!MinerPlacementSolver.Fits(NativeMiner(3.2f), [native]), "The 0.8m coarse lattice lands on a foot-end collision.");
        Check(MinerPlacementSolver.Fits(NativeMiner(6.2f), [native, NativeMiner(3.1f)]), "Three native miners must fit in an interleaving row.");
        Check(MinerPlacementSolver.Fits(NativeMiner(3.65f, -0.6f, 15), [native]), "Angled crossing must also fit the independent native colliders.");
        Check(!MinerPlacementSolver.Fits(NativeMiner(4), [native]), "Actual foot-end collisions must still be rejected.");
        var envelope = new Box(Collider(new Vector3(0, 1.2f, -0.175f), new Vector3(2.4f, 2.25f, 3.725f), Quaternion.identity));
        var shiftedEnvelope = new Box(Collider(new Vector3(3.1f, 1.2f, -0.175f), new Vector3(2.4f, 2.25f, 3.725f), Quaternion.identity));
        Check(MinerPlacementGeometry.Intersects(envelope, shiftedEnvelope), "Interleaving fixture must expose an enclosing-box false positive.");

        var random = new System.Random(2301);
        float Next(float scale) => (float)(random.NextDouble() * 2 - 1) * scale;
        for (var i = 0; i < 2000; i++)
        {
            Quaternion Rotation()
            {
                var x = Next(1); var y = Next(1); var z = Next(1); var w = Next(1);
                var length = (float)Math.Sqrt(x * x + y * y + z * z + w * w);
                return new Quaternion(x / length, y / length, z / length, w / length);
            }
            var a = Collider(new Vector3(Next(3), Next(3), Next(3)), new Vector3(Math.Abs(Next(2)) + 0.1f, 0.4f, 1.3f), Rotation());
            var b = Collider(new Vector3(Next(3), Next(3), Next(3)), new Vector3(1.1f, 0.6f, Math.Abs(Next(2)) + 0.1f), Rotation());
            Check(MinerPlacementGeometry.Intersects(new Box(a), new Box(b)) == CornerOracle(a, b), "Oriented-box result disagrees with corner projections.");
            Check(MinerPlacementGeometry.Intersects(new Box(a), new Box(b)) == MinerPlacementGeometry.Intersects(new Box(b), new Box(a)), "Box intersection must be symmetric.");

            var start = new Vector3(Next(5), Next(5), Next(5));
            var end = new Vector3(Next(5), Next(5), Next(5));
            double Distance(double t)
            {
                var p = start + (end - start) * (float)t;
                var x = Math.Max(0, Math.Abs(p.x) - 1);
                var y = Math.Max(0, Math.Abs(p.y) - 1);
                var z = Math.Max(0, Math.Abs(p.z) - 1);
                return x * x + y * y + z * z;
            }
            double low = 0, high = 1;
            for (var n = 0; n < 60; n++)
            {
                var left = (low * 2 + high) / 3;
                var right = (low + high * 2) / 3;
                if (Distance(left) < Distance(right)) high = right; else low = left;
            }
            var expected = Distance((low + high) / 2);
            Check(Math.Abs(MinerPlacementGeometry.SegmentDistanceSquared(start, end, Vector3.one) - expected) < 0.0001, "Capsule distance disagrees with independent convex minimization.");
        }
    }

    private static ColliderData[] NativeColliders() =>
    [
        Collider(new Vector3(0, 1.2f, 0.5f), new Vector3(1.51f, 2.25f, 3.05f), Quaternion.identity),
        Collider(new Vector3(-2, 0.5f, -3.5f), new Vector3(0.4f, 1.5f, 0.4f), Quaternion.identity),
        Collider(new Vector3(2, 0.5f, -3.5f), new Vector3(0.4f, 1.5f, 0.4f), Quaternion.identity),
        Collider(new Vector3(0, 1.2f, -0.4f), Vector3.one * 0.1f, Quaternion.identity)
    ];

    private static Candidate NativeMiner(float x, float z = 0, float yaw = 0)
    {
        var colliders = NativeColliders();
        var position = new Vector3(x, 0, z);
        var rotation = Yaw(yaw);
        return new Candidate
        {
            X = x, Z = z, Yaw = yaw, Position = position, Rotation = rotation, Radius = 8, Covered = [0], Buried = [],
            BuildBoxes = colliders.Take(3).Select(c => new Box(c.BindToObject(0, 0, EObjectType.None, position, rotation))).ToArray(),
            ObstacleBoxes = colliders.Select(c => new Box(c.BindToObject(0, 0, EObjectType.None, position, rotation))).ToArray()
        };
    }

    private static readonly JsonSerializerOptions ReplayJson = new() { IncludeFields = true };

    private static void CancellationChecks()
    {
        if (MinerPlacementSolver.WorkerCount == 0) return;
        var candidates = Enumerable.Repeat(Placement(0, [0, 1, 2]), 50000).ToList();
        foreach (var beforeStart in new[] { true, false })
        {
            using var cancellation = new CancellationTokenSource();
            if (beforeStart) cancellation.Cancel();
            else cancellation.CancelAfter(10);
            var solver = new MinerPlacementSolver(candidates, 3);
            var work = solver.SearchAsync(64, cancellation.Token);
            try
            {
                work.GetAwaiter().GetResult();
                throw new InvalidOperationException("Cancelled worker search completed normally.");
            }
            catch (OperationCanceledException) { Check(work.IsCanceled, "Cancellation must join all parallel work before completing the task."); }
        }
        candidates.Clear();
        candidates.Add(Placement(0, [0, 1, 2]));
        Check(Solve(candidates, 3).BestScore == 3, "A cancelled search must not interfere with the next calculation.");
    }

    private static void ReplayChecks()
    {
        var original = NativeMiner(3.65f, -0.6f, 15);
        original.Covered = [0, 1, 2];
        var record = MinerPlacementDebug.CandidateData.Capture(original);
        var json = JsonSerializer.Serialize(record, ReplayJson);
        record = JsonSerializer.Deserialize<MinerPlacementDebug.CandidateData>(json, ReplayJson);
        var colliders = NativeColliders().Select(MinerPlacementDebug.Collider.Capture).ToArray();
        var restored = record.Restore(colliders.Take(3).ToArray(), colliders);
        Check(restored.Position == original.Position && restored.Rotation == original.Rotation, "Replay must preserve the actual spherical pose.");
        Check(restored.BuildBoxes.Zip(original.BuildBoxes).All(pair => pair.First.Center == pair.Second.Center && pair.First.Extents == pair.Second.Extents && pair.First.X == pair.Second.X && pair.First.Bounds == pair.Second.Bounds), "Replay must reconstruct each native collider exactly.");
        Check(MinerPlacementSolver.Fits(restored, [NativeMiner(0)]), "Replay must preserve legal angled crossings.");
        var snapshot = new MinerPlacementDebug.Snapshot
        {
            version = "test", gameVersion = "test", startedUtc = "2026-10-19T00:00:00Z", outcome = "completed", error = "",
            planetId = 101, veinType = 1, veinGroup = 3, initialStock = 0, placedCount = 1, totalCoverage = 3, buriedCount = 0,
            planetRadius = 200, buildRange = 80, unlimitedRange = true, allowVeinReform = false, elapsedMilliseconds = 1,
            logicalProcessors = Environment.ProcessorCount, workerCount = MinerPlacementSolver.WorkerCount,
            playerPosition = [0, 200, 0], center = [0, 200, 0], right = [1, 0, 0], forward = [0, 0, 1],
            nodes = Enumerable.Range(1, 3).Select(id => new MinerPlacementDebug.Node
            {
                id = id, type = 1, target = true, visible = true, canBury = true,
                originalPosition = [0, 200, 0], position = [0, 200, 0], collider = colliders[0]
            }).ToArray(),
            candidates = Enumerable.Repeat(record, 4000).ToArray(), buildColliders = colliders.Take(3).ToArray(), minerColliders = colliders, obstacles = [],
            existingMiners = [new MinerPlacementDebug.ExistingMiner { entityId = 2, minerId = 0, prebuild = true, position = record.position, rotation = record.rotation, veins = [1, 2, 3] }],
            phases = [new MinerPlacementDebug.Phase { name = "roundtrip", candidateCount = 4000, passes = 2, score = 3, elapsedMilliseconds = 1, searchMilliseconds = 1, selected = [0] }],
            placements = [new MinerPlacementDebug.Placement { candidate = 0, prebuildId = 1, coverObjectId = 0, itemRequired = 1, outcome = "placed", nativeCondition = "Ok", nativeVeins = [1, 2, 3], plannedVeins = [1, 2, 3], finalVeins = [1, 2, 3] }],
            rejections = new MinerPlacementDebug.Rejections { attempted = 1, range = 0, clearance = 0, mixedType = 0, noCoverage = 0, veinCollision = 0, buildingCollision = 0, ground = 0 }
        };
        var path = Path.Combine(AppContext.BaseDirectory, "replay-smoke.json");
        MinerPlacementDebug.Write(path, snapshot);
        var snapshotJson = File.ReadAllText(path);
        using var document = JsonDocument.Parse(snapshotJson);
        Check(document.RootElement.GetProperty("nodes").GetArrayLength() == 3 &&
              document.RootElement.GetProperty("candidates").GetArrayLength() == 4000 &&
              document.RootElement.GetProperty("buildColliders").GetArrayLength() == 3,
            "The production writer must serialize internal nested records, not just summary fields.");
        var roundtrip = JsonSerializer.Deserialize<MinerPlacementDebug.Snapshot>(snapshotJson, ReplayJson);
        Check(roundtrip.schema == 4 && !roundtrip.allowVeinReform && roundtrip.minimumCoverage == 3 && roundtrip.existingMiners[0].prebuild &&
              roundtrip.workerCount <= roundtrip.logicalProcessors / 2 && roundtrip.phases[0].searchMilliseconds == 1 &&
              roundtrip.initialStock == 0 && roundtrip.placements[0].itemRequired == 1 &&
              roundtrip.placements[0].plannedVeins.Length == 3 && roundtrip.placements[0].finalVeins[0] == 1 && roundtrip.rejections.attempted == 1,
            "Replay schema must retain existing miners, native placements and rejection counters.");
        Replay(path);
        snapshot.allowVeinReform = true;
        MinerPlacementDebug.Write(path, snapshot);
        Check(JsonSerializer.Deserialize<MinerPlacementDebug.Snapshot>(File.ReadAllText(path), ReplayJson).allowVeinReform,
            "Replay snapshots must preserve the enabled reform option.");
        Replay(path);
    }

    private static void Replay(string path, int minimumTotalCoverage = 0)
    {
        var snapshot = JsonSerializer.Deserialize<MinerPlacementDebug.Snapshot>(File.ReadAllText(path), ReplayJson);
        // Schema 2 snapshots also recorded a stock limit, which the current search does not use.
        Check(snapshot.schema is 1 or 2 or 3 or 4, "Unsupported debug snapshot schema.");
        Check(snapshot.nodes != null && snapshot.candidates != null && snapshot.buildColliders != null && snapshot.minerColliders != null && snapshot.phases != null,
            "This snapshot contains summary data only and cannot be replayed. Run quick miner placement again with the updated build.");
        Console.WriteLine($"Snapshot: outcome={snapshot.outcome}, nodes={snapshot.nodes.Length}, candidates={snapshot.candidates.Length}, placed={snapshot.placedCount}, actual coverage={snapshot.totalCoverage}");
        foreach (var failure in (snapshot.placements ?? []).Where(p => p.outcome != "placed").GroupBy(p => (p.outcome, p.nativeCondition)))
            Console.WriteLine($"Placement {failure.Key.outcome}: native={failure.Key.nativeCondition}, count={failure.Count()}");
        var candidates = new List<Candidate>();
        // Older snapshots always used restoration and burial, before the option existed.
        var allowVeinReform = snapshot.schema < 4 || snapshot.allowVeinReform;
        var solver = new MinerPlacementSolver(candidates, snapshot.nodes.Length, allowVeinReform);
        var parallel = new MinerPlacementSolver(candidates, snapshot.nodes.Length, allowVeinReform);
        foreach (var phase in snapshot.phases)
        {
            while (candidates.Count < phase.candidateCount)
                candidates.Add(snapshot.candidates[candidates.Count].Restore(snapshot.buildColliders, snapshot.minerColliders));
            var timer = Stopwatch.StartNew();
            var search = solver.Search(phase.passes);
            while (search.MoveNext()) { }
            var sequentialMilliseconds = timer.ElapsedMilliseconds;
            long parallelMilliseconds = 0;
            if (MinerPlacementSolver.WorkerCount > 0)
            {
                timer.Restart();
                parallel.SearchAsync(phase.passes, CancellationToken.None).GetAwaiter().GetResult();
                parallelMilliseconds = timer.ElapsedMilliseconds;
                Check(parallel.BestScore == solver.BestScore && parallel.Best.SequenceEqual(solver.Best),
                    "Recorded multi-stage replay must select the exact same layout on the worker pool.");
            }
            var buried = solver.Best.SelectMany(c => c.Buried).ToHashSet();
            Check(solver.BestScore == solver.Best.Sum(c => c.Covered.Count(n => !buried.Contains(n))), "Replay score differs from summed coverage after burial.");
            for (var i = 0; i < solver.Best.Count; i++)
            {
                Check(solver.Best[i].Covered.Count(n => !buried.Contains(n)) >= MinerPlacementSolver.MinimumCoverage,
                    "Replay selected a miner below the minimum coverage.");
                Check(MinerPlacementSolver.Fits(solver.Best[i], solver.Best.Take(i).ToList()), "Replay selected colliding miners.");
            }
            Console.WriteLine($"Replay {phase.name}: recorded={phase.score}, current={solver.BestScore}, miners={solver.Best.Count}, candidates={candidates.Count}, sequential={sequentialMilliseconds}ms, workers={MinerPlacementSolver.WorkerCount}, parallel={parallelMilliseconds}ms");
        }
        Check(solver.BestScore >= Math.Max(snapshot.totalCoverage, minimumTotalCoverage),
            $"Replay reduced total coverage: recorded={snapshot.totalCoverage}, required={minimumTotalCoverage}, current={solver.BestScore}.");
    }

    public static void Main(string[] args)
    {
        if (args.Length > 0) { Replay(args[0], args.Length > 1 ? int.Parse(args[1]) : 0); return; }
        GeometryChecks();
        SolverChecks();
        WithoutReformChecks();
        CancellationChecks();
        ReplayChecks();
        var desc = new PrefabDesc { veinMiner = true, minerType = EMinerType.Vein };
        var pose = new Pose(new Vector3(0, 200, 0), Quaternion.identity);
        Check(MinerComponent.IsTargetVeinInRange(new Vector3(0, 200, -5), pose, desc), "Native rear mining sector must be covered.");
        Check(!MinerComponent.IsTargetVeinInRange(new Vector3(0, 200, 5), pose, desc), "Front-facing resource must not be counted.");
        Check(!MinerComponent.IsTargetVeinInRange(new Vector3(0, 200, -12), pose, desc), "Distant resource must not be counted.");
        Check(!MinerComponent.IsTargetVeinInRange(new Vector3(5, 200, -4), pose, desc), "Resources outside the native sector angle must not be counted.");
        var half = (float)Math.Sqrt(0.5);
        pose = new Pose(new Vector3(200, 0, 0), new Quaternion(0, 0, -half, half));
        Check(MinerComponent.IsTargetVeinInRange(new Vector3(200, 0, -5), pose, desc), "Native range checks must work away from the north pole.");
        Console.WriteLine($"Quick miner placement: {_checks} checks passed.");
    }
}
