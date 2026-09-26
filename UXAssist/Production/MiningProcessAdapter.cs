using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class MiningProcessAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return recipe == null && building?.Kind == ProductionBuildingKind.Miner;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (building.MiningPeriodTicks <= 0 || building.MinerKind == ProductionMinerKind.None)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "The mining building has no native period or resource type.", buildingItemId: building.ItemId);
            return false;
        }

        if (operatingParameters == null ||
            !operatingParameters.TryGetValue("ResourceItemId", out var outputId) ||
            !ValidItemId(outputId) || !catalog.Items.ContainsKey((int)outputId) ||
            !operatingParameters.TryGetValue("MiningSpeedMultiplier", out var miningSpeed) ||
            !FinitePositive(miningSpeed))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the resource item and mining technology speed.", buildingItemId: building.ItemId);
            return false;
        }

        var resourceFactor = 1.0;
        if (building.MinerKind == ProductionMinerKind.Vein &&
            (!operatingParameters.TryGetValue("VeinCount", out resourceFactor) ||
             resourceFactor != Math.Truncate(resourceFactor) || !FinitePositive(resourceFactor)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the number of covered veins.", buildingItemId: building.ItemId);
            return false;
        }

        if (building.MinerKind == ProductionMinerKind.Oil &&
            (!operatingParameters.TryGetValue("OilUnits", out resourceFactor) ||
             !FinitePositive(resourceFactor)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the covered oil well's native flow factor.", buildingItemId: building.ItemId);
            return false;
        }

        var machineSpeed = 1.0;
        if (operatingParameters.TryGetValue("MachineSpeedFactor", out var suppliedSpeed))
        {
            if (!FinitePositive(suppliedSpeed))
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "The miner speed factor must be positive.", buildingItemId: building.ItemId);
                return false;
            }

            machineSpeed = suppliedSpeed;
        }

        var speedDamper = 1.0;
        if (operatingParameters.TryGetValue("SpeedDamper", out var suppliedDamper))
        {
            if (!FinitePositive(suppliedDamper))
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "The miner speed damper must be positive.", buildingItemId: building.ItemId);
                return false;
            }

            speedDamper = suppliedDamper;
        }

        var perMinute = ProductionUnits.TicksPerMinute * ProductionUnits.FixedPointScale /
                        building.MiningPeriodTicks * miningSpeed * machineSpeed * speedDamper * resourceFactor;
        var powerRatio = speedDamper * machineSpeed * machineSpeed;
        var workingWatts = building.WorkingPowerWatts * powerRatio +
                           building.IdlePowerWatts * (1 - powerRatio);
        if (!FinitePositive(perMinute) || workingWatts < 0 ||
            double.IsNaN(workingWatts) || double.IsInfinity(workingWatts))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The mineral output rate or power demand is outside the supported range.",
                buildingItemId: building.ItemId);
            return false;
        }

        process = new ProductionProcess(0, building.ItemId, ProliferationMode.None, 0,
            Array.Empty<KeyValuePair<int, double>>(),
            new[] { new KeyValuePair<int, double>((int)outputId, 1) },
            perMinute, workingWatts, true);
        return true;
    }

    private static bool ValidItemId(double value)
    {
        return value > 0 && value <= int.MaxValue && value == Math.Truncate(value);
    }

    private static bool FinitePositive(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
