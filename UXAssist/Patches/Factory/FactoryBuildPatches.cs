using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class FactoryBuildPatches
{
    internal class AutoConstructPatch : PatchImpl<AutoConstructPatch>
    {
        private const long AutoConstructTickInterval = 15L;
        private const long PlannerTickInterval = 120L;
        private const int MaxPlannerCandidates = 32;
        private const int MaxImmediateBuildTargets = 120;
        private const int MaxOverflowBuildTargets = 600;
        private const int MaxPlannerEntriesPerCandidate = 4096;
        private const int ItemAvailabilityCacheSize = 64;
        private const float PlanArrivalDistance = 5f;
        private const float CandidateMergeDistance = 6f;
        private const float CoverageValuePerTarget = 100f;
        private const float OverflowValuePerTarget = 12f;
        private const float DroneDistanceCost = 0.3f;
        private const float PlayerTravelCost = 1f;
        private const float AvoidanceAltitude = 35f;
        private const float AvoidanceClearance = 1.5f;
        private const float StuckDistance = 1.5f;
        private const int StuckTickThreshold = 45;
        private static readonly float[] DetourSideOffsets = { 1f, -1f, 1f, -1f, 1f, -1f };

        private static OrderNode _autoOrder;
        private static Vector3 _lastPosition;
        private static int _stuckTicks;
        private static int _detourCount;
        private static bool _isAvoidingObstacle;
        private static Vector3 _pendingWaypoint;
        private static bool _hasPendingWaypoint;
        private static Vector3 _constructionDestination;
        private static bool _hasConstructionPlan;
        private static bool _atConstructionDestination;
        private static int _planAstroId;
        private static long _lastPlanAttemptTick;
        private static bool _hasPlanAttempted;
        private static readonly Vector3[] PlannerCandidates = new Vector3[MaxPlannerCandidates];
        private static readonly float[] PlannerCandidateDistanceSquared = new float[MaxPlannerCandidates];
        private static int _plannerCandidateCount;
        private static int _plannerNearestCandidateCount;
        private static int _plannerSampleCount;
        private static readonly int[] ItemAvailabilityProtoIds = new int[ItemAvailabilityCacheSize];
        private static readonly int[] ItemAvailabilityCounts = new int[ItemAvailabilityCacheSize];
        private static int _itemAvailabilityCount;
        private static uint _plannerRandomState = 0x6D2B79F5u;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameEnd += ResetNavigation;
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            // Diagnostic aid for reports of the auto-construct button not showing up:
            // log the visibility predicate inputs once per enable.
            var planet = GameMain.localPlanet;
            var factoryLoaded = planet != null && planet.factoryLoaded;
            UXAssist.Logger.LogInfo(
                $"AutoConstruct button enabled: buttonCreated={Functions.UI.AutoConstructUI.ToggleAutoConstruct != null}, " +
                $"localPlanet={planet != null}, factoryLoaded={factoryLoaded}, " +
                $"prebuildCount={(factoryLoaded ? planet.factory.prebuildCount : 0)}");
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameEnd -= ResetNavigation;
            ResetNavigation();
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        }

        // Button visibility and the pending-construction count text are reconciled periodically
        // in AutoConstructUI.OnUpdate() (independent of Harmony patch state), so this postfix
        // only implements the auto-construct fly-to-target behavior. This also keeps the patch
        // surface small: PlanetData.NotifyFactoryLoaded/UnloadFactory no longer need postfixes.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        private static void PlayerAction_Rts_GameTick_Postfix(PlayerAction_Rts __instance, long timei)
        {
            if (timei % AutoConstructTickInterval != 0) return;
            if (!FactoryPatch.AutoConstructEnabled.Value) return;
            var planet = GameMain.localPlanet;
            if (planet == null || !planet.factoryLoaded) return;
            var factory = planet.factory;
            var player = __instance.player;
            if (player.planetData != planet)
            {
                ResetNavigation();
                return;
            }

            var currentOrder = player.orders.currentOrder;
            if (HasManualInput(player) ||
                currentOrder != null && currentOrder != _autoOrder ||
                currentOrder == null && player.orders.orderCount > 0)
            {
                ClearAutoRoute(player);
                ResetNavigation();
                return;
            }

            var prebuildCount = factory.prebuildCount;
            if (prebuildCount <= 0)
            {
                ClearAutoRoute(player);
                ResetNavigation();
                return;
            }

            if (_hasConstructionPlan && _planAstroId != planet.astroId)
            {
                ClearAutoRoute(player);
                ResetNavigation();
            }

            if (!_hasConstructionPlan)
            {
                if (!IsPlannerDue(timei) || !TryCreatePlan(factory, player, timei)) return;
            }
            else if (_atConstructionDestination && IsPlannerDue(timei))
            {
                if (HasLocalConstructionWork(factory, player)) return;
                ClearAutoRoute(player);
                ResetNavigation();
                if (!TryCreatePlan(factory, player, timei)) return;
            }

            if (!_hasConstructionPlan) return;

            if (_autoOrder != null && player.orders.currentOrder == null)
            {
                _autoOrder = null;
                _stuckTicks = 0;
                _lastPosition = player.position;
                if (_hasPendingWaypoint)
                {
                    var pendingWaypoint = _pendingWaypoint;
                    _pendingWaypoint = Vector3.zero;
                    _hasPendingWaypoint = false;
                    IssueRoute(player, pendingWaypoint, true);
                    return;
                }
            }

            if (!_hasPendingWaypoint &&
                (player.position - _constructionDestination).sqrMagnitude <= PlanArrivalDistance * PlanArrivalDistance)
            {
                ClearAutoRoute(player);
                _atConstructionDestination = true;
                _stuckTicks = 0;
                return;
            }

            _atConstructionDestination = false;
            if (player.movementState == EMovementState.Walk && player.mecha.thrusterLevel >= 1)
            {
                player.controller.actionWalk.SwitchToFly();
            }

            if (_autoOrder == null)
            {
                IssueRoute(player, _constructionDestination, false);
                return;
            }

            if ((player.position - _lastPosition).sqrMagnitude < StuckDistance * StuckDistance)
            {
                _stuckTicks += (int)AutoConstructTickInterval;
            }
            else
            {
                _stuckTicks = 0;
                _lastPosition = player.position;
            }

            var routeTarget = _autoOrder.target;
            var altitude = _isAvoidingObstacle ? AvoidanceAltitude : GetCurrentAltitude(player);
            if (_stuckTicks >= StuckTickThreshold || !IsPathClear(player, routeTarget.normalized, altitude))
            {
                if (_detourCount < 4 && TryFindDetour(player, routeTarget, altitude, out var detour, out var pendingWaypoint))
                {
                    _detourCount++;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = pendingWaypoint;
                    _hasPendingWaypoint = pendingWaypoint.sqrMagnitude > 0.01f;
                    player.controller.actionFly.targetAltitude = AvoidanceAltitude;
                    IssueRoute(player, detour, true);
                }
                else
                {
                    _detourCount = 0;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = Vector3.zero;
                    _hasPendingWaypoint = false;
                    player.controller.actionFly.targetAltitude = AvoidanceAltitude;
                    IssueRoute(player, _constructionDestination, true);
                }

                _stuckTicks = 0;
                _lastPosition = player.position;
            }
        }

        private static bool IsPlannerDue(long timei)
        {
            return !_hasPlanAttempted || timei < _lastPlanAttemptTick || timei - _lastPlanAttemptTick >= PlannerTickInterval;
        }

        private static bool TryCreatePlan(PlanetFactory factory, Player player, long timei)
        {
            _lastPlanAttemptTick = timei;
            _hasPlanAttempted = true;
            _hasConstructionPlan = false;
            _atConstructionDestination = false;
            _plannerCandidateCount = 0;
            _plannerNearestCandidateCount = 0;
            _plannerSampleCount = 0;
            _itemAvailabilityCount = 0;

            var buildArea = Mathf.Max(0f, player.mecha.buildArea);
            if (buildArea < PlanArrivalDistance) return false;

            CollectPlannerCandidates(factory, player, buildArea * buildArea);
            if (_plannerCandidateCount == 0) return false;

            var bestScore = float.MinValue;
            var bestCoverageCount = 0;
            var bestCandidate = Vector3.zero;
            var buildAreaSquared = buildArea * buildArea;
            for (var i = 0; i < _plannerCandidateCount; i++)
            {
                var score = EvaluatePlannerCandidate(factory, player, PlannerCandidates[i], buildAreaSquared, out var coverageCount);
                if (coverageCount > 0 && score > bestScore)
                {
                    bestScore = score;
                    bestCoverageCount = coverageCount;
                    bestCandidate = PlannerCandidates[i];
                }
            }

            if (bestCoverageCount == 0) return false;

            _constructionDestination = bestCandidate;
            _hasConstructionPlan = true;
            _planAstroId = factory.planet.astroId;
            _autoOrder = null;
            _atConstructionDestination = false;
            _stuckTicks = 0;
            _detourCount = 0;
            _isAvoidingObstacle = false;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
            _lastPosition = player.position;
            return true;
        }

        private static void CollectPlannerCandidates(PlanetFactory factory, Player player, float buildAreaSquared)
        {
            var prebuilds = factory.prebuildPool;
            for (var i = 1; i < factory.prebuildCursor; i++)
            {
                ref var prebuild = ref prebuilds[i];
                if (prebuild.id != i || prebuild.isDestroyed || prebuild.builderLaunched) continue;
                if (!HasRequiredItems(player, prebuild.protoId, prebuild.itemRequired)) continue;

                var distanceSquared = (prebuild.pos - player.position).sqrMagnitude;
                if (distanceSquared <= buildAreaSquared) continue;
                AddPlannerCandidate(prebuild.pos, distanceSquared);
            }
        }

        private static void AddPlannerCandidate(Vector3 position, float distanceSquared)
        {
            var mergeDistanceSquared = CandidateMergeDistance * CandidateMergeDistance;
            for (var i = 0; i < _plannerCandidateCount; i++)
            {
                if ((PlannerCandidates[i] - position).sqrMagnitude <= mergeDistanceSquared) return;
            }

            if (_plannerNearestCandidateCount < MaxPlannerCandidates / 2)
            {
                var index = _plannerNearestCandidateCount++;
                PlannerCandidates[index] = position;
                PlannerCandidateDistanceSquared[index] = distanceSquared;
                _plannerCandidateCount = Mathf.Max(_plannerCandidateCount, _plannerNearestCandidateCount);
                return;
            }

            var farthestIndex = 0;
            var farthestDistanceSquared = PlannerCandidateDistanceSquared[0];
            for (var i = 1; i < _plannerNearestCandidateCount; i++)
            {
                if (PlannerCandidateDistanceSquared[i] > farthestDistanceSquared)
                {
                    farthestIndex = i;
                    farthestDistanceSquared = PlannerCandidateDistanceSquared[i];
                }
            }

            if (distanceSquared < farthestDistanceSquared)
            {
                PlannerCandidates[farthestIndex] = position;
                PlannerCandidateDistanceSquared[farthestIndex] = distanceSquared;
                return;
            }

            _plannerSampleCount++;
            if (_plannerCandidateCount < MaxPlannerCandidates)
            {
                var index = _plannerCandidateCount++;
                PlannerCandidates[index] = position;
                PlannerCandidateDistanceSquared[index] = distanceSquared;
                return;
            }

            var replacement = (int)(NextPlannerRandom() % (uint)_plannerSampleCount);
            if (replacement < MaxPlannerCandidates / 2)
            {
                var index = MaxPlannerCandidates / 2 + replacement;
                PlannerCandidates[index] = position;
                PlannerCandidateDistanceSquared[index] = distanceSquared;
            }
        }

        private static float EvaluatePlannerCandidate(PlanetFactory factory, Player player, Vector3 candidate, float buildAreaSquared, out int coverageCount)
        {
            coverageCount = 0;
            var distanceSquaredSum = 0f;
            var inspectedCount = 0;
            var reachedScanLimit = false;
            var prebuilds = factory.prebuildPool;
            var hashSystem = factory.hashSystemStatic;
            var hashPool = hashSystem.hashPool;
            var bucketOffsets = hashSystem.bucketOffsets;
            hashSystem.GetBucketIdxesInArea(candidate, Mathf.Sqrt(buildAreaSquared));
            var activeBucketsCount = hashSystem.activeBucketsCount;
            for (var bucketIndex = 0; bucketIndex < activeBucketsCount && !reachedScanLimit; bucketIndex++)
            {
                var bucket = hashSystem.activeBuckets[bucketIndex];
                var bucketOffset = bucketOffsets[bucket];
                var bucketCursor = hashSystem.bucketCursors[bucket];
                for (var entryIndex = 0; entryIndex < bucketCursor; entryIndex++)
                {
                    if (++inspectedCount > MaxPlannerEntriesPerCandidate)
                    {
                        reachedScanLimit = true;
                        break;
                    }

                    var hashEntry = hashPool[bucketOffset + entryIndex];
                    if (hashEntry == 0 || hashEntry >> 28 != 3) continue;
                    var prebuildId = hashEntry & 0xFFFFFFF;
                    if (prebuildId <= 0 || prebuildId >= prebuilds.Length) continue;

                    ref var prebuild = ref prebuilds[prebuildId];
                    if (prebuild.id != prebuildId || prebuild.isDestroyed || prebuild.builderLaunched) continue;
                    if (!HasRequiredItems(player, prebuild.protoId, prebuild.itemRequired)) continue;

                    var distanceSquared = (prebuild.pos - candidate).sqrMagnitude;
                    if (distanceSquared > buildAreaSquared) continue;

                    coverageCount++;
                    if (coverageCount <= MaxImmediateBuildTargets)
                    {
                        distanceSquaredSum += distanceSquared;
                    }

                    if (coverageCount >= MaxOverflowBuildTargets) break;
                }

                if (coverageCount >= MaxOverflowBuildTargets) break;
            }

            hashSystem.ClearActiveBuckets();
            if (coverageCount == 0) return float.MinValue;

            var immediateCount = Mathf.Min(coverageCount, MaxImmediateBuildTargets);
            var overflowCount = Mathf.Min(Mathf.Max(0, coverageCount - MaxImmediateBuildTargets),
                MaxOverflowBuildTargets - MaxImmediateBuildTargets);
            var averageDistanceSquared = distanceSquaredSum / immediateCount;
            var travelDistance = (candidate - player.position).magnitude;
            return immediateCount * CoverageValuePerTarget +
                   overflowCount * OverflowValuePerTarget -
                   averageDistanceSquared * DroneDistanceCost -
                   travelDistance * PlayerTravelCost;
        }

        private static bool HasRequiredItems(Player player, int protoId, int itemRequired)
        {
            if (itemRequired <= 0) return true;

            for (var i = 0; i < _itemAvailabilityCount; i++)
            {
                if (ItemAvailabilityProtoIds[i] == protoId)
                {
                    return ItemAvailabilityCounts[i] >= itemRequired;
                }
            }

            var itemCount = player.package.GetItemCount(protoId);
            if (_itemAvailabilityCount < ItemAvailabilityCacheSize)
            {
                ItemAvailabilityProtoIds[_itemAvailabilityCount] = protoId;
                ItemAvailabilityCounts[_itemAvailabilityCount] = itemCount;
                _itemAvailabilityCount++;
            }

            return itemCount >= itemRequired;
        }

        private static uint NextPlannerRandom()
        {
            _plannerRandomState ^= _plannerRandomState << 13;
            _plannerRandomState ^= _plannerRandomState >> 17;
            _plannerRandomState ^= _plannerRandomState << 5;
            return _plannerRandomState;
        }

        private static bool HasLocalConstructionWork(Player player)
        {
            var constructionModule = player.mecha.constructionModule;
            return constructionModule.buildTargetTotalCount > 0 ||
                   constructionModule.droneIdleCount < constructionModule.droneCount;
        }

        private static bool HasLocalConstructionWork(PlanetFactory factory, Player player)
        {
            if (HasLocalConstructionWork(player)) return true;

            var buildArea = Mathf.Max(0f, player.mecha.buildArea);
            if (buildArea <= 0f) return false;

            _itemAvailabilityCount = 0;
            var prebuilds = factory.prebuildPool;
            var hashSystem = factory.hashSystemStatic;
            var hashPool = hashSystem.hashPool;
            var bucketOffsets = hashSystem.bucketOffsets;
            hashSystem.GetBucketIdxesInArea(player.position, buildArea);
            var hasLocalTarget = false;
            for (var bucketIndex = 0; bucketIndex < hashSystem.activeBucketsCount && !hasLocalTarget; bucketIndex++)
            {
                var bucket = hashSystem.activeBuckets[bucketIndex];
                var bucketOffset = bucketOffsets[bucket];
                var bucketCursor = hashSystem.bucketCursors[bucket];
                for (var entryIndex = 0; entryIndex < bucketCursor; entryIndex++)
                {
                    var hashEntry = hashPool[bucketOffset + entryIndex];
                    if (hashEntry == 0 || hashEntry >> 28 != 3) continue;
                    var prebuildId = hashEntry & 0xFFFFFFF;
                    if (prebuildId <= 0 || prebuildId >= prebuilds.Length) continue;

                    ref var prebuild = ref prebuilds[prebuildId];
                    if (prebuild.id != prebuildId || prebuild.isDestroyed || prebuild.builderLaunched) continue;
                    if (HasRequiredItems(player, prebuild.protoId, prebuild.itemRequired))
                    {
                        hasLocalTarget = true;
                        break;
                    }
                }
            }

            hashSystem.ClearActiveBuckets();
            return hasLocalTarget;
        }

        private static bool HasManualInput(Player player)
        {
            var input0 = player.controller.input0;
            var input1 = player.controller.input1;
            return Mathf.Abs(input0.x) > 0.01f || Mathf.Abs(input0.y) > 0.01f || input0.z > 0f ||
                   Mathf.Abs(input1.y) > 0.01f;
        }

        private static void ClearAutoRoute(Player player)
        {
            if (_autoOrder != null && player.orders.currentOrder == _autoOrder)
            {
                player.ClearOrders();
            }

            _autoOrder = null;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
        }

        private static void IssueRoute(Player player, Vector3 targetPosition, bool keepAltitude)
        {
            var direction = targetPosition - player.position;
            if (direction.sqrMagnitude < 0.01f) return;

            _autoOrder = OrderNode.MoveTo(targetPosition);
            player.Order(_autoOrder, false);
            if (keepAltitude && player.movementState == EMovementState.Fly)
            {
                player.controller.actionFly.targetAltitude = AvoidanceAltitude;
            }
        }

        private static bool TryFindDetour(Player player, Vector3 routeTarget, float altitude, out Vector3 detour, out Vector3 pendingWaypoint)
        {
            detour = default;
            pendingWaypoint = default;
            var planet = player.planetData;
            if (planet == null) return false;

            var origin = GetFlightPoint(player.position.normalized, altitude, planet.realRadius);
            var finalDirection = routeTarget.normalized;
            var path = GetFlightPoint(finalDirection, altitude, planet.realRadius) - origin;
            if (path.sqrMagnitude < 0.01f) return false;
            if (!Physics.SphereCast(origin, AvoidanceClearance, path.normalized, out var hit, path.magnitude, 8704, QueryTriggerInteraction.Collide) ||
                hit.distance + 4f >= path.magnitude)
            {
                return false;
            }

            var obstacleBounds = hit.collider != null ? hit.collider.bounds : new Bounds(hit.point, Vector3.one * 2f);
            var anchor = obstacleBounds.center.normalized;
            var forward = Vector3.ProjectOnPlane(finalDirection, anchor).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.ProjectOnPlane(player.position, anchor).normalized;
            }

            var side = Vector3.Cross(anchor, forward).normalized;
            if (side.sqrMagnitude < 0.01f) return false;

            var sideExtent = Mathf.Min(GetProjectedExtent(obstacleBounds.extents, side), 48f);
            var forwardExtent = Mathf.Min(GetProjectedExtent(obstacleBounds.extents, forward), 48f);
            var sideOffset = Mathf.Max(6f, sideExtent + AvoidanceClearance + 2f);
            var forwardOffset = Mathf.Max(5f, forwardExtent + AvoidanceClearance + 2f);
            for (var offsetIndex = 0; offsetIndex < DetourSideOffsets.Length; offsetIndex++)
            {
                var lateralOffset = sideOffset * DetourSideOffsets[offsetIndex];
                if (offsetIndex >= 2)
                {
                    lateralOffset += 8f * (offsetIndex / 2) * DetourSideOffsets[offsetIndex];
                }
                var entry = SurfaceOffset(anchor, side * lateralOffset - forward * forwardOffset, planet.realRadius);
                var exit = SurfaceOffset(anchor, side * lateralOffset + forward * forwardOffset, planet.realRadius);
                if (IsPathClear(player, entry, altitude) &&
                    IsPathClear(entry, exit, altitude, planet.realRadius) &&
                    IsPathClear(exit, finalDirection, altitude, planet.realRadius))
                {
                    detour = entry * planet.realRadius;
                    pendingWaypoint = exit * planet.realRadius;
                    return true;
                }

                var candidate = SurfaceOffset(anchor, side * lateralOffset, planet.realRadius);
                if (!IsPathClear(player, candidate, altitude) ||
                    !IsPathClear(candidate, finalDirection, altitude, planet.realRadius)) continue;

                detour = candidate * planet.realRadius;
                return true;
            }

            return false;
        }

        private static float GetProjectedExtent(Vector3 extents, Vector3 axis)
        {
            return Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;
        }

        private static bool IsPathClear(Player player, Vector3 targetDirection, float altitude)
        {
            var planet = player.planetData;
            return planet != null && IsPathClear(player.position.normalized, targetDirection, altitude, planet.realRadius);
        }

        private static bool IsPathClear(Vector3 startDirection, Vector3 targetDirection, float altitude, float realRadius)
        {
            var origin = GetFlightPoint(startDirection, altitude, realRadius);
            var target = GetFlightPoint(targetDirection, altitude, realRadius);
            var path = target - origin;
            if (path.sqrMagnitude < 0.01f) return true;

            return !Physics.SphereCast(origin, AvoidanceClearance, path.normalized, out var hit, path.magnitude, 8704, QueryTriggerInteraction.Collide) ||
                   hit.distance + 4f >= path.magnitude;
        }

        private static Vector3 SurfaceOffset(Vector3 surfaceDirection, Vector3 tangentOffset, float realRadius)
        {
            var distance = tangentOffset.magnitude;
            if (distance < 0.01f) return surfaceDirection.normalized;

            var tangent = Vector3.ProjectOnPlane(tangentOffset, surfaceDirection).normalized;
            var angle = distance / Mathf.Max(1f, realRadius);
            return (surfaceDirection.normalized * Mathf.Cos(angle) + tangent * Mathf.Sin(angle)).normalized;
        }

        private static Vector3 GetFlightPoint(Vector3 direction, float altitude, float realRadius)
        {
            return direction.normalized * (realRadius + Mathf.Max(2f, altitude));
        }

        private static float GetCurrentAltitude(Player player)
        {
            return Mathf.Clamp(player.position.magnitude - player.planetData.realRadius, 15f, AvoidanceAltitude);
        }

        private static void ResetNavigation()
        {
            _autoOrder = null;
            _lastPosition = Vector3.zero;
            _stuckTicks = 0;
            _detourCount = 0;
            _isAvoidingObstacle = false;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
            _constructionDestination = Vector3.zero;
            _hasConstructionPlan = false;
            _atConstructionDestination = false;
            _planAstroId = 0;
            _lastPlanAttemptTick = 0;
            _hasPlanAttempted = false;
            _plannerCandidateCount = 0;
            _plannerNearestCandidateCount = 0;
            _plannerSampleCount = 0;
            _itemAvailabilityCount = 0;
        }
    }

    internal class QuickBuildAndDismantleLab : PatchImpl<QuickBuildAndDismantleLab>
    {
        private static bool DetermineMoreLabsForDismantle(BuildTool dismantle, int id)
        {
            if (!VFInput._chainReaction) return true;
            var factory = dismantle.factory;
            var proto = dismantle.GetItemProto(id);
            var protoId = proto.ID;
            var prefDesc = proto.prefabDesc;
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return true;
            factory.ReadObjectConn(id, 14, out _, out var nextId, out _);
            /* We keep last lab if selected lab is not the ground one */
            if (nextId > 0)
            {
                while (true)
                {
                    factory.ReadObjectConn(nextId, 14, out _, out var nextNextId, out _);
                    if (nextNextId <= 0) break;
                    var itemProto = dismantle.GetItemProto(nextId);
                    if (itemProto.ID != protoId) break;
                    var desc = itemProto.prefabDesc;
                    var pose = dismantle.GetObjectPose(nextId);
                    var preview = new BuildPreview
                    {
                        item = itemProto,
                        desc = desc,
                        lpos = pose.position,
                        lrot = pose.rotation,
                        lpos2 = pose.position,
                        lrot2 = pose.rotation,
                        objId = nextId,
                        needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                        isConnNode = true
                    };
                    dismantle.buildPreviews.Add(preview);
                    nextId = nextNextId;
                }
            }

            nextId = id;
            while (true)
            {
                factory.ReadObjectConn(nextId, 15, out _, out var nextId2, out _);
                if (nextId2 <= 0)
                {
                    factory.ReadObjectConn(nextId, 13, out _, out nextId2, out _);
                    if (nextId2 <= 0) break;
                }

                nextId = nextId2;
                var itemProto = dismantle.GetItemProto(nextId);
                var desc = itemProto.prefabDesc;
                var pose = dismantle.GetObjectPose(nextId);
                var preview = new BuildPreview
                {
                    item = itemProto,
                    desc = desc,
                    lpos = pose.position,
                    lrot = pose.rotation,
                    lpos2 = pose.position,
                    lrot2 = pose.rotation,
                    objId = nextId,
                    needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                    isConnNode = true
                };
                dismantle.buildPreviews.Add(preview);
            }

            return false;
        }

        private static void BuildLabsToTop(BuildTool_Click click)
        {
            if (!click.multiLevelCovering || !VFInput._chainReaction) return;
            var prefDesc = click.GetPrefabDesc(click.castObjectId);
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return;
            var levelMax = prefDesc.isLab ? GameMain.history.labLevel : GameMain.history.storageLevel;
            var factory = click.factory;
            var currLevel = 2;
            var nid = click.castObjectId;
            do
            {
                factory.ReadObjectConn(nid, 14, out _, out nid, out _);
                if (nid <= 0) break;
                currLevel++;
            } while (true);

            while (currLevel < levelMax)
            {
                click.UpdateRaycast();
                click.DeterminePreviews();
                click.UpdateCollidersForCursor();
                click.UpdateCollidersForGiantBp();
                var model = click.actionBuild.model;
                click.UpdatePreviewModels(model);
                if (!click.CheckBuildConditions())
                {
                    model.ClearAllPreviewsModels();
                    model.EarlyGameTickIgnoreActive();
                    return;
                }

                click.UpdatePreviewModelConditions(model);
                click.UpdateGizmos(model);
                click.CreatePrebuilds();
                currLevel++;
            }
        }
        // Harmony transpiler: BuildTool_Dismantle_DeterminePreviews_Transpiler
        // Target: BuildTool_Dismantle.DeterminePreviews
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isBattleBase))),
                new CodeMatch(OpCodes.Brfalse)
            ).Advance(-1);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.objId))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(DetermineMoreLabsForDismantle))),
                new CodeInstruction(OpCodes.And)
            );
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: BuildTool_Click__OnTick_Transpiler
        // Target: BuildTool_Click._OnTick
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnTick))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds)))
            ).Advance(2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(BuildLabsToTop)))
            );
            return matcher.InstructionEnumeration();
        }
    }
}
