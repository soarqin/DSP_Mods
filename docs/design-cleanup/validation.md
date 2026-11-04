# Validation receipt

Date: 2026-11-04. Baseline and final fetched `origin/master`: `64a23e9772e7878f216cc2e281da2bc3f87d3140`.

## Builds and checks

| Command/check | Result |
| --- | --- |
| `dotnet restore DSP_Mods.sln` | Passed using the existing dependency graph. |
| `dotnet build DSP_Mods.sln -c Release --no-restore` | Passed after final edits: 0 warnings, 0 errors; all 13 solution projects including UpdateGameDlls and DustbinPreloader. |
| `dotnet build LogisticMiner/LogisticMiner.csproj -c Release --no-restore` | Passed: 0 warnings, 0 errors; project is not listed in solution. |
| `dotnet run --project UXAssist/tools/ProductionCheck/ProductionCheck.csproj -c Release` | 569 checks passed; production worker also ran them before and after its edits. |
| PoolOpt before/after managed comparison | 16,382 cases passed: every occupancy mask through cursor 13 for ID-based and trash-sentinel pools; return value, cursor, recycle cursor/array and reset count match. Temporary project stays in excluded `PoolOpt/obj/design-cleanup-check`; source is copied to delivery artifacts. |
| `powershell.exe -NoProfile -File tools/tests/Test-QuickMinerPlacement.ps1` | 6,620 checks passed, including captured replay roundtrip and sequential/parallel output checks. Placement algorithms are unchanged. |
| ReflectionReaderCheck Release runner | 40 checks passed, final OK; runtime boundary and allowlist cases included. |
| WebSocketTransportCheck Release runner | Echo/fragmentation check passed, final OK. |
| WebSocketTransportCheck `--regression` | Production transport, dispatcher, bounds, session, refresh, shutdown and failure regressions passed, final OK. Expected timeout/occupied-port logs are deliberate cases. |
| `python LiveStreamAssist/js/scripts/pack_js.py` | Packed output matches existing tracked sources; no frontend content changes. |
| Documented five-suite `node --test` command | 43 passed, 7 failed. All seven failures are independently reproduced from exact baseline files; see below. |
| `python LiveStreamAssist/js/scripts/test_text.py` | Passed, including installed locale checks. |
| `powershell.exe -NoProfile -File tools/tests/Test-PublishMod.ps1` | 318 assertions passed with synthetic credentials; no real upload/remote lookup. |
| WebSocket acceptance-script focused check | Parser and three synthetic connection/null/disposal cases passed. Running DSP acceptance was not executed. |
| `git diff --check` | Passed after final source edits and again for staged delivery. |

Existing warning policy is unchanged. Steam registry resolved `d:/steam`, `libraryfolders.vdf` listed the DSP library, and the original DLL exists at `D:/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll`. Normal UpdateGameDlls publicizing/content comparison reported both compile references current. Original game method-body analysis was unnecessary for final changes: no native method semantics, active IL injection pattern or patch target was changed. No reference DLL or decompilation was used as a substitute for an original method body.

## Baseline dashboard failure

`live-dashboard.html` calls the real `GameQuery.getClusterString`. The existing dashboard suite's FakeQuery lacks that method, so seven cases fail with unchanged placeholder values or `query.getClusterString is not a function`. The initialization-error case still passes. Copied exact `git show 64a23e9:<path>` frontend/example files into excluded `LiveStreamAssist/obj/design-cleanup-baseline` and ran the same dashboard suite: the same seven named tests fail for the same reason. No frontend, generated-data or dashboard-test content is changed in this PR. This unrelated existing fixture defect is reported rather than mixed into design cleanup.

## Limits

No interactive DSP/Unity/OBS/LAN session, saved-game mutation, Thunderstore publication or native mod-compatibility acceptance was performed. The retired LabOpt pair was inventoried read-only and not built or revived. Publicized compile references were used for compilation only. Existing native patch correctness outside the unchanged contracts is not certified by this audit. No dependency advisories or external API changes are claimed as part of this behavior-preserving design audit.
