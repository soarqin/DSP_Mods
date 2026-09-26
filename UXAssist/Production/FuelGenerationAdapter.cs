using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class FuelGenerationAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return recipe == null && building?.Kind == ProductionBuildingKind.FuelGenerator;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (operatingParameters == null ||
            !operatingParameters.TryGetValue("FuelItemId", out var fuelId) ||
            fuelId <= 0 || fuelId > int.MaxValue || fuelId != Math.Truncate(fuelId) ||
            !catalog.Items.TryGetValue((int)fuelId, out var fuel) ||
            fuel.HeatValueJoules <= 0 || (fuel.FuelTypeMask & building.FuelMask) == 0)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Select a fuel item compatible with the generator's native fuel mask.",
                buildingItemId: building.ItemId);
            return false;
        }

        if (mode != ProliferationMode.None && mode != ProliferationMode.Speedup &&
            mode != ProliferationMode.ExtraProducts ||
            mode == ProliferationMode.ExtraProducts && !fuel.ProductiveFuel)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The fuel cannot use the requested proliferation mode.", buildingItemId: building.ItemId);
            return false;
        }

        if (building.FuelUseWatts <= 0 || building.RatedGenerationWatts <= 0 ||
            double.IsNaN(building.FuelUseWatts) || double.IsNaN(building.RatedGenerationWatts))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "The generator's fuel consumption or power rating is unavailable.",
                buildingItemId: building.ItemId);
            return false;
        }

        var speedBonus = 0.0;
        var extraBonus = 0.0;
        if (mode != ProliferationMode.None &&
            (level <= 0 || !catalog.TryGetProliferation(level, out speedBonus, out extraBonus, out _)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The fuel's proliferation level is unavailable.", buildingItemId: building.ItemId);
            return false;
        }

        var boostFactor = 1.0;
        var boostModeKey = SandboxBoost.ModeKey(building);
        if (boostModeKey != null)
        {
            if (!SandboxBoost.TryResolve(operatingParameters, boostModeKey, building.ItemId, out var boosted,
                    out diagnostic))
                return false;

            // Artificial stars apply the sandbox boost and the strange annihilation fuel multiplier natively.
            boostFactor = (boosted ? 100 : 1) * fuel.StarOutputMultiplier;
        }

        var proliferationFactor = mode == ProliferationMode.None ? 1 :
            fuel.ProductiveFuel ? 1 + extraBonus : 1 + speedBonus;
        var generation = building.RatedGenerationWatts * proliferationFactor * boostFactor;
        var burnedFuel = building.FuelUseWatts * (fuel.ProductiveFuel ? 1 : proliferationFactor) *
                         boostFactor * 60 / fuel.HeatValueJoules;
        if (double.IsInfinity(generation) || double.IsInfinity(burnedFuel) || burnedFuel <= 0 ||
            double.IsNaN(generation) || double.IsNaN(burnedFuel))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The selected fuel produces an invalid generation rate.", buildingItemId: building.ItemId);
            return false;
        }

        process = new ProductionProcess(0, building.ItemId,
            mode == ProliferationMode.None ? mode : fuel.ProductiveFuel
                ? ProliferationMode.ExtraProducts : ProliferationMode.Speedup,
            mode == ProliferationMode.None ? 0 : level,
            new[] { new KeyValuePair<int, double>(fuel.Id, 1) },
            Array.Empty<KeyValuePair<int, double>>(), burnedFuel,
            building.WorkingPowerWatts, true, generationWatts: generation);
        return true;
    }
}
