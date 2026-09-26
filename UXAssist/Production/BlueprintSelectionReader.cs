using System;
using System.Collections.Generic;
using System.Linq;

namespace UXAssist.Production;

public sealed class BlueprintSelectionContext
{
    public Dictionary<int, Dictionary<string, double>> OperatingParametersByIndex { get; } =
        new Dictionary<int, Dictionary<string, double>>();
    public Dictionary<int, Dictionary<string, double>> OperatingParametersByObjectId { get; } =
        new Dictionary<int, Dictionary<string, double>>();
}

public sealed class BlueprintSelectionCapture
{
    public IReadOnlyList<ProductionBuildingSnapshot> Buildings { get; }
    public IReadOnlyList<ProductionDiagnostic> Diagnostics { get; }

    public BlueprintSelectionCapture(IEnumerable<ProductionBuildingSnapshot> buildings,
        IEnumerable<ProductionDiagnostic> diagnostics)
    {
        Buildings = Array.AsReadOnly(buildings.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
    }

    public FactoryBlackBoxRequest CreateRequest(bool proliferationEnabled)
    {
        var request = new FactoryBlackBoxRequest { ProliferationEnabled = proliferationEnabled };
        request.Buildings.AddRange(Buildings);
        request.CaptureDiagnostics.AddRange(Diagnostics);
        return request;
    }
}

public static class BlueprintSelectionReader
{
    public static BlueprintSelectionCapture Capture(PlanetFactory factory, IEnumerable<int> selectedObjectIds)
    {
        return Capture(factory, selectedObjectIds, null);
    }

    public static BlueprintSelectionCapture Capture(PlanetFactory factory, IEnumerable<int> selectedObjectIds,
        BlueprintSelectionContext context)
    {
        if (factory == null || ProductionCatalogService.Current == null)
            return Failure(ProductionDiagnosticCode.DataNotReady, "Factory or production data is not loaded.");
        if (selectedObjectIds == null)
            return Failure(ProductionDiagnosticCode.InvalidRequest, "The selected object IDs are required.");

        var snapshots = new List<ProductionBuildingSnapshot>();
        var diagnostics = new List<ProductionDiagnostic>();
        var seen = new HashSet<int>();
        foreach (var objectId in selectedObjectIds)
        {
            if (!seen.Add(objectId))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.DuplicateSelection,
                    "A blueprint-selected object ID is repeated."));
                continue;
            }

            if (!IsLiveObject(factory, objectId))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "A blueprint-selected object no longer exists."));
                continue;
            }

            var parameters = new BuildingParameters();
            if (!parameters.CopyFromFactoryObject(objectId, factory, false))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "The selected building's settings could not be captured."));
                continue;
            }

            if (!ProductionCatalogService.Current.Buildings.ContainsKey(parameters.itemId)) continue;
            var operation = new Dictionary<string, double>();
            var mode = GetMode(parameters);
            var speedFactor = GetLiveSpeed(factory, objectId, parameters.type, operation);
            AddSettings(parameters, operation);
            var kind = ProductionCatalogService.Current.Buildings[parameters.itemId].Kind;
            if (parameters.type == BuildingType.Lab && operation.ContainsKey("ResearchMode"))
                CaptureResearchSettings(factory, operation);
            if (kind == ProductionBuildingKind.Miner)
                CaptureMiningSettings(factory, objectId,
                    ProductionCatalogService.Current.Buildings[parameters.itemId], operation);
            if (kind == ProductionBuildingKind.Collector)
                CaptureCollectionSettings(factory, operation);
            if (kind == ProductionBuildingKind.FuelGenerator)
                CaptureFuelSettings(factory, objectId, operation);
            if (kind == ProductionBuildingKind.RayReceiver)
                CaptureRaySettings(factory, objectId, operation);
            if (kind == ProductionBuildingKind.Ejector || kind == ProductionBuildingKind.Silo)
                CaptureLaunchSettings(factory, objectId, kind, operation);
            if (context?.OperatingParametersByObjectId.TryGetValue(objectId, out var supplied) == true && supplied != null)
            {
                foreach (var entry in supplied) operation[entry.Key] = entry.Value;
            }

            var recipeId = kind == ProductionBuildingKind.Fractionator
                ? GetLiveFractionationRecipe(factory, objectId) : parameters.recipeId;
            if (kind == ProductionBuildingKind.Fractionator &&
                operation.TryGetValue("FluidItemId", out var fluidItemId))
                recipeId = FindFractionationRecipe((int)fluidItemId);
            ReportMissingSettings(kind, parameters, operation, diagnostics);
            if (kind == ProductionBuildingKind.Fractionator && recipeId == 0)
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The fractionator's input item is not selected.", buildingItemId: parameters.itemId));
            snapshots.Add(new ProductionBuildingSnapshot(objectId, parameters.itemId, recipeId, 1,
                mode, speedFactor, operation));
        }

        return new BlueprintSelectionCapture(snapshots, diagnostics);
    }

    public static BlueprintSelectionCapture FromBlueprint(IEnumerable<BlueprintBuilding> buildings,
        BlueprintSelectionContext context = null)
    {
        if (ProductionCatalogService.Current == null)
            return Failure(ProductionDiagnosticCode.DataNotReady, "Production data is not loaded.");
        if (buildings == null)
            return Failure(ProductionDiagnosticCode.InvalidRequest, "A blueprint building list is required.");

        var snapshots = new List<ProductionBuildingSnapshot>();
        var diagnostics = new List<ProductionDiagnostic>();
        var seen = new HashSet<int>();
        foreach (var blueprint in buildings)
        {
            if (blueprint == null)
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "A blueprint building is missing."));
                continue;
            }

            if (!seen.Add(blueprint.index))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.DuplicateSelection,
                    "A blueprint building index is repeated."));
                continue;
            }

            var model = LDB.models.Select(blueprint.modelIndex);
            if (model?.prefabDesc == null || LDB.items.Select(blueprint.itemId) == null)
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.UnknownItem,
                    "A blueprint building's model or item is unknown.", buildingItemId: blueprint.itemId));
                continue;
            }

            if (!ProductionCatalogService.Current.Buildings.ContainsKey(blueprint.itemId)) continue;

            var parameters = new BuildingParameters
            {
                type = ClassifyNativeType(model.prefabDesc),
                itemId = blueprint.itemId,
                modelIndex = blueprint.modelIndex,
                recipeId = blueprint.recipeId,
                filterId = blueprint.filterId
            };
            parameters.FromParamsArray(blueprint.parameters);
            var operation = new Dictionary<string, double>();
            AddSettings(parameters, operation);
            if (context?.OperatingParametersByIndex.TryGetValue(blueprint.index, out var supplied) == true)
            {
                foreach (var entry in supplied) operation[entry.Key] = entry.Value;
            }

            var kind = ProductionCatalogService.Current.Buildings[blueprint.itemId].Kind;
            var recipeId = blueprint.recipeId;
            if (kind == ProductionBuildingKind.Fractionator &&
                operation.TryGetValue("FluidItemId", out var fluidItemId))
                recipeId = FindFractionationRecipe((int)fluidItemId);
            ReportMissingSettings(kind, parameters, operation, diagnostics);
            if (kind == ProductionBuildingKind.Fractionator && recipeId == 0)
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "A blueprint fractionator needs its input item.", buildingItemId: blueprint.itemId));
            snapshots.Add(new ProductionBuildingSnapshot(0, blueprint.itemId, recipeId, 1,
                GetMode(parameters), operatingParameters: operation));
        }

        return new BlueprintSelectionCapture(snapshots, diagnostics);
    }

    private static BlueprintSelectionCapture Failure(ProductionDiagnosticCode code, string message)
    {
        return new BlueprintSelectionCapture(Array.Empty<ProductionBuildingSnapshot>(),
            new[] { new ProductionDiagnostic(code, message) });
    }

    private static bool IsLiveObject(PlanetFactory factory, int objectId)
    {
        if (objectId > 0)
            return factory.entityPool != null && objectId < factory.entityCursor &&
                   objectId < factory.entityPool.Length && factory.entityPool[objectId].id == objectId;
        if (objectId >= 0 || objectId == int.MinValue) return false;
        var prebuildId = -objectId;
        return factory.prebuildPool != null && prebuildId < factory.prebuildCursor &&
               prebuildId < factory.prebuildPool.Length && factory.prebuildPool[prebuildId].id == prebuildId;
    }

    private static double? GetLiveSpeed(PlanetFactory factory, int objectId, BuildingType type,
        IDictionary<string, double> operation)
    {
        if (objectId <= 0) return null;
        ref var entity = ref factory.entityPool[objectId];
        var system = factory.factorySystem;
        if (type == BuildingType.Assembler && entity.assemblerId > 0 && system?.assemblerPool != null &&
            entity.assemblerId < system.assemblerPool.Length &&
            system.assemblerPool[entity.assemblerId].id == entity.assemblerId)
            return system.assemblerPool[entity.assemblerId].speed / 10000.0;
        if (type == BuildingType.Lab && entity.labId > 0 && system?.labPool != null &&
            entity.labId < system.labPool.Length && system.labPool[entity.labId].id == entity.labId)
        {
            ref var lab = ref system.labPool[entity.labId];
            if (lab.researchMode && lab.techId > 0) operation["TechId"] = lab.techId;
            return lab.speed / 10000.0;
        }

        return null;
    }

    private static int GetLiveFractionationRecipe(PlanetFactory factory, int objectId)
    {
        if (objectId <= 0 || factory.factorySystem?.fractionatorPool == null) return 0;
        var fractionatorId = factory.entityPool[objectId].fractionatorId;
        var pool = factory.factorySystem.fractionatorPool;
        return fractionatorId > 0 && fractionatorId < pool.Length && pool[fractionatorId].id == fractionatorId
            ? FindFractionationRecipe(pool[fractionatorId].fluidId) : 0;
    }

    private static void CaptureMiningSettings(PlanetFactory factory, int objectId, ProductionBuilding building,
        IDictionary<string, double> operation)
    {
        var history = factory.gameData?.history;
        if (history != null) operation["MiningSpeedMultiplier"] = history.miningSpeedScale;
        if (building.MinerKind == ProductionMinerKind.Water && factory.planet?.waterItemId > 0)
            operation["ResourceItemId"] = factory.planet.waterItemId;
        if (objectId <= 0 || factory.factorySystem?.minerPool == null) return;

        var minerId = factory.entityPool[objectId].minerId;
        var pool = factory.factorySystem.minerPool;
        if (minerId <= 0 || minerId >= pool.Length || pool[minerId].id != minerId) return;
        ref var miner = ref pool[minerId];
        operation["MachineSpeedFactor"] = miner.speed / ProductionUnits.FixedPointScale;
        operation["SpeedDamper"] = miner.speedDamper;
        if (building.MinerKind == ProductionMinerKind.Water || miner.veinCount <= 0 ||
            miner.veins == null || miner.veinCount > miner.veins.Length || factory.veinPool == null) return;

        var resourceItemId = 0;
        for (var index = 0; index < miner.veinCount; index++)
        {
            var veinId = miner.veins[index];
            if (veinId <= 0 || veinId >= factory.veinPool.Length ||
                factory.veinPool[veinId].id != veinId || factory.veinPool[veinId].productId <= 0 ||
                resourceItemId != 0 && resourceItemId != factory.veinPool[veinId].productId) return;
            resourceItemId = factory.veinPool[veinId].productId;
        }

        operation["ResourceItemId"] = resourceItemId;
        if (building.MinerKind == ProductionMinerKind.Vein)
            operation["VeinCount"] = miner.veinCount;
        else if (building.MinerKind == ProductionMinerKind.Oil)
            operation["OilUnits"] = factory.veinPool[miner.veins[0]].amount * VeinData.oilSpeedMultiplier;
    }

    private static void CaptureCollectionSettings(PlanetFactory factory,
        IDictionary<string, double> operation)
    {
        var planet = factory.planet;
        if (planet?.gasItems == null || planet.gasSpeeds == null ||
            planet.gasItems.Length != planet.gasSpeeds.Length) return;
        operation["GasCount"] = planet.gasItems.Length;
        operation["GasTotalHeat"] = planet.gasTotalHeat;
        if (factory.gameData?.history != null)
            operation["MiningSpeedMultiplier"] = factory.gameData.history.miningSpeedScale;
        for (var index = 0; index < planet.gasItems.Length; index++)
        {
            operation[$"GasItemId{index}"] = planet.gasItems[index];
            operation[$"GasSpeedPerSecond{index}"] = planet.gasSpeeds[index];
        }
    }

    private static void CaptureResearchSettings(PlanetFactory factory,
        IDictionary<string, double> operation)
    {
        var history = factory.gameData?.history;
        if (history == null) return;
        operation["ResearchSpeed"] = history.techSpeed;
        if (!operation.ContainsKey("TechId") && history.currentTech > 0)
            operation["TechId"] = history.currentTech;
    }

    private static void CaptureFuelSettings(PlanetFactory factory, int objectId,
        IDictionary<string, double> operation)
    {
        if (objectId <= 0 || factory.powerSystem?.genPool == null) return;
        var generatorId = factory.entityPool[objectId].powerGenId;
        var pool = factory.powerSystem.genPool;
        if (generatorId <= 0 || generatorId >= pool.Length || pool[generatorId].id != generatorId) return;
        var fuelId = pool[generatorId].curFuelId > 0 ? pool[generatorId].curFuelId : pool[generatorId].fuelId;
        if (fuelId > 0) operation["FuelItemId"] = fuelId;
    }

    private static void CaptureRaySettings(PlanetFactory factory, int objectId,
        IDictionary<string, double> operation)
    {
        if (factory.gameData?.history != null)
            operation["SolarEnergyLossRate"] = factory.gameData.history.solarEnergyLossRate;
        if (objectId <= 0 || factory.powerSystem?.genPool == null) return;
        var generatorId = factory.entityPool[objectId].powerGenId;
        var pool = factory.powerSystem.genPool;
        if (generatorId <= 0 || generatorId >= pool.Length || pool[generatorId].id != generatorId) return;
        ref var generator = ref pool[generatorId];
        operation["Mode0"] = generator.productId;
        operation["LensItemId"] = generator.catalystPoint > 0 || generator.catalystCount > 0
            ? generator.catalystPoint > 0 && generator.curCatalystId > 0
                ? generator.curCatalystId : generator.catalystId : 0;
    }

    private static void CaptureLaunchSettings(PlanetFactory factory, int objectId,
        ProductionBuildingKind kind, IDictionary<string, double> operation)
    {
        if (objectId <= 0 || factory.factorySystem == null) return;
        ref var entity = ref factory.entityPool[objectId];
        var system = factory.factorySystem;
        if (kind == ProductionBuildingKind.Silo)
        {
            var siloId = entity.siloId;
            if (system.siloPool == null || siloId <= 0 || siloId >= system.siloPool.Length ||
                system.siloPool[siloId].id != siloId) return;
            operation["BoostEnabled"] = system.siloPool[siloId].boost ? 1 : 0;
            operation["LaunchAvailable"] = factory.dysonSphere != null &&
                                           factory.dysonSphere.GetAutoNodeCount() > 0 ? 1 : 0;
            return;
        }

        var ejectorId = entity.ejectorId;
        if (system.ejectorPool == null || ejectorId <= 0 || ejectorId >= system.ejectorPool.Length ||
            system.ejectorPool[ejectorId].id != ejectorId) return;
        ref var ejector = ref system.ejectorPool[ejectorId];
        operation["BoostEnabled"] = ejector.boost ? 1 : 0;
        var swarm = factory.dysonSphere?.swarm;
        operation["LaunchAvailable"] = 0;
        if (swarm?.orbits == null) return;
        if (ejector.autoOrbit)
        {
            for (var orbitId = 1; orbitId < swarm.orbitCursor; orbitId++)
            {
                if (swarm.orbits[orbitId].id != orbitId || !swarm.orbits[orbitId].enabled) continue;
                operation["LaunchAvailable"] = 1;
                break;
            }
        }
        else if (ejector.orbitId > 0 && ejector.orbitId < swarm.orbitCursor &&
                 swarm.orbits[ejector.orbitId].id == ejector.orbitId &&
                 swarm.orbits[ejector.orbitId].enabled)
        {
            operation["LaunchAvailable"] = 1;
        }
    }

    private static int FindFractionationRecipe(int inputItemId)
    {
        var recipes = RecipeProto.fractionatorRecipes;
        if (inputItemId <= 0 || recipes == null) return 0;
        foreach (var recipe in recipes)
        {
            if (recipe.Items.Length == 1 && recipe.Items[0] == inputItemId) return recipe.ID;
        }

        return 0;
    }

    private static void AddSettings(BuildingParameters parameters, IDictionary<string, double> operation)
    {
        operation["Mode0"] = parameters.mode0;
        operation["Mode1"] = parameters.mode1;
        operation["Mode2"] = parameters.mode2;
        operation["FilterItemId"] = parameters.filterId;
        if (parameters.type == BuildingType.Lab && parameters.mode0 == 2)
            operation["ResearchMode"] = 1;
    }

    private static void ReportMissingSettings(ProductionBuildingKind kind, BuildingParameters parameters,
        IReadOnlyDictionary<string, double> operation, ICollection<ProductionDiagnostic> diagnostics)
    {
        var requiredKey = parameters.type == BuildingType.Lab && parameters.mode0 == 2 ? "TechId" :
            kind == ProductionBuildingKind.Miner ? "ResourceItemId" :
            kind == ProductionBuildingKind.Collector ? "GasCount" :
            kind == ProductionBuildingKind.FuelGenerator ? "FuelItemId" :
            kind == ProductionBuildingKind.Fractionator ? "CirculatingItemsPerMinute" : null;
        if (requiredKey != null && !operation.ContainsKey(requiredKey))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                $"The selected building needs the {requiredKey} operating parameter.",
                buildingItemId: parameters.itemId));
        if (parameters.type == BuildingType.Lab && parameters.mode0 == 2 &&
            !operation.ContainsKey("ResearchSpeed"))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The research lab needs the current research technology speed.",
                buildingItemId: parameters.itemId));
        if (kind == ProductionBuildingKind.Fractionator && !operation.ContainsKey("StackSize"))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The fractionator needs its circulating stack size.", buildingItemId: parameters.itemId));
        if (kind == ProductionBuildingKind.RayReceiver)
        {
            if (!operation.ContainsKey("LensItemId"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The receiver needs a lens item ID, or zero for no lens.", buildingItemId: parameters.itemId));
            if (!operation.ContainsKey("SolarEnergyLossRate"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The receiver needs the solar energy loss technology setting.",
                    buildingItemId: parameters.itemId));
        }
        if (kind == ProductionBuildingKind.Collector)
        {
            if (!operation.ContainsKey("GasTotalHeat") ||
                !operation.ContainsKey("MiningSpeedMultiplier"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The collector needs the planet's gas energy and current mining technology speed.",
                    buildingItemId: parameters.itemId));
            if (operation.TryGetValue("GasCount", out var count) && count > 0 && count <= 16 &&
                count == Math.Truncate(count))
            {
                for (var index = 0; index < (int)count; index++)
                {
                    if (operation.ContainsKey($"GasItemId{index}") &&
                        operation.ContainsKey($"GasSpeedPerSecond{index}")) continue;
                    diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                        $"The collector needs gas item and speed for resource {index}.",
                        buildingItemId: parameters.itemId));
                }
            }
        }
        if (kind == ProductionBuildingKind.Ejector || kind == ProductionBuildingKind.Silo)
        {
            if (!operation.ContainsKey("LaunchAvailable"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The launcher needs a usable orbit or Dyson-sphere node setting.",
                    buildingItemId: parameters.itemId));
            var boostKey = kind == ProductionBuildingKind.Ejector ? "Mode1" : "Mode0";
            if (operation.TryGetValue(boostKey, out var boostMode) && boostMode == 1 &&
                !operation.ContainsKey("BoostEnabled"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The launcher needs its current sandbox boost setting.",
                    buildingItemId: parameters.itemId));
        }
        if (kind == ProductionBuildingKind.Miner)
        {
            if (!operation.ContainsKey("MiningSpeedMultiplier"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The miner needs the current mining technology speed.", buildingItemId: parameters.itemId));
            var minerKind = ProductionCatalogService.Current.Buildings[parameters.itemId].MinerKind;
            var factorName = minerKind == ProductionMinerKind.Vein ? "VeinCount" :
                minerKind == ProductionMinerKind.Oil ? "OilUnits" : null;
            if (factorName != null && !operation.ContainsKey(factorName))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    $"The miner needs its {factorName} resource factor.", buildingItemId: parameters.itemId));
        }
    }

    private static ProliferationMode GetMode(BuildingParameters parameters)
    {
        if (parameters.type != BuildingType.Assembler && parameters.type != BuildingType.Lab)
            return ProliferationMode.None;

        var recipeId = parameters.recipeId;
        var filterId = parameters.filterId;
        var mode0 = parameters.mode0;
        var mode1 = parameters.mode1;
        if (parameters.type == BuildingType.Assembler)
            BuildingParameters.SimpleParamFromParamsArray(parameters.type, parameters.parameters,
                ref recipeId, ref filterId, ref mode0, ref mode1);
        var recipe = LDB.recipes.Select(recipeId);
        return mode1 == 1 || recipe != null && !recipe.productive
            ? ProliferationMode.Speedup : ProliferationMode.ExtraProducts;
    }

    private static BuildingType ClassifyNativeType(PrefabDesc prefab)
    {
        if (prefab.isAssembler) return BuildingType.Assembler;
        if (prefab.isLab) return BuildingType.Lab;
        if (prefab.isEjector) return BuildingType.Ejector;
        if (prefab.isSilo) return BuildingType.Silo;
        if (prefab.isPowerExchanger) return BuildingType.Exchanger;
        if (prefab.gammaRayReceiver) return BuildingType.Gamma;
        if (prefab.minerType != EMinerType.None) return BuildingType.Miner;
        if (prefab.isPowerGen && prefab.fuelMask == 4) return BuildingType.ArtifacialStar;
        if (prefab.geothermal) return BuildingType.Geothermal;
        return BuildingType.Other;
    }
}
