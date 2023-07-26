using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;

namespace LabOpt;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LabOptPatch : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(LabOptPatch));
    }

    // Remove use of LabComponent.UpdateOutputToNext() for single-thread mode
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), "GameTick")]
    private static IEnumerable<CodeInstruction> RemoveLabUpdateOutputToNextForSingleThread(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FactorySystem), nameof(FactorySystem.GameTickLabOutputToNext), new[] { typeof(long), typeof(bool) })))
            .Advance(-4)
            .RemoveInstructions(5);
        return matcher.InstructionEnumeration();
    }

    // Remove use of LabComponent.UpdateOutputToNext() for multi-threads mode
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WorkerThreadExecutor), nameof(WorkerThreadExecutor.ComputerThread))]
    private static IEnumerable<CodeInstruction> RemoveLabUpdateOutputToNextForMultiThreads(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WorkerThreadExecutor), nameof(WorkerThreadExecutor.LabOutput2NextPartExecute))))
            .Advance(-1)
            .RemoveInstructions(2);
        return matcher.InstructionEnumeration();
    }

    // Set rootLabId for LabComponent on lab stacking
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.SetLabNextTarget))]
    private static IEnumerable<CodeInstruction> FactorySystem_SetLabNextTarget_Transpier(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.labId))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.nextLabId))))
            .Advance(2)
            .Insert(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.SetRootLabIdForStacking))));
        return matcher.InstructionEnumeration();
    }

    // Set rootLabId for LabComponent after loading game-save
    [HarmonyPostfix]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.Import))]
    private static void FactorySystem_Import_Postfix(FactorySystem __instance)
    {
        LabOptPatchFunctions.SetRootLabIdOnLoading(__instance);
    }

    // Insert to root lab
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
    private static IEnumerable<CodeInstruction> PlanetFactory_InsertInto_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.labId))))
            .MatchForward(false, new CodeMatch(OpCodes.Ret))
            .Advance(1)
            .Insert(
                // rootLabId = this.factorySystem.labPool[labId].rootLabId;
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
                new CodeInstruction(OpCodes.Ldloc_S, 5),
                new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
                new CodeInstruction(OpCodes.Stloc_S, 6),
                // if (rootLabId <= 0) goto label1;
                new CodeInstruction(OpCodes.Ldloc_S, 6),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ble, label1),
                // labId = rootLabId;
                new CodeInstruction(OpCodes.Ldloc_S, 6),
                new CodeInstruction(OpCodes.Stloc_S, 5),
                // entityId = this.factorySystem.labPool[labId].entityId;
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
                new CodeInstruction(OpCodes.Ldloc_S, 5),
                new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.entityId))),
                new CodeInstruction(OpCodes.Starg_S, 1)
                // lable1:
            );
        matcher.Instruction.labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    // Fill into root lab
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
    private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastFillIn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))))
            .Advance(2)
            .InsertAndAdvance(
                // rootLabId = labPool[labId].rootLabId;
                new CodeInstruction(OpCodes.Ldloc_S, 69),
                new CodeInstruction(OpCodes.Ldloc_S, 68),
                new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
                new CodeInstruction(OpCodes.Stloc_S, 73),
                // if (rootLabId <= 0) goto label1;
                new CodeInstruction(OpCodes.Ldloc_S, 73),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ble, label1),
                // labId = rootLabId;
                new CodeInstruction(OpCodes.Ldloc_S, 73),
                new CodeInstruction(OpCodes.Stloc_S, 68)
                // lable1:
            );
        matcher.Instruction.labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    // Fill into root lab
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow.OnItemButtonClick))]
    private static IEnumerable<CodeInstruction> UILabWindow_OnItemButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var local1 = generator.DeclareLocal(typeof(int));
        var label1 = generator.DefineLabel();
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ret))
            .Advance(1);
        var labels = matcher.Instruction.labels;
        matcher.InsertAndAdvance(
            // labId = this.labId;
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(UILabWindow), nameof(UILabWindow.labId))),
            new CodeInstruction(OpCodes.Stloc_S, local1),
            // rootLabId = this.factorySystem.labPool[labId].rootLabId;
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
            new CodeInstruction(OpCodes.Stloc_S, 27),
            // if (rootLabId <= 0) goto label1;
            new CodeInstruction(OpCodes.Ldloc_S, 27),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble, label1),
            // labId = rootLabId;
            new CodeInstruction(OpCodes.Ldloc_S, 27),
            new CodeInstruction(OpCodes.Stloc_S, local1)
            // lable1:
        );
        matcher.Instruction.labels = new List<Label> { label1 };
        for (;;)
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UILabWindow), nameof(UILabWindow.labId))));
            if (matcher.IsInvalid) break;
            var instr = new CodeInstruction(OpCodes.Ldloc_S, local1).WithLabels(matcher.Instruction.labels);
            matcher.RemoveInstructions(2).InsertAndAdvance(instr);
        }

        return matcher.InstructionEnumeration();
    }

    // Display UI: use root lab's count
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow._OnUpdate))]
    private static IEnumerable<CodeInstruction> UILabWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var local1 = generator.DeclareLocal(typeof(LabComponent).MakeByRefType());
        var matcher = new CodeMatcher(instructions, generator);
        matcher.End().MatchBack(false,
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(LabComponent), nameof(LabComponent.matrixMode)))
        ).Advance(-1);
        var labels = matcher.Instruction.labels;
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        matcher.InsertAndAdvance(
            // rootLabId = labComponent.rootLabId;
            new CodeInstruction(OpCodes.Ldloca_S, 0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
            new CodeInstruction(OpCodes.Stloc_S, 46),
            // if (rootLabId <= 0) goto label1;
            new CodeInstruction(OpCodes.Ldloc_S, 46),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble, label1),
            // labComponent2 = ref this.factorySystem.labPool[rootLabId];
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Ldloc_S, 46),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Stloc_S, local1),
            new CodeInstruction(OpCodes.Br, label2),
            // lable1:
            // labComponent2 = ref labComponent;
            new CodeInstruction(OpCodes.Ldloca_S, 0).WithLabels(label1),
            new CodeInstruction(OpCodes.Stloc_S, local1)
        );
        matcher.Instruction.labels = new List<Label>{label2};
        matcher.Advance(2);
        var startPos = matcher.Pos;
        for (;;)
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.served)))
            );
            if (matcher.IsInvalid) break;
            matcher.Set(OpCodes.Ldloc_S, local1).Advance(2);
        }

        matcher.Start().Advance(startPos);
        for (;;)
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.incServed)))
            );
            if (matcher.IsInvalid) break;
            matcher.Set(OpCodes.Ldloc_S, local1).Advance(2);
        }

        matcher.Start().Advance(startPos);
        for (;;)
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.matrixServed)))
            );
            if (matcher.IsInvalid) break;
            matcher.Set(OpCodes.Ldloc_S, local1).Advance(2);
        }

        matcher.Start().Advance(startPos);
        for (;;)
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.matrixIncServed)))
            );
            if (matcher.IsInvalid) break;
            matcher.Set(OpCodes.Ldloc_S, local1).Advance(2);
        }

        return matcher.InstructionEnumeration();
    }
}

public class LabOptPatchFunctions
{
    private static readonly FieldInfo RootLabIdField = AccessTools.Field(typeof(LabComponent), "rootLabId");

    public static void SetRootLabIdForStacking(FactorySystem factorySystem, int labId, int nextEntityId)
    {
        var rootId = (int)RootLabIdField.GetValue(factorySystem.labPool[labId]);
        var targetLabId = factorySystem.factory.entityPool[nextEntityId].labId;
        if (rootId <= 0) rootId = labId;
        RootLabIdField.SetValueDirect(__makeref(factorySystem.labPool[targetLabId]), rootId);
        LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {targetLabId} to {rootId}");
    }

    public static void SetRootLabIdOnLoading(FactorySystem factorySystem)
    {
        var labCursor = factorySystem.labCursor;
        var labPool = factorySystem.labPool;
        var parentDict = new Dictionary<int, int>();
        for (var id = 1; id < labCursor; id++)
        {
            if (labPool[id].id != id) continue;
            ref var lab = ref labPool[id];
            if (lab.nextLabId != 0) parentDict[lab.nextLabId] = id;
        }

        foreach (var pair in parentDict)
        {
            var rootId = pair.Value;
            while (parentDict.TryGetValue(rootId, out var parentId)) rootId = parentId;
            RootLabIdField.SetValueDirect(__makeref(labPool[pair.Key]), rootId);
            LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {pair.Key} to {rootId}");
        }
    }
}