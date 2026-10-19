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
    private int _bestBuriedCount;
    public List<Candidate> Best { get; private set; } = [];
    public int BestScore { get; private set; }

    public MinerPlacementSolver(List<Candidate> candidates, int nodeCount)
    {
        _candidates = candidates;
        _nodeCount = nodeCount;
    }

    public IEnumerator Search(int passes) => Search(int.MaxValue, passes);

    public IEnumerator Search(int limit, int passes) => Search(limit, passes, 0, CancellationToken.None);

    public Task SearchAsync(int limit, int passes, CancellationToken cancellationToken, int workers = 4)
    {
        workers = Math.Min(workers, WorkerCount);
        if (workers <= 0) throw new InvalidOperationException("Use the main-thread search when no worker is available.");
        // The caller must not mutate candidates until this task has completed. Workers only
        // read captured geometry; no Unity physics, game objects, or factory state is accessed.
        return Task.Run(() => Drain(Search(limit, passes, workers, cancellationToken), cancellationToken), cancellationToken);
    }

    private IEnumerator Search(int limit, int passes, int workers, CancellationToken cancellationToken)
    {
        // Keep a burial-free baseline, explore different packing orders, then try individual
        // replacements before the more expensive coordinated neighborhood search.
        var results = new SearchResult[passes];
        var work = RunBatch(passes, pass => GreedyPass(limit, pass, null, results, pass, cancellationToken), workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        foreach (var result in results) Accept(result);
        var seeds = new List<Candidate>(Best);
        results = new SearchResult[seeds.Count];
        work = RunBatch(seeds.Count, index => GreedyPass(limit, passes + index, seeds, results, index, cancellationToken), workers, cancellationToken);
        while (work.MoveNext()) yield return null;
        foreach (var result in results) Accept(result);
        var repair = Repack(limit, workers, cancellationToken);
        while (repair.MoveNext()) yield return null;
    }

    private IEnumerator GreedyPass(int limit, int pass, List<Candidate> seeds, SearchResult[] results, int resultIndex, CancellationToken cancellationToken)
    {
        var selected = new List<Candidate>();
        var buried = new bool[_nodeCount];
        var uses = new int[_nodeCount];
        var score = 0;
        if (seeds != null)
            for (var i = 0; i < seeds.Count && selected.Count < limit; i++)
                if (i != resultIndex) Add(seeds[i], selected, buried, uses, ref score);
        while (selected.Count < limit)
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
                var candidate = _candidates[i];
                if (seeds != null && ReferenceEquals(candidate, seeds[resultIndex])) continue;
                if (seeds == null && pass == 0 && candidate.Buried.Length != 0) continue;
                var gain = Gain(candidate, buried, uses, out var coverage);
                if (coverage < MinimumCoverage || gain <= 0) continue;
                // Deterministic exploration without depending on the game's random stream.
                var hash = unchecked((uint)((i + 1) * 73856093 ^ (pass + 1) * 19349663 ^ (selected.Count + 1) * 83492791));
                hash ^= hash >> 16;
                hash = unchecked(hash * 0x7feb352dU);
                hash ^= hash >> 15;
                hash = unchecked(hash * 0x846ca68bU);
                hash ^= hash >> 16;
                var jitter = pass is 0 or 1 || seeds != null ? 1f : 0.65f + (hash & 65535) / 65535f * 0.7f;
                var value = gain * jitter - candidate.Buried.Length * 0.0001f;
                if (value > priority && Fits(candidate, selected) && PreservesCoverage(candidate, selected, buried))
                {
                    chosen = candidate;
                    priority = value;
                }
            }
            if (chosen == null) break;
            Add(chosen, selected, buried, uses, ref score);
        }
        results[resultIndex] = new SearchResult(selected, score, CountBuried(selected));
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
    private IEnumerator Repack(int limit, int workers, CancellationToken cancellationToken)
    {
        for (var round = 0; round < 2 && Best.Count > 0; round++)
        {
            var seeds = new List<Candidate>(Best);
            var initialScore = BestScore;
            var initialBuried = _bestBuriedCount;
            var regions = new HashSet<(int, int, int)>();
            for (var pivot = 0; pivot < seeds.Count; pivot++)
            {
                regions.Add((pivot, -1, -1));
                var neighbors = new List<int>();
                for (var i = 0; i < seeds.Count; i++) if (i != pivot) neighbors.Add(i);
                neighbors.Sort((a, b) => (seeds[a].Position - seeds[pivot].Position).sqrMagnitude.CompareTo(
                    (seeds[b].Position - seeds[pivot].Position).sqrMagnitude));
                if (neighbors.Count > 3) neighbors.RemoveRange(3, neighbors.Count - 3);
                foreach (var neighbor in neighbors) regions.Add((Math.Min(pivot, neighbor), Math.Max(pivot, neighbor), -1));
                for (var a = 0; a < neighbors.Count; a++)
                for (var b = a + 1; b < neighbors.Count; b++)
                {
                    var indices = new[] { pivot, neighbors[a], neighbors[b] };
                    Array.Sort(indices);
                    regions.Add((indices[0], indices[1], indices[2]));
                }
            }
            // Every region keeps a subset of the same seeds. Compute each candidate/seed
            // collision once, instead of repeating the SAT tests for every overlapping region.
            var fits = new bool[seeds.Count][];
            var cacheWork = RunBatch(seeds.Count, index => CacheSeedFits(index, seeds[index], fits, cancellationToken), workers, cancellationToken);
            while (cacheWork.MoveNext()) yield return null;
            var regionList = new List<(int, int, int)>(regions);
            var results = new SearchResult[regionList.Count];
            for (var i = 0; i < results.Length; i++) results[i] = new SearchResult(Best, BestScore, _bestBuriedCount);
            var regionWork = RunBatch(regionList.Count,
                index => RepackRegion(limit, seeds, regionList[index], fits, results[index], cancellationToken), workers, cancellationToken);
            while (regionWork.MoveNext()) yield return null;
            // Merge in the original region order, independent of worker completion order.
            foreach (var result in results) Accept(result);
            if (BestScore == initialScore && _bestBuriedCount == initialBuried && Best.Count == seeds.Count) break;
        }
    }

    private IEnumerator CacheSeedFits(int index, Candidate seed, bool[][] fits, CancellationToken cancellationToken)
    {
        var row = new bool[_candidates.Count];
        for (var i = 0; i < row.Length; i++)
        {
            if ((i & 127) == 0) { cancellationToken.ThrowIfCancellationRequested(); yield return null; }
            row[i] = PairFits(_candidates[i], seed);
        }
        fits[index] = row;
    }

    private IEnumerator RepackRegion(int limit, List<Candidate> seeds, (int, int, int) region, bool[][] fits,
        SearchResult result, CancellationToken cancellationToken)
    {
        var state = new PartialLayout(_nodeCount);
        var removed = new List<Candidate>();
        var centerX = 0f;
        var centerZ = 0f;
        for (var i = 0; i < seeds.Count; i++)
        {
            if (i != region.Item1 && i != region.Item2 && i != region.Item3) state.Add(seeds[i]);
            else
            {
                removed.Add(seeds[i]);
                centerX += seeds[i].X;
                centerZ += seeds[i].Z;
            }
        }
        centerX /= removed.Count;
        centerZ /= removed.Count;
        var slots = Math.Min(3, limit - state.Selected.Count);
        if (slots <= 0) yield break;
        var buckets = new List<(Candidate Candidate, int Gain)>[8];
        for (var b = 0; b < buckets.Length; b++) buckets[b] = [];
        for (var i = 0; i < _candidates.Count; i++)
        {
            if ((i & 127) == 0) { cancellationToken.ThrowIfCancellationRequested(); yield return null; }
            var candidate = _candidates[i];
            if (candidate.Covered.Length < MinimumCoverage) continue;
            var nearby = false;
            foreach (var seed in removed)
                if ((candidate.Position - seed.Position).sqrMagnitude <= 36f) { nearby = true; break; }
            if (!nearby) continue;
            var legal = true;
            for (var j = 0; j < seeds.Count; j++)
                if (j != region.Item1 && j != region.Item2 && j != region.Item3 && !fits[j][i]) { legal = false; break; }
            if (!legal) continue;
            var angle = Math.Atan2(candidate.Z - centerZ, candidate.X - centerX) + Math.PI;
            var bucket = Math.Min(7, (int)(angle * 4 / Math.PI));
            buckets[bucket].Add((candidate, state.Gain(candidate)));
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
        var compatible = new bool[options.Count, options.Count];
        var operations = 0;
        for (var i = 0; i < options.Count; i++)
        for (var j = i + 1; j < options.Count; j++)
        {
            if ((operations++ & 127) == 0) { cancellationToken.ThrowIfCancellationRequested(); yield return null; }
            compatible[i, j] = PairFits(options[i], options[j]);
        }
        for (var i = 0; i < options.Count; i++)
        {
            state.Add(options[i]);
            if (!state.HasMinimumCoverage()) { state.RemoveLast(); continue; }
            Consider(state, result);
            for (var j = i + 1; slots > 1 && j < options.Count; j++)
            {
                if ((operations++ & 127) == 0) { cancellationToken.ThrowIfCancellationRequested(); yield return null; }
                if (state.RawScore + (slots - 1) * options[j].Covered.Length < result.Score) break;
                if (!compatible[i, j]) continue;
                state.Add(options[j]);
                if (!state.HasMinimumCoverage()) { state.RemoveLast(); continue; }
                Consider(state, result);
                for (var k = j + 1; slots > 2 && k < options.Count; k++)
                {
                    if ((operations++ & 127) == 0) { cancellationToken.ThrowIfCancellationRequested(); yield return null; }
                    if (state.RawScore + options[k].Covered.Length < result.Score) break;
                    if (!compatible[i, k] || !compatible[j, k]) continue;
                    state.Add(options[k]);
                    if (state.HasMinimumCoverage()) Consider(state, result);
                    state.RemoveLast();
                }
                state.RemoveLast();
            }
            state.RemoveLast();
        }
    }

    private sealed class PartialLayout(int nodeCount)
    {
        public readonly List<Candidate> Selected = [];
        private readonly int[] _buried = new int[nodeCount], _uses = new int[nodeCount];
        public int Score, RawScore, BuriedCount;

        public int Gain(Candidate candidate)
        {
            var gain = 0;
            foreach (var index in candidate.Covered) if (_buried[index] == 0) gain++;
            foreach (var index in candidate.Buried) if (_buried[index] == 0) gain -= _uses[index];
            return gain;
        }

        public void Add(Candidate candidate)
        {
            RawScore += candidate.Covered.Length;
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
            foreach (var index in candidate.Covered)
            {
                if (_buried[index] == 0) Score--;
                _uses[index]--;
            }
            foreach (var index in candidate.Buried)
                if (--_buried[index] == 0) { Score += _uses[index]; BuriedCount--; }
        }

        public bool HasMinimumCoverage()
        {
            foreach (var candidate in Selected)
            {
                var visible = 0;
                foreach (var index in candidate.Covered)
                    if (_buried[index] == 0 && ++visible >= MinimumCoverage) break;
                if (visible < MinimumCoverage) return false;
            }
            return true;
        }
    }

    private static void Consider(PartialLayout state, SearchResult result)
    {
        if (!IsBetter(state.Score, state.BuriedCount, state.Selected.Count, result.Score, result.BuriedCount, result.Selected.Count)) return;
        result.Selected = new List<Candidate>(state.Selected);
        result.Score = state.Score;
        result.BuriedCount = state.BuriedCount;
    }

    private static int CountBuried(List<Candidate> selected)
    {
        var buried = new HashSet<int>();
        foreach (var candidate in selected) buried.UnionWith(candidate.Buried);
        return buried.Count;
    }

    private static int Gain(Candidate candidate, bool[] buried, int[] uses, out int coverage)
    {
        coverage = 0;
        foreach (var index in candidate.Covered) if (!buried[index]) coverage++;
        var gain = coverage;
        foreach (var index in candidate.Buried) if (!buried[index]) gain -= uses[index];
        return gain;
    }

    private static void Add(Candidate candidate, List<Candidate> selected, bool[] buried, int[] uses, ref int score)
    {
        foreach (var index in candidate.Buried)
        {
            if (buried[index]) continue;
            buried[index] = true;
            score -= uses[index];
            uses[index] = 0;
        }
        foreach (var index in candidate.Covered)
        {
            if (buried[index]) continue;
            uses[index]++;
            score++;
        }
        selected.Add(candidate);
    }

    private static bool PreservesCoverage(Candidate candidate, List<Candidate> selected, bool[] buried)
    {
        if (candidate.Buried.Length == 0) return true;
        // A placement must not invalidate an earlier miner, even if the total gain is positive.
        foreach (var other in selected)
        {
            var visible = 0;
            foreach (var index in other.Covered)
                if (!buried[index] && Array.IndexOf(candidate.Buried, index) < 0 && ++visible >= MinimumCoverage) break;
            if (visible < MinimumCoverage) return false;
        }
        return true;
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
        foreach (var first in a)
        foreach (var second in b)
            if (MinerPlacementGeometry.Intersects(first, second)) return true;
        return false;
    }
}
