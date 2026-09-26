using System;
using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Production;

public sealed class ProductionBuildingSnapshot
{
    public int ObjectId { get; }
    public int BuildingItemId { get; }
    public int RecipeId { get; }
    public int Count { get; }
    public ProliferationMode ProliferationMode { get; }
    public double? SpeedFactor { get; }
    public IReadOnlyDictionary<string, double> OperatingParameters { get; }

    public ProductionBuildingSnapshot(int objectId, int buildingItemId, int recipeId, int count,
        ProliferationMode proliferationMode, double? speedFactor = null,
        IEnumerable<KeyValuePair<string, double>> operatingParameters = null)
    {
        ObjectId = objectId;
        BuildingItemId = buildingItemId;
        RecipeId = recipeId;
        Count = count;
        ProliferationMode = proliferationMode;
        SpeedFactor = speedFactor;
        OperatingParameters = new System.Collections.ObjectModel.ReadOnlyDictionary<string, double>(
            operatingParameters?.ToDictionary(entry => entry.Key, entry => entry.Value) ??
            new Dictionary<string, double>());
    }
}

public sealed class FactoryBlackBoxRequest
{
    public List<ProductionBuildingSnapshot> Buildings { get; } = new List<ProductionBuildingSnapshot>();
    public List<ProductionDiagnostic> CaptureDiagnostics { get; } = new List<ProductionDiagnostic>();
    public bool ProliferationEnabled { get; set; }
}

public sealed class FactoryBlackBoxAnalyzer
{
    private readonly ProductionCatalog _catalog;
    private readonly ProcessEvaluator _evaluator;

    public FactoryBlackBoxAnalyzer(ProductionCatalog catalog, ProcessEvaluator evaluator = null)
    {
        _catalog = catalog;
        _evaluator = evaluator ?? new ProcessEvaluator();
    }

    public ProductionReport Analyze(FactoryBlackBoxRequest request)
    {
        if (_catalog == null)
            return ProductionReport.Failure(new ProductionDiagnostic(ProductionDiagnosticCode.DataNotReady,
                "Production data is not loaded."));
        if (request == null)
            return ProductionReport.Failure(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "A building selection is required."));

        var diagnostics = new List<ProductionDiagnostic>(request.CaptureDiagnostics);
        var groups = new List<ProductionGroupFlow>();
        var grossProduction = new SortedDictionary<int, double>();
        var grossConsumption = new SortedDictionary<int, double>();
        var producers = new Dictionary<int, List<int>>();
        var consumers = new Dictionary<int, List<int>>();
        var smallestProducer = new Dictionary<int, double>();
        var selectedIds = new HashSet<int>();
        var skippedSelection = diagnostics.Any(diagnostic =>
            diagnostic.Code == ProductionDiagnosticCode.InvalidRequest ||
            diagnostic.Code == ProductionDiagnosticCode.UnknownItem ||
            diagnostic.Code == ProductionDiagnosticCode.DataNotReady);
        var materialComplete = !skippedSelection;
        var powerComplete = !skippedSelection;
        var consumptionWatts = 0.0;
        var generationWatts = 0.0;
        var chargingWatts = 0.0;
        var dischargingWatts = 0.0;
        var dysonRequirementWatts = 0.0;

        foreach (var snapshot in request.Buildings)
        {
            if (snapshot == null || snapshot.Count <= 0)
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "A selected building has an invalid count."));
                materialComplete = false;
                powerComplete = false;
                continue;
            }

            if (snapshot.ObjectId != 0 && !selectedIds.Add(snapshot.ObjectId))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.DuplicateSelection,
                    "A selected building was already included.", buildingItemId: snapshot.BuildingItemId));
                continue;
            }

            if (!_catalog.Buildings.TryGetValue(snapshot.BuildingItemId, out var building))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                    "The selected building is not supported by the catalog.",
                    buildingItemId: snapshot.BuildingItemId));
                materialComplete = false;
                powerComplete = false;
                continue;
            }

            if (snapshot.RecipeId != 0 &&
                snapshot.OperatingParameters.TryGetValue("ResearchMode", out var activeResearchMode) &&
                activeResearchMode > 0)
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "A research-mode lab cannot also run a manufacturing recipe.",
                    recipeId: snapshot.RecipeId, buildingItemId: snapshot.BuildingItemId));
                materialComplete = false;
                powerComplete = false;
                continue;
            }

            if (snapshot.RecipeId == 0 && (building.Kind == ProductionBuildingKind.Ordinary ||
                                           building.Kind == ProductionBuildingKind.Auxiliary))
            {
                if (building.Kind == ProductionBuildingKind.Auxiliary ||
                    !snapshot.OperatingParameters.TryGetValue("ResearchMode", out var researchMode) ||
                    researchMode <= 0)
                {
                    var idleWatts = (building.Kind == ProductionBuildingKind.Auxiliary
                        ? building.WorkingPowerWatts : building.IdlePowerWatts) * snapshot.Count;
                    consumptionWatts += idleWatts;
                    groups.Add(new ProductionGroupFlow(null, snapshot.Count, 0, snapshot.Count, snapshot.Count,
                        idleWatts, idleWatts, Array.Empty<KeyValuePair<int, double>>(),
                        Array.Empty<KeyValuePair<int, double>>()));
                    continue;
                }
            }

            ProductionRecipe recipe = null;
            if (snapshot.RecipeId != 0 && !_catalog.Recipes.TryGetValue(snapshot.RecipeId, out recipe))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.UnknownRecipe,
                    "The selected building's recipe is missing.", recipeId: snapshot.RecipeId,
                    buildingItemId: snapshot.BuildingItemId));
                materialComplete = false;
                powerComplete = false;
                continue;
            }

            if (snapshot.SpeedFactor.HasValue)
                building = new ProductionBuilding(building.ItemId, building.Category, snapshot.SpeedFactor.Value,
                    building.WorkingPowerWatts, building.IdlePowerWatts, building.Kind,
                    building.RatedGenerationWatts, building.ExchangeRateWatts,
                    building.AccumulatorEnergyJoules, building.EmptyAccumulatorItemId,
                    building.FullAccumulatorItemId, building.RenewableSource,
                    building.MinerKind, building.MiningPeriodTicks,
                    building.FuelMask, building.FuelUseWatts,
                    building.PowerProductItemId, building.PowerProductEnergyJoules,
                    building.CatalystMask, building.LaunchChargeTicks,
                    building.LaunchCooldownTicks, building.AmmunitionItemId,
                    building.CollectorSpeedMultiplier);
            var mode = request.ProliferationEnabled
                ? building.Kind == ProductionBuildingKind.Fractionator
                    ? ProliferationMode.Speedup : building.Kind == ProductionBuildingKind.Exchanger &&
                        snapshot.OperatingParameters.TryGetValue("Mode0", out var exchangerMode) && exchangerMode != 0
                        ? ProliferationMode.Speedup : building.Kind == ProductionBuildingKind.FuelGenerator
                            ? ProliferationMode.Speedup : building.Kind == ProductionBuildingKind.RayReceiver
                                ? snapshot.OperatingParameters.TryGetValue("LensItemId", out var lensId) && lensId > 0
                                    ? ProliferationMode.Speedup : ProliferationMode.None
                                : building.Kind == ProductionBuildingKind.Collector
                                    ? ProliferationMode.None
                                : building.Category == ProductionRecipeCategory.Research && snapshot.RecipeId == 0 &&
                                  snapshot.OperatingParameters.TryGetValue("ResearchMode", out var researchModeSetting) &&
                                  researchModeSetting > 0 ? ProliferationMode.ExtraProducts
                                : building.Kind == ProductionBuildingKind.Ejector ||
                                  building.Kind == ProductionBuildingKind.Silo
                                    ? ProliferationMode.Speedup
                                : snapshot.ProliferationMode
                : ProliferationMode.None;
            if (!_evaluator.TryEvaluate(_catalog, recipe, building, mode,
                    mode == ProliferationMode.None ? 0 : _catalog.MaximumProliferationLevel,
                    snapshot.OperatingParameters, out var process, out var diagnostic))
            {
                diagnostics.Add(diagnostic);
                if (building.Kind != ProductionBuildingKind.RenewableGenerator) materialComplete = false;
                powerComplete = false;
                continue;
            }

            if (diagnostic != null) diagnostics.Add(diagnostic);
            if (!process.PowerKnown) powerComplete = false;
            var cycleRate = process.CyclesPerBuildingPerMinute;
            var output = Scale(process.OutputsPerCycle, cycleRate * snapshot.Count);
            var input = Scale(process.InputsPerCycle, cycleRate * snapshot.Count);
            var groupIndex = groups.Count;
            foreach (var flow in output)
            {
                Add(grossProduction, flow.Key, flow.Value);
                AddGroup(producers, flow.Key, groupIndex);
                var perBuildingNet = (process.OutputsPerCycle[flow.Key] -
                    (process.InputsPerCycle.TryGetValue(flow.Key, out var consumed) ? consumed : 0)) * cycleRate;
                if (perBuildingNet > 1e-9 &&
                    (!smallestProducer.TryGetValue(flow.Key, out var previous) || perBuildingNet < previous))
                    smallestProducer[flow.Key] = perBuildingNet;
            }

            foreach (var flow in input)
            {
                Add(grossConsumption, flow.Key, flow.Value);
                AddGroup(consumers, flow.Key, groupIndex);
            }

            var watts = process.WorkingPowerWatts * snapshot.Count;
            consumptionWatts += watts;
            generationWatts += process.GenerationWatts * snapshot.Count;
            chargingWatts += process.AccumulatorChargingWatts * snapshot.Count;
            dischargingWatts += process.AccumulatorDischargingWatts * snapshot.Count;
            dysonRequirementWatts += (process.DysonSphereRequirementWatts ?? 0) * snapshot.Count;
            groups.Add(new ProductionGroupFlow(process, snapshot.Count, cycleRate * snapshot.Count,
                snapshot.Count, snapshot.Count, watts, watts, output, input,
                process.GenerationWatts * snapshot.Count, process.AccumulatorChargingWatts * snapshot.Count,
                process.AccumulatorDischargingWatts * snapshot.Count,
                process.DysonSphereRequirementWatts * snapshot.Count,
                cycleRate * snapshot.Count * process.LaunchesPerCycle,
                cycleRate * snapshot.Count * process.ResearchHashesPerCycle));
        }

        var flows = new List<ProductionItemFlow>();
        var allItems = new SortedSet<int>(grossProduction.Keys);
        allItems.UnionWith(grossConsumption.Keys);
        foreach (var itemId in allItems)
        {
            var produced = grossProduction.TryGetValue(itemId, out var p) ? p : 0;
            var consumed = grossConsumption.TryGetValue(itemId, out var c) ? c : 0;
            var net = produced - consumed;
            var threshold = smallestProducer.TryGetValue(itemId, out var single) ? single : 0;
            var isFinalProduct = produced > 1e-9 && consumed <= 1e-9;
            flows.Add(new ProductionItemFlow(itemId, produced, consumed, 0, 0, 0,
                Math.Max(0, net), Math.Max(0, -net), isFinalProduct,
                !isFinalProduct && consumed > 1e-9 && net > 1e-9 && threshold > 0 &&
                net + 1e-9 >= threshold,
                threshold, producers.TryGetValue(itemId, out var producing) ? producing : Array.Empty<int>(),
                consumers.TryGetValue(itemId, out var usingItem) ? usingItem : Array.Empty<int>()));
        }

        var power = powerComplete ? new ProductionPower(consumptionWatts, consumptionWatts, generationWatts,
            chargingWatts, dischargingWatts, dysonRequirementWatts,
            ProductionPowerScope.SelectedFacilities) : null;
        var status = materialComplete && powerComplete && diagnostics.Count == 0
            ? ProductionStatus.Complete : materialComplete || powerComplete || groups.Count > 0
                ? ProductionStatus.Partial : ProductionStatus.Failed;
        return new ProductionReport(status, materialComplete, powerComplete, flows, groups, diagnostics, power);
    }

    private static Dictionary<int, double> Scale(IReadOnlyDictionary<int, double> flows, double factor)
    {
        return flows.ToDictionary(entry => entry.Key, entry => entry.Value * factor);
    }

    private static void Add(IDictionary<int, double> flows, int itemId, double count)
    {
        flows[itemId] = (flows.TryGetValue(itemId, out var previous) ? previous : 0) + count;
    }

    private static void AddGroup(IDictionary<int, List<int>> groups, int itemId, int index)
    {
        if (!groups.TryGetValue(itemId, out var indices))
        {
            indices = new List<int>();
            groups[itemId] = indices;
        }

        indices.Add(index);
    }
}
