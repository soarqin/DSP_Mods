using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UXAssist.Functions.Factory;

internal sealed class MinerPlacementSolver
{
    internal const int MinimumCoverage = 3;
    // Coordinated repacking replaces at most this many nearby miners at once.
    private const int RegionSize = 3;
    public static int WorkerCount => GetWorkerCount(Environment.ProcessorCount);
    internal static int GetWorkerCount(int processors) => Math.Min(4, Math.Max(0, processors / 2));

    internal sealed class Candidate
    {
        public float X, Z, Yaw, Radius;
        public Vector3 Position;
        public Quaternion Rotation;
        public MinerPlacementGeometry.Box[] BuildBoxes, ObstacleBoxes;
        // Covered excludes this candidate's own buried nodes. Repeated coverage is intentional.
        public int[] Covered, Buried;
    }

    private readonly List<Candidate> _candidates;
    private readonly int _nodeCount;
    private readonly bool _allowVeinReform;
    private int _bestBuriedCount;
    public List<Candidate> Best { get; private set; } = [];
    public int BestScore { get; private set; }

    // With reform disabled, the caller supplies only burial-free candidates.
    public MinerPlacementSolver(List<Candidate> candidates, int nodeCount, bool allowVeinReform = true)
    {
        _candidates = candidates;
        _nodeCount = nodeCount;
        _allowVeinReform = allowVeinReform;
    }

    public IEnumerator Search(int passes) => Search(passes, 0, CancellationToken.None);

    public Task SearchAsync(int passes, CancellationToken cancellationToken, int workers = 4)
    {
        workers = Math.Min(workers, WorkerCount);
        if (workers <= 0) throw new InvalidOperationException("Use the main-thread search when no worker is available.");
        // The caller must not mutate candidates until this task has completed. Workers only
        // read captured geometry; no Unity physics, game objects, or factory state is accessed.
        return Task.Run(() => Drain(Search(passes, workers, cancellationToken), cancellationToken), cancellationToken);
    }

    private IEnumerator Search(int passes, int workers, CancellationToken cancellationToken)
    {
        // Keep a burial-free baseline, explore different packing orders, then try individual
        // replacements before the more expensive coordinated neighborhood search.
        var results = new SearchResult[passes];
        var work = RunBatch(passes, pass => GreedyPass(pass, null, null, -1, results, pass, cancellationToken), workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        foreach (var result in results) Accept(result);
        // Candidate indices shift when a few poses become invalid. Duplicated poses also
        // get extra lottery tickets with index-based jitter. Keep the existing starts and
        // add pose-based starts whose priorities survive filtering and duplicate candidates.
        var keys = new uint[_candidates.Count];
        for (var i = 0; i < keys.Length; i++)
        {
            if ((i & 127) == 0) { cancellationToken.ThrowIfCancellationRequested(); yield return null; }
            keys[i] = PoseKey(_candidates[i]);
        }
        results = new SearchResult[Math.Max(0, passes - 2)];
        work = RunBatch(results.Length, index => GreedyPass(index + 2, keys, null, -1, results, index, cancellationToken), workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        foreach (var result in results) Accept(result);
        var seeds = new SeedCollisions(_candidates.Count);
        work = TrackBest(seeds, workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        results = new SearchResult[seeds.Seeds.Count];
        work = RunBatch(results.Length, index => GreedyPass(0, null, seeds, index, results, index, cancellationToken), workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        foreach (var result in results) Accept(result);
        for (var round = 0; round < 2 && Best.Count > 0; round++)
        {
            work = TrackBest(seeds, workers, cancellationToken);
            while (work.MoveNext()) yield return null;
            work = Repack(seeds, workers, cancellationToken);
            while (work.MoveNext()) yield return null;
            if (ReferenceEquals(seeds.Seeds, Best)) break;
        }
        // A local replacement can open space beyond its neighborhood. Saturate the full
        // candidate set once more, preserving every selected miner's minimum coverage.
        work = TrackBest(seeds, workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        results = new SearchResult[1];
        work = GreedyPass(0, null, seeds, -1, results, 0, cancellationToken);
        while (work.MoveNext()) yield return null;
        Accept(results[0]);
    }

    private static uint PoseKey(Candidate candidate)
    {
        // Quantize only the exploration key, never the geometry or collision tests.
        var x = (int)Math.Round(candidate.X * 1000f);
        var z = (int)Math.Round(candidate.Z * 1000f);
        var yaw = (int)Math.Round(((candidate.Yaw % 360f + 360f) % 360f) * 10f);
        return unchecked((uint)(x * 73856093 ^ z * 19349663 ^ yaw * 83492791));
    }

    // Without seeds, fill an empty layout in the exploration order of pass, keyed by candidate
    // index or by keys. With seeds, keep every seed except removed (all when negative) and
    // refill greedily.
    private IEnumerator GreedyPass(int pass, uint[] keys, SeedCollisions seeds, int removed, SearchResult[] results, int resultIndex,
        CancellationToken cancellationToken)
    {
        var layout = new Layout(_nodeCount, _allowVeinReform);
        // Selected miners already proven compatible with each candidate, or -1 once it cannot
        // join. Layouts and burials only grow during a pass, so every exclusion is permanent.
        var verified = new int[_candidates.Count];
        if (seeds != null)
        {
            for (var i = 0; i < seeds.Seeds.Count; i++)
                if (i != removed) layout.Add(seeds.Seeds[i]);
            for (var i = 0; i < verified.Length; i++)
                verified[i] = (removed >= 0 && ReferenceEquals(_candidates[i], seeds.Seeds[removed])) || !seeds.AllowsWithout(i, removed)
                    ? -1 : layout.Selected.Count;
        }
        else if (_allowVeinReform && pass == 0)
        {
            for (var i = 0; i < verified.Length; i++)
                if (_candidates[i].Buried.Length != 0) verified[i] = -1;
        }
        while (true)
        {
            Candidate chosen = null;
            var priority = float.NegativeInfinity;
            for (var i = 0; i < _candidates.Count; i++)
            {
                if ((i & 127) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return null;
                }
                if (verified[i] < 0) continue;
                var candidate = _candidates[i];
                var gain = layout.Gain(candidate, out var coverage);
                if (coverage < MinimumCoverage)
                {
                    verified[i] = -1;
                    continue;
                }
                if (gain <= 0) continue;
                var jitter = 1f;
                if (seeds == null && pass > 1)
                {
                    // Deterministic exploration without depending on the game's random stream.
                    var hash = (keys?[i] ?? unchecked((uint)((i + 1) * 73856093))) ^
                        unchecked((uint)((pass + 1) * 19349663 ^ (layout.Selected.Count + 1) * 83492791));
                    hash ^= hash >> 16;
                    hash = unchecked(hash * 0x7feb352dU);
                    hash ^= hash >> 15;
                    hash = unchecked(hash * 0x846ca68bU);
                    hash ^= hash >> 16;
                    jitter = 0.65f + (hash & 65535) / 65535f * 0.7f;
                }
                var value = gain * jitter;
                if (_allowVeinReform) value -= candidate.Buried.Length * 0.0001f;
                if (value <= priority) continue;
                if (!layout.FitsFrom(candidate, verified[i]) || !layout.PreservesCoverage(candidate))
                {
                    verified[i] = -1;
                    continue;
                }
                verified[i] = layout.Selected.Count;
                chosen = candidate;
                priority = value;
            }
            if (chosen == null) break;
            layout.Add(chosen);
        }
        results[resultIndex] = new SearchResult(layout.Selected, layout.Score, layout.BuriedCount);
    }

    private static void Drain(IEnumerator work, CancellationToken cancellationToken)
    {
        try { while (work.MoveNext()) cancellationToken.ThrowIfCancellationRequested(); }
        finally { (work as IDisposable)?.Dispose(); }
    }

    private static IEnumerator RunBatch(int count, Func<int, IEnumerator> create, int workers, CancellationToken cancellationToken)
    {
        if (workers > 0)
        {
            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = cancellationToken },
                index => Drain(create(index), cancellationToken));
        }
        else for (var i = 0; i < count; i++)
        {
            var work = create(i);
            try { while (work.MoveNext()) yield return null; }
            finally { (work as IDisposable)?.Dispose(); }
        }
    }

    private sealed class SearchResult(List<Candidate> selected, int score, int buriedCount)
    {
        public List<Candidate> Selected = selected;
        public int Score = score, BuriedCount = buriedCount;
    }

    // Accept replaces Best with a new list only for a strictly better layout, so an unchanged
    // reference means no improvement.
    private void Accept(SearchResult result)
    {
        if (!IsBetter(result.Score, result.BuriedCount, result.Selected.Count, BestScore, _bestBuriedCount, Best.Count)) return;
        Best = result.Selected;
        BestScore = result.Score;
        _bestBuriedCount = result.BuriedCount;
    }

    private static bool IsBetter(int score, int buried, int count, int bestScore, int bestBuried, int bestCount) =>
        score > bestScore || score == bestScore && (buried < bestBuried || buried == bestBuried && count < bestCount);

    // A legal crossing may require moving several miners together. Removing only one and
    // greedily refilling cannot escape that local optimum. Repack up to three nearby miners
    // with bounded, spatially diverse alternatives, enumerating their compatible combinations.
    private IEnumerator Repack(SeedCollisions seeds, int workers, CancellationToken cancellationToken)
    {
        var regions = Regions(seeds.Seeds);
        var results = new SearchResult[regions.Count];
        for (var i = 0; i < results.Length; i++) results[i] = new SearchResult(Best, BestScore, _bestBuriedCount);
        var work = RunBatch(regions.Count, index => RepackRegion(seeds, regions[index], results[index], cancellationToken), workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        // Merge in the original region order, independent of worker completion order.
        foreach (var result in results) Accept(result);
    }

    // Each miner alone, with each of its three nearest neighbors, and with each pair of them.
    private static List<(int, int, int)> Regions(List<Candidate> seeds)
    {
        var regions = new List<(int, int, int)>();
        var unique = new HashSet<(int, int, int)>();
        for (var pivot = 0; pivot < seeds.Count; pivot++)
        {
            Add((pivot, -1, -1));
            var neighbors = new List<int>();
            for (var i = 0; i < seeds.Count; i++) if (i != pivot) neighbors.Add(i);
            neighbors.Sort((a, b) => (seeds[a].Position - seeds[pivot].Position).sqrMagnitude.CompareTo(
                (seeds[b].Position - seeds[pivot].Position).sqrMagnitude));
            if (neighbors.Count > 3) neighbors.RemoveRange(3, neighbors.Count - 3);
            foreach (var neighbor in neighbors) Add((Math.Min(pivot, neighbor), Math.Max(pivot, neighbor), -1));
            for (var a = 0; a < neighbors.Count; a++)
            for (var b = a + 1; b < neighbors.Count; b++)
            {
                var indices = new[] { pivot, neighbors[a], neighbors[b] };
                Array.Sort(indices);
                Add((indices[0], indices[1], indices[2]));
            }
        }
        return regions;

        void Add((int, int, int) region)
        {
            if (unique.Add(region)) regions.Add(region);
        }
    }

    private static bool InRegion(int seed, (int, int, int) region) => seed == region.Item1 || seed == region.Item2 || seed == region.Item3;

    // Seed passes, repacking regions and the final refill keep a subset of the same seeds. Find
    // the seeds that block each candidate once per seed set, instead of repeating the SAT tests.
    private sealed class SeedCollisions(int candidateCount)
    {
        public List<Candidate> Seeds;
        // Counts saturate past RegionSize: no single removal can free such a candidate.
        public readonly byte[] Counts = new byte[candidateCount];
        public readonly int[] Blockers = new int[candidateCount * RegionSize];

        // A negative seed removes nothing, so only unblocked candidates qualify.
        public bool AllowsWithout(int candidate, int seed) =>
            Counts[candidate] == 0 || Counts[candidate] == 1 && Blockers[candidate * RegionSize] == seed;

        public bool Allows(int candidate, (int, int, int) region)
        {
            var count = Counts[candidate];
            if (count > RegionSize) return false;
            for (var i = 0; i < count; i++)
                if (!InRegion(Blockers[candidate * RegionSize + i], region)) return false;
            return true;
        }
    }

    // Best is replaced, never mutated, so an unchanged reference keeps the collisions valid.
    private IEnumerator TrackBest(SeedCollisions seeds, int workers, CancellationToken cancellationToken)
    {
        if (ReferenceEquals(seeds.Seeds, Best)) yield break;
        seeds.Seeds = Best;
        var chunks = Math.Max(1, workers * 4);
        var size = (_candidates.Count + chunks - 1) / chunks;
        var work = RunBatch(chunks, chunk => FindCollisions(seeds, chunk * size, Math.Min(_candidates.Count, (chunk + 1) * size), cancellationToken),
            workers, cancellationToken);
        while (work.MoveNext()) yield return null;
    }

    private IEnumerator FindCollisions(SeedCollisions seeds, int start, int end, CancellationToken cancellationToken)
    {
        for (var i = start; i < end; i++)
        {
            if (((i - start) & 31) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return null;
            }
            var count = 0;
            for (var seed = 0; seed < seeds.Seeds.Count && count <= RegionSize; seed++)
            {
                if (PairFits(_candidates[i], seeds.Seeds[seed])) continue;
                if (count < RegionSize) seeds.Blockers[i * RegionSize + count] = seed;
                count++;
            }
            seeds.Counts[i] = (byte)count;
        }
    }

    private IEnumerator RepackRegion(SeedCollisions seeds, (int, int, int) region, SearchResult result, CancellationToken cancellationToken)
    {
        var layout = new Layout(_nodeCount, _allowVeinReform);
        var removed = new List<Candidate>();
        var centerX = 0f;
        var centerZ = 0f;
        for (var i = 0; i < seeds.Seeds.Count; i++)
        {
            var seed = seeds.Seeds[i];
            if (!InRegion(i, region)) layout.Add(seed);
            else
            {
                removed.Add(seed);
                centerX += seed.X;
                centerZ += seed.Z;
            }
        }
        centerX /= removed.Count;
        centerZ /= removed.Count;
        var buckets = new List<(Candidate Candidate, int Gain)>[8];
        for (var b = 0; b < buckets.Length; b++) buckets[b] = [];
        for (var i = 0; i < _candidates.Count; i++)
        {
            if ((i & 127) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return null;
            }
            var candidate = _candidates[i];
            if (candidate.Covered.Length < MinimumCoverage) continue;
            var nearby = false;
            foreach (var seed in removed)
                if ((candidate.Position - seed.Position).sqrMagnitude <= 36f) { nearby = true; break; }
            if (!nearby || !seeds.Allows(i, region)) continue;
            var angle = Math.Atan2(candidate.Z - centerZ, candidate.X - centerX) + Math.PI;
            var bucket = Math.Min(7, (int)(angle * 4 / Math.PI));
            buckets[bucket].Add((candidate, layout.Gain(candidate, out _)));
        }
        var options = new List<Candidate>(removed);
        foreach (var bucket in buckets)
        {
            bucket.Sort((a, b) => b.Gain.CompareTo(a.Gain));
            var taken = 0;
            foreach (var entry in bucket)
            {
                var candidate = entry.Candidate;
                var duplicate = false;
                foreach (var option in options)
                {
                    var yaw = Math.Abs(candidate.Yaw - option.Yaw) % 360f;
                    if ((candidate.Position - option.Position).sqrMagnitude < 0.45f * 0.45f && Math.Min(yaw, 360f - yaw) < 6f)
                    { duplicate = true; break; }
                }
                if (duplicate) continue;
                options.Add(candidate);
                if (++taken == 12) break;
            }
        }
        options.Sort((a, b) => b.Covered.Length.CompareTo(a.Covered.Length));
        // Pruning skips most option pairs, so test their collisions only when reached.
        var pairs = new sbyte[options.Count * options.Count];
        var operations = 0;
        for (var i = 0; i < options.Count; i++)
        {
            if (!layout.CanAdd(options[i])) continue;
            layout.Add(options[i]);
            Consider(layout, result);
            for (var j = i + 1; j < options.Count; j++)
            {
                if ((operations++ & 127) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return null;
                }
                if (layout.RawScore + (RegionSize - 1) * options[j].Covered.Length < result.Score) break;
                if (!Compatible(i, j) || !layout.CanAdd(options[j])) continue;
                layout.Add(options[j]);
                Consider(layout, result);
                for (var k = j + 1; k < options.Count; k++)
                {
                    if ((operations++ & 127) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return null;
                    }
                    if (layout.RawScore + options[k].Covered.Length < result.Score) break;
                    if (!Compatible(i, k) || !Compatible(j, k) || !layout.CanAdd(options[k])) continue;
                    layout.Add(options[k]);
                    Consider(layout, result);
                    layout.RemoveLast();
                }
                layout.RemoveLast();
            }
            layout.RemoveLast();
        }

        bool Compatible(int first, int second)
        {
            var slot = first * options.Count + second;
            if (pairs[slot] == 0) pairs[slot] = PairFits(options[first], options[second]) ? (sbyte)1 : (sbyte)-1;
            return pairs[slot] > 0;
        }
    }

    // A partial layout. Score counts each visible node once per covering miner; RawScore
    // ignores burials and is therefore an upper bound for pruning.
    private sealed class Layout(int nodeCount, bool allowVeinReform)
    {
        public readonly List<Candidate> Selected = [];
        private readonly int[] _buried = allowVeinReform ? new int[nodeCount] : null,
            _uses = allowVeinReform ? new int[nodeCount] : null;
        public int Score, RawScore, BuriedCount;

        public int Gain(Candidate candidate, out int coverage)
        {
            coverage = Coverage(candidate);
            if (_buried == null) return coverage;
            var gain = coverage;
            foreach (var index in candidate.Buried) if (_buried[index] == 0) gain -= _uses[index];
            return gain;
        }

        private int Coverage(Candidate candidate)
        {
            if (_buried == null) return candidate.Covered.Length;
            var coverage = 0;
            foreach (var index in candidate.Covered) if (_buried[index] == 0) coverage++;
            return coverage;
        }

        public bool CanAdd(Candidate candidate) => Coverage(candidate) >= MinimumCoverage && PreservesCoverage(candidate);

        public bool FitsFrom(Candidate candidate, int start)
        {
            for (var i = start; i < Selected.Count; i++)
                if (!PairFits(candidate, Selected[i])) return false;
            return true;
        }

        // A placement must not invalidate an earlier miner, even if the total gain is positive.
        // Every selected miner already keeps the minimum, so only newly buried used nodes matter.
        public bool PreservesCoverage(Candidate candidate)
        {
            if (_buried == null) return true;
            var affected = false;
            foreach (var index in candidate.Buried)
                if (_buried[index] == 0 && _uses[index] > 0) { affected = true; break; }
            if (!affected) return true;
            foreach (var other in Selected)
            {
                var visible = 0;
                foreach (var index in other.Covered)
                    if (_buried[index] == 0 && Array.IndexOf(candidate.Buried, index) < 0 && ++visible >= MinimumCoverage) break;
                if (visible < MinimumCoverage) return false;
            }
            return true;
        }

        public void Add(Candidate candidate)
        {
            RawScore += candidate.Covered.Length;
            if (_buried == null)
            {
                Score = RawScore;
                Selected.Add(candidate);
                return;
            }
            foreach (var index in candidate.Buried)
                if (_buried[index]++ == 0) { Score -= _uses[index]; BuriedCount++; }
            foreach (var index in candidate.Covered)
            {
                if (_buried[index] == 0) Score++;
                _uses[index]++;
            }
            Selected.Add(candidate);
        }

        public void RemoveLast()
        {
            var candidate = Selected[Selected.Count - 1];
            Selected.RemoveAt(Selected.Count - 1);
            RawScore -= candidate.Covered.Length;
            if (_buried == null)
            {
                Score = RawScore;
                return;
            }
            foreach (var index in candidate.Covered)
            {
                if (_buried[index] == 0) Score--;
                _uses[index]--;
            }
            foreach (var index in candidate.Buried)
                if (--_buried[index] == 0) { Score += _uses[index]; BuriedCount--; }
        }
    }

    private static void Consider(Layout layout, SearchResult result)
    {
        if (!IsBetter(layout.Score, layout.BuriedCount, layout.Selected.Count, result.Score, result.BuriedCount, result.Selected.Count)) return;
        result.Selected = new List<Candidate>(layout.Selected);
        result.Score = layout.Score;
        result.BuriedCount = layout.BuriedCount;
    }

    public static bool Fits(Candidate candidate, List<Candidate> selected)
    {
        foreach (var other in selected)
        {
            if (!PairFits(candidate, other)) return false;
        }
        return true;
    }

    private static bool PairFits(Candidate candidate, Candidate other)
    {
        var radius = candidate.Radius + other.Radius;
        return (candidate.Position - other.Position).sqrMagnitude > radius * radius ||
               !Intersects(candidate.BuildBoxes, other.ObstacleBoxes) && !Intersects(other.BuildBoxes, candidate.ObstacleBoxes);
    }

    private static bool Intersects(MinerPlacementGeometry.Box[] a, MinerPlacementGeometry.Box[] b)
    {
        for (var i = 0; i < a.Length; i++)
        for (var j = 0; j < b.Length; j++)
            if (MinerPlacementGeometry.Intersects(in a[i], in b[j])) return true;
        return false;
    }
}
