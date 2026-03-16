# Agent Guidelines

## Rules

- Update `AGENTS.md` after completing every task.
- Do not record a changelog in `AGENTS.md`; modify the document content directly instead.
- All documentation and code comments must be written in English.

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
└── CompressSave/             # Stub only (moved to external repo)
```

## Mods Summary

| Mod | GUID | Description |
|-----|------|-------------|
| **UXAssist** | `org.soardev.uxassist` | Core QoL mod and shared library. Window resize, profile-based saves, FPS control, factory/logistics/navigation/Dyson Sphere tweaks, UI improvements, config panel UI, and `Common/` + `UI/` widget library shared by other mods. |
| **CheatEnabler** | `org.soardev.cheatenabler` | Cheat pack (depends on UXAssist). Instant build, architect mode, infinite resources, power boosts, Dyson Sphere cheats, mecha invincibility, and more. |
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

- **`Directory.Build.props`** — shared `PropertyGroup` defaults (`TargetFramework`, `AllowUnsafeBlocks`, `LangVersion`, `RestoreAdditionalProjectSources`) and shared `ItemGroup`s (BepInEx packages, game DLL references, `Microsoft.NETFramework.ReferenceAssemblies`).
- **`Directory.Build.targets`** — defines the `UpdateGameDlls`, `ZipMod`, and `CopyToParentPackage` targets (see below).
- **`UpdateGameDlls.ps1`** — PowerShell script invoked by the `UpdateGameDlls` target; locates the DSP installation via Steam registry and `libraryfolders.vdf`, compares DLL timestamps, and re-publicizes stale DLLs using `assembly-publicizer`.

Individual `.csproj` files only declare what is unique to that project (GUID, version, extra packages, embedded resources).

### Automatic Game DLL Update

`AssemblyFromGame/` holds publicized copies of two game DLLs used as compile-time references. They are refreshed automatically **before every build** by the `UpdateGameDlls` MSBuild target, which calls `UpdateGameDlls.ps1`.

The script:
1. Reads the Steam installation path from the Windows registry (`HKCU\Software\Valve\Steam`).
2. Parses `steamapps/libraryfolders.vdf` to find the library that contains DSP (AppID `1366540`).
3. Locates `<game_root>/DSPGAME_Data/Managed/`.
4. For each DLL (`Assembly-CSharp.dll`, `UnityEngine.UI.dll`): if the game copy is newer than the local copy, runs `assembly-publicizer … --strip --overwrite` to regenerate the local file and stamps it with the source timestamp.

The target can also be run explicitly:
```
dotnet build -t:UpdateGameDlls
```

**Prerequisite:** `assembly-publicizer` must be installed as a .NET global tool:
```
dotnet tool install -g BepInEx.AssemblyPublicizer.Cli
```
If the tool is missing or DSP is not found, the target prints a warning and continues without failing the build.

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

## Key Architectural Patterns

- **Shared library:** `UXAssist` acts as a common library. `CheatEnabler` and `UniverseGenTweaks` reference `UXAssist.csproj` directly to reuse `Common/`, `UI/`, and config panel infrastructure.
- **Preloader pattern:** `DustbinPreloader` and `LabOptPreloader` use Mono.Cecil to inject new fields into game assemblies at BepInEx preload time, enabling their corresponding main mods to read/write those fields via normal C# without reflection.
- **Internationalization:** `UXAssist/Common/I18N.cs` provides bilingual (EN + ZH) string lookup used across UXAssist and CheatEnabler.
- **Transpiler patches:** Performance-critical mods (LabOpt, MechaDronesTweaks) use `[HarmonyTranspiler]` to rewrite IL instructions directly for maximum efficiency.
- **Save persistence:** Mods that need to persist data use the `IModCanSave` interface from DSPModSave.
