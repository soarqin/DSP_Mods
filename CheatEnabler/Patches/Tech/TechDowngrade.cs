using System.Collections.Generic;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Plans the complete dependency cascade before changing tech states or live game objects.
/// A dependent loses all applied levels when its prerequisite can no longer be researched.
/// </summary>
internal static class TechDowngrade
{
    internal const int DeliveryPackageRecipeId = 122;

    // Values are the last applied levels after this click, rather than the next research levels.
    private static readonly Dictionary<int, int> Targets = new();
    private static readonly HashSet<int> DirtyFuncs = new();
    private static readonly HashSet<int> Recipes = new();
    private static bool _inserterDirty;

    public static void Run(GameHistoryData history, TechProto techProto, int steps)
    {
        if (!TryGetLastAppliedLevel(history, techProto, out int lastApplied) || steps <= 0) return;

        int target = steps == TechKeyCombo.LevelToMax
            ? techProto.Level - 1
            : System.Math.Max(techProto.Level - 1, lastApplied - steps);
        Targets.Clear();
        DirtyFuncs.Clear();
        Recipes.Clear();
        _inserterDirty = false;
        Plan(history, techProto, target);

        if (!TechFuncRevert.CanRevert(history, Targets)) return;

        int localStorageBefore = history.localStationExtraStorage;
        int remoteStorageBefore = history.remoteStationExtraStorage;
        foreach (var pair in Targets)
        {
            Apply(history, LDB.techs.Select(pair.Key), pair.Value);
        }

        // All states must be final before checking shared recipes or recomputing function values.
        foreach (int recipeId in Recipes)
        {
            if (IsRecipeGranted(history, recipeId)) continue;
            history.recipeUnlocked.Remove(recipeId);
            if (recipeId == DeliveryPackageRecipeId)
            {
                GameMain.mainPlayer.deliveryPackage.unlocked = false;
                GameMain.mainPlayer.deliveryPackage.NotifySizeChange();
            }
        }

        foreach (int func in DirtyFuncs)
        {
            // Both functions share one package layout and must only resize it once.
            if (func == 38 && DirtyFuncs.Contains(5)) continue;
            TechFuncRevert.Recompute(history, func);
        }

        TechFuncRevert.ReconcileStationStorage(history, localStorageBefore, remoteStorageBefore);
        if (_inserterDirty) history.gameData.OnInserterTechChange();

        TechUnlock.FinishClick(history);
        RefreshOpenWindow();
    }

    internal static void MarkDirty(int func) => DirtyFuncs.Add(func);
    internal static void MarkInserterDirty() => _inserterDirty = true;

    /// <summary>Completed techs store the last applied level; other techs store the next level.</summary>
    internal static int GetLastAppliedLevel(TechState state) => state.unlocked ? state.curLevel : state.curLevel - 1;

    internal static bool TryGetLastAppliedLevel(GameHistoryData history, TechProto proto, out int lastApplied)
    {
        lastApplied = 0;
        if (history?.techStates == null || proto == null ||
            !history.techStates.TryGetValue(proto.ID, out TechState state)) return false;

        lastApplied = GetLastAppliedLevel(state);
        return lastApplied >= proto.Level;
    }

    internal static bool HasAnyAppliedLevel(GameHistoryData history, TechProto proto) =>
        TryGetLastAppliedLevel(history, proto, out _);

    private static void Plan(GameHistoryData history, TechProto proto, int target)
    {
        if (Targets.ContainsKey(proto.ID) || !history.techStates.TryGetValue(proto.ID, out TechState state)) return;
        Targets.Add(proto.ID, target);

        foreach (TechProto post in LDB.techs.dataArray)
        {
            if (post == null || post.ID == proto.ID ||
                !(Contains(post.PreTechs, proto.ID) || Contains(post.PreTechsImplicit, proto.ID))) continue;

            // Mirrors GameHistoryData.TechUnlocked(id, max). A partially researched dependent also
            // needs to be reset; its own unlocked flag does not describe its prerequisite's validity.
            if (state.unlocked && target >= state.curLevel && (!post.PreTechsMax || target == state.maxLevel)) continue;
            if (!history.techStates.TryGetValue(post.ID, out TechState postState)) continue;
            if (GetLastAppliedLevel(postState) < post.Level && postState.hashUploaded == 0) continue;
            Plan(history, post, post.Level - 1);
        }
    }

    private static void Apply(GameHistoryData history, TechProto proto, int target)
    {
        TechState state = history.techStates[proto.ID];
        for (int level = GetLastAppliedLevel(state); level > target; level--)
        {
            for (int j = 0; j < proto.UnlockFunctions.Length; j++)
            {
                TechFuncRevert.Revert(history, proto.UnlockFunctions[j], proto.UnlockValues[j], level);
            }
        }

        state.unlocked = target >= state.maxLevel;
        state.curLevel = state.unlocked ? state.maxLevel : target + 1;
        state.hashNeeded = proto.GetHashNeeded(state.curLevel);
        state.hashUploaded = state.unlocked ? state.hashNeeded : 0L;
        state.unlockTick = 0;
        history.techStates[proto.ID] = state;
        if (target < proto.Level)
        {
            foreach (int recipe in proto.UnlockRecipes) Recipes.Add(recipe);
        }
    }

    internal static bool Contains(int[] array, int value)
    {
        if (array == null) return false;
        foreach (int item in array)
        {
            if (item == value) return true;
        }

        return false;
    }

    internal static bool IsRecipeGranted(GameHistoryData history, int recipeId,
        IReadOnlyDictionary<int, int> targets = null)
    {
        if (Contains(Configs.freeMode.recipes, recipeId)) return true;
        foreach (TechProto proto in LDB.techs.dataArray)
        {
            if (proto == null || !Contains(proto.UnlockRecipes, recipeId)) continue;
            int last = targets != null && targets.TryGetValue(proto.ID, out int target)
                ? target
                : history.techStates.TryGetValue(proto.ID, out TechState state) ? GetLastAppliedLevel(state) : proto.Level - 1;
            if (last >= proto.Level) return true;
        }

        return false;
    }

    internal static void RefreshOpenWindow()
    {
        UITechTree window = UIRoot.instance?.uiGame?.techTree;
        if (window == null || !window.active) return;

        // Restore native layering for all nodes once, without emitting research-complete events
        // or promoting downgraded nodes through the upgrade-only SortUnlockedNode path.
        window.SortTechNodeOrderInGroup(window.graphGroup0);
        window.SortTechNodeOrderInGroup(window.graphGroup1);
        foreach (UITechNode node in window.nodes.Values)
        {
            node.UpdateInfoComplete();
            node.UpdateInfoDynamic(true);
            node.UpdateLayoutDynamic(true, true);
            node.UpdateConnDynamic();
        }

        window.selected?.RefreshPreTechCheckboxes();
        window.ResetTipDelay();
        window.RefreshDataValueText();
    }
}
