using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UXAssist.Production;

internal sealed class BlackBoxProcessGroup
{
    public ProductionProcess Process { get; }
    public string StableKey { get; }
    public int Count { get; }
    public IReadOnlyDictionary<int, double> NetPerBuilding { get; }
    public bool HasJointProducts { get; }

    public BlackBoxProcessGroup(ProductionProcess process, ProductionBuildingSnapshot snapshot)
    {
        Process = process;
        Count = snapshot.Count;
        var net = new Dictionary<int, double>();
        foreach (var flow in process.OutputsPerCycle)
            net[flow.Key] = flow.Value * process.CyclesPerBuildingPerMinute;
        foreach (var flow in process.InputsPerCycle)
            net[flow.Key] = (net.TryGetValue(flow.Key, out var previous) ? previous : 0) -
                            flow.Value * process.CyclesPerBuildingPerMinute;
        NetPerBuilding = net;
        HasJointProducts = net.Values.Count(value => value > BlackBoxMaterialClassifier.Tolerance) > 1;

        var key = new StringBuilder();
        key.Append(process.RecipeId).Append(':').Append(process.BuildingItemId).Append(':')
            .Append((int)process.ProliferationMode).Append(':').Append(process.ProliferationLevel)
            .Append(':').Append(snapshot.SpeedFactor?.ToString("R", CultureInfo.InvariantCulture) ?? "-");
        foreach (var parameter in snapshot.OperatingParameters.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            key.Append('|').Append(parameter.Key.Length).Append(':').Append(parameter.Key).Append(':')
                .Append(parameter.Value.ToString("R", CultureInfo.InvariantCulture));
        StableKey = key.ToString();
    }
}

internal sealed class BlackBoxItemClassification
{
    public bool IsFinalProduct { get; }
    public bool IsResearchProduct { get; }
    public bool IsIntermediate { get; }
    public double IntermediateShortage { get; }
    public double? OverbuildSurplus { get; }
    public double? CoproductSurplus { get; }

    public BlackBoxItemClassification(bool isFinalProduct, bool isResearchProduct, bool isIntermediate,
        double intermediateShortage, double? overbuildSurplus, double? coproductSurplus)
    {
        IsFinalProduct = isFinalProduct;
        IsResearchProduct = isResearchProduct;
        IsIntermediate = isIntermediate;
        IntermediateShortage = intermediateShortage;
        OverbuildSurplus = overbuildSurplus;
        CoproductSurplus = coproductSurplus;
    }
}

internal static class BlackBoxMaterialClassifier
{
    internal const double Tolerance = 1e-9;

    private sealed class ReductionGroup
    {
        public BlackBoxProcessGroup Source { get; }
        public int Count { get; }
        public int Removed { get; set; }

        public ReductionGroup(IGrouping<string, BlackBoxProcessGroup> grouped)
        {
            Source = grouped.First();
            Count = grouped.Sum(entry => entry.Count);
        }
    }

    public static IReadOnlyDictionary<int, BlackBoxItemClassification> Classify(
        IReadOnlyList<BlackBoxProcessGroup> processes, IReadOnlyDictionary<int, double> grossProduction,
        IReadOnlyDictionary<int, double> grossConsumption, bool materialComplete,
        IReadOnlyCollection<int> uncertainDemandItems, bool unknownDemand, ISet<int> researchMatrixSinks,
        out IReadOnlyDictionary<int, double> steadyStateBalance)
    {
        var producerItems = new HashSet<int>();
        var consumerItems = new HashSet<int>();
        foreach (var process in processes)
        {
            foreach (var flow in process.NetPerBuilding)
            {
                if (flow.Value > Tolerance) producerItems.Add(flow.Key);
                if (flow.Value < -Tolerance) consumerItems.Add(flow.Key);
            }
        }

        var allItems = new SortedSet<int>(grossProduction.Keys);
        allItems.UnionWith(grossConsumption.Keys);
        var originalBalance = allItems.ToDictionary(itemId => itemId, itemId =>
            (grossProduction.TryGetValue(itemId, out var produced) ? produced : 0) -
            (grossConsumption.TryGetValue(itemId, out var consumed) ? consumed : 0));
        var remainingBalance = new Dictionary<int, double>(originalBalance);
        var finalItems = new HashSet<int>(producerItems.Where(itemId => !consumerItems.Contains(itemId)));
        var intermediateItems = new HashSet<int>(producerItems.Where(consumerItems.Contains));
        var protectedItems = new HashSet<int>(finalItems);
        protectedItems.UnionWith(producerItems.Where(researchMatrixSinks.Contains));
        if (unknownDemand) protectedItems.UnionWith(producerItems);
        else protectedItems.UnionWith(uncertainDemandItems);
        var groups = processes.GroupBy(process => process.StableKey, StringComparer.Ordinal)
            .Select(grouped => new ReductionGroup(grouped))
            .OrderBy(group => group.Source.Process.RecipeId)
            .ThenBy(group => group.Source.Process.BuildingItemId)
            .ThenBy(group => group.Source.StableKey, StringComparer.Ordinal).ToList();

        if (materialComplete)
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var group in groups)
                {
                    if (!group.Source.NetPerBuilding.Values.Any(value => value > Tolerance)) continue;
                    while (group.Removed < group.Count && CanRemove(group.Source.NetPerBuilding, originalBalance,
                               remainingBalance, protectedItems, intermediateItems))
                    {
                        foreach (var flow in group.Source.NetPerBuilding)
                            remainingBalance[flow.Key] -= flow.Value;
                        group.Removed++;
                        changed = true;
                    }
                }
            } while (changed);
        }

        var classification = new Dictionary<int, BlackBoxItemClassification>();
        foreach (var itemId in allItems)
        {
            var originalNet = originalBalance[itemId];
            var isIntermediate = intermediateItems.Contains(itemId);
            double? overbuild = null;
            double? coproduct = null;
            if (materialComplete)
            {
                overbuild = 0;
                coproduct = 0;
                if (isIntermediate && !researchMatrixSinks.Contains(itemId))
                {
                    overbuild = Math.Min(Math.Max(0, originalNet),
                        Math.Max(0, originalNet - remainingBalance[itemId]));
                    var necessaryJointProduction = groups.Sum(group => NecessaryJointProduction(group, itemId,
                        originalBalance, remainingBalance, protectedItems, intermediateItems));
                    var internalConsumption = groups.Sum(group =>
                        group.Source.NetPerBuilding.TryGetValue(itemId, out var amount) && amount < -Tolerance
                            ? -amount * (group.Count - group.Removed) : 0);
                    coproduct = Math.Min(Math.Max(0, remainingBalance[itemId]),
                        Math.Max(0, necessaryJointProduction - internalConsumption));
                }
            }

            var finalKnown = !unknownDemand && !uncertainDemandItems.Contains(itemId);
            var isResearchProduct = finalKnown && researchMatrixSinks.Contains(itemId) && originalNet > Tolerance;
            classification[itemId] = new BlackBoxItemClassification(
                finalKnown && finalItems.Contains(itemId) || isResearchProduct, isResearchProduct,
                isIntermediate, isIntermediate ? Math.Max(0, -originalNet) : 0, overbuild, coproduct);
        }

        steadyStateBalance = CalculateSteadyStateBalance(groups, originalBalance, remainingBalance,
            protectedItems, intermediateItems);
        return classification;
    }

    private static double NecessaryJointProduction(ReductionGroup group, int itemId,
        IReadOnlyDictionary<int, double> originalBalance, IReadOnlyDictionary<int, double> remainingBalance,
        ISet<int> protectedItems, ISet<int> intermediateItems)
    {
        var retained = group.Count - group.Removed;
        if (retained == 0 || !group.Source.HasJointProducts ||
            !group.Source.NetPerBuilding.TryGetValue(itemId, out var production) || production <= Tolerance)
            return 0;

        var removable = (double)retained;
        foreach (var flow in group.Source.NetPerBuilding)
        {
            if (flow.Key == itemId || flow.Value <= Tolerance) continue;
            double minimum;
            if (protectedItems.Contains(flow.Key)) minimum = originalBalance[flow.Key];
            else if (intermediateItems.Contains(flow.Key)) minimum = Math.Min(0, originalBalance[flow.Key]);
            else continue;
            var available = Math.Floor((remainingBalance[flow.Key] - minimum + Tolerance) / flow.Value);
            removable = Math.Min(removable, Math.Max(0, available));
        }

        return production * (retained - removable);
    }

    private static IReadOnlyDictionary<int, double> CalculateSteadyStateBalance(
        IReadOnlyList<ReductionGroup> groups, IReadOnlyDictionary<int, double> originalBalance,
        IReadOnlyDictionary<int, double> remainingBalance, ISet<int> protectedItems, ISet<int> intermediateItems)
    {
        var balance = remainingBalance.ToDictionary(entry => entry.Key, entry => entry.Value);
        var available = groups.ToDictionary(group => group, group => (double)(group.Count - group.Removed));
        bool changed;
        do
        {
            changed = false;
            foreach (var group in groups)
            {
                if (!group.Source.NetPerBuilding.Values.Any(value => value > Tolerance)) continue;
                var reducible = available[group];
                foreach (var flow in group.Source.NetPerBuilding)
                {
                    if (flow.Value <= Tolerance) continue;
                    double minimum;
                    if (protectedItems.Contains(flow.Key)) minimum = originalBalance[flow.Key];
                    else if (intermediateItems.Contains(flow.Key)) minimum = Math.Min(0, originalBalance[flow.Key]);
                    else continue;
                    reducible = Math.Min(reducible, Math.Max(0, (balance[flow.Key] - minimum) / flow.Value));
                }

                if (reducible <= Tolerance) continue;
                foreach (var flow in group.Source.NetPerBuilding)
                    balance[flow.Key] -= flow.Value * reducible;
                available[group] -= reducible;
                changed = true;
            }
        } while (changed);

        return balance;
    }

    private static bool CanRemove(IReadOnlyDictionary<int, double> netPerBuilding,
        IReadOnlyDictionary<int, double> originalBalance, IReadOnlyDictionary<int, double> remainingBalance,
        ISet<int> protectedItems, ISet<int> intermediateItems)
    {
        foreach (var flow in netPerBuilding)
        {
            var reduced = remainingBalance[flow.Key] - flow.Value;
            if (protectedItems.Contains(flow.Key) && reduced < originalBalance[flow.Key] - Tolerance)
                return false;
            if (intermediateItems.Contains(flow.Key) &&
                Math.Max(0, -reduced) > Math.Max(0, -originalBalance[flow.Key]) + Tolerance)
                return false;
        }

        return true;
    }
}
