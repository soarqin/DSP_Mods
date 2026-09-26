using System;
using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Production;

public sealed class FractionationProcessAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return recipe?.Category == ProductionRecipeCategory.Fractionate;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (recipe.Inputs.Count != 1 || recipe.Outputs.Count != 1 ||
            !FinitePositive(recipe.Inputs.Values.Single()) ||
            !FinitePositive(recipe.Outputs.Values.Single()) ||
            mode != ProliferationMode.None && mode != ProliferationMode.Speedup)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "Fractionation requires one input, one output, and the native acceleration mode.",
                recipeId: recipe.Id);
            return false;
        }

        if (building != null && building.Kind != ProductionBuildingKind.Fractionator)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "The selected building cannot fractionate items.", recipeId: recipe.Id,
                buildingItemId: building.ItemId);
            return false;
        }

        var speedBonus = 0.0;
        var powerMultiplier = 1.0;
        if (mode == ProliferationMode.Speedup &&
            (level <= 0 || !catalog.TryGetProliferation(level, out speedBonus, out _, out powerMultiplier)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The fractionator's proliferation level is unavailable.", recipeId: recipe.Id);
            return false;
        }

        var throughput = 0.0;
        if (building != null)
        {
            if (operatingParameters == null ||
                !operatingParameters.TryGetValue("CirculatingItemsPerMinute", out throughput) ||
                !operatingParameters.TryGetValue("StackSize", out var stackSize) ||
                !FinitePositive(throughput) || !FinitePositive(stackSize) ||
                stackSize > 4 || throughput > stackSize * 30 * ProductionUnits.SecondsPerMinute + 1e-9)
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "Provide circulating items per minute and a valid stack size for the fractionator.",
                    recipeId: recipe.Id, buildingItemId: building.ItemId);
                return false;
            }
        }

        var input = recipe.Inputs.Single();
        var output = recipe.Outputs.Single();
        var probability = Math.Min(1, output.Value / input.Value * (1 + speedBonus));
        var throughputPerSecond = throughput / ProductionUnits.SecondsPerMinute;
        var watts = building == null ? 0 : building.WorkingPowerWatts *
            (1 + Math.Max(0, throughputPerSecond - 30) * 0.05) * powerMultiplier;
        process = new ProductionProcess(recipe.Id, building?.ItemId ?? 0, mode,
            mode == ProliferationMode.None ? 0 : level,
            new[] { new KeyValuePair<int, double>(input.Key, probability) },
            new[] { new KeyValuePair<int, double>(output.Key, probability) },
            throughput, watts, building != null, throughput);
        return true;
    }

    private static bool FinitePositive(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
