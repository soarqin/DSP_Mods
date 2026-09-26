# UXAssist Production Planner and Factory Black Box Analysis Plan

Status: Implemented. Automated acceptance uses synthetic calculations and project builds; in-game verification is a separate manual step. The maintained interface and behavior reference is [ProductionArchitecture.md](ProductionArchitecture.md).

This document records the architecture plan agreed during the planning discussion on September 26, 2026. It is an implementation handoff; consult the maintained architecture reference for current behavior.

## 1. Goals and Scope

Introduce a shared production model in UXAssist with two independent calculation entry points:

- **Production planner:** Start from target items and required rates, then calculate upstream production, external material requirements, proliferator consumption, final surplus, and power requirements.
- **Factory black box analyzer:** Start from a fixed building list and calculate material flows and power at full load. Do not replace game production updates or reduce downstream output automatically when upstream supply is insufficient.
- Deliver reusable C# interfaces, a blueprint-selection adapter, and migration of CheatEnabler's existing upstream calculation in v1. Do not add a complete calculator UI or a black box analysis window.
- Do not simulate belt reachability, logistics throughput, startup inventory, or transportation scheduling. Treat identical items inside a black box as shareable.
- Represent material rates as `double` values in items per minute and public power values in watts. Preserve fractional values during calculation and format them only for presentation.

**Default assumption, not explicitly confirmed:** Joint planning may expand an already selected production line to supply another target through its coproducts. It must not introduce an unselected recipe, and any additional primary products must appear in the final surplus report. This was the last unanswered planning question; the recommended option is the default for this handoff.

This document-only task does not include implementation or changes to `AGENTS.md`. Keep implementation documentation in English. After implementation, agent guidance should contain only durable architecture rules or a reference to the maintained architecture document, not this plan's task history.

## 2. Shared Production Model and Interfaces

### 2.1 Data and Lifecycle

- Read recipes, items, buildings, and resources from loaded `LDB` data, Proto objects, and `PrefabDesc`. Read game rules not represented by Proto data, such as proliferation multipliers, from the game's `Cargo` tables. Implement special mechanics against the original game DLL, not the stripped publicized assembly.
- Do not use an external recipe database or copy the reference calculator's data files. The algorithm reference is `DSPCalculator/dsp-calc`; it is not a runtime data dependency.
- Capture immutable data snapshots on the game thread. The calculation core must not access Unity objects, current inventories, or mutable component pools. This keeps it independently testable and permits off-thread calculation after capture.
- Return `DataNotReady` before preload completes and retry through `GameLogic.OnDataLoaded`. Invalidate relevant caches when the catalog is rebuilt, calculation settings change, or a game session ends.
- Return structured diagnostics for missing recipes, building selections, and special operating parameters. Track material-result and power-result completeness separately; never silently represent unknown values as zero.

### 2.2 Public Entry Points

| Interface | Input and responsibility | Output |
| --- | --- | --- |
| `ProductionPlanner.Calculate` | Targets, external-material boundaries, recipe and proliferation policies, and building selections | External material requirements, delivered targets, final surplus, process details, and power |
| `FactoryBlackBoxAnalyzer.Analyze` | Building snapshots, a global proliferation switch, and required special operating parameters | Aggregate flows, per-item shortages and surplus, final products, and power |
| `BlueprintSelectionReader.Capture` | A `PlanetFactory` and the object IDs selected in blueprint mode | Building snapshots without retained mutable game-object references |
| `BlueprintSelectionReader.FromBlueprint` | A `BlueprintBuilding` list and supplementary context | The same building-snapshot format, with missing settings identified explicitly |

The shared report retains `GrossProduction`, `GrossConsumption`, `NetFlow`, and per-building-group details. A report containing only raw materials and final products is insufficient for diagnostics and migration of production statistics.

### 2.3 Normalized Production Processes

Each process records its recipe or special-process identifier, inputs, outputs, building capability, effective proliferation strategy, and power terms. The same recipe with different buildings or strategies produces distinct processes. Do not average unlike processes before evaluating them.

Use a common evaluator for ordinary assembling, smelting, chemical processing, refining, particle processing, and matrix production. Use dedicated adapters for the following mechanics:

| Mechanic | Required behavior |
| --- | --- |
| Fractionation | Separate circulating throughput from net material consumption. Use native probability, proliferation, stacking, throughput, and power rules. Unconverted circulating material is not an external material consumption. |
| Accumulator charging and discharging | Preserve empty/full item conversion, energy accounting, and proliferation inheritance. Charging consumes electricity; report discharge separately rather than treating it as newly generated energy. |
| Photons and ray receivers | Distinguish photon generation from electricity generation and account for lens consumption. Keep Dyson-sphere-side energy requirements separate from factory-grid demand. |
| Mining, water extraction, and collection | Determine outputs from the actual resource type, coverage, or caller-supplied resource configuration. Do not invent vein counts or collection conditions. |
| Fuel generators, ejectors, silos, and research mode | Use native fuel, proliferation, and cycle rules. Count ammunition, fuel, and matrices as consumed items. Report launches and research hashes separately instead of inventing item outputs. |

Provide an extensible process-adapter interface for special mechanics. Report unsupported production buildings rather than silently omitting them.

## 3. Architecture One: Production Planner

### 3.1 Request Configuration

`ProductionPlanRequest` contains:

- **Targets:** Item IDs and required delivery rates per minute. Merge duplicate targets before solving.
- **External-material boundaries:** Allow any intermediate item to be treated as an external input. Dark Fog materials are always external inputs; do not model enemy drops or their conditions.
- **Recipe choices:** Select a primary recipe for each product. Direct production uses only selected recipes, while coproducts from other selected processes can satisfy demand.
- **Proliferation settings:** Whether proliferation is enabled, the proliferator item, whether it is self-sprayed, and per-product choices of no proliferation, speedup, or extra products.
- **Building selections:** Per-recipe choices override per-building-category choices. Do not automatically choose the highest-tier building.
- **Additional configuration:** Known external surplus, operating parameters for special processes, and an optional auxiliary-building list.

When no primary recipe is specified, prefer the valid default production recipe provided by the Proto data. An explicit production recipe for a naturally obtainable resource overrides its default collection source. Explicitly marking an item as an external material stops its upstream expansion.

When proliferation is enabled without a per-product override, ordinary recipes follow the native available mode: extra products when supported, otherwise speedup. An explicit request for an unsupported extra-products mode is a configuration error, not permission to silently select another mode.

### 3.2 Global Material-Balance Solver

Use a global material matrix and linear programming, rather than recursively calculating each target independently and merging the results afterward.

Define:

- `A`: Net input/output matrix for the selected production processes.
- `x`: Process executions per minute.
- `r`: Allowed external material imports per minute.
- `s`: Fixed external surplus supplied by the caller per minute.
- `d`: Target delivery rates per minute.
- `u`: Final surplus rates per minute.

The model satisfies:

`A * x + r + s = d + u`

Process execution rates, external imports, and final surplus are nonnegative. Import variables exist only for items permitted as external materials.

- Include all targets, coproduct reuse, cyclic recipes, and proliferator production in the same model so traversal order cannot change the result.
- Generate only selected recipes and required special processes. Keep separate variables for distinct proliferation or building configurations.
- Use a deterministic v1 objective: first minimize total process executions, then external imports, then remaining surplus. This is not a claim of minimum power, minimum building count, or minimum mineral value.
- Implement an internal pure-managed two-phase simplex solver. Do not introduce a JavaScript runtime or a native solver dependency.
- Include scaling, deterministic pivot selection, an iteration budget, cancellation, and a post-solve conservation check. Return diagnostics on failure rather than an unverified successful result.

### 3.3 Proliferator Self-Spraying

- Derive spray demand from the actual input flows requiring spraying under their production strategies.
- Derive base sprays per bottle and additional sprays after self-spraying from Proto data and native rules. Also deduct the spray consumed by coating that bottle itself.
- Include proliferator manufacturing and its own upstream inputs in the same solver model. Do not retain fixed proliferator ingredient tables or a fixed effective-sprays-per-bottle constant.
- Support optional spraying of delivered target items so the existing belt-signal feature can express its finished-product spraying requirements.
- Exclude initial seed inventory for cyclic processes from steady-state per-minute demand, but keep cyclic processes identifiable in the result.

### 3.4 Power and Results

- Return all external material requirements, delivered targets, final surplus, and the full intermediate production/consumption ledger.
- Evaluate ordinary production power from the selected building's speed, working power, and proliferation power increase. Do not add idle power a second time to an already applicable working-power value.
- Use equivalent fully utilized building counts for the power needed to meet target flow. Report rounded deployment counts and their peak power separately; do not round the material model.
- Without a building selection, still calculate material results that do not depend on that choice, but mark power results incomplete.
- Do not infer sorter or spray-coater counts from recipes without a layout. Include auxiliary power only when the caller supplies the auxiliary-building list, and identify the reported power scope explicitly.
- Exclude logistics transportation and charging. Include accumulator charging used as a production process and report Dyson-sphere-side requirements separately.

## 4. Architecture Two: Factory Black Box Analyzer

### 4.1 Building Lists and Blueprint Capture

- Each production-building snapshot identifies its building type, recipe, actual speedup/extra-products mode, and required speed or special-process settings.
- Decode blueprint-selection settings using the native `BuildingParameters` semantics instead of maintaining an unverified independent parameter-index table.
- Deduplicate and validate object IDs. Support prebuilds and stacked labs. Analyze a prebuild using its known intended completed configuration, and report missing settings explicitly.
- Capture must not refresh, modify, or clear the user's blueprint selection. Do not carry mutable component references into background calculation.

### 4.2 Proliferation and Full-Load Assumptions

- Expose only a global no-proliferation/full-proliferation choice. Do not infer proliferation levels from current input buffers or offer per-building level overrides in v1.
- Determine the maximum level from the capabilities of available proliferator items, not the last index in a multiplier table.
- Preserve each building's actual speedup/extra-products mode.
- Turning the global switch off also disables proliferation effects for fuel, ejectors, silos, and other special consumers.
- When enabled, assume required inputs were sprayed outside the black box. Do not add proliferator demand or a self-spraying production chain. A selected building that manufactures proliferators still contributes its ordinary recipe flows.
- Evaluate all configured processes at full load. A material deficit represents the external supply needed to sustain that capacity; it does not reduce downstream theoretical output.

### 4.3 Balances and the One-Building Surplus Threshold

The black box has fixed recipes and building counts, so it does not need simplex optimization. Reuse the shared process evaluator, then aggregate each building's flows by item:

`NetFlow = GrossProduction - GrossConsumption`

- A negative net flow is the required external supply of that raw material or intermediate item.
- A positive output without any internal consumer is a final product. Do not apply the intermediate-surplus threshold to it.
- For an intermediate item, the surplus threshold is the smallest positive net output of one actual upstream building in the selection. Include its tier, actual proliferation mode, and any consumption of the same item by that process.
- Classify positive intermediate surplus as excess only when it reaches this threshold. A positive balance smaller than one such building's capacity is normally balanced.
- Keep numerical tolerance separate from the business threshold. Preserve all fractional ledger values; classification must not alter the true net flow.
- Retain producer-group and consumer-group attribution so every intermediate balance can be explained. Excess equivalent to one building does not, by itself, establish that the building can safely be removed.

Example: two upstream buildings produce net rates of 60 and 120 items per minute. If internal consumption is 150, the remaining 30 is normally balanced. If consumption is 120, the remaining 60 reaches the threshold and is reported as excess.

### 4.4 Power and Generation

- Evaluate wind, solar, geothermal, and ray-receiver generation in a standard ideal environment: standard environmental multipliers, full illumination, completed continuous-receiving warmup, and sufficient Dyson sphere supply. Do not use current day/night conditions or temporary energy starvation.
- Treat technology, speed settings, fuel, and operating mode as separate parameters, not environmental assumptions. Capture settings that can be determined from the selection and request missing information explicitly.
- Sum rated demand for selected production buildings and non-logistics auxiliary facilities, including sorters and spray coaters. Buildings without a configured production process contribute idle demand.
- Calculate generation using fuel properties and native proliferation rules. Do not apply a single manufacturing-building power multiplier to every generator.
- Report consumption, generation, accumulator charging/discharging capacity, and Dyson-sphere-side requirements separately. A net-power value must not replace the breakdown.
- Exclude logistics stations, distributors, transportation, and mecha charging in v1.

## 5. Implementation, Migration, and Acceptance

### 5.1 Implementation Order

1. Build the immutable catalog, public report types, and shared process evaluator. Cover ordinary recipes and proliferation first.
2. Implement special-process adapters, the planner's matrix compiler, and the solver. Verify cyclic and multi-target conservation.
3. Implement black box aggregation, the single-building surplus threshold, and blueprint-selection adapters.
4. Replace the upstream-source calculation in `CheatEnabler/Patches/Factory/BeltSignalPatch.cs`, starting at `AddSourcesToBeltSignal`. Remove the old recursive algorithm from the active calculation path.
5. Add English architecture documentation and usage examples. After implementation, update `AGENTS.md` only with stable architecture guidance or a documentation reference, not the plan or investigation history.

### 5.2 Existing-Feature Compatibility

- Preserve existing configuration keys, statistics switches, item generation, speed limits, and portal behavior.
- Translate the old policy into a caller-side preset using Proto-validated item identifiers. Do not preserve incorrect hard-coded ingredient lists or incorrect statistics as compatibility requirements.
- Apply production and consumption ledgers independently. A single quantity plus an `isExtra` flag cannot represent all cases. Avoid counting delivered target items twice.
- Cache per-unit results by item and relevant settings, and retain independent fractional accumulators. The game logic tick applies precomputed results; it does not run the solver.
- When data is not ready or calculation fails, suspend the affected additional upstream statistics and report the reason without blocking existing item generation.

### 5.3 Acceptance Scenarios

| Area | Required scenarios |
| --- | --- |
| Ordinary production | Building tiers, recipe durations, extra products, speedup, and nonproductive recipes agree with native data and mechanics. |
| Global balancing | Multiple targets share coproducts; target order does not affect results; intermediate items can be external inputs; cyclic refining, external surplus, and infeasible settings are handled. |
| Self-spraying | Cover each proliferator grade, self-spraying on/off, proliferator manufacturing, and finished-product spraying without duplicate charges. |
| Black box flows | Cover mixed building tiers and modes, shortages at every intermediate stage, the minimum single-building net-capacity threshold, and fractional final-product rates. Shortages do not reduce downstream full-load output. |
| Special power rules | Verify fractionation circulation, accumulator round trips, photon/grid separation, different fuels, ejectors, and research mode. Disabling the global proliferation switch also affects special facilities. |
| Boundaries and migration | Cover empty selections, duplicate/stale IDs, prebuilds, stacked buildings, missing special settings, session transitions, calls before preload, and target statistics without double counting. |

Use an independent check program consistent with the repository's existing check-tool approach. Validate calculations with synthetic game-free scenarios and verify special mechanics against the original game DLL. In-game behavior should be checked separately by a person; parsing real saves is not an acceptance requirement for this calculation infrastructure.

Implementation acceptance runs the UXAssist and CheatEnabler Release builds, the relevant checks, and `git diff --check`. Use the repository's normal build form, `dotnet build <project>/<project>.csproj -c Release --no-restore`. If shared-constant changes affect additional dependent projects, build those projects as well.
