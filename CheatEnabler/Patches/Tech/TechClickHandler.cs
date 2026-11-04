using System;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Entry point injected into UITechNode.OnPointerDown, shared by upgrade and downgrade clicks.
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
            CheatEnabler.Logger.LogWarning($"[TechTweaks] click handling failed: {e}");
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

        if (!TryDecodeModifiers(out int steps))
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

    private static bool TryDecodeModifiers(out int steps)
    {
        steps = 0;

        if (VFInput.shift)
        {
            if (VFInput.alt) return false;
            steps = VFInput.control ? 100 : 1;
            return true;
        }

        if (VFInput.control)
        {
            if (VFInput.alt) return false;
            steps = 10;
            return true;
        }

        if (VFInput.alt)
        {
            // Alt applies or reverts every level available to this click.
            steps = int.MaxValue;
            return true;
        }

        return false;
    }
}
