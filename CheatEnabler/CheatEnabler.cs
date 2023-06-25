using System;
using System.Collections.Generic;
using System.Linq;
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
    private bool _waterPumpAnywhere = true;
    private static bool _sitiVeinsOnBirthPlanet = true;
    private static bool _fireIceOnBirthPlanet = false;
    private static bool _kimberliteOnBirthPlanet = false;
    private static bool _fractalOnBirthPlanet = false;
    private static bool _organicOnBirthPlanet = true;
    private static bool _opticalOnBirthPlanet = false;
    private static bool _spiniformOnBirthPlanet = false;
    private static bool _unipolarOnBirthPlanet = false;
    private static bool _flatBirthPlanet = true;
    private static bool _highLuminosityBirthStar = true;
    private static bool _terraformAnyway = false;
    private static string _unlockTechToMaximumLevel = "";
    private static readonly List<int> TechToUnlock = new();

    private void Awake()
    {
        _devShortcuts = Config.Bind("General", "DevShortcuts", _devShortcuts, "enable DevMode shortcuts").Value;
        _disableAbnormalChecks = Config.Bind("General", "DisableAbnormalChecks", _disableAbnormalChecks,
            "disable all abnormal checks").Value;
        _alwaysInfiniteResource = Config.Bind("General", "AlwaysInfiniteResource", _alwaysInfiniteResource,
            "always infinite resource").Value;
        _unlockTechToMaximumLevel = Config.Bind("General", "UnlockTechToMaxLevel", _unlockTechToMaximumLevel,
            "Unlock listed tech to MaxLevel").Value;
        _waterPumpAnywhere = Config.Bind("General", "WaterPumpAnywhere", _waterPumpAnywhere,
            "Can pump water anywhere (while water type is not None)").Value;
        _sitiVeinsOnBirthPlanet = Config.Bind("Birth", "SiTiVeinsOnBirthPlanet", _sitiVeinsOnBirthPlanet,
            "Has Silicon/Titanium veins on birth planet").Value;
        _fireIceOnBirthPlanet = Config.Bind("Birth", "FireIceOnBirthPlanet", _fireIceOnBirthPlanet,
            "Fire ice on birth planet (You should enable Rare Veins first)").Value;
        _kimberliteOnBirthPlanet = Config.Bind("Birth", "KimberliteOnBirthPlanet", _kimberliteOnBirthPlanet,
            "Kimberlite on birth planet (You should enable Rare Veins first)").Value;
        _fractalOnBirthPlanet = Config.Bind("Birth", "FractalOnBirthPlanet", _fractalOnBirthPlanet,
            "Fractal silicon on birth planet (You should enable Rare Veins first)").Value;
        _organicOnBirthPlanet = Config.Bind("Birth", "OrganicOnBirthPlanet", _organicOnBirthPlanet,
            "Organic crystal on birth planet (You should enable Rare Veins first)").Value;
        _opticalOnBirthPlanet = Config.Bind("Birth", "OpticalOnBirthPlanet", _opticalOnBirthPlanet,
            "Optical grating crystal on birth planet (You should enable Rare Veins first)").Value;
        _spiniformOnBirthPlanet = Config.Bind("Birth", "SpiniformOnBirthPlanet", _spiniformOnBirthPlanet,
            "Spiniform stalagmite crystal on birth planet (You should enable Rare Veins first)").Value;
        _unipolarOnBirthPlanet = Config.Bind("Birth", "UnipolarOnBirthPlanet", _unipolarOnBirthPlanet,
            "Unipolar magnet on birth planet (You should enable Rare Veins first)").Value;
        _flatBirthPlanet = Config.Bind("Birth", "FlatBirthPlanet", _flatBirthPlanet,
            "Birth planet is solid flat (no water)").Value;
        _highLuminosityBirthStar = Config.Bind("Birth", "HighLuminosityBirthStar", _highLuminosityBirthStar,
            "Birth star has high luminosity").Value;
        _terraformAnyway = Config.Bind("General", "TerraformAnyway", _terraformAnyway,
            "Can do terraform without enough sands").Value;
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

        foreach (var idstr in _unlockTechToMaximumLevel.Split(','))
        {
            if (int.TryParse(idstr, out var id))
            {
                TechToUnlock.Add(id);
            }
        }

        if (TechToUnlock.Count > 0)
        {
            Harmony.CreateAndPatchAll(typeof(UnlockTechOnGameStart));
        }

        if (_waterPumpAnywhere)
        {
            Harmony.CreateAndPatchAll(typeof(WaterPumperCheat));
        }

        if (_sitiVeinsOnBirthPlanet || _fireIceOnBirthPlanet || _kimberliteOnBirthPlanet || _fractalOnBirthPlanet ||
            _organicOnBirthPlanet || _opticalOnBirthPlanet || _spiniformOnBirthPlanet || _unipolarOnBirthPlanet ||
            _flatBirthPlanet || _highLuminosityBirthStar)
        {
            Harmony.CreateAndPatchAll(typeof(BirthPlanetCheat));
        }

        if (_terraformAnyway)
        {
            Harmony.CreateAndPatchAll(typeof(TerraformAnyway));
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
                UIRealtimeTip.PopupAhead(
                    (lastActive ? "Developer Mode Shortcuts Disabled" : "Developer Mode Shortcuts Enabled").Translate(),
                    false);
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
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.OperandIs(99.5f))
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

    private class UnlockTechOnGameStart
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameScenarioLogic), "NotifyOnGameBegin")]
        private static void UnlockTechPatch()
        {
            var history = GameMain.history;
            if (GameMain.mainPlayer == null || GameMain.mainPlayer.mecha == null)
            {
                return;
            }

            foreach (var currentTech in TechToUnlock)
            {
                UnlockTechRecursive(history, currentTech, currentTech == 3606 ? 7000 : 10000);
            }

            var techQueue = history.techQueue;
            if (techQueue == null || techQueue.Length == 0)
            {
                return;
            }

            history.VarifyTechQueue();
            if (history.currentTech > 0 && history.currentTech != techQueue[0])
            {
                history.AlterCurrentTech(techQueue[0]);
            }
        }

        private static void UnlockTechRecursive(GameHistoryData history, int currentTech, int maxLevel = 10000)
        {
            var techStates = history.techStates;
            if (techStates == null || !techStates.ContainsKey(currentTech))
            {
                return;
            }

            var techProto = LDB.techs.Select(currentTech);
            if (techProto == null)
            {
                return;
            }

            var value = techStates[currentTech];
            var maxLvl = Math.Min(maxLevel, value.maxLevel);
            if (value.unlocked)
            {
                return;
            }

            foreach (var preid in techProto.PreTechs)
            {
                UnlockTechRecursive(history, preid, maxLevel);
            }

            var techQueue = history.techQueue;
            if (techQueue != null)
            {
                for (var i = 0; i < techQueue.Length; i++)
                {
                    if (techQueue[i] == currentTech)
                    {
                        techQueue[i] = 0;
                    }
                }
            }

            if (value.curLevel < techProto.Level) value.curLevel = techProto.Level;
            while (value.curLevel <= maxLvl)
            {
                for (var j = 0; j < techProto.UnlockFunctions.Length; j++)
                {
                    history.UnlockTechFunction(techProto.UnlockFunctions[j], techProto.UnlockValues[j], value.curLevel);
                }

                value.curLevel++;
            }

            value.unlocked = maxLvl >= value.maxLevel;
            value.curLevel = value.unlocked ? maxLvl : maxLvl + 1;
            value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
            value.hashUploaded = value.unlocked ? value.hashNeeded : 0;
            techStates[currentTech] = value;
        }
    }

    private class BirthPlanetCheat
    {
        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            var theme = LDB.themes.Select(1);
            if (_flatBirthPlanet)
            {
                theme.Algos[0] = 2;
            }

            if (_sitiVeinsOnBirthPlanet)
            {
                theme.VeinSpot[2] = 2;
                theme.VeinSpot[3] = 2;
                theme.VeinCount[2] = 0.7f;
                theme.VeinCount[3] = 0.7f;
                theme.VeinOpacity[2] = 1f;
                theme.VeinOpacity[3] = 1f;
            }

            List<int> veins = new();
            List<float> settings = new();
            if (_fireIceOnBirthPlanet)
            {
                veins.Add(8);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            if (_kimberliteOnBirthPlanet)
            {
                veins.Add(9);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            if (_fractalOnBirthPlanet)
            {
                veins.Add(10);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            if (_organicOnBirthPlanet)
            {
                veins.Add(11);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            if (_opticalOnBirthPlanet)
            {
                veins.Add(12);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            if (_spiniformOnBirthPlanet)
            {
                veins.Add(13);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            if (_unipolarOnBirthPlanet)
            {
                veins.Add(14);
                settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
            }

            theme.RareVeins = veins.ToArray();
            theme.RareSettings = settings.ToArray();
            if (_highLuminosityBirthStar)
            {
                StarGen.specifyBirthStarMass = 100f;
            }
        }
    }

    private class WaterPumperCheat
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var isFirst = true;
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4_S && instr.OperandIs(22))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_S, 0);
                        continue;
                    }
                }

                yield return instr;
            }
        }
    }

    private class TerraformAnyway
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Reform), "ReformAction")]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Patch(
            IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                var instr = list[i];
                yield return instr;
                if (instr.opcode == OpCodes.Callvirt &&
                    instr.OperandIs(AccessTools.Method(typeof(Player), "get_sandCount")))
                {
                    /* ldloc.s 6 */
                    i++;
                    instr = list[i];
                    yield return instr;
                    /* sub */
                    i++;
                    instr = list[i];
                    yield return instr;
                    /* ldc.i4.0 */
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    /* call Math.Max() */
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Math), "Max", new[] { typeof(int), typeof(int) }));
                    /* stloc.s 21 */
                    i++;
                    instr = list[i];
                    yield return instr;
                    /* skip 3 instructions:
                     *  ldloc.s 21
                     *  ldc.i4.0
                     *  blt (633)
                     */
                    i += 3;
                }
            }
        }
    }
}