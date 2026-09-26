using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class GasCollectionAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return building?.Kind == ProductionBuildingKind.Collector;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (recipe != null || mode != ProliferationMode.None)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "Gas collection has no recipe or native proliferation mode.",
                recipeId: recipe?.Id ?? 0, buildingItemId: building.ItemId);
            return false;
        }

        if (operatingParameters == null ||
            !operatingParameters.TryGetValue("GasCount", out var gasCount) ||
            gasCount < 1 || gasCount > 16 || gasCount != Math.Truncate(gasCount) ||
            !operatingParameters.TryGetValue("GasTotalHeat", out var totalHeat) ||
            !FiniteNonnegative(totalHeat) ||
            !operatingParameters.TryGetValue("MiningSpeedMultiplier", out var miningSpeed) ||
            !FinitePositive(miningSpeed) || building.CollectorSpeedMultiplier <= 0 ||
            !FiniteNonnegative(building.WorkingPowerWatts))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the planet's gas composition, total fuel energy, mining speed and collector speed.",
                buildingItemId: building.ItemId);
            return false;
        }

        var workCost = building.WorkingPowerWatts / building.CollectorSpeedMultiplier;
        var recovery = totalHeat == 0 ? 1 : 1 - workCost / totalHeat;
        if (recovery == 0) recovery = 1;
        var techRate = totalHeat - workCost <= 0 ? 1 :
            (miningSpeed * totalHeat - workCost) / (totalHeat - workCost);
        if (!FinitePositive(recovery) || !FinitePositive(techRate))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The selected gas flow cannot sustain the collector's native energy consumption.",
                buildingItemId: building.ItemId);
            return false;
        }

        var outputs = new List<KeyValuePair<int, double>>();
        for (var index = 0; index < (int)gasCount; index++)
        {
            if (!operatingParameters.TryGetValue($"GasItemId{index}", out var itemId) ||
                itemId <= 0 || itemId > int.MaxValue || itemId != Math.Truncate(itemId) ||
                !catalog.Items.ContainsKey((int)itemId) ||
                !operatingParameters.TryGetValue($"GasSpeedPerSecond{index}", out var gasSpeed) ||
                !FiniteNonnegative(gasSpeed))
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "Each collected gas needs a valid item ID and native gas speed.",
                    buildingItemId: building.ItemId);
                return false;
            }

            var rate = gasSpeed * ProductionUnits.SecondsPerMinute *
                       building.CollectorSpeedMultiplier * recovery * techRate;
            if (!FiniteNonnegative(rate))
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "A gas production rate is outside the supported range.",
                    itemId: (int)itemId, buildingItemId: building.ItemId);
                return false;
            }

            if (rate > 0) outputs.Add(new KeyValuePair<int, double>((int)itemId, rate));
        }

        process = new ProductionProcess(0, building.ItemId, ProliferationMode.None, 0,
            Array.Empty<KeyValuePair<int, double>>(), outputs,
            1, 0, true);
        return true;
    }

    private static bool FiniteNonnegative(double value)
    {
        return value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool FinitePositive(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
