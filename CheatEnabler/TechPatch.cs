using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;

namespace CheatEnabler;

public static class TechPatch
{
    public static ConfigEntry<bool> Enabled;
    private static Harmony _patch;
    
    
    public static void Init()
    {
        Enabled.SettingChanged += (_, _) => ValueChanged();
        ValueChanged();
    }

    public static void Uninit()
    {
        _patch?.UnpatchSelf();
        _patch = null;
    }

    private static void ValueChanged()
    {
        if (Enabled.Value)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(TechPatch));
        }
        else
        {
            _patch?.UnpatchSelf();
            _patch = null;
        }
    }

    private static void UnlockTechRecursive([NotNull] TechProto techProto, int maxLevel = 10000)
    {
        var history = GameMain.history;
        var techStates = history.techStates;
        var techID = techProto.ID;
        if (techStates == null || !techStates.ContainsKey(techID))
        {
            return;
        }

        var value = techStates[techID];
        if (value.unlocked)
        {
            return;
        }

        var maxLvl = Math.Min(maxLevel < 0 ? value.curLevel - maxLevel - 1 : maxLevel, value.maxLevel);

        foreach (var preid in techProto.PreTechs)
        {
            var preProto = LDB.techs.Select(preid);
            if (preProto != null)
                UnlockTechRecursive(preProto, maxLevel);
        }

        var techQueue = history.techQueue;
        if (techQueue != null)
        {
            for (var i = 0; i < techQueue.Length; i++)
            {
                if (techQueue[i] == techID)
                {
                    techQueue[i] = 0;
                }
            }
        }

        if (value.curLevel < techProto.Level) value.curLevel = techProto.Level;
        while (value.curLevel <= maxLvl)
        {
            if (value.curLevel == 0)
            {
                foreach (var recipe in techProto.UnlockRecipes)
                {
                    history.UnlockRecipe(recipe);
                }
            }
            for (var j = 0; j < techProto.UnlockFunctions.Length; j++)
            {
                history.UnlockTechFunction(techProto.UnlockFunctions[j], techProto.UnlockValues[j], value.curLevel);
            }
            for (var k = 0; k < techProto.AddItems.Length; k++)
            {
                history.GainTechAwards(techProto.AddItems[k], techProto.AddItemCounts[k]);
            }
            value.curLevel++;
        }

        value.unlocked = maxLvl >= value.maxLevel;
        value.curLevel = value.unlocked ? maxLvl : maxLvl + 1;
        value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
        value.hashUploaded = value.unlocked ? value.hashNeeded : 0;
        techStates[techID] = value;
        history.RegFeatureKey(1000100);
        history.NotifyTechUnlock(techID, maxLvl, true);
    }

    private static void OnClickTech(UITechNode node)
    {
        if (VFInput.shift)
        {
            if (VFInput.alt) return;
            if (VFInput.control)
                UnlockTechRecursive(node.techProto, -100);
            else
                UnlockTechRecursive(node.techProto, -1);
            return;
        }
        if (VFInput.control)
        {
            if (!VFInput.alt)
                UnlockTechRecursive(node.techProto, -10);
        }
        else if (VFInput.alt)
        {
            UnlockTechRecursive(node.techProto, 10000);
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.OnPointerDown))]
    private static IEnumerable<CodeInstruction> UITechNode_OnPointerDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UITechNode), nameof(UITechNode.tree))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(UITechTree), "get_selected"))
        );
        var labels = matcher.Labels;
        matcher.Labels = null;
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TechPatch), nameof(TechPatch.OnClickTech)))
        );
        return matcher.InstructionEnumeration();
    }

}