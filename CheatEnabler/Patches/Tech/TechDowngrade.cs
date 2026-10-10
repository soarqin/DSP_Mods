using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Downgrade side - decrease a tech by clicking it with a key-modifier held while downgrade mode is on.
///
/// The recursion is the mirror image of <see cref="TechUnlock"/>: the unlock walks into the *pre*-techs
/// so the tech tree is never left with a gap, the downgrade walks into the *post*-techs so it is never
/// left with "prerequisite locked, dependent unlocked".
///
/// Downgrading is limited to levels the tech actually has applied, never goes below zero and does
/// nothing at all for a tech that is not unlocked yet.
/// </summary>
internal static class TechDowngrade
{
    private static readonly HashSet<int> Visited = new HashSet<int>();

    private static readonly HashSet<int> DirtyFuncs = new HashSet<int>();

    /// <summary>techId -> new curLevel, for every tech touched by the current click.</summary>
    private static readonly Dictionary<int, int> Touched = new Dictionary<int, int>();

    private static bool _inserterDirty;

    public static void Run(GameHistoryData history, TechProto techProto, int steps)
    {
        if (history?.techStates == null || techProto == null)
        {
            return;
        }

        if (!history.techStates.TryGetValue(techProto.ID, out TechState state))
        {
            return;
        }

        int lastApplied = GetLastAppliedLevel(state);
        if (lastApplied < techProto.Level)
        {
            // Nothing applied yet: clicking a tech that is not unlocked does nothing.
            return;
        }

        int target = steps == TechKeyCombo.LevelToMax
            ? techProto.Level - 1
            : lastApplied - steps;
        if (target < techProto.Level)
        {
            target = techProto.Level - 1;
        }

        if (target >= lastApplied)
        {
            return;
        }

        Visited.Clear();
        DirtyFuncs.Clear();
        Touched.Clear();
        _inserterDirty = false;

        DowngradeTo(history, techProto, lastApplied, target);

        foreach (int func in DirtyFuncs)
        {
            try
            {
                TechFuncRevert.Recompute(history, func);
            }
            catch (System.Exception e)
            {
                TechLog.Warn($"recomputing tech function {func} failed: {e}");
            }
        }

        if (_inserterDirty && history.gameData != null)
        {
            history.gameData.OnInserterTechChange();
        }

        TechUnlock.FinishClick(history);

        RefreshOpenWindow();
    }

    internal static void MarkDirty(int func) => DirtyFuncs.Add(func);

    internal static void MarkInserterDirty() => _inserterDirty = true;

    /// <summary>
    /// <c>curLevel</c> means "levels completed plus one" while the tech is being researched, but
    /// "the max level" once it is unlocked. This helper hides that difference and returns the index of
    /// the highest level whose <c>UnlockTechFunction</c> calls have already been applied.
    /// </summary>
    internal static int GetLastAppliedLevel(TechState state) => state.unlocked ? state.curLevel : state.curLevel - 1;

    internal static bool TryGetLastAppliedLevel(GameHistoryData history, TechProto proto, out int lastApplied)
    {
        lastApplied = 0;
        if (history?.techStates == null || proto == null)
        {
            return false;
        }

        if (!history.techStates.TryGetValue(proto.ID, out TechState state))
        {
            return false;
        }

        lastApplied = GetLastAppliedLevel(state);
        return lastApplied >= proto.Level;
    }

    internal static bool HasAnyAppliedLevel(GameHistoryData history, TechProto proto) =>
        TryGetLastAppliedLevel(history, proto, out int _);

    private static void DowngradeTo(GameHistoryData history, TechProto proto, int lastApplied, int target)
    {
        if (!Visited.Add(proto.ID))
        {
            return;
        }

        if (!history.techStates.TryGetValue(proto.ID, out TechState state))
        {
            return;
        }

        if (target >= lastApplied)
        {
            return;
        }

        bool willBeUnlocked = target >= state.maxLevel;

        RevertDependentTechs(history, proto, willBeUnlocked, target, state.maxLevel);

        for (int level = lastApplied; level > target; level--)
        {
            for (int j = 0; j < proto.UnlockFunctions.Length; j++)
            {
                // One failing function must not abort the whole downgrade: that would leave the tech
                // state updated for some techs and stale for others.
                try
                {
                    TechFuncRevert.Revert(history, proto.UnlockFunctions[j], proto.UnlockValues[j], level);
                }
                catch (System.Exception e)
                {
                    TechLog.Warn($"reverting tech function {proto.UnlockFunctions[j]} of tech {proto.ID} " +
                                 $"at level {level} failed: {e}");
                }
            }
        }

        // Recipes are unlocked when the tech reaches level 0 (see TechUnlock); they are locked again
        // only when the tech itself becomes locked.
        if (target < proto.Level)
        {
            foreach (int recipe in proto.UnlockRecipes)
            {
                LockRecipeIfUnused(history, recipe, proto.ID);
            }
        }

        state = history.techStates[proto.ID];
        state.unlocked = willBeUnlocked;
        state.curLevel = willBeUnlocked ? state.maxLevel : target + 1;
        state.hashNeeded = proto.GetHashNeeded(state.curLevel);
        state.hashUploaded = willBeUnlocked ? state.hashNeeded : 0L;
        history.techStates[proto.ID] = state;
        Touched[proto.ID] = state.curLevel;
    }

    /// <summary>
    /// Pulls back every already-unlocked dependent tech whose prerequisite requirement would be
    /// violated by this downgrade. <c>PreTechsMax</c> demands the prerequisite to be fully unlocked, so
    /// the dependent is dropped to "not unlocked" (<c>maxLevel - 1</c> applied levels); a prerequisite
    /// that becomes fully locked forces the dependent to be locked as well.
    /// </summary>
    private static void RevertDependentTechs(GameHistoryData history, TechProto proto, bool willBeUnlocked,
        int target, int maxLevel)
    {
        bool preMaxSatisfied = willBeUnlocked && target >= maxLevel;
        TechProto[] all = LDB.techs.dataArray;

        for (int i = 0; i < all.Length; i++)
        {
            TechProto post = all[i];
            if (post == null || post.ID == proto.ID || !DependsOn(post, proto.ID))
            {
                continue;
            }

            if (!history.techStates.TryGetValue(post.ID, out TechState postState))
            {
                continue;
            }

            int postLast = GetLastAppliedLevel(postState);
            if (postLast < post.Level)
            {
                continue;
            }

            // Only techs that currently meet their requirement can become illegal.
            bool currentlyOk = post.PreTechsMax
                ? postState.unlocked && postState.curLevel == postState.maxLevel
                : postState.unlocked;
            bool stillOk = post.PreTechsMax ? preMaxSatisfied : willBeUnlocked;
            if (!currentlyOk || stillOk)
            {
                continue;
            }

            int postTarget = postState.maxLevel - 1;
            if (postTarget < post.Level)
            {
                postTarget = post.Level - 1;
            }

            if (postTarget >= postLast)
            {
                continue;
            }

            DowngradeTo(history, post, postLast, postTarget);
        }
    }

    private static bool DependsOn(TechProto post, int techId) =>
        Contains(post.PreTechs, techId) || Contains(post.PreTechsImplicit, techId);

    private static bool Contains(int[] array, int value)
    {
        if (array == null)
        {
            return false;
        }

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes a recipe from the unlocked set unless another tech that still has levels applied
    /// unlocks it too. <c>GameHistoryData</c> only exposes <c>UnlockRecipe</c>, so the Set is edited
    /// directly and the one special recipe id (the delivery package, recipe 122) is reverted by hand.
    /// </summary>
    private static void LockRecipeIfUnused(GameHistoryData history, int recipeId, int excludeTechId)
    {
        TechProto[] all = LDB.techs.dataArray;
        for (int i = 0; i < all.Length; i++)
        {
            TechProto proto = all[i];
            if (proto == null || proto.ID == excludeTechId || !HasAnyAppliedLevel(history, proto))
            {
                continue;
            }

            if (Contains(proto.UnlockRecipes, recipeId))
            {
                return;
            }
        }

        if (history.recipeUnlocked != null)
        {
            history.recipeUnlocked.Remove(recipeId);
        }

        const int dispenserRecipeId = 122;
        if (recipeId == dispenserRecipeId)
        {
            Player player = GameMain.mainPlayer;
            if (player?.deliveryPackage != null)
            {
                player.deliveryPackage.unlocked = false;
            }
        }
    }

    /// <summary>
    /// Replays the per-node refresh the game performs on an upgrade, without firing the "research
    /// complete" popup or the scenario hooks (a downgrade is not a research event).
    /// </summary>
    private static void RefreshTechTree()
    {
        UITechTree window = UIRoot.instance?.uiGame?.techTree;
        if (window == null || !window.active || Touched.Count == 0)
        {
            return;
        }

        MethodInfo refresh = AccessTools.Method(typeof(UITechTree), "OnTechUnlocked",
            new[] { typeof(int), typeof(int), typeof(bool) });
        if (refresh == null)
        {
            window.RefreshDataValueText();
            return;
        }

        foreach (var pair in Touched)
        {
            refresh.Invoke(window, new object[] { pair.Key, pair.Value, true });
        }
    }

    internal static void RefreshOpenWindow() => RefreshTechTree();
}
