# Independent review: CheatEnabler and UniverseGenTweaks

What this change does: removes two internal helper files, ineffective feature registrations, duplicate array-capacity state and construction-only UI fields. The review assumes the normal single-player game/main-thread lifecycle and existing dependent-mod APIs; it does not assume that a missing ordinary text call makes a Harmony or reflection target unused.

Reviewed independently from the implementation author, against baseline `64a23e9772e7878f216cc2e281da2bc3f87d3140`. Scope: the complete 18-file CheatEnabler/UniverseGenTweaks working diff, connected owners and callers, registry implementation, current configuration/save entry points and relevant README contracts. No code was edited during this independent review.

## Findings

No actionable regression or replacement abstraction found. The changes are appropriately focused and retain existing public type identities.

## Evidence checked

| Change | Independent check and conclusion |
| --- | --- |
| `TechKeyCombo.cs` removal | The deleted class is internal. Repository-wide text/string searches find no remaining runtime reference. `TechClickHandler.HandleClick` was its sole decoder consumer; its injected public entry method and exception boundary remain unchanged. The private decoder preserves all eight modifier cases, listed below. `TechUnlock.Run` and `TechDowngrade.Run` use the identical `int.MaxValue` sentinel. No cascade or capacity-validation code was moved or removed. |
| `TechLog.cs` removal | Its internal method only prefixed `[TechTweaks] ` and called `Logger.LogWarning`. All five replacement calls retain the same prefix, message and severity. `UnknownFuncsWarned.Add(func)` still gates repeated unsupported-function logs. No alternate logger, service or helper file replaces it. |
| Eight `[ModFeature]` removals | Checked `ModFeatureRegistry.Discover`, `StaticFeature` and `InitStatic`: only parameterless `Init`, `Start`, `Uninit`, `OnInputUpdate` and `OnUpdate` affect runtime work. The seven CheatEnabler function classes and `GalaxyGenSave` have no working lifecycle callbacks or static constructors; the only two matching `Init` bodies are empty and their public methods remain available. The registry's stored list is private and feature names have no consumer/lookup. Removing empty records preserves the order and work of the remaining real features. |
| `GalaxyGenSave` registration removal | `UniverseGenTweaks` still implements `IModCanSave` and directly calls `GalaxyGenSave.Export/Import`. The save version and the sequence of four doubles are unchanged. The serializer class, namespace and methods retain their public identities. |
| `_signalBeltsCapacity` removal | Every original assignment equalled the allocated array length, or zero after setting the array null. Initial size 64, doubling and copy lengths remain the same. `GetSignalBelts` now explicitly checks null; this matches the former zero-capacity short circuit on disabled/reset state. `GetOrCreateSignalBelts` is reached during initialization after array allocation, or from `_initialized`-guarded callbacks. Constructor/removal callbacks can safely read the null array. Existing growth policy is unchanged. |
| Dyson-shell finalization simplification | The deleted layer argument was private and unread. All six calls preserve the existing `notify` and `resetRenderMasks` values; eliminating `FirstOrDefault(l => l != null)` removes only a side-effect-free selection. Native mutations and their order are unchanged. The two deleted local collections were constructed but never read. |
| Config-window parent fields | Both `_windowTrans` fields were assigned from the current `CreateUI` argument and used only during that same call. Direct `trans` use preserves object identity, tab order and geometry. No listener or deferred callback captured the removed fields. |
| `UpdateButtons` merge | `UpdateUI` remains the delegate subscribed to `MyConfigWindow.OnUpdateUI`. It now contains exactly its sole callee's former body, including all readiness guards. Native account/achievement state and button activation conditions are unchanged. |
| Galaxy-title fields | The four title references were used only during `_OnCreate` and now remain locals there. The instantiated controls remain owned by Unity's transform hierarchy. Names, localizer keys, parent objects, spacing constants and creation order are unchanged; slider/value fields needed later remain stored. |
| Galaxy listener removal | `_OnUnregEvent` calls the existing `RemoveAllListeners`, whose four statements exactly match the former body. `_OnCreate`/`_OnOpen` still remove, update, then add listeners in the same order. No extra subscription or event path was introduced. |

Modifier mapping verified against the deleted body:

| Shift | Ctrl | Alt | Result |
| --- | --- | --- | --- |
| false | false | false | Ignore |
| true | false | false | 1 level |
| false | true | false | 10 levels |
| true | true | false | 100 levels |
| false | false | true | `int.MaxValue` (all available levels) |
| true | false | true | Ignore |
| false | true | true | Ignore |
| true | true | true | Ignore |

## Checks and limits

- `git diff --check -- CheatEnabler UniverseGenTweaks`: passed.
- Existing managed production checks had already passed 569 checks in this reviewer's production partition, including belt-source conversion/preset behavior; full affected Release builds and original-DLL target checks are coordinated and recorded by the root audit.
- No C# signature used by injected IL, native game method body, public compatibility facade, configuration key/default or saved data layout was removed by this partition.
- No new abstraction, forwarding file, generic factory, persistent cache, dependency or test framework was added.

Verdict: Ship.

Not checked: manual Unity interaction and in-game execution; this review does not claim them from compilation or static inspection.
