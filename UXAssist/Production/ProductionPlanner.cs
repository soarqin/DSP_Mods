using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UXAssist.Production;

public sealed partial class ProductionPlanner
{
    private readonly ProductionCatalog _catalog;
    private readonly ProcessEvaluator _evaluator;

    public ProductionPlanner(ProductionCatalog catalog, ProcessEvaluator evaluator = null)
    {
        _catalog = catalog;
        _evaluator = evaluator ?? new ProcessEvaluator();
    }

    public ProductionReport Calculate(ProductionPlanRequest request, CancellationToken cancellation = default)
    {
        if (_catalog == null)
            return ProductionReport.Failure(new ProductionDiagnostic(ProductionDiagnosticCode.DataNotReady,
                "Production data is not loaded."));
        if (request == null || request.IterationBudget <= 0)
            return Fail(ProductionDiagnosticCode.InvalidRequest, "A valid plan request and iteration budget are required.");
        if (cancellation.IsCancellationRequested)
            return Fail(ProductionDiagnosticCode.Cancelled, "The production plan was cancelled.");

        var targets = new SortedDictionary<int, double>();
        foreach (var target in request.Targets)
        {
            if (target == null || target.ItemId <= 0 || !ValidNonnegative(target.ItemsPerMinute))
                return Fail(ProductionDiagnosticCode.InvalidRequest, "A target has an invalid item or rate.");
            AddFlow(targets, target.ItemId, target.ItemsPerMinute);
            if (!ValidNonnegative(targets[target.ItemId]))
                return Fail(ProductionDiagnosticCode.InvalidRequest, "The combined target rate is too large.");
        }

        foreach (var surplus in request.KnownExternalSurplus)
        {
            if (surplus.Key <= 0 || !ValidNonnegative(surplus.Value))
                return Fail(ProductionDiagnosticCode.InvalidRequest, "Known external surplus must be nonnegative.");
            if (!_catalog.Items.ContainsKey(surplus.Key))
                return Fail(ProductionDiagnosticCode.UnknownItem, "An external-surplus item is unknown.", surplus.Key);
        }

        if (request.SprayDeliveredItems.Any(itemId => !targets.ContainsKey(itemId)))
            return Fail(ProductionDiagnosticCode.InvalidRequest, "Finished-product spraying requires a target item.");
        if (!request.ProliferationEnabled && request.SprayDeliveredItems.Count > 0)
            return Fail(ProductionDiagnosticCode.InvalidRequest, "Finished-product spraying requires proliferation.");

        var processes = new SortedDictionary<(int recipeId, int buildingId, ProliferationMode mode, int level),
            ProductionProcess>();
        var externalItems = new SortedSet<int>();
        var pending = new SortedSet<int>(targets.Keys);
        var visited = new HashSet<int>();
        var warnings = new List<ProductionDiagnostic>();
        var hasSprayedInputs = false;
        var hasSprayedTargets = request.SprayDeliveredItems.Any(id => targets[id] > 0);
        ProductionItem proliferator = null;
        if (request.ProliferationEnabled)
        {
            if (!_catalog.Items.TryGetValue(request.ProliferatorItemId, out proliferator) ||
                proliferator.SpraysPerItem <= 0 || proliferator.ProliferationLevel <= 0 ||
                proliferator.ProliferationLevel > _catalog.MaximumProliferationLevel)
                return Fail(ProductionDiagnosticCode.InvalidRequest, "Select a valid proliferator item.",
                    request.ProliferatorItemId);
            if (hasSprayedTargets) pending.Add(proliferator.Id);
        }

        while (pending.Count > 0)
        {
            if (cancellation.IsCancellationRequested)
                return Fail(ProductionDiagnosticCode.Cancelled, "The production plan was cancelled.");
            var itemId = pending.Min;
            pending.Remove(itemId);
            if (!visited.Add(itemId)) continue;
            if (!_catalog.Items.TryGetValue(itemId, out var item))
                return Fail(ProductionDiagnosticCode.UnknownItem, "A required item is unknown.", itemId);

            var hasExplicitRecipe = request.RecipeByItem.TryGetValue(itemId, out var recipeId);
            if (item.IsDarkFogMaterial || request.ExternalMaterials.Contains(itemId) ||
                item.IsNatural && !hasExplicitRecipe)
            {
                externalItems.Add(itemId);
                continue;
            }

            if (!hasExplicitRecipe) recipeId = item.DefaultRecipeId;
            if (recipeId == 0 || !_catalog.Recipes.TryGetValue(recipeId, out var recipe))
                return Fail(ProductionDiagnosticCode.UnknownRecipe, "An item has no selected production recipe.", itemId);
            if (!recipe.Outputs.ContainsKey(itemId))
                return Fail(ProductionDiagnosticCode.InvalidRequest, "The selected recipe does not produce the item.",
                    itemId, recipeId);

            request.OperatingParametersByRecipe.TryGetValue(recipeId, out var parameters);
            var mode = ProliferationMode.None;
            if (request.ProliferationEnabled)
                mode = request.ProliferationModeByItem.TryGetValue(itemId, out var selectedMode)
                    ? selectedMode : DefaultProliferationMode(recipe, parameters);
            var level = mode == ProliferationMode.None ? 0 : proliferator.ProliferationLevel;
            var buildingId = request.BuildingByRecipe.TryGetValue(recipeId, out var recipeBuildingId)
                ? recipeBuildingId
                : request.BuildingByCategory.TryGetValue(recipe.Category, out var categoryBuildingId)
                    ? categoryBuildingId : 0;
            _catalog.Buildings.TryGetValue(buildingId, out var building);
            if (buildingId > 0 && building == null)
                warnings.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingBuilding,
                    "The selected building is missing; material rates remain available.", itemId, recipeId, buildingId));

            if (!_evaluator.TryEvaluate(_catalog, recipe, building, mode, level, parameters,
                    out var process, out var diagnostic))
            {
                if (diagnostic.Code != ProductionDiagnosticCode.IncompatibleBuilding ||
                    !_evaluator.TryEvaluate(_catalog, recipe, null, mode, level, parameters,
                        out process, out _))
                    return ProductionReport.Failure(diagnostic);
                warnings.Add(diagnostic);
            }
            else if (diagnostic != null)
            {
                warnings.Add(diagnostic);
            }

            processes[(recipeId, buildingId, mode, level)] = process;
            foreach (var input in process.InputsPerCycle.Keys) pending.Add(input);
            if (mode == ProliferationMode.None || process.SprayedInputsPerCycle <= 0) continue;
            hasSprayedInputs = true;
            pending.Add(proliferator.Id);
        }

        var sprayDemand = hasSprayedInputs || hasSprayedTargets;
        var spraysPerBottle = 0.0;
        if (sprayDemand)
        {
            spraysPerBottle = proliferator.SpraysPerItem;
            if (request.SelfSprayProliferator)
            {
                _catalog.TryGetProliferation(proliferator.ProliferationLevel, out _, out var bonus, out _);
                spraysPerBottle += Math.Floor(proliferator.SpraysPerItem * bonus + 0.1) - 1;
            }

            if (spraysPerBottle <= 0)
                return Fail(ProductionDiagnosticCode.InvalidRequest, "The selected proliferator has no usable sprays.",
                    proliferator.Id);
        }

        var selectedProcesses = processes.Values.ToArray();
        var reusableSprayItems = sprayDemand
            ? selectedProcesses.Where(process => process.ProliferationMode != ProliferationMode.None)
                .SelectMany(process => process.PreservedSpraysPerCycle.Keys).Distinct().OrderBy(itemId => itemId).ToArray()
            : Array.Empty<int>();
        var itemIds = new SortedSet<int>(targets.Keys);
        itemIds.UnionWith(request.KnownExternalSurplus.Keys);
        itemIds.UnionWith(externalItems);
        foreach (var process in selectedProcesses)
        {
            itemIds.UnionWith(process.InputsPerCycle.Keys);
            itemIds.UnionWith(process.OutputsPerCycle.Keys);
        }

        if (sprayDemand) itemIds.Add(proliferator.Id);
        var rows = itemIds.ToArray();
        var rowByItem = rows.Select((id, index) => (id, index))
            .ToDictionary(entry => entry.id, entry => entry.index);
        var importItems = externalItems.ToArray();
        var processCount = selectedProcesses.Length;
        var importStart = processCount;
        var surplusStart = importStart + importItems.Length;
        var sprayCreditStart = surplusStart + rows.Length;
        var spraySupplySlackStart = sprayCreditStart + reusableSprayItems.Length;
        var sprayDemandSlackStart = spraySupplySlackStart + reusableSprayItems.Length;
        var columnCount = sprayDemandSlackStart + reusableSprayItems.Length;
        var constraintCount = rows.Length + reusableSprayItems.Length * 2;
        var matrix = new double[constraintCount, columnCount];
        var rhs = new double[constraintCount];
        var priorityProcesses = new double[columnCount];
        var priorityImports = new double[columnCount];
        var prioritySprayReuse = new double[columnCount];
        var prioritySurplus = new double[columnCount];

        for (var index = 0; index < processCount; index++)
        {
            var process = selectedProcesses[index];
            priorityProcesses[index] = 1;
            foreach (var input in process.InputsPerCycle) matrix[rowByItem[input.Key], index] -= input.Value;
            foreach (var output in process.OutputsPerCycle) matrix[rowByItem[output.Key], index] += output.Value;
            if (process.ProliferationMode != ProliferationMode.None && process.SprayedInputsPerCycle > 0)
                matrix[rowByItem[proliferator.Id], index] -=
                    process.SprayedInputsPerCycle / spraysPerBottle;
        }

        for (var index = 0; index < importItems.Length; index++)
        {
            matrix[rowByItem[importItems[index]], importStart + index] = 1;
            priorityImports[importStart + index] = 1;
        }

        for (var index = 0; index < rows.Length; index++)
        {
            var itemId = rows[index];
            var target = targets.TryGetValue(itemId, out var amount) ? amount : 0;
            rhs[index] = target - (request.KnownExternalSurplus.TryGetValue(itemId, out var known) ? known : 0);
            if (sprayDemand && itemId == proliferator.Id)
                rhs[index] += request.SprayDeliveredItems.Sum(targetId => targets[targetId]) / spraysPerBottle;
            matrix[index, surplusStart + index] = -1;
            prioritySurplus[surplusStart + index] = 1;
        }

        for (var index = 0; index < reusableSprayItems.Length; index++)
        {
            var itemId = reusableSprayItems[index];
            var supplyRow = rows.Length + index * 2;
            var demandRow = supplyRow + 1;
            for (var processIndex = 0; processIndex < processCount; processIndex++)
            {
                var process = selectedProcesses[processIndex];
                if (process.ProliferationMode != ProliferationMode.None &&
                    process.PreservedSpraysPerCycle.TryGetValue(itemId, out var preserved))
                    matrix[supplyRow, processIndex] = preserved;
                matrix[demandRow, processIndex] = SprayedInputAmount(process, itemId);
            }

            matrix[supplyRow, sprayCreditStart + index] = -1;
            matrix[supplyRow, spraySupplySlackStart + index] = -1;
            matrix[demandRow, sprayCreditStart + index] = -1;
            matrix[demandRow, sprayDemandSlackStart + index] = -1;
            matrix[rowByItem[proliferator.Id], sprayCreditStart + index] = 1 / spraysPerBottle;
            if (request.SprayDeliveredItems.Contains(itemId)) rhs[demandRow] = -targets[itemId];
            prioritySprayReuse[sprayCreditStart + index] = -1;
        }

        var solution = TwoPhaseSimplex.Solve(matrix, rhs,
            new[] { priorityProcesses, priorityImports, prioritySprayReuse, prioritySurplus },
            request.IterationBudget, cancellation);
        if (solution.Status != LinearSolveStatus.Optimal)
            return SolverFailure(solution.Status);
        for (var row = 0; row < constraintCount; row++)
        {
            var balance = -rhs[row];
            var magnitude = Math.Max(1, Math.Abs(rhs[row]));
            for (var column = 0; column < columnCount; column++)
            {
                var term = matrix[row, column] * solution.Values[column];
                balance += term;
                magnitude = Math.Max(magnitude, Math.Abs(term));
            }

            if (Math.Abs(balance) > 1e-7 * magnitude)
                return Fail(ProductionDiagnosticCode.ConservationFailure,
                    "The solved production ledger does not conserve this item.",
                    row < rows.Length ? rows[row] : reusableSprayItems[(row - rows.Length) / 2]);
        }

        return BuildReport(request, targets, rows, selectedProcesses, importItems, solution.Values,
            importStart, surplusStart, sprayDemand ? proliferator.Id : 0, spraysPerBottle, warnings,
            reusableSprayItems, sprayCreditStart);
    }

    private static double SprayedInputAmount(ProductionProcess process, int itemId)
    {
        if (process.ProliferationMode == ProliferationMode.None || process.SprayedInputsPerCycle <= 0 ||
            !process.InputsPerCycle.TryGetValue(itemId, out var amount)) return 0;
        if (process.InputsPerCycle.Count == 1) return Math.Min(amount, process.SprayedInputsPerCycle);
        return process.SprayedInputsPerCycle == process.InputsPerCycle.Values.Sum() ? amount : 0;
    }

    private static ProliferationMode DefaultProliferationMode(ProductionRecipe recipe,
        IReadOnlyDictionary<string, double> operatingParameters)
    {
        if (recipe.Category == ProductionRecipeCategory.Fractionate ||
            recipe.Category == ProductionRecipeCategory.Exchange) return ProliferationMode.Speedup;
        if (recipe.Category == ProductionRecipeCategory.PhotonStore)
            return operatingParameters != null &&
                   operatingParameters.TryGetValue("LensItemId", out var lensItemId) && lensItemId > 0
                ? ProliferationMode.Speedup : ProliferationMode.None;
        if (recipe.Inputs.Count == 0) return ProliferationMode.None;
        return recipe.Productive ? ProliferationMode.ExtraProducts : ProliferationMode.Speedup;
    }

    private static bool ValidNonnegative(double value)
    {
        return value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static void AddFlow(IDictionary<int, double> flows, int itemId, double amount)
    {
        flows[itemId] = (flows.TryGetValue(itemId, out var old) ? old : 0) + amount;
    }

    private static ProductionReport Fail(ProductionDiagnosticCode code, string message, int itemId = 0,
        int recipeId = 0)
    {
        return ProductionReport.Failure(new ProductionDiagnostic(code, message, itemId, recipeId));
    }

    private static ProductionReport SolverFailure(LinearSolveStatus status)
    {
        switch (status)
        {
            case LinearSolveStatus.Infeasible:
                return Fail(ProductionDiagnosticCode.Infeasible, "The selected processes cannot satisfy the targets.");
            case LinearSolveStatus.Unbounded:
                return Fail(ProductionDiagnosticCode.Unbounded, "The production model is unbounded.");
            case LinearSolveStatus.IterationLimit:
                return Fail(ProductionDiagnosticCode.IterationLimit, "The simplex iteration budget was exhausted.");
            default:
                return Fail(ProductionDiagnosticCode.Cancelled, "The production plan was cancelled.");
        }
    }
}
