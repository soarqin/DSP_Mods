using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Help button for the tech / upgrade window.
///
/// The button is created once under the <c>UITechTree</c> window itself, so it inherits the window's
/// lifetime and disappears with it.
///
/// It is anchored directly under the "Tech" tab caption in the top-left corner
/// (<c>UITechTree.tabButtonText0</c>). It was originally placed in the bottom-left corner, but the
/// tooltip is tall and the game's bottom HUD bar clipped it there.
///
/// The button clones the game's own tip-button prefab used by the sandbox settings
/// (<c>galaxySelect.sandboxToggle</c> parent / <c>tip-button</c>), which is what UXAssist's
/// <c>LayoutHelper.AddTipsButton</c> does as well. The prefab's <c>UIButton.TipSettings</c> carries
/// the tip prefab plumbing and tip delay, so it is saved before the button state is normalised and
/// restored afterwards - dropping it would produce a button without a tooltip.
/// </summary>
internal static class TechTreeUI
{
    private const float HelpButtonGap = 4f;
    private const float HelpButtonLeftFallback = 24f;
    private const float HelpButtonBottomFallback = 24f;

    private static RectTransform _root;
    private static UIButton _helpButton;

    public static void OnTechTreeOpened(UITechTree window)
    {
        EnsureCreated(window);
    }

    /// <summary>
    /// The window's whole GameObject is destroyed when it is freed, taking our elements with it,
    /// so the cached references only have to be dropped.
    /// </summary>
    public static void OnTechTreeFreed()
    {
        _root = null;
        _helpButton = null;
    }

    /// <summary>
    /// Drops the created elements. The window's own GameObject may still be alive here (the feature was
    /// switched off while the tech tree was open), so the root is destroyed explicitly instead of only
    /// clearing the cached references.
    /// </summary>
    public static void Reset()
    {
        if (_root != null)
        {
            Object.Destroy(_root.gameObject);
        }

        _root = null;
        _helpButton = null;
    }

    private static void EnsureCreated(UITechTree window)
    {
        if (_root != null || window == null)
        {
            return;
        }

        if (!(window.transform is RectTransform windowRect))
        {
            TechLog.Warn("tech tree window has no RectTransform, help button was not created");
            return;
        }

        var rootGo = new GameObject("TechTweaks-UI", typeof(RectTransform));
        _root = (RectTransform)rootGo.transform;
        _root.SetParent(windowRect, false);
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.anchoredPosition3D = Vector3.zero;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;
        _root.SetAsLastSibling();

        _helpButton = CreateHelpButton(window, _root);
    }

    private static UIButton CreateHelpButton(UITechTree window, RectTransform root)
    {
        Transform source = FindTipsButtonPrefab();
        if (source == null)
        {
            TechLog.Warn("native tip-button prefab not found, tech tree help button was not created");
            return null;
        }

        GameObject clone = Object.Instantiate(source.gameObject);
        clone.name = "TechTweaks-HelpButton";
        var button = clone.GetComponent<UIButton>();
        if (button == null)
        {
            TechLog.Warn("tip-button prefab has no UIButton, tech tree help button was not created");
            Object.Destroy(clone);
            return null;
        }

        // Keep the prefab's tip plumbing (simpleGeneralTipPrefab, level, delay, corner, width)
        // before the state reset wipes it out.
        UIButton.TipSettings tipSettings = button.tips;
        button.Init();
        button.enabled = true;
        button.highlighted = false;
        button.updating = true;
        button.data = 0;
        if (button.button != null)
        {
            button.button.onClick.RemoveAllListeners();
            button.button.enabled = true;
            button.button.interactable = true;
        }

        button.MReset();
        button.tips = tipSettings;
        button.tips.topLevel = true;
        // corner selects which corner of the button the tooltip hangs off (3 = below-right, see
        // UISimpleGeneralTip.SetTip). The prefab defaults to a vertically centred placement, which
        // pushes a tall tooltip off the top of the screen once the button sits in the header area.
        button.tips.corner = 3;
        // Same title and body as the config panel's tooltip button, so the two read identically.
        button.tips.tipTitle = Localization.UnlockDowngradeTechWithKeyModifiers.Translate();
        button.tips.tipText = Localization.TechModifierHelpBody.Translate();

        var rect = (RectTransform)clone.transform;
        PlaceHelpButton(window, rect, root);
        button.UpdateTip();
        clone.SetActive(true);
        return button;
    }

    /// <summary>
    /// Puts the button right under the "Tech" tab caption in the window's top-left corner, reusing
    /// that caption's anchors and pivot so the offset means the same thing at any resolution. Falls
    /// back to the bottom-left corner if the caption is not available.
    /// </summary>
    private static void PlaceHelpButton(UITechTree window, RectTransform rect, RectTransform root)
    {
        Text caption = window.tabButtonText0;
        RectTransform captionRect = caption != null ? caption.rectTransform : null;

        if (captionRect != null && captionRect.parent != null)
        {
            rect.SetParent(captionRect.parent, false);
            rect.anchorMin = captionRect.anchorMin;
            rect.anchorMax = captionRect.anchorMax;
            rect.pivot = captionRect.pivot;
            Vector3 position = captionRect.anchoredPosition3D;
            rect.anchoredPosition3D = new Vector3(
                position.x,
                position.y - captionRect.rect.height - HelpButtonGap,
                position.z);
            rect.localScale = Vector3.one;
            return;
        }

        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition3D = new Vector3(HelpButtonLeftFallback, HelpButtonBottomFallback, 0f);
        rect.localScale = Vector3.one;
    }

    private static Transform FindTipsButtonPrefab()
    {
        UIGalaxySelect galaxySelect = UIRoot.instance != null ? UIRoot.instance.galaxySelect : null;
        Transform sandboxToggle = galaxySelect?.sandboxToggle != null
            ? galaxySelect.sandboxToggle.transform
            : null;
        Transform container = sandboxToggle != null ? sandboxToggle.parent : null;
        return container != null ? container.Find("tip-button") : null;
    }
}
