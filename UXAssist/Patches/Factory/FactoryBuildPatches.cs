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
        private const int PlannerFanSectorCount = MaxPlannerCandidates / 2;
        private const int MaxPlannerRefinementCandidates = 8;
        private const int MaxImmediateBuildTargets = 120;
        private const int MaxOverflowBuildTargets = 600;
        private const int MaxPlannerEntriesPerCandidate = 4096;
        private const int ItemAvailabilityCacheSize = 64;
        private const int FinalConstructionTargetLimit = 8;
        private const float PlanArrivalDistance = 5f;
        private const float MinimumSiteMoveDistance = 8f;
        private const float ConstructionFlightAltitude = 15f;
        private const float SiteSwitchGain = 1.1f;
        private const float DroneBuildSecondsPerTarget = 2f;
        private const int MinimumConstructionSiteHoldTicks = 45;
        private const int FlightSettleDurationTicks = 45;
        private const float FlightSettleRtsSpeed = 4f;
        private const float FlightSettleTangentialSpeed = 6f;
        private const float CandidateMergeDistance = 6f;
        private const float AvoidanceAltitude = 35f;
        private const float AvoidanceClearance = 1.5f;
        private const float StuckDistance = 1.5f;
        private const int StuckTickThreshold = 45;
        private const float OrbitRadialMovementThreshold = 0.75f;
        private const float OrbitAngleProgressThreshold = 0.5f;
        private const int OrbitTickThreshold = 45;
        private static readonly float[] DetourSideOffsets = { 1f, -1f, 1f, -1f, 1f, -1f };

        private static OrderNode _autoOrder;
        private static Vector3 _lastPosition;
        private static int _stuckTicks;
        private static float _bestRouteAngle;
        private static int _orbitTicks;
        private static bool _orbitRecoveryUsed;
        private static int _detourCount;
        private static bool _isAvoidingObstacle;
        private static Vector3 _pendingWaypoint;
        private static bool _hasPendingWaypoint;
        private static Vector3 _constructionDestination;
        private static bool _hasConstructionPlan;
        private static bool _atConstructionDestination;
        private static long _constructionArrivalTick;
        private static bool _hasConstructionArrival;
        private static int _flightSettleTicks;
        private static Vector3 _deferredRoute;
        private static bool _hasDeferredRoute;
        private static bool _deferredRouteKeepAltitude;
        private static int _planAstroId;
        private static long _lastPlanAttemptTick;
        private static bool _hasPlanAttempted;
        private static readonly Vector3[] PlannerCandidates = new Vector3[MaxPlannerCandidates];
        private static readonly float[] PlannerCandidateDistanceSquared = new float[MaxPlannerCandidates];
        private static readonly Vector3[] PlannerRefinementSources = new Vector3[MaxPlannerRefinementCandidates];
        private static readonly Vector3[] PlannerRefinementCandidates = new Vector3[MaxPlannerRefinementCandidates];
        private static readonly float[] PlannerRefinementScores = new float[MaxPlannerRefinementCandidates];
        private static readonly float[] PlannerTargetDistancesSquared = new float[MaxImmediateBuildTargets];
        private static readonly Vector3[] PlannerTargetPositions = new Vector3[MaxImmediateBuildTargets];
        private static int _plannerCandidateCount;
        private static int _plannerRefinementCount;
        private static int _plannerTargetCount;
        private static int _plannerEligibleTargetCount;
        private static Vector3 _nearestPlannerTarget;
        private static float _nearestPlannerTargetDistanceSquared;
        private static float _plannerTargetDistanceSum;
        private static Vector3 _plannerTargetDirectionSum;
        private static int _plannerNearbyTargetCount;
        private static int _currentSiteTargetCount;
        private static int _currentSiteNearbyTargetCount;
        private static float _currentSiteScore;
        private static float _nextSiteScore;
        private static readonly int[] ItemAvailabilityProtoIds = new int[ItemAvailabilityCacheSize];
        private static readonly int[] ItemAvailabilityCounts = new int[ItemAvailabilityCacheSize];
        private static Vector3 _plannerFanForward;
        private static Vector3 _plannerFanRight;
        private static int _itemAvailabilityCount;

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

            if (player.navigation.navigating || global::UXAssist.Patches.PlayerPatch.AutoNavigationG.IsActive)
            {
                ClearAutoRoute(player);
                ResetNavigation();
                return;
            }

            var currentOrder = player.orders.currentOrder;
            if (HasManualInput() ||
                currentOrder != null && currentOrder != _autoOrder)
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

            if (_flightSettleTicks > 0 && !UpdateFlightSettling(player)) return;

            if (_hasDeferredRoute)
            {
                var deferredRoute = _deferredRoute;
                var keepAltitude = _deferredRouteKeepAltitude;
                _deferredRoute = Vector3.zero;
                _hasDeferredRoute = false;
                _deferredRouteKeepAltitude = false;
                IssueRoute(player, deferredRoute, keepAltitude);
                return;
            }

            if (_atConstructionDestination && IsPlannerDue(timei))
            {
                if (!TryFindBestDestination(factory, player, timei, out var nextDestination)) return;

                if (!IsFinalConstructionRun() && ShouldStayAtConstructionSite(player, timei)) return;

                ClearAutoRoute(player);
                ResetNavigation();
                ApplyConstructionPlan(factory, player, nextDestination, timei);
            }

            if (!_hasConstructionPlan) return;
            if (_atConstructionDestination && _autoOrder == null && !_hasPendingWaypoint) return;

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

                if (!IsAtConstructionDestination(player))
                {
                    if (_isAvoidingObstacle)
                    {
                        _isAvoidingObstacle = false;
                        _detourCount = 0;
                        IssueRoute(player, _constructionDestination, false);
                        return;
                    }

                    ResetNavigation();
                    return;
                }

                _atConstructionDestination = true;
                _constructionArrivalTick = timei;
                _hasConstructionArrival = true;
                BeginFlightSettling();
                return;
            }

            if (!_hasPendingWaypoint && IsAtConstructionDestination(player))
            {
                ClearAutoRoute(player);
                _atConstructionDestination = true;
                _constructionArrivalTick = timei;
                _hasConstructionArrival = true;
                BeginFlightSettling();
                _stuckTicks = 0;
                return;
            }

            _atConstructionDestination = false;
            if (player.movementState == EMovementState.Walk && player.mecha.thrusterLevel >= 1)
            {
                CancelAutoMotion(player);
                player.controller.actionWalk.SwitchToFly();
                return;
            }

            if (_autoOrder == null)
            {
                _isAvoidingObstacle = false;
                _detourCount = 0;
                IssueRoute(player, _constructionDestination, false);
                return;
            }

            var positionDelta = player.position - _lastPosition;
            var moved = positionDelta.sqrMagnitude >= StuckDistance * StuckDistance;
            var radialMovement = Mathf.Abs(player.position.magnitude - _lastPosition.magnitude);
            if (!moved)
            {
                _stuckTicks += (int)AutoConstructTickInterval;
            }
            else
            {
                _stuckTicks = 0;
                _lastPosition = player.position;
            }

            var routeTarget = _autoOrder.target;
            var orbitDetected = DetectOrbit(player, routeTarget, moved, radialMovement);
            if (_stuckTicks >= StuckTickThreshold || orbitDetected)
            {
                if (orbitDetected)
                {
                    _orbitRecoveryUsed = true;
                    CancelAutoMotion(player);
                }

                var altitude = _isAvoidingObstacle ? AvoidanceAltitude : GetCurrentAltitude(player);
                if (_detourCount < 4 && TryFindDetour(player, routeTarget, altitude, out var detour, out var pendingWaypoint))
                {
                    _detourCount++;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = pendingWaypoint;
                    _hasPendingWaypoint = pendingWaypoint.sqrMagnitude > 0.01f;
                    player.controller.actionFly.targetAltitude = AvoidanceAltitude;
                    if (orbitDetected)
                    {
                        QueueRouteAfterFlightSettle(detour, true);
                    }
                    else
                    {
                        IssueRoute(player, detour, true);
                    }
                }
                else
                {
                    _detourCount = 0;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = Vector3.zero;
                    _hasPendingWaypoint = false;
                    player.controller.actionFly.targetAltitude = AvoidanceAltitude;
                    if (orbitDetected)
                    {
                        QueueRouteAfterFlightSettle(_constructionDestination, true);
                    }
                    else
                    {
                        IssueRoute(player, _constructionDestination, true);
                    }
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
            _hasConstructionPlan = false;
            _atConstructionDestination = false;
            var hasDestination = TryFindBestDestination(factory, player, timei, out var destination);
            if (!hasDestination && _currentSiteTargetCount == 0) return false;
            if (!hasDestination || (!IsFinalConstructionRun() && ShouldStayAtConstructionSite(player, timei)))
            {
                destination = player.position;
            }

            ApplyConstructionPlan(factory, player, destination, timei);
            return true;
        }

        private static bool TryFindBestDestination(PlanetFactory factory, Player player, long timei, out Vector3 destination)
        {
            destination = default;
            _lastPlanAttemptTick = timei;
            _hasPlanAttempted = true;
            _plannerCandidateCount = 0;
            _plannerRefinementCount = 0;
            _itemAvailabilityCount = 0;
            _plannerEligibleTargetCount = 0;
            _nearestPlannerTarget = Vector3.zero;
            _nearestPlannerTargetDistanceSquared = float.MaxValue;
            _currentSiteTargetCount = 0;
            _currentSiteNearbyTargetCount = 0;
            _currentSiteScore = 0f;
            _nextSiteScore = 0f;
            for (var i = 0; i < MaxPlannerCandidates; i++)
            {
                PlannerCandidateDistanceSquared[i] = -1f;
            }
            InitializePlannerFan(player);

            var buildArea = Mathf.Max(0f, player.mecha.buildArea);
            if (buildArea < PlanArrivalDistance) return false;

            var buildAreaSquared = buildArea * buildArea;
            _currentSiteScore = EvaluatePlannerCandidate(
                factory, player, player.position, buildAreaSquared, out _currentSiteTargetCount, out _, true);
            _currentSiteNearbyTargetCount = _plannerNearbyTargetCount;
            CollectPlannerCandidates(factory, player);
            if (_plannerCandidateCount == 0)
            {
                if (!IsFinalConstructionRun()) return false;

                destination = _nearestPlannerTarget;
                return destination.sqrMagnitude >= 0.01f;
            }

            var bestScore = float.MinValue;
            var bestCoverageCount = 0;
            var bestCandidate = Vector3.zero;
            for (var i = 0; i < MaxPlannerCandidates; i++)
            {
                if (PlannerCandidateDistanceSquared[i] < 0f) continue;
                var candidate = PlannerCandidates[i];
                var score = EvaluatePlannerCandidate(
                    factory, player, candidate, buildAreaSquared, out var coverageCount, out var refinedCandidate);
                ConsiderPlannerDestination(
                    candidate, score, coverageCount, ref bestScore, ref bestCoverageCount, ref bestCandidate);
                AddPlannerRefinementCandidate(candidate, refinedCandidate, score, coverageCount);
            }

            for (var i = 0; i < _plannerRefinementCount; i++)
            {
                var refinedCandidate = PlannerRefinementCandidates[i];
                if (GetSurfaceDistanceSquared(factory, refinedCandidate, player.position) <= MinimumSiteMoveDistance * MinimumSiteMoveDistance ||
                    (refinedCandidate - PlannerRefinementSources[i]).sqrMagnitude <= CandidateMergeDistance * CandidateMergeDistance)
                {
                    continue;
                }

                var score = EvaluatePlannerCandidate(
                    factory, player, refinedCandidate, buildAreaSquared, out var coverageCount, out _);
                ConsiderPlannerDestination(
                    refinedCandidate, score, coverageCount, ref bestScore, ref bestCoverageCount, ref bestCandidate);
            }

            if (bestCoverageCount == 0) return false;

            _nextSiteScore = bestScore;
            destination = IsFinalConstructionRun() ? _nearestPlannerTarget : bestCandidate;
            return true;
        }

        private static void ConsiderPlannerDestination(
            Vector3 candidate,
            float score,
            int coverageCount,
            ref float bestScore,
            ref int bestCoverageCount,
            ref Vector3 bestCandidate)
        {
            if (coverageCount <= 0 || score <= bestScore) return;

            bestScore = score;
            bestCoverageCount = coverageCount;
            bestCandidate = candidate;
        }

        private static void AddPlannerRefinementCandidate(
            Vector3 source,
            Vector3 refinedCandidate,
            float score,
            int coverageCount)
        {
            if (coverageCount <= 0 || refinedCandidate.sqrMagnitude < 0.01f ||
                (refinedCandidate - source).sqrMagnitude <= CandidateMergeDistance * CandidateMergeDistance)
            {
                return;
            }

            if (_plannerRefinementCount < MaxPlannerRefinementCandidates)
            {
                var index = _plannerRefinementCount++;
                PlannerRefinementSources[index] = source;
                PlannerRefinementCandidates[index] = refinedCandidate;
                PlannerRefinementScores[index] = score;
                return;
            }

            var weakestIndex = 0;
            var weakestScore = PlannerRefinementScores[0];
            for (var i = 1; i < MaxPlannerRefinementCandidates; i++)
            {
                if (PlannerRefinementScores[i] < weakestScore)
                {
                    weakestIndex = i;
                    weakestScore = PlannerRefinementScores[i];
                }
            }

            if (score <= weakestScore) return;
            PlannerRefinementSources[weakestIndex] = source;
            PlannerRefinementCandidates[weakestIndex] = refinedCandidate;
            PlannerRefinementScores[weakestIndex] = score;
        }

        private static void ApplyConstructionPlan(PlanetFactory factory, Player player, Vector3 destination, long timei)
        {
            _constructionDestination = destination;
            _hasConstructionPlan = true;
            _planAstroId = factory.planet.astroId;
            _autoOrder = null;
            _atConstructionDestination = false;
            _constructionArrivalTick = 0;
            _hasConstructionArrival = false;
            _flightSettleTicks = 0;
            _deferredRoute = Vector3.zero;
            _hasDeferredRoute = false;
            _deferredRouteKeepAltitude = false;
            _stuckTicks = 0;
            _bestRouteAngle = 0f;
            _orbitTicks = 0;
            _orbitRecoveryUsed = false;
            _detourCount = 0;
            _isAvoidingObstacle = false;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
            _lastPosition = player.position;
            _lastPlanAttemptTick = timei;
            _hasPlanAttempted = true;
        }

        private static void CollectPlannerCandidates(PlanetFactory factory, Player player)
        {
            var prebuilds = factory.prebuildPool;
            for (var i = 1; i < factory.prebuildCursor; i++)
            {
                ref var prebuild = ref prebuilds[i];
                if (prebuild.id != i || prebuild.isDestroyed || prebuild.builderLaunched) continue;
                if (!HasRequiredItems(player, prebuild.protoId, prebuild.itemRequired)) continue;

                var distanceSquared = GetSurfaceDistanceSquared(factory, prebuild.pos, player.position);
                if (_plannerEligibleTargetCount < FinalConstructionTargetLimit + 1)
                {
                    _plannerEligibleTargetCount++;
                }

                if (distanceSquared < _nearestPlannerTargetDistanceSquared)
                {
                    _nearestPlannerTargetDistanceSquared = distanceSquared;
                    _nearestPlannerTarget = prebuild.pos;
                }

                if (distanceSquared <= MinimumSiteMoveDistance * MinimumSiteMoveDistance) continue;
                AddPlannerCandidate(prebuild.pos, distanceSquared);
            }
        }

        private static bool IsFinalConstructionRun()
        {
            return _plannerEligibleTargetCount > 0 &&
                   _plannerEligibleTargetCount <= FinalConstructionTargetLimit;
        }

        private static float GetSurfaceDistanceSquared(PlanetFactory factory, Vector3 first, Vector3 second)
        {
            var radius = factory.planet.realRadius;
            return (first.normalized - second.normalized).sqrMagnitude * radius * radius;
        }

        private static void InitializePlannerFan(Player player)
        {
            var radial = player.position.normalized;
            var forward = Vector3.ProjectOnPlane(player.uRotation * Vector3.forward, radial);
            if (forward.sqrMagnitude < 0.01f)
            {
                var reference = Mathf.Abs(radial.y) < 0.9f ? Vector3.up : Vector3.right;
                forward = Vector3.ProjectOnPlane(reference, radial);
            }

            _plannerFanForward = forward.normalized;
            _plannerFanRight = Vector3.Cross(radial, _plannerFanForward).normalized;
        }

        private static void AddPlannerCandidate(Vector3 position, float distanceSquared)
        {
            var sector = GetPlannerFanSector(position);
            var nearIndex = sector * 2;
            var farIndex = nearIndex + 1;
            var mergeDistanceSquared = CandidateMergeDistance * CandidateMergeDistance;

            if (PlannerCandidateDistanceSquared[nearIndex] < 0f ||
                distanceSquared < PlannerCandidateDistanceSquared[nearIndex])
            {
                if (PlannerCandidateDistanceSquared[nearIndex] < 0f)
                {
                    _plannerCandidateCount++;
                }

                PlannerCandidates[nearIndex] = position;
                PlannerCandidateDistanceSquared[nearIndex] = distanceSquared;
            }

            if (PlannerCandidateDistanceSquared[farIndex] < 0f &&
                distanceSquared - PlannerCandidateDistanceSquared[nearIndex] > mergeDistanceSquared)
            {
                _plannerCandidateCount++;
                PlannerCandidates[farIndex] = position;
                PlannerCandidateDistanceSquared[farIndex] = distanceSquared;
            }
            else if (PlannerCandidateDistanceSquared[farIndex] >= 0f &&
                     distanceSquared > PlannerCandidateDistanceSquared[farIndex] &&
                     (position - PlannerCandidates[nearIndex]).sqrMagnitude > mergeDistanceSquared)
            {
                PlannerCandidates[farIndex] = position;
                PlannerCandidateDistanceSquared[farIndex] = distanceSquared;
            }
        }

        private static int GetPlannerFanSector(Vector3 position)
        {
            var direction = position.normalized;
            var angle = Mathf.Atan2(
                Vector3.Dot(direction, _plannerFanRight),
                Vector3.Dot(direction, _plannerFanForward));
            if (angle < 0f) angle += Mathf.PI * 2f;
            return Mathf.Min(
                PlannerFanSectorCount - 1,
                (int)(angle / (Mathf.PI * 2f / PlannerFanSectorCount)));
        }

        private static float EvaluatePlannerCandidate(
            PlanetFactory factory,
            Player player,
            Vector3 candidate,
            float buildAreaSquared,
            out int coverageCount,
            out Vector3 refinedCandidate,
            bool currentSite = false)
        {
            coverageCount = 0;
            refinedCandidate = candidate;
            _plannerTargetCount = 0;
            _plannerTargetDistanceSum = 0f;
            _plannerNearbyTargetCount = 0;
            _plannerTargetDirectionSum = Vector3.zero;
            var inspectedCount = 0;
            var reachedScanLimit = false;
            var prebuilds = factory.prebuildPool;
            var hashSystem = factory.hashSystemStatic;
            var hashPool = hashSystem.hashPool;
            var bucketOffsets = hashSystem.bucketOffsets;
            var realRadius = factory.planet.realRadius;
            var planningCenter = candidate.normalized * (realRadius + ConstructionFlightAltitude);
            var productiveSurfaceRadius = Mathf.Min(
                Mathf.Sqrt(buildAreaSquared),
                Mathf.Clamp(Mathf.Sqrt(buildAreaSquared) * 0.45f, 10f, 24f));
            if (currentSite)
            {
                planningCenter = player.position.normalized * (realRadius + ConstructionFlightAltitude);
            }
            hashSystem.GetBucketIdxesInArea(planningCenter, Mathf.Sqrt(buildAreaSquared));
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

                    var distanceSquared = (prebuild.pos - planningCenter).sqrMagnitude;
                    if (distanceSquared > buildAreaSquared) continue;

                    if (coverageCount < MaxOverflowBuildTargets)
                    {
                        coverageCount++;
                    }

                    var surfaceDistanceSquared = GetSurfaceDistanceSquared(factory, prebuild.pos, planningCenter);
                    if (surfaceDistanceSquared <= productiveSurfaceRadius * productiveSurfaceRadius &&
                        _plannerNearbyTargetCount < MaxImmediateBuildTargets)
                    {
                        _plannerNearbyTargetCount++;
                    }
                    AddPlannerTarget(prebuild.pos, distanceSquared);
                }
            }

            hashSystem.ClearActiveBuckets();
            if (_plannerTargetCount == 0) return float.MinValue;

            var droneCount = Mathf.Max(1, player.mecha.constructionModule.droneCount);
            var droneSpeed = Mathf.Max(1f, factory.gameData.history.constructionDroneSpeed);
            var targetsPerDrone = Mathf.Clamp(factory.gameData.history.constructionDroneMovement, 1, 4);
            var waveSize = Mathf.Max(1, droneCount * targetsPerDrone);
            var productiveTargetCount = Mathf.Max(
                1f,
                _plannerNearbyTargetCount + Mathf.Min(
                    Mathf.Max(0, coverageCount - _plannerNearbyTargetCount), waveSize) * 0.15f);
            if (currentSite)
            {
                productiveTargetCount = Mathf.Max(1, _plannerNearbyTargetCount);
            }

            var waveCount = Mathf.Ceil(productiveTargetCount / waveSize);
            var averageDroneDistance = _plannerTargetDistanceSum / _plannerTargetCount;
            var launchSeconds = 1f / Mathf.Clamp(droneSpeed * 0.35f, 1f, 3f);
            var flightSeconds = averageDroneDistance * 2f / droneSpeed;
            var constructionSeconds = productiveTargetCount * DroneBuildSecondsPerTarget / droneCount;
            var droneWorkSeconds = constructionSeconds + waveCount * (launchSeconds + flightSeconds);
            var playerTravelDistance = currentSite
                ? 0f
                : Mathf.Sqrt(GetSurfaceDistanceSquared(factory, planningCenter, player.position));
            var playerTravelSeconds = playerTravelDistance / Mathf.Max(1f, player.mecha.walkSpeed * 2.5f);
            var score = productiveTargetCount / Mathf.Max(0.25f, droneWorkSeconds + playerTravelSeconds);
            if (_plannerTargetDirectionSum.sqrMagnitude > 0.01f)
            {
                refinedCandidate = GetPlannerRepresentativeTarget();
            }

            return score;
        }

        private static Vector3 GetPlannerRepresentativeTarget()
        {
            var centerDirection = _plannerTargetDirectionSum.normalized;
            if (centerDirection.sqrMagnitude < 0.01f) return PlannerTargetPositions[0];

            var bestIndex = 0;
            var bestAlignment = Vector3.Dot(PlannerTargetPositions[0].normalized, centerDirection);
            for (var i = 1; i < _plannerTargetCount; i++)
            {
                var alignment = Vector3.Dot(PlannerTargetPositions[i].normalized, centerDirection);
                if (alignment <= bestAlignment) continue;

                bestIndex = i;
                bestAlignment = alignment;
            }

            return PlannerTargetPositions[bestIndex];
        }

        private static void AddPlannerTarget(Vector3 position, float distanceSquared)
        {
            var direction = position.normalized;
            if (_plannerTargetCount < MaxImmediateBuildTargets)
            {
                var index = _plannerTargetCount++;
                PlannerTargetDistancesSquared[index] = distanceSquared;
                PlannerTargetPositions[index] = position;
                _plannerTargetDistanceSum += Mathf.Sqrt(distanceSquared);
                _plannerTargetDirectionSum += direction;
                SiftPlannerTargetUp(index);
                return;
            }

            if (distanceSquared >= PlannerTargetDistancesSquared[0]) return;

            _plannerTargetDistanceSum -= Mathf.Sqrt(PlannerTargetDistancesSquared[0]);
            _plannerTargetDirectionSum -= PlannerTargetPositions[0].normalized;
            PlannerTargetDistancesSquared[0] = distanceSquared;
            PlannerTargetPositions[0] = position;
            _plannerTargetDistanceSum += Mathf.Sqrt(distanceSquared);
            _plannerTargetDirectionSum += direction;
            SiftPlannerTargetDown(0);
        }

        private static void SiftPlannerTargetUp(int index)
        {
            while (index > 0)
            {
                var parent = (index - 1) >> 1;
                if (PlannerTargetDistancesSquared[parent] >= PlannerTargetDistancesSquared[index]) return;
                SwapPlannerTargets(parent, index);
                index = parent;
            }
        }

        private static void SiftPlannerTargetDown(int index)
        {
            while (true)
            {
                var left = (index << 1) + 1;
                if (left >= _plannerTargetCount) return;

                var largest = left;
                var right = left + 1;
                if (right < _plannerTargetCount &&
                    PlannerTargetDistancesSquared[right] > PlannerTargetDistancesSquared[left])
                {
                    largest = right;
                }

                if (PlannerTargetDistancesSquared[index] >= PlannerTargetDistancesSquared[largest]) return;
                SwapPlannerTargets(index, largest);
                index = largest;
            }
        }

        private static void SwapPlannerTargets(int left, int right)
        {
            var distanceSquared = PlannerTargetDistancesSquared[left];
            PlannerTargetDistancesSquared[left] = PlannerTargetDistancesSquared[right];
            PlannerTargetDistancesSquared[right] = distanceSquared;

            var position = PlannerTargetPositions[left];
            PlannerTargetPositions[left] = PlannerTargetPositions[right];
            PlannerTargetPositions[right] = position;
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

        private static bool IsAtConstructionDestination(Player player)
        {
            var planet = player.planetData;
            if (planet == null) return false;

            if (_constructionDestination.sqrMagnitude < 0.01f) return false;

            var realRadius = planet.realRadius;
            var playerSurfacePosition = player.position.normalized * realRadius;
            var targetSurfacePosition = _constructionDestination.normalized * realRadius;
            var nativeArrivalDistance = Mathf.Max(0.25f, player.speed * 0.1f);
            if (player.movementState >= EMovementState.Fly) nativeArrivalDistance *= 1.5f;
            var arrivalDistance = Mathf.Max(PlanArrivalDistance, nativeArrivalDistance);
            return (playerSurfacePosition - targetSurfacePosition).sqrMagnitude <= arrivalDistance * arrivalDistance;
        }

        private static bool ShouldStayAtConstructionSite(Player player, long timei)
        {
            var constructionModule = player.mecha.constructionModule;
            var activeDroneCount = Mathf.Clamp(
                constructionModule.droneCount - constructionModule.droneIdleCount,
                0,
                Mathf.Max(1, constructionModule.droneCount));

            if (_hasConstructionArrival && timei - _constructionArrivalTick < MinimumConstructionSiteHoldTicks)
            {
                return _currentSiteTargetCount > 0 || activeDroneCount > 0;
            }

            if (_currentSiteNearbyTargetCount <= 0 || _currentSiteScore <= 0f) return false;
            return _nextSiteScore < _currentSiteScore * SiteSwitchGain;
        }

        private static bool HasManualInput()
        {
            return VFInput._pullUp.pressing || VFInput._pushDown.pressing ||
                   VFInput._moveLeft.pressing || VFInput._moveRight.pressing ||
                   VFInput._moveForward.pressing || VFInput._moveBackward.pressing;
        }

        private static bool DetectOrbit(Player player, Vector3 routeTarget, bool moved, float radialMovement)
        {
            var routeAngle = Vector3.Angle(player.position.normalized, routeTarget.normalized);
            if (_bestRouteAngle <= 0f || routeAngle + OrbitAngleProgressThreshold < _bestRouteAngle)
            {
                _bestRouteAngle = routeAngle;
                _orbitTicks = 0;
                return false;
            }

            if (!moved || radialMovement > OrbitRadialMovementThreshold)
            {
                _orbitTicks = 0;
                return false;
            }

            _orbitTicks += (int)AutoConstructTickInterval;
            return !_orbitRecoveryUsed && _orbitTicks >= OrbitTickThreshold;
        }

        private static void BeginFlightSettling()
        {
            _flightSettleTicks = Mathf.Max(_flightSettleTicks, FlightSettleDurationTicks);
        }

        private static bool UpdateFlightSettling(Player player)
        {
            if (_flightSettleTicks > 0)
            {
                _flightSettleTicks -= (int)AutoConstructTickInterval;
                return false;
            }

            var radial = player.position.sqrMagnitude > 0.01f ? player.position.normalized : Vector3.up;
            var tangentialSpeed = Vector3.ProjectOnPlane(player.controller.velocity, radial).magnitude;
            var rtsSpeed = player.controller.actionFly != null ? player.controller.actionFly.rtsVelocity.magnitude : 0f;
            if (rtsSpeed <= FlightSettleRtsSpeed && tangentialSpeed <= FlightSettleTangentialSpeed)
            {
                _flightSettleTicks = 0;
                return true;
            }

            return false;
        }

        private static void QueueRouteAfterFlightSettle(Vector3 target, bool keepAltitude)
        {
            _deferredRoute = target;
            _deferredRouteKeepAltitude = keepAltitude;
            _hasDeferredRoute = true;
        }

        private static bool ShouldBrakeBeforeRoute(Player player, Vector3 targetPosition)
        {
            if (player.movementState != EMovementState.Fly || player.planetData == null) return false;

            var radial = player.position.sqrMagnitude > 0.01f ? player.position.normalized : Vector3.up;
            var currentVelocity = Vector3.ProjectOnPlane(player.controller.velocity, radial);
            var currentSpeed = currentVelocity.magnitude;
            if (currentSpeed <= FlightSettleTangentialSpeed) return false;

            var desiredVelocity = Vector3.ProjectOnPlane(targetPosition.normalized, radial);
            if (desiredVelocity.sqrMagnitude < 0.01f) return false;

            var headingAlignment = Vector3.Dot(currentVelocity.normalized, desiredVelocity.normalized);
            var surfaceDistance = Vector3.Angle(radial, targetPosition.normalized) * Mathf.Deg2Rad * player.planetData.realRadius;
            return headingAlignment < 0.35f ||
                   headingAlignment < 0.85f && surfaceDistance < currentSpeed * 1.25f;
        }

        private static void CancelAutoMotion(Player player)
        {
            player.Order(OrderNode.Stop, false);
            player.ClearOrders();
            _autoOrder = null;
            _stuckTicks = 0;
            _orbitTicks = 0;
            BeginFlightSettling();
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

            if (!_hasDeferredRoute && ShouldBrakeBeforeRoute(player, targetPosition))
            {
                CancelAutoMotion(player);
                QueueRouteAfterFlightSettle(targetPosition, keepAltitude);
                return;
            }

            _autoOrder = OrderNode.MoveTo(targetPosition);
            player.Order(_autoOrder, false);
            _bestRouteAngle = Vector3.Angle(player.position.normalized, targetPosition.normalized);
            _orbitTicks = 0;
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
            _bestRouteAngle = 0f;
            _orbitTicks = 0;
            _orbitRecoveryUsed = false;
            _detourCount = 0;
            _isAvoidingObstacle = false;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
            _constructionDestination = Vector3.zero;
            _hasConstructionPlan = false;
            _atConstructionDestination = false;
            _constructionArrivalTick = 0;
            _hasConstructionArrival = false;
            _flightSettleTicks = 0;
            _deferredRoute = Vector3.zero;
            _hasDeferredRoute = false;
            _deferredRouteKeepAltitude = false;
            _planAstroId = 0;
            _lastPlanAttemptTick = 0;
            _hasPlanAttempted = false;
            _plannerCandidateCount = 0;
            _plannerEligibleTargetCount = 0;
            _nearestPlannerTarget = Vector3.zero;
            _nearestPlannerTargetDistanceSquared = float.MaxValue;
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
