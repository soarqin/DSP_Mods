using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UXAssist.Production;

public enum ProductionRecipeCategory
{
    None,
    Smelt,
    Chemical,
    Refine,
    Assemble,
    Particle,
    Research,
    Fractionate,
    Exchange,
    PhotonStore
}

internal static class ProductionUnits
{
    internal const double SecondsPerMinute = 60;
    internal const double TicksPerSecond = 60;
    internal const double TicksPerMinute = SecondsPerMinute * TicksPerSecond;
    internal const double FixedPointScale = 10000;
}

public enum ProliferationMode
{
    None,
    Speedup,
    ExtraProducts
}

public enum ProductionBuildingKind
{
    Ordinary,
    Fractionator,
    Exchanger,
    RayReceiver,
    Miner,
    Collector,
    FuelGenerator,
    RenewableGenerator,
    Ejector,
    Silo,
    Auxiliary,
    Logistics
}

public enum RenewablePowerSource
{
    None,
    Wind,
    Solar,
    Geothermal
}

public enum ProductionMinerKind
{
    None,
    Vein,
    Oil,
    Water
}

public enum ProductionDiagnosticCode
{
    DataNotReady,
    UnknownItem,
    UnknownRecipe,
    MissingBuilding,
    IncompatibleBuilding,
    UnsupportedProcess,
    UnsupportedProliferation,
    InvalidRequest,
    DuplicateSelection,
    MissingOperatingParameter,
    Infeasible,
    Unbounded,
    IterationLimit,
    Cancelled,
    ConservationFailure
}

public sealed class ProductionDiagnostic
{
    public ProductionDiagnosticCode Code { get; }
    public string Message { get; }
    public int ItemId { get; }
    public int RecipeId { get; }
    public int BuildingItemId { get; }

    public ProductionDiagnostic(ProductionDiagnosticCode code, string message, int itemId = 0, int recipeId = 0,
        int buildingItemId = 0)
    {
        Code = code;
        Message = message;
        ItemId = itemId;
        RecipeId = recipeId;
        BuildingItemId = buildingItemId;
    }
}

public sealed class ProductionItem
{
    public int Id { get; }
    public int DefaultRecipeId { get; }
    public bool IsNatural { get; }
    public bool IsDarkFogMaterial { get; }
    public int ProliferationLevel { get; }
    public int SpraysPerItem { get; }
    public long HeatValueJoules { get; }
    public int FuelTypeMask { get; }
    public bool ProductiveFuel { get; }
    public double StarOutputMultiplier { get; }
    public int CatalystTypeMask { get; }
    public double CatalystAbilityMultiplier { get; }

    public ProductionItem(int id, int defaultRecipeId, bool isNatural, bool isDarkFogMaterial,
        int proliferationLevel, int spraysPerItem, long heatValueJoules = 0, int fuelTypeMask = 0,
        bool productiveFuel = false, double starOutputMultiplier = 1, int catalystTypeMask = 0,
        double catalystAbilityMultiplier = 0)
    {
        Id = id;
        DefaultRecipeId = defaultRecipeId;
        IsNatural = isNatural;
        IsDarkFogMaterial = isDarkFogMaterial;
        ProliferationLevel = proliferationLevel;
        SpraysPerItem = spraysPerItem;
        HeatValueJoules = heatValueJoules;
        FuelTypeMask = fuelTypeMask;
        ProductiveFuel = productiveFuel;
        StarOutputMultiplier = starOutputMultiplier;
        CatalystTypeMask = catalystTypeMask;
        CatalystAbilityMultiplier = catalystAbilityMultiplier;
    }
}

public sealed class ProductionRecipe
{
    public int Id { get; }
    public bool IsSynthetic => Id < 0;
    public ProductionRecipeCategory Category { get; }
    public int TimeTicks { get; }
    public bool Productive { get; }
    public IReadOnlyDictionary<int, double> Inputs { get; }
    public IReadOnlyDictionary<int, double> Outputs { get; }

    public ProductionRecipe(int id, ProductionRecipeCategory category, int timeTicks, bool productive,
        IEnumerable<KeyValuePair<int, double>> inputs, IEnumerable<KeyValuePair<int, double>> outputs)
    {
        Id = id;
        Category = category;
        TimeTicks = timeTicks;
        Productive = productive;
        Inputs = CopyFlows(inputs);
        Outputs = CopyFlows(outputs);
    }

    internal static IReadOnlyDictionary<int, double> CopyFlows(IEnumerable<KeyValuePair<int, double>> flows)
    {
        var result = new Dictionary<int, double>();
        foreach (var flow in flows)
        {
            result[flow.Key] = result.TryGetValue(flow.Key, out var count) ? count + flow.Value : flow.Value;
        }

        return new ReadOnlyDictionary<int, double>(result);
    }
}

public sealed class ProductionTechnology
{
    public int Id { get; }
    public IReadOnlyDictionary<int, double> MatrixPointsPerHash { get; }

    public ProductionTechnology(int id, IEnumerable<KeyValuePair<int, double>> matrixPointsPerHash)
    {
        Id = id;
        MatrixPointsPerHash = ProductionRecipe.CopyFlows(matrixPointsPerHash);
    }
}

public sealed class ProductionBuilding
{
    public int ItemId { get; }
    public ProductionRecipeCategory Category { get; }
    public double SpeedFactor { get; }
    public double WorkingPowerWatts { get; }
    public double IdlePowerWatts { get; }
    public ProductionBuildingKind Kind { get; }
    public double RatedGenerationWatts { get; }
    public double ExchangeRateWatts { get; }
    public double AccumulatorEnergyJoules { get; }
    public int EmptyAccumulatorItemId { get; }
    public int FullAccumulatorItemId { get; }
    public RenewablePowerSource RenewableSource { get; }
    public ProductionMinerKind MinerKind { get; }
    public int MiningPeriodTicks { get; }
    public int FuelMask { get; }
    public double FuelUseWatts { get; }
    public int PowerProductItemId { get; }
    public double PowerProductEnergyJoules { get; }
    public int CatalystMask { get; }
    public int LaunchChargeTicks { get; }
    public int LaunchCooldownTicks { get; }
    public int AmmunitionItemId { get; }
    public int CollectorSpeedMultiplier { get; }

    public ProductionBuilding(int itemId, ProductionRecipeCategory category, double speedFactor,
        double workingPowerWatts, double idlePowerWatts,
        ProductionBuildingKind kind = ProductionBuildingKind.Ordinary, double ratedGenerationWatts = 0,
        double exchangeRateWatts = 0, double accumulatorEnergyJoules = 0,
        int emptyAccumulatorItemId = 0, int fullAccumulatorItemId = 0,
        RenewablePowerSource renewableSource = RenewablePowerSource.None,
        ProductionMinerKind minerKind = ProductionMinerKind.None, int miningPeriodTicks = 0,
        int fuelMask = 0, double fuelUseWatts = 0, int powerProductItemId = 0,
        double powerProductEnergyJoules = 0, int catalystMask = 0,
        int launchChargeTicks = 0, int launchCooldownTicks = 0, int ammunitionItemId = 0,
        int collectorSpeedMultiplier = 0)
    {
        ItemId = itemId;
        Category = category;
        SpeedFactor = speedFactor;
        WorkingPowerWatts = workingPowerWatts;
        IdlePowerWatts = idlePowerWatts;
        Kind = kind;
        RatedGenerationWatts = ratedGenerationWatts;
        ExchangeRateWatts = exchangeRateWatts;
        AccumulatorEnergyJoules = accumulatorEnergyJoules;
        EmptyAccumulatorItemId = emptyAccumulatorItemId;
        FullAccumulatorItemId = fullAccumulatorItemId;
        RenewableSource = renewableSource;
        MinerKind = minerKind;
        MiningPeriodTicks = miningPeriodTicks;
        FuelMask = fuelMask;
        FuelUseWatts = fuelUseWatts;
        PowerProductItemId = powerProductItemId;
        PowerProductEnergyJoules = powerProductEnergyJoules;
        CatalystMask = catalystMask;
        LaunchChargeTicks = launchChargeTicks;
        LaunchCooldownTicks = launchCooldownTicks;
        AmmunitionItemId = ammunitionItemId;
        CollectorSpeedMultiplier = collectorSpeedMultiplier;
    }

    internal ProductionBuilding WithSpeedFactor(double speedFactor)
    {
        return new ProductionBuilding(ItemId, Category, speedFactor, WorkingPowerWatts, IdlePowerWatts, Kind,
            RatedGenerationWatts, ExchangeRateWatts, AccumulatorEnergyJoules, EmptyAccumulatorItemId,
            FullAccumulatorItemId, RenewableSource, MinerKind, MiningPeriodTicks, FuelMask, FuelUseWatts,
            PowerProductItemId, PowerProductEnergyJoules, CatalystMask, LaunchChargeTicks, LaunchCooldownTicks,
            AmmunitionItemId, CollectorSpeedMultiplier);
    }
}

public sealed class ProductionCatalog
{
    private readonly double[] _speedBonus;
    private readonly double[] _extraBonus;
    private readonly double[] _powerMultiplier;

    public IReadOnlyDictionary<int, ProductionItem> Items { get; }
    public IReadOnlyDictionary<int, ProductionRecipe> Recipes { get; }
    public IReadOnlyDictionary<int, ProductionBuilding> Buildings { get; }
    public IReadOnlyDictionary<int, ProductionTechnology> Technologies { get; }
    public int MaximumProliferationLevel { get; }

    public ProductionCatalog(IEnumerable<ProductionItem> items, IEnumerable<ProductionRecipe> recipes,
        IEnumerable<ProductionBuilding> buildings, double[] speedBonus, double[] extraBonus, double[] powerMultiplier,
        IEnumerable<ProductionTechnology> technologies = null)
    {
        Items = new ReadOnlyDictionary<int, ProductionItem>(items.ToDictionary(item => item.Id));
        Recipes = new ReadOnlyDictionary<int, ProductionRecipe>(recipes.ToDictionary(recipe => recipe.Id));
        Buildings = new ReadOnlyDictionary<int, ProductionBuilding>(buildings.ToDictionary(building => building.ItemId));
        Technologies = new ReadOnlyDictionary<int, ProductionTechnology>(
            (technologies ?? Array.Empty<ProductionTechnology>()).ToDictionary(tech => tech.Id));
        _speedBonus = (double[])speedBonus.Clone();
        _extraBonus = (double[])extraBonus.Clone();
        _powerMultiplier = (double[])powerMultiplier.Clone();
        MaximumProliferationLevel = Items.Values.Where(item => item.SpraysPerItem > 0)
            .Select(item => item.ProliferationLevel).DefaultIfEmpty(0).Max();
    }

    public bool TryGetProliferation(int level, out double speedBonus, out double extraBonus,
        out double powerMultiplier)
    {
        speedBonus = 0;
        extraBonus = 0;
        powerMultiplier = 1;
        if (level < 0 || level >= _speedBonus.Length || level >= _extraBonus.Length ||
            level >= _powerMultiplier.Length) return false;
        speedBonus = _speedBonus[level];
        extraBonus = _extraBonus[level];
        powerMultiplier = _powerMultiplier[level];
        return true;
    }
}

public sealed class ProductionProcess
{
    public int RecipeId { get; }
    public int BuildingItemId { get; }
    public ProliferationMode ProliferationMode { get; }
    public int ProliferationLevel { get; }
    public IReadOnlyDictionary<int, double> InputsPerCycle { get; }
    public IReadOnlyDictionary<int, double> OutputsPerCycle { get; }
    public double CyclesPerBuildingPerMinute { get; }
    public double WorkingPowerWatts { get; }
    public bool HasBuilding { get; }
    public double CirculatingItemsPerBuildingPerMinute { get; }
    public double GenerationWatts { get; }
    public double AccumulatorChargingWatts { get; }
    public double AccumulatorDischargingWatts { get; }
    public double? DysonSphereRequirementWatts { get; }
    public double SprayedInputsPerCycle { get; }
    public IReadOnlyDictionary<int, double> PreservedSpraysPerCycle { get; }
    public bool PowerKnown { get; }
    public double LaunchesPerCycle { get; }
    public double ResearchHashesPerCycle { get; }

    public ProductionProcess(int recipeId, int buildingItemId, ProliferationMode proliferationMode,
        int proliferationLevel, IEnumerable<KeyValuePair<int, double>> inputsPerCycle,
        IEnumerable<KeyValuePair<int, double>> outputsPerCycle, double cyclesPerBuildingPerMinute,
        double workingPowerWatts, bool hasBuilding, double circulatingItemsPerBuildingPerMinute = 0,
        double generationWatts = 0, double accumulatorChargingWatts = 0,
        double accumulatorDischargingWatts = 0, double? dysonSphereRequirementWatts = 0,
        double? sprayedInputsPerCycle = null, bool powerKnown = true, double launchesPerCycle = 0,
        double researchHashesPerCycle = 0,
        IEnumerable<KeyValuePair<int, double>> preservedSpraysPerCycle = null)
    {
        RecipeId = recipeId;
        BuildingItemId = buildingItemId;
        ProliferationMode = proliferationMode;
        ProliferationLevel = proliferationLevel;
        InputsPerCycle = ProductionRecipe.CopyFlows(inputsPerCycle);
        OutputsPerCycle = ProductionRecipe.CopyFlows(outputsPerCycle);
        CyclesPerBuildingPerMinute = cyclesPerBuildingPerMinute;
        WorkingPowerWatts = workingPowerWatts;
        HasBuilding = hasBuilding;
        CirculatingItemsPerBuildingPerMinute = circulatingItemsPerBuildingPerMinute;
        GenerationWatts = generationWatts;
        AccumulatorChargingWatts = accumulatorChargingWatts;
        AccumulatorDischargingWatts = accumulatorDischargingWatts;
        DysonSphereRequirementWatts = dysonSphereRequirementWatts;
        SprayedInputsPerCycle = sprayedInputsPerCycle ?? InputsPerCycle.Values.Sum();
        PreservedSpraysPerCycle = ProductionRecipe.CopyFlows(
            preservedSpraysPerCycle ?? Array.Empty<KeyValuePair<int, double>>());
        PowerKnown = powerKnown;
        LaunchesPerCycle = launchesPerCycle;
        ResearchHashesPerCycle = researchHashesPerCycle;
    }
}

public interface IProductionProcessAdapter
{
    bool CanEvaluate(ProductionRecipe recipe, ProductionBuilding building);

    bool TryEvaluate(ProductionCatalog catalog, ProductionRecipe recipe, ProductionBuilding building,
        ProliferationMode mode, int level, IReadOnlyDictionary<string, double> operatingParameters,
        out ProductionProcess process, out ProductionDiagnostic diagnostic);
}
