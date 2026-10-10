using System;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Entry point injected into <c>UITechNode.OnPointerDown</c>. Decides between "do nothing", "hand over
/// to the built-in UnlockTech feature", "upgrade" and "downgrade".
/// </summary>
internal static class TechClickHandler
{
    public static void OnClickTech(UITechNode node)
    {
        // The handler runs inside the game's own click path, so nothing may escape from here and break
        // the tech tree UI.
        try
        {
            HandleClick(node);
        }
        catch (Exception e)
        {
            TechLog.Warn($"click handling failed: {e}");
        }
    }

    private static void HandleClick(UITechNode node)
    {
        if (node == null || TechTweaksPatch.Enabled == null || !TechTweaksPatch.Enabled.Value)
        {
            return;
        }

        TechProto techProto = node.techProto;
        if (techProto == null)
        {
            return;
        }

        GameHistoryData history = GameMain.history;
        if (history == null || history.techStates == null)
        {
            return;
        }

        if (!TechKeyCombo.TryDecode(out int steps))
        {
            return;
        }

        if (DowngradeMode.IsOn)
        {
            TechDowngrade.Run(history, techProto, steps);
        }
        else
        {
            TechUnlock.Run(history, techProto, steps);
        }
    }
}
