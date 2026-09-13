using System.Reflection;
using BepInEx;
using CommonAPI.Systems;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace LiveStreamAssist;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(UXAssist.PluginInfo.PLUGIN_GUID)]
public class LiveStreamAssist : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private void Awake()
    {
        I18N.Init();
        I18N.Add("KEYToggleLiveStreamAssist", "[LSA] Toggle live-stream statistics assist", "[LSA] \u5207\u6362\u76f4\u64ad\u7edf\u8ba1\u8f85\u52a9");
        ModFeatureRegistry.Discover(Assembly.GetExecutingAssembly());
        I18N.Apply();
    }
}

[ModFeature("LiveStreamAssist", Order = 50)]
internal static class LiveStreamAssistFeature
{
    private const float MinIntervalSeconds = 15f;
    private const float MaxIntervalSeconds = 30f;
    private static PressKeyBind _toggleKey;
    private static bool _enabled;
    private static float _nextSwitchAt;

    public static void Init()
    {
        _toggleKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.F8, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING |
                             KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.UI | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleLiveStreamAssist",
            canOverride = true
        });
    }

    public static void Start()
    {
        GameLogicProc.OnGameEnd += Stop;
    }

    public static void Uninit()
    {
        GameLogicProc.OnGameEnd -= Stop;
        Stop();
    }

    public static void OnInputUpdate()
    {
        if (!_toggleKey.keyValue) return;

        if (_enabled)
        {
            Stop();
            return;
        }

        TryStart();
    }

    public static void OnUpdate()
    {
        if (!_enabled) return;

        var statWindow = GetStatWindow();
        if (statWindow == null || !statWindow.active)
        {
            Stop();
            return;
        }

        if (Time.unscaledTime < _nextSwitchAt) return;

        var nextTab = statWindow.tabIndex == 1 ? 4 : 1;
        statWindow.OnTabButtonClick(nextTab);
        ScheduleNextSwitch();
    }

    private static void TryStart()
    {
        var statWindow = GetStatWindow();
        if (statWindow == null || !GameMain.isRunning) return;

        UIRoot.instance.uiGame.OpenProductionWindow();
        statWindow = GetStatWindow();
        if (statWindow == null || !statWindow.active) return;

        statWindow.OnTabButtonClick(1);
        _enabled = true;
        ScheduleNextSwitch();
    }

    private static UIStatisticsWindow GetStatWindow()
    {
        if (UIRoot.instance == null || UIRoot.instance.uiGame == null) return null;
        return UIRoot.instance.uiGame.statWindow;
    }

    private static void ScheduleNextSwitch()
    {
        _nextSwitchAt = Time.unscaledTime + Random.Range(MinIntervalSeconds, MaxIntervalSeconds);
    }

    private static void Stop()
    {
        _enabled = false;
        _nextSwitchAt = 0f;
    }
}
