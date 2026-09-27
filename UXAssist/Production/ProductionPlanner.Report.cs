using System;
using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Production;

public sealed partial class ProductionPlanner
{
    private ProductionReport BuildReport(ProductionPlanRequest request, IDictionary<int, double> targets, int[] rows,
        ProductionProcess[] processes, int[] importItems, double[] solution, int importStart, int surplusStart,
        int proliferatorItemId, double spraysPerBottle, List<ProductionDiagnostic> warnings,
        int[] reusableSprayItems, int sprayCreditStart)
    {
        var remainingSprayCredits = new Dictionary<int, double>();
        for (var index = 0; index < reusableSprayItems.Length; index++)
            remainingSprayCredits[reusableSprayItems[index]] = solution[sprayCreditStart + index];
        var grossProduction = new Dictionary<int, double>();
        var grossConsumption = new Dictionary<int, double>();
        var producers = new Dictionary<int, List<int>>();
        var consumers = new Dictionary<int, List<int>>();
        var groups = new List<ProductionGroupFlow>();
        var activeRecipes = new HashSet<int>();
        var powerComplete = true;
        var factoryPowerComplete = true;
        var workingPower = 0.0;
        var peakPower = 0.0;
        var generationWatts = 0.0;
        var chargingWatts = 0.0;
        var dischargingWatts = 0.0;
        var dysonRequirementWatts = 0.0;
        for (var index = 0; index < processes.Length; index++)
        {
            var executions = solution[index];
            if (executions <= 1e-9) continue;
            var process = processes[index];
            activeRecipes.Add(process.RecipeId);
            var output = ScaleFlows(process.OutputsPerCycle, executions);
            var input = ScaleFlows(process.InputsPerCycle, executions);
            if (proliferatorItemId > 0 && process.ProliferationMode != ProliferationMode.None &&
                process.SprayedInputsPerCycle > 0)
            {
                var sprays = process.SprayedInputsPerCycle * executions;
                foreach (var itemId in reusableSprayItems)
                {
                    var reused = Math.Min(remainingSprayCredits[itemId],
                        SprayedInputAmount(process, itemId) * executions);
                    remainingSprayCredits[itemId] -= reused;
                    sprays -= reused;
                }

                if (sprays > 0) AddFlow(input, proliferatorItemId, sprays / spraysPerBottle);
            }

            var groupIndex = groups.Count;
            foreach (var flow in output)
            {
                AddFlow(grossProduction, flow.Key, flow.Value);
                AddGroup(producers, flow.Key, groupIndex);
            }

            foreach (var flow in input)
            {
                AddFlow(grossConsumption, flow.Key, flow.Value);
                AddGroup(consumers, flow.Key, groupIndex);
            }

            double? equivalentBuildings = null;
            int? deploymentCount = null;
            double? consumedWatts = null;
            double? peakWatts = null;
            if (process.HasBuilding && process.CyclesPerBuildingPerMinute > 0)
            {
                equivalentBuildings = executions / process.CyclesPerBuildingPerMinute;
                if (!process.PowerKnown) powerComplete = false;
                if (equivalentBuildings.Value > int.MaxValue || double.IsInfinity(equivalentBuildings.Value))
                {
                    powerComplete = false;
                    factoryPowerComplete = false;
                    warnings.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                        "The rounded deployment count exceeds the supported range.", recipeId: process.RecipeId));
                }
                else
                {
                    deploymentCount = Math.Max(1, (int)Math.Ceiling(equivalentBuildings.Value - 1e-9));
                    consumedWatts = equivalentBuildings.Value * process.WorkingPowerWatts;
                    peakWatts = deploymentCount.Value * process.WorkingPowerWatts;
                    if (consumedWatts.Value < 0 || double.IsNaN(consumedWatts.Value) ||
                        double.IsInfinity(consumedWatts.Value)) factoryPowerComplete = false;
                    workingPower += consumedWatts.Value;
                    peakPower += peakWatts.Value;
                    generationWatts += equivalentBuildings.Value * process.GenerationWatts;
                    chargingWatts += equivalentBuildings.Value * process.AccumulatorChargingWatts;
                    dischargingWatts += equivalentBuildings.Value * process.AccumulatorDischargingWatts;
                    dysonRequirementWatts += equivalentBuildings.Value * (process.DysonSphereRequirementWatts ?? 0);
                }
            }
            else
            {
                powerComplete = false;
                factoryPowerComplete = false;
                if (!warnings.Any(warning => warning.RecipeId == process.RecipeId &&
                                             (warning.Code == ProductionDiagnosticCode.MissingBuilding ||
                                              warning.Code == ProductionDiagnosticCode.IncompatibleBuilding)))
                    warnings.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingBuilding,
                        "Select a building to calculate power for this recipe.", recipeId: process.RecipeId));
            }

            groups.Add(new ProductionGroupFlow(process, equivalentBuildings ?? 0, executions, equivalentBuildings,
                deploymentCount, consumedWatts, peakWatts, output, input,
                equivalentBuildings * process.GenerationWatts,
                equivalentBuildings * process.AccumulatorChargingWatts,
                equivalentBuildings * process.AccumulatorDischargingWatts,
                equivalentBuildings * process.DysonSphereRequirementWatts,
                executions * process.LaunchesPerCycle,
                executions * process.ResearchHashesPerCycle));
        }

        var auxiliaryIds = new HashSet<int>();
        foreach (var auxiliary in request.AuxiliaryBuildings)
        {
            if (auxiliary == null || auxiliary.Count <= 0 || auxiliary.RecipeId != 0)
            {
                powerComplete = false;
                factoryPowerComplete = false;
                warnings.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "Auxiliary power requires a positive building count and no production recipe."));
                continue;
            }

            if (auxiliary.ObjectId != 0 && !auxiliaryIds.Add(auxiliary.ObjectId))
            {
                warnings.Add(new ProductionDiagnostic(ProductionDiagnosticCode.DuplicateSelection,
                    "An auxiliary building was selected more than once.",
                    buildingItemId: auxiliary.BuildingItemId));
                continue;
            }

            if (!_catalog.Buildings.TryGetValue(auxiliary.BuildingItemId, out var building) ||
                building.Kind != ProductionBuildingKind.Auxiliary ||
                building.WorkingPowerWatts < 0 || double.IsInfinity(building.WorkingPowerWatts) ||
                double.IsNaN(building.WorkingPowerWatts) ||
                double.IsInfinity(building.WorkingPowerWatts * auxiliary.Count))
            {
                powerComplete = false;
                factoryPowerComplete = false;
                warnings.Add(new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                    "The auxiliary building has no rated working power.",
                    buildingItemId: auxiliary.BuildingItemId));
                continue;
            }

            var watts = building.WorkingPowerWatts * auxiliary.Count;
            workingPower += watts;
            peakPower += watts;
            var process = new ProductionProcess(0, building.ItemId, ProliferationMode.None, 0,
                Array.Empty<KeyValuePair<int, double>>(), Array.Empty<KeyValuePair<int, double>>(),
                1, building.WorkingPowerWatts, true);
            groups.Add(new ProductionGroupFlow(process, auxiliary.Count, 0, auxiliary.Count,
                auxiliary.Count, watts, watts, Array.Empty<KeyValuePair<int, double>>(),
                Array.Empty<KeyValuePair<int, double>>()));
        }

        if (proliferatorItemId > 0 && request.SprayDeliveredItems.Count > 0)
        {
            var sprays = request.SprayDeliveredItems.Sum(itemId => targets[itemId]);
            foreach (var itemId in reusableSprayItems)
            {
                if (request.SprayDeliveredItems.Contains(itemId)) sprays -= remainingSprayCredits[itemId];
            }

            if (sprays > 0) AddFlow(grossConsumption, proliferatorItemId, sprays / spraysPerBottle);
        }

        var imports = new Dictionary<int, double>();
        for (var index = 0; index < importItems.Length; index++)
            imports[importItems[index]] = solution[importStart + index];

        var itemFlows = new List<ProductionItemFlow>();
        for (var index = 0; index < rows.Length; index++)
        {
            var itemId = rows[index];
            var produced = grossProduction.TryGetValue(itemId, out var production) ? production : 0;
            var consumed = grossConsumption.TryGetValue(itemId, out var consumption) ? consumption : 0;
            var imported = imports.TryGetValue(itemId, out var externalImport) ? externalImport : 0;
            var known = request.KnownExternalSurplus.TryGetValue(itemId, out var knownSupply) ? knownSupply : 0;
            var delivered = targets.TryGetValue(itemId, out var target) ? target : 0;
            var surplus = solution[surplusStart + index];
            var finalProduct = produced > 1e-9 && consumed <= 1e-9;
            itemFlows.Add(new ProductionItemFlow(itemId, produced, consumed, imported, known, delivered, surplus,
                imported, finalProduct, produced > 1e-9 && consumed > 1e-9 && surplus > 1e-9,
                0, producers.TryGetValue(itemId, out var producing) ? producing : Array.Empty<int>(),
                consumers.TryGetValue(itemId, out var usingItem) ? usingItem : Array.Empty<int>()));
        }

        var diagnostics = warnings.Where(warning => warning.RecipeId == 0 || activeRecipes.Contains(warning.RecipeId))
            .ToList();
        var power = powerComplete ? new ProductionPower(workingPower, peakPower, generationWatts,
            chargingWatts, dischargingWatts, dysonRequirementWatts,
            request.AuxiliaryBuildings.Count == 0 ? ProductionPowerScope.ProductionBuildings :
                ProductionPowerScope.ProductionBuildingsAndAuxiliary) : null;
        var status = powerComplete && diagnostics.Count == 0 ? ProductionStatus.Complete : ProductionStatus.Partial;
        return new ProductionReport(status, true, powerComplete, itemFlows, groups, diagnostics, power,
            new ProductionPowerBreakdown(workingPower, 0, factoryPowerComplete, true));
    }

    private static Dictionary<int, double> ScaleFlows(IReadOnlyDictionary<int, double> flows, double multiplier)
    {
        return flows.ToDictionary(flow => flow.Key, flow => flow.Value * multiplier);
    }

    private static void AddGroup(IDictionary<int, List<int>> groups, int itemId, int groupIndex)
    {
        if (!groups.TryGetValue(itemId, out var indices))
        {
            indices = new List<int>();
            groups[itemId] = indices;
        }

        indices.Add(groupIndex);
    }
}
