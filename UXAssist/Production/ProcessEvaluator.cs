using System;
using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Production;

public sealed class ProcessEvaluator
{
    private readonly IProductionProcessAdapter[] _adapters;

    public ProcessEvaluator(IEnumerable<IProductionProcessAdapter> adapters = null)
    {
        _adapters = (adapters ?? Array.Empty<IProductionProcessAdapter>())
            .Concat(new IProductionProcessAdapter[]
            {
                new FractionationProcessAdapter(),
                new AccumulatorExchangeAdapter(),
                new RenewableGenerationAdapter(),
                new MiningProcessAdapter(),
                new FuelGenerationAdapter(),
                new RayReceiverAdapter(),
                new LaunchProcessAdapter(),
                new GasCollectionAdapter(),
                new ResearchProcessAdapter()
            }).ToArray();
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (catalog == null)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.DataNotReady, "Production data is not loaded.");
            return false;
        }

        foreach (var adapter in _adapters)
        {
            if (adapter.CanEvaluate(recipe, building))
                return adapter.TryEvaluate(catalog, recipe, building, mode, level, operatingParameters,
                    out process, out diagnostic);
        }

        if (recipe == null)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "No process adapter supports the selected building without a recipe.",
                buildingItemId: building?.ItemId ?? 0);
            return false;
        }

        if (!IsOrdinary(recipe.Category))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "The recipe requires a special-process adapter.", recipeId: recipe.Id,
                buildingItemId: building?.ItemId ?? 0);
            return false;
        }

        if (building != null && (building.Category != recipe.Category || !FinitePositive(building.SpeedFactor) ||
                                 !FiniteNonnegative(building.WorkingPowerWatts)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "The selected building cannot execute this recipe.", recipeId: recipe.Id,
                buildingItemId: building.ItemId);
            return false;
        }

        if (recipe.TimeTicks <= 0 || !ValidFlows(recipe.Inputs) || !ValidFlows(recipe.Outputs) ||
            !Enum.IsDefined(typeof(ProliferationMode), mode))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The recipe or proliferation mode is invalid.", recipeId: recipe.Id);
            return false;
        }

        var speedBonus = 0.0;
        var extraBonus = 0.0;
        var powerMultiplier = 1.0;
        if (mode != ProliferationMode.None)
        {
            if (recipe.Inputs.Count == 0 || level < 1 ||
                !catalog.TryGetProliferation(level, out speedBonus, out extraBonus,
                    out powerMultiplier) ||
                mode == ProliferationMode.ExtraProducts && !recipe.Productive)
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                    "The selected proliferation mode or level is unavailable for this recipe.", recipeId: recipe.Id);
                return false;
            }
        }

        var outputMultiplier = mode == ProliferationMode.ExtraProducts ? 1 + extraBonus : 1;
        var cycleMultiplier = mode == ProliferationMode.Speedup ? 1 + speedBonus : 1;
        var cycles = building == null ? 0 :
            ProductionUnits.TicksPerMinute * building.SpeedFactor / recipe.TimeTicks * cycleMultiplier;
        var power = building == null ? 0 : building.WorkingPowerWatts * powerMultiplier;
        process = new ProductionProcess(recipe.Id, building?.ItemId ?? 0, mode,
            mode == ProliferationMode.None ? 0 : level, recipe.Inputs,
            recipe.Outputs.Select(output => new KeyValuePair<int, double>(output.Key, output.Value * outputMultiplier)),
            cycles, power, building != null);
        return true;
    }

    private static bool IsOrdinary(ProductionRecipeCategory category)
    {
        return category == ProductionRecipeCategory.Assemble || category == ProductionRecipeCategory.Smelt ||
               category == ProductionRecipeCategory.Chemical || category == ProductionRecipeCategory.Refine ||
               category == ProductionRecipeCategory.Particle || category == ProductionRecipeCategory.Research;
    }

    private static bool ValidFlows(IReadOnlyDictionary<int, double> flows)
    {
        return flows.All(flow => flow.Key > 0 && flow.Value > 0 && !double.IsNaN(flow.Value) &&
                                 !double.IsInfinity(flow.Value));
    }

    private static bool FinitePositive(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool FiniteNonnegative(double value)
    {
        return value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
