using System;
using System.Collections.Generic;
using System.Text;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using Newtonsoft.Json.Linq;
using NLua;
using OBSWebsocketDotNet;

namespace LuaScriptEngine;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LuaScriptEngine : BaseUnityPlugin
{
    private readonly OBSWebsocket _obs = new();
    private readonly Dictionary<string, string> _scheduledText = [];

    private class Timer(LuaFunction func, long startInterval, long repeatInterval = 0L)
    {
        public bool Check(long gameTick)
        {
            if (gameTick < _nextTick) return false;
            try
            {
                _func.Call();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error in Lua script: {e}");
            }

            if (_repeatInterval <= 0L) return true;
            _nextTick += _repeatInterval;
            if (_nextTick < gameTick)
                _nextTick = gameTick + 1;
            return false;
        }

        public bool Reset(long gameTick)
        {
            if (_repeatInterval <= 0L) return true;
            _nextTick = gameTick + _repeatInterval;
            return false;
        }

        private readonly LuaFunction _func = func;
        private readonly long _repeatInterval = repeatInterval;
        private long _nextTick = GameMain.gameTick + startInterval;
    }
    public new static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private Harmony _harmony;

    private static readonly Lua LuaState = new();
    private static readonly List<LuaFunction> PostDataLoadedFuncs = [];
    private static readonly List<LuaFunction> PreUpdateFuncs = [];
    private static readonly List<LuaFunction> PostUpdateFuncs = [];
    private static readonly List<LuaFunction> PreGameBeginFuncs = [];
    private static readonly List<LuaFunction> PostGameBeginFuncs = [];
    private static readonly List<LuaFunction> PreGameEndFuncs = [];
    private static readonly List<LuaFunction> PostGameEndFuncs = [];
    private static readonly HashSet<Timer> Timers = [];
    private static readonly List<Timer> TimersToRemove = [];

    private void Awake()
    {
        LuaState.State.Encoding = Encoding.UTF8;
        LuaState.LoadCLRPackage();
        LuaState.DoString("import('Assembly-CSharp')");
        LuaState["register_callback"] = (string tp, LuaFunction action) =>
        {
            switch (tp)
            {
                case "data_loaded":
                    PostDataLoadedFuncs.Add(action);
                    break;
                case "pre_update":
                    PreUpdateFuncs.Add(action);
                    break;
                case "post_update":
                    PostUpdateFuncs.Add(action);
                    break;
                case "pre_game_begin":
                    PreGameBeginFuncs.Add(action);
                    break;
                case "post_game_begin":
                    PostGameBeginFuncs.Add(action);
                    break;
                case "pre_game_end":
                    PreGameEndFuncs.Add(action);
                    break;
                case "post_game_end":
                    PostGameEndFuncs.Add(action);
                    break;
            }
        };
        LuaState["add_timer"] = Timer(LuaFunction func, long firstInterval, long repeatInterval) =>
        {
            var timer = new Timer(func, firstInterval, repeatInterval);
            Timers.Add(timer);
            return timer;
        };
        LuaState["remove_timer"] = void (Timer timer) =>
        {
            Timers.Remove(timer);
        };
        LuaState["obs_connect"] = void (string server, string password) =>
        {
            _obs.Connected += (sender, e) =>
            {
                Logger.LogDebug("Connected to OBS");
                foreach (var (sourceName, text) in _scheduledText)
                {
                    _obs.SetInputSettings(sourceName, 
                        new JObject {
                            {"text", text}
                        });
                }
                _scheduledText.Clear();
            };
            _obs.Disconnected += (sender, e) =>
            {
                Logger.LogDebug("Disconnected from OBS");
                _obs.ConnectAsync(server, password);
            };
            _obs.ConnectAsync(server, password);
        };
        LuaState["obs_set_source_text"] = void (string sourceName, string text) =>
        {
            if (_obs.IsConnected)
            {
                try
                {
                    _obs.SetInputSettings(sourceName, new JObject {
                        {"text", text}
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error setting source text: {e}");
                    _obs.Disconnect();
                    _scheduledText[sourceName] = text;
                }
            }
            else
            {
                _scheduledText[sourceName] = text;
            }
        };
        var assemblyPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "scripts"
        );
        LuaState.DoString($"package.path = '{assemblyPath.Replace('\\', '/')}/?.lua'");

        foreach (var file in System.IO.Directory.GetFiles(assemblyPath, "*.lua"))
        {
            Logger.LogInfo($"Loading Lua script: {file}");
            LuaState.DoFile(file);
        }
        _harmony = Harmony.CreateAndPatchAll(typeof(Patches));
    }
    
    private void OnDestroy()
    {
        Timers.Clear();
        PreUpdateFuncs.Clear();
        PostUpdateFuncs.Clear();
        PreGameBeginFuncs.Clear();
        PostGameBeginFuncs.Clear();
        PreGameEndFuncs.Clear();
        PostGameEndFuncs.Clear();

        _harmony?.UnpatchSelf();
        LuaState.Dispose();
    }

    private static class Patches
    {
        private static void LoopCall(List<LuaFunction> funcs)
        {
            foreach (var func in funcs)
            {
                try
                {
                    func.Call();
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error in Lua script: {e}");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            LoopCall(PostDataLoadedFuncs);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void GameMain_FixedUpdate_Prefix()
        {
            if (Timers.Count > 0)
            {
                var gameTick = GameMain.gameTick;
                foreach (var timer in Timers)
                {
                    if (timer == null || !timer.Check(gameTick)) continue;
                    TimersToRemove.Add(timer);
                }
                if (TimersToRemove.Count > 0)
                {
                    foreach (var timer in TimersToRemove)
                    {
                        Timers.Remove(timer);
                    }
                    TimersToRemove.Clear();
                }
            }

            LoopCall(PreUpdateFuncs);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void GameMain_FixedUpdate_Postfix()
        {
            LoopCall(PostUpdateFuncs);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Prefix()
        {
            var tick = GameMain.gameTick;
            foreach (var timer in Timers)
            {
                if (timer.Reset(tick))
                {
                    TimersToRemove.Add(timer);
                }
            }
            if (TimersToRemove.Count > 0)
            {
                foreach (var timer in TimersToRemove)
                {
                    Timers.Remove(timer);
                }
                TimersToRemove.Clear();
            }
            LoopCall(PreGameBeginFuncs);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Postfix()
        {
            LoopCall(PostGameBeginFuncs);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        private static void GameMain_End_Prefix()
        {
            LoopCall(PreGameEndFuncs);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        private static void GameMain_End_Postfix()
        {
            LoopCall(PostGameEndFuncs);
        }
    }
}