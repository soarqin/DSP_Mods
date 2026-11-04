# CheatEnabler and UniverseGenTweaks design audit

Baseline: `64a23e9772e7878f216cc2e281da2bc3f87d3140` (`origin/master`, 2026-11-04).

## Coverage

Reviewed the design and call relationships of all 45 tracked C# sources (35 CheatEnabler, 10 UniverseGenTweaks) and both project files. Read the plugin entry points, configuration bindings, UI construction/update paths, all function classes, patch owners and their nested patch implementations, serialization, and localization registration. The original native methods are not reimplemented or changed by this cleanup; no claim of an in-game execution test is made.

| Area | Files covered | Result |
| --- | --- | --- |
| CheatEnabler entry/config/localization | `CheatEnabler.cs`, `UIConfigWindow.cs`, `Localization.cs`, `CheatEnabler.csproj` | Retain config keys/defaults and public APIs; remove transient UI state and a forwarding method. |
| CheatEnabler functions | `Functions/{DysonSphereFunctions,PlanetFunctions,PlayerFunctions}.cs`; `Functions/DysonSphere/{DysonSphereResolver,FrameRemovalFunctions,GeometryHelpers,IllegalShellFunctions,ShellCompletionFunctions}.cs` | Remove empty feature registrations, an unused private argument, and unused local collections; preserve all operations and geometry algorithms. |
| CheatEnabler factory | `Patches/Factory/{ArchitectModePatch,BeltSignalPatch,BeltSignalSourceStats,FactoryPatch,ImmediateBuildPatch,LogisticsControlPatch,NoConditionBuildPatch,PowerBoostPatch}.cs`; `Patches/FactoryPatchCompat.cs` | Remove the duplicated signal-belt capacity; retain independent patch groups, statistics boundary and compatibility facade. |
| CheatEnabler general patches | `Patches/{CombatPatch,DysonSpherePatch,GamePatch,PlanetPatch,PlayerPatch,ResourcePatch,TechTweaksPatch}.cs` | Retain patch targeting, native contracts, option ownership and lifecycle. |
| CheatEnabler tech | `Patches/Tech/{DowngradeMode,TechClickHandler,TechDowngrade,TechFuncRevert,TechKeyCombo,TechLog,TechTreeUI,TechUnlock}.cs` | Merge the modifier decoder into the click handler; remove a prefix-only logging wrapper. Retain cascade planning, inverse/recomputation, session state and UI resource ownership. |
| UniverseGenTweaks | `UniverseGenTweaks.cs`, `UIConfigWindow.cs`, `Localization.cs`, `MoreSettings.cs`, `EpicDifficulty.cs`, `BirthPlanetPatch.cs`, `Functions/GalaxyGenSave.cs`, `Patches/{CombatSettingsPatch,GalaxyGenSettingsPatch,GalaxySelectUIPatch}.cs`, `UniverseGenTweaks.csproj` | Remove construction-only state, duplicate listener-removal code and the empty save-helper feature registration; preserve config, UI, patching and save layout. |

Repository-wide searches included ordinary calls, string/type references, the feature registry's reflective lifecycle lookup, Harmony attributes, and linked source in test projects. Historical `docs/StaticStateInventory.md` is evidence from an earlier audit, not an executable caller.

## Changes and evidence

1. **Delete `TechKeyCombo.cs`.** `TryDecode` had exactly one consumer: `TechClickHandler.HandleClick`. Its modifier decoding now lives privately in that owner. `TechUnlock` and `TechDowngrade` compare the same `int.MaxValue` sentinel directly. All eight Shift/Ctrl/Alt combinations retain their previous meaning: none/Shift/Ctrl/both/Alt/Alt+Shift/Alt+Ctrl/all map to ignored/1/10/100/max/ignored/ignored/ignored. The injected Harmony entry method and its exception boundary remain unchanged.
2. **Delete `TechLog.cs`.** Its only action was to call the existing logger with a fixed prefix. All five call sites now call that logger directly with identical `[TechTweaks] ` text and warning severity. The log-once guard in `TechFuncRevert` remains in place.
3. **Remove eight ineffective `[ModFeature]` registrations.** Seven pure function classes in CheatEnabler and `GalaxyGenSave` had no working lifecycle methods. `ModFeatureRegistry` looks only for parameterless static `Init`, `Start`, `Uninit`, `OnInputUpdate`, and `OnUpdate`; its per-feature storage is private, and the attribute name has no lookup path. These classes are static and have no inherited lifecycle. The only two matching `Init` methods are empty. Removing the attributes prevents useless descriptors and per-frame null-delegate dispatches. Public types and their methods, including those two legacy empty `Init` methods, remain available.
4. **Remove `_signalBeltsCapacity`.** Every write copied the allocated `_signalBelts.Length` (64 initially, doubled on growth, zero after nulling). The single array now supplies its capacity directly. `GetSignalBelts` explicitly checks the array for null, preserving lookup behavior after disable/reset. `GetOrCreateSignalBelts` is called only after initialization creates the array; growth and copy lengths are identical. No native field or reflective reference uses this private field.
5. **Remove unused private finalization input and allocations.** `FinalizeDysonSphereChanges` never read its `DysonSphereLayer` argument. Its six calls now pass only the sphere and existing flags; one no-longer-needed `FirstOrDefault` layer search disappears. `availableFrames` and `unusedFrameIds` were allocated but never read in `CreateIllegalDysonShellsSpecially`; their declarations are removed. Shell search, ordering, synchronization and native mutations are unchanged.
6. **Stop storing construction-only UI references globally.** Both config windows copied `CreateUI`'s `trans` parameter into `_windowTrans` solely to read it during the same call. Tabs now use `trans` directly. Four galaxy-slider title fields were read only in `_OnCreate`; they are local variables now. The Unity hierarchy still owns these controls. Coordinates, parents, localization and creation order are unchanged.
7. **Reuse existing UI code without a new abstraction.** The body of CheatEnabler's `UpdateButtons` is placed into its sole private caller `UpdateUI`, which remains the same event delegate. Galaxy selection's `_OnUnregEvent` calls the already-existing `RemoveAllListeners` method, whose four statements match the former body exactly. Listener registration order and timing remain unchanged.

No new source file, runtime abstraction, dependency, option, persistent field or compatibility wrapper was introduced. No version, README feature description or changelog was changed because this is an internal behavior-preserving cleanup.

## Deliberately retained candidates

| Candidate | Concrete reason to retain |
| --- | --- |
| `FactoryPatchCompat.cs` and `DysonSphereFunctions` forwarding methods | Public legacy type/method identities and external reflection are documented compatibility contracts. Removing these would make reducing file count more important than callers. |
| `DysonSphereResolver.cs` | Resolves editor/local targets and supplies consistent user-facing errors to eight operation call sites across three classes; it has real shared responsibility. |
| `FrameRemovalFunctions` / `ShellCompletionFunctions` | Complete independent public operations with confirmation, native resource mutations and statistics/render reconciliation, not a few-line helper split. |
| `GeometryHelpers.cs` | Shared specialized geometry/pool operations. The triangle-count path has thread-local maps for parallel search; full mesh creation uses native shared maps and mutates shells. Similar loops have different contracts; merging them would couple parallel evaluation to game state or add parameterized machinery. |
| `BeltSignalSourceStats.cs` and `BeltSignalSourcePreset` | Pure-managed conversion/accumulation/request selection linked explicitly into `ProductionCheck.csproj`; existing checks exercise fractional rates and recipe preferences. This boundary keeps solver work out of the logic tick. |
| `SourceCache`, `PendingSources`, `FailedSources`, `_sourceCatalog` | Avoid solver execution in ticks, deduplicate pending frame work and warnings, and invalidate results when catalog ownership changes. They are not duplicate game values. |
| `CargoTrafficPatch` batch flags and sets | Building and subsequent renderer flush are distinct phases. `_disableRefreshBatchesBuffers` intentionally remains true while `_isBatchBuilding` is already false, preventing intermediate refreshes during flush. Sets coalesce native renderer work. |
| Multiple factory patch classes/files | Independently activated config options and distinct Harmony targets; combining them into a common dispatcher would obscure patch ownership. The apparently similar logistics transpilers match different native property getters. |
| `DowngradeMode`, `TechTreeUI`, `TechDowngrade`, `TechFuncRevert`, `TechUnlock` | Separate session state, Unity resource lifetime, validation/cascade planning, inverse/recompute rules, and upgrade semantics. Native capacity/data checks and non-invertible bonuses must not be collapsed into generic subtraction. |
| `TechFuncRevert` rounding helpers | Native functions use different rounding/truncation sequences; superficially similar conversions are not equivalent. |
| `SkipBulletPatch` sail caches and `_instantAbsorb` | Bounded per-star buffers and a derived fast-path flag are consumed in performance-sensitive patches; restructuring that algorithm without an output/performance proof would not be a safe design cleanup. |
| `MoreSettings.cs` | Shared public config identity used by plugin binding, both UIs and three patch owners; its `MaxStarCount` field is explicitly referenced by injected IL. |
| `GalaxyGenSave.cs` | A compact, explicit public binary-serialization boundary. Export/import order remains four doubles; it no longer registers as a lifecycle feature. Removing its public identity is unnecessary. |
| Galaxy generation live versus selected settings | `MinDist`/`MinStep`/`MaxStep`/`Flatten` describe selection controls; `Game*` fields describe the current saved game's generation parameters. They can intentionally differ while another galaxy is previewed. |
| `BirthPlanetPatch.BackupData` | Captures originals for repeated configuration changes; restoration state is necessary, not a redundant copy. The lifecycle/preload sequence is not changed. |
| Combat slider mapping table | Seven actual slider handlers share the mapper. Separate UI display and difficulty thresholds implement inverse/display/scoring semantics, so replacing them with one table would change boundaries. |
| Empty public `Init` methods in two function facades | Kept to avoid gratuitous public method removal; no longer discovered or invoked by the feature registry. |

## Validation scope

Root execution owns the Release builds and existing production regression checks so game-reference generation is not run concurrently. See the consolidated validation record for command output and results. Scoped `git diff --check -- CheatEnabler UniverseGenTweaks` is required before handoff.

This audit checks repository design and behavior-preserving source transformations. It does not certify unrelated native patch patterns against a new game release or execute Unity/in-game interactions. No game method body needed a new semantic interpretation for these changes; native patch targets and injected IL are unchanged.
