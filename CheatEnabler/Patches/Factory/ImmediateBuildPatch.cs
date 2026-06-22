using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Patches.Factory;

internal class CargoTrafficPatch : PatchImpl<CargoTrafficPatch>
{
    private static bool _isBatchBuilding;
    private static bool _disableRefreshBatchesBuffers;
    private static bool _anyBelt;
    private static readonly HashSet<int> _alterBeltRendererIds = [];
    private static readonly HashSet<int> _alterPathRendererIds = [];
    private static readonly HashSet<int> _refreshPathUVIds = [];

    public static bool IsBatchBuilding => _isBatchBuilding;

    public static void StartBatchBuilding(PlanetFactory factory)
    {
        factory.BeginFlattenTerrain();
        factory.cargoTraffic._batch_buffer_no_refresh = true;
        PlanetFactory.batchBuild = true;
        _isBatchBuilding = true;
        _disableRefreshBatchesBuffers = true;
        _anyBelt = false;
    }

    public static void EndBatchBuilding(PlanetFactory factory)
    {
        PlanetFactory.batchBuild = false;
        factory.cargoTraffic._batch_buffer_no_refresh = false;
        factory.EndFlattenTerrain();
        _isBatchBuilding = false;
        var cargoTraffic = factory.cargoTraffic;
        var entityPool = factory.entityPool;
        var colChunks = factory.planet.physics?.colChunks;
        foreach (var beltId in _alterBeltRendererIds)
        {
            cargoTraffic.AlterBeltRenderer(beltId, entityPool, colChunks, false);
        }
        foreach (var pathId in _alterPathRendererIds)
        {
            cargoTraffic.AlterPathRenderer(pathId, false);
        }
        foreach (var pathId in _refreshPathUVIds)
        {
            cargoTraffic.RefreshPathUV(pathId);
        }
        _alterBeltRendererIds.Clear();
        _alterPathRendererIds.Clear();
        _refreshPathUVIds.Clear();
        _disableRefreshBatchesBuffers = false;
        if (_anyBelt)
        {
            factory.cargoTraffic.RefreshBeltBatchesBuffers();
            factory.cargoTraffic.RefreshPathBatchesBuffers();
        }
        _anyBelt = false;
        factory.planet.physics?.raycastLogic?.NotifyBatchObjectRemove();
        factory.planet.audio?.SetPlanetAudioDirty();
    }

    public static void TryEndBatchBuilding(PlanetFactory factory)
    {
        if (!_isBatchBuilding) return;
        EndBatchBuilding(factory);
    }

    public static void InstantBuild(Player player, PlanetFactory factory, int id)
    {
        if (!_isBatchBuilding) StartBatchBuilding(factory);
        _anyBelt = _anyBelt || (FactoryPatch.BeltIds?.Contains(factory.prebuildPool[id].protoId) ?? false);
        factory.BuildFinally(player, id, false);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterBeltRenderer))]
    private static bool CargoTraffic_AlterBeltRenderer_Prefix(int beltId)
    {
        if (!_isBatchBuilding) return true;
        _alterBeltRendererIds.Add(beltId);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterPathRenderer))]
    private static bool CargoTraffic_AlterPathRenderer_Prefix(int pathId)
    {
        if (!_isBatchBuilding) return true;
        _alterPathRendererIds.Add(pathId);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RefreshPathUV))]
    private static bool CargoTraffic_RefreshPathUV_Prefix(int pathId)
    {
        if (!_isBatchBuilding) return true;
        _refreshPathUVIds.Add(pathId);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RefreshBeltBatchesBuffers))]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RefreshPathBatchesBuffers))]
    private static bool CargoTraffic_RefreshBeltBatchesBuffers_Prefix()
    {
        return !_disableRefreshBatchesBuffers;
    }
}

internal class ImmediateBuild : PatchImpl<ImmediateBuild>
{
    protected override void OnEnable()
    {
        var factory = GameMain.mainPlayer?.factory;
        if (factory?.planet?.data != null)
        {
            FactoryPatch.ArrivePlanet(factory);
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CreatePrebuilds))]
    [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
    [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
    [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
    [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.End().MatchBack(false,
            new CodeMatch(OpCodes.Ret)
        );
        if (matcher.IsInvalid)
        {
            CheatEnabler.Logger.LogWarning($"Failed to patch CreatePrebuilds");
            return matcher.InstructionEnumeration();
        }

        matcher.Advance(-1);
        if (matcher.Opcode != OpCodes.Nop && (matcher.Opcode != OpCodes.Call || !matcher.Instruction.OperandIs(AccessTools.Method(typeof(System.GC), nameof(System.GC.Collect)))))
        {
            CheatEnabler.Logger.LogWarning($"Failed to patch CreatePrebuilds: last instruction is not `Nop` or `Call GC.Collect()`: {matcher.Instruction}");
            return matcher.InstructionEnumeration();
        }

        var labels = matcher.Labels;
        matcher.Labels = [];
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool.factory))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FactoryPatch), nameof(FactoryPatch.ArrivePlanet)))
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.FactoryConstructionSystemGameTick))]
    private static void GameLogic_FactoryConstructionSystemGameTick_Prefix(GameLogic __instance)
    {
        var time = __instance.timei;
        if (time % 6 != 0) return;
        var planet = GameMain.localPlanet;
        if (planet == null || !planet.factoryLoaded) return;
        var factory = planet.factory;
        if (factory == null || factory.prebuildCount <= 0) return;
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var total = factory.prebuildCursor - 1;
        var stepCount = total switch
        {
            < 256 => 1,
            < 2048 => 3,
            < 16384 => 10,
            _ => 20,
        };
        var step = (int)(time / 6 % stepCount);
        var start = 1 + total * step / stepCount;
        var end = 1 + total * (step + 1) / stepCount;
        for (var i = start; i < end; i++)
        {
            ref var prebuild = ref factory.prebuildPool[i];
            if (prebuild.id != i || prebuild.isDestroyed) continue;
            if (prebuild.itemRequired > 0)
            {
                int itemId = prebuild.protoId;
                int count = prebuild.itemRequired;
                player.package.TakeTailItems(ref itemId, ref count, out var _, false);
                if (count > 0)
                {
                    prebuild.itemRequired -= count;
                    if (prebuild.itemRequired <= 0)
                    {
                        CargoTrafficPatch.InstantBuild(player, factory, i);
                    }
                }
            }
        }
        if (CargoTrafficPatch.IsBatchBuilding)
        {
            for (var i = start - 1; i > 0; i--)
            {
                ref var prebuild = ref factory.prebuildPool[i];
                if (prebuild.id != i || prebuild.isDestroyed) continue;
                if (prebuild.itemRequired > 0)
                {
                    int itemId = prebuild.protoId;
                    int count = prebuild.itemRequired;
                    player.package.TakeTailItems(ref itemId, ref count, out var _, false);
                    if (count > 0)
                    {
                        prebuild.itemRequired -= count;
                        if (prebuild.itemRequired <= 0)
                        {
                            CargoTrafficPatch.InstantBuild(player, factory, i);
                        }
                    }
                }
            }
            for (var i = end; i <= total; i++)
            {
                ref var prebuild = ref factory.prebuildPool[i];
                if (prebuild.id != i || prebuild.isDestroyed) continue;
                if (prebuild.itemRequired > 0)
                {
                    int itemId = prebuild.protoId;
                    int count = prebuild.itemRequired;
                    player.package.TakeTailItems(ref itemId, ref count, out var _, false);
                    if (count > 0)
                    {
                        prebuild.itemRequired -= count;
                        if (prebuild.itemRequired <= 0)
                        {
                            CargoTrafficPatch.InstantBuild(player, factory, i);
                        }
                    }
                }
            }
            CargoTrafficPatch.EndBatchBuilding(factory);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UXAssist.Functions.PlanetFunctions), nameof(UXAssist.Functions.PlanetFunctions.BuildOrbitalCollectors))]
    private static void UXAssist_PlanetFunctions_BuildOrbitalCollectors_Postfix()
    {
        var factory = GameMain.mainPlayer?.factory;
        if (factory?.planet?.data != null)
        {
            FactoryPatch.ArrivePlanet(factory);
        }
    }
}
