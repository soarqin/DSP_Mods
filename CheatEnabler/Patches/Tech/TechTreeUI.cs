using UnityEngine;
using UnityEngine.UI;
using UIUtil = UXAssist.UI.Util;

namespace CheatEnabler.Patches.Tech;

/// <summary>Owns the cloned native help button, including its lifetime and localized tooltip.</summary>
internal static class TechTreeUI
{
    private static UITechTree _window;
    private static UIButton _helpButton;

    public static void OnTechTreeOpened(UITechTree window)
    {
        if (window == null) return;
        if (_window != window) Reset();
        _window = window;
        if (_helpButton != null) return;

        Transform source = UIRoot.instance?.galaxySelect?.sandboxToggle?.transform.parent?.Find("tip-button");
        if (source == null) return;

        GameObject clone = Object.Instantiate(source.gameObject, window.transform, false);
        clone.name = "TechTweaks-HelpButton";
        _helpButton = clone.GetComponent<UIButton>();
        if (_helpButton == null)
        {
            Object.Destroy(clone);
            return;
        }

        // Reset cloned runtime state while retaining the native tooltip prefab and timing.
        UIButton.TipSettings tipSettings = _helpButton.tips;
        _helpButton.button?.onClick.RemoveAllListeners();
        UIUtil.ResetButton(_helpButton);
        _helpButton.tips = tipSettings;
        _helpButton.tips.topLevel = true;
        _helpButton.tips.corner = 3;

        var rect = (RectTransform)clone.transform;
        Text caption = window.tabButtonText0;
        if (caption != null)
        {
            RectTransform captionRect = caption.rectTransform;
            rect.SetParent(captionRect.parent, false);
            rect.anchorMin = captionRect.anchorMin;
            rect.anchorMax = captionRect.anchorMax;
            rect.pivot = captionRect.pivot;
            rect.anchoredPosition3D = captionRect.anchoredPosition3D -
                new Vector3(0f, captionRect.rect.height + 4f, 0f);
        }
        else
        {
            UIUtil.NormalizeRectWithBottomLeft(rect, 24f, 24f, window.transform);
        }

        rect.localScale = Vector3.one;
        RefreshText();
        clone.SetActive(true);
    }

    public static void RefreshText()
    {
        if (_helpButton == null) return;
        _helpButton.tips.tipTitle = Localization.UnlockDowngradeTechWithKeyModifiers.Translate();
        _helpButton.tips.tipText = Localization.TechModifierHelpBody.Translate();
        _helpButton.UpdateTip();
    }

    public static void Reset()
    {
        if (_helpButton != null)
        {
            _helpButton.gameObject.SetActive(false);
            Object.Destroy(_helpButton.gameObject);
        }

        _window = null;
        _helpButton = null;
    }
}
