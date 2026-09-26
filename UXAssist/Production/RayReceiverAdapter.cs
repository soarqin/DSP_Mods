using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class RayReceiverAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return building?.Kind == ProductionBuildingKind.RayReceiver ||
               recipe?.Category == ProductionRecipeCategory.PhotonStore;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (building == null)
            return TryEvaluateWithoutReceiver(recipe, mode, operatingParameters, out process, out diagnostic);

        if (building.Kind != ProductionBuildingKind.RayReceiver)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "The selected building cannot receive Dyson-sphere rays.", recipeId: recipe?.Id ?? 0,
                buildingItemId: building.ItemId);
            return false;
        }

        if (building.RatedGenerationWatts <= 0 || !Finite(building.RatedGenerationWatts) ||
            (recipe != null && (recipe.Category != ProductionRecipeCategory.PhotonStore ||
                                recipe.Inputs.Count != 0 || recipe.Outputs.Count != 1 ||
                                !recipe.Outputs.ContainsKey(building.PowerProductItemId))))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.IncompatibleBuilding,
                "The receiver's native photon product or power rating does not match the recipe.",
                recipeId: recipe?.Id ?? 0, buildingItemId: building.ItemId);
            return false;
        }

        var photonMode = recipe != null;
        if (operatingParameters == null)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the receiver's operating settings.", buildingItemId: building.ItemId);
            return false;
        }

        var hasMode = operatingParameters.TryGetValue("Mode0", out var selectedMode);
        if (!hasMode && !photonMode ||
            hasMode && (selectedMode != 0 && selectedMode != building.PowerProductItemId ||
                        photonMode && selectedMode != building.PowerProductItemId))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Select the receiver's grid or native photon operating mode.",
                recipeId: recipe?.Id ?? 0, buildingItemId: building.ItemId);
            return false;
        }

        if (recipe == null && selectedMode > 0) photonMode = true;
        if (photonMode && (building.PowerProductItemId <= 0 ||
                           building.PowerProductEnergyJoules <= 0 ||
                           !Finite(building.PowerProductEnergyJoules) ||
                           !catalog.Items.ContainsKey(building.PowerProductItemId)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The ray receiver has no valid native photon product or required photon energy.",
                buildingItemId: building.ItemId);
            return false;
        }

        if (!operatingParameters.TryGetValue("LensItemId", out var lensId) ||
            lensId < 0 || lensId > int.MaxValue || lensId != Math.Truncate(lensId))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the receiver's lens item ID, or zero for no lens.", buildingItemId: building.ItemId);
            return false;
        }

        ProductionItem lens = null;
        if (lensId > 0 && (!catalog.Items.TryGetValue((int)lensId, out lens) ||
                           (lens.CatalystTypeMask & building.CatalystMask) == 0 ||
                           lens.CatalystAbilityMultiplier <= 0 || !Finite(lens.CatalystAbilityMultiplier)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The selected item is not a compatible ray-receiver lens.",
                itemId: (int)lensId, buildingItemId: building.ItemId);
            return false;
        }

        if (mode != ProliferationMode.None && mode != ProliferationMode.Speedup ||
            mode == ProliferationMode.Speedup && lens == null)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "Only an installed lens can receive native ray-receiver speedup.", buildingItemId: building.ItemId);
            return false;
        }

        var speedBonus = 0.0;
        if (mode != ProliferationMode.None &&
            (level <= 0 || !catalog.TryGetProliferation(level, out speedBonus, out _, out _)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProliferation,
                "The lens's proliferation level is unavailable.", buildingItemId: building.ItemId);
            return false;
        }

        var energyWatts = building.RatedGenerationWatts * 2.5 *
                          (lens?.CatalystAbilityMultiplier ?? 1) *
                          (1 + speedBonus) * (photonMode ? 8 : 1);
        var cyclesPerMinute = photonMode
            ? energyWatts * 60 / building.PowerProductEnergyJoules : 1;
        if (cyclesPerMinute <= 0 || !Finite(cyclesPerMinute) || !Finite(energyWatts))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The receiver's native output is outside the supported range.", buildingItemId: building.ItemId);
            return false;
        }

        var lossKnown = operatingParameters.TryGetValue("SolarEnergyLossRate", out var solarLoss);
        if (lossKnown && (solarLoss < 0 || solarLoss >= 1 || !Finite(solarLoss)))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "Solar energy loss must be a finite fraction below one.", buildingItemId: building.ItemId);
            return false;
        }

        if (!lossKnown)
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The Dyson-sphere power requirement needs the current solar energy loss technology setting.",
                buildingItemId: building.ItemId);

        var lensFlow = lens == null ? Array.Empty<KeyValuePair<int, double>>() :
            new[] { new KeyValuePair<int, double>(lens.Id, 0.1 / cyclesPerMinute) };
        var productFlow = photonMode
            ? new[] { new KeyValuePair<int, double>(building.PowerProductItemId, 1) }
            : Array.Empty<KeyValuePair<int, double>>();
        process = new ProductionProcess(recipe?.Id ?? 0, building.ItemId, mode,
            mode == ProliferationMode.None ? 0 : level, lensFlow, productFlow,
            cyclesPerMinute, building.WorkingPowerWatts, true,
            generationWatts: photonMode ? 0 : energyWatts,
            dysonSphereRequirementWatts: lossKnown ? energyWatts / (1 - solarLoss * 0.6) : (double?)null,
            powerKnown: lossKnown);
        return true;
    }

    // Photon output per cycle does not depend on the receiver, but lens wear, speedup, and power do.
    private static bool TryEvaluateWithoutReceiver(ProductionRecipe recipe, ProliferationMode mode,
        IReadOnlyDictionary<string, double> operatingParameters, out ProductionProcess process,
        out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (recipe.Inputs.Count != 0 || recipe.Outputs.Count != 1)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "A photon recipe must have one output and no inputs.", recipeId: recipe.Id);
            return false;
        }

        if (mode != ProliferationMode.None ||
            operatingParameters != null && operatingParameters.TryGetValue("LensItemId", out var lensId) &&
            lensId != 0)
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingBuilding,
                "Select a ray receiver to evaluate lens consumption and speedup.", recipeId: recipe.Id);
            return false;
        }

        process = new ProductionProcess(recipe.Id, 0, ProliferationMode.None, 0,
            Array.Empty<KeyValuePair<int, double>>(), recipe.Outputs, 0, 0, false);
        return true;
    }

    private static bool Finite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
