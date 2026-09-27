using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UXAssist.Production;

public enum ProductionStatus
{
    Complete,
    Partial,
    Failed,
    DataNotReady
}

public enum ProductionPowerScope
{
    ProductionBuildings,
    SelectedFacilities,
    ProductionBuildingsAndAuxiliary
}

public sealed class ProductionPower
{
    public double ConsumptionWatts { get; }
    public double PeakConsumptionWatts { get; }
    public double GenerationWatts { get; }
    public double AccumulatorChargingWatts { get; }
    public double AccumulatorDischargingWatts { get; }
    public double DysonSphereRequirementWatts { get; }
    public ProductionPowerScope Scope { get; }

    public ProductionPower(double consumptionWatts, double peakConsumptionWatts, double generationWatts,
        double accumulatorChargingWatts, double accumulatorDischargingWatts, double dysonSphereRequirementWatts,
        ProductionPowerScope scope)
    {
        ConsumptionWatts = consumptionWatts;
        PeakConsumptionWatts = peakConsumptionWatts;
        GenerationWatts = generationWatts;
        AccumulatorChargingWatts = accumulatorChargingWatts;
        AccumulatorDischargingWatts = accumulatorDischargingWatts;
        DysonSphereRequirementWatts = dysonSphereRequirementWatts;
        Scope = scope;
    }
}

public sealed class ProductionPowerBreakdown
{
    public double KnownFactoryConsumptionWatts { get; }
    public double KnownLogisticsConsumptionWatts { get; }
    public bool FactoryComplete { get; }
    public bool LogisticsComplete { get; }
    public double? FactoryConsumptionWatts => FactoryComplete ? KnownFactoryConsumptionWatts : null;
    public double? LogisticsConsumptionWatts => LogisticsComplete ? KnownLogisticsConsumptionWatts : null;

    public ProductionPowerBreakdown(double knownFactoryConsumptionWatts, double knownLogisticsConsumptionWatts,
        bool factoryComplete, bool logisticsComplete)
    {
        KnownFactoryConsumptionWatts = knownFactoryConsumptionWatts;
        KnownLogisticsConsumptionWatts = knownLogisticsConsumptionWatts;
        FactoryComplete = factoryComplete;
        LogisticsComplete = logisticsComplete;
    }
}

public sealed class ProductionItemFlow
{
    public int ItemId { get; }
    public double GrossProduction { get; }
    public double GrossConsumption { get; }
    public double NetFlow => GrossProduction - GrossConsumption;
    public double ExternalImports { get; }
    public double KnownExternalSupply { get; }
    public double Delivered { get; }
    public double Surplus { get; }
    public double RequiredExternalSupply { get; }
    public double? SteadyStateExternalSupply { get; }
    public bool IsFinalProduct { get; }
    public bool IsResearchProduct { get; }
    public bool IsExcessIntermediate { get; }
    public double SingleBuildingSurplusThreshold { get; }
    public double IntermediateShortage { get; }
    public double? OverbuildSurplus { get; }
    public double? CoproductSurplus { get; }
    public IReadOnlyList<int> ProducerGroups { get; }
    public IReadOnlyList<int> ConsumerGroups { get; }

    public ProductionItemFlow(int itemId, double grossProduction, double grossConsumption, double externalImports,
        double knownExternalSupply, double delivered, double surplus, double requiredExternalSupply,
        bool isFinalProduct, bool isExcessIntermediate, double singleBuildingSurplusThreshold,
        IEnumerable<int> producerGroups, IEnumerable<int> consumerGroups)
        : this(itemId, grossProduction, grossConsumption, externalImports, knownExternalSupply, delivered, surplus,
            requiredExternalSupply, isFinalProduct, isExcessIntermediate, singleBuildingSurplusThreshold,
            producerGroups, consumerGroups, 0, null, null)
    {
    }

    public ProductionItemFlow(int itemId, double grossProduction, double grossConsumption, double externalImports,
        double knownExternalSupply, double delivered, double surplus, double requiredExternalSupply,
        bool isFinalProduct, bool isExcessIntermediate, double singleBuildingSurplusThreshold,
        IEnumerable<int> producerGroups, IEnumerable<int> consumerGroups, double intermediateShortage,
        double? overbuildSurplus, double? coproductSurplus, double? steadyStateExternalSupply = null,
        bool isResearchProduct = false)
    {
        ItemId = itemId;
        GrossProduction = grossProduction;
        GrossConsumption = grossConsumption;
        ExternalImports = externalImports;
        KnownExternalSupply = knownExternalSupply;
        Delivered = delivered;
        Surplus = surplus;
        RequiredExternalSupply = requiredExternalSupply;
        SteadyStateExternalSupply = steadyStateExternalSupply;
        IsFinalProduct = isFinalProduct;
        IsResearchProduct = isResearchProduct;
        IsExcessIntermediate = isExcessIntermediate;
        SingleBuildingSurplusThreshold = singleBuildingSurplusThreshold;
        IntermediateShortage = intermediateShortage;
        OverbuildSurplus = overbuildSurplus;
        CoproductSurplus = coproductSurplus;
        ProducerGroups = Array.AsReadOnly(producerGroups.ToArray());
        ConsumerGroups = Array.AsReadOnly(consumerGroups.ToArray());
    }
}

public sealed class ProductionGroupFlow
{
    public ProductionProcess Process { get; }
    public double BuildingCount { get; }
    public double ExecutionsPerMinute { get; }
    public double? EquivalentBuildings { get; }
    public int? RoundedDeploymentCount { get; }
    public double? ConsumptionWatts { get; }
    public double? PeakConsumptionWatts { get; }
    public double? GenerationWatts { get; }
    public double? AccumulatorChargingWatts { get; }
    public double? AccumulatorDischargingWatts { get; }
    public double? DysonSphereRequirementWatts { get; }
    public double LaunchesPerMinute { get; }
    public double ResearchHashesPerMinute { get; }
    public IReadOnlyDictionary<int, double> GrossProduction { get; }
    public IReadOnlyDictionary<int, double> GrossConsumption { get; }

    public ProductionGroupFlow(ProductionProcess process, double buildingCount, double executionsPerMinute,
        double? equivalentBuildings, int? roundedDeploymentCount, double? consumptionWatts,
        double? peakConsumptionWatts,
        IEnumerable<KeyValuePair<int, double>> grossProduction,
        IEnumerable<KeyValuePair<int, double>> grossConsumption, double? generationWatts = 0,
        double? accumulatorChargingWatts = 0, double? accumulatorDischargingWatts = 0,
        double? dysonSphereRequirementWatts = 0, double launchesPerMinute = 0,
        double researchHashesPerMinute = 0)
    {
        Process = process;
        BuildingCount = buildingCount;
        ExecutionsPerMinute = executionsPerMinute;
        EquivalentBuildings = equivalentBuildings;
        RoundedDeploymentCount = roundedDeploymentCount;
        ConsumptionWatts = consumptionWatts;
        PeakConsumptionWatts = peakConsumptionWatts;
        GenerationWatts = generationWatts;
        AccumulatorChargingWatts = accumulatorChargingWatts;
        AccumulatorDischargingWatts = accumulatorDischargingWatts;
        DysonSphereRequirementWatts = dysonSphereRequirementWatts;
        LaunchesPerMinute = launchesPerMinute;
        ResearchHashesPerMinute = researchHashesPerMinute;
        GrossProduction = ProductionRecipe.CopyFlows(grossProduction);
        GrossConsumption = ProductionRecipe.CopyFlows(grossConsumption);
    }
}

public sealed class ProductionReport
{
    public ProductionStatus Status { get; }
    public bool MaterialComplete { get; }
    public bool PowerComplete { get; }
    public IReadOnlyDictionary<int, ProductionItemFlow> ItemFlows { get; }
    public IReadOnlyList<ProductionGroupFlow> Groups { get; }
    public IReadOnlyList<ProductionDiagnostic> Diagnostics { get; }
    public ProductionPower Power { get; }
    public ProductionPowerBreakdown PowerBreakdown { get; }
    public double LaunchesPerMinute => Groups.Sum(group => group.LaunchesPerMinute);
    public double ResearchHashesPerMinute => Groups.Sum(group => group.ResearchHashesPerMinute);

    public ProductionReport(ProductionStatus status, bool materialComplete, bool powerComplete,
        IEnumerable<ProductionItemFlow> itemFlows, IEnumerable<ProductionGroupFlow> groups,
        IEnumerable<ProductionDiagnostic> diagnostics, ProductionPower power)
        : this(status, materialComplete, powerComplete, itemFlows, groups, diagnostics, power, null)
    {
    }

    public ProductionReport(ProductionStatus status, bool materialComplete, bool powerComplete,
        IEnumerable<ProductionItemFlow> itemFlows, IEnumerable<ProductionGroupFlow> groups,
        IEnumerable<ProductionDiagnostic> diagnostics, ProductionPower power,
        ProductionPowerBreakdown powerBreakdown)
    {
        Status = status;
        MaterialComplete = materialComplete;
        PowerComplete = powerComplete;
        ItemFlows = new ReadOnlyDictionary<int, ProductionItemFlow>(itemFlows.ToDictionary(flow => flow.ItemId));
        Groups = Array.AsReadOnly(groups.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        Power = power;
        PowerBreakdown = powerBreakdown;
    }

    internal static ProductionReport Failure(ProductionDiagnostic diagnostic)
    {
        return new ProductionReport(diagnostic.Code == ProductionDiagnosticCode.DataNotReady
                ? ProductionStatus.DataNotReady : ProductionStatus.Failed,
            false, false, Array.Empty<ProductionItemFlow>(), Array.Empty<ProductionGroupFlow>(),
            new[] { diagnostic }, null);
    }
}
