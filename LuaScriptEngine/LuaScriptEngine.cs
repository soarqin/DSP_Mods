using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NLua;

namespace LuaScriptEngine;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LuaScriptEngine : BaseUnityPlugin
{
    private class Timer
    {
        public Timer(LuaFunction func, long startInterval, long repeatInterval = 0L)
        {
            _func = func;
            _repeatInterval = repeatInterval;
            _nextTick = GameMain.gameTick + startInterval;
        }

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

        private readonly LuaFunction _func;
        private readonly long _repeatInterval;
        private long _nextTick;
    }
    public new static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private Harmony _harmony;

    private static readonly Lua LuaState = new();
    private static readonly List<LuaFunction> PreUpdateFuncs = [];
    private static readonly List<LuaFunction> PostUpdateFuncs = [];
    private static readonly List<LuaFunction> PreGameBeginFuncs = [];
    private static readonly List<LuaFunction> PostGameBeginFuncs = [];
    private static readonly List<LuaFunction> PreGameEndFuncs = [];
    private static readonly List<LuaFunction> PostGameEndFuncs = [];
    private static readonly List<Timer> Timers = [];
    private static readonly List<int> RemovedTimers = [];

    private void Awake()
    {
        LuaState.LoadCLRPackage();
        LuaState.DoString("import('Assembly-CSharp')");
        LuaState["register_callback"] = (string tp, bool pre, LuaFunction action) =>
        {
            switch (tp)
            {
                case "update":
                    (pre ? PreUpdateFuncs : PostUpdateFuncs).Add(action);
                    break;
                case "game_begin":
                    (pre ? PreGameBeginFuncs : PostGameBeginFuncs).Add(action);
                    break;
                case "game_end":
                    (pre ? PreGameEndFuncs : PostGameEndFuncs).Add(action);
                    break;
            }
        };
        LuaState["add_timer"] = int(LuaFunction func, long firstInterval, long repeatInterval) =>
        {
            var timer = new Timer(func, firstInterval, repeatInterval);
            if (RemovedTimers.Count <= 0)
            {
                Timers.Add(timer);
                return Timers.Count - 1;
            }
            var index = RemovedTimers[RemovedTimers.Count - 1];
            Timers[index] = timer;
            RemovedTimers.RemoveAt(RemovedTimers.Count - 1);
            return index;
        };
        LuaState["remove_timer"] = void (int index) =>
        {
            if (index < 0 || index >= Timers.Count) return;
            Timers[index] = null;
            RemovedTimers.Add(index);
        };

        var assemblyPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "scripts"
        );

        foreach (var file in System.IO.Directory.GetFiles(assemblyPath, "*.lua"))
        {
            Logger.LogInfo($"Loading Lua script: {file}");
            LuaState.DoFile(file);
        }
        _harmony = Harmony.CreateAndPatchAll(typeof(Patches));
    }
    
    private void OnDestroy()
    {
        RemovedTimers.Clear();
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void GameMain_FixedUpdate_Prefix()
        {
            if (Timers.Count > 0)
            {
                var gameTick = GameMain.gameTick;
                for (var index = Timers.Count - 1; index >= 0; index--)
                {
                    var timer = Timers[index];
                    if (timer == null || !timer.Check(gameTick)) continue;
                    Timers[index] = null;
                    RemovedTimers.Add(index);
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