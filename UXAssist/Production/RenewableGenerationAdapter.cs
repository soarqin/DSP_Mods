using System;
using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class RenewableGenerationAdapter : IProductionProcessAdapter
{
    public bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building)
    {
        return recipe == null && building?.Kind == ProductionBuildingKind.RenewableGenerator;
    }

    public bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic)
    {
        process = null;
        diagnostic = null;
        if (building.RenewableSource == RenewablePowerSource.None ||
            building.RatedGenerationWatts < 0 || double.IsNaN(building.RatedGenerationWatts) ||
            double.IsInfinity(building.RatedGenerationWatts))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.UnsupportedProcess,
                "The generator has no renewable power rating.", buildingItemId: building.ItemId);
            return false;
        }

        process = new ProductionProcess(0, building.ItemId, ProliferationMode.None, 0,
            Array.Empty<KeyValuePair<int, double>>(), Array.Empty<KeyValuePair<int, double>>(),
            1, building.WorkingPowerWatts, true,
            generationWatts: building.RatedGenerationWatts);
        return true;
    }
}
