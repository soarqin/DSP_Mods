using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;

namespace CheatEnabler;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CheatEnabler : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _devShortcuts = true;
    private bool _disableAbnormalChecks = true;
    private bool _alwaysInfiniteResource = true;

    private void Awake()
    {
        _devShortcuts = Config.Bind("General", "DevShortcuts", _devShortcuts, "enable DevMode shortcuts").Value;
        _disableAbnormalChecks = Config.Bind("General", "DisableAbnormalChecks", _disableAbnormalChecks,
            "disable all abnormal checks").Value;
        _alwaysInfiniteResource = Config.Bind("General", "AlwaysInfiniteResource", _alwaysInfiniteResource,
            "always infinite resource").Value;
        if (_devShortcuts)
        {
            Harmony.CreateAndPatchAll(typeof(DevShortcuts));
        }
        if (_disableAbnormalChecks)
        {
            Harmony.CreateAndPatchAll(typeof(AbnormalDisabler));
        }
        if (_alwaysInfiniteResource)
        {
            Harmony.CreateAndPatchAll(typeof(AlwaysInfiniteResource));
        }
    }

    private class DevShortcuts
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "Init")]
        private static void PlayerControllerInit(PlayerController __instance)
        {
            var cnt = __instance.actions.Length;
            var newActions = new PlayerAction[cnt + 1];
            for (var i = 0; i < cnt; i++)
            {
                newActions[i] = __instance.actions[i];
            }

            var test = new PlayerAction_Test();
            test.Init(__instance.player);
            newActions[cnt] = test;
            __instance.actions = newActions;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAction_Test), "GameTick")]
        private static void PlayerAction_TestGameTick(PlayerAction_Test __instance)
        {
            var lastActive = __instance.active;
            __instance.Update();
            if (lastActive != __instance.active)
            {
                UIRealtimeTip.PopupAhead((lastActive ? "Developer Mode Shortcuts Disabled" : "Developer Mode Shortcuts Enabled").Translate(), false);
            }
        }
    }

    private class AbnormalDisabler
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AbnormalityLogic), "NotifyBeforeGameSave")]
        [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnAssemblerRecipePick")]
        [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnGameBegin")]
        [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnMechaForgeTaskComplete")]
        [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnUnlockTech")]
        [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnUseConsole")]
        private static bool DisableAbnormalLogic()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AbnormalityLogic), "InitDeterminators")]
        private static bool DisableAbnormalDeterminators(ref Dictionary<int, AbnormalityDeterminator> ___determinators)
        {
            ___determinators = new Dictionary<int, AbnormalityDeterminator>();
            return false;
        }
    }

    private class AlwaysInfiniteResource
    {
        private static FieldInfo _rmulField = AccessTools.Field(typeof(GameDesc), nameof(GameDesc.resourceMultiplier));

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameDesc), "isInfiniteResource", MethodType.Getter)]
        private static IEnumerable<CodeInstruction> ForceInfiniteResource(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_I4, 1);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int),
            typeof(int))]
        [HarmonyPatch(typeof(UIMinerWindow), "_OnUpdate")]
        [HarmonyPatch(typeof(UIVeinCollectorPanel), "_OnUpdate")]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand.Equals(99.5f))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}