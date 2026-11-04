# Test tools and retired-code coverage

Baseline: `64a23e9772e7878f216cc2e281da2bc3f87d3140` (`origin/master`, 2026-11-04).

## First-party check tools

Reviewed all tracked executable sources and project definitions in these requested partitions. Generated `obj`/`bin` outputs are excluded; the tracked lock file and runtime config are retained dependency/runtime inputs.

| Partition | Files | Design decision |
| --- | --- | --- |
| Reflection reader executable | `LiveStreamAssist/tools/ReflectionReaderCheck/{Program.cs,ReflectionReaderCheck.csproj,Copy-GameFacades.ps1}` | Retain: its fixtures explicitly exercise inherited private fields, denied getters, runtime type/assembly boundaries, collections, cycles, integers and non-finite values. The game facade copy script is reused by both executable checks. |
| Transport and production executable checks | `LiveStreamAssist/tools/WebSocketTransportCheck/{Program.cs,ProductionChecks.cs,ProductionStatisticsChecks.cs,WebSocketTransportCheck.csproj,App.config,packages.lock.json}` | Retain: the echo host checks the actual desktop transport; regression mode exercises production server/dispatcher lifetime. Partial files split a 533-line production regression group from the statistics-specific scenarios; they introduce no runtime abstraction or forwarding type. |
| Running API acceptance script | `LiveStreamAssist/tools/Test-WebSocketApi.ps1` | Simplify its speculative connection wrapper; retain socket lifetime, timeout, fragmented-message and response assertion helpers, which have multiple real uses. |
| Browser checks | `LiveStreamAssist/js/scripts/{test_helpers.js,test_client.js,test_query.js,test_text.js,test_overlays.js,test_dashboard.js}` | Retain: `test_helpers.js` is shared by all five suites and supplies deterministic timers/WebSockets/VM loading. Overlay and dashboard DOM fixtures intentionally model different UI needs; merging them would create an unnecessary DOM abstraction. |
| Python checks | `LiveStreamAssist/js/scripts/test_text.py` | Retain standalone assertions against generators and locale parsing; test fixtures are local and use standard-library temporary directories/mocks. |
| Repository check scripts | `tools/tests/{Test-PublishMod.ps1,Test-QuickMinerPlacement.ps1,QuickMinerPlacementTests.cs}` | Retain: publication fixtures isolate synthetic credentials/network/package outputs; miner checks link production source and original DLLs into an excluded `obj` test executable with independent geometry oracles and exact sequential/parallel layout checks. |

The C# transport fake socket/context/probe are nested in the checks that own them. The CLR facade exception and binding redirects distinguish desktop Kestrel from Unity's Mono facade dependencies; consolidating the projects would erase that useful runtime boundary. The publication wrapper's argument flexibility is exercised by build, restore, metadata, tool-version and upload simulations, so it is not speculative generalization.

## Applied simplification

`Get-ConnSocket` in `Test-WebSocketApi.ps1` supported hashtables and arbitrary property-bearing objects. All non-null connection values in this standalone script come from `Connect-Api`, which returns exactly `[pscustomobject]@{ Ws = $ws }`. There is no export/module entry, alternate constructor, or external caller. Its four consumers now read `.Ws` directly. `Close-Api` retains an explicit null-connection guard and its existing null-socket guard. This removes a standalone forwarding/shape-decoding function without changing valid inputs, close/dispose behavior, exception handling or deadlines.

Focused validation: PowerShell parser plus three synthetic cleanup cases passed in `LiveStreamAssist/obj/design-cleanup-websocket-check.ps1`: null connection, known connection with closed mock socket (disposed once), and known connection with null socket. No network or game was used. Scoped `git diff --check -- LiveStreamAssist/tools/Test-WebSocketApi.ps1` passed. The real `-RequireGame` acceptance path still requires a running DSP save and was not executed by this audit.

The root verification also found a pre-existing dashboard fixture failure: its `FakeQuery` lacks `getClusterString`, while the dashboard invokes that real query method. Root owns baseline reproduction and the final test receipt. This test source was not changed as part of a design-only cleanup.

## Retired modules

`LabOpt` and `LabOptPreloader` are covered by read-only structure inspection, not maintenance. The repository README explicitly marks LabOpt obsolete, and the persistent project guidance excludes it from routine mod maintenance. Inspected their project/README/entry/preloader structure and the lab-function boundary. `LabOpt.cs` owns Harmony hooks; `Functions.cs` owns the actual stacked-lab operations; the separate preloader injects `LabComponent.rootLabId` through Mono.Cecil. These are meaningful compilation/runtime boundaries, not one-use few-line helpers.

No retired source, package or version was edited or built. This entry does not certify retired native method compatibility or audit the full lab algorithm for gameplay correctness.
