using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CheatEnabler.Patches.Tech;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using UXAssist.Common.Patching;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches;

/// <summary>
/// Unlock / downgrade techs by clicking the tech tree with a key-modifier held.
///
/// This replaces the previous unlock-only handler: the option keeps its original config key
/// (<c>General/UnlockTech</c>) and its position in the config panel, but now it also owns downgrade
/// mode, so a single switch covers both directions.
///
/// <list type="bullet">
/// <item><c>UITechNode.OnPointerDown</c> - transpiler that injects <see cref="TechClickHandler"/>.</item>
/// <item><c>UITechTree._OnOpen</c> / <c>_OnFree</c> - create and release the help button.</item>
/// <item>The per-frame <c>Caps Lock</c> poll runs through <see cref="OnInputUpdate"/>, the registry
/// lifecycle hook, so no separate <c>VFInput.OnUpdate</c> patch is needed.</item>
/// </list>
/// </summary>
[ModFeature("TechTweaks")]
public static class TechTweaksPatch
{
    public static ConfigEntry<bool> Enabled;

    public static void Init()
    {
        Enabled.SettingChanged += (_, _) =>
        {
            Impl.Enable(Enabled.Value);
            DowngradeMode.Reset();
        };
        GameLogicProc.OnGameEnd += ResetState;
    }

    public static void Start()
    {
        Impl.Enable(Enabled.Value);
    }

    public static void Uninit()
    {
        GameLogicProc.OnGameEnd -= ResetState;
        Impl.Enable(false);
    }

    /// <summary>Per-frame input hook driven by the mod feature registry.</summary>
    public static void OnInputUpdate()
    {
        try
        {
            DowngradeMode.Update();
        }
        catch (Exception e)
        {
            Log.Warn($"downgrade mode update failed: {e}");
        }
    }

    private static void ResetState()
    {
        DowngradeMode.Reset();
        TechTreeUI.Reset();
    }

    /// <summary>Harmony patch slot to use; mirrors <c>HarmonyLib.HarmonyPatchType</c>.</summary>
    private enum PatchKind
    {
        Postfix,
        Prefix,
        Transpiler
    }

    public sealed class Impl : PatchImpl<Impl>
    {
        protected override void OnEnable()
        {
            // Downgrade mode is a per-session switch and always starts out off.
            DowngradeMode.Reset();

            PatchMethod(typeof(UITechNode), "OnPointerDown", nameof(UITechNode_OnPointerDown_Transpiler),
                PatchKind.Transpiler);
            PatchMethod(typeof(UITechTree), "_OnOpen", nameof(UITechTree_OnOpen_Postfix));
            PatchMethod(typeof(UITechTree), "_OnFree", nameof(UITechTree_OnFree_Postfix));
        }

        protected override void OnDisable()
        {
            DowngradeMode.Reset();
            TechTreeUI.Reset();
        }

        private static void PatchMethod(Type type, string targetName, string patchName, PatchKind kind = PatchKind.Postfix)
        {
            var harmony = GetHarmony();
            if (harmony == null)
            {
                Log.Warn($"cannot patch {type.Name}.{targetName}: this feature is not enabled");
                return;
            }

            MethodInfo target = AccessTools.Method(type, targetName);
            if (target == null)
            {
                Log.Warn($"patch target {type.Name}.{targetName} not found; a game update may have changed it. " +
                         "This part of the feature stays inactive.");
                return;
            }

            MethodInfo patch = AccessTools.Method(typeof(Impl), patchName);
            if (patch == null)
            {
                Log.Warn($"patch method {patchName} not found");
                return;
            }

            try
            {
                harmony.Patch(target,
                    kind == PatchKind.Prefix ? new HarmonyMethod(patch) : null,
                    kind == PatchKind.Postfix ? new HarmonyMethod(patch) : null,
                    kind == PatchKind.Transpiler ? new HarmonyMethod(patch) : null);
            }
            catch (Exception e)
            {
                Log.Warn($"failed to patch {type.Name}.{targetName}: {e.Message}");
            }
        }

        private static void UITechTree_OnOpen_Postfix(UITechTree __instance)
        {
            try
            {
                TechTreeUI.OnTechTreeOpened(__instance);
            }
            catch (Exception e)
            {
                Log.Warn($"creating the tech tree UI failed: {e}");
            }
        }

        private static void UITechTree_OnFree_Postfix()
        {
            try
            {
                TechTreeUI.OnTechTreeFreed();
            }
            catch (Exception e)
            {
                Log.Warn($"releasing the tech tree UI failed: {e}");
            }
        }

        /// <summary>
        /// Injects <c>TechClickHandler.OnClickTech(UITechNode)</c> at the start of the click handling.
        /// The injection point is the <c>tree.selected</c> read, which is the first thing the method does
        /// after the "tech id 1" early-out, so ignored nodes never reach the handler.
        ///
        /// The method is not rewritten in any other way; if the pattern is not found the original
        /// instructions are returned unchanged and the feature simply does not react to clicks.
        /// </summary>
        // Harmony transpiler: UITechNode_OnPointerDown_Transpiler
        // Target: UITechNode.OnPointerDown
        // Fallback: TranspilerGuard returns the original instructions and logs a warning — the
        //           key-modifier click handling then stays inactive instead of throwing.
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

            MethodInfo handler = AccessTools.Method(typeof(TechClickHandler), nameof(TechClickHandler.OnClickTech));
            if (handler == null)
            {
                Log.Warn("TechClickHandler.OnClickTech not found, key-modifier click handling is inactive");
                return instructions;
            }

            // Move the labels of the matched instruction onto the injected sequence so that any branch
            // which used to land there now runs the handler first.
            List<Label> labels = matcher.Labels;
            matcher.Labels = null;
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Call, handler));

            return matcher.Finish(instructions, CheatEnabler.Logger, nameof(UITechNode_OnPointerDown_Transpiler));
        }
    }

    private static class Log
    {
        public static void Warn(string message)
        {
            CheatEnabler.Logger.LogWarning("[TechTweaks] " + message);
        }
    }
}
