using System.Collections.Generic;
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
        var local1 = generator.DeclareLocal(typeof(LabComponent).MakeArrayType());
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.labId))))
            .MatchForward(false, new CodeMatch(OpCodes.Ret))
            .Advance(1);
        var labels = matcher.Instruction.labels;
        matcher.InsertAndAdvance(
            // labPool = this.factorySystem.labPool;
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Stloc_S, local1),
            // rootLabId = labPool[labId].rootLabId;
            new CodeInstruction(OpCodes.Ldloc_S, local1),
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
            // entityId = labPool[labId].entityId;
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldloc_S, 5),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.entityId))),
            new CodeInstruction(OpCodes.Starg_S, 1),
            new CodeInstruction(OpCodes.Ldarg_S, 1),
            // array = this.entityNeeds[entityId];
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.entityNeeds))),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldelem_Ref),
            new CodeInstruction(OpCodes.Stloc_1)
            // lable1:
        );
        matcher.Instruction.labels = new List<Label> { label1 };

        for (;;)
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool)))
            );
            if (matcher.IsInvalid) break;
            var labels2 = matcher.Instruction.labels;
            var instr = new CodeInstruction(OpCodes.Ldloc_S, local1).WithLabels(labels2);
            matcher.RemoveInstructions(3).InsertAndAdvance(instr);
        }

        // UpdateNeedsXXXXSingle() after item count changed
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_S),
            new CodeMatch(OpCodes.Ldelema, typeof(int)),
            new CodeMatch(OpCodes.Dup),
            new CodeMatch(OpCodes.Ldind_I4),
            new CodeMatch(OpCodes.Ldarg_S, (byte)5),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Stind_I4)
        );
        var v19 = matcher.Operand;
        matcher.Advance(7).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldloc_S, 5),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldloc_S, 19),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsAssembleSingle)))
        );
        
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_S, v19),
            new CodeMatch(OpCodes.Ldelema, typeof(int)),
            new CodeMatch(OpCodes.Dup),
            new CodeMatch(OpCodes.Ldind_I4),
            new CodeMatch(OpCodes.Ldc_I4, 0xE10),
            new CodeMatch(OpCodes.Ldarg_S, (byte)5),
            new CodeMatch(OpCodes.Mul),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Stind_I4)
        ).Advance(9).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldloc_S, 5),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldloc_S, 19),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsResearchSingle)))
        );

        return matcher.InstructionEnumeration();
    }

    // Fill into root lab
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
    private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastFillIn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool)))
            )
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

        // Add UpdateNeedsXXXXSingle() after item count changed
        // -- Find V_76
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Sub),
            new CodeMatch(OpCodes.Stloc_S),
            new CodeMatch(OpCodes.Ldc_I4_0),
            new CodeMatch(OpCodes.Stloc_S))
        .Advance(3);
        var v76 = matcher.Operand;
        // -- Patch
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_S, v76),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Stind_I4)
        ).Advance(3).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldloc_S, 69),
            new CodeInstruction(OpCodes.Ldloc_S, 68),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldloc_S, 73),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsAssembleSingle)))
        );
        // -- Find V_83
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4, 0xE10),
            new CodeMatch(OpCodes.Div),
            new CodeMatch(OpCodes.Sub),
            new CodeMatch(OpCodes.Stloc_S),
            new CodeMatch(OpCodes.Ldc_I4_0),
            new CodeMatch(OpCodes.Stloc_S))
        .Advance(5);
        var v83 = matcher.Operand;
        // -- Patch
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_S, v83),
            new CodeMatch(OpCodes.Ldc_I4, 0xE10),
            new CodeMatch(OpCodes.Mul),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Stind_I4)
        ).Advance(5).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldloc_S, 69),
            new CodeInstruction(OpCodes.Ldloc_S, 68),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldloc_S, 73),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsResearchSingle)))
        );
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
        matcher
            .MatchForward(false, new CodeMatch(OpCodes.Ret))
            .Advance(1)
            .MatchForward(false, new CodeMatch(OpCodes.Ret))
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
            matcher.RemoveInstructions(2).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, local1));
        }

        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.AddHandItemCount_Unsafe)))
        ).MatchBack(false,
            new CodeMatch(OpCodes.Ldarg_0)
        ).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsResearchSingle)))
        ).Advance(10).MatchForward(false,
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.AddHandItemCount_Unsafe)))
        ).MatchBack(false,
            new CodeMatch(OpCodes.Ldarg_0)
        ).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsAssembleSingle)))
        );
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Stelem_I4),
            new CodeMatch(OpCodes.Ret)
        ).Advance(1).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsResearchSingle)))
        ).Advance(1).MatchForward(false,
            new CodeMatch(OpCodes.Stelem_I4),
            new CodeMatch(OpCodes.Ret)
        ).Advance(1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILabWindow), nameof(UILabWindow.factorySystem))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Ldloc_S, local1),
            new CodeInstruction(OpCodes.Ldelema, typeof(LabComponent)),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof (LabOptPatchFunctions), nameof(LabOptPatchFunctions.UpdateNeedsAssembleSingle)))
        );

        return matcher.InstructionEnumeration();
    }
    
    // Add a parameter on calling LabComponent.InternalUpdateAssemble()
    // Remove call to LabComponent.InternalUpdateAssemble()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabProduceMode), typeof(long), typeof(bool))]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabProduceMode), typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
    private static IEnumerable<CodeInstruction> FactorySystem_GameTickLabProduceMode_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble)))
        ).RemoveInstructions(1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.InternalUpdateAssembleNew)))
        );
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble)))
        ).Advance(-4).RemoveInstructions(5);
        return matcher.InstructionEnumeration();
    }

    // Add a parameter on calling LabComponent.InternalUpdateResearch()
    // Remove call to LabComponent.InternalUpdateResearch()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabResearchMode), typeof(long), typeof(bool))]
    private static IEnumerable<CodeInstruction> FactorySystem_GameTickLabResearchMode_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch)))
        ).RemoveInstructions(1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.labPool))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabOptPatchFunctions), nameof(LabOptPatchFunctions.InternalUpdateResearchNew)))
        );
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch)))
        ).Advance(-4).RemoveInstructions(5);
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
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.produced)))
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
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.SetFunction))]
    private static IEnumerable<CodeInstruction> LabComponent_SetFunction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ret)
        ).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch)))
        ).Advance(1).MatchForward(false,
            new CodeMatch(OpCodes.Newarr, typeof(int)),
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(LabComponent), nameof(LabComponent.produced)))
        ).Advance(2).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble)))
        );
        return matcher.InstructionEnumeration();
    }
    
}
