using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches;

public static class TechPatch
{
    public static ConfigEntry<bool> SorterCargoStackingEnabled;
    public static ConfigEntry<bool> DisableBattleRelatedTechsInPeaceModeEnabled;
    public static ConfigEntry<bool> BatchBuyoutTechEnabled;

    public static void Init()
    {
        I18N.Add("分拣器运货量", "Sorter Mk.III cargo stacking : ", "极速分拣器每次可运送 ");
        SorterCargoStackingEnabled.SettingChanged += (_, _) => SorterCargoStacking.Enable(SorterCargoStackingEnabled.Value);
        DisableBattleRelatedTechsInPeaceModeEnabled.SettingChanged += (_, _) => DisableBattleRelatedTechsInPeaceMode.Enable(DisableBattleRelatedTechsInPeaceModeEnabled.Value);
        BatchBuyoutTechEnabled.SettingChanged += (_, _) => BatchBuyoutTech.Enable(BatchBuyoutTechEnabled.Value);
    }

    public static void Start()
    {
        SorterCargoStacking.Enable(SorterCargoStackingEnabled.Value);
        DisableBattleRelatedTechsInPeaceMode.Enable(DisableBattleRelatedTechsInPeaceModeEnabled.Value);
        BatchBuyoutTech.Enable(BatchBuyoutTechEnabled.Value);
    }

    public static void Uninit()
    {
        BatchBuyoutTech.Enable(false);
        DisableBattleRelatedTechsInPeaceMode.Enable(false);
        SorterCargoStacking.Enable(false);
    }

    private class SorterCargoStacking
    {
        private static bool _protoPatched;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                TryPatchProto(true);
                GameLogicProc.OnDataLoaded += VFPreload_InvokeOnLoadWorkEnded_Postfix;
            }
            else
            {
                GameLogicProc.OnDataLoaded -= VFPreload_InvokeOnLoadWorkEnded_Postfix;
                TryPatchProto(false);
            }
        }

        private static void TryPatchProto(bool on)
        {
            if (DSPGame.IsMenuDemo) return;
            var techs = LDB.techs;
            if (techs == null || techs?.dataArray == null || techs.dataArray.Length == 0) return;
            if (on)
            {
                var delim = -26.0f;
                var x = 9.0f;
                var y = -27.0f;
                var tp3301 = techs.Select(3301);
                if (tp3301 != null && tp3301.IsObsolete)
                {
                    _protoPatched = false;
                    var tp3311 = techs.Select(3311);
                    if (tp3311 != null)
                    {
                        x = tp3311.Position.x;
                        y = tp3311.Position.y;
                        delim = y + 1.0f;
                    }
                }
                if (_protoPatched) return;

                foreach (var tp in techs.dataArray)
                {
                    switch (tp.ID)
                    {
                        case >= 3301 and <= 3305:
                            tp.UnlockValues[0] = tp.ID - 3300 + 1;
                            tp.IsObsolete = false;
                            tp.Position = new Vector2(x + 4.0f * (tp.ID - 3301), y);
                            if (tp.ID == 3305)
                            {
                                tp.postTechArray = [];
                                if (UIRoot.instance.uiGame.techTree.nodes.TryGetValue(tp.ID, out var node))
                                {
                                    node.outputLine.gameObject.SetActive(false);
                                }
                            }
                            continue;
                        case 3306:
                            tp.PreTechs = [];
                            tp.preTechArray = [];
                            tp.Position = new Vector2(x + 4.0f * (tp.ID - 3301), y);
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
            UIRoot.instance.uiGame.techTree.OnPageChanged();
        }

        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            TryPatchProto(true);
        }
    }

    private static class DisableBattleRelatedTechsInPeaceMode
    {
        private static bool _protoPatched;
        private static HashSet<int> _techsToDisableSet;
        private static Dictionary<int, TechProto[]> _originTechPosts = [];

        public static void Enable(bool enable)
        {
            if (_techsToDisableSet == null)
            {
                (int, int)[] techListToDisable =
                [
                    // Explosive Unit, Crystal Explosive Unit
                    // 爆破单元，晶石爆破单元
                    (1803, 1804),
                    // Implosion Cannon
                    // 聚爆加农炮
                    (1807, 1807),
                    // Signal Tower, Planetary Defense System, Jammer Tower, Plasma Turret, Titanium Ammo Box, Superalloy Ammo Box, High-Explosive Shell Set, Supersonic Missile Set, Crystal Shell Set, Gravity Missile Set, Antimatter Capsule, Precision Drone, Prototype, Attack Drone, Corvette, Destroyer, Suppressing Capsule, EM Capsule Mk.III
                    // 信号塔, 行星防御系统, 干扰塔, 磁化电浆炮, 钛化弹箱, 超合金弹箱, 高爆炮弹组, 超音速导弹组, 晶石炮弹组, 引力导弹组, 反物质胶囊, 地面战斗机-A型, 地面战斗机-E型, 地面战斗机-F型, 太空战斗机-A型, 太空战斗机-F型, 电磁胶囊II, 电磁胶囊III
                    (1809, 1825),
                    // Auto Reconstruction Marking
                    // 自动标记重建
                    (2951, 2956),
                    // Energy Shield
                    // 能量护盾
                    (2801, 2807),
                    // Kinetic Weapon Damage
                    // 动能武器伤害
                    (5001, 5006),
                    // Energy Weapon Damage
                    // 能量武器伤害
                    (5101, 5106),
                    // Explosive Weapon Damage
                    // 爆炸武器伤害
                    (5201, 5206),
                    // Combat Drone Damage
                    // 战斗无人机伤害
                    (5301, 5305),
                    // Combat Drone Attack Speed
                    // 战斗无人机攻击速度
                    (5401, 5405),
                    // Combat Drone Engine
                    // 战斗无人机引擎
                    (5601, 5605),
                    // Combat Drone Durability
                    // 战斗无人机耐久
                    (5701, 5705),
                    // Ground Squadron Expansion
                    // 地面编队扩容
                    (5801, 5807),
                    // Space Fleet Expansion
                    // 太空编队扩容
                    (5901, 5907),
                    // Enhanced Structure
                    // 结构强化
                    (6001, 6006),
                    // Planetary Shield
                    // 行星护盾
                    (6101, 6106),
                ];
                _techsToDisableSet = [.. techListToDisable.SelectMany(t => Enumerable.Range(t.Item1, t.Item2 - t.Item1 + 1))];
            }
            if (enable)
            {
                if (DSPGame.GameDesc != null)
                    TryPatchProto(DSPGame.GameDesc.isPeaceMode);
                GameLogicProc.OnGameBegin += OnGameBegin;
            }
            else
            {
                GameLogicProc.OnGameBegin -= OnGameBegin;
                TryPatchProto(false);
            }
        }

        private static void OnGameBegin()
        {
            TryPatchProto(DSPGame.GameDesc.isPeaceMode);
        }

        private static void TryPatchProto(bool on)
        {
            if (DSPGame.IsMenuDemo || on == _protoPatched) return;
            var techs = LDB.techs;
            if (techs?.dataArray == null || techs.dataArray.Length == 0) return;
            if (on)
            {
                foreach (var tp in techs.dataArray)
                {
                    if (_techsToDisableSet.Contains(tp.ID))
                    {
                        tp.IsObsolete = true;
                    }
                    var postTechs = tp.postTechArray;
                    for (var i = postTechs.Length - 1; i >= 0; i--)
                    {
                        if (_techsToDisableSet.Contains(postTechs[i].ID))
                        {
                            _originTechPosts[tp.ID] = postTechs;
                            tp.postTechArray = [.. postTechs.Where(p => !_techsToDisableSet.Contains(p.ID))];
                            break;
                        }
                    }
                }
                _protoPatched = true;
            }
            else
            {
                foreach (var tp in techs.dataArray)
                {
                    if (_techsToDisableSet.Contains(tp.ID))
                    {
                        tp.IsObsolete = false;
                    }
                    if (_originTechPosts.TryGetValue(tp.ID, out var postTechs))
                    {
                        tp.postTechArray = postTechs;
                    }
                }
                _originTechPosts.Clear();
                _protoPatched = false;
            }
            var nodes = UIRoot.instance.uiGame.techTree.nodes;
            var history = GameMain.history;
            foreach (var item in nodes)
            {
                var node = item.Value;
                foreach (var arrow in node.arrows)
                {
                    arrow.gameObject.SetActive(false);
                }
                var postTechArray = node.techProto.postTechArray;
                var active = postTechArray.Length > 0;
                node.connGroup.gameObject.SetActive(active);
                node.connImages = active ? node.connGroup.GetComponentsInChildren<Image>(true) : [];
                if (!active) continue;
                bool onlyOneOutput = postTechArray.Length == 1;
                for (var i = 0; i < postTechArray.Length; i++)
                {
                    var postTech = postTechArray[i];
                    if (!nodes.ContainsKey(postTech.ID)) continue;
                    bool itemUnlocked = true;
                    for (var n = 0; n < postTech.PreItem.Length; n++)
                    {
                        if (history.ItemUnlocked(postTech.PreItem[n])) continue;
                        itemUnlocked = false;
                        break;
                    }
                    bool techUnlocked = history.TechUnlocked(postTech.ID);
                    node.arrows[i].gameObject.SetActive(itemUnlocked || techUnlocked);
                    if (onlyOneOutput && !itemUnlocked && !techUnlocked)
                    {
                        node.outputArrow.enabled = false;
                        node.outputLine.gameObject.SetActive(false);
                    }
                    else
                    {
                        node.outputLine.gameObject.SetActive(true);
                    }
                }
            }
            UIRoot.instance.uiGame.techTree.OnPageChanged();
        }
    }

    private class BatchBuyoutTech : PatchImpl<BatchBuyoutTech>
    {
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
            Functions.TechFunctions.UnlockProtoWithMetadataAndPrompt([techProto], -1, true);
            return false;
        }
    }
}
