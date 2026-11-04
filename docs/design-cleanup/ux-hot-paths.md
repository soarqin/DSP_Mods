# UXAssist construction and movement audit

Baseline: `64a23e9772e7878f216cc2e281da2bc3f87d3140` (`origin/master`, 2026-11-04).

## Coverage

Supplemental structural review of all seven requested hot-path files (5,861 lines at baseline):

| File | Baseline lines | Reviewed responsibilities |
| --- | ---: | --- |
| `Patches/Factory/BuildToolPatch.cs` | 942 | Always-on patch owner, off-grid geometry/text, stacking, drag-build poles, tank transfer, whole-belt transfer |
| `Patches/Factory/FactoryBuildPatches.cs` | 1,786 | Auto-construct planner, material eligibility cache, bounded candidate scans, construction order ownership, flight settling, recovery detours, lab stack operations |
| `Patches/PlayerPatch.cs` | 1,235 | Feature activation, starmap names, native presentation patches, auto-cruise target resolution, navigation lifecycle, local/sailing movement, obstacle avoidance, warp and approach controls |
| `Functions/Factory/QuickMinerBuild.cs` | 1,084 | Job lifecycle, main-thread capture, candidate generation, coroutine/worker scheduling, current-state validation, native placement commit and rollback, diagnostics |
| `Functions/Factory/MinerPlacementSolver.cs` | 552 | Deterministic greedy starts, stable pose keys, coordinated repacking, seed collision reuse, incremental burial/coverage accounting, cancellation and bounded worker count |
| `Functions/Factory/MinerPlacementGeometry.cs` | 124 | Individual oriented colliders, bounding rejection, separating-axis tests and exact segment/box distance |
| `Functions/Factory/MinerPlacementDebug.cs` | 138 | Debug-only serializable replay records and shared capture/restore serializer |

Traced references to the three miner modules throughout game sources and `tools/tests/QuickMinerPlacementTests.cs` / `Test-QuickMinerPlacement.ps1`, and feature activation in `FactoryPatch`, `PlayerPatch` and the auto-construct/auto-cruise UI owners. No new general-purpose abstraction was introduced.

## Changes

- Delete the entirely block-commented `PatchToPerformSteppedRotate` / `RotateStepped` experiment and its commented `SteppedRotationDegrees` declaration from `BuildToolPatch.cs` (58 lines). It has no compiled method, active Harmony attribute, config entry or caller. Git history already retains the discarded experiment.
- Delete the entirely block-commented `UIStarmapPlanet__OnLateUpdate_Transpiler` / `UIStarmapDFHive__OnLateUpdate_Transpiler` experiment from `PlayerPatch.cs` (104 lines). It references the stale `_showAllStarsNameStatus` spelling and has no compiled patch or runtime use.

Both changes remove comment text only; they do not remove or modify a compiled method, Harmony target, IL matcher, navigation state, option, geometry rule or placement algorithm. The original UTF-8 BOM on `PlayerPatch.cs` is preserved.

## Candidates deliberately retained

- **Miner geometry and solver files:** substantive, independent algorithm boundaries shared by in-game placement and the existing offline linked-source regression runner. The geometry class implements complete collision mathematics, and the solver coordinates searches over captured immutable candidate data; neither is a few-line one-use helper.
- **`MinerPlacementDebug.cs`:** the runtime Debug path and offline replay checks share these records and serializer. Serialization field names and schema are actual persisted replay contracts. Release excludes this file through the project, avoiding a release dependency on diagnostic serialization. Folding records into the Unity/game-facing owner would destroy the current game-free replay boundary.
- **Candidate arrays and probe cache:** `QuickMinerBuild` reuses build boxes, nearby colliders and covered/reachable lists across candidate attempts, cloning only the captured arrays retained by a candidate. The probe avoids repeated spatial work across yaw angles. Removing this derived state would increase hot-path work or let later probes mutate prior candidates.
- **Solver collision caches and separate score counters:** verified-prefix arrays and seed blockers avoid repeated separating-axis tests. `RawScore`, burial counts, usage counts and actual score encode different quantities for pruning and reversible layout updates. Treating them as duplicate state would change either output or performance.
- **Coroutine and worker paths:** both are required: the worker count becomes zero on a single-core machine, while background workers operate only on captured geometry. Main-thread yield points bound frame work; cancellation ownership waits for worker completion before disposing resources.
- **Auto-construct fixed arrays and eligibility cache:** capacities explicitly bound planning work and allocations. Current-site, proposed-site, launched-target and refinement statistics are distinct decisions, and per-pass reset prevents inventory data leaking between plans. No replacement collection framework is warranted.
- **Arrival, settling, deferred-route and recovery state:** these describe different temporal stages. The actual native order is retained separately so disabling or changing planets clears only the planner-owned order. Recovery history, pending second waypoints and failed-route retries preserve progress and altitude. Simplifying them solely by field count would alter movement behavior.
- **Auto-cruise target snapshot and obstacle list:** resolution unifies planet/star/hive/carrier identity and changing world positions; the reusable list is cleared each calculation/session. It is not a cache of permanent world state. NavigationTarget and Obstacle records already live within the owning source file.
- **Private tiny movement/collision helpers:** local mathematical operations are reused or name a necessary calculation within the feature owner. They do not create cross-file abstractions. Parameter-rich calls pass value types without introducing request/config objects or hot-loop allocation.
- **Native replacement signatures:** `PlanetGridSnapDotsNonAllocNotAligned` / `PlanetAuxDataSnapDotsNonAllocNotAligned` retain apparently unused native `yaw`/`gap` arguments. The injected replacement call must consume the exact native operand stack; removing arguments for cosmetic simplicity would break IL stack behavior.
- **Harmony callbacks with no ordinary C# caller:** preserved. Attributes, emitted delegates and `AccessTools` member lookup are real uses. Patch group separation preserves configuration-controlled activation and does not introduce extra lifecycle ownership.
- **Similar preview/transfer loops:** different traversal, refund, target and native-stack contracts prevent mechanical deduplication. No new wrapper or factory was added to hide those distinctions.

## Validation and limits

- Scoped `git diff --check` passed after deleting the two inactive experiments.
- These edits change only comments. Runtime patch application/target checks and affected Release builds are coordinated by the root audit and recorded in its final validation report.
- This review does not claim native method-body equivalence from the publicized DLL or manual in-game execution. No algorithm improvement was attempted, so no replay inputs or expected placement output were changed.
