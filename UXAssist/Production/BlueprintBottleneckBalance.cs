using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static UXAssist.Production.ProductionMath;

namespace UXAssist.Production;

internal static class BlueprintBottleneckBalance
{
    private const double Tolerance = 1e-7;

    internal static bool TryCalculate(IReadOnlyList<BlackBoxProcessGroup> processes,
        IReadOnlyDictionary<int, double> production, IReadOnlyDictionary<int, double> consumption,
        ISet<int> researchMatrixSinks,
        out SortedDictionary<int, double> balancedProduction, out SortedDictionary<int, double> balancedConsumption,
        out IReadOnlyDictionary<int, BlackBoxItemClassification> balancedClassifications,
        out IReadOnlyDictionary<int, double> balancedNet)
    {
        balancedProduction = null;
        balancedConsumption = null;
        balancedClassifications = null;
        balancedNet = null;
        if (production.Count == 0 && consumption.Count == 0)
        {
            balancedProduction = new SortedDictionary<int, double>();
            balancedConsumption = new SortedDictionary<int, double>();
            balancedClassifications = new Dictionary<int, BlackBoxItemClassification>();
            balancedNet = new Dictionary<int, double>();
            return true;
        }

        var groups = processes.GroupBy(process => process.StableKey, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (source: group.First(), count: group.Sum(process => process.Count))).ToArray();
        var produced = new HashSet<int>(groups.SelectMany(group => group.source.NetPerBuilding)
            .Where(flow => flow.Value > BlackBoxMaterialClassifier.Tolerance).Select(flow => flow.Key));
        var consumed = new HashSet<int>(groups.SelectMany(group => group.source.NetPerBuilding)
            .Where(flow => flow.Value < -BlackBoxMaterialClassifier.Tolerance).Select(flow => flow.Key));
        var items = produced.OrderBy(itemId => itemId).ToArray();
        // Normalize only the scale variable; the solver still resolves shared demand, recipe ratios, and cycles together.
        var commonScale = items.Where(consumed.Contains).Select(itemId => production[itemId] / consumption[itemId])
            .Aggregate(1.0, Math.Min);
        var targets = items.Where(itemId => !consumed.Contains(itemId) || researchMatrixSinks.Contains(itemId))
            .ToDictionary(itemId => itemId, itemId => Math.Max(0, production[itemId] -
                (consumption.TryGetValue(itemId, out var amount) ? amount : 0)));
        // Launchers, generators, and precise research can terminate a chain without an item output.
        var sinks = Enumerable.Range(0, groups.Length).Where(index =>
            groups[index].source.NetPerBuilding.Values.Any(value => value < -BlackBoxMaterialClassifier.Tolerance) &&
            !groups[index].source.NetPerBuilding.Values.Any(value => value > BlackBoxMaterialClassifier.Tolerance))
            .ToArray();
        if (!targets.Values.Any(FinitePositive) && sinks.Length == 0) return false;

        var scaleColumn = groups.Length;
        var surplusStart = scaleColumn + 1;
        var capacitySlackStart = surplusStart + items.Length;
        var columnCount = capacitySlackStart + groups.Length + 1;
        var matrix = new double[items.Length + groups.Length + 1 + sinks.Length, columnCount];
        var rhs = new double[matrix.GetLength(0)];
        var maximizeOutput = new double[columnCount];
        var minimizeUse = new double[columnCount];
        var minimizeSurplus = new double[columnCount];
        maximizeOutput[scaleColumn] = -1;
        for (var row = 0; row < items.Length; row++)
        {
            var itemId = items[row];
            var rowScale = Math.Max(production[itemId], consumption.TryGetValue(itemId, out var amount) ? amount : 0);
            for (var index = 0; index < groups.Length; index++)
                if (groups[index].source.NetPerBuilding.TryGetValue(itemId, out var net))
                    matrix[row, index] = net * groups[index].count / rowScale;
            if (targets.TryGetValue(itemId, out var target)) matrix[row, scaleColumn] = -target / rowScale * commonScale;
            matrix[row, surplusStart + row] = -1;
            minimizeSurplus[surplusStart + row] = 1;
        }

        for (var index = 0; index <= groups.Length; index++)
        {
            var row = items.Length + index;
            matrix[row, index] = index < groups.Length ? 1 : commonScale;
            matrix[row, capacitySlackStart + index] = 1;
            rhs[row] = 1;
            if (index < groups.Length) minimizeUse[index] = groups[index].count;
        }

        for (var index = 0; index < sinks.Length; index++)
        {
            var row = items.Length + groups.Length + 1 + index;
            matrix[row, sinks[index]] = 1;
            matrix[row, scaleColumn] = -commonScale;
        }

        var solution = TwoPhaseSimplex.Solve(matrix, rhs,
            new[] { maximizeOutput, minimizeUse, minimizeSurplus }, 20000, CancellationToken.None);
        if (solution.Status != LinearSolveStatus.Optimal || !FinitePositive(solution.Values[scaleColumn])) return false;
        for (var row = 0; row < rhs.Length; row++)
        {
            var residual = -rhs[row];
            for (var column = 0; column < columnCount; column++) residual += matrix[row, column] * solution.Values[column];
            if (Math.Abs(residual) > Tolerance) return false;
        }

        balancedProduction = new SortedDictionary<int, double>(production.ToDictionary(flow => flow.Key, _ => 0.0));
        balancedConsumption = new SortedDictionary<int, double>(consumption.ToDictionary(flow => flow.Key, _ => 0.0));
        var unusedProduction = new Dictionary<int, double>();
        for (var index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            var utilized = Math.Min(1, solution.Values[index]) * group.count;
            var executions = group.source.Process.CyclesPerBuildingPerMinute * utilized;
            foreach (var flow in group.source.Process.OutputsPerCycle)
                AddFlow(balancedProduction, flow.Key, flow.Value * executions);
            foreach (var flow in group.source.Process.InputsPerCycle)
                AddFlow(balancedConsumption, flow.Key, flow.Value * executions);
            foreach (var flow in group.source.NetPerBuilding)
                if (flow.Value > 0) AddFlow(unusedProduction, flow.Key, flow.Value * (group.count - utilized));
        }

        var netFlows = new Dictionary<int, double>();
        var result = new Dictionary<int, BlackBoxItemClassification>();
        var allItems = new SortedSet<int>(production.Keys);
        allItems.UnionWith(consumption.Keys);
        foreach (var itemId in allItems)
        {
            var grossProduced = balancedProduction.TryGetValue(itemId, out var p) ? p : 0;
            var grossConsumed = balancedConsumption.TryGetValue(itemId, out var c) ? c : 0;
            var net = grossProduced - grossConsumed;
            if (targets.TryGetValue(itemId, out var target))
            {
                var required = target * solution.Values[scaleColumn] * commonScale;
                if (net < required - Tolerance * Math.Max(1, required)) return false;
            }
            if (Math.Abs(net) <= Tolerance * Math.Max(1, Math.Max(grossProduced, grossConsumed))) net = 0;
            netFlows[itemId] = net;
            var intermediate = produced.Contains(itemId) && consumed.Contains(itemId);
            if (intermediate && net < 0) return false;
            var research = researchMatrixSinks.Contains(itemId);
            var researchProduct = research && net > 0;
            result[itemId] = new BlackBoxItemClassification(produced.Contains(itemId) && !consumed.Contains(itemId) || researchProduct,
                researchProduct, 0,
                intermediate && !research && unusedProduction.TryGetValue(itemId, out var unused) ? unused : 0,
                intermediate && !research ? Math.Max(0, net) : 0);
        }

        balancedClassifications = result;
        balancedNet = netFlows;
        return true;
    }
}
