using System;
using System.Collections.Generic;
using System.Linq;
using UXAssist.Common.GameConstants;
using UXAssist.Production;

namespace CheatEnabler.Patches.Factory;

internal readonly struct BeltSignalSourceRate
{
    public int ItemId { get; }
    public double ProductionPerItem { get; }
    public double ConsumptionPerItem { get; }

    public BeltSignalSourceRate(int itemId, double productionPerItem, double consumptionPerItem)
    {
        ItemId = itemId;
        ProductionPerItem = productionPerItem;
        ConsumptionPerItem = consumptionPerItem;
    }
}

internal sealed class BeltSignalSourceStats
{
    private readonly double[] _productionProgress;
    private readonly double[] _consumptionProgress;

    public IReadOnlyList<BeltSignalSourceRate> Rates { get; }

    public BeltSignalSourceStats(IReadOnlyList<BeltSignalSourceRate> rates)
    {
        Rates = rates;
        _productionProgress = new double[rates.Count];
        _consumptionProgress = new double[rates.Count];
    }

    public static IReadOnlyList<BeltSignalSourceRate> FromReport(ProductionReport report, int targetItemId)
    {
        if (report == null || !report.MaterialComplete)
            throw new InvalidOperationException("A complete material report is required for belt statistics.");

        var rates = new List<BeltSignalSourceRate>();
        foreach (var flow in report.ItemFlows.Values.OrderBy(flow => flow.ItemId))
        {
            var produced = flow.GrossProduction + flow.ExternalImports -
                           (flow.ItemId == targetItemId ? flow.Delivered : 0);
            var consumed = flow.GrossConsumption;
            if (!ValidRate(produced) || !ValidRate(consumed))
                throw new InvalidOperationException("The production report contains an invalid belt statistics rate.");
            if (produced > 0 || consumed > 0)
                rates.Add(new BeltSignalSourceRate(flow.ItemId, Math.Max(0, produced), consumed));
        }

        return Array.AsReadOnly(rates.ToArray());
    }

    public void Apply(int stack, int[] productRegister, int[] consumeRegister)
    {
        for (var index = 0; index < Rates.Count; index++)
        {
            var rate = Rates[index];
            Accumulate(rate.ProductionPerItem, stack, ref _productionProgress[index],
                productRegister, rate.ItemId);
            Accumulate(rate.ConsumptionPerItem, stack, ref _consumptionProgress[index],
                consumeRegister, rate.ItemId);
        }
    }

    private static bool ValidRate(double rate)
    {
        return rate >= -1e-7 && rate < int.MaxValue / 4.0 &&
               !double.IsNaN(rate) && !double.IsInfinity(rate);
    }

    private static void Accumulate(double rate, int stack, ref double progress, int[] register, int itemId)
    {
        if (rate <= 0) return;
        var total = progress + rate * stack;
        if (total <= 0)
        {
            progress = total;
            return;
        }

        var count = (int)Math.Ceiling(total);
        register[itemId] += count;
        progress = total - count;
    }
}

internal static class BeltSignalSourcePreset
{
    public static ProductionItem SelectProliferator(ProductionCatalog catalog)
    {
        return catalog?.Items.Values.Where(item => item.ProliferationLevel > 0 && item.SpraysPerItem > 0)
            .OrderByDescending(item => item.ProliferationLevel)
            .ThenByDescending(item => item.SpraysPerItem)
            .ThenBy(item => item.Id).FirstOrDefault();
    }

    public static ProductionPlanRequest Create(ProductionCatalog catalog, int itemId,
        bool useProliferator, bool sprayDeliveredItem)
    {
        var request = new ProductionPlanRequest
        {
            ProliferationEnabled = useProliferator,
            ProliferatorItemId = useProliferator ? SelectProliferator(catalog)?.Id ?? 0 : 0,
            SelfSprayProliferator = useProliferator
        };
        request.Targets.Add(new ProductionTarget(itemId, 1));
        foreach (var externalItemId in ItemIds.ExtraOreItemIds)
        {
            if (catalog.Items.ContainsKey(externalItemId)) request.ExternalMaterials.Add(externalItemId);
        }

        if (!useProliferator) return request;
        if (sprayDeliveredItem) request.SprayDeliveredItems.Add(itemId);
        foreach (var item in catalog.Items.Values)
        {
            if (!catalog.Recipes.TryGetValue(item.DefaultRecipeId, out var recipe) ||
                !recipe.Outputs.ContainsKey(item.Id) || recipe.Inputs.Count == 0) continue;
            request.ProliferationModeByItem[item.Id] = ItemIds.NoProliferationItemIds.Contains(item.Id)
                ? ProliferationMode.None
                : ItemIds.ExtraProliferationItemIds.Contains(item.Id) && recipe.Productive
                    ? ProliferationMode.ExtraProducts : ProliferationMode.Speedup;
        }

        return request;
    }
}
