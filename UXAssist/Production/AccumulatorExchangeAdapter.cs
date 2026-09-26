using System;
using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Production;

public sealed class AccumulatorExchangeAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return recipe?.Category == ProductionRecipeCategory.Exchange ||
               recipe == null && building?.Kind == ProductionBuildingKind.Exchanger;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (building != null && building.Kind != ProductionBuildingKind.Exchanger)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "This building cannot charge or discharge accumulators.", recipeId: recipe?.Id ?? 0,
                buildingItemId: building.ItemId);
            return false;
        }

        if (mode != ProliferationMode.None && mode != ProliferationMode.Speedup)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "Accumulator exchange supports native speedup only.", recipeId: recipe?.Id ?? 0);
            return false;
        }

        var speedBonus = 0.0;
        if (mode != ProliferationMode.None &&
            (level <= 0 || !catalog.TryGetProliferation(level, out speedBonus, out _, out _)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The accumulator proliferation level is unavailable.", recipeId: recipe?.Id ?? 0);
            return false;
        }

        var state = 0;
        if (recipe != null)
        {
            if (recipe.Inputs.Count != 1 || recipe.Outputs.Count != 1 ||
                recipe.Inputs.Single().Value != 1 || recipe.Outputs.Single().Value != 1)
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                    "Accumulator exchange requires one empty/full item conversion.", recipeId: recipe.Id);
                return false;
            }

            var inputId = recipe.Inputs.Single().Key;
            var outputId = recipe.Outputs.Single().Key;
            state = building == null || inputId == building.EmptyAccumulatorItemId &&
                outputId == building.FullAccumulatorItemId ? 1 :
                inputId == building.FullAccumulatorItemId &&
                outputId == building.EmptyAccumulatorItemId ? -1 : 0;
            if (state == 0)
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "The exchanger's accumulator items do not match the recipe.", recipeId: recipe.Id);
                return false;
            }
        }
        else if (operatingParameters == null ||
                 !operatingParameters.TryGetValue("Mode0", out var selectedMode) ||
                 double.IsNaN(selectedMode) || double.IsInfinity(selectedMode) ||
                 selectedMode != Math.Truncate(selectedMode) ||
                 selectedMode < -1 || selectedMode > 1)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the exchanger's native charging, discharging, or idle mode.",
                buildingItemId: building?.ItemId ?? 0);
            return false;
        }
        else
        {
            state = (int)selectedMode;
        }

        if (recipe != null && operatingParameters != null &&
            operatingParameters.TryGetValue("Mode0", out var requestedMode) && requestedMode != state)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The exchanger's operating mode conflicts with the selected recipe.", recipeId: recipe.Id);
            return false;
        }

        var inputItem = recipe?.Inputs.Single().Key ??
                        (state == 1 ? building.EmptyAccumulatorItemId : building.FullAccumulatorItemId);
        var outputItem = recipe?.Outputs.Single().Key ??
                         (state == 1 ? building.FullAccumulatorItemId : building.EmptyAccumulatorItemId);
        if (state != 0 && (inputItem <= 0 || outputItem <= 0))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The exchanger's accumulator items are unavailable.", buildingItemId: building?.ItemId ?? 0);
            return false;
        }

        if (building != null && state != 0 &&
            (!FinitePositive(building.ExchangeRateWatts) || !FinitePositive(building.AccumulatorEnergyJoules)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The exchanger's energy capacity is unavailable.", buildingItemId: building.ItemId);
            return false;
        }

        var effectiveWatts = building == null || state == 0 ? 0 : building.ExchangeRateWatts * (1 + speedBonus);
        var cycles = building == null ? 0 : state == 0 ? 1 :
            effectiveWatts * 60 / building.AccumulatorEnergyJoules;
        var inputs = state == 0 ? Array.Empty<KeyValuePair<int, double>>() :
            new[] { new KeyValuePair<int, double>(inputItem, 1) };
        var outputs = state == 0 ? Array.Empty<KeyValuePair<int, double>>() :
            new[] { new KeyValuePair<int, double>(outputItem, 1) };
        process = new ProductionProcess(recipe?.Id ?? 0, building?.ItemId ?? 0,
            state == 0 ? ProliferationMode.None : mode, state == 0 ? 0 : level,
            inputs, outputs, cycles, state == 1 ? effectiveWatts : building?.IdlePowerWatts ?? 0,
            building != null, accumulatorChargingWatts: state == 1 ? effectiveWatts : 0,
            accumulatorDischargingWatts: state == -1 ? effectiveWatts : 0,
            sprayedInputsPerCycle: state == 0 ? 0 : 1,
            preservedSpraysPerCycle: mode == ProliferationMode.None
                ? Array.Empty<KeyValuePair<int, double>>() : outputs);
        return true;
    }

    private static bool FinitePositive(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
