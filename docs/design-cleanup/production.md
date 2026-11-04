# Production calculation audit

Baseline: `64a23e9772e7878f216cc2e281da2bc3f87d3140` (`origin/master`, 2026-11-04).

## Coverage

Read all 23 C# files in `UXAssist/Production/`, the complete 2,670-line `UXAssist/tools/ProductionCheck/Program.cs`, its project and local build properties, and `UXAssist/docs/ProductionArchitecture.md`. Traced repository callers of the planner, analyzer, formatter, capture API, adapters and catalog lifecycle. Generated `bin/` and `obj/` contents are build products, not audit input.

| Files | Responsibilities checked |
| --- | --- |
| `ProductionModel.cs`, `ProductionPlanRequest.cs`, `ProductionReports.cs` | Public immutable snapshots and report contracts; mutable requests; units and shared numeric helpers |
| `ProductionCatalogReader.cs` | Main-thread Proto/Cargo capture, synthetic recipes, default routes, catalog change subscriptions and clear/rebuild ownership |
| `BlueprintSelectionReader.cs` | Existing entities, prebuilds and offline blueprints; native parameter decoding; supplied context; validation diagnostics |
| `ProcessEvaluator.cs` | Ordinary recipes and ordered dispatch to special mechanics |
| `AccumulatorExchangeAdapter.cs`, `FractionationProcessAdapter.cs`, `FuelGenerationAdapter.cs`, `GasCollectionAdapter.cs`, `LaunchProcessAdapter.cs`, `MiningProcessAdapter.cs`, `RayReceiverAdapter.cs`, `RenewableGenerationAdapter.cs`, `ResearchProcessAdapter.cs` | All nine special mechanics; prerequisites; independent material/power completeness; no speculative merging of distinct calculations |
| `SandboxBoost.cs` | Shared boost-mode resolution and validation for capture, launch and fuel paths |
| `ProductionPlanner.cs`, `ProductionPlanner.Report.cs`, `TwoPhaseSimplex.cs` | Target expansion, material ledger, spray reuse, constraints, cancellation/budget, stable numerical solve and report assembly |
| `FactoryBlackBoxAnalyzer.cs`, `BlackBoxMaterialClassifier.cs`, `BlueprintBottleneckBalance.cs` | Fixed-capacity analysis, whole-building redundancy, bounded fractional reduction, bottleneck basis and uncertainty propagation |
| `BlueprintDescriptionText.cs` | Deterministic item order, category formatting, native field validation/replacement and public result types |
| `tools/ProductionCheck/*` (three tracked files) | Existing managed checks and their linked-source boundary; fixture helpers remain beside their owner |

## Changes

1. **Remove duplicate immutable-array copies.** `ProductionCatalogReader.Capture` previously cloned all three Cargo proliferation arrays before passing them to `ProductionCatalog`, whose constructor clones the same arrays again. Pass the source arrays directly to that constructor. It still owns the immutable copy; its public API and all other callers retain exactly the same isolation. Capture remains synchronous on the game thread, and no source array is stored directly in the catalog. This removes three temporary arrays per capture without removing the snapshot boundary.
2. **Sort description flows once.** `BlueprintDescriptionFormatter.Format` previously independently sorted the same immutable report values for shortages, excess and normal material categories. Materialize one ordered array for those three passes. Preserve category order and every `itemTag` callback's order; formatting, rounding, missing/unknown behavior and serialized text are unchanged. This is per-call temporary data, not persistent derived state.

No files, public types, methods, configuration fields or data formats were removed in this partition. No native game mechanics, production equations, solver ordering, placement or UI geometry changed.

## Candidates deliberately retained

- **`IProductionProcessAdapter` and the evaluator constructor:** nine real implementations, not a one-product interface/factory. These are public extension contracts shared by both `ProductionPlanner` and `FactoryBlackBoxAnalyzer`; custom adapters have priority before native implementations. Replacing them with a switch would remove compatibility and extension behavior.
- **`RenewableGenerationAdapter.cs` (34 lines):** small, but a complete named public mechanic with its own validation and generation-only result. It is not an owner-specific helper pretending to be a utility.
- **`SandboxBoost.cs` (50 lines):** genuinely shared by fuel generation, launches and blueprint/live capture. It centralizes the distinction between stored mode and applied sandbox state, including conflict diagnostics.
- **`BlueprintBottleneckBalance.cs`:** one owning analyzer call, but 148 lines implement an independent constrained optimization problem with normalization, residual validation and material classification. It shares the numerical solver with global planning. Merging it into the analyzer would hide a substantive algorithm, not eliminate an abstraction.
- **`BlackBoxMaterialClassifier.cs`:** stable grouping, whole-building removal, joint-product accounting and bounded fractional propagation are substantial pure calculation responsibilities. `BlackBoxProcessGroup` and `BlackBoxItemClassification` already live beside their owner instead of occupying extra files, and are also consumed by bottleneck calculation.
- **`ProductionPlanner.Report.cs`:** an existing partial of the same class, containing 202 lines of report construction. It adds no forwarding object, interface or lifecycle; merging would only enlarge the 337-line solver file.
- **Public request/report/capture types:** external mod APIs need immutable inputs and independently incomplete material/power outputs. Similar-looking fields have different meanings (gross/net, required/steady-state, equivalent/rounded, grid/Dyson, factory/logistics); they are not duplicate caches.
- **Catalog event state:** `_initialized` prevents duplicate subscriptions; `Current` is a captured snapshot used by UXAssist and CheatEnabler, not a redundant mirror of live Proto state. Clear notifications invalidate belt-derived reports at session boundaries.
- **Capture diagnostics plus evaluator validation:** capture reports unavailable game context while the pure evaluator validates manually constructed requests too. Removing one would weaken a real input boundary.
- **Test fixtures and native escaping helpers:** local to the existing test owner, reused by distinct checks and intentionally independent of the implementation. No new test framework or helper file was introduced.

## Validation

- `dotnet run --project UXAssist/tools/ProductionCheck/ProductionCheck.csproj -c Release`: **569 checks passed before and after changes**.
- `git diff --check -- UXAssist/Production UXAssist/tools/ProductionCheck`: passed.
- Release game-linked project builds are coordinated by the root audit and recorded in its final validation report.
- No original game method-body claim or Harmony target/IL change was made in these edits. The native mechanics were retained verbatim; this audit does not claim in-game execution or Unity UI validation.
