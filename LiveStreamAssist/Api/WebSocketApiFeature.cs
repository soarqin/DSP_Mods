using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using BepInEx.Configuration;
using UXAssist.Common.ModFeatures;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace LiveStreamAssist.Api;

[ModFeature("WebSocketApi", Order = 51)]
internal static class WebSocketApiFeature
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<string> ListenAddress;
    internal static ConfigEntry<int> Port;

    static readonly string[] RootNames =
        { "game", "player", "history", "statistics", "galaxy", "localPlanet", "localStar" };

    static readonly Type[] RootTypes =
    {
        typeof(GameData), typeof(Player), typeof(GameHistoryData), typeof(GameStatData),
        typeof(GalaxyData), typeof(PlanetData), typeof(StarData)
    };

    static readonly object Gate = new object();
    static object _ownerToken;
    static WebSocketApiServer _server;
    static MainThreadDispatcher _dispatcher;
    static ReflectionReader _reader;
    static GameVersionSnapshot _version = GameVersionSnapshot.Empty;
    static SessionSnapshot _session = new SessionSnapshot(0, null);
    static bool _started;

    public static void BindConfig(ConfigFile config)
    {
        Enabled = config.Bind("WebSocketApi", "Enabled", false,
            "Start the WebSocket API listener when the mod starts");
        ListenAddress = config.Bind("WebSocketApi", "ListenAddress", "127.0.0.1",
            "Bind address; use a specific LAN address or 0.0.0.0 for LAN access");
        Port = config.Bind("WebSocketApi", "Port", 18080,
            new ConfigDescription("Listening port", new AcceptableValueRange<int>(1, 65535)));
    }

    public static void Init()
    {
        _reader = new ReflectionReader();
        ReflectionReader.Warn = msg => LiveStreamAssist.Logger.LogWarning(msg);
        _version = GameVersionSnapshot.Empty;
        _session = new SessionSnapshot(0, null);
    }

    public static void Start()
    {
        if (_started) return;
        if (Enabled == null || !Enabled.Value) return;
        var ctx = SynchronizationContext.Current;
        if (ctx == null || ctx.GetType().FullName != "UnityEngine.UnitySynchronizationContext")
        {
            LiveStreamAssist.Logger.LogError(
                "WebSocket API requires Unity's installed SynchronizationContext; startup aborted.");
            return;
        }

        if (!IPAddress.TryParse(ListenAddress.Value, out var address))
        {
            LiveStreamAssist.Logger.LogError($"WebSocket API listen address is invalid: {ListenAddress.Value}");
            return;
        }

        var port = Port.Value;
        if (port < 1 || port > 65535)
        {
            LiveStreamAssist.Logger.LogError($"WebSocket API port is invalid: {port}");
            return;
        }

        var token = new object();
        var dispatcher = new MainThreadDispatcher(ctx, token, ExecuteWork, () => _server?.NowMs ?? 0);
        var server = new WebSocketApiServer(address, port, PluginInfo.PLUGIN_VERSION, GetVersion, GetSession, dispatcher);
        GameLogicProc.OnDataLoaded += OnDataLoaded;
        GameLogicProc.OnGameBegin += OnGameBegin;
        GameLogicProc.OnGameEnd += OnGameEnd;
        lock (Gate)
        {
            _ownerToken = token;
            _dispatcher = dispatcher;
            _server = server;
            _started = true;
        }

        try
        {
            server.Start();
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogError($"WebSocket API failed to start: {ex}");
            Uninit();
            return;
        }

        if (VFPreload.done || VFPreload.dbDone)
            PublishVersion();
        if (IsGameReady())
            BeginSession();
    }

    public static void Uninit()
    {
        WebSocketApiServer server;
        MainThreadDispatcher dispatcher;
        lock (Gate)
        {
            if (!_started && _server == null) return;
            _started = false;
            _ownerToken = new object();
            server = _server;
            dispatcher = _dispatcher;
            _server = null;
            _dispatcher = null;
            _session = new SessionSnapshot(_session.Generation + 1, null);
        }

        GameLogicProc.OnDataLoaded -= OnDataLoaded;
        GameLogicProc.OnGameBegin -= OnGameBegin;
        GameLogicProc.OnGameEnd -= OnGameEnd;
        try { dispatcher?.Stop(); }
        catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"WebSocket API dispatcher stop failed: {ex.Message}"); }
        try { server?.Stop(); }
        catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"WebSocket API server stop failed: {ex.Message}"); }
        _reader?.ClearCache();
        ReflectionReader.Warn = null;
    }

    public static bool IsKnownRoot(string name)
    {
        for (var i = 0; i < RootNames.Length; i++)
        {
            if (string.Equals(RootNames[i], name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static GameVersionSnapshot GetVersion() => _version;
    static SessionSnapshot GetSession() => _session;

    static void OnDataLoaded() => PublishVersion();

    static void PublishVersion()
    {
        var v = GameConfig.gameVersion;
        _version = new GameVersionSnapshot(true, v.ToString(), GameConfig.build);
    }

    static void OnGameBegin()
    {
        if (IsGameReady())
            BeginSession();
    }

    static void OnGameEnd()
    {
        EndSession();
    }

    static void BeginSession()
    {
        MainThreadDispatcher dispatcher;
        int oldGeneration;
        lock (Gate)
        {
            oldGeneration = _session.Generation;
            _session = new SessionSnapshot(oldGeneration + 1, Guid.NewGuid().ToString("D"));
            dispatcher = _dispatcher;
        }

        dispatcher?.CancelQueued(_ => ApiErrors.SessionChanged());
    }

    static void EndSession()
    {
        MainThreadDispatcher dispatcher;
        lock (Gate)
        {
            _session = new SessionSnapshot(_session.Generation + 1, null);
            dispatcher = _dispatcher;
        }

        dispatcher?.CancelQueued(_ => ApiErrors.SessionChanged());
    }

    static bool IsGameReady()
    {
        return GameMain.data != null && GameMain.isRunning && !GameMain.isLoading && !DSPGame.IsMenuDemo;
    }

    static object ResolveRoot(string name)
    {
        switch (name)
        {
            case "game": return GameMain.data;
            case "player": return GameMain.mainPlayer;
            case "history": return GameMain.history;
            case "statistics": return GameMain.statistics;
            case "galaxy": return GameMain.galaxy;
            case "localPlanet": return GameMain.localPlanet;
            case "localStar": return GameMain.localStar;
            default: return null;
        }
    }

    static Type DeclaredRootType(string name)
    {
        for (var i = 0; i < RootNames.Length; i++)
        {
            if (string.Equals(RootNames[i], name, StringComparison.Ordinal))
                return RootTypes[i];
        }

        return null;
    }

    static object ExecuteWork(GameWork work)
    {
        var session = _session;
        if (work.Generation != session.Generation)
            return ApiErrors.SessionChanged();
        var call = work.Call;
        if (call.Method == DataMethod.Roots)
            return BuildRoots(session);
        if (!IsGameReady())
            return ApiErrors.GameNotReady();
        if (session.SessionId == null)
            return ApiErrors.GameNotReady();
        var instance = ResolveRoot(call.Root);
        if (instance == null)
            return ApiErrors.RootUnavailable(call.Root);
        var declared = DeclaredRootType(call.Root);
        WalkResult walked;
        if (call.Method == DataMethod.Describe)
            walked = _reader.Describe(instance, declared, call.Path);
        else
            walked = _reader.Read(instance, declared, call.Path, call.Options);
        if (walked.Error != null)
            return walked.Error;
        if (_session.Generation != work.Generation)
            return ApiErrors.SessionChanged();
        var tick = GameMain.gameTick.ToString(CultureInfo.InvariantCulture);
        if (call.Method == DataMethod.Describe)
            return BuildDescribe(walked, session.SessionId, tick);
        return BuildRead(walked, session.SessionId, tick);
    }

    static Dictionary<string, object> BuildRoots(SessionSnapshot session)
    {
        var ready = IsGameReady() && session.SessionId != null;
        var roots = new List<object>(RootNames.Length);
        for (var i = 0; i < RootNames.Length; i++)
        {
            var available = ready && ResolveRoot(RootNames[i]) != null;
            roots.Add(new Dictionary<string, object>
            {
                ["name"] = RootNames[i],
                ["type"] = ReflectionReader.TypeName(RootTypes[i]),
                ["available"] = available
            });
        }

        return new Dictionary<string, object>
        {
            ["gameReady"] = ready,
            ["sessionId"] = ready ? session.SessionId : null,
            ["gameTick"] = ready ? GameMain.gameTick.ToString(CultureInfo.InvariantCulture) : null,
            ["roots"] = roots
        };
    }

    static Dictionary<string, object> BuildDescribe(WalkResult walked, string sessionId, string tick)
    {
        var members = new List<object>(walked.Members?.Length ?? 0);
        if (walked.Members != null)
        {
            foreach (var m in walked.Members)
            {
                members.Add(new Dictionary<string, object>
                {
                    ["name"] = m.Name,
                    ["type"] = m.Type,
                    ["kind"] = m.Kind
                });
            }
        }

        object collection = null;
        if (walked.Collection != null)
        {
            collection = new Dictionary<string, object>
            {
                ["kind"] = walked.Collection.Kind,
                ["elementType"] = walked.Collection.ElementType,
                ["keyType"] = walked.Collection.KeyType,
                ["count"] = walked.Collection.Count
            };
        }

        return new Dictionary<string, object>
        {
            ["type"] = walked.TypeName,
            ["isNull"] = walked.IsNull,
            ["members"] = members,
            ["collection"] = collection,
            ["sessionId"] = sessionId,
            ["gameTick"] = tick
        };
    }

    static Dictionary<string, object> BuildRead(WalkResult walked, string sessionId, string tick)
    {
        return new Dictionary<string, object>
        {
            ["type"] = walked.TypeName,
            ["value"] = walked.Encoded,
            ["sessionId"] = sessionId,
            ["gameTick"] = tick
        };
    }
}
