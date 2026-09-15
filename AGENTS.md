# Agent Guidelines

## Rules

- Update `AGENTS.md` after completing every task; edit its current guidance directly rather than keeping a changelog.
- Write all documentation and code comments in English.
- Inspect the original DSP game DLL, never the stripped publicized copy in `AssemblyFromGame/`, when method bodies are required. Locate DSP using the Steam registry and `steamapps/libraryfolders.vdf` as `UpdateGameDlls.ps1` does, then decompile `<game_root>/DSPGAME_Data/Managed/Assembly-CSharp.dll`.
- Resolve in-game terminology from `<game_root>/Locale/`: `Names` contains keys, `1033` is English, and `2052` is Simplified Chinese.

## Project

- This repository contains independent BepInEx 5.x C# mods for Dyson Sphere Program. HarmonyLib patches game methods at runtime; `UXAssist` is the shared library used directly by `CheatEnabler` and `UniverseGenTweaks`.
- Projects target `net472` or `netstandard2.1` with SDK-style `.csproj` files. Compile-time game references live in `AssemblyFromGame/`.
- `DustbinPreloader` and `LabOptPreloader` are Mono.Cecil preloaders; their main mods consume the injected fields without reflection.

## Build

- `Directory.Build.props` supplies common frameworks, BepInEx packages, game references, warning policy, and the `UpdateGameDlls` dependency. `Directory.Build.targets` defines `ZipMod` and `CopyToParentPackage`.
- Normal validation: `dotnet build <project>/<project>.csproj -c Release --no-restore`.
- Refresh game references explicitly with `dotnet build UpdateGameDlls/UpdateGameDlls.csproj`; the script compares timestamps and uses `assembly-publicizer --strip --overwrite`. Missing DSP or the publicizer is a warning, not a build failure.
- Package main mods with `dotnet build -t:ZipMod -c Release`; package preloaders with `dotnet build -t:CopyToParentPackage -c Release`. The mod `.csproj` `<Version>` is the single version source and packaging synchronizes `package/manifest.json`.
- Treat warnings as errors except the intentional obsolete API warning `0618`; do not weaken this policy to hide new warnings.

## Architecture

- `ModFeatureRegistry` discovers dependent-mod features during `Awake`; feature initialization is eager, while UXAssist alone drives the deferred start, input, update, and uninitialization lifecycle. Keep dispatchers internal and idempotent.
- Register localization keys through each project registration class and use `.Translate()` with keys. Do not add Chinese literals at call sites.
- Prefer `UXAssist/Common/GameConstants` for item, tech, logistics, and Dyson sphere constants instead of inline literals.
- Persist mod data through `IModCanSave` from DSPModSave.
- Use `ModCompatHelper` for external plugin/type/member resolution and preserve legacy public type identities when refactoring reflection targets. Use `DysonSphereReflection` for DSPOptimizations-compatible Dyson sphere fields.
- Performance-sensitive Harmony transpilers must include the standard target/fallback header and use `TranspilerGuard` when a matcher can fail; returning original instructions is the fallback.
- `PatchImpl<T>.Enable(true)` must remain fail-soft: log, unpatch, and leave the patch unset if Harmony application fails so config delegate chains remain consistent.
- Overlay UI state must converge from actual game state on periodic updates, not depend only on one-shot event or patch callbacks. Cloned controls must reset runtime state while retaining source styles; use `UXAssist.UI.Util.GetPreferredWidth` for dynamic text and `ResetButton` for cloned buttons.

## Verified Game Facts

- In the original `Assembly-CSharp.dll`, `PlanetFactory.prebuildCount` is `prebuildCursor - prebuildRecycleCursor - 1`; investigate planet, factory, preview state, and UI refreshes before replacing it with a pool scan.
- `ProductStat.cursor` has 12 rings: indices `0..5` are production and `6..11` are consumption. Production-panel reference rate comes from `refProductSpeed`, which the extra-info calculator may leave stale or zero until it runs.
- `PlanetATField.physicsArgs` is null until the first physics-shape recalculation. Immediate completion of the first Planetary Shield Generator must restore that invariant before shield UI reads it; Signal Towers do not share this race.
- Estimated Dyson shell geometry can differ at numerical boundaries from the final rasterized polygon. If `QuickAddDysonShell` rejects a candidate, try the next candidate rather than trusting the estimate.

## Automation Invariants

### Auto-construct

- `AutoConstructEnabled` controls patch activation; `AutoConstructButtonEnabled` controls only button visibility. Never couple them.
- Keep planning bounded and allocation-free: use the native 120-target construction capacity, fixed candidate limits, actual construction hash entries, and the same material-delivery and enemy-proximity eligibility rules as `ConstructionModuleComponent.GameTick`.
- Prebuild and player positions are planet-local. Derive angular fan axes in the planet-local frame, use great-circle surface distances, and preserve the game's surface-direction arrival semantics.
- Select a nearest local cluster before falling back to the nearest candidate; refine destinations from scanned ghosts. In the approach corridor, retain the closest route-prefix ghost so nearer work cannot be bypassed. When at most eight eligible ghosts remain, route directly to the nearest one; the existing scan caps the final-pass count at nine.
- A site is held only while its initial drone wave is below capacity and its throughput is competitive. Replan before the site empties when the remaining workload no longer covers the next flight.
- Normal route changes settle residual flight speed before issuing the new route. Recovery detours and their continuation/final routes bypass settling so they can take over while the mecha is stuck. Detours use collider footprint bounds and resume the construction route after completion.
- Clear the active auto `MoveTo` order before resetting planner state during disable and planet changes, but clear it only when it is still the auto order so user orders are preserved.

### Auto-cruise

- `PlayerPatch.AutoNavigation` is the only auto-cruise implementation. Its Harmony patches follow `AutoCruiseEnabled`; all user-facing terminology is auto-cruise / `自动巡航`, not the game's `自动导航` autopilot terminology.
- Yield to `player.navigation.navigating`. Starting requires a resolvable target and must reject an already-running native autopilot before showing a started notification. Reset target state and reusable obstacle data through one navigation lifecycle path.
- Sail velocity uses the game's planet-frame blend `Clamp01((600 - altitude) / 450)`. Apply the same blend to `visual_uvel`; do not write `uRotation` from the sail postfix because the game recomputes it afterward.
- Apply braking directly using the game's decay behavior. Fixed targets need an approach speed cap below the 75 m/s fly handoff threshold, and warp must release early enough for braking; manual warp speed input takes precedence.
- Preserve native flight input signs: `input0.y = 1` is forward thrust, `input1.y = 1` is upward thrust, and `input0.z` is the walk jump/takeoff axis.
- Satellite routing uses a parent-body corridor and current-heading tangent selection. Null-check `SpaceSector.dfHives`, which is absent in saves without hive data.

## Input Timing

- Dispatch UXAssist shortcuts from the postfix of `VFInput.OnUpdate`, after DSP refreshes modifier and UI state. Keep typing/menu guards and per-frame registry guards; do not poll from an independent `Update()` or logic-tick callback.

## Review Standard

- Fix root causes with minimal focused changes. Do not alter unrelated behavior, add copyright headers, commit changes, or create branches unless requested.
- Before handoff, run the narrowest relevant build or test, then `git diff --check`. Mention unrelated pre-existing failures instead of changing them.
