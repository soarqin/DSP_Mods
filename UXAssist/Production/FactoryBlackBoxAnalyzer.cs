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
        var processGroups = new List<BlackBoxProcessGroup>();
        var grossProduction = new SortedDictionary<int, double>();
        var grossConsumption = new SortedDictionary<int, double>();
        var producers = new Dictionary<int, List<int>>();
        var consumers = new Dictionary<int, List<int>>();
        var smallestProducer = new Dictionary<int, double>();
        var hasResearchMatrixSink = false;
        var uncertainDemandItems = new HashSet<int>();
        var selectedIds = new HashSet<int>();
        var skippedSelection = diagnostics.Any(diagnostic =>
            diagnostic.Code == ProductionDiagnosticCode.InvalidRequest ||
            diagnostic.Code == ProductionDiagnosticCode.UnknownItem ||
            diagnostic.Code == ProductionDiagnosticCode.DataNotReady);
        var unknownDemand = skippedSelection;
        var materialComplete = !skippedSelection;
        var factoryPowerComplete = !skippedSelection;
        var logisticsPowerComplete = !skippedSelection;
        var totalPowerKnown = !skippedSelection;
        var factoryConsumptionWatts = 0.0;
        var logisticsConsumptionWatts = 0.0;
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
                factoryPowerComplete = false;
                logisticsPowerComplete = false;
                unknownDemand = true;
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
                factoryPowerComplete = false;
                logisticsPowerComplete = false;
                unknownDemand = true;
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
                factoryPowerComplete = false;
                logisticsPowerComplete = false;
                unknownDemand = true;
                continue;
            }

            if (snapshot.RecipeId == 0 && building.Kind == ProductionBuildingKind.Logistics)
            {
                double? logisticsWatts = null;
                if (!snapshot.OperatingParameters.TryGetValue("ChargePowerWatts", out var chargePowerWatts))
                    diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                        "The selected logistics facility needs its configured charging power.",
                        buildingItemId: snapshot.BuildingItemId));
                else if (chargePowerWatts < 0 || double.IsNaN(chargePowerWatts) ||
                         double.IsInfinity(chargePowerWatts * snapshot.Count))
                    diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                        "The selected logistics charging power must be finite and nonnegative.",
                        buildingItemId: snapshot.BuildingItemId));
                else
                    logisticsWatts = chargePowerWatts * snapshot.Count;

                if (logisticsWatts.HasValue) logisticsConsumptionWatts += logisticsWatts.Value;
                else logisticsPowerComplete = false;
                groups.Add(new ProductionGroupFlow(null, snapshot.Count, 0, snapshot.Count, snapshot.Count,
                    logisticsWatts, logisticsWatts, Array.Empty<KeyValuePair<int, double>>(),
                    Array.Empty<KeyValuePair<int, double>>()));
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
                    factoryConsumptionWatts += idleWatts;
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
                factoryPowerComplete = false;
                unknownDemand = true;
                continue;
            }

            if (snapshot.SpeedFactor.HasValue) building = building.WithSpeedFactor(snapshot.SpeedFactor.Value);
            var mode = request.ProliferationEnabled
                ? FullProliferationMode(building, snapshot) : ProliferationMode.None;
            if (IsResearchModeLab(building, snapshot) && _catalog.MaximumProliferationLevel == 0)
                mode = ProliferationMode.None;
            var researchMatrixSink = IsResearchModeLab(building, snapshot) &&
                snapshot.OperatingParameters.TryGetValue("ResearchMatrixSink", out var sinkSetting) &&
                sinkSetting == 1 && _catalog.ResearchMatrixItemIds.Count > 0;
            if (researchMatrixSink) hasResearchMatrixSink = true;
            if (!_evaluator.TryEvaluate(_catalog, recipe, building, mode,
                    mode == ProliferationMode.None ? 0 : _catalog.MaximumProliferationLevel,
                    snapshot.OperatingParameters, out var process, out var diagnostic))
            {
                diagnostics.Add(diagnostic);
                if (building.Kind != ProductionBuildingKind.RenewableGenerator && !researchMatrixSink)
                    materialComplete = false;
                if (recipe != null) uncertainDemandItems.UnionWith(recipe.Inputs.Keys);
                else if (building.Kind != ProductionBuildingKind.Miner &&
                         building.Kind != ProductionBuildingKind.Collector &&
                         building.Kind != ProductionBuildingKind.RenewableGenerator && !researchMatrixSink)
                    unknownDemand = true;
                if (TryGetConsumptionWithoutMaterials(building, snapshot, diagnostic, mode,
                        out var knownWatts))
                {
                    if (!IsResearchModeLab(building, snapshot)) totalPowerKnown = false;
                    factoryConsumptionWatts += knownWatts;
                    groups.Add(new ProductionGroupFlow(null, snapshot.Count, 0, snapshot.Count, snapshot.Count,
                        knownWatts, knownWatts, Array.Empty<KeyValuePair<int, double>>(),
                        Array.Empty<KeyValuePair<int, double>>()));
                }
                else
                {
                    totalPowerKnown = false;
                    factoryPowerComplete = false;
                }
                continue;
            }

            if (diagnostic != null) diagnostics.Add(diagnostic);
            if (!process.PowerKnown) totalPowerKnown = false;
            if (process.WorkingPowerWatts < 0 || double.IsNaN(process.WorkingPowerWatts) ||
                double.IsInfinity(process.WorkingPowerWatts * snapshot.Count)) factoryPowerComplete = false;
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
            factoryConsumptionWatts += watts;
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
            processGroups.Add(new BlackBoxProcessGroup(process, snapshot));
        }

        var matrixSinkItems = hasResearchMatrixSink
            ? new HashSet<int>(_catalog.ResearchMatrixItemIds) : new HashSet<int>();
        var classifications = BlackBoxMaterialClassifier.Classify(processGroups, grossProduction, grossConsumption,
            materialComplete, uncertainDemandItems, unknownDemand, matrixSinkItems, out var steadyStateBalance);
        var flows = new List<ProductionItemFlow>();
        var allItems = new SortedSet<int>(grossProduction.Keys);
        allItems.UnionWith(grossConsumption.Keys);
        foreach (var itemId in allItems)
        {
            var produced = grossProduction.TryGetValue(itemId, out var p) ? p : 0;
            var consumed = grossConsumption.TryGetValue(itemId, out var c) ? c : 0;
            var net = produced - consumed;
            var threshold = smallestProducer.TryGetValue(itemId, out var single) ? single : 0;
            var classification = classifications[itemId];
            var isFinalProduct = classification.IsFinalProduct;
            flows.Add(new ProductionItemFlow(itemId, produced, consumed, 0, 0, 0,
                Math.Max(0, net), Math.Max(0, -net), isFinalProduct,
                classification.IsIntermediate && !matrixSinkItems.Contains(itemId) &&
                net > 1e-9 && threshold > 0 &&
                net + 1e-9 >= threshold,
                threshold, producers.TryGetValue(itemId, out var producing) ? producing : Array.Empty<int>(),
                consumers.TryGetValue(itemId, out var usingItem) ? usingItem : Array.Empty<int>(),
                classification.IntermediateShortage, classification.OverbuildSurplus,
                classification.CoproductSurplus,
                steadyStateBalance == null ? null : Math.Max(0, -steadyStateBalance[itemId]),
                classification.IsResearchProduct));
        }

        var powerComplete = factoryPowerComplete && logisticsPowerComplete && totalPowerKnown;
        var consumptionWatts = factoryConsumptionWatts + logisticsConsumptionWatts;
        var power = powerComplete ? new ProductionPower(consumptionWatts, consumptionWatts, generationWatts,
            chargingWatts, dischargingWatts, dysonRequirementWatts,
            ProductionPowerScope.SelectedFacilities) : null;
        var status = materialComplete && powerComplete && diagnostics.Count == 0
            ? ProductionStatus.Complete : materialComplete || powerComplete || groups.Count > 0
                ? ProductionStatus.Partial : ProductionStatus.Failed;
        return new ProductionReport(status, materialComplete, powerComplete, flows, groups, diagnostics, power,
            new ProductionPowerBreakdown(factoryConsumptionWatts, logisticsConsumptionWatts,
                factoryPowerComplete, logisticsPowerComplete));
    }

    private static ProliferationMode FullProliferationMode(ProductionBuilding building,
        ProductionBuildingSnapshot snapshot)
    {
        var parameters = snapshot.OperatingParameters;
        switch (building.Kind)
        {
            case ProductionBuildingKind.Fractionator:
            case ProductionBuildingKind.FuelGenerator:
            case ProductionBuildingKind.Ejector:
            case ProductionBuildingKind.Silo:
                return ProliferationMode.Speedup;
            case ProductionBuildingKind.Exchanger:
                return parameters.TryGetValue("Mode0", out var exchangerMode) && exchangerMode != 0
                    ? ProliferationMode.Speedup : snapshot.ProliferationMode;
            case ProductionBuildingKind.RayReceiver:
                return parameters.TryGetValue("LensItemId", out var lensId) && lensId > 0
                    ? ProliferationMode.Speedup : ProliferationMode.None;
            case ProductionBuildingKind.Collector:
                return ProliferationMode.None;
            default:
                return building.Category == ProductionRecipeCategory.Research && snapshot.RecipeId == 0 &&
                       parameters.TryGetValue("ResearchMode", out var researchMode) && researchMode > 0
                    ? ProliferationMode.ExtraProducts : snapshot.ProliferationMode;
        }
    }

    private bool TryGetConsumptionWithoutMaterials(ProductionBuilding building,
        ProductionBuildingSnapshot snapshot, ProductionDiagnostic diagnostic, ProliferationMode mode,
        out double watts)
    {
        watts = 0;
        if (IsResearchModeLab(building, snapshot))
        {
            var multiplier = 1.0;
            if (mode != ProliferationMode.None)
            {
                if (mode != ProliferationMode.ExtraProducts) return false;
                if (_catalog.MaximumProliferationLevel > 0 &&
                    !_catalog.TryGetProliferation(_catalog.MaximumProliferationLevel,
                        out _, out _, out multiplier)) return false;
            }

            watts = building.WorkingPowerWatts * multiplier * snapshot.Count;
            return watts > 0 && !double.IsNaN(watts) && !double.IsInfinity(watts);
        }

        if (diagnostic?.Code != ProductionDiagnosticCode.MissingOperatingParameter) return false;
        if (building.Kind == ProductionBuildingKind.Collector) return true;
        if (building.Kind != ProductionBuildingKind.Miner) return false;

        var speed = snapshot.OperatingParameters.TryGetValue("MachineSpeedFactor", out var capturedSpeed)
            ? capturedSpeed : 1;
        if (speed <= 0 || double.IsNaN(speed) || double.IsInfinity(speed)) return false;
        var ratio = speed * speed;
        watts = (building.WorkingPowerWatts * ratio + building.IdlePowerWatts * (1 - ratio)) * snapshot.Count;
        return watts >= 0 && !double.IsNaN(watts) && !double.IsInfinity(watts);
    }

    private static bool IsResearchModeLab(ProductionBuilding building, ProductionBuildingSnapshot snapshot)
    {
        return building.Kind == ProductionBuildingKind.Ordinary &&
               building.Category == ProductionRecipeCategory.Research && snapshot.RecipeId == 0 &&
               snapshot.OperatingParameters.TryGetValue("ResearchMode", out var researchMode) && researchMode == 1;
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
