using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NLua;
using OBSWebsocketDotNet;

namespace LuaScriptEngine;

public class LuaState: IDisposable
{
    private readonly Lua state = new();
    private readonly List<LuaFunction> PostDataLoadedFuncs = [];
    private readonly List<LuaFunction> PreUpdateFuncs = [];
    private readonly List<LuaFunction> PostUpdateFuncs = [];
    private readonly List<LuaFunction> PreGameBeginFuncs = [];
    private readonly List<LuaFunction> PostGameBeginFuncs = [];
    private readonly List<LuaFunction> PreGameEndFuncs = [];
    private readonly List<LuaFunction> PostGameEndFuncs = [];
    private readonly HashSet<Timer> Timers = [];
    private readonly List<Timer> TimersToRemove = [];
    private readonly OBSWebsocket _obs = new();
    private readonly Dictionary<string, string> _scheduledText = [];

    private long _lastSeedKey = 0L;
    private string _lastClusterString = "";

    public LuaState()
    {
        var assemblyPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "scripts"
        );

        state.State.Encoding = Encoding.UTF8;
        state.LoadCLRPackage();
        state.DoString("import('Assembly-CSharp')");
        state.DoString($"package.path = '{assemblyPath.Replace('\\', '/')}/?.lua'");
        RegisterFunctions();

        foreach (var file in System.IO.Directory.GetFiles(assemblyPath, "*.lua"))
        {
            LuaScriptEngine.Logger.LogInfo($"Loading Lua script: {file}");
            state.DoFile(file);
        }
    }

    public void Dispose()
    {
        state.Dispose();
        if (_obs.IsConnected)
        {
            _obs.Disconnect();
        }
        _scheduledText.Clear();
        Timers.Clear();
        PreUpdateFuncs.Clear();
        PostUpdateFuncs.Clear();
        PreGameBeginFuncs.Clear();
        PostGameBeginFuncs.Clear();
        PreGameEndFuncs.Clear();
        PostGameEndFuncs.Clear();
    }

    private void RegisterFunctions()
    {
        #region Callback Functions
        state["register_callback"] = (string tp, LuaFunction action) =>
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

        state["add_timer"] = Timer (LuaFunction func, long firstInterval, long repeatInterval) =>
        {
            var timer = new Timer(func, firstInterval, repeatInterval);
            Timers.Add(timer);
            return timer;
        };

        state["remove_timer"] = void (Timer timer) => { Timers.Remove(timer); };
        #endregion

        #region OBS Functions
        state["obs_connect"] = void (string server, string password) =>
        {
            if (_obs.IsConnected)
            {
                LuaScriptEngine.Logger.LogInfo("Already connected to OBS");
                return;
            }
            _obs.Connected += (_, _) =>
            {
                LuaScriptEngine.Logger.LogInfo($"Connected to OBS at {server}");
                foreach (var (sourceName, text) in _scheduledText)
                {
                    try
                    {
                        _obs.SetInputSettings(sourceName,
                        new JObject
                        {
                            { "text", text }
                        });
                    }
                    catch (Exception e)
                    {
                        LuaScriptEngine.Logger.LogError($"Error setting {sourceName}'s text to `{text}`: {e}");
                    }
                }

                _scheduledText.Clear();
            };
            _obs.Disconnected += (_, _) =>
            {
                _obs.ConnectAsync(server, password);
            };
            _obs.ConnectAsync(server, password);
        };

        state["obs_disconnect"] = void () =>
        {
            _obs.Disconnect();
        };

        state["obs_set_source_text"] = void (string sourceName, string text) =>
        {
            if (_obs.IsConnected)
            {
                try
                {
                    _obs.SetInputSettings(sourceName, new JObject
                    {
                        { "text", text }
                    });
                }
                catch (Exception e)
                {
                    LuaScriptEngine.Logger.LogError($"Error setting source text: {e}");
                    _obs.Disconnect();
                    _scheduledText[sourceName] = text;
                }
            }
            else
            {
                _scheduledText[sourceName] = text;
            }
        };
        #endregion

        #region Common Data Retrieval Functions
        state["get_current_cluster"] = string () =>
        {
            var data = GameMain.data;
            if (data == null) return "";
            var seedKey = data.GetClusterSeedKey();
            if (seedKey == _lastSeedKey) return _lastClusterString;
            _lastSeedKey = seedKey;
            _lastClusterString = data.gameDesc.clusterString;
            return _lastClusterString;
        };

        var getCurrentResearch = () =>
        {
            var history = GameMain.history;
            if (history == null) return (0, 0, 0L, 0L);
            var current = history.currentTech;
            if (current == 0) return (0, 0, 0L, 0L);
            var techStates = history.techStates;
            if (!techStates.TryGetValue(current, out var value)) return (0, 0, 0L, 0L);
            return (current, value.curLevel, value.hashUploaded, value.hashNeeded);
        };
        state["get_current_research"] = getCurrentResearch;
        state["get_current_research_str"] = string (string format) =>
        {
            var (current, level, hashUploaded, hashNeeded) = getCurrentResearch();
            if (current == 0) return "";
            return string.Format(format, LDB.techs.Select(current).name, level, JournalUtility.TranslateKMGValue(hashUploaded), JournalUtility.TranslateKMGValue(hashNeeded));
        };

        var getTechLevel = (NLua.LuaTable techIds) =>
        {
            var techStates = GameMain.history?.techStates;
            if (techStates == null) return 0;
            int level = 0;
            foreach (var techId in techIds.Values.Cast<long>())
            {
                if (!techStates.TryGetValue((int)techId, out var value))
                {
                    return level;
                }
                var newLevel = value.unlocked ? value.curLevel : value.curLevel - 1;
                if (newLevel > level)
                {
                    level = newLevel;
                }
                if (!value.unlocked)
                {
                    return level;
                }
            }
            return level;
        };
        state["get_tech_level"] = getTechLevel;
        state["get_tech_level_str"] = string (string format, NLua.LuaTable techIds) =>
        {
            var level = getTechLevel(techIds);
            if (level <= 0) return "";
            return string.Format(format, level);
        };

        var getFactoryStat = (int itemId) =>
        {
            var gameData = GameMain.data;
            if (gameData == null) return (0, 0);
            var statPool = GameMain.statistics?.production.factoryStatPool;
            if (statPool == null) return (0, 0);
            long productTotal = 0L, consumeTotal = 0L;
            for (var i = gameData.factoryCount - 1; i >= 0; i--)
            {
                var stat = statPool[i];
                if (stat == null) continue;
                var index = stat.productIndices[itemId];
                var ppool = stat.productPool[index];
                if (ppool == null) continue;
                var cursor = ppool.cursor[4];
                if (cursor > 0)
                {
                    productTotal += ppool.count[cursor - 1];
                }
                cursor = ppool.cursor[4 + 6];
                if (cursor > 0)
                {
                    consumeTotal += ppool.count[cursor - 1];
                }
            }
            return (productTotal, consumeTotal);
        };
        state["get_factory_stat"] = getFactoryStat;
        state["get_factory_stat_str"] = string (string format, int itemId) =>
        {
            var (productTotal, consumeTotal) = getFactoryStat(itemId);
            return string.Format(format, JournalUtility.TranslateKMGValue(productTotal), JournalUtility.TranslateKMGValue(consumeTotal));
        };

        var getDysonSphereTotalGen = () =>
        {
            var data = GameMain.data;
            if (data == null) return 0;
            return data.GetDysonSphereTotalGen();
        };
        state["get_dyson_sphere_power_gen"] = getDysonSphereTotalGen;
        state["get_dyson_sphere_power_gen_str"] = string (string format) =>
        {
            var totalGen = getDysonSphereTotalGen();
            if (totalGen <= 0) return "";
            return string.Format(format, JournalUtility.TranslateKMGValue(totalGen * 60L));
        };
        #endregion
    }

    public void PostDataLoaded()
    {
        LoopCall(PostDataLoadedFuncs);
    }

    public void PreUpdate()
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

    public void PostUpdate()
    {
        LoopCall(PostUpdateFuncs);
    }

    public void PreGameBegin()
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

    public void PostGameBegin()
    {
        LoopCall(PostGameBeginFuncs);
    }

    public void PreGameEnd()
    {
        LoopCall(PreGameEndFuncs);
    }

    public void PostGameEnd()
    {
        LoopCall(PostGameEndFuncs);
    }

    private void LoopCall(List<LuaFunction> funcs)
    {
        foreach (var func in funcs)
        {
            try
            {
                func.Call();
            }
            catch (Exception e)
            {
                LuaScriptEngine.Logger.LogError($"Error in Lua script: {e}");
            }
        }
    }

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
                LuaScriptEngine.Logger.LogError($"Error in Lua script: {e}");
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
}
