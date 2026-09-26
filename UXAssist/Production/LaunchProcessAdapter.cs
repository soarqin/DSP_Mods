using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class LaunchProcessAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return building?.Kind == ProductionBuildingKind.Ejector || building?.Kind == ProductionBuildingKind.Silo;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (recipe != null)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "Launchers consume ammunition without producing a recipe item.", recipeId: recipe.Id,
                buildingItemId: building.ItemId);
            return false;
        }

        if (mode != ProliferationMode.None && mode != ProliferationMode.Speedup)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "Launch ammunition supports native speedup only.", buildingItemId: building.ItemId);
            return false;
        }

        if (operatingParameters == null ||
            !operatingParameters.TryGetValue("LaunchAvailable", out var launchAvailable) ||
            (launchAvailable != 0 && launchAvailable != 1))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Specify whether a usable orbit or Dyson-sphere node is available.",
                buildingItemId: building.ItemId);
            return false;
        }

        if (launchAvailable == 0)
        {
            process = new ProductionProcess(0, building.ItemId, ProliferationMode.None, 0,
                Array.Empty<KeyValuePair<int, double>>(), Array.Empty<KeyValuePair<int, double>>(),
                1, building.IdlePowerWatts, true);
            return true;
        }

        if (building.LaunchChargeTicks <= 0 || building.LaunchCooldownTicks <= 0 ||
            building.AmmunitionItemId <= 0 || !catalog.Items.ContainsKey(building.AmmunitionItemId))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "The launcher's native ammunition or cycle timings are unavailable.",
                buildingItemId: building.ItemId);
            return false;
        }

        if (!SandboxBoost.TryResolve(operatingParameters, SandboxBoost.ModeKey(building), building.ItemId,
                out var boosted, out diagnostic))
            return false;

        var speedBonus = 0.0;
        var powerMultiplier = 1.0;
        if (mode != ProliferationMode.None &&
            (level <= 0 || !catalog.TryGetProliferation(level, out speedBonus, out _, out powerMultiplier)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The launch ammunition's proliferation level is unavailable.",
                buildingItemId: building.ItemId);
            return false;
        }

        var nativeStep = Math.Truncate(10000 * (1 + speedBonus) + 0.1) * (boosted ? 10 : 1);
        if (nativeStep <= 0 || double.IsInfinity(nativeStep) || double.IsNaN(nativeStep))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The launcher's native speed is outside the supported range.", buildingItemId: building.ItemId);
            return false;
        }

        var cycleTicks = Math.Ceiling(building.LaunchChargeTicks * 10000.0 / nativeStep) +
                         Math.Ceiling(building.LaunchCooldownTicks * 10000.0 / nativeStep);
        var launchesPerMinute = 3600.0 / cycleTicks;
        if (launchesPerMinute <= 0 || double.IsInfinity(launchesPerMinute) ||
            double.IsNaN(launchesPerMinute))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The launcher cycle is outside the supported range.", buildingItemId: building.ItemId);
            return false;
        }

        process = new ProductionProcess(0, building.ItemId, mode,
            mode == ProliferationMode.None ? 0 : level,
            new[] { new KeyValuePair<int, double>(building.AmmunitionItemId, 1) },
            Array.Empty<KeyValuePair<int, double>>(), launchesPerMinute,
            building.WorkingPowerWatts * powerMultiplier, true, launchesPerCycle: 1);
        return true;
    }
}
