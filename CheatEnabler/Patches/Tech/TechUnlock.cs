using System;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Upgrade side - unlock techs by clicking them with a key-modifier held while downgrade mode is off.
///
/// <see cref="UnlockTechRecursive"/> preserves the previous modifier-click upgrade behavior. The
/// prerequisites are unlocked
/// recursively (<c>PreTechs</c> / <c>PreTechsImplicit</c>, driven by <c>PreTechsMax</c>), recipes are
/// unlocked on level 0, every level calls <c>UnlockTechFunction</c>, tech awards are granted, the hash
/// counters are maintained and <c>NotifyTechUnlock</c> plus <c>RegFeatureKey(1000100)</c> are fired.
/// </summary>
internal static class TechUnlock
{
    public static void Run(GameHistoryData history, TechProto techProto, int steps)
    {
        // Mirrors the built-in maxLevel argument: "to MAX" passes the default, the other combinations
        // pass -(steps) so the loop advances exactly `steps` levels.
        int maxLevel = steps == TechKeyCombo.LevelToMax ? 10000 : -steps;

        UnlockTechRecursive(history, techProto, maxLevel);

        FinishClick(history);
    }

    /// <summary>Same post-click queue fix-up the built-in feature performs.</summary>
    internal static void FinishClick(GameHistoryData history)
    {
        history.VerifyTechQueue();
        if (history.techQueue != null && history.techQueue.Length > 0 && history.currentTech != history.techQueue[0])
        {
            history.currentTech = history.techQueue[0];
        }
    }

    private static void UnlockTechRecursive(GameHistoryData history, TechProto techProto, int maxLevel = 10000)
    {
        var techStates = history.techStates;
        int techID = techProto.ID;
        if (techStates == null || !techStates.TryGetValue(techID, out TechState value))
        {
            return;
        }

        if (value.unlocked)
        {
            return;
        }

        int maxLvl = Math.Min(maxLevel < 0 ? value.curLevel - maxLevel - 1 : maxLevel, value.maxLevel);

        foreach (int preid in techProto.PreTechs)
        {
            TechProto preProto = LDB.techs.Select(preid);
            if (preProto != null)
            {
                UnlockTechRecursive(history, preProto, techProto.PreTechsMax ? 10000 : -1);
            }
        }

        foreach (int preid in techProto.PreTechsImplicit)
        {
            TechProto preProto = LDB.techs.Select(preid);
            if (preProto != null)
            {
                UnlockTechRecursive(history, preProto, techProto.PreTechsMax ? 10000 : -1);
            }
        }

        if (value.curLevel < techProto.Level)
        {
            value.curLevel = techProto.Level;
        }

        while (value.curLevel <= maxLvl)
        {
            if (value.curLevel == 0)
            {
                foreach (int recipe in techProto.UnlockRecipes)
                {
                    history.UnlockRecipe(recipe);
                }
            }

            for (int j = 0; j < techProto.UnlockFunctions.Length; j++)
            {
                history.UnlockTechFunction(techProto.UnlockFunctions[j], techProto.UnlockValues[j], value.curLevel);
            }

            for (int k = 0; k < techProto.AddItems.Length; k++)
            {
                history.GainTechAwards(techProto.AddItems[k], techProto.AddItemCounts[k]);
            }

            value.curLevel++;
        }

        // No trace inside the loop above on purpose: a single Alt-click on an "infinite" tech applies
        // thousands of levels, and one log write per level makes the game freeze.
        value.unlocked = maxLvl >= value.maxLevel;
        value.curLevel = value.unlocked ? maxLvl : maxLvl + 1;
        value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
        value.hashUploaded = value.unlocked ? value.hashNeeded : 0L;
        techStates[techID] = value;
        history.RegFeatureKey(1000100);
        history.NotifyTechUnlock(techID, maxLvl, true);
    }
}
