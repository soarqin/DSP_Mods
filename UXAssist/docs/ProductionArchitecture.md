# Production Calculation Architecture

UXAssist exposes a shared production catalog, a material planner, a fixed-building black box analyzer, and blueprint capture. These APIs calculate reports; they do not change DSP production, replace factory updates, or provide a calculator UI.

## Data and lifecycle

- `ProductionCatalogService.Current` is an immutable snapshot of loaded item, recipe, technology, building, `PrefabDesc`, and `Cargo` data. The service rebuilds after data preload and on game begin, so changes made by later preload callbacks are captured. It clears the snapshot when a game ends. It can be `null` before preload. Subscribe to `ProductionCatalogService.Changed` to invalidate derived reports and retry against the current snapshot; the event also reports clearing the catalog.
- Capture a live blueprint selection on the game thread. The planner, black box analyzer, adapters, and simplex implementation read only immutable snapshots and managed request data. They do not retain game component references or inspect current inventories during calculation.
- Material rates use `double` items per minute; process executions, hashes, and launches are also per minute. Native simulation runs at 60 ticks per second, so recipe cycle rates use 3,600 ticks per minute, not 60. Convert fixed-point speed values by 10,000 and energy per tick to watts by 60. Gas speeds arrive in items per second and must also be converted to per-minute outputs. Round only for display and for the separately reported building deployment count.
- A report carries separate `MaterialComplete` and `PowerComplete` flags, `Diagnostics`, individual `ItemFlows`, and `Groups`. `ProductionPower` is `null` if any power requirement is unknown; a group's unknown Dyson-sphere demand is also `null`. Do not interpret an absent power result as zero.

Accumulator charging and critical photon production have no native `RecipeProto` entries. The catalog adds calculation-only recipes from the captured exchanger and receiver metadata, with stable IDs equal to the negative output item ID. `ProductionRecipe.IsSynthetic` identifies these entries. Resolve them through `ProductionCatalog.Recipes`, never through `LDB.recipes` or a native building recipe setter. They supply default routes only when no native recipe provides one.

## Global production planning

Select a target and, if desired, a building for the recipe or its category:

```csharp
var request = new ProductionPlanRequest();
request.Targets.Add(new ProductionTarget(itemId, 60));
request.BuildingByCategory[ProductionRecipeCategory.Assemble] = assemblerItemId;
request.ExternalMaterials.Add(rawResourceItemId);

var report = new ProductionPlanner(ProductionCatalogService.Current).Calculate(request);
if (report.MaterialComplete)
{
    var rawImportsPerMinute = report.ItemFlows[rawResourceItemId].ExternalImports;
}
```

The solver includes all selected recipes, coproducts, finite external supplies, permitted imports, and proliferator consumption in one material balance. It never silently chooses another recipe. Explicit boundaries and Dark Fog materials are external; natural items are external unless an explicit recipe overrides their default collection source. Without a building choice, material flows that do not depend on that choice remain available, but building power is incomplete. An explicit extra-products mode for a recipe without that native capability is an error.

When proliferation is enabled, ordinary recipes default to their native extra-products or acceleration capability. Fractionation and accumulator exchange use acceleration; a ray receiver uses acceleration only with a lens. Per-item choices can override these defaults when the process supports the selected mode.

`SelfSprayProliferator` uses the captured spray count and extra-products bonus to calculate effective sprays per produced proliferator, including the spray spent coating the proliferator itself. `SprayDeliveredItems` adds spraying for requested deliveries. Accumulators retain their coating during both charging and discharging: adapters expose `PreservedSpraysPerCycle`, and the planner credits that coating only against demand for the same item. Each credit is bounded by both preserved output and sprayed input or delivery demand, so coating is neither created nor charged twice. Credits reduce proliferator consumption, not material quantities. The solver minimizes process executions, then imports, maximizes valid spray reuse, and finally minimizes remaining surplus.

`GrossProduction` and `GrossConsumption` are independent ledgers. `NetFlow` is their difference; imports, known supply, target delivery, and surplus remain separate fields. `Power.ConsumptionWatts` uses equivalent fully utilized buildings, whereas `PeakConsumptionWatts` uses rounded deployment counts. Fixed layout power is not inferred: add explicitly selected sorters, spray coaters, or other non-logistics auxiliary devices to `request.AuxiliaryBuildings`. The power scope then changes from `ProductionBuildings` to `ProductionBuildingsAndAuxiliary`.

## Fixed-building analysis

The black box evaluates each selected building at full load, even if another selected building cannot provide all its inputs. It does not simulate belt layouts, startup storage, or logistics throughput. Missing inputs appear as `RequiredExternalSupply`; byproducts and internal deficits retain producer and consumer group attribution. A positive intermediate balance is classified as excess only when it reaches the smallest net per-building output among actual producers of that item. The underlying fractional ledger is never discarded.

```csharp
var capture = BlueprintSelectionReader.Capture(factory, selectedObjectIds);
var selection = capture.CreateRequest(proliferationEnabled: true);
var report = new FactoryBlackBoxAnalyzer(ProductionCatalogService.Current).Analyze(selection);
```

`Capture` accepts existing entity IDs and negative prebuild IDs. Its optional third argument is a `BlueprintSelectionContext`; use `OperatingParametersByObjectId` to supply missing settings keyed by those same signed IDs. `FromBlueprint(blueprintBuildings, context)` captures a blueprint without live game objects, decodes settings through the native blueprint-paste path, and uses `OperatingParametersByIndex`, keyed by blueprint index. Supplied settings override captured values before validation. In particular, fractionator circulation and stack size must describe the intended full-load input, not transient inventory contents; `FluidItemId` can identify the input when the fractionator has not been filled. Transient runtime throttles, such as a miner's output backlog damper, are never captured.

The capture is read-only and does not alter the game's selection. `CreateRequest` carries capture diagnostics into the analyzer, so stale or unrecognized selected objects cannot turn into a falsely complete empty report. Duplicate selected IDs are diagnosed and counted once. Assembler proliferation modes are decoded from the native parameter array; lab modes use their native mode field. Nonproductive recipes use native acceleration rather than an invalid extra-products mode.

The global proliferation switch uses the highest level supported by the captured proliferator items while preserving each production building's acceleration or extra-products setting. It also controls fuel and launch-ammunition proliferation. The black box assumes input coating is supplied externally and does not add proliferator consumption for that coating.

## Special-process settings

All parameters below are numbers in a building snapshot's `OperatingParameters`. A live capture supplies available values from the selected factory; either capture path may need supplementary context. Do not infer missing resource coverage, inventory contents, research technology, orbit readiness, or sandbox settings. Planner callers supply recipe-specific values through `OperatingParametersByRecipe`, including for synthetic recipes.

| Facility | Settings and behavior |
| --- | --- |
| Fractionator | A fractionation recipe, `CirculatingItemsPerMinute`, and `StackSize`. The throughput limit is 30 cargo per second times the stack size, or `1800 * StackSize` items per minute. Only successful conversions consume inputs; circulation changes throughput and power without becoming an external material input. The native power formula uses items per second, not per minute. |
| Energy exchanger | `Mode0`: charging `1`, discharging `-1`, idle `0`. Charging converts empty accumulators to full ones and draws grid power. Discharging converts them back and is reported as discharge capacity, not generation. Coating accelerates both directions and is preserved on the output accumulator. |
| Ray receiver | `Mode0`: `0` for grid generation or its native photon item ID; `LensItemId`: zero or a compatible catalyst; `SolarEnergyLossRate`: technology loss fraction. Missing loss leaves photon flows available but Dyson power incomplete. Without a selected receiver, the planner keeps lens-free photon flows and leaves power incomplete; lens wear and speedup require a receiver. Ideal full illumination and warmup use the native 2.5 warmup factor, eightfold photon mode energy, and one lens per 36,000 game ticks. Grid generation and Dyson demand are separate. |
| Renewable generator | Wind, solar, and geothermal use ideal standard conditions and their rated generation. Local wind strength, day/night illumination, and geothermal ground heat do not modify this full-load model. |
| Miner or water extractor | `ResourceItemId`, `MiningSpeedMultiplier`, and `VeinCount` or `OilUnits` for the matching resource. Optional `MachineSpeedFactor` defaults to 1; live capture records the raw miner speed divided by 10,000. The native speed damper only throttles backed-up output, so full load assumes its unobstructed value of 1. Output scales by machine speed; power uses `ratio = MachineSpeedFactor * MachineSpeedFactor` and `workingWatts * ratio + idleWatts * (1 - ratio)`. |
| Gas collector | `GasCount`, `GasTotalHeat`, `MiningSpeedMultiplier`, and each `GasItemId{n}` and `GasSpeedPerSecond{n}`. Native self-power recovery and collection technology scale all gas outputs, which are reported per minute. Collector working energy reduces recovered gas; it is not counted again as grid consumption. |
| Fuel generator | `FuelItemId` compatible with the generator. Artificial stars also require the native boost mode `Mode0`, with `BoostEnabled` when it is set. Output and burned fuel follow the native fuel type and proliferation rule. |
| EM-rail ejector or vertical launching silo | `LaunchAvailable` plus the native boost mode (`Mode1` for ejectors, `Mode0` for silos), with `BoostEnabled` when it is set. An available ideal target consumes one ammunition item per completed charge/cooldown cycle. Proliferation changes both launch speed and working power. `LaunchesPerMinute` is separate from item production. |
| Research-mode lab | `ResearchMode = 1`, `TechId`, and `ResearchSpeed`. The captured technology supplies matrix point requirements. Native matrix proliferation increases hashes and power, not matrix input per base hash; `ResearchHashesPerMinute` is separate from item production. |

Native blueprint paste applies a stored boost mode only while sandbox tools are enabled, so `BoostEnabled` states whether the boost is actually applied. Live capture reports the applied boost of ejectors, silos, and artificial stars; blueprint callers must supply it for boosted buildings.

The ejector and silo assume a usable orbit or node when `LaunchAvailable = 1`; their native geometric visibility and construction schedules are outside this full-load model. Power reports exclude logistics stations, distributors, transport, and mecha charging. Auxiliary power must come from an explicitly supplied list.

## CheatEnabler migration

The belt-signal generator uses `ProductionPlanner` material reports, not its former recursive ingredient table. Its caller-side preset chooses a proliferator with valid captured Proto capabilities and permits legacy natural-source boundaries only when those item IDs exist in the catalog. For each generated target unit, it records upstream gross production plus external imports, subtracts the already delivered target, and applies gross consumption independently. Each belt keeps separate fractional progress for production and consumption, while per-unit reports are cached by item, spray configuration, and catalog. `ProductionCatalogService.Changed` invalidates cached reports and refreshes existing belts; clearing the catalog drops their derived statistics without accessing unloaded factory objects. The game logic tick applies cached rates only. Request construction, calculation, and report conversion fail softly: if data is missing or calculation fails, only additional upstream statistics pause and a warning explains why; item generation and its own configured count switch remain unchanged.

## Validation

Use compilation, the synthetic game-free checks, and static review for development checks:

```powershell
dotnet build UXAssist/UXAssist.csproj -c Release --no-restore
dotnet build CheatEnabler/CheatEnabler.csproj -c Release --no-restore
dotnet run --project UXAssist/tools/ProductionCheck/ProductionCheck.csproj -c Release
git diff --check
```

`ProductionCheck` compiles the pure-managed production sources and the belt-signal statistics with hand-built catalogs. Update its expectations together with any intended change to production rules.

Compare special mechanics against method bodies in the original DSP DLL; the publicized repository reference is not an implementation source. Builds and DLL inspection do not establish in-game correctness. In-game acceptance is manual; automated tests and save parsing are not required.

Use a test game for the following manual checks. Compare full-load rates only with sufficient inputs, power, and output capacity; do not compare a throttled statistics window with theoretical capacity.

- Check ordinary production and mining against native cycle periods and the per-minute UI. A 60-tick recipe at unit speed completes 60 cycles per minute before proliferation.
- Capture assemblers in acceleration and extra-products modes, nonproductive recipes, prebuilds, and stacked labs. Confirm that each captured mode and building count matches the selection.
- Supply fractionator circulation and stack context. For native 1% fractionation with fully sprayed input, 1,800 circulating items per minute yield 36 conversions per minute; at stack size 4, 7,200 circulating items per minute yield 144. Check throughput-dependent power separately.
- Request full accumulators and critical photons through the catalog's default synthetic routes. Confirm that missing building or receiver-loss settings produce incomplete power rather than an invented result.
- Charge and discharge sprayed accumulators, then consume or deliver them with spraying enabled. Confirm acceleration in both directions and no repeated spray charge for inherited coating. Compare self-spraying enabled and disabled.
- Select miners at different machine speeds, including one with a backed-up output, a gas collector, and renewable generators. Check native miner power scaling, full-load miner output regardless of the output backlog, per-minute gas output without added grid demand, and ideal rated renewable power rather than local conditions.
- Select ray receivers in both modes, fuel generators, ejectors, and silos. Toggle proliferation and compare working power, generation, accumulator exchange, and Dyson demand as separate quantities. In a sandbox game, capture boosted artificial stars and launchers, and confirm that blueprint analysis of boosted buildings requires `BoostEnabled`.
- Remove an upstream producer from a mixed production selection. Confirm that downstream capacity is unchanged and the missing material appears as required external supply. Check intermediate excess around the smallest selected producer's net output threshold.
- Combine planner targets with shared coproducts, explicit raw-material boundaries, and alternate selected recipes. Confirm material conservation, shared surplus reuse, and no introduction of unselected recipes.
- Toggle belt-signal proliferation and reload a game. Confirm that existing belts use the new catalog, item generation continues, and fractional production and consumption statistics remain independent.
