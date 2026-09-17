# Agent Guidelines

## Rules

- Update `AGENTS.md` after each task by editing current guidance directly. Keep only project standards and durable architecture references; do not retain investigation evidence, task histories, completed plans, or implementation snapshots.
- Write all documentation and code comments in English.
- Inspect the original DSP game DLL, never the stripped publicized copy in `AssemblyFromGame/`, when method bodies are required. Locate DSP using the Steam registry and `steamapps/libraryfolders.vdf` as `UpdateGameDlls.ps1` does, then decompile `<game_root>/DSPGAME_Data/Managed/Assembly-CSharp.dll`.
- Resolve in-game terminology from `<game_root>/Locale/`: `Names` contains keys, `1033` is English, and `2052` is Simplified Chinese.

## Project

- This repository contains independent BepInEx 5.x C# mods for Dyson Sphere Program. HarmonyLib patches game methods at runtime; `UXAssist` is the shared library used directly by `CheatEnabler` and `UniverseGenTweaks`.
- Projects target `net472` or `netstandard2.1` with SDK-style `.csproj` files. Compile-time game references live in `AssemblyFromGame/`.
- `DustbinPreloader` and `LabOptPreloader` are Mono.Cecil preloaders; their main mods consume the injected fields without reflection.
- LiveStreamAssist: [WebSocket architecture, transport migration, and validation](LiveStreamAssist/docs/WebSocketDesign.md).

## Build

- `Directory.Build.props` supplies common frameworks, BepInEx packages, game references, warning policy, and the `UpdateGameDlls` dependency. `Directory.Build.targets` defines `ZipMod` and `CopyToParentPackage`.
- Normal validation: `dotnet build <project>/<project>.csproj -c Release --no-restore`.
- Refresh game references explicitly with `dotnet build UpdateGameDlls/UpdateGameDlls.csproj`; the script compares timestamps and uses `assembly-publicizer --strip --overwrite`. Missing DSP or the publicizer is a warning, not a build failure.
- Package main mods with `dotnet build -t:ZipMod -c Release`; package preloaders with `dotnet build -t:CopyToParentPackage -c Release`. The mod `.csproj` `<Version>` is the single version source and packaging synchronizes `package/manifest.json`.
- Treat warnings as errors except the intentional obsolete API warning `0618`; do not weaken this policy to hide new warnings.

## Architecture

- `ModFeatureRegistry` discovers dependent-mod features during `Awake`; feature initialization is eager, while UXAssist alone drives the deferred start, input, update, and uninitialization lifecycle. Keep dispatchers internal and idempotent.
- Dispatch UXAssist shortcuts from the postfix of `VFInput.OnUpdate`, after DSP refreshes modifier and UI state. Keep typing/menu guards and per-frame registry guards; do not poll from an independent `Update()` or logic-tick callback.
- Register localization keys through each project registration class and use `.Translate()` with keys. Do not add Chinese literals at call sites.
- Prefer `UXAssist/Common/GameConstants` for item, tech, logistics, and Dyson sphere constants instead of inline literals.
- Persist mod data through `IModCanSave` from DSPModSave.
- Use `ModCompatHelper` for external plugin/type/member resolution and preserve legacy public type identities when refactoring reflection targets. Use `DysonSphereReflection` for DSPOptimizations-compatible Dyson sphere fields.
- Performance-sensitive Harmony transpilers must include the standard target/fallback header and use `TranspilerGuard` when a matcher can fail; returning original instructions is the fallback.
- `PatchImpl<T>.Enable(true)` must remain fail-soft: log, unpatch, and leave the patch unset if Harmony application fails so config delegate chains remain consistent.
- Overlay UI state must converge from actual game state on periodic updates, not depend only on one-shot event or patch callbacks. Cloned controls must reset runtime state while retaining source styles; use `UXAssist.UI.Util.GetPreferredWidth` for dynamic text and `ResetButton` for cloned buttons.

## Automation Architecture

### Auto-construct

- `AutoConstructEnabled` controls patch activation; `AutoConstructButtonEnabled` controls only button visibility. Never couple them.
- Keep planning bounded and allocation-free. Follow native construction eligibility and planet-local surface movement semantics.
- The planner owns only its generated `MoveTo` orders. Clear an active auto order before resetting planner state on disable or planet changes, without clearing user orders. Recovery detours must return control to the construction route.
- Keep obstacle recovery at the current flight altitude. Validate lateral detour and escape waypoints against physics before issuing them, and retain recovery state to retry broader routes instead of treating an exhausted candidate list as success.
- Let native flight control settle altitude after construction arrival; do not force a target altitude from auto-construct. The native non-gas flight baseline is 15f, while gas planets use a different lower bound.

### Auto-cruise

- `PlayerPatch.AutoNavigation` is the only auto-cruise implementation. Its Harmony patches follow `AutoCruiseEnabled`; use auto-cruise terminology in user-facing text rather than the game's native autopilot terminology.
- Use one navigation lifecycle path for target state and reusable obstacle data. Require a resolvable target, yield to `player.navigation.navigating`, and reject an active native autopilot before reporting that auto-cruise started.
- Preserve native movement and braking conventions; manual warp input takes precedence.

## Review Standard

- Fix root causes with minimal focused changes. Do not alter unrelated behavior, add copyright headers, commit changes, or create branches unless requested.
- Before handoff, run the narrowest relevant build or test, then `git diff --check`. Mention unrelated pre-existing failures instead of changing them.
