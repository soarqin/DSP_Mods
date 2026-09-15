using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.Utils;
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
        private const float SiteSwitchGain = 1.15f;
        private const float DroneBuildSecondsPerTarget = 2f;
        private const int MinimumConstructionSiteHoldTicks = 45;
        private const int FlightSettleDurationTicks = 45;
        private const int FlightSettleTimeoutTicks = 180;
        private const float FlightSettleRtsSpeed = 4f;
        private const float FlightSettleTangentialSpeed = 6f;
        private const float CandidateMergeDistance = 6f;
        private const float AvoidanceAltitude = 35f;
        private const float AvoidanceClearance = 2.5f;
        private const float StuckDistance = 1.5f;
        private const int StuckTickThreshold = 45;
        private const float OrbitRadialMovementThreshold = 0.75f;
        private const float OrbitAngleProgressThreshold = 0.5f;
        private const int OrbitTickThreshold = 45;
        private const int MaxDetourAttempts = 8;
        private const float LocalRouteRadiusMultiplier = 3f;
        private const float RouteCorridorWidthFactor = 0.8f;
        private static readonly float[] DetourRadiusScales = { 1f, 1.5f, 2f, 3f };
        private static readonly int[] DetourSideSigns = { -1, 1 };
        private static readonly Collider[] ObstacleOverlapColliders = new Collider[8];

        /// <summary>
        /// Physics layers probed when looking for flight obstacles: bit 9 (building colliders) and
        /// bit 13 (terrain).
        /// </summary>
        private const int ObstacleLayerMask = (1 << 9) | (1 << 13);

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
        private static bool _flightSettlePending;
        private static int _flightSettleTicks;
        private static int _flightSettleWaitTicks;
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
        private static int _plannerLaunchedTargetCount;
        private static Vector3 _nearestPlannerTarget;
        private static float _nearestPlannerTargetDistanceSquared;
        private static float _plannerTargetDistanceSum;
        private static Vector3 _plannerTargetDirectionSum;
        private static int _currentSiteTargetCount;
        private static int _currentSiteLaunchedTargetCount;
        private static float _currentSiteScore;
        private static float _nextSiteScore;
        private static readonly int[] ItemAvailabilityProtoIds = new int[ItemAvailabilityCacheSize];
        private static readonly int[] ItemAvailabilityCounts = new int[ItemAvailabilityCacheSize];
        private static Vector3 _plannerFanForward;
        private static Vector3 _plannerFanRight;
        private static int _itemAvailabilityCount;
        private static bool _checkEnemyProximity;
        private static bool _enableDiagnosticsLogged;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameEnd += ResetNavigation;
            GameLogicProc.OnGameEnd += ResetEnableDiagnostics;
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            LogEnableDiagnostics();
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameEnd -= ResetNavigation;
            GameLogicProc.OnGameEnd -= ResetEnableDiagnostics;
            ClearAutoRoute(GameMain.mainPlayer);
            ResetNavigation();
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        }

        private static void ResetEnableDiagnostics() => _enableDiagnosticsLogged = false;

        /// <summary>
        /// Diagnostic aid for reports of the auto-construct button not showing up: logs the inputs of
        /// the visibility predicate. Auto-construct is toggled from in-game, so this is throttled to
        /// once per game session unless the button object itself is missing, which is exactly the
        /// failure being investigated.
        /// </summary>
        private static void LogEnableDiagnostics()
        {
            var buttonCreated = Functions.UI.AutoConstructUI.ToggleAutoConstruct != null;
            if (_enableDiagnosticsLogged && buttonCreated) return;

            _enableDiagnosticsLogged = true;
            var planet = GameMain.localPlanet;
            var factoryLoaded = planet != null && planet.factoryLoaded;
            UXAssist.Logger.LogInfo(
                $"AutoConstruct enabled: buttonCreated={buttonCreated}, " +
                $"buttonConfig={FactoryPatch.AutoConstructButtonEnabled.Value}, " +
                $"localPlanet={planet != null}, factoryLoaded={factoryLoaded}, " +
                $"prebuildCount={(factoryLoaded ? planet.factory.prebuildCount : 0)}");
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
                ClearAutoRoute(player);
                ResetNavigation();
                return;
            }

            if (player.navigation.navigating || global::UXAssist.Patches.PlayerPatch.AutoNavigation.IsActive)
            {
                ClearAutoRoute(player);
                ResetNavigation();
                return;
            }

            var currentOrder = player.orders.currentOrder;
            if (PlayerInputUtil.HasManualMovementInput() ||
                currentOrder != null && currentOrder != _autoOrder)
            {
                ClearAutoRoute(player);
                ResetNavigation();
                return;
            }

            // PlanetFactory.prebuildCount is prebuildCursor - prebuildRecycleCursor - 1, the same
            // gate ConstructionSystem.DetermineLaunch uses, so it is the authoritative "there is
            // still construction work on this planet" signal.
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

            MaintainAvoidanceAltitude(player);

            if (_flightSettlePending && !UpdateFlightSettling(player)) return;

            if (_hasDeferredRoute)
            {
                var deferredRoute = _deferredRoute;
                var raiseAltitude = _deferredRouteKeepAltitude;
                _deferredRoute = Vector3.zero;
                _hasDeferredRoute = false;
                _deferredRouteKeepAltitude = false;
                // The braking this route was deferred for has already happened, so bypass the brake
                // check. Re-evaluating it here could defer the same route again and, if the settling
                // wait ends on its timeout instead of on a low speed, keep the mecha stationary.
                IssueRoute(player, deferredRoute, raiseAltitude, true);
                return;
            }

            if (_atConstructionDestination && IsPlannerDue(timei))
            {
                if (!TryFindBestDestination(factory, player, timei, out var nextDestination)) return;

                if (!IsFinalConstructionRun() && ShouldStayAtConstructionSite(factory, player, timei)) return;

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
                    IssueRoute(player, pendingWaypoint, true, true);
                    return;
                }

                if (!IsAtConstructionDestination(player))
                {
                    if (_isAvoidingObstacle)
                    {
                        _isAvoidingObstacle = false;
                        _detourCount = 0;
                        IssueRoute(player, _constructionDestination, false, true);
                        return;
                    }

                    ResetNavigation();
                    return;
                }

                MarkConstructionArrival(player, timei);
                return;
            }

            if (!_hasPendingWaypoint && IsAtConstructionDestination(player))
            {
                ClearAutoRoute(player);
                MarkConstructionArrival(player, timei);
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
                if (_detourCount < MaxDetourAttempts && TryFindDetour(player, routeTarget, altitude, out var detour, out var pendingWaypoint))
                {
                    _detourCount++;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = pendingWaypoint;
                    _hasPendingWaypoint = pendingWaypoint.sqrMagnitude > 0.01f;
                    IssueRecoveryRoute(player, detour);
                }
                else
                {
                    _detourCount = 0;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = Vector3.zero;
                    _hasPendingWaypoint = false;
                    IssueRecoveryRoute(player, _constructionDestination);
                }

                _stuckTicks = 0;
                _lastPosition = player.position;
            }
        }

        private static bool IsPlannerDue(long timei)
        {
            return !_hasPlanAttempted || timei < _lastPlanAttemptTick || timei - _lastPlanAttemptTick >= PlannerTickInterval;
        }

        private static void ResetPlannerScan()
        {
            _plannerCandidateCount = 0;
            _plannerRefinementCount = 0;
            _plannerTargetCount = 0;
            _plannerEligibleTargetCount = 0;
            _plannerLaunchedTargetCount = 0;
            _nearestPlannerTarget = Vector3.zero;
            _nearestPlannerTargetDistanceSquared = float.MaxValue;
            _plannerTargetDistanceSum = 0f;
            _plannerTargetDirectionSum = Vector3.zero;
            _currentSiteTargetCount = 0;
            _currentSiteLaunchedTargetCount = 0;
            _currentSiteScore = 0f;
            _nextSiteScore = 0f;
            _itemAvailabilityCount = 0;
            _checkEnemyProximity = false;
            for (var i = 0; i < MaxPlannerCandidates; i++)
            {
                PlannerCandidateDistanceSquared[i] = -1f;
            }
        }

        private static bool TryCreatePlan(PlanetFactory factory, Player player, long timei)
        {
            _hasConstructionPlan = false;
            _atConstructionDestination = false;
            var hasDestination = TryFindBestDestination(factory, player, timei, out var destination);
            if (!hasDestination && _currentSiteTargetCount == 0) return false;
            if (!hasDestination || (!IsFinalConstructionRun() && ShouldStayAtConstructionSite(factory, player, timei)))
            {
                destination = player.position;
            }

            ApplyConstructionPlan(factory, player, destination, timei);
            return true;
        }

        private static void MarkConstructionArrival(Player player, long timei)
        {
            _atConstructionDestination = true;
            _constructionArrivalTick = timei;
            _hasConstructionArrival = true;
            _stuckTicks = 0;
            // A detour raises the flight altitude to AvoidanceAltitude, but the planner scored this
            // site's build-range coverage at ConstructionFlightAltitude. Release the detour state on
            // arrival, otherwise the mecha keeps hovering high enough that the construction drones
            // cannot reach part of the ghosts that score was based on.
            _isAvoidingObstacle = false;
            _detourCount = 0;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
            if (player.movementState == EMovementState.Fly)
            {
                player.controller.actionFly.targetAltitude = ConstructionFlightAltitude;
            }

            BeginFlightSettling();
        }

        private static bool TryFindBestDestination(PlanetFactory factory, Player player, long timei, out Vector3 destination)
        {
            destination = default;
            _lastPlanAttemptTick = timei;
            _hasPlanAttempted = true;
            ResetPlannerScan();
            InitializePlannerFan(player);

            var buildArea = Mathf.Max(0f, player.mecha.buildArea);
            if (buildArea < PlanArrivalDistance) return false;

            // Probing every ghost for nearby Dark Fog buildings is only worth it when the planet
            // actually has ground bases; resolve that once per planning pass.
            var enemySystem = factory.enemySystem;
            _checkEnemyProximity = enemySystem?.bases != null && enemySystem.bases.cursor > 1;
            var buildAreaSquared = buildArea * buildArea;
            _currentSiteScore = EvaluatePlannerCandidate(
                factory, player, player.position, buildAreaSquared, out _currentSiteTargetCount, out _, true);
            _currentSiteLaunchedTargetCount = _plannerLaunchedTargetCount;
            CollectPlannerCandidates(factory, player);
            if (IsFinalConstructionRun())
            {
                destination = _nearestPlannerTarget;
                return destination.sqrMagnitude >= 0.01f;
            }

            if (_plannerCandidateCount == 0)
            {
                return false;
            }

            var localRouteDistance = Mathf.Max(
                MinimumSiteMoveDistance, buildArea * LocalRouteRadiusMultiplier);
            var localRouteDistanceSquared = localRouteDistance * localRouteDistance;
            var localBestScore = float.MinValue;
            var localBestCoverageCount = 0;
            var localBestCandidate = Vector3.zero;
            var nearestCandidate = Vector3.zero;
            var nearestCandidateDistanceSquared = float.MaxValue;
            var nearestCandidateScore = float.MinValue;
            Vector3 prefixDestination;
            float prefixScore;
            for (var i = 0; i < MaxPlannerCandidates; i++)
            {
                if (PlannerCandidateDistanceSquared[i] < 0f) continue;
                var candidate = PlannerCandidates[i];
                var score = EvaluatePlannerCandidate(
                    factory, player, candidate, buildAreaSquared, out var coverageCount, out var refinedCandidate);
                if (PlannerCandidateDistanceSquared[i] <= localRouteDistanceSquared)
                {
                    ConsiderPlannerDestination(
                        candidate, score, coverageCount,
                        ref localBestScore, ref localBestCoverageCount, ref localBestCandidate);
                }
                ConsiderNearestDestination(
                    candidate, coverageCount, PlannerCandidateDistanceSquared[i], score,
                    ref nearestCandidateDistanceSquared, ref nearestCandidate, ref nearestCandidateScore);
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
                var refinementDistanceSquared = GetSurfaceDistanceSquared(factory, refinedCandidate, player.position);
                if (refinementDistanceSquared <= localRouteDistanceSquared)
                {
                    ConsiderPlannerDestination(
                        refinedCandidate, score, coverageCount,
                        ref localBestScore, ref localBestCoverageCount, ref localBestCandidate);
                }
                ConsiderNearestDestination(
                    refinedCandidate, coverageCount, refinementDistanceSquared, score,
                    ref nearestCandidateDistanceSquared, ref nearestCandidate, ref nearestCandidateScore);
            }

            if (localBestCoverageCount > 0)
            {
                destination = localBestCandidate;
                _nextSiteScore = localBestScore;
                if (TryFindRoutePrefixDestination(
                        factory, player, destination, buildArea, out prefixDestination, out prefixScore))
                {
                    destination = prefixDestination;
                    _nextSiteScore = prefixScore;
                }

                return true;
            }

            if (nearestCandidateDistanceSquared >= float.MaxValue || nearestCandidateScore <= float.MinValue) return false;

            destination = nearestCandidate;
            _nextSiteScore = nearestCandidateScore;
            if (TryFindRoutePrefixDestination(
                    factory, player, destination, buildArea, out prefixDestination, out prefixScore))
            {
                destination = prefixDestination;
                _nextSiteScore = prefixScore;
            }

            return true;
        }

        private static bool TryFindRoutePrefixDestination(
            PlanetFactory factory,
            Player player,
            Vector3 destination,
            float buildArea,
            out Vector3 prefixDestination,
            out float prefixScore)
        {
            prefixDestination = default;
            prefixScore = float.MinValue;
            var realRadius = factory.planet.realRadius;
            var originDirection = player.position.normalized;
            var targetDirection = destination.normalized;
            var routeNormal = Vector3.Cross(originDirection, targetDirection);
            if (routeNormal.sqrMagnitude < 0.000001f) return false;

            routeNormal.Normalize();
            var routeCos = Vector3.Dot(originDirection, targetDirection);
            var routeAngle = Mathf.Acos(Mathf.Clamp(routeCos, -1f, 1f));
            var routeDistance = routeAngle * realRadius;
            if (routeDistance <= MinimumSiteMoveDistance) return false;
            var minimumDistanceCos = Mathf.Cos(Mathf.Min(Mathf.PI, MinimumSiteMoveDistance / realRadius));

            // The corridor half-width is expressed as the sine of its central angle so it can be
            // compared directly against the candidate's out-of-plane component (which is also a
            // sine). Clamp the angle to 90 degrees: past that, sin() would start shrinking again
            // and collapse the corridor instead of widening it.
            var corridorWidth = Mathf.Max(16f, buildArea * RouteCorridorWidthFactor);
            var maximumCrossTrackSine = Mathf.Sin(
                Mathf.Min(Mathf.PI * 0.5f, corridorWidth / realRadius));
            var prebuilds = factory.prebuildPool;
            var bestCandidateCos = -2f;
            var found = false;
            for (var i = 1; i < factory.prebuildCursor; i++)
            {
                ref var prebuild = ref prebuilds[i];
                if (prebuild.id != i || prebuild.isDestroyed || prebuild.builderLaunched) continue;

                var candidateDirection = prebuild.pos.normalized;
                var candidateCos = Vector3.Dot(originDirection, candidateDirection);
                if (candidateCos >= minimumDistanceCos || candidateCos <= routeCos) continue;
                if (Vector3.Dot(candidateDirection, targetDirection) <= routeCos) continue;

                var crossTrackSine = Mathf.Abs(Vector3.Dot(routeNormal, candidateDirection));
                if (crossTrackSine > maximumCrossTrackSine) continue;
                if (candidateCos <= bestCandidateCos) continue;
                if (!IsPrebuildEligible(factory, player, ref prebuild)) continue;

                bestCandidateCos = candidateCos;
                prefixDestination = prebuild.pos;
                found = true;
            }

            if (!found) return false;

            prefixScore = EvaluatePlannerCandidate(
                factory, player, prefixDestination, buildArea * buildArea, out _, out _);
            return prefixScore > float.MinValue;
        }

        private static void ConsiderNearestDestination(
            Vector3 candidate,
            int coverageCount,
            float distanceSquared,
            float score,
            ref float nearestDistanceSquared,
            ref Vector3 nearestCandidate,
            ref float nearestScore)
        {
            if (coverageCount <= 0 || distanceSquared >= nearestDistanceSquared) return;

            nearestDistanceSquared = distanceSquared;
            nearestCandidate = candidate;
            nearestScore = score;
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
            EndFlightSettling();
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
                if (!IsPrebuildEligible(factory, player, ref prebuild)) continue;

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

        /// <summary>
        /// Great-circle distance squared between two surface positions on the current planet.
        /// </summary>
        private static float GetSurfaceDistanceSquared(PlanetFactory factory, Vector3 first, Vector3 second)
        {
            var radius = factory.planet.realRadius;
            var angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(first.normalized, second.normalized), -1f, 1f));
            var distance = angle * radius;
            return distance * distance;
        }

        private static void InitializePlannerFan(Player player)
        {
            // Prebuild and player positions are planet-local, so the fan axes must be planet-local
            // too. player.uRotation is a universal rotation and has to be brought back into the
            // planet frame before it can be used as a heading reference.
            var radial = player.position.normalized;
            var planet = player.planetData;
            var heading = player.uRotation * Vector3.forward;
            if (planet != null)
            {
                heading = Maths.QInvRotate(planet.runtimeRotation, heading);
            }

            var forward = Vector3.ProjectOnPlane(heading, radial);
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

            if (PlannerCandidateDistanceSquared[nearIndex] < 0f)
            {
                _plannerCandidateCount++;
                PlannerCandidates[nearIndex] = position;
                PlannerCandidateDistanceSquared[nearIndex] = distanceSquared;
                return;
            }

            if (distanceSquared >= PlannerCandidateDistanceSquared[nearIndex])
            {
                TryStorePlannerFarCandidate(farIndex, nearIndex, position, distanceSquared);
                return;
            }

            // Demote the previous near representative instead of dropping it, so a sector keeps a
            // near/far pair regardless of the order the prebuild pool happens to be scanned in.
            var demoted = PlannerCandidates[nearIndex];
            var demotedDistanceSquared = PlannerCandidateDistanceSquared[nearIndex];
            PlannerCandidates[nearIndex] = position;
            PlannerCandidateDistanceSquared[nearIndex] = distanceSquared;
            TryStorePlannerFarCandidate(farIndex, nearIndex, demoted, demotedDistanceSquared);
        }

        private static void TryStorePlannerFarCandidate(
            int farIndex,
            int nearIndex,
            Vector3 position,
            float distanceSquared)
        {
            // Separation has to be measured as an actual distance between the two representatives.
            // Comparing a difference of squared distances against a squared merge distance is not
            // dimensionally valid and lets any slightly farther ghost claim the far slot.
            var mergeDistanceSquared = CandidateMergeDistance * CandidateMergeDistance;
            if ((position - PlannerCandidates[nearIndex]).sqrMagnitude <= mergeDistanceSquared) return;

            if (PlannerCandidateDistanceSquared[farIndex] < 0f)
            {
                _plannerCandidateCount++;
                PlannerCandidates[farIndex] = position;
                PlannerCandidateDistanceSquared[farIndex] = distanceSquared;
                return;
            }

            if (distanceSquared <= PlannerCandidateDistanceSquared[farIndex]) return;
            PlannerCandidates[farIndex] = position;
            PlannerCandidateDistanceSquared[farIndex] = distanceSquared;
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
            _plannerLaunchedTargetCount = 0;
            _plannerTargetCount = 0;
            _plannerTargetDistanceSum = 0f;
            _plannerTargetDirectionSum = Vector3.zero;
            var inspectedCount = 0;
            var reachedScanLimit = false;
            var prebuilds = factory.prebuildPool;
            var hashSystem = factory.hashSystemStatic;
            var hashPool = hashSystem.hashPool;
            var bucketOffsets = hashSystem.bucketOffsets;
            var realRadius = factory.planet.realRadius;
            var planningCenter = candidate.normalized * (realRadius + ConstructionFlightAltitude);
            if (currentSite)
            {
                planningCenter = player.position;
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
                    if (prebuild.id != prebuildId || prebuild.isDestroyed) continue;
                    var distanceSquared = (prebuild.pos - planningCenter).sqrMagnitude;
                    if (distanceSquared > buildAreaSquared) continue;
                    if (prebuild.builderLaunched)
                    {
                        _plannerLaunchedTargetCount++;
                        continue;
                    }
                    if (!IsPrebuildEligible(factory, player, ref prebuild)) continue;

                    if (coverageCount < MaxOverflowBuildTargets)
                    {
                        coverageCount++;
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
            var productiveTargetCount = Mathf.Max(1f, coverageCount);

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

        /// <summary>
        /// Picks the scanned ghost whose surface direction is closest to the centre of the scanned
        /// group. Callers must ensure <c>_plannerTargetCount &gt; 0</c> and that the direction sum is
        /// not degenerate.
        /// </summary>
        private static Vector3 GetPlannerRepresentativeTarget()
        {
            var centerDirection = _plannerTargetDirectionSum.normalized;
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

        /// <summary>
        /// Returns true when the game would eventually launch a construction drone for this ghost.
        /// Mirrors the material-delivery filter in <c>ConstructionModuleComponent.GameTick</c>: a
        /// ghost the game will never hand items to can never clear <c>itemRequired</c>, and routing
        /// to it would park the mecha on a site that makes no progress.
        /// </summary>
        private static bool IsPrebuildEligible(PlanetFactory factory, Player player, ref PrebuildData prebuild)
        {
            if (prebuild.itemRequired <= 0) return true;
            if (!HasRequiredItems(player, prebuild.protoId, prebuild.itemRequired)) return false;
            if (!ItemProto.constructableIdHash.Contains(prebuild.protoId)) return false;
            return !_checkEnemyProximity || !factory.HasEnemyBuildingNear(-prebuild.id);
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

        private static bool ShouldStayAtConstructionSite(PlanetFactory factory, Player player, long timei)
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

            if (_currentSiteTargetCount <= 0) return false;

            if (_currentSiteLaunchedTargetCount >= GetConstructionWaveCapacity(factory, player)) return false;

            return _nextSiteScore < _currentSiteScore * SiteSwitchGain;
        }

        private static int GetConstructionWaveCapacity(PlanetFactory factory, Player player)
        {
            var constructionModule = player.mecha.constructionModule;
            var targetsPerDrone = Mathf.Clamp(
                factory.gameData.history.constructionDroneMovement, 1, 4);
            return Mathf.Clamp(
                constructionModule.droneCount * targetsPerDrone, 1, MaxImmediateBuildTargets);
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
            _flightSettlePending = true;
            _flightSettleTicks = Mathf.Max(_flightSettleTicks, FlightSettleDurationTicks);
            _flightSettleWaitTicks = 0;
        }

        private static void EndFlightSettling()
        {
            _flightSettlePending = false;
            _flightSettleTicks = 0;
            _flightSettleWaitTicks = 0;
        }

        /// <summary>
        /// Returns true once the mecha has actually stopped sliding, so the next route starts from a
        /// near-standstill instead of fighting the previous heading.
        /// </summary>
        /// <remarks>
        /// The fixed delay only covers the latency between clearing the orders and
        /// <c>PlayerMove_Fly</c> decaying <c>rtsVelocity</c>. The residual-speed check must stay
        /// gated on <see cref="_flightSettlePending"/> rather than on the countdown itself,
        /// otherwise it becomes unreachable as soon as the countdown hits zero. A timeout keeps an
        /// unexpected steady state (for example a gas giant hover) from stalling the planner.
        /// </remarks>
        private static bool UpdateFlightSettling(Player player)
        {
            if (_flightSettleTicks > 0)
            {
                _flightSettleTicks -= (int)AutoConstructTickInterval;
                return false;
            }

            _flightSettleWaitTicks += (int)AutoConstructTickInterval;
            var radial = player.position.sqrMagnitude > 0.01f ? player.position.normalized : Vector3.up;
            var tangentialSpeed = Vector3.ProjectOnPlane(player.controller.velocity, radial).magnitude;
            var rtsSpeed = player.controller.actionFly.rtsVelocity.magnitude;
            if ((rtsSpeed <= FlightSettleRtsSpeed && tangentialSpeed <= FlightSettleTangentialSpeed) ||
                _flightSettleWaitTicks >= FlightSettleTimeoutTicks)
            {
                EndFlightSettling();
                return true;
            }

            return false;
        }

        private static void QueueRouteAfterFlightSettle(Vector3 target, bool raiseAltitude)
        {
            _deferredRoute = target;
            _deferredRouteKeepAltitude = raiseAltitude;
            _hasDeferredRoute = true;
        }

        private static void IssueRecoveryRoute(Player player, Vector3 target)
        {
            // Recovery must take over before the mecha comes to a stop, so it deliberately skips
            // the flight-settling wait that a normal route change goes through.
            player.controller.actionFly.targetAltitude = AvoidanceAltitude;
            EndFlightSettling();
            _deferredRoute = Vector3.zero;
            _hasDeferredRoute = false;
            _deferredRouteKeepAltitude = false;
            IssueRoute(player, target, true, true);
        }

        private static void MaintainAvoidanceAltitude(Player player)
        {
            if (!_isAvoidingObstacle || player.movementState != EMovementState.Fly) return;

            player.controller.actionFly.targetAltitude = AvoidanceAltitude;
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
            // Clearing the orders is what actually brakes: PlayerMove_Fly decays rtsVelocity toward
            // zero whenever there is no unreached order. An explicit OrderNode.Stop would be
            // discarded by the following Clear() before PlayerOrder.GameTick could ever process it,
            // and would only leave a stray order gizmo at the planet centre.
            player.ClearOrders();
            _autoOrder = null;
            _stuckTicks = 0;
            _orbitTicks = 0;
            BeginFlightSettling();
        }

        private static void ClearAutoRoute(Player player)
        {
            if (player != null && _autoOrder != null && player.orders.currentOrder == _autoOrder)
            {
                player.ClearOrders();
            }

            _autoOrder = null;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
        }

        /// <param name="raiseAltitude">
        /// Lifts the mecha to <see cref="AvoidanceAltitude"/> for the duration of the route, used
        /// for detours and recoveries that have to clear an obstacle.
        /// </param>
        private static void IssueRoute(
            Player player,
            Vector3 targetPosition,
            bool raiseAltitude,
            bool bypassBrake = false)
        {
            var direction = targetPosition - player.position;
            if (direction.sqrMagnitude < 0.01f) return;

            if (!bypassBrake && ShouldBrakeBeforeRoute(player, targetPosition))
            {
                CancelAutoMotion(player);
                QueueRouteAfterFlightSettle(targetPosition, raiseAltitude);
                return;
            }

            _autoOrder = OrderNode.MoveTo(targetPosition);
            player.Order(_autoOrder, false);
            _bestRouteAngle = Vector3.Angle(player.position.normalized, targetPosition.normalized);
            _orbitTicks = 0;
            if (raiseAltitude && player.movementState == EMovementState.Fly)
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
            var obstacleBounds = new Bounds();
            var hasObstacle = false;
            var overlapCount = Physics.OverlapSphereNonAlloc(
                origin, AvoidanceClearance + 0.5f, ObstacleOverlapColliders, ObstacleLayerMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < overlapCount; i++)
            {
                var overlapCollider = ObstacleOverlapColliders[i];
                if (overlapCollider == null) continue;

                obstacleBounds = overlapCollider.bounds;
                hasObstacle = true;
                break;
            }

            if (!hasObstacle &&
                Physics.SphereCast(origin, AvoidanceClearance, path.normalized, out var hit, path.magnitude, ObstacleLayerMask, QueryTriggerInteraction.Collide) &&
                hit.distance + 4f < path.magnitude)
            {
                obstacleBounds = hit.collider != null ? hit.collider.bounds : new Bounds(hit.point, Vector3.one * 2f);
                hasObstacle = true;
            }

            if (!hasObstacle) return false;

            var anchor = obstacleBounds.center.normalized;
            var forward = Vector3.ProjectOnPlane(finalDirection, anchor).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.ProjectOnPlane(player.position, anchor).normalized;
            }

            var side = Vector3.Cross(anchor, forward).normalized;
            if (side.sqrMagnitude < 0.01f) return false;

            var footprintRadius = GetObstacleFootprintRadius(obstacleBounds, anchor);
            var fallbackDetour = default(Vector3);
            var fallbackWaypoint = default(Vector3);
            var fallbackClearCount = -1;
            var optionCount = DetourRadiusScales.Length * DetourSideSigns.Length;
            for (var optionIndex = 0; optionIndex < optionCount; optionIndex++)
            {
                var scaleIndex = optionIndex / DetourSideSigns.Length;
                var sideIndex = optionIndex % DetourSideSigns.Length;
                var routeOffset = footprintRadius * DetourRadiusScales[scaleIndex];
                var lateralOffset = routeOffset * DetourSideSigns[sideIndex];
                var forwardOffset = routeOffset;
                var entry = SurfaceOffset(anchor, side * lateralOffset - forward * forwardOffset, planet.realRadius);
                var exit = SurfaceOffset(anchor, side * lateralOffset + forward * forwardOffset, planet.realRadius);
                var entryClear = IsPathClear(player, entry, altitude);
                var exitClear = IsPathClear(entry, exit, altitude, planet.realRadius);
                var finalClear = IsPathClear(exit, finalDirection, altitude, planet.realRadius);
                if (entryClear && exitClear && finalClear)
                {
                    detour = entry * planet.realRadius;
                    pendingWaypoint = exit * planet.realRadius;
                    return true;
                }

                var clearCount = (entryClear ? 1 : 0) + (exitClear ? 1 : 0) + (finalClear ? 1 : 0);
                if (clearCount > fallbackClearCount)
                {
                    fallbackClearCount = clearCount;
                    fallbackDetour = entry * planet.realRadius;
                    fallbackWaypoint = exit * planet.realRadius;
                }

                var candidate = SurfaceOffset(anchor, side * lateralOffset, planet.realRadius);
                var candidateEntryClear = IsPathClear(player, candidate, altitude);
                var candidateFinalClear = IsPathClear(candidate, finalDirection, altitude, planet.realRadius);
                if (candidateEntryClear && candidateFinalClear)
                {
                    detour = candidate * planet.realRadius;
                    pendingWaypoint = default;
                    return true;
                }

                var candidateClearCount = (candidateEntryClear ? 1 : 0) + (candidateFinalClear ? 1 : 0);
                if (candidateClearCount > fallbackClearCount)
                {
                    fallbackClearCount = candidateClearCount;
                    fallbackDetour = candidate * planet.realRadius;
                    fallbackWaypoint = default;
                }
            }

            if (fallbackClearCount < 0) return false;

            detour = fallbackDetour;
            pendingWaypoint = fallbackWaypoint;
            return true;
        }

        private static float GetObstacleFootprintRadius(Bounds bounds, Vector3 anchor)
        {
            var maximumRadius = 0f;
            for (var cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                var corner = new Vector3(
                    (cornerIndex & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (cornerIndex & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (cornerIndex & 4) == 0 ? bounds.min.z : bounds.max.z);
                var cornerOffset = Vector3.ProjectOnPlane(corner - bounds.center, anchor);
                maximumRadius = Mathf.Max(maximumRadius, cornerOffset.magnitude);
            }

            return Mathf.Clamp(maximumRadius + AvoidanceClearance + 2f, 5f, 64f);
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

            return !Physics.SphereCast(origin, AvoidanceClearance, path.normalized, out var hit, path.magnitude, ObstacleLayerMask, QueryTriggerInteraction.Collide) ||
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
            EndFlightSettling();
            _deferredRoute = Vector3.zero;
            _hasDeferredRoute = false;
            _deferredRouteKeepAltitude = false;
            _planAstroId = 0;
            _lastPlanAttemptTick = 0;
            _hasPlanAttempted = false;
            ResetPlannerScan();
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
