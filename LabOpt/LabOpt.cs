using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine.Yoga;

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
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.labId))))
            .MatchForward(false, new CodeMatch(OpCodes.Ret))
            .Advance(1);
        var labels = matcher.Instruction.labels;
        matcher.InsertAndAdvance(
            // rootLabId = this.factorySystem.labPool[labId].rootLabId;
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
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
    
    // Add a parameter on calling LabComponent.InternalUpdateAssemble()
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
        return matcher.InstructionEnumeration();
    }

    // Add a parameter on calling LabComponent.InternalUpdateResearch()
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

            ref var rootLab = ref labPool[rootId];
            ref var thisLab = ref labPool[pair.Key];
            int len;
            if (rootLab.researchMode)
            {
                len = rootLab.matrixServed.Length;
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.matrixServed[i] == 0) continue;
                    rootLab.matrixServed[i] += thisLab.matrixServed[i];
                    thisLab.matrixServed[i] = 0;
                }
                len = rootLab.matrixIncServed.Length;
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.matrixIncServed[i] == 0) continue;
                    rootLab.matrixIncServed[i] += thisLab.matrixIncServed[i];
                    thisLab.matrixIncServed[i] = 0;
                }
            }
            else
            {
                len = rootLab.produced.Length;
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.produced[i] == 0) continue;
                    rootLab.produced[i] += thisLab.produced[i];
                    thisLab.produced[i] = 0;
                }
                len = rootLab.served.Length;
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.served[i] == 0) continue;
                    rootLab.served[i] += thisLab.served[i];
                    thisLab.served[i] = 0;
                }
                len = rootLab.incServed.Length;
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.incServed[i] == 0) continue;
                    rootLab.incServed[i] += thisLab.incServed[i];
                    thisLab.incServed[i] = 0;
                }
            }

            LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {pair.Key} to {rootId}");
        }
    }

    public static uint InternalUpdateAssembleNew(ref LabComponent lab, float power, int[] productRegister, int[] consumeRegister, LabComponent[] labPool)
    {
        if (power < 0.1f)
        {
            return 0U;
        }

        var extraPassed = lab.extraTime >= lab.extraTimeSpend;
        var timePassed = lab.time >= lab.timeSpend;
        if (extraPassed || timePassed || !lab.replicating)
        {
            var rootLabId = (int)RootLabIdField.GetValue(lab);
            ref var rootLab = ref rootLabId > 0 ? ref labPool[rootLabId] : ref lab;
            if (extraPassed)
            {
                int num = lab.products.Length;
                if (num == 1)
                {
                    rootLab.produced[0] += lab.productCounts[0];
                    int[] array = productRegister;
                    lock (array)
                    {
                        productRegister[lab.products[0]] += lab.productCounts[0];
                    }
                }
                else
                {
                    for (int i = 0; i < num; i++)
                    {
                        rootLab.produced[i] += lab.productCounts[i];
                        lock (productRegister)
                        {
                            productRegister[lab.products[i]] += lab.productCounts[i];
                        }
                    }
                }

                lab.extraTime -= lab.extraTimeSpend;
            }

            if (timePassed)
            {
                lab.replicating = false;
                int num2 = lab.products.Length;
                if (num2 == 1)
                {
                    if (rootLab.produced[0] + lab.productCounts[0] > 10)
                    {
                        return 0U;
                    }

                    rootLab.produced[0] += lab.productCounts[0];
                    int[] array = productRegister;
                    lock (array)
                    {
                        productRegister[lab.products[0]] += lab.productCounts[0];
                    }
                }
                else
                {
                    for (int j = 0; j < num2; j++)
                    {
                        if (rootLab.produced[j] + lab.productCounts[j] > 50)
                        {
                            return 0U;
                        }
                    }

                    for (int k = 0; k < num2; k++)
                    {
                        rootLab.produced[k] += lab.productCounts[k];
                        lock (productRegister)
                        {
                            productRegister[lab.products[k]] += lab.productCounts[k];
                        }
                    }
                }

                lab.extraSpeed = 0;
                lab.speedOverride = lab.speed;
                lab.extraPowerRatio = 0;
                lab.time -= lab.timeSpend;
            }

            if (!lab.replicating)
            {
                int num3 = lab.requireCounts.Length;
                for (int l = 0; l < num3; l++)
                {
                    if (rootLab.incServed[l] <= 0)
                    {
                        rootLab.incServed[l] = 0;
                    }

                    if (rootLab.served[l] < lab.requireCounts[l] || rootLab.served[l] == 0)
                    {
                        lab.time = 0;
                        return 0U;
                    }
                }

                int num4 = ((num3 > 0) ? 10 : 0);
                for (int m = 0; m < num3; m++)
                {
                    int num5 = lab.split_inc_level(ref rootLab.served[m], ref rootLab.incServed[m], lab.requireCounts[m]);
                    num4 = ((num4 < num5) ? num4 : num5);
                    if (rootLab.served[m] == 0)
                    {
                        rootLab.incServed[m] = 0;
                    }

                    int[] array = consumeRegister;
                    lock (array)
                    {
                        consumeRegister[lab.requires[m]] += lab.requireCounts[m];
                    }
                }

                if (num4 < 0)
                {
                    num4 = 0;
                }

                if (lab.productive && !lab.forceAccMode)
                {
                    lab.extraSpeed = (int)((double)lab.speed * Cargo.incTableMilli[num4] * 10.0 + 0.1);
                    lab.speedOverride = lab.speed;
                    lab.extraPowerRatio = Cargo.powerTable[num4];
                }
                else
                {
                    lab.extraSpeed = 0;
                    lab.speedOverride = (int)((double)lab.speed * (1.0 + Cargo.accTableMilli[num4]) + 0.1);
                    lab.extraPowerRatio = Cargo.powerTable[num4];
                }

                lab.replicating = true;
            }
        }

        if (lab.replicating && lab.time < lab.timeSpend && lab.extraTime < lab.extraTimeSpend)
        {
            lab.time += (int)(power * (float)lab.speedOverride);
            lab.extraTime += (int)(power * (float)lab.extraSpeed);
        }

        if (!lab.replicating)
        {
            return 0U;
        }

        return (uint)(lab.products[0] - LabComponent.matrixIds[0] + 1);
    }

    public static uint InternalUpdateResearchNew(ref LabComponent lab, float power, float speed, int[] consumeRegister, ref TechState ts, ref int techHashedThisFrame, ref long uMatrixPoint, ref long hashRegister, LabComponent[] labPool)
    {
        if (power < 0.1f)
        {
            return 0U;
        }
        var rootLabId = (int)RootLabIdField.GetValue(lab);
        ref var rootLab = ref rootLabId > 0 ? ref labPool[rootLabId] : ref lab;

        int num = (int)(speed + 2f);
        for (var i = 0; i < 6; i++)
        {
            if (lab.matrixPoints[i] <= 0) continue;
            int num2 = rootLab.matrixServed[i] / lab.matrixPoints[i];
            if (num2 < num)
            {
                num = num2;
                if (num == 0)
                {
                    lab.replicating = false;
                    return 0U;
                }
            }
        }

        lab.replicating = true;
        speed = ((speed < (float)num) ? speed : ((float)num));
        int num3 = (int)(power * 10000f * speed + 0.5f);
        lab.hashBytes += num3;
        long num4 = (long)(lab.hashBytes / 10000);
        lab.hashBytes -= (int)num4 * 10000;
        long num5 = ts.hashNeeded - ts.hashUploaded;
        num4 = ((num4 < num5) ? num4 : num5);
        num4 = ((num4 < (long)num) ? num4 : ((long)num));
        int num6 = (int)num4;
        if (num6 > 0)
        {
            int num7 = rootLab.matrixServed.Length;
            int num8 = ((num7 == 0) ? 0 : 10);
            for (int i = 0; i < num7; i++)
            {
                if (lab.matrixPoints[i] > 0)
                {
                    int num9 = rootLab.matrixServed[i] / 3600;
                    int num10 = lab.split_inc_level(ref rootLab.matrixServed[i], ref rootLab.matrixIncServed[i], lab.matrixPoints[i] * num6);
                    num8 = ((num8 < num10) ? num8 : num10);
                    int num11 = rootLab.matrixServed[i] / 3600;
                    if (rootLab.matrixServed[i] <= 0 || rootLab.matrixIncServed[i] < 0)
                    {
                        rootLab.matrixIncServed[i] = 0;
                    }

                    consumeRegister[LabComponent.matrixIds[i]] += num9 - num11;
                }
            }

            if (num8 < 0)
            {
                num8 = 0;
            }

            lab.extraSpeed = (int)(10000.0 * Cargo.incTableMilli[num8] * 10.0 + 0.1);
            lab.extraPowerRatio = Cargo.powerTable[num8];
            lab.extraHashBytes += (int)(power * (float)lab.extraSpeed * speed + 0.5f);
            long num12 = (long)(lab.extraHashBytes / 100000);
            lab.extraHashBytes -= (int)num12 * 100000;
            num12 = ((num12 < 0L) ? 0L : num12);
            int num13 = (int)num12;
            ts.hashUploaded += num4 + num12;
            hashRegister += num4 + num12;
            uMatrixPoint += (long)ts.uPointPerHash * num4;
            techHashedThisFrame += num6 + num13;
            if (ts.hashUploaded >= ts.hashNeeded)
            {
                TechProto techProto = LDB.techs.Select(lab.techId);
                if (ts.curLevel >= ts.maxLevel)
                {
                    ts.curLevel = ts.maxLevel;
                    ts.hashUploaded = ts.hashNeeded;
                    ts.unlocked = true;
                }
                else
                {
                    ts.curLevel++;
                    ts.hashUploaded = 0L;
                    ts.hashNeeded = techProto.GetHashNeeded(ts.curLevel);
                }
            }
        }
        else
        {
            lab.extraSpeed = 0;
            lab.extraPowerRatio = 0;
        }

        return 1U;
    }
}
