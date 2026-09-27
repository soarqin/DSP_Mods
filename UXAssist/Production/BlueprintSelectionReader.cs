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
        var catalog = ProductionCatalogService.Current;
        if (factory == null || catalog == null)
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

            var protoId = GetLiveProtoId(factory, objectId);
            if (protoId == 0)
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "A blueprint-selected object no longer exists."));
                continue;
            }

            if (!catalog.Buildings.TryGetValue(protoId, out var building)) continue;
            var parameters = new BuildingParameters();
            if (!parameters.CopyFromFactoryObject(objectId, factory, false))
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
                    "The selected building's settings could not be captured.", buildingItemId: protoId));
                continue;
            }

            var operation = new Dictionary<string, double>();
            var speedFactor = GetLiveSpeed(factory, objectId, parameters.type, operation);
            AddSettings(parameters, operation);
            if (building.Kind == ProductionBuildingKind.Logistics)
            {
                if (objectId > 0)
                    CaptureLiveChargePower(factory, objectId, operation);
                else
                {
                    ref var prebuild = ref factory.prebuildPool[-objectId];
                    CaptureStoredChargePower(building, parameters, prebuild.parameters, operation);
                }
            }
            if (operation.ContainsKey("ResearchMode")) CaptureResearchSettings(factory, operation);
            switch (building.Kind)
            {
                case ProductionBuildingKind.Miner:
                    CaptureMiningSettings(factory, objectId, building, operation);
                    break;
                case ProductionBuildingKind.Collector:
                    CaptureCollectionSettings(factory, operation);
                    break;
                case ProductionBuildingKind.FuelGenerator:
                    CaptureFuelSettings(factory, objectId, operation);
                    break;
                case ProductionBuildingKind.RayReceiver:
                    CaptureRaySettings(factory, objectId, operation);
                    break;
                case ProductionBuildingKind.Ejector:
                case ProductionBuildingKind.Silo:
                    CaptureLaunchSettings(factory, objectId, building.Kind, operation);
                    break;
            }

            ApplySupplied(context?.OperatingParametersByObjectId, objectId, operation);
            var recipeId = building.Kind == ProductionBuildingKind.Fractionator
                ? GetLiveFractionationRecipe(factory, objectId) : parameters.recipeId;
            snapshots.Add(CreateSnapshot(catalog, building, objectId, parameters, recipeId, speedFactor, operation,
                diagnostics));
        }

        return new BlueprintSelectionCapture(snapshots, diagnostics);
    }

    public static BlueprintSelectionCapture FromBlueprint(IEnumerable<BlueprintBuilding> buildings,
        BlueprintSelectionContext context = null)
    {
        var catalog = ProductionCatalogService.Current;
        if (catalog == null)
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

            var item = LDB.items.Select(blueprint.itemId);
            var model = LDB.models.Select(blueprint.modelIndex);
            if (item == null || model?.prefabDesc == null)
            {
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.UnknownItem,
                    "A blueprint building's model or item is unknown.", buildingItemId: blueprint.itemId));
                continue;
            }

            if (!catalog.Buildings.TryGetValue(blueprint.itemId, out var building)) continue;

            // Decode the settings the same way native blueprint paste does.
            var parameters = new BuildingParameters();
            parameters.CopyFromBuildPreview(new BuildPreview
            {
                item = item,
                desc = model.prefabDesc,
                recipeId = blueprint.recipeId,
                filterId = blueprint.filterId,
                parameters = blueprint.parameters
            });
            var operation = new Dictionary<string, double>();
            AddSettings(parameters, operation);
            if (building.Kind == ProductionBuildingKind.Logistics)
                CaptureStoredChargePower(building, parameters, blueprint.parameters, operation);
            ApplySupplied(context?.OperatingParametersByIndex, blueprint.index, operation);
            snapshots.Add(CreateSnapshot(catalog, building, 0, parameters, parameters.recipeId, null, operation,
                diagnostics));
        }

        return new BlueprintSelectionCapture(snapshots, diagnostics);
    }

    private static BlueprintSelectionCapture Failure(ProductionDiagnosticCode code, string message)
    {
        return new BlueprintSelectionCapture(Array.Empty<ProductionBuildingSnapshot>(),
            new[] { new ProductionDiagnostic(code, message) });
    }

    private static int GetLiveProtoId(PlanetFactory factory, int objectId)
    {
        if (objectId > 0)
            return factory.entityPool != null && objectId < factory.entityCursor &&
                   objectId < factory.entityPool.Length && factory.entityPool[objectId].id == objectId
                ? factory.entityPool[objectId].protoId : 0;
        if (objectId == 0 || objectId == int.MinValue) return 0;
        var prebuildId = -objectId;
        return factory.prebuildPool != null && prebuildId < factory.prebuildCursor &&
               prebuildId < factory.prebuildPool.Length && factory.prebuildPool[prebuildId].id == prebuildId
            ? factory.prebuildPool[prebuildId].protoId : 0;
    }

    private static void ApplySupplied(IReadOnlyDictionary<int, Dictionary<string, double>> supplied, int key,
        IDictionary<string, double> operation)
    {
        if (supplied == null || !supplied.TryGetValue(key, out var settings) || settings == null) return;
        foreach (var entry in settings) operation[entry.Key] = entry.Value;
    }

    private static void CaptureLiveChargePower(PlanetFactory factory, int objectId,
        IDictionary<string, double> operation)
    {
        var powerSystem = factory.powerSystem;
        var pool = powerSystem?.consumerPool;
        var consumerId = factory.entityPool[objectId].powerConId;
        if (pool == null || consumerId <= 0 || consumerId >= powerSystem.consumerCursor ||
            consumerId >= pool.Length || pool[consumerId].id != consumerId ||
            pool[consumerId].entityId != objectId) return;
        operation["ChargePowerWatts"] = pool[consumerId].workEnergyPerTick * ProductionUnits.TicksPerSecond;
    }

    private static void CaptureStoredChargePower(ProductionBuilding building, BuildingParameters parameters,
        int[] storedParameters, IDictionary<string, double> operation)
    {
        var watts = building.WorkingPowerWatts;
        switch (parameters.type)
        {
            case BuildingType.Station:
                if (storedParameters != null &&
                    storedParameters.Length >= BuildingParameters.kStationParamsLength && storedParameters[320] > 0)
                    watts = storedParameters[320] * ProductionUnits.TicksPerSecond;
                break;
            case BuildingType.Dispenser:
                if (storedParameters != null && storedParameters.Length >= BuildingParameters.kAddonParamsLength)
                    watts = storedParameters[2] * ProductionUnits.TicksPerSecond;
                break;
            case BuildingType.BattleBase:
                var index = parameters.mode1 == 0 ? 10 : 70;
                if (storedParameters != null && storedParameters.Length > index)
                    watts = storedParameters[index] * ProductionUnits.TicksPerSecond;
                break;
            default:
                return;
        }

        operation["ChargePowerWatts"] = watts;
    }

    private static ProductionBuildingSnapshot CreateSnapshot(ProductionCatalog catalog, ProductionBuilding building,
        int objectId, BuildingParameters parameters, int recipeId, double? speedFactor,
        Dictionary<string, double> operation, ICollection<ProductionDiagnostic> diagnostics)
    {
        if (building.Kind == ProductionBuildingKind.Fractionator &&
            operation.TryGetValue("FluidItemId", out var fluidItemId))
            recipeId = FindFractionationRecipe((int)fluidItemId);
        ReportMissingSettings(building, parameters, operation, diagnostics);
        if (building.Kind == ProductionBuildingKind.Fractionator && recipeId == 0)
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The fractionator's input item is not selected.", buildingItemId: building.ItemId));
        return new ProductionBuildingSnapshot(objectId, building.ItemId, recipeId, 1, GetMode(catalog, parameters),
            speedFactor, operation);
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
        ref var generator = ref pool[generatorId];
        var fuelId = generator.curFuelId > 0 ? generator.curFuelId : generator.fuelId;
        if (fuelId > 0) operation["FuelItemId"] = fuelId;
        if (generator.fuelMask == 4) operation[SandboxBoost.EnabledKey] = generator.boost ? 1 : 0;
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
            operation[SandboxBoost.EnabledKey] = system.siloPool[siloId].boost ? 1 : 0;
            operation["LaunchAvailable"] = factory.dysonSphere != null &&
                                           factory.dysonSphere.GetAutoNodeCount() > 0 ? 1 : 0;
            return;
        }

        var ejectorId = entity.ejectorId;
        if (system.ejectorPool == null || ejectorId <= 0 || ejectorId >= system.ejectorPool.Length ||
            system.ejectorPool[ejectorId].id != ejectorId) return;
        ref var ejector = ref system.ejectorPool[ejectorId];
        operation[SandboxBoost.EnabledKey] = ejector.boost ? 1 : 0;
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
        if (parameters.type == BuildingType.Lab && parameters.mode0 == 2)
            operation["ResearchMode"] = 1;
    }

    private static void ReportMissingSettings(ProductionBuilding building, BuildingParameters parameters,
        IReadOnlyDictionary<string, double> operation, ICollection<ProductionDiagnostic> diagnostics)
    {
        var kind = building.Kind;
        var requiredKey = parameters.type == BuildingType.Lab && parameters.mode0 == 2 ? "TechId" :
            kind == ProductionBuildingKind.Miner ? "ResourceItemId" :
            kind == ProductionBuildingKind.Collector ? "GasCount" :
            kind == ProductionBuildingKind.FuelGenerator ? "FuelItemId" :
            kind == ProductionBuildingKind.Fractionator ? "CirculatingItemsPerMinute" : null;
        if (requiredKey != null && !operation.ContainsKey(requiredKey))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                $"The selected building needs the {requiredKey} operating parameter.",
                buildingItemId: building.ItemId));
        if (parameters.type == BuildingType.Lab && parameters.mode0 == 2 &&
            !operation.ContainsKey("ResearchSpeed"))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The research lab needs the current research technology speed.",
                buildingItemId: building.ItemId));
        if (kind == ProductionBuildingKind.Fractionator && !operation.ContainsKey("StackSize"))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The fractionator needs its circulating stack size.", buildingItemId: building.ItemId));
        if (kind == ProductionBuildingKind.RayReceiver)
        {
            if (!operation.ContainsKey("LensItemId"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The receiver needs a lens item ID, or zero for no lens.", buildingItemId: building.ItemId));
            if (!operation.ContainsKey("SolarEnergyLossRate"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The receiver needs the solar energy loss technology setting.",
                    buildingItemId: building.ItemId));
        }
        if (kind == ProductionBuildingKind.Collector)
        {
            if (!operation.ContainsKey("GasTotalHeat") ||
                !operation.ContainsKey("MiningSpeedMultiplier"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The collector needs the planet's gas energy and current mining technology speed.",
                    buildingItemId: building.ItemId));
            if (operation.TryGetValue("GasCount", out var count) && count > 0 && count <= 16 &&
                count == Math.Truncate(count))
            {
                for (var index = 0; index < (int)count; index++)
                {
                    if (operation.ContainsKey($"GasItemId{index}") &&
                        operation.ContainsKey($"GasSpeedPerSecond{index}")) continue;
                    diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                        $"The collector needs gas item and speed for resource {index}.",
                        buildingItemId: building.ItemId));
                }
            }
        }
        if ((kind == ProductionBuildingKind.Ejector || kind == ProductionBuildingKind.Silo) &&
            !operation.ContainsKey("LaunchAvailable"))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The launcher needs a usable orbit or Dyson-sphere node setting.",
                buildingItemId: building.ItemId));
        var boostKey = SandboxBoost.ModeKey(building);
        if (boostKey != null && operation.TryGetValue(boostKey, out var boostMode) && boostMode == 1 &&
            !operation.ContainsKey(SandboxBoost.EnabledKey))
            diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                "The building needs its current sandbox boost setting.", buildingItemId: building.ItemId));
        if (kind == ProductionBuildingKind.Miner)
        {
            if (!operation.ContainsKey("MiningSpeedMultiplier"))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    "The miner needs the current mining technology speed.", buildingItemId: building.ItemId));
            var factorName = building.MinerKind == ProductionMinerKind.Vein ? "VeinCount" :
                building.MinerKind == ProductionMinerKind.Oil ? "OilUnits" : null;
            if (factorName != null && !operation.ContainsKey(factorName))
                diagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.MissingOperatingParameter,
                    $"The miner needs its {factorName} resource factor.", buildingItemId: building.ItemId));
        }
    }

    private static ProliferationMode GetMode(ProductionCatalog catalog, BuildingParameters parameters)
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
        return mode1 == 1 || catalog.Recipes.TryGetValue(recipeId, out var recipe) && !recipe.Productive
            ? ProliferationMode.Speedup : ProliferationMode.ExtraProducts;
    }
}
