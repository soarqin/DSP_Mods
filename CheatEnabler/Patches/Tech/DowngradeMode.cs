using UnityEngine;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Session level downgrade mode, toggled with <c>Caps Lock</c>.
///
/// The flag is intentionally not persisted: it is a per-session switch, so a game restart always comes
/// back in the normal (upgrade) mode. The click handler reads <see cref="IsOn"/> at click time, so
/// whatever the mode is at that moment wins.
///
/// Toggling reports itself exactly like the other CheatEnabler toggles (no condition build / no
/// collision) do: a realtime tip ahead popup, plus an entry in the critical warning banner. The banner
/// entry outranks the other two so the mode cannot be missed while they are on as well.
/// </summary>
internal static class DowngradeMode
{
    public static bool IsOn { get; private set; }

    /// <summary>Per-frame <c>Caps Lock</c> poll; driven by the mod feature registry input hook.</summary>
    public static void Update()
    {
        if (TechTweaksPatch.Enabled == null || !TechTweaksPatch.Enabled.Value)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.CapsLock))
        {
            Toggle();
        }
    }

    public static void Toggle()
    {
        IsOn = !IsOn;

        if (!DSPGame.IsMenuDemo && GameMain.isRunning)
        {
            UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead(
                (IsOn ? Localization.TechDowngradeModeOn : Localization.TechDowngradeModeOff).Translate());
            GameMain.data?.warningSystem?.UpdateCriticalWarningText();
        }
    }

    public static void Reset()
    {
        IsOn = false;
    }
}
