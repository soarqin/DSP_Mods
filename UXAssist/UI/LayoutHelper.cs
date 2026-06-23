using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UXAssist.Common;

namespace UXAssist.UI;

public static class LayoutHelper
{
    private static void AddElement(float x, float y, RectTransform rect, RectTransform parent = null)
    {
        if (rect != null)
        {
            Util.NormalizeRectWithTopLeft(rect, x, y, parent);
        }
    }

    public static Text AddText(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")
    {
        var src = UIRoot.instance.uiGame.assemblerWindow.stateText;
        var txt = UnityEngine.Object.Instantiate(src);
        txt.gameObject.name = objName;
        txt.text = label.Translate();
        txt.color = new Color(1f, 1f, 1f, 0.4f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.fontSize = fontSize;
        txt.rectTransform.sizeDelta = new Vector2(txt.preferredWidth + 8f, txt.preferredHeight + 8f);
        AddElement(x, y, txt.rectTransform, parent);
        return txt;
    }

    public static UIButton AddTipsButton(float x, float y, RectTransform parent, string label, string tip, string content, string objName = "tips-button")
    {
        var src = UIRoot.instance.galaxySelect.sandboxToggle.gameObject.transform.parent.Find("tip-button");
        var dst = UnityEngine.Object.Instantiate(src);
        dst.gameObject.name = objName;
        var btn = dst.GetComponent<UIButton>();
        Util.NormalizeRectWithTopLeft(btn, x, y, parent);
        btn.tips.topLevel = true;
        btn.tips.tipTitle = label;
        btn.tips.tipText = tip;
        btn.UpdateTip();
        return btn;
    }

    public static UIButton AddButton(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)
    {
        return AddButton(x, y, 150f, parent, text, fontSize, objName, onClick);
    }

    public static UIButton AddButton(float x, float y, float width, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)
    {
        var panel = UIRoot.instance.uiGame.statWindow.performancePanelUI;
        var btn = UnityEngine.Object.Instantiate(panel.cpuActiveButton);
        btn.gameObject.name = objName;
        var rect = Util.NormalizeRectWithTopLeft(btn, x, y, parent);
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        var l = btn.gameObject.transform.Find("button-text").GetComponent<Localizer>();
        var t = btn.gameObject.transform.Find("button-text").GetComponent<Text>();
        if (l != null)
        {
            l.stringKey = text;
            l.translation = text.Translate();
        }

        if (t != null)
        {
            t.text = text.Translate();
        }

        t.fontSize = fontSize;
        btn.tip = null;
        btn.tips = new UIButton.TipSettings();
        btn.button.onClick.RemoveAllListeners();
        if (onClick != null) btn.button.onClick.AddListener(onClick);

        return btn;
    }
}
