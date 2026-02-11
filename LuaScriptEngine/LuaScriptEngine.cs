using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using NLua;

namespace LuaScriptEngine;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LuaScriptEngine : BaseUnityPlugin
{
    public new static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private Harmony _harmony;

    private static readonly LuaState State = new();

    private void Awake()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(Patches));
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        State.Dispose();
    }

    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            State.PostDataLoaded();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void GameMain_FixedUpdate_Prefix()
        {
            State.PreUpdate();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void GameMain_FixedUpdate_Postfix()
        {
            State.PostUpdate();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Prefix()
        {
            State.PreGameBegin();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Postfix()
        {
            State.PostGameBegin();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        private static void GameMain_End_Prefix()
        {
            State.PreGameEnd();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        private static void GameMain_End_Postfix()
        {
            State.PostGameEnd();
        }
    }
}