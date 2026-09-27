using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class ResearchProcessAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return recipe == null && building?.Kind == ProductionBuildingKind.Ordinary &&
               building.Category == ProductionRecipeCategory.Research;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (operatingParameters == null ||
            !operatingParameters.TryGetValue("ResearchMode", out var researchMode) || researchMode != 1)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Research mode needs a lab technology and current research speed.",
                buildingItemId: building.ItemId);
            return false;
        }

        var matrixSink = operatingParameters.TryGetValue("ResearchMatrixSink", out var sinkSetting) &&
                         sinkSetting == 1;
        var researchSpeed = 0.0;
        ProductionTechnology technology = null;
        if (matrixSink)
        {
            if (catalog.ResearchMatrixItemIds.Count == 0)
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The catalog has no native research matrix item IDs.",
                    buildingItemId: building.ItemId);
                return false;
            }
        }
        else
        {
            if (!operatingParameters.TryGetValue("TechId", out var techId) || techId <= 0 ||
                techId > int.MaxValue || techId != Math.Truncate(techId) ||
                !operatingParameters.TryGetValue("ResearchSpeed", out researchSpeed) ||
                !FinitePositive(researchSpeed))
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "Research mode needs a lab technology and current research speed.",
                    buildingItemId: building.ItemId);
                return false;
            }

            if (!catalog.Technologies.TryGetValue((int)techId, out technology))
            {
                diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "The selected technology is not a lab-research technology.",
                    buildingItemId: building.ItemId);
                return false;
            }

        }

        if (mode != ProliferationMode.None && mode != ProliferationMode.ExtraProducts)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "Research proliferation creates extra hashes, not faster research cycles.",
                buildingItemId: building.ItemId);
            return false;
        }

        var extraBonus = 0.0;
        var powerMultiplier = 1.0;
        if (mode == ProliferationMode.ExtraProducts &&
            (level <= 0 || !catalog.TryGetProliferation(level, out _, out extraBonus,
                 out powerMultiplier)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The matrix proliferation level is unavailable.", buildingItemId: building.ItemId);
            return false;
        }

        var baseHashes = 3600.0 * researchSpeed;
        var hashRate = baseHashes * (1 + extraBonus);
        var workingWatts = building.WorkingPowerWatts * powerMultiplier;
        if (!FinitePositive(workingWatts) || !matrixSink && !FinitePositive(hashRate))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The research hash rate or power demand is outside the supported range.",
                buildingItemId: building.ItemId);
            return false;
        }

        var inputs = new List<KeyValuePair<int, double>>();
        if (!matrixSink)
        {
            foreach (var requirement in technology.MatrixPointsPerHash)
            {
                if (!catalog.Items.ContainsKey(requirement.Key) || !FinitePositive(requirement.Value) ||
                    !FinitePositive(researchSpeed * requirement.Value))
                {
                    diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                        "The technology has an invalid matrix point requirement.",
                        itemId: requirement.Key, buildingItemId: building.ItemId);
                    return false;
                }

                inputs.Add(new KeyValuePair<int, double>(requirement.Key,
                    researchSpeed * requirement.Value));
            }
        }

        process = new ProductionProcess(0, building.ItemId, mode,
            mode == ProliferationMode.None ? 0 : level, inputs,
            Array.Empty<KeyValuePair<int, double>>(), 1, workingWatts, true,
            researchHashesPerCycle: hashRate);
        return true;
    }

    private static bool FinitePositive(double value)
    {
        return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
