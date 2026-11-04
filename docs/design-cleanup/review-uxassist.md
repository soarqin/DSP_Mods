# Independent UXAssist change review

What this change does: removes inactive scaffolding, unused internal members, duplicate array copies/sorts and repeated UI formatting. It keeps the existing feature, calculation and native patch contracts. Expected load is one running DSP process, including frame/tick-sensitive paths and occasional snapshot/description creation.

Reviewer: the CheatEnabler/UniverseGenTweaks audit worker, independent of both UXAssist implementation workers. Reviewed the complete UXAssist diff against baseline `64a23e9772e7878f216cc2e281da2bc3f87d3140`, then connected source and callers. No source was changed during this review.

Looks good. Ship.

## Evidence

- **All 23 changed UXAssist files checked.** Common: `I18N`, `KnownItemId`, `WinApi`. Functions: three UI owners, `UIFunctions`, `WindowFunctions`. Patches: four removed bulk-enable facades, inactive build-tool/game/player experiments, logistics item projection and an unused UI size helper. UI: key binder, commented window hooks and shared charge-power formatting. Production: catalog capture and blueprint text. Entry point: commented autosave binding.
- **Deleted methods are not runtime entry points.** Repository searches include ordinary and quoted method names, static imports, feature discovery and Harmony attributes. Removed factory bulk-enable methods belong to internal outer types; actual config handlers and Start/Uninit still call each nested patch directly. Removed UI lifecycle methods have empty bodies and are called explicitly rather than discovered. Public `UIFunctions.Start` is retained. Active lifecycle order and patch ownership are unchanged.
- **Localization remains native.** Removed `I18N.Translate` is internal, has no `this` receiver, no caller/static import, and no quoted reflection lookup. It is unrelated to native string `.Translate()` calls. Apply/register/language-change hooks remain intact.
- **The key-binder branch is unreachable.** Baseline `_nextNotOn` is private, not `[SerializeField]`, and has no true assignment or quoted reflective reference. Its only assignment is false inside the branch. Removing that field/branch preserves the click handler's reachable assignment and all serialized UI control fields.
- **Public enum contract is stable.** Compared `KnownItemId` constants to `GameConstants.ItemIds`: 5001, 5002, 5003 and 1210 are unchanged, as are enum/member names and underlying `int` type. Existing logistics/player callers keep their values.
- **UI formatting is exactly reused.** All four mapper multipliers remain `long` arithmetic with their original constants. The shared private method retains the nine-space StringBuilder, WriteKMG width/flags, W suffix and Trim. Mapper ranges and creation geometry are unchanged. The overlay predicate directly tests each original storage item ID, retaining short-circuit and empty-array behavior.
- **Cargo snapshots still own their arrays.** Read the `ProductionCatalog` constructor: it clones each of speed, extra and power arrays into private arrays after copying model inputs. Capture remains synchronous on the game thread. Removing clones immediately before the constructor eliminates duplicate allocations while keeping the defensive-copy boundary and public constructor behavior.
- **Description order stays deterministic.** `ProductionReport` creates its own read-only ItemFlows dictionary from copied inputs. Sorting its values once by ItemId and retaining one per-call array produces the same missing/excess/input/output/research passes and `itemTag` call order. No persistent cache or solver ordering changed.
- **Removed patches were already comments.** The autosave experiment, stepped rotation experiment, two star/hive display experiments and three window snippets were fully commented at baseline. Deleting them removes no compiled Harmony method or active setting. The live IL matching/injection bodies are unchanged.
- **No replacement fragmentation.** No new C# file, runtime type, dependency, generic framework or broad utility container was introduced. The one shared formatting method stays private in its actual four-call owner. Existing compatibility facades, pure production adapters, miner geometry/solver/replay boundaries and distinct UI ownership were retained for concrete reasons in the audit reports.

## Checks and limits

Scoped `git diff --check -- UXAssist` passed during review. Existing production checks and normal game-linked Release builds are recorded by the root executor and production worker; this review does not invent additional runtime results.

Not checked: in-game/Unity execution, third-party reflection into private/internal implementation details, or unrelated native target compatibility after a game update. No changed code needs a new native-method semantic interpretation; active Harmony targets and IL bodies were retained.
