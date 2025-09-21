namespace UXAssist.Functions;

using System;
using System.Collections.Generic;
using Common;
using UnityEngine;

public static class TechFunctions
{
    public static void Init()
    {
        I18N.Add("Do you want to use metadata to buyout the following tech?", "Do you want to use metadata to buyout the following tech?", "要使用元数据买断以下科技吗？");
        I18N.Add("The following is the required metadata for buyout:", "The following is the required metadata for buyout:", "以下是买断所需元数据：");
        I18N.Add("Batch buyout tech", "Batch buyout tech", "批量买断科技");
    }

    public static void GenerateTechListWithPrerequisites(GameHistoryData history, int techId, List<int> techIdList)
    {
        var techProto = LDB.techs.Select(techId);
        if (techProto == null || !techProto.Published) return;
        var flag = true;
        for (var i = 0; i < 2; i++)
        {
            foreach (var preTechId in (i == 1 ? techProto.PreTechsImplicit : techProto.PreTechs))
            {
                if (!history.techStates.ContainsKey(preTechId) || history.techStates[preTechId].unlocked) continue;
                if (history.techStates[preTechId].maxLevel > history.techStates[preTechId].curLevel)
                {
                    flag = false;
                }
                GenerateTechListWithPrerequisites(history, preTechId, techIdList);
            }
        }
        if (history.techStates.ContainsKey(techId) && !history.techStates[techId].unlocked && flag)
        {
            techIdList.Add(techId);
        }
    }

    private static void CheckTechUnlockProperties(GameHistoryData history, TechProto techProto, SortedList<int, int> properties, List<Tuple<TechProto, int, int>> techList, int maxLevel = 10000, bool withPrerequisites = true)
    {
        var techStates = history.techStates;
        var techID = techProto.ID;
        if (techStates == null || !techStates.TryGetValue(techID, out var value)) return;
        if (value.unlocked) return;

        var maxLvl = Math.Min(maxLevel < 0 ? value.curLevel - maxLevel - 1 : maxLevel, value.maxLevel);
        if (withPrerequisites)
        {
            foreach (var preid in techProto.PreTechs)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    CheckTechUnlockProperties(history, preProto, properties, techList, techProto.PreTechsMax ? 10000 : preProto.Level, true);
            }
            foreach (var preid in techProto.PreTechsImplicit)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    CheckTechUnlockProperties(history, preProto, properties, techList, techProto.PreTechsMax ? 10000 : preProto.Level, true);
            }
        }

        if (value.curLevel < techProto.Level) value.curLevel = techProto.Level;
        techList.Add(new Tuple<TechProto, int, int>(techProto, value.curLevel, techProto.Level));
        while (value.curLevel <= maxLvl)
        {
            if (techProto.PropertyOverrideItemArray != null)
            {
                var propertyOverrideItemArray = techProto.PropertyOverrideItemArray;
                for (var i = 0; i < propertyOverrideItemArray.Length; i++)
                {
                    var id = propertyOverrideItemArray[i].id;
                    var count = (float)propertyOverrideItemArray[i].count;
                    var ratio = Mathf.Clamp01((float)((double)value.hashUploaded / value.hashNeeded));
                    var consume = Mathf.CeilToInt(count * (1f - ratio));
                    if (properties.TryGetValue(id, out var existingCount))
                        properties[id] = existingCount + consume;
                    else
                        properties.Add(id, consume);
                }
            }
            else
            {
                for (var i = 0; i < techProto.itemArray.Length; i++)
                {
                    var id = techProto.itemArray[i].ID;
                    var consume = (int)(techProto.ItemPoints[i] * (value.hashNeeded - value.hashUploaded) / 3600L);
                    if (properties.TryGetValue(id, out var existingCount))
                        properties[id] = existingCount + consume;
                    else
                        properties.Add(id, consume);
                }
            }
            value.curLevel++;
            value.hashUploaded = 0;
            value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
        }
    }

    private static int UnlockTechImpl(GameHistoryData history, TechProto techProto, int maxLevel = 10000, bool withPrerequisites = true)
    {
        var techStates = history.techStates;
        var techID = techProto.ID;
        if (techStates == null || !techStates.TryGetValue(techID, out var value))
        {
            return -1;
        }

        if (value.unlocked)
        {
            return -1;
        }

        var maxLvl = Math.Min(maxLevel < 0 ? value.curLevel - maxLevel - 1 : maxLevel, value.maxLevel);

        if (withPrerequisites)
        {
            foreach (var preid in techProto.PreTechs)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    UnlockTechImpl(history, preProto, techProto.PreTechsMax ? 10000 : preProto.Level, true);
            }
            foreach (var preid in techProto.PreTechsImplicit)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    UnlockTechImpl(history, preProto, techProto.PreTechsMax ? 10000 : preProto.Level, true);
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
        return maxLvl;
    }

    private static bool UnlockTechImmediately(TechProto techProto, int maxLevel = 10000, bool withPrerequisites = true)
    {
        if (techProto == null) return false;
        var history = GameMain.history;
        var ulvl = UnlockTechImpl(history, techProto, maxLevel, withPrerequisites);
        if (ulvl < 0) return false;
        history.RegFeatureKey(1000100);
        history.NotifyTechUnlock(techProto.ID, ulvl);
        return true;
    }

    public static void UnlockAllProtoWithMetadataAndPrompt()
    {
        var history = GameMain.history;
        List<TechProto> techProtos = [];
        foreach (var techProto in LDB.techs.dataArray)
        {
            if (techProto.Published && !techProto.IsObsolete && !techProto.IsHiddenTech && !history.TechUnlocked(techProto.ID) && (techProto.MaxLevel <= techProto.Level || techProto.MaxLevel <= 16) && (techProto.ID is (< 3301 or > 3309) and (< 3601 or > 3609) and (< 3901 or > 3909)) && techProto.ID != 1508)
            {
                techProtos.Add(techProto);
            }
        }
        UnlockProtoWithMetadataAndPrompt([.. techProtos], 16, false);
    }

    public static void UnlockProtoWithMetadataAndPrompt(TechProto[] techProtos, int toLevel, bool withPrerequisites = true)
    {
        var techList = new List<Tuple<TechProto, int, int>>();
        var properties = new SortedList<int, int>();
        var history = GameMain.history;
        foreach (var techProto in techProtos)
        {
            CheckTechUnlockProperties(history, techProto, properties, techList, toLevel, withPrerequisites);
        }
        var propertySystem = DSPGame.propertySystem;
        var clusterSeedKey = history.gameData.GetClusterSeedKey();
        var enough = true;
        foreach (var consumption in properties)
        {
            if (propertySystem.GetItemAvaliableProperty(clusterSeedKey, consumption.Key) >= consumption.Value) continue;
            enough = false;
            break;
        }
        if (!enough)
        {
            UIMessageBox.Show("元数据".Translate(), "元数据不足".Translate(), "确定".Translate(), UIMessageBox.ERROR);
            return;
        }

        if (!history.hasUsedPropertyBanAchievement)
        {
            UIMessageBox.Show("初次使用元数据标题".Translate(), "初次使用元数据描述".Translate(), "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null, DoUnlockCalculatedTechs);
            return;
        }

        DoUnlockCalculatedTechs();
        return;

        void DoUnlockCalculatedTechs()
        {
            if (techList.Count <= 1)
            {
                UnlockWithPropertiesImmediately();
                return;
            }
            var msg = "Do you want to use metadata to buyout the following tech?".Translate();

            if (techList.Count <= 10)
            {
                foreach (var tuple in techList)
                {
                    AddToMsg(ref msg, tuple);
                }
            }
            else
            {
                for (var i = 0; i < 5; i++)
                {
                    AddToMsg(ref msg, techList[i]);
                }
                msg += "\n  ...";
                for (var i = techList.Count - 5; i < techList.Count; i++)
                {
                    AddToMsg(ref msg, techList[i]);
                }
            }

            msg += "\n\n";
            msg += "The following is the required metadata for buyout:".Translate();
            foreach (var consumption in properties)
            {
                if (consumption.Value <= 0) continue;
                msg += $"\n  {LDB.items.Select(consumption.Key).propertyName}x{consumption.Value}";
            }
            UIMessageBox.Show("Batch buyout tech".Translate(), msg, "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null, UnlockWithPropertiesImmediately);
            return;

            void AddToMsg(ref string str, Tuple<TechProto, int, int> tuple)
            {
                if (tuple.Item2 == tuple.Item3)
                {
                    if (tuple.Item2 <= 0)
                        str += $"\n  {tuple.Item1.name}";
                    else
                        str += $"\n  {tuple.Item1.name}{"杠等级".Translate()}{tuple.Item2}";
                }
                else
                    str += $"\n  {tuple.Item1.name}{"杠等级".Translate()}{tuple.Item2}->{tuple.Item3}";
            }
        }

        void UnlockWithPropertiesImmediately()
        {
            var mainPlayer = GameMain.mainPlayer;
            foreach (var consumption in properties)
            {
                if (consumption.Value <= 0) continue;
                propertySystem.AddItemConsumption(clusterSeedKey, consumption.Key, consumption.Value);
                history.AddPropertyItemConsumption(consumption.Key, consumption.Value, true);
                mainPlayer.mecha.AddProductionStat(consumption.Key, consumption.Value, mainPlayer.nearestFactory);
                mainPlayer.mecha.AddConsumptionStat(consumption.Key, consumption.Value, mainPlayer.nearestFactory);
            }
            foreach (var techProto in techProtos)
            {
                UnlockTechImmediately(techProto, toLevel, withPrerequisites);
            }
            history.VerifyTechQueue();
            if (history.currentTech != history.techQueue[0])
            {
                history.currentTech = history.techQueue[0];
            }
        }
    }

    public static void RemoveCargoStackingTechs()
    {
        var history = GameMain.data?.history;
        if (history == null) return;
        history.inserterStackCountObsolete = 1;
        for (var id = 3301; id <= 3305; id++)
        {
            history.techStates.TryGetValue(id, out var state);
            if (!state.unlocked) continue;
            state.unlocked = false;
            state.hashUploaded = 0;
            history.techStates[id] = state;
        }
    }
}
