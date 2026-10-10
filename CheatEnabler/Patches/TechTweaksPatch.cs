using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CheatEnabler.Patches.Tech;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using UXAssist.Common.Patching;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches;

/// <summary>Owns the General/UnlockTech option, session input and tech tree patches.</summary>
[ModFeature("TechTweaks")]
public static class TechTweaksPatch
{
    public static ConfigEntry<bool> Enabled;

    public static void Init()
    {
        Enabled.SettingChanged += OnSettingChanged;
        GameLogicProc.OnGameEnd += ResetState;
    }

    public static void Start() => Impl.Enable(Enabled.Value);

    public static void Uninit()
    {
        Enabled.SettingChanged -= OnSettingChanged;
        GameLogicProc.OnGameEnd -= ResetState;
        Impl.Enable(false);
        ResetState();
    }

    private static void OnSettingChanged(object sender, EventArgs args) => Impl.Enable(Enabled.Value);

    public static void OnInputUpdate() => DowngradeMode.Update();

    private static void ResetState()
    {
        DowngradeMode.Reset();
        TechTreeUI.Reset();
    }

    public sealed class Impl : PatchImpl<Impl>
    {
        protected override void OnEnable()
        {
            DowngradeMode.Reset();
            UITechTree window = UIRoot.instance?.uiGame?.techTree;
            if (window != null && window.active) UpdateHelpButton(window);
        }

        protected override void OnDisable() => ResetState();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree._OnOpen))]
        private static void UITechTree_OnOpen_Postfix(UITechTree __instance) => UpdateHelpButton(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree.OnLanguageChange))]
        private static void UITechTree_OnLanguageChange_Postfix() => TechTreeUI.RefreshText();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree._OnFree))]
        private static void UITechTree_OnFree_Postfix() => TechTreeUI.Reset();

        private static void UpdateHelpButton(UITechTree window)
        {
            try
            {
                TechTreeUI.OnTechTreeOpened(window);
            }
            catch (Exception e)
            {
                TechTreeUI.Reset();
                CheatEnabler.Logger.LogWarning($"[TechTweaks] creating the tech tree help button failed: {e}");
            }
        }

        // Harmony transpiler: UITechNode_OnPointerDown_Transpiler
        // Target: UITechNode.OnPointerDown
        // Fallback: TranspilerGuard returns original instructions and logs a warning.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.OnPointerDown))]
        private static IEnumerable<CodeInstruction> UITechNode_OnPointerDown_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UITechNode), nameof(UITechNode.tree))),
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(UITechTree), nameof(UITechTree.selected))));
            if (matcher.IsInvalid)
            {
                return matcher.Finish(instructions, CheatEnabler.Logger, nameof(UITechNode_OnPointerDown_Transpiler));
            }

            var labels = matcher.Labels;
            matcher.Labels = new List<Label>();
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(TechClickHandler), nameof(TechClickHandler.OnClickTech))));

            return matcher.Finish(instructions, CheatEnabler.Logger, nameof(UITechNode_OnPointerDown_Transpiler));
        }
    }
}
