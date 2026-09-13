# Agent Guidelines

## Rules

- Update `AGENTS.md` after completing every task.
- Do not record a changelog in `AGENTS.md`; modify the document content directly instead.
- All documentation and code comments must be written in English.
- When you need to inspect game method implementations, decompile the **original** game DLL rather than the publicized copy in `AssemblyFromGame/` (which was built with `--strip` and has all method bodies removed). Locate the original DLL using the same logic as `UpdateGameDlls.ps1`: read the Steam installation path from the Windows registry (`HKCU\Software\Valve\Steam`), parse `steamapps/libraryfolders.vdf` to find the library containing DSP (AppID `1366540`), then decompile `<game_root>/DSPGAME_Data/Managed/Assembly-CSharp.dll` directly.
- When looking up in-game terminology, use the localization files under `<game_root>/Locale/` (located via the same Steam registry + `libraryfolders.vdf` method). The `Names` directory contains the dictionary keys; `1033` is the English translation directory; `2052` is the Simplified Chinese translation directory.

## Project Overview

This repository is a collection of **BepInEx mods** for the game **Dyson Sphere Program (DSP)**, a factory/automation game on Steam. Each subdirectory is an independent mod plugin loaded by the BepInEx framework at game startup. Mods use **HarmonyLib** to patch the game's compiled C# methods at runtime (prefix, postfix, and transpiler patches).

## Tech Stack

- **Language:** C# (`net472` / `netstandard2.1`, latest LangVersion)
- **Modding Framework:** BepInEx 5.x
- **Patching Library:** HarmonyLib (runtime IL patching via `[HarmonyPatch]` attributes)
- **Build System:** Visual Studio solution (`DSP_Mods.sln`), SDK-style `.csproj` per mod
- **Package Manager:** NuGet (standard feed + BepInEx dev feed)
- **Packaging:** `ZipMod` MSBuild target (explicit, not post-build) produces Thunderstore-ready `.zip` files via `powershell.exe Compress-Archive`
- **Game DLL references:** `AssemblyFromGame/Assembly-CSharp.dll` and `UnityEngine.UI.dll`
- **Notable dependencies:** DSPModSave, NebulaMultiplayer API, CommonAPI, NLua, obs-websocket-dotnet, Mono.Cecil

## Repository Structure

```
DSP_Mods/
├── DSP_Mods.sln              # Visual Studio solution
├── AssemblyFromGame/         # Game DLLs used as compile-time references
├── UXAssist/                 # Core UX mod + shared library (largest mod)
├── CheatEnabler/             # Cheat functions mod (depends on UXAssist)
├── Dustbin/                  # Storage/tank dustbin mod
├── DustbinPreloader/         # BepInEx preloader for Dustbin
├── HideTips/                 # Hides tutorial/tip popups
├── LabOpt/                   # Lab performance optimizations
├── LabOptPreloader/          # BepInEx preloader for LabOpt
├── LogisticMiner/            # Logistic stations auto-mine ores
├── LuaScriptEngine/          # Lua scripting support for the game
├── MechaDronesTweaks/        # Mecha drone speed/energy tweaks
├── OverclockEverything/      # Speed/power multipliers for all buildings
├── PoolOpt/                  # Memory pool optimization on save loading
├── UniverseGenTweaks/        # Universe generator parameter tweaks
├── UserCloak/                # Hides/fakes Steam account info
├── UpdateGameDlls/           # MSBuild helper project; runs UpdateGameDlls.ps1 before any mod compiles
└── CompressSave/             # Stub only (moved to external repo)
```

## Mods Summary

| Mod | GUID | Description |
|-----|------|-------------|
| **LiveStreamAssist** | `org.soardev.livestreamassist` | UXAssist-based live-stream statistics display. The rebindable Ctrl+F8 shortcut starts or stops the feature; starting opens Production Statistics, then alternates with Dyson Sphere at random 15-30-second unscaled-time intervals. Closing the statistics window or ending the game session stops the feature without reopening the window. |
| **UXAssist** | `org.soardev.uxassist` | Core QoL mod and shared library. Window resize, profile-based saves, FPS control, factory/logistics/navigation/Dyson Sphere tweaks, UI improvements, config panel UI, and `Common/` + `UI/` widget library shared by other mods. The Factory tab's building-buffer controls include a comparison tip with the original game values. The Logistics tab can push auto-config values to all existing facilities of a type on the current planet (per-setting `Apply` and per-category `Apply All` buttons), and can set both product limits of every Orbital Collector across all loaded factories; logic is in `LogisticsPatch.Apply*`/`ForEach*` and wired in `UIConfigWindow`. Auto-construct uses a bounded construction-site planner, forward sphere-cast probing, horizontal footprint-based detours, adaptive flight altitude, and stuck recovery while flying to construction ghosts. |
| **CheatEnabler** | `org.soardev.cheatenabler` | Cheat pack (depends on UXAssist). Instant build, architect mode, infinite resources, power boosts, Dyson Sphere cheats with a bounded shell-count input (1–99,999), mecha invincibility, and more. |
| **LogisticMiner** | — | Makes logistic stations automatically mine ores and water from the current planet. |
| **HideTips** | — | Suppresses all tutorial popups, random tips, achievement/milestone cards, and skips the prologue cutscene. |
| **MechaDronesTweaks** | — | Configurable drone speed multiplier, skip stage-1 animation, reduce energy consumption. Successor to FastDrones. |
| **OverclockEverything** | — | Multiplies speed and power consumption of belts, sorters, assemblers, labs, miners, generators, ejectors, and silos. |
| **PoolOpt** | — | Shrinks all object pool arrays to actual used size on save load, then forces GC to reduce memory footprint. |
| **UniverseGenTweaks** | — | Adds Epic difficulty, expands max star count to 1024, allows rare veins and flat terrain on birth planet. |
| **UserCloak** | — | Prevents Steam leaderboard/achievement uploads; can fake or block Steam user identity. |
| **Dustbin** | — | Turns storage boxes and tanks into item-destroying dustbins. Supports Nebula multiplayer and DSPModSave. Requires DustbinPreloader. |
| **DustbinPreloader** | — | Mono.Cecil preloader that injects `bool IsDustbin` into `StorageComponent` and `TankComponent` before game load. |
| **LabOpt** | — | Optimizes stacked Matrix Lab updates via a `rootLabId` concept. Temporarily marked obsolete. Requires LabOptPreloader. |
| **LabOptPreloader** | — | Mono.Cecil preloader that injects `int rootLabId` into `LabComponent` before game load. |
| **LuaScriptEngine** | `org.soardev.luascriptengine` | Embeds NLua runtime; loads `.lua` files from `scripts/`; exposes game lifecycle hooks and OBS WebSocket integration. |
| **CompressSave** | — | Stub only; functionality moved to external repository `soarqin/DSP_Mods_TO`. |

## Build System

### Shared MSBuild Configuration

Common properties and references are factored into two root-level files that MSBuild automatically imports for every project:

- **`Directory.Build.props`** — shared `PropertyGroup` defaults (`TargetFramework`, `AllowUnsafeBlocks`, `LangVersion`, `RestoreAdditionalProjectSources`) and shared `ItemGroup`s (BepInEx packages, game DLL references, `Microsoft.NETFramework.ReferenceAssemblies`), and a global `ProjectReference` to the `UpdateGameDlls` helper project (ensuring game DLLs are refreshed before any mod project resolves assembly references).
- **`Directory.Build.targets`** — defines the `ZipMod` and `CopyToParentPackage` targets (see below).
- **`UpdateGameDlls.ps1`** — PowerShell script invoked by the `UpdateGameDlls` helper project; locates the DSP installation via Steam registry and `libraryfolders.vdf`, compares DLL timestamps, and re-publicizes stale DLLs using `assembly-publicizer`.

Individual `.csproj` files only declare what is unique to that project (GUID, version, extra packages, embedded resources).

### Automatic Game DLL Update

`AssemblyFromGame/` holds publicized copies of two game DLLs used as compile-time references. They are refreshed automatically before any mod project compiles by the `UpdateGameDlls` helper project.

The helper project (`UpdateGameDlls/UpdateGameDlls.csproj`) uses the `Microsoft.Build.NoTargets` SDK and is declared as a global `ProjectReference` in `Directory.Build.props` with `ReferenceOutputAssembly=false`, `SkipGetTargetFrameworkProperties=true`, and `Private=false`. This ensures that MSBuild's dependency graph guarantees the helper project completes before any mod project resolves assembly references, with no need for file locks or conditional triggers.

The `UpdateGameDlls.ps1` script:
1. Reads the Steam installation path from the Windows registry (`HKCU\Software\Valve\Steam`).
2. Parses `steamapps/libraryfolders.vdf` to find the library that contains DSP (AppID `1366540`).
3. Locates `<game_root>/DSPGAME_Data/Managed/`.
4. For each DLL (`Assembly-CSharp.dll`, `UnityEngine.UI.dll`): if the game copy is newer than the local copy, runs `assembly-publicizer … --strip --overwrite` to regenerate the local file and stamps it with the source timestamp.

To explicitly update game DLLs:
```
dotnet build UpdateGameDlls\UpdateGameDlls.csproj
```

**Prerequisite:** `assembly-publicizer` must be installed as a .NET global tool:
```
dotnet tool install -g BepInEx.AssemblyPublicizer.Cli
```
If the tool is missing or DSP is not found, the script prints a warning and continues without failing the build.

### Packaging

Packaging is a **separate, explicit build target** — it does not run on every normal build.

To produce a Thunderstore-ready zip:
```
dotnet build -t:ZipMod -c Release
```

The `ZipMod` target (defined in `Directory.Build.targets`) uses pure MSBuild tasks (`MakeDir`, `Copy`, `Delete`) plus `powershell.exe -NoProfile -Command` for `Compress-Archive`. Calling `powershell.exe` as an explicit executable path works correctly from any shell environment (cmd, PowerShell, bash/WSL).

> Note: the target is named `ZipMod` rather than `Pack` because `Pack` is a reserved target name in the .NET SDK (used for NuGet packaging) and would be silently intercepted.

**Per-project packaging properties** (set in the project's `PropertyGroup`):

| Property | Default | Description |
|----------|---------|-------------|
| `PackHasChangelog` | `false` | Include `CHANGELOG.md` in the zip |
| `PackUsePluginsLayout` | `false` | Use `plugins/` + `patchers/` folder layout (Dustbin, LabOpt) |
| `PackPreloaderTargetDir` | *(empty)* | Preloader projects: destination folder for `CopyToParentPackage` |

**Preloader projects** (DustbinPreloader, LabOptPreloader) use `CopyToParentPackage` instead of `ZipMod`:
```
dotnet build -t:CopyToParentPackage -c Release
```
This copies the preloader DLL into the sibling main mod's `package/patchers/` directory, ready to be zipped by the main mod's `ZipMod` target.

### Version Management

Each mod's `<Version>` property in its `.csproj` file is the **single source of truth** for the version number. The `version_number` field in `package/manifest.json` is automatically synchronized from `<Version>` during the `ZipMod` target — no manual update of `manifest.json` is needed.

**Release workflow:**
1. Update `<Version>` in the mod's `.csproj` file (e.g., `<Version>1.2.3</Version>`)
2. Run `dotnet build -t:ZipMod -c Release` — `manifest.json` is updated automatically before packaging

The sync is implemented as an inline PowerShell `Exec` step inside the `ZipMod` target in `Directory.Build.targets`. It uses a regex replace that preserves the original UTF-8 BOM encoding and CRLF line endings of `manifest.json`. Preloader projects (which have no `manifest.json`) are safely skipped via a `Condition="Exists(...)"` guard.

## Key Architectural Patterns

- **Shared library:** `UXAssist` acts as a common library. `CheatEnabler` and `UniverseGenTweaks` reference `UXAssist.csproj` directly to reuse `Common/`, `UI/`, and config panel infrastructure.
- **Centralized mod-feature lifecycle:** `UXAssist.Common.ModFeatures.ModFeatureRegistry` holds shared static lists of mod features discovered across all mods. **Only UXAssist drives the shared deferred lifecycle** (`StartAll`/`UninitAll`/`OnInputUpdateAll`/`OnUpdateAll`); these dispatchers are `internal` so dependent mods (separate assemblies, no `InternalsVisibleTo`) cannot call them and re-trigger other mods' features. A feature's `Init` runs **eagerly** when it is registered (via `Discover`/`Register`), preserving the original `Awake`-phase timing that keybind registration and other early setup rely on — the game's `UIOptionWindow._OnCreate` copies registered keybinds only after all plugins have finished loading. Dependent mods only call `ModFeatureRegistry.Discover(Assembly.GetExecutingAssembly())` (and optionally `Register<T>()`) in their `Awake`. UXAssist begins the deferred lifecycle from its own `Start`; if a dependent feature is discovered after that transition, the registry starts it immediately after initialization so it cannot miss `Start`. The registry also guards start idempotency per feature (start at most once; uninit resets) and per-frame re-entrancy (`Time.frameCount`) for the update dispatchers, as defense-in-depth.
- **Preloader pattern:** `DustbinPreloader` and `LabOptPreloader` use Mono.Cecil to inject new fields into game assemblies at BepInEx preload time, enabling their corresponding main mods to read/write those fields via normal C# without reflection.
- **Internationalization:** `UXAssist/Common/I18N.cs` provides bilingual (EN + ZH) string lookup used across UXAssist and CheatEnabler. Localization keys are declared as `public const string` in per-project registration classes (`UXAssist/Common/I18NKeys.cs`, `CheatEnabler/Localization.cs`, `UniverseGenTweaks/Localization.cs`) and registered through a single `Register()` call from each mod's `Awake()`. Do not pass Chinese string literals to `.Translate()` at call sites.
- **Centralized game constants:** Hard-coded item IDs, tech IDs, logistics capacities, and Dyson sphere geometry defaults live in `UXAssist/Common/GameConstants` (`ItemIds`, `TechIds`, `LogisticsConstants`, `DysonSphereConstants`). Prefer these constants over inline literals in UXAssist patches.
- **Game source facts:** In the original DSP `Assembly-CSharp.dll`, `PlanetFactory.prebuildCount` is computed as `prebuildCursor - prebuildRecycleCursor - 1`, and normal prebuild add/remove paths maintain those cursors. If Auto Construct UI reports zero while visible construction ghosts exist, first suspect the wrong planet/factory, non-prebuild preview state, or a missed UI refresh path before replacing this property with a pool scan.
- **Production statistics cursor layout:** In the original DSP `Assembly-CSharp.dll`, each `ProductStat.cursor` has 12 ring-buffer write pointers over `count[7200]`. Fresh initialization sets `cursor[i] = i * 600`; indices `0..5` are production levels and `6..11` are consumption levels. The level tick intervals are `1, 6, 60, 360, 3600, 36000`, corresponding to `1/60` second, `0.1` second, `1` second, `6` seconds, `1` minute, and `10` minutes per ring sample at 60 game ticks per second. Because each ring has 600 samples, the UI exposes levels 1-5 as 1-minute, 10-minute, 1-hour, 10-hour, and 100-hour history windows, plus the total view through `total[1..6]` for production and `total[8..13]` for consumption.
- **Production statistics reference rate:** In the original DSP `Assembly-CSharp.dll`, the production panel's `参考速率`/`Reference Rate` is read from `ProductStat.refProductSpeed`, not from `cursor`, `count`, or `total`. `ProductionExtraInfoCalculator.CalculateFactory` recomputes it from powered facilities, recipe speed, recipe item counts, proliferator mode, mining speed, and other facility-specific rates; `UIProductEntry.UpdateExtraProductTexts` sums the field across the factories selected by the current planet/star/cluster filter and formats it with `ProductionExtraInfoCalculator.RefSpeedToString`. The value is an ideal rate in items per minute and may be stale or zero until the extra-info calculator runs.
- **Immediate-build shield initialization:** `PlanetATField.physicsArgs` starts as `null` and is allocated by the first physics-shape recalculation. If CheatEnabler completes the first Planetary Shield Generator synchronously from a build-tool callback, `UIPlanetShieldDetail` can observe `fieldGenerators.count > 0` before that recalculation and dereference the null array. `CargoTrafficPatch.EndBatchBuilding` restores the invariant once per batch by calling `planetATField.UpdatePhysicsShape(true)` when field generators exist but `physicsArgs` is still null.
- **Immediate-build beacon timing:** Signal Towers do not share the Planetary Shield initialization race. `DataPool<BeaconComponent>.Add()` resets the value-type component, `NewBeaconComponent` synchronously assigns its entity and already-created power-node IDs, and no UI opens from `beacons.count` or reads tick-created resources. Holo Beacons use `MarkerComponent`; `NewMarkerComponent` also initializes and registers their UI-visible state synchronously.
- **Illegal Dyson shell generation:** Estimated triangle vertex counts mirror the original `DysonShell.GenerateGeometry` rasterization as closely as possible, but can still differ from the final frame-generated shell polygon at numerical boundaries. Max-output generation must treat `QuickAddDysonShell` failure as a candidate rejection and try the next candidate rather than assuming the estimate is exact.
- **Fail-soft patch application:** `PatchImpl<T>.Enable(true)` applies Harmony patches inside a try/catch. Runtime patching can fail through no fault of ours (Harmony re-runs other mods' transpilers on shared target methods), and an escaping exception would abort the calling `ConfigEntry.SettingChanged` delegate chain, desyncing config UI from config values. On failure it logs a `LogError` with the feature type name, rolls back via `UnpatchSelf()`, and leaves `_patch` null so a later `Enable(true)` can retry.
- **Convergent in-game UI state:** In-game overlay widgets whose visibility depends on game state (e.g. `AutoConstructUI`) must not rely solely on one-shot event-driven refreshes (`SettingChanged` handlers, patch `OnEnable`/`OnDisable`), because a thrown exception earlier in a delegate chain or a failed patch application silently drops the refresh. `AutoConstructUI.OnUpdate()` reconciles button visibility and the pending-construction count with actual game state every 30 frames (also effective while paused); the `AutoConstructPatch` postfix on `PlayerAction_Rts.GameTick` only implements the fly-to-target behavior, and `AutoConstructPatch.OnEnable` logs its visibility predicate inputs once per enable as a remote-diagnosis aid.
- **Auto-construct planner:** `AutoConstructPatch` keeps a construction-site plan while drones work, chooses among a fixed candidate set using the native 120-target construction capacity, total drone travel cost, and player travel cost, then refines the strongest candidates toward the target-group surface centroid. It replans before the current site is empty when the remaining local workload no longer covers the next flight. Candidate evaluation has fixed limits and uses allocation-free arrays; local construction checks scan actual hash entries instead of trusting stale module counters and include ghosts whose materials can be delivered from the mecha package.
- **Auto-navigation modes:** The legacy auto-navigation and the new navigation algorithm are mutually exclusive; legacy mode wins if both configuration values load as enabled. The shared `ToggleAutoCruise` key dispatches to only the selected implementation. New-navigation teardown is unconditional for configuration, target, capability, and lifecycle failures, while the manual-input option controls only user-override behavior. Its session state and cloned status label are reset through `GameLogic.OnGameEnd` and patch teardown. When the new algorithm is selected but inactive, the cloned status label prompts the player with the currently configured `ToggleAutoCruise` key only when an effective navigation target exists and it is not the local planet; it shows the active status while navigation is running.
- **Auto-cruise flight input:** In the original `PlayerMove_Fly.GameTick`, `PlayerController.input0.y = 1` matches manual forward movement and `input1.y = 1` matches manual upward thrust. The legacy auto-cruise `Fly` handler must preserve these signs so the mecha leaves a planet with the same orientation and motion as manual control.
- **Shortcut input timing:** UXAssist dispatches mod shortcuts from a postfix of the original `VFInput.OnUpdate`, after DSP has refreshed its current modifier and UI input state. Do not move shortcut polling back to an independent plugin `Update()` or a logic-tick callback: CommonAPI `PressKeyBind.keyValue` combines Unity's current-frame `Input.GetKeyDown()` with `CombineKey` modifier caches refreshed by `VFInput.OnUpdate`. Polling before that refresh can miss a shortcut when a modifier changes in the same frame, which is easier to encounter at low frame rates. Keep the typing/menu guards and per-frame registry guards.
- **UI clone initialization and text measurement:** UXAssist UI factories and dependent mods' direct UI clones treat runtime DSP objects as style sources only; after `Instantiate`, they explicitly reset stateful `UIButton`, `Text`, `Image`, `InputField`, `Slider`, and `UIComboBox` fields that can otherwise carry source state. Use `UXAssist.UI.Util.GetPreferredWidth` for dynamic text width because it invalidates DSP's custom text-generator cache before reading `preferredWidth`; do not replace it with a generic `TextGenerator` or canvas-wide rebuild without validating the rendered result.
- **Transpiler patches:** Performance-critical mods (LabOpt, MechaDronesTweaks) use `[HarmonyTranspiler]` to rewrite IL instructions directly for maximum efficiency. All transpilers in UXAssist, CheatEnabler, and UniverseGenTweaks carry a standard header comment (`// Harmony transpiler:`, `// Target:`, `// Fallback:`) documenting the target method and fallback behavior. `UXAssist.Common.Patching.TranspilerGuard` provides a reusable `CodeMatcher.Finish` helper that returns original instructions when a matcher becomes invalid.
- **Mod-compatibility reflection:** Use `UXAssist.Common.ModCompat.ModCompatHelper` for BepInEx plugin detection, external mod type/method/field resolution, and property-setter lookup. Preserve the old public type identity with a forwarding or inherited compatibility facade when a refactor moves a type that external mods may locate through reflection; build and verify that the legacy reflection target forwards to the refactored implementation. Use `UXAssist.Common.Utils.DysonSphereReflection` for the DSPOptimizations-compatible `DysonSphereLayer` private fields (`totalNodeSP`, `totalFrameSP`, `totalCP`) instead of resolving them locally in each consumer.
- **Build quality gates:** Root `.editorconfig` defines suggestion-only C# style conventions. `Directory.Build.props` enables `TreatWarningsAsErrors` with `NoWarn>0618` for the expected obsolete-API usage, so any new warning fails the build. `.github/workflows/build.yml` runs a Release build and packages the three main mods on every push/PR.
- **Save persistence:** Mods that need to persist data use the `IModCanSave` interface from DSPModSave.
- **Cloned UI button reset:** `UXAssist.UI.Util.ResetButton` calls the original `UIButton.Init` before resetting copied interaction state, preserving the original `Transition.normalColor` values. Do not replace cloned UI styles with guessed colors such as `Color.white`; clear runtime state while retaining the source's default style.
