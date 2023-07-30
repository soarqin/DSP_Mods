using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;

namespace LabOpt;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LabOptPatch : BaseUnityPlugin
{
    private const int RequireCountForAssemble = 15;
    private const int RequireCountForResearch = 54000;

    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(LabOptPatch));
    }
    
    // Patch LabComponent.Export() to save zero value if rootLabId > 0.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.Export))]
    private static IEnumerable<CodeInstruction> LabComponent_Export_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(LabComponent), nameof(LabComponent.matrixMode)))
        ).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble, label1),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.LabExportZero))),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Instruction.labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    // Patch UpdateNeedsAssemble() and UpdateNeedsResearch() to remove the execution if rootLabId > 0.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble))]
    private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsAssemble_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.Start().InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble, label1),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Instruction.labels.Add(label1);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldelem_I4),
            new CodeMatch(OpCodes.Ldc_I4_4)
        );
        matcher.Repeat(codeMatcher =>
            codeMatcher.Advance(1).SetAndAdvance(OpCodes.Ldc_I4_S, RequireCountForAssemble)
        );
        return matcher.InstructionEnumeration();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch))]
    private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsResearch_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.Start().InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble, label1),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Instruction.labels.Add(label1);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4, 0x8CA0)
        );
        matcher.Repeat(codeMatcher =>
            codeMatcher.SetAndAdvance(OpCodes.Ldc_I4, RequireCountForResearch)
        );
        return matcher.InstructionEnumeration();
    }

    // Remove use of LabComponent.UpdateOutputToNext() for single-thread mode
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.GameTick))]
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
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.Import))]
    private static IEnumerable<CodeInstruction> FactorySystem_Import_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.End().MatchForward(false,
            new CodeMatch(OpCodes.Ret)
        ).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.SetRootLabIdOnLoading)))
        );
        return matcher.InstructionEnumeration();
    }

    // Redirect call of LabComponent.InternalUpdateAssemble() to InternalUpdateAssembleNew()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabProduceMode), typeof(long), typeof(bool))]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabProduceMode), typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
    private static IEnumerable<CodeInstruction> FactorySystem_GameTickLabProduceMode_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble)))
        ).SetInstruction(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.InternalUpdateAssembleNew)))
        );
        return matcher.InstructionEnumeration();
    }

    // Redirect call of LabComponent.SetFunction() to SetFunctionNew()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildingParameters), nameof(BuildingParameters.ApplyPrebuildParametersToEntity))]
    private static IEnumerable<CodeInstruction> BuildingParameters_ApplyPrebuildParametersToEntity_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.SetFunction)))
        ).Repeat(codeMatcher =>
        {
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool)))
            ).SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.SetFunctionNew)))
            );
        });
        return matcher.InstructionEnumeration();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildingParameters), nameof(BuildingParameters.PasteToFactoryObject))]
    private static IEnumerable<CodeInstruction> BuildingParameters_PasteToFactoryObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.SetFunction)))
        ).Repeat(codeMatcher =>
        {
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool)))
            ).SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.SetFunctionNew)))
            );
        });
        return matcher.InstructionEnumeration();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.FindLabFunctionsForBuild))]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabResearchMode))]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.SyncLabFunctions))]
    // no need to patch this function, it just set everything to empty
    //  [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.TakeBackItems_Lab))]
    private static IEnumerable<CodeInstruction> FactorySystem_ReplaceLabSetFunction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.SetFunction)))
        ).Repeat(codeMatcher =>
        {
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool)))
            ).SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.SetFunctionNew)))
            );
        });
        return matcher.InstructionEnumeration();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow.OnItemButtonClick))]
    [HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow.OnProductButtonClick))]
    private static IEnumerable<CodeInstruction> UILabWindow_PasteToFactoryObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.SetFunction)))
        ).Repeat(codeMatcher =>
        {
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool)))
            ).SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.SetFunctionNew)))
            );
        });
        return matcher.InstructionEnumeration();
    }

    // Do not take items back on dismantling labs
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.TakeBackItems_Lab))]
    private static IEnumerable<CodeInstruction> FactorySystem_TakeBackItems_Lab_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.SetFunction)))
        ).Advance(-10);
        var label1 = matcher.Labels[0];
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.recipeId)))
        ).Insert(
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), "rootLabId")),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Bgt, label1)
        );
        return matcher.InstructionEnumeration();
    }
    
    // Change locks on PlanetFactory.InsertInto(), by calling LabOptPatchFunctions.InsertIntoLab()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
    private static IEnumerable<CodeInstruction> PlanetFactory_InsertInto_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.labId)))
        ).Advance(1).MatchForward(false,
            new CodeMatch(OpCodes.Ret)
        ).Advance(1);
        var labels = matcher.Instruction.labels;
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldloc_S, 5),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Ldarg_S, 4),
            new CodeInstruction(OpCodes.Ldarg_S, 5),
            new CodeInstruction(OpCodes.Ldarg_S, 6),
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.InsertIntoLab))),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Instruction.labels.Clear();
        return matcher.InstructionEnumeration();
    }

    // Change locks on PlanetFactory.PickFrom(), by calling LabOptPatchFunctions.PickFromLab()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.PickFrom))]
    private static IEnumerable<CodeInstruction> PlanetFactory_PickFrom_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.labId)))
        ).Advance(1).MatchForward(false,
            new CodeMatch(OpCodes.Ble)
        ).Advance(1);
        var labels = matcher.Instruction.labels;
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldloc_S, 7),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Ldarg_S, 4),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.PickFromLab))),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Instruction.labels.Clear();
        return matcher.InstructionEnumeration();
    }

}
