using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches;

public static class TechPatch
{
    public static ConfigEntry<bool> SorterCargoStackingEnabled;
    public static ConfigEntry<bool> DisableBattleRelatedTechsInPeaceModeEnabled;
    public static ConfigEntry<bool> BatchBuyoutTechEnabled;

    public static void Init()
    {
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
                var tp3301 = techs.Select(TechIds.SorterCargoStackingCustomStart);
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
                        case >= TechIds.SorterCargoStackingCustomStart and <= TechIds.SorterCargoStackingCustomEnd - 1:
                            tp.UnlockValues[0] = tp.ID - TechIds.SorterCargoStackingCustomStart + 2;
                            tp.IsObsolete = false;
                            tp.Position = new Vector2(x + 4.0f * (tp.ID - TechIds.SorterCargoStackingCustomStart), y);
                            if (tp.ID == TechIds.SorterCargoStackingCustomEnd - 1)
                            {
                                tp.postTechArray = [];
                                if (UIRoot.instance.uiGame.techTree.nodes.TryGetValue(tp.ID, out var node))
                                {
                                    node.outputLine.gameObject.SetActive(false);
                                }
                            }
                            continue;
                        case TechIds.SorterCargoStackingCustomEnd:
                            tp.PreTechs = [];
                            tp.preTechArray = [];
                            tp.Position = new Vector2(x + 4.0f * (tp.ID - TechIds.SorterCargoStackingCustomStart), y);
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
                var tp3301 = techs.Select(TechIds.SorterCargoStackingCustomStart);
                if (tp3301 != null && !tp3301.IsObsolete)
                {
                    _protoPatched = true;
                    delim = tp3301.Position.y - 1.0f;
                }
                if (!_protoPatched) return;
                foreach (var tp in techs.dataArray)
                {
                    if (tp.ID is >= TechIds.SorterCargoStackingCustomStart and <= TechIds.SorterCargoStackingCustomEnd)
                    {
                        tp.IsObsolete = true;
                        continue;
                    }

                    if (tp.Position.y > delim) continue;
                    tp.Position.y += 4.0f;
                }

                _protoPatched = false;
            }
            LDB.techs.Signature = ProtoSignature_0_10_30_3100.CalculateSignature(LDB.techs);
            var techTree = UIRoot.instance?.uiGame?.techTree;
            if (techTree != null && techTree.isActiveAndEnabled)
                techTree.OnPageChanged();
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
            _techsToDisableSet ??= new HashSet<int>(TechIds.CombatTechs);
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
            LDB.techs.Signature = ProtoSignature_0_10_30_3100.CalculateSignature(LDB.techs);
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
            var techTree = UIRoot.instance?.uiGame?.techTree;
            if (techTree != null && techTree.isActiveAndEnabled)
                techTree.OnPageChanged();
        }
    }

    private class BatchBuyoutTech : PatchImpl<BatchBuyoutTech>
    {
        // Harmony transpiler: UITechNode_UpdateInfoDynamic_Transpiler
        // Target: UITechNode.UpdateInfoDynamic
        // Fallback: None — patch will fail loudly if the target method body changes.
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
