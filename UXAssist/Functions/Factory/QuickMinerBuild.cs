using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using UXAssist.Common.ModFeatures;
using UXAssist.Patches.Factory;
using Candidate = UXAssist.Functions.Factory.MinerPlacementSolver.Candidate;
using Box = UXAssist.Functions.Factory.MinerPlacementGeometry.Box;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Functions.Factory;

[ModFeature("QuickMinerBuild")]
public static class QuickMinerBuild
{
    private static Job _job;
    private static IEnumerator _work;
    private static readonly Stopwatch FrameTimer = new();
    private static UIRealtimeTip _resultTip;
    private static string _resultText;
    private static float _resultTipUntil, _resultTipSpeed;
    public static bool IsRunning => _job != null;

    public static void Init() => GameLogicProc.OnGameEnd += OnGameEnd;

    public static void Uninit()
    {
        OnGameEnd();
        GameLogicProc.OnGameEnd -= OnGameEnd;
    }

    public static void Toggle()
    {
        ReleaseResultTip(true);
        if (IsRunning)
        {
            Cancel();
            UIRealtimeTip.Popup(I18NKeys.QuickMinerCancelled.Translate());
            return;
        }
        var player = GameMain.mainPlayer;
        var planet = GameMain.localPlanet;
        if (!GameMain.isRunning || DSPGame.IsMenuDemo || player?.controller?.actionBuild == null ||
            planet?.factory == null || !planet.factoryLoaded || planet.physics == null || planet.type == EPlanetType.Gas)
        {
            UIRealtimeTip.Popup(I18NKeys.QuickMinerNeedPlanet.Translate());
            return;
        }
        try
        {
            _job = new Job(player, planet);
            _work = _job.Run();
            UIRealtimeTip.Popup(I18NKeys.QuickMinerSearching.Translate());
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    public static void OnUpdate()
    {
        UpdateResultTip();
        if (_job == null) return;
        if (!_job.IsCurrent)
        {
            Cancel();
            return;
        }
        if (GameMain.isPaused) return;
        try
        {
            FrameTimer.Restart();
            while (FrameTimer.ElapsedMilliseconds < 6)
            {
                if (_work.MoveNext())
                {
                    if (_job.WaitingForWorkers) break;
                    continue;
                }
                Cancel();
                break;
            }
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    private static void Fail(Exception exception)
    {
        UXAssist.Logger.LogError($"Quick miner placement failed: {exception}");
        _job?.RecordFailure(exception);
        Cancel();
        UIRealtimeTip.Popup(I18NKeys.QuickMinerFailed.Translate());
    }

    private static void Cancel()
    {
        _job?.Cancel();
        (_work as IDisposable)?.Dispose();
        _work = null;
        _job = null;
    }

    private static void OnGameEnd()
    {
        Cancel();
        ReleaseResultTip(true);
    }

    private static void ShowResult(string message)
    {
        UXAssist.Logger.LogInfo($"Quick miner result: {message.Replace("\n", " | ")}");
        UIRealtimeTip.Popup(message);
        foreach (var tip in UIRoot.instance.uiGame.generalTips.realtimeTips)
        {
            if (!tip.gameObject.activeSelf || tip.textComp.text != message) continue;
            _resultTip = tip;
            _resultText = message;
            _resultTipSpeed = tip.upSpeed;
            _resultTipUntil = Time.unscaledTime + 8f;
            tip.upSpeed = 0f;
            break;
        }
    }

    private static void UpdateResultTip()
    {
        if (!_resultTip) return;
        if (!_resultTip.gameObject.activeSelf || _resultTip.textComp.text != _resultText || Time.unscaledTime >= _resultTipUntil)
        {
            ReleaseResultTip(false);
            return;
        }
        // Native Update derives scale and alpha from lifeTime; values above 1 hide the tip.
        _resultTip.lifeTime = 0.8f;
    }

    private static void ReleaseResultTip(bool hide)
    {
        if (_resultTip && _resultTip.textComp.text == _resultText)
        {
            _resultTip.upSpeed = _resultTipSpeed;
            if (hide) _resultTip.gameObject.SetActive(false);
        }
        _resultTip = null;
        _resultText = null;
    }

    private sealed class Node
    {
        public int Id;
        public Vector3 Position, OriginalPosition;
        public ColliderData Collider;
        public bool Target, Visible, CanBury;
        public EVeinType Type;
    }

    private sealed class Job
    {
        private readonly Player _player;
        private readonly PlanetData _planet;
        private readonly PlanetFactory _factory;
        private readonly PlanetPhysics _physics;
        private readonly ItemProto _item;
        private readonly List<Node> _nodes = [];
        private readonly List<ColliderData> _obstacles = [];
        private readonly List<Candidate> _candidates = [];
        private readonly List<int> _covered = [], _buried = [];
        private readonly Box[] _buildBoxes, _obstacleBoxes;
        private readonly ColliderData[] _nativeObstacles;
        private readonly float _footprintRadius;
        private readonly MinerPlacementDebug.Snapshot _debug = new();
        private readonly List<MinerPlacementDebug.Phase> _phases = [];
        private readonly List<MinerPlacementDebug.Placement> _placements = [];
        private readonly Stopwatch _timer = Stopwatch.StartNew();
        private readonly CancellationTokenSource _cancellation = new();
        private readonly int _workers = MinerPlacementSolver.WorkerCount;
        private Task _search;
        private long _searchMilliseconds;
        private string _result;
        private Vector3 _center, _right, _forward;
        private EVeinType _type;
        private float _minX, _maxX, _minZ, _maxZ;

        public bool IsCurrent => GameMain.isRunning && GameMain.mainPlayer == _player &&
            GameMain.localPlanet == _planet && _planet.factory == _factory && _planet.physics == _physics && _planet.factoryLoaded;
        public bool WaitingForWorkers => _search != null && !_search.IsCompleted;

        public Job(Player player, PlanetData planet)
        {
            _player = player;
            _planet = planet;
            _factory = planet.factory;
            _physics = planet.physics;
            _item = LDB.items.Select(ItemIds.MiningMachine);
            var desc = _item.prefabDesc;
            _buildBoxes = new Box[desc.buildColliders.Length];
            var obstacles = new List<ColliderData>();
            foreach (var collider in desc.colliders)
            {
                if (!collider.isForBuild) continue;
                if (collider.shape != EColliderShape.Box) throw new NotSupportedException("Mining Machine build collider is not a box.");
                obstacles.Add(collider);
                _footprintRadius = Mathf.Max(_footprintRadius, collider.pos.magnitude + collider.ext.magnitude);
            }
            _nativeObstacles = obstacles.ToArray();
            _obstacleBoxes = new Box[_nativeObstacles.Length];
            _debug.version = PluginInfo.PLUGIN_VERSION;
            _debug.gameVersion = GameConfig.gameVersion.ToString();
            _debug.startedUtc = DateTime.UtcNow.ToString("O");
            _debug.logicalProcessors = Environment.ProcessorCount;
            _debug.workerCount = _workers;
            _debug.planetId = planet.id;
            _debug.planetRadius = planet.realRadius;
            _debug.initialStock = player.package.GetItemCount(ItemIds.MiningMachine) +
                (player.inhandItemId == ItemIds.MiningMachine ? player.inhandItemCount : 0);
            _debug.playerPosition = MinerPlacementDebug.Vector(player.position);
            _debug.buildRange = player.mecha.buildArea;
            _debug.unlimitedRange = FactoryPatch.RemoveBuildRangeLimitEnabled.Value;
            _debug.buildColliders = desc.buildColliders.Select(MinerPlacementDebug.Collider.Capture).ToArray();
            _debug.minerColliders = _nativeObstacles.Select(MinerPlacementDebug.Collider.Capture).ToArray();
            _debug.outcome = "cancelled";
        }

        public IEnumerator Run()
        {
            try
            {
                var work = RunCore();
                try
                {
                    while (true)
                    {
                        bool next;
                        try { next = work.MoveNext(); }
                        catch (Exception exception) { RecordFailure(exception); throw; }
                        if (!next) break;
                        yield return null;
                    }
                }
                finally { (work as IDisposable)?.Dispose(); }
            }
            finally
            {
                var location = WriteDebug();
                if (_result != null) ShowResult(_result + "\n" + location);
            }
        }

        public void RecordFailure(Exception exception)
        {
            _debug.outcome = "failed";
            _debug.error = exception.ToString();
        }

        public void Cancel()
        {
            _cancellation.Cancel();
            if (_search == null || _search.IsCompleted)
            {
                if (_search?.IsFaulted == true) _ = _search.Exception;
                _cancellation.Dispose();
            }
            else _search.ContinueWith(task =>
            {
                if (task.IsFaulted) _ = task.Exception;
                _cancellation.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private IEnumerator Search(MinerPlacementSolver solver, int passes)
        {
            var timer = Stopwatch.StartNew();
            if (_workers > 0)
            {
                _search = solver.SearchAsync(int.MaxValue, passes, _cancellation.Token, _workers);
                while (!_search.IsCompleted) yield return null;
                _search.GetAwaiter().GetResult();
                _search = null;
            }
            else
            {
                var work = solver.Search(passes);
                while (work.MoveNext()) yield return null;
            }
            _searchMilliseconds = timer.ElapsedMilliseconds;
        }

        private IEnumerator RunCore()
        {
            if (!FindNodes())
            {
                _debug.outcome = "no-veins";
                _result = I18NKeys.QuickMinerNoVeins.Translate();
                yield break;
            }
            RestoreTargetVeins();
            var reach = 9.5f * _planet.realRadius / 200f;
            var width = _maxX - _minX + reach * 2;
            var depth = _maxZ - _minZ + reach * 2;
            // Bound the coarse search by 4,000 surface positions, then refine promising layouts.
            var step = Mathf.Max(0.8f, Mathf.Sqrt(width * depth / 4000f));
            for (var x = _minX - reach; x <= _maxX + reach; x += step)
            for (var z = _minZ - reach; z <= _maxZ + reach; z += step)
            {
                for (var yaw = 0f; yaw < 360f; yaw += 15f)
                {
                    TryCandidate(x, z, yaw);
                    yield return null;
                }
            }
            var solver = new MinerPlacementSolver(_candidates, _nodes.Count);
            var search = Search(solver, 10);
            while (search.MoveNext()) yield return null;
            RecordPhase("coarse", solver, 10);
            var packing = PackingNeighbors(solver.Best);
            while (packing.MoveNext()) yield return null;
            var refine = Refine(solver.Best, 0.35f, 3f);
            while (refine.MoveNext()) yield return null;
            search = Search(solver, 6);
            while (search.MoveNext()) yield return null;
            RecordPhase("refine-position", solver, 6);
            packing = PackingNeighbors(solver.Best);
            while (packing.MoveNext()) yield return null;
            refine = Refine(solver.Best, 0.12f, 1f);
            while (refine.MoveNext()) yield return null;
            search = Search(solver, 4);
            while (search.MoveNext()) yield return null;
            RecordPhase("refine-angle", solver, 4);
            if (IsCurrent)
            {
                _debug.outcome = "completed";
                Commit(solver.Best);
            }
        }

        private void RecordPhase(string name, MinerPlacementSolver solver, int passes) => _phases.Add(new MinerPlacementDebug.Phase
        {
            name = name, candidateCount = _candidates.Count, limit = int.MaxValue, passes = passes, score = solver.BestScore,
            searchMilliseconds = _searchMilliseconds,
            selected = solver.Best.Select(candidate => _candidates.IndexOf(candidate)).ToArray(), elapsedMilliseconds = _timer.ElapsedMilliseconds
        });

        private string WriteDebug()
        {
            try
            {
                _debug.elapsedMilliseconds = _timer.ElapsedMilliseconds;
                _debug.veinType = (int)_type;
                _debug.center = MinerPlacementDebug.Vector(_center);
                _debug.right = MinerPlacementDebug.Vector(_right);
                _debug.forward = MinerPlacementDebug.Vector(_forward);
                _debug.nodes = _nodes.Select(node => new MinerPlacementDebug.Node
                {
                    id = node.Id, type = (int)node.Type, target = node.Target, visible = node.Visible, canBury = node.CanBury,
                    originalPosition = MinerPlacementDebug.Vector(node.OriginalPosition), position = MinerPlacementDebug.Vector(node.Position),
                    collider = MinerPlacementDebug.Collider.Capture(node.Collider)
                }).ToArray();
                _debug.obstacles = _obstacles.Select(MinerPlacementDebug.Collider.Capture).ToArray();
                _debug.candidates = _candidates.Select(MinerPlacementDebug.CandidateData.Capture).ToArray();
                _debug.phases = _phases.ToArray();
                _debug.placements = _placements.ToArray();
                const string relative = "debug/UXAssist/QuickMiner";
                var directory = Path.Combine(Paths.BepInExRootPath, relative);
                Directory.CreateDirectory(directory);
                var file = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-planet-{_debug.planetId}-{Guid.NewGuid():N}.json");
                MinerPlacementDebug.Write(file, _debug);
                UXAssist.Logger.LogInfo($"Quick miner debug: {file}");
                return string.Format(I18NKeys.QuickMinerDebugSaved.Translate(), "BepInEx/" + relative);
            }
            catch (Exception exception)
            {
                UXAssist.Logger.LogWarning($"Quick miner debug export failed: {exception}");
                return I18NKeys.QuickMinerDebugFailed.Translate();
            }
        }

        private bool FindNodes()
        {
            var nearest = float.PositiveInfinity;
            var group = 0;
            for (var i = 1; i < _factory.veinCursor; i++)
            {
                ref var vein = ref _factory.veinPool[i];
                if (vein.id != i || vein.amount <= 0 || vein.type is EVeinType.None or EVeinType.Oil) continue;
                var distance = (vein.pos.normalized * _planet.realRadius - _player.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                group = vein.groupIndex;
                _type = vein.type;
            }
            if (group == 0) return false;
            _debug.veinGroup = group;
            var count = 0;
            for (var i = 1; i < _factory.veinCursor; i++)
            {
                ref var vein = ref _factory.veinPool[i];
                if (vein.id != i || vein.amount <= 0 || vein.groupIndex != group) continue;
                _center += vein.pos.normalized;
                count++;
            }
            if (count == 0) return false;
            _center = _center.normalized * (_planet.realRadius + 0.2f);
            var rotation = Maths.SphericalRotation(_center, 0f);
            _right = rotation * Vector3.right;
            _forward = rotation * Vector3.forward;
            var radius = 0f;
            for (var i = 1; i < _factory.veinCursor; i++)
            {
                ref var vein = ref _factory.veinPool[i];
                if (vein.id != i || vein.amount <= 0 || vein.groupIndex != group) continue;
                var delta = vein.pos.normalized * (_planet.realRadius + 0.2f) - _center;
                var x = Vector3.Dot(delta, _right);
                var z = Vector3.Dot(delta, _forward);
                _minX = Mathf.Min(_minX, x);
                _maxX = Mathf.Max(_maxX, x);
                _minZ = Mathf.Min(_minZ, z);
                _maxZ = Mathf.Max(_maxZ, z);
                radius = Mathf.Max(radius, delta.magnitude);
            }
            radius += 20f * _planet.realRadius / 200f + _footprintRadius;
            // Read the native data, including inactive colliders outside the camera/player area.
            // Physics queries alone can miss those colliders when the config window is open.
            foreach (var chunk in _physics.colChunks)
            {
                for (var i = 1; i < chunk.cursor; i++)
                {
                    var collider = chunk.colliderPool[i];
                    var bound = radius + collider.ext.magnitude + collider.radius;
                    if (collider.idType != 0 && collider.isForBuild && (collider.pos - _center).sqrMagnitude <= bound * bound)
                        _obstacles.Add(collider);
                }
            }
            var reserved = ReservedNodes();
            for (var i = 1; i < _factory.veinCursor; i++)
            {
                ref var vein = ref _factory.veinPool[i];
                if (vein.id != i || vein.amount <= 0 || vein.colliderId <= 0) continue;
                var surface = vein.pos.normalized * (_planet.realRadius + 0.2f);
                if ((surface - _center).sqrMagnitude > radius * radius) continue;
                _nodes.Add(new Node
                {
                    Id = i, Target = vein.groupIndex == group, Type = vein.type,
                    Position = vein.pos, OriginalPosition = vein.pos, Collider = _physics.GetColliderData(vein.colliderId),
                    Visible = vein.pos.magnitude >= _planet.realRadius - 40f,
                    CanBury = vein.groupIndex == group && vein.minerCount == 0 && !reserved.Contains(i)
                });
            }
            var existing = new List<MinerPlacementDebug.ExistingMiner>();
            for (var i = 1; i < _factory.entityCursor; i++)
            {
                ref var entity = ref _factory.entityPool[i];
                if (entity.id != i || entity.protoId != ItemIds.MiningMachine || entity.minerId <= 0 ||
                    (entity.pos - _center).sqrMagnitude > radius * radius) continue;
                ref var miner = ref _factory.factorySystem.minerPool[entity.minerId];
                existing.Add(new MinerPlacementDebug.ExistingMiner
                {
                    entityId = i, minerId = entity.minerId, position = MinerPlacementDebug.Vector(entity.pos),
                    rotation = MinerPlacementDebug.Rotation(entity.rot), veins = miner.veins?.Take(miner.veinCount).ToArray() ?? []
                });
            }
            for (var i = 1; i < _factory.prebuildCursor; i++)
            {
                ref var prebuild = ref _factory.prebuildPool[i];
                if (prebuild.id != i || prebuild.protoId != ItemIds.MiningMachine ||
                    (prebuild.pos - _center).sqrMagnitude > radius * radius) continue;
                existing.Add(new MinerPlacementDebug.ExistingMiner
                {
                    entityId = i, prebuild = true, position = MinerPlacementDebug.Vector(prebuild.pos),
                    rotation = MinerPlacementDebug.Rotation(prebuild.rot), veins = prebuild.parameters?.Take(prebuild.paramCount).ToArray() ?? []
                });
            }
            _debug.existingMiners = existing.ToArray();
            return _nodes.Count > 0;
        }

        private HashSet<int> ReservedNodes()
        {
            var reserved = new HashSet<int>();
            for (var i = 1; i < _factory.prebuildCursor; i++)
            {
                ref var prebuild = ref _factory.prebuildPool[i];
                if (prebuild.id != i || prebuild.parameters == null) continue;
                var desc = LDB.models.Select(prebuild.modelIndex)?.prefabDesc;
                if (desc == null || !desc.veinMiner) continue;
                for (var j = desc.isVeinCollector ? 2048 : 0; j < prebuild.paramCount; j++) reserved.Add(prebuild.parameters[j]);
            }
            return reserved;
        }

        private void RestoreTargetVeins()
        {
            var changed = false;
            foreach (var node in _nodes)
            {
                if (!node.Target || node.Visible) continue;
                var surface = node.Position.normalized * (_planet.data.QueryModifiedHeight(node.Position) - 0.13f);
                var collider = node.Collider;
                collider.pos = surface + surface.normalized * 0.4f;
                var obstructed = false;
                foreach (var obstacle in _obstacles)
                {
                    // Building triggers are normally boxes. For unusual spherical/capsule
                    // triggers, conservatively keep the node buried inside their bounds.
                    var bound = obstacle;
                    if (bound.shape != EColliderShape.Box)
                    {
                        bound.idType = (int)EColliderShape.Box << 24;
                        bound.q = Quaternion.identity;
                        bound.ext = new Vector3(Mathf.Abs(obstacle.ext.x), Mathf.Abs(obstacle.ext.y), Mathf.Abs(obstacle.ext.z)) + Vector3.one * obstacle.radius;
                    }
                    if (MinerPlacementGeometry.Intersects(new Box(bound), collider))
                    {
                        obstructed = true;
                        break;
                    }
                }
                if (obstructed) continue;
                SetVeinPosition(node.Id, surface);
                node.Position = surface;
                node.Collider = _physics.GetColliderData(_factory.veinPool[node.Id].colliderId);
                node.Visible = true;
                changed = true;
            }
            if (changed) RefreshVeins(2);
        }

        private IEnumerator Refine(List<Candidate> seeds, float distance, float angle)
        {
            foreach (var seed in seeds)
            for (var x = -2; x <= 2; x++)
            for (var z = -2; z <= 2; z++)
            for (var yaw = -2; yaw <= 2; yaw++)
            {
                TryCandidate(seed.X + x * distance, seed.Z + z * distance, seed.Yaw + yaw * angle);
                yield return null;
            }
        }

        private IEnumerator PackingNeighbors(List<Candidate> seeds)
        {
            var body = _item.prefabDesc.buildColliders.OrderByDescending(c => c.ext.x * c.ext.y * c.ext.z).First();
            foreach (var seed in seeds)
            for (var angle = -15f; angle <= 15f; angle += 5f)
            for (var side = -1; side <= 1; side++)
            for (var clearance = 0.03f; clearance <= 0.14f; clearance += 0.05f)
            for (var stagger = -2; stagger <= 2; stagger++)
            {
                if (side == 0 && clearance > 0.03f) continue;
                // The legal interleaving gap is narrower than the coarse grid. Derive nearby
                // rows from the actual body width instead of expanding a rectangle over the feet.
                var rotation = seed.Rotation * Quaternion.AngleAxis(angle, Vector3.up);
                var bodyRotation = rotation * body.q;
                var offset = bodyRotation * new Vector3(side * (body.ext.x * 2f + clearance), 0f, stagger * 0.6f);
                var x = seed.X + Vector3.Dot(offset, _right);
                var z = seed.Z + Vector3.Dot(offset, _forward);
                var position = (_center + _right * x + _forward * z).normalized * (_planet.realRadius + 0.2f);
                var frame = Maths.SphericalRotation(position, 0f);
                var forward = rotation * Vector3.forward;
                // Preserve the neighbor's surface heading across the planet's changing local frame.
                var yaw = Mathf.Atan2(Vector3.Dot(forward, frame * Vector3.right), Vector3.Dot(forward, frame * Vector3.forward)) * Mathf.Rad2Deg;
                TryCandidate(x, z, yaw);
                yield return null;
            }
        }

        private void TryCandidate(float x, float z, float yaw)
        {
            _debug.rejections.attempted++;
            var pos = (_center + _right * x + _forward * z).normalized * (_planet.realRadius + 0.2f);
            if (!FactoryPatch.RemoveBuildRangeLimitEnabled.Value &&
                (pos - _player.position).sqrMagnitude > _player.mecha.buildArea * _player.mecha.buildArea)
            { _debug.rejections.range++; return; }
            var rotation = Maths.SphericalRotation(pos, yaw);
            var pose = new Pose(pos, rotation);
            _covered.Clear();
            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (!node.Visible || node.Type == EVeinType.Oil || !MinerComponent.IsTargetVeinInRange(node.Position, pose, _item.prefabDesc)) continue;
                if (node.Type != _type) { _debug.rejections.mixedType++; return; }
                _covered.Add(i);
            }
            if (_covered.Count < MinerPlacementSolver.MinimumCoverage) { _debug.rejections.noCoverage++; return; }
            for (var i = 0; i < _buildBoxes.Length; i++)
                _buildBoxes[i] = new Box(_item.prefabDesc.buildColliders[i].BindToObject(0, 0, EObjectType.None, pos, rotation));
            _buried.Clear();
            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (!node.Visible || !Hits(node.Collider)) continue;
                if (!node.CanBury) { _debug.rejections.veinCollision++; return; }
                _buried.Add(i);
            }
            foreach (var index in _buried) _covered.Remove(index);
            if (_covered.Count < MinerPlacementSolver.MinimumCoverage) { _debug.rejections.noCoverage++; return; }
            foreach (var obstacle in _obstacles)
                if (Hits(obstacle)) { _debug.rejections.buildingCollision++; return; }
            if (!HasGround(pos, rotation)) { _debug.rejections.ground++; return; }
            for (var i = 0; i < _nativeObstacles.Length; i++)
                _obstacleBoxes[i] = new Box(_nativeObstacles[i].BindToObject(0, 0, EObjectType.None, pos, rotation));
            _candidates.Add(new Candidate
            {
                X = x, Z = z, Yaw = yaw, Position = pos, Rotation = rotation, Radius = _footprintRadius,
                Covered = _covered.ToArray(), Buried = _buried.ToArray(),
                BuildBoxes = (Box[])_buildBoxes.Clone(), ObstacleBoxes = (Box[])_obstacleBoxes.Clone()
            });
        }

        private bool Hits(ColliderData collider)
        {
            foreach (var box in _buildBoxes)
            {
                var radius = box.Extents.magnitude + collider.ext.magnitude + collider.radius;
                if ((box.Center - collider.pos).sqrMagnitude <= radius * radius && MinerPlacementGeometry.Intersects(box, collider)) return true;
            }
            return false;
        }

        private bool HasGround(Vector3 position, Quaternion rotation)
        {
            // Match the ordinary miner's native land-point tests, including water clearance.
            var desc = _item.prefabDesc;
            foreach (var point in desc.landPoints)
            {
                var origin = position + rotation * new Vector3(point.x, 0f, point.z);
                var up = origin.normalized;
                origin += up * 3f;
                if (!Physics.Raycast(origin, -up, out var ground, 18f,
                        (1 << (int)Layer.Terrain) | (1 << (int)Layer.Platform), QueryTriggerInteraction.Collide) ||
                    ground.point.magnitude - _planet.realRadius < -0.3f - desc.landOffset) return false;
                var waterDistance = Physics.Raycast(origin, -up, out var water, 20f, 1 << (int)Layer.Water, QueryTriggerInteraction.Collide)
                    ? water.distance : 1000f;
                if (ground.distance - waterDistance > 0.27f + desc.landOffset) return false;
            }
            return true;
        }

        private void Commit(List<Candidate> layout)
        {
            if (!NodesUnchanged(layout))
            {
                _debug.outcome = "veins-changed";
                _result = I18NKeys.QuickMinerVeinsChanged.Translate();
                return;
            }
            var placed = new List<(Candidate Candidate, int Prebuild, MinerPlacementDebug.Placement Debug)>();
            var plannedBurial = layout.SelectMany(candidate => candidate.Buried).Select(index => _nodes[index].Id).ToHashSet();
            var hidden = new Dictionary<int, Vector3>();
            if (layout.Count == 0)
            {
                _debug.outcome = "no-placement";
                _result = I18NKeys.QuickMinerNoPlacement.Translate();
                return;
            }
            var coverage = 0;
            var buriedCount = 0;
            var validator = new BuildTool_Click();
            try
            {
                validator._Init(GameMain.data);
                validator.planet = _planet;
                validator.factory = _factory;
                validator.handItem = _item;
                validator.handPrefabDesc = _item.prefabDesc;
                validator.tmpPackage = new StorageComponent(1);
                var preview = new BuildPreview();
                validator.buildPreviews.Add(preview);
                for (var i = 0; i < layout.Count; i++)
                {
                    var candidate = layout[i];
                    var debug = new MinerPlacementDebug.Placement
                    {
                        candidate = _candidates.IndexOf(candidate), outcome = "pending", nativeCondition = "not-checked", nativeVeins = [], plannedVeins = [], finalVeins = []
                    };
                    _placements.Add(debug);
                    // No yield between burial, native validation, item consumption, and creation.
                    foreach (var index in candidate.Buried)
                    {
                        var node = _nodes[index];
                        ref var vein = ref _factory.veinPool[node.Id];
                        if (vein.id != node.Id || hidden.ContainsKey(node.Id)) continue;
                        hidden.Add(node.Id, vein.pos);
                        SetVeinPosition(node.Id, vein.pos.normalized * (_planet.realRadius - 50f));
                    }
                    RefreshVeins(0);
                    if (!Validate(validator, preview, candidate, plannedBurial, debug)) continue;
                    var itemId = ItemIds.MiningMachine;
                    var count = 1;
                    if (_player.inhandItemId == itemId && _player.inhandItemCount > 0) _player.UseHandItems(1, out _);
                    else _player.package.TakeTailItems(ref itemId, ref count, out _);
                    // Match native blueprint placement: missing items leave unfunded prebuilds.
                    var prebuild = new PrebuildData
                    {
                        protoId = (short)_item.ID, modelIndex = (short)_item.prefabDesc.modelIndex,
                        pos = candidate.Position, pos2 = candidate.Position, rot = candidate.Rotation, rot2 = candidate.Rotation,
                        itemRequired = count == 1 ? 0 : 1,
                        filterId = _factory.veinPool[_nodes[candidate.Covered[0]].Id].productId
                    };
                    prebuild.InitParametersArray(debug.plannedVeins.Length);
                    Array.Copy(debug.plannedVeins, prebuild.parameters, debug.plannedVeins.Length);
                    var id = _factory.AddPrebuildDataWithComponents(prebuild);
                    debug.prebuildId = id;
                    debug.itemRequired = prebuild.itemRequired;
                    debug.outcome = id > 0 ? "placed" : "prebuild-failed";
                    if (id == 0 && count == 1) _player.TryAddItemToPackage(ItemIds.MiningMachine, 1, 0, true);
                    if (id > 0) placed.Add((candidate, id, debug));
                }
            }
            finally
            {
                validator._Free();
                // A failed/shortened layout must not leave nodes buried for unplaced miners.
                var needed = new HashSet<int>();
                foreach (var entry in placed)
                foreach (var index in entry.Candidate.Buried) needed.Add(_nodes[index].Id);
                foreach (var entry in hidden)
                    if (!needed.Contains(entry.Key)) SetVeinPosition(entry.Key, entry.Value);
                foreach (var entry in placed)
                {
                    var ids = new List<int>();
                    var pose = new Pose(entry.Candidate.Position, entry.Candidate.Rotation);
                    foreach (var node in _nodes)
                    {
                        ref var vein = ref _factory.veinPool[node.Id];
                        if (vein.id == node.Id && vein.amount > 0 && vein.type == _type && vein.pos.magnitude >= _planet.realRadius - 40f &&
                            MinerComponent.IsTargetVeinInRange(vein.pos, pose, _item.prefabDesc)) ids.Add(node.Id);
                    }
                    ref var prebuild = ref _factory.prebuildPool[entry.Prebuild];
                    prebuild.InitParametersArray(ids.Count);
                    if (ids.Count > 0) ids.CopyTo(prebuild.parameters);
                    entry.Debug.finalVeins = ids.ToArray();
                    coverage += ids.Count;
                }
                buriedCount = needed.Count;
                RefreshVeins(buriedCount > 0 ? 1 : 0);
            }
            _debug.placedCount = placed.Count;
            _debug.totalCoverage = coverage;
            _debug.buriedCount = buriedCount;
            if (placed.Count == 0) _debug.outcome = "no-placement";
            _result = placed.Count == 0 ? I18NKeys.QuickMinerNoPlacement.Translate() :
                string.Format(I18NKeys.QuickMinerResult.Translate(), placed.Count, coverage, buriedCount);
        }

        private bool NodesUnchanged(List<Candidate> layout)
        {
            foreach (var node in _nodes)
            {
                if (node.Id >= _factory.veinPool.Length) return false;
                ref var vein = ref _factory.veinPool[node.Id];
                if (vein.id != node.Id || vein.type != node.Type || vein.amount <= 0 || vein.colliderId <= 0 ||
                    (vein.pos - node.Position).sqrMagnitude > 0.0001f) return false;
            }
            var reserved = ReservedNodes();
            foreach (var candidate in layout)
            foreach (var index in candidate.Buried)
            {
                var id = _nodes[index].Id;
                if (_factory.veinPool[id].minerCount > 0 || reserved.Contains(id)) return false;
            }
            return true;
        }

        private bool Validate(BuildTool_Click validator, BuildPreview preview, Candidate candidate, HashSet<int> plannedBurial, MinerPlacementDebug.Placement debug)
        {
            debug.outcome = "native-rejected";
            if (!HasGround(candidate.Position, candidate.Rotation)) { debug.outcome = "ground-changed"; return false; }
            preview.ResetAll();
            preview.item = _item;
            preview.desc = _item.prefabDesc;
            preview.lpos = preview.lpos2 = candidate.Position;
            preview.lrot = preview.lrot2 = candidate.Rotation;
            preview.condition = EBuildCondition.Ok;
            validator.tmpInhandId = _item.ID;
            validator.tmpInhandCount = 1;
            var action = _player.controller.actionBuild;
            var oldTest = _player.controller.cmd.test;
            var oldCursorState = action.model.cursorState;
            var oldCursorText = action.model.cursorText;
            var oldCursor = UICursor.cursorIndex;
            try
            {
                // CommandState is a struct; update the controller's field, not a local copy.
                _player.controller.cmd.test = candidate.Position;
                _physics.nearColliderLogic.ActiveCollidersInArea(candidate.Position, _footprintRadius + 16f);
                // UpdateCursorNear does nothing outside build mode. Activate the native vein
                // colliders explicitly so CheckBuildConditions still sees them from this button.
                foreach (var node in _nodes)
                {
                    var id = _factory.veinPool[node.Id].colliderId;
                    while (id > 0)
                    {
                        var collider = _physics.GetColliderData(id);
                        var objects = _physics.nearColliderLogic.colliderObjs;
                        if (objects.TryGetValue(id, out var existing)) existing.live = true;
                        else objects[id] = new ColliderObject(id, collider);
                        id = collider.link;
                    }
                }
                Physics.SyncTransforms();
                var valid = validator.CheckBuildConditions();
                debug.nativeCondition = preview.condition.ToString();
                debug.coverObjectId = preview.coverObjId;
                debug.nativeVeins = preview.parameters?.Take(preview.paramCount).ToArray() ?? [];
                if (!valid || preview.condition != EBuildCondition.Ok || preview.coverObjId != 0) return false;
                // Include every planned burial now, so a later miner cannot reduce this one
                // below the minimum after its item has already been consumed.
                debug.plannedVeins = debug.nativeVeins.Where(id => !plannedBurial.Contains(id)).ToArray();
                if (debug.plannedVeins.Length < MinerPlacementSolver.MinimumCoverage)
                { debug.outcome = "below-minimum-coverage"; return false; }
                for (var i = 0; i < preview.paramCount; i++)
                {
                    ref var vein = ref _factory.veinPool[preview.parameters[i]];
                    if (vein.id != preview.parameters[i] || vein.amount <= 0 || vein.type != _type) return false;
                }
                return true;
            }
            finally
            {
                _player.controller.cmd.test = oldTest;
                action.model.cursorState = oldCursorState;
                action.model.cursorText = oldCursorText;
                UICursor.cursorIndex = oldCursor;
            }
        }

        private void SetVeinPosition(int id, Vector3 position)
        {
            ref var vein = ref _factory.veinPool[id];
            vein.pos = position;
            ref var collider = ref _physics.GetColliderDataRef(vein.colliderId);
            collider.pos = position + position.normalized * 0.4f;
            _physics.SetPlanetPhysicsColliderDirty();
            GameMain.gpuiManager.AlterModel(vein.modelIndex, vein.modelId, id, position, setBuffer: false);
            var proto = LDB.veins.Select((int)vein.type);
            if (proto == null) return;
            var up = position.normalized;
            if (vein.minerId0 > 0)
            {
                GameMain.gpuiManager.AlterModel(proto.MinerBaseModelIndex, vein.minerBaseModelId, vein.minerId0, position + up * 0.1f, setBuffer: false);
                GameMain.gpuiManager.AlterModel(proto.MinerCircleModelIndex, vein.minerCircleModelId0, vein.minerId0, position + up * 0.4f, setBuffer: false);
            }
            if (vein.minerId1 > 0) GameMain.gpuiManager.AlterModel(proto.MinerCircleModelIndex, vein.minerCircleModelId1, vein.minerId1, position + up * 0.6f, setBuffer: false);
            if (vein.minerId2 > 0) GameMain.gpuiManager.AlterModel(proto.MinerCircleModelIndex, vein.minerCircleModelId2, vein.minerId2, position + up * 0.8f, setBuffer: false);
            if (vein.minerId3 > 0) GameMain.gpuiManager.AlterModel(proto.MinerCircleModelIndex, vein.minerCircleModelId3, vein.minerId3, position + up, setBuffer: false);
        }

        private void RefreshVeins(int operation)
        {
            _physics.RefreshColliders();
            GameMain.gpuiManager.SyncAllGPUBuffer();
            if (operation != 0) GameMain.gameScenario?.NotifyOnVeinReformed(operation);
        }
    }
}
