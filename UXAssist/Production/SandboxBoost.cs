using System.Collections.Generic;

namespace UXAssist.Production;

internal static class SandboxBoost
{
    internal const string EnabledKey = "BoostEnabled";

    internal static string ModeKey(ProductionBuilding building)
    {
        switch (building.Kind)
        {
            case ProductionBuildingKind.Ejector:
                return "Mode1";
            case ProductionBuildingKind.Silo:
            case ProductionBuildingKind.FuelGenerator when building.FuelMask == 4:
                return "Mode0";
            default:
                return null;
        }
    }

    // Native paste applies a stored boost mode only with sandbox tools enabled; live captures report the applied state.
    internal static bool TryResolve(IReadOnlyDictionary<string, double> operatingParameters, string modeKey,
        int buildingItemId, out bool boosted, out ProductionDiagnostic diagnostic)
    {
        boosted = false;
        diagnostic = null;
        if (operatingParameters == null || !operatingParameters.TryGetValue(modeKey, out var mode) ||
            mode != 0 && mode != 1 || mode == 1 && !operatingParameters.ContainsKey(EnabledKey))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "Provide the building's native boost mode and whether sandbox boost is active.",
                buildingItemId: buildingItemId);
            return false;
        }

        if (operatingParameters.TryGetValue(EnabledKey, out var enabled) &&
            (enabled != 0 && enabled != 1 || enabled == 1 && mode != 1))
        {
            diagnostic = new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                "The sandbox boost setting conflicts with the building's native boost mode.",
                buildingItemId: buildingItemId);
            return false;
        }

        boosted = enabled == 1;
        return true;
    }
}
