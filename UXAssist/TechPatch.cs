using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist;

public static class TechPatch
{
    public static ConfigEntry<bool> SorterCargoStackingEnabled;
    public static ConfigEntry<bool> BatchBuyoutTechEnabled;
    
    public static void Init()
    {
        I18N.Add("分拣器运货量", "Sorter Mk.III cargo stacking : ", "极速分拣器每次可运送 ");
        SorterCargoStackingEnabled.SettingChanged += (_, _) => SorterCargoStacking.Enable(SorterCargoStackingEnabled.Value);
        BatchBuyoutTechEnabled.SettingChanged += (_, _) => BatchBuyoutTech.Enable(BatchBuyoutTechEnabled.Value);
        SorterCargoStacking.Enable(SorterCargoStackingEnabled.Value);
        BatchBuyoutTech.Enable(BatchBuyoutTechEnabled.Value);
    }
    
    public static void Uninit()
    {
        BatchBuyoutTech.Enable(false);
        SorterCargoStacking.Enable(false);
    }
    
    private static class SorterCargoStacking
    {
        private static Harmony _patch;
        private static bool _protoPatched;
        
        public static void Enable(bool on)
        {
            TryPatchProto(on);
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(SorterCargoStacking));
                GameLogic.OnDataLoaded += VFPreload_InvokeOnLoadWorkEnded_Postfix;
                return;
            }
            GameLogic.OnDataLoaded -= VFPreload_InvokeOnLoadWorkEnded_Postfix;
            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static void TryPatchProto(bool on)
        {
            var techs = LDB.techs;
            if (techs == null || techs.dataArray == null || techs.dataArray.Length == 0) return;
            if (on)
            {
                var delim = -26.0f;
                var tp3301 = techs.Select(3301);
                if (tp3301 != null && tp3301.IsObsolete)
                {
                    _protoPatched = false;
                    delim = tp3301.Position.y + 1.0f;
                }
                if (_protoPatched) return;

                foreach (var tp in techs.dataArray)
                {
                    switch (tp.ID)
                    {
                        case >= 3301 and <= 3305:
                            tp.UnlockValues[0] = tp.ID - 3300 + 1;
                            tp.IsObsolete = false;
                            continue;
                        case 3306:
                            tp.PreTechs = [];
                            continue;
                    }

                    if (tp.Position.y > delim) continue;
                    tp.Position.y -= 4.0f;
                }

                _protoPatched = true;
            }
            else
            {
                var delim = -28.0f;
                var tp3301 = techs.Select(3301);
                if (tp3301 != null && !tp3301.IsObsolete)
                {
                    _protoPatched = true;
                    delim = tp3301.Position.y - 1.0f;
                }
                if (!_protoPatched) return;
                foreach (var tp in techs.dataArray)
                {
                    if (tp.ID is >= 3301 and <= 3306)
                    {
                        tp.IsObsolete = true;
                        continue;
                    }

                    if (tp.Position.y > delim) continue;
                    tp.Position.y += 4.0f;
                }

                _protoPatched = false;
            }
        }

        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            TryPatchProto(true);
        }
    }

    private static class BatchBuyoutTech
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BatchBuyoutTech));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static void GenerateTechList(GameHistoryData history, int techId, List<int> techIdList)
        {
            var techProto = LDB.techs.Select(techId);
            if (techProto == null || !techProto.Published) return;
            var flag = true;
            for (var i = 0; i < 2; i++)
            {
                var array = techProto.PreTechs;
                if (i == 1)
                {
                    array = techProto.PreTechsImplicit;
                }
                for (var j = 0; j < array.Length; j++)
                {
                    if (!history.techStates.ContainsKey(array[j]) || history.techStates[array[j]].unlocked) continue;
                    if (history.techStates[array[j]].maxLevel > history.techStates[array[j]].curLevel)
                    {
                        flag = false;
                    }
                    GenerateTechList(history, array[j], techIdList);
                }
            }
            if (history.techStates.ContainsKey(techId) && !history.techStates[techId].unlocked && flag)
            {
                techIdList.Add(techId);
            }
        }

        
        private static void CheckTechUnlockProperties(GameHistoryData history, TechProto techProto, int[] properties, List<Tuple<TechProto, int, int>> techList, int maxLevel = 10000)
        {
            var techStates = history.techStates;
            var techID = techProto.ID;
            if (techStates == null || !techStates.TryGetValue(techID, out var value)) return;
            if (value.unlocked) return;

            var maxLvl = Math.Min(maxLevel < 0 ? value.curLevel - maxLevel - 1 : maxLevel, value.maxLevel);

            foreach (var preid in techProto.PreTechs)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    CheckTechUnlockProperties(history, preProto, properties, techList, techProto.PreTechsMax ? 10000 : preProto.Level);
            }
            foreach (var preid in techProto.PreTechsImplicit)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    CheckTechUnlockProperties(history, preProto, properties, techList, techProto.PreTechsMax ? 10000 : preProto.Level);
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
                        properties[id - 6001] += consume;
                    }
                }
                else
                {
                    for (var j = 0; j < techProto.itemArray.Length; j++)
                    {
                        var id = techProto.itemArray[j].ID;
                        var consume = (int)(techProto.ItemPoints[j] * (value.hashNeeded - value.hashUploaded) / 3600L);
                        properties[id - 6001] += consume;
                    }
                }
                value.curLevel++;
                value.hashUploaded = 0;
                value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
            }
        }

        private static int UnlockTechRecursiveImpl(GameHistoryData history, TechProto techProto, int maxLevel = 10000)
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

            foreach (var preid in techProto.PreTechs)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    UnlockTechRecursiveImpl(history, preProto, techProto.PreTechsMax ? 10000 : preProto.Level);
            }
            foreach (var preid in techProto.PreTechsImplicit)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    UnlockTechRecursiveImpl(history, preProto, techProto.PreTechsMax ? 10000 : preProto.Level);
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

        private static bool UnlockTechRecursive(TechProto techProto, int maxLevel = 10000)
        {
            if (techProto == null) return false;
            var history = GameMain.history;
            var ulvl = UnlockTechRecursiveImpl(history, techProto, maxLevel);
            if (ulvl < 0) return false;
            history.RegFeatureKey(1000100);
            history.NotifyTechUnlock(techProto.ID, ulvl);
            return true;
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.UpdateInfoDynamic))]
        private static IEnumerable<CodeInstruction> UITechNode_UpdateInfoDynamic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(UITechTree), nameof(UITechTree.showProperty))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.And)
            ).Advance(1).SetAndAdvance(OpCodes.Ldloc_3, null).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq)
            );
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.OnBuyoutButtonClick))]
        private static bool UITechNode_OnBuyoutButtonClick_Prefix(UITechNode __instance)
        {
            if (GameMain.isFullscreenPaused)
            {
                return false;
            }
            var techProto = __instance.techProto;
            if (techProto == null) return false;
            var properties = new int[6];
            List<Tuple<TechProto, int, int>> techList = new();
            var history = GameMain.history;
            var maxLevel = -1;
            CheckTechUnlockProperties(history, techProto, properties, techList, maxLevel);
            var propertySystem = DSPGame.propertySystem;
            var clusterSeedKey = history.gameData.GetClusterSeedKey();
            var enough = true;
            for (var i = 0; i < 6; i++)
            {
                if (propertySystem.GetItemAvaliableProperty(clusterSeedKey, 6001 + i) >= properties[i]) continue;
                enough = false;
                break;
            }
            if (!enough)
            {
                UIRealtimeTip.Popup("元数据不足".Translate(), true, 0);
                return false;
            }

            if (!history.hasUsedPropertyBanAchievement)
            {
                UIMessageBox.Show("初次使用元数据标题".Translate(), "初次使用元数据描述".Translate(), "取消".Translate(), "确定".Translate(), 2, null, DoUnlockFunc);
                return false;
            }

            DoUnlockFunc();
            return false;

            void DoUnlockFunc()
            {
                if (techList.Count <= 1)
                {
                    DoUnlockFuncInternal();
                    return;
                }
                var msg = "要使用元数据买断以下科技吗？";

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
                    msg += "  ...\n";
                    for (var i = techList.Count - 5; i < techList.Count; i++)
                    {
                        AddToMsg(ref msg, techList[i]);
                    }
                }

                msg += "\n\n";
                msg += "以下是买断所需元数据：";
                for (var i = 0; i < 6; i++)
                {
                    var itemCount = properties[i];
                    if (itemCount <= 0) continue;
                    msg += $"\n  {LDB.items.Select(6001 + i).propertyName}x{itemCount}";
                }
                UIMessageBox.Show("批量买断科技", msg, "取消".Translate(), "确定".Translate(), 2, null, DoUnlockFuncInternal);
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

            void DoUnlockFuncInternal()
            {
                UnlockTechRecursive(__instance.techProto, maxLevel);
                history.VarifyTechQueue();
                if (history.currentTech != history.techQueue[0])
                {
                    history.currentTech = history.techQueue[0];
                }
                var mainPlayer = GameMain.mainPlayer;
                for (var i = 0; i < 6; i++)
                {
                    var itemCount = properties[i];
                    if (itemCount <= 0) continue;
                    var itemId = 6001 + i;
                    propertySystem.AddItemConsumption(clusterSeedKey, itemId, itemCount);
                    history.AddPropertyItemConsumption(itemId, itemCount, true);
                    mainPlayer.mecha.AddProductionStat(itemId, itemCount, mainPlayer.nearestFactory);
                    mainPlayer.mecha.AddConsumptionStat(itemId, itemCount, mainPlayer.nearestFactory);
                }
            }
        }
    }
}
