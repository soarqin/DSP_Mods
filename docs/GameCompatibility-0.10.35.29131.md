# DSP 0.10.35.29131 Compatibility

## Scope and sources

The 2026-10-12 audit compares the supplied DSP **0.10.35.29104** backup with the installed **0.10.35.29131**, Steam build **25878145**. It covers the maintained mods in `DSP_Mods.sln`, including Dustbin's preloader. The retired LabOpt projects are excluded.

Method bodies and metadata come from the original `DSPGAME_Data/Managed/Assembly-CSharp.dll` in:

- Previous game: `D:/Steam/steamapps/content/app_13466540/depot_1366541`.
- Current game: `D:/Steam/steamapps/common/Dyson Sphere Program`, located through Steam's registry entry and `steamapps/libraryfolders.vdf`.

Versions come from each installation's `Updates/Versions.txt` and the installed `appmanifest_1366540.acf`. `AssemblyFromGame/Assembly-CSharp.dll` is refreshed only for compilation.

## Findings and fixes

The native assembly has 67 changed method bodies, 11 added fields, 3 removed fields, and two replaced method signatures. No types are added or removed. Eight changed methods are directly targeted by maintained mod patches:

| Native methods | Change relevant to review | Result |
| --- | --- | --- |
| `BuildTool_Reform.ExecuteBrushAction` | Terrain previews cache their preparation position and soil cost; soil cost moves from a local to a field. | Expanded brush and build-range patches still match. Flatten/restore execution signatures and soil guards remain valid. |
| `BuildTool_BlueprintPaste.DetermineReforms` | A terrain calculation now divides floating-point operands. | The blueprint soil-check patch still matches the intended comparison. |
| `BuildTool_Upgrade.DeterminePreviews` | Downgrade cursor text is distinguished from upgrade text. | Cursor-size and build-range patches retain their targets. |
| `MonitorComponent.InternalUpdate` | Cargo deletion scans a locked belt buffer and adds locals and exception handling. | The stack-count patch still targets the earlier cargo query and its `Cargo` local; branch targets remain valid. |
| `PlayerAction_Rts.GameTick` | Manual order cancellation changes for mining/building commands. | Globe-view conflict checks and auto-construct postfix bindings remain valid. |
| `ProductionExtraInfoCalculator.CalculateFactory`, `UIReferenceSpeedTip.AddEntryDataWithFactory` | Ray Receiver catalyst storage and active-catalyst identification are corrected. | Mining-speed and protected-vein patches still target the mining calculations. |
| `UIBuildMenu._OnUpdate` | Native vegetation-planting buttons are hidden. | The modifier-key patch still matches the native input checks. |

The audit also reproduces an existing UXAssist defect on **both** game versions: the Ray Receiver buffer setting is ignored. Native `GameTick_Gamma` tests `keyFrame && useIon && catalystCount < 2`, while the mod searches for a floating-point conversion followed by `20f`. The pattern is absent, so the guarded transpiler returns the original method and the configured threshold never takes effect.

The fix replaces the integer constant in the current catalyst-count comparison with `ReceiverBufferCount`. Only that constant changes. Native key-frame and mode checks, catalyst consumption, proliferation, both input ports, photon output, and branch destinations are preserved. The missing-pattern fallback still returns the original instructions. Native input can take stacked cargo, so the setting is an intake threshold rather than a strict inventory cap.

The bilingual help text and README now show the native threshold as **2**. UXAssist's existing **1.6.2** unreleased changelog includes the fix; mod versions are unchanged.

A subsequent mining-speed regression check finds an existing type error in `Increase maximum power usage in Logistic Stations and Advanced Mining Machines`. A floating-point slider constant makes the injected conversion delegate return `Single`, but native `OnMaxMiningSpeedChange` stores that result in an `Int32` local and then in `MinerComponent.speed`. This invalid IL corrupts the actual speed, rather than only its displayed percentage. A check using the original handler and the pre-fix package DLL reproduces invalid integer values; the exact result of the invalid IL depends on the runtime.

The conversion now explicitly returns `Int32`, preserves native rounding, and retains 100%–300% in 10-percentage-point steps and 400%–1,000% in 100-percentage-point steps. Its transpiler matches the complete native calculation without depending on a fixed local slot and returns the original instructions when that calculation is absent. For miners whose speed has already been corrupted, adjust the slider after installing the fix; the previous intended setting cannot be recovered reliably from the corrupted value.

LiveStreamAssist's offline text generator also fails on both game versions because it asks the type-tree generator for the removed `CreationPartProtoSet`; the original DLL and asset script metadata contain `VehiclePartProtoSet`. Its extraction map now uses the current type while preserving the public `creationPart` dictionary group. Text and icons are regenerated from the current installation, including item 1211, technology 1509, recipe 162, signal 519, updated English guide text, and current localized vegetation names. The offline layers retain their existing behavior and make no WebSocket requests.

## Validation

- `dotnet build DSP_Mods.sln -c Release --no-restore`: zero warnings and zero errors.
- 217 prefix/postfix target and parameter bindings pass, including Dustbin's injected fields.
- 212 transpiler applications: 200 transformations and 12 unchanged results. No target-resolution, transpiler-execution, branch-label, or missing-operand errors occur. The 12 unchanged results also occur with the previous game DLL.
- Dustbin's actual preloader injects the two expected Boolean fields into a temporary copy of the original game assembly; its mod checks pass against that copy.
- The receiver regression check fails before the fix because configuration value 1 leaves the native threshold at 2. After the fix, 21 assertions pass on each game version, covering thresholds 1, 2, 7, and 20, below/equal/above boundaries, emitted comparison types, preservation of surrounding instructions, and missing-pattern fallback.
- 55 existing behavior assertions pass, including terrain buffer bounds, soil guards, saved brush restoration, and composition of both `ExecuteBrushAction` transpilers in either order.
- 75 mining-speed assertions pass. The check executes the original handler with the injected conversion against real game data types, replacing only the Unity text setter with a recording function. It verifies actual miner-speed assignments and displayed percentages, every slider position, rounding boundaries, reopening readback, native event/station guards, the unpatched path, and complete/partial missing-pattern fallbacks. This check covers the return/local type mismatch that the earlier structural transpiler checks did not detect.
- LiveStreamAssist's text and icon generators succeed against the current original game files; the dictionary includes 175 items and 315 technologies. Repacking the JS bundle produces no source changes.
- 42 JS client/query/text/overlay checks and the Python locale/format checks pass. The full JS suite also includes seven pre-existing dashboard failures: its `FakeQuery` omits `getClusterString`, which the unchanged dashboard calls. The same seven failures reproduce in an isolated copy of the files from `HEAD`. These tests use their own mocked text data, independent of the regenerated dictionary.
- `git diff --check` passes.

The temporary audit harness and raw comparison outputs remain under the excluded `UXAssist/obj/game-update-29131/` directory. `MONOMOD_DMD_TYPE=cecil` is set only for the check process so the repository's older MonoMod delegate generator can run on the installed .NET runtime. No detours are installed into the game.

These checks do not verify Unity rendering, live input, or native detour installation. In-game testing remains necessary: exercise expanded foundation/Restore Terrain brushes, monitor stack counting, globe-view movement and auto-construct, protected-vein estimates, and Ray Receiver intake with the buffer feature enabled and disabled.
