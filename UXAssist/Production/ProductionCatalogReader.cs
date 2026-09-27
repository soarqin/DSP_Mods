using System;
using System.Collections.Generic;
using System.Linq;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Production;

public static class ProductionCatalogReader
{
    public static ProductionCatalog Capture()
    {
        var itemProtos = LDB.items?.dataArray;
        var recipeProtos = LDB.recipes?.dataArray;
        if (itemProtos == null || itemProtos.Length == 0 || recipeProtos == null || recipeProtos.Length == 0)
            return null;

        var naturalItems = new HashSet<int>();
        if (LDB.veins?.dataArray != null)
        {
            foreach (var vein in LDB.veins.dataArray)
            {
                if (vein != null && vein.MiningItem > 0) naturalItems.Add(vein.MiningItem);
            }
        }

        var proliferators = new HashSet<int>();
        var buildings = new List<ProductionBuilding>();
        foreach (var item in itemProtos)
        {
            if (item?.prefabDesc == null) continue;
            var prefab = item.prefabDesc;
            if (prefab.isSpraycoster && prefab.incItemId != null)
                proliferators.UnionWith(prefab.incItemId);

            var category = prefab.isLab ? ProductionRecipeCategory.Research :
                prefab.isAssembler ? MapCategory(prefab.assemblerRecipeType) : ProductionRecipeCategory.None;
            var kind = ClassifyBuilding(prefab);
            if (category == ProductionRecipeCategory.None && kind == ProductionBuildingKind.Ordinary) continue;
            var speed = prefab.isLab ? prefab.labAssembleSpeed : prefab.assemblerSpeed;
            buildings.Add(new ProductionBuilding(item.ID, category, speed / 10000.0,
                prefab.workEnergyPerTick * 60.0, prefab.idleEnergyPerTick * 60.0, kind,
                prefab.genEnergyPerTick * 60.0, prefab.exchangeEnergyPerTick * 60.0,
                prefab.maxExcEnergy, prefab.emptyId, prefab.fullId,
                prefab.windForcedPower ? RenewablePowerSource.Wind :
                    prefab.photovoltaic ? RenewablePowerSource.Solar :
                    prefab.geothermal ? RenewablePowerSource.Geothermal : RenewablePowerSource.None,
                prefab.minerType == EMinerType.Vein ? ProductionMinerKind.Vein :
                    prefab.minerType == EMinerType.Oil ? ProductionMinerKind.Oil :
                    prefab.minerType == EMinerType.Water ? ProductionMinerKind.Water : ProductionMinerKind.None,
                prefab.minerPeriod, prefab.fuelMask, prefab.useFuelPerTick * 60.0,
                prefab.powerProductId, prefab.powerProductHeat, prefab.powerCatalystMask,
                prefab.isEjector ? prefab.ejectorChargeFrame : prefab.siloChargeFrame,
                prefab.isEjector ? prefab.ejectorColdFrame : prefab.siloColdFrame,
                prefab.isEjector ? prefab.ejectorBulletId : prefab.siloBulletId,
                prefab.stationCollectSpeed));
        }

        var defaultRecipes = new Dictionary<int, (int recipeId, int resultIndex)>();
        var recipes = new List<ProductionRecipe>();
        foreach (var recipe in recipeProtos)
        {
            if (recipe == null || recipe.Items == null || recipe.ItemCounts == null ||
                recipe.Results == null || recipe.ResultCounts == null ||
                recipe.Items.Length != recipe.ItemCounts.Length ||
                recipe.Results.Length != recipe.ResultCounts.Length) continue;

            recipes.Add(new ProductionRecipe(recipe.ID, MapCategory(recipe.Type), recipe.TimeSpend, recipe.productive,
                PairFlows(recipe.Items, recipe.ItemCounts), PairFlows(recipe.Results, recipe.ResultCounts)));
            for (var resultIndex = 0; resultIndex < recipe.Results.Length; resultIndex++)
            {
                var productId = recipe.Results[resultIndex];
                if (!defaultRecipes.TryGetValue(productId, out var old) || resultIndex < old.resultIndex)
                    defaultRecipes[productId] = (recipe.ID, resultIndex);
            }
        }

        AddSpecialRecipes(itemProtos, buildings, recipes, defaultRecipes);

        var items = new List<ProductionItem>();
        foreach (var item in itemProtos)
        {
            if (item == null) continue;
            var defaultRecipeId = item.maincraft?.ID ??
                                  (defaultRecipes.TryGetValue(item.ID, out var fallback) ? fallback.recipeId : 0);
            var isProliferator = proliferators.Contains(item.ID);
            items.Add(new ProductionItem(item.ID, defaultRecipeId,
                naturalItems.Contains(item.ID) || !string.IsNullOrEmpty(item.MiningFrom),
                ItemIds.DarkFogItemIds.Contains(item.ID), isProliferator ? item.Ability : 0,
                isProliferator ? item.HpMax : 0, item.HeatValue, item.FuelType,
                item.Productive, item.ID == ItemIds.StrangeAnnihilationFuelRod ? 2 : 1,
                item.CatalystType, item.CatalystType > 0 ? item.Ability * 0.01 : 0));
        }

        var matrixIds = new HashSet<int>(LabComponent.matrixIds);
        var technologies = new List<ProductionTechnology>();
        if (LDB.techs?.dataArray != null)
        {
            foreach (var tech in LDB.techs.dataArray)
            {
                if (tech == null || !tech.IsLabTech || tech.Items == null || tech.ItemPoints == null ||
                    tech.Items.Length != tech.ItemPoints.Length) continue;
                var requirements = new Dictionary<int, double>();
                for (var index = 0; index < tech.Items.Length; index++)
                {
                    if (matrixIds.Contains(tech.Items[index]))
                        requirements[tech.Items[index]] = tech.ItemPoints[index];
                }

                technologies.Add(new ProductionTechnology(tech.ID, requirements));
            }
        }

        return new ProductionCatalog(items, recipes, buildings,
            (double[])Cargo.accTableMilli.Clone(), (double[])Cargo.incTableMilli.Clone(),
            (double[])Cargo.powerTableRatio.Clone(), technologies);
    }

    private static void AddSpecialRecipes(IEnumerable<ItemProto> itemProtos,
        IEnumerable<ProductionBuilding> buildings, ICollection<ProductionRecipe> recipes,
        IDictionary<int, (int recipeId, int resultIndex)> defaultRecipes)
    {
        var itemIds = new HashSet<int>(itemProtos.Where(item => item != null).Select(item => item.ID));
        var recipeIds = new HashSet<int>(recipes.Select(recipe => recipe.Id));
        foreach (var building in buildings.OrderBy(building => building.ItemId))
        {
            var inputItemId = 0;
            int outputItemId;
            ProductionRecipeCategory category;
            if (building.Kind == ProductionBuildingKind.Exchanger)
            {
                inputItemId = building.EmptyAccumulatorItemId;
                outputItemId = building.FullAccumulatorItemId;
                category = ProductionRecipeCategory.Exchange;
                if (inputItemId <= 0 || inputItemId == outputItemId || !itemIds.Contains(inputItemId)) continue;
            }
            else if (building.Kind == ProductionBuildingKind.RayReceiver)
            {
                outputItemId = building.PowerProductItemId;
                category = ProductionRecipeCategory.PhotonStore;
            }
            else
            {
                continue;
            }

            if (outputItemId <= 0 || !itemIds.Contains(outputItemId) || !recipeIds.Add(-outputItemId)) continue;
            var inputs = inputItemId == 0 ? Array.Empty<KeyValuePair<int, double>>() :
                new[] { new KeyValuePair<int, double>(inputItemId, 1) };
            recipes.Add(new ProductionRecipe(-outputItemId, category, 1, false, inputs,
                new[] { new KeyValuePair<int, double>(outputItemId, 1) }));
            if (!defaultRecipes.ContainsKey(outputItemId)) defaultRecipes[outputItemId] = (-outputItemId, 0);
        }
    }

    private static IEnumerable<KeyValuePair<int, double>> PairFlows(int[] itemIds, int[] counts)
    {
        for (var index = 0; index < itemIds.Length; index++)
            yield return new KeyValuePair<int, double>(itemIds[index], counts[index]);
    }

    private static ProductionRecipeCategory MapCategory(ERecipeType type)
    {
        switch (type)
        {
            case ERecipeType.Smelt: return ProductionRecipeCategory.Smelt;
            case ERecipeType.Chemical: return ProductionRecipeCategory.Chemical;
            case ERecipeType.Refine: return ProductionRecipeCategory.Refine;
            case ERecipeType.Assemble: return ProductionRecipeCategory.Assemble;
            case ERecipeType.Particle: return ProductionRecipeCategory.Particle;
            case ERecipeType.Research: return ProductionRecipeCategory.Research;
            case ERecipeType.Fractionate: return ProductionRecipeCategory.Fractionate;
            case ERecipeType.Exchange: return ProductionRecipeCategory.Exchange;
            case ERecipeType.PhotonStore: return ProductionRecipeCategory.PhotonStore;
            default: return ProductionRecipeCategory.None;
        }
    }

    private static ProductionBuildingKind ClassifyBuilding(PrefabDesc prefab)
    {
        if (prefab.isLab || prefab.isAssembler) return ProductionBuildingKind.Ordinary;
        if (prefab.isFractionator) return ProductionBuildingKind.Fractionator;
        if (prefab.isPowerExchanger) return ProductionBuildingKind.Exchanger;
        if (prefab.gammaRayReceiver) return ProductionBuildingKind.RayReceiver;
        if (prefab.minerType != EMinerType.None) return ProductionBuildingKind.Miner;
        if (prefab.isCollectStation) return ProductionBuildingKind.Collector;
        if (prefab.isEjector) return ProductionBuildingKind.Ejector;
        if (prefab.isSilo) return ProductionBuildingKind.Silo;
        if (prefab.isPowerGen)
            return prefab.photovoltaic || prefab.windForcedPower || prefab.geothermal
                ? ProductionBuildingKind.RenewableGenerator : ProductionBuildingKind.FuelGenerator;
        if (prefab.isStation || prefab.isDispenser || prefab.isBattleBase)
            return ProductionBuildingKind.Logistics;
        if (prefab.isPowerConsumer && !prefab.isStation && !prefab.isDispenser)
            return ProductionBuildingKind.Auxiliary;
        return ProductionBuildingKind.Ordinary;
    }
}

public static class ProductionCatalogService
{
    private static ProductionCatalog _current;
    private static bool _initialized;

    public static ProductionCatalog Current => _current;
    public static event Action Changed;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        GameLogicProc.OnDataLoaded += Rebuild;
        GameLogicProc.OnGameBegin += Rebuild;
        GameLogicProc.OnGameEnd += Clear;
    }

    public static void Uninit()
    {
        if (!_initialized) return;
        _initialized = false;
        GameLogicProc.OnDataLoaded -= Rebuild;
        GameLogicProc.OnGameBegin -= Rebuild;
        GameLogicProc.OnGameEnd -= Clear;
        Clear();
    }

    public static void Rebuild()
    {
        _current = ProductionCatalogReader.Capture();
        Changed.InvokeSafe(UXAssist.Logger, nameof(Changed));
    }

    private static void Clear()
    {
        if (_current == null) return;
        _current = null;
        Changed.InvokeSafe(UXAssist.Logger, nameof(Changed));
    }
}
