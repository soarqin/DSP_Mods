using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheatEnabler.UI;

// MyWindow modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyWindowCtl.cs

public class MyWindow: ManualBehaviour
{
    private readonly Dictionary<InputField, Tuple<UnityAction<string>, UnityAction<string>>> _inputFields = new();
    protected bool EventRegistered { get; private set; }

    public virtual void TryClose()
    {
        _Close();
    }

    public virtual bool IsWindowFunctional()
    {
        return true;
    }

    public void Open()
    {
        _Open();
        transform.SetAsLastSibling();
    }

    public void Close() => _Close();
    
    public void SetTitle(string title)
    {
        var txt = gameObject.transform.Find("panel-bg/title-text")?.gameObject.GetComponent<Text>();
        if (txt)
        {
            txt.text = title;
        }
    }

    private static void AddElement(float x, float y, RectTransform rect, RectTransform parent = null)
    {
        if (rect != null)
        {
            Util.NormalizeRectWithTopLeft(rect, x, y, parent);
        }
    }

    protected static Text AddText(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")
    {
        var src = UIRoot.instance.uiGame.assemblerWindow.stateText;
        var txt = Instantiate(src);
        txt.gameObject.name = objName;
        txt.text = label;
        txt.color = new Color(1f, 1f, 1f, 0.4f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.fontSize = fontSize;
        if (txt.transform is RectTransform rect)
        {
            rect.sizeDelta = new Vector2(txt.preferredWidth + 40f, 30f);
        }
        AddElement(x, y, txt.rectTransform, parent);
        return txt;
    }
    
    protected InputField AddInputField(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "input", UnityAction<string> onChanged = null, UnityAction<string> onEditEnd = null)
    {
        var stationWindow = UIRoot.instance.uiGame.stationWindow;
        //public InputField nameInput;
        var inputField = Instantiate(stationWindow.nameInput);
        inputField.gameObject.name = "search-field";
        Destroy(inputField.GetComponent<UIButton>());
        inputField.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        var rect = Util.NormalizeRectWithTopLeft(inputField, x, y, parent);
        rect.sizeDelta = new Vector2(210, rect.sizeDelta.y);
        inputField.textComponent.text = text;
        inputField.textComponent.fontSize = fontSize;
        _inputFields[inputField] = Tuple.Create(onChanged, onEditEnd);
        if (EventRegistered)
        {
            if (onChanged != null)
                inputField.onValueChanged.AddListener(onChanged);
            if (onEditEnd != null)
                inputField.onEndEdit.AddListener(onEditEnd);
        }
        return inputField;
    }

    public override void _OnRegEvent()
    {
        base._OnRegEvent();
        if (EventRegistered) return;
        foreach (var t in _inputFields)
        {
            var inputField = t.Key;
            if (t.Value.Item1 != null)
                inputField.onValueChanged.AddListener(t.Value.Item1);
            if (t.Value.Item2 != null)
                inputField.onEndEdit.AddListener(t.Value.Item2);
        }
        EventRegistered = true;
    }

    public override void _OnUnregEvent()
    {
        base._OnUnregEvent();
        if (!EventRegistered) return;
        EventRegistered = false;
        foreach (var t in _inputFields)
        {
            var inputField = t.Key;
            if (t.Value.Item1 != null)
                inputField.onValueChanged.RemoveListener(t.Value.Item1);
            if (t.Value.Item2 != null)
                inputField.onEndEdit.RemoveListener(t.Value.Item2);
        }
    }
}

public class MyWindowWithTabs : MyWindow
{
    private readonly List<Tuple<RectTransform, UIButton>> _tabs = new();
    public override void TryClose()
    {
        _Close();
    }

    public override bool IsWindowFunctional()
    {
        return true;
    }
    
    protected RectTransform AddTab(float x, int index, RectTransform parent, string label)
    {
        var tab = new GameObject();
        var tabRect = tab.AddComponent<RectTransform>();
        Util.NormalizeRectWithMargin(tabRect, 54f + 28f, 36f, 0f, 0f, parent);
        tab.name = "tab-" + index;
        var swarmPanel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.swarmPanel;
        var src = swarmPanel.orbitButtons[0];
        var btn = Instantiate(src);
        var btnRect = Util.NormalizeRectWithTopLeft(btn, x, 54f, parent);
        btnRect.sizeDelta = new Vector2(100f, 24f);
        btn.transform.Find("frame").gameObject.SetActive(false);
        if (btn.transitions.Length >= 3)
        {
            btn.transitions[0].normalColor = new Color(0.1f, 0.1f, 0.1f, 0.68f);
            btn.transitions[0].highlightColorOverride = new Color(0.9906f, 0.5897f, 0.3691f, 0.4f);
            btn.transitions[1].normalColor = new Color(1f, 1f, 1f, 0.6f);
            btn.transitions[1].highlightColorOverride = new Color(0.2f, 0.1f, 0.1f, 0.9f);
        }

        var btnText = btn.transform.Find("Text").GetComponent<Text>();
        btnText.text = label;
        btnText.fontSize = 16;
        btn.data = index;

        _tabs.Add(Tuple.Create(tabRect, btn));
        if (EventRegistered)
        {
            btn.onClick += OnTabButtonClick;
        }
        return tabRect;
    }

    public override void _OnRegEvent()
    {
        if (!EventRegistered)
        {
            foreach (var t in _tabs)
            {
                t.Item2.onClick += OnTabButtonClick;
            }
        }
        base._OnRegEvent();
    }

    public override void _OnUnregEvent()
    {
        if (EventRegistered)
        {
            foreach (var t in _tabs)
            {
                t.Item2.onClick -= OnTabButtonClick;
            }
        }
        base._OnUnregEvent();
    }

    protected void SetCurrentTab(int index) => OnTabButtonClick(index);

    private void OnTabButtonClick(int index)
    {
        foreach (var t in _tabs)
        {
            var btn = t.Item2;
            if (btn.data != index)
            {
                btn.highlighted = false;
                t.Item1.gameObject.SetActive(false);
                continue;
            }
            btn.highlighted = true;
            t.Item1.gameObject.SetActive(true);
        }
    }
}

public static class MyWindowManager
{
    private static readonly List<ManualBehaviour> Windows = new(4);
    private static bool _initialized;

    public static T CreateWindow<T>(string name, string title = "") where T : MyWindow
    {
        var srcWin = UIRoot.instance.uiGame.tankWindow;
        var src = srcWin.gameObject;
        var go = Object.Instantiate(src, UIRoot.instance.uiGame.transform.parent);
        go.name = name;
        go.SetActive(false);
        Object.Destroy(go.GetComponent<UITankWindow>());
        var win = go.AddComponent<T>() as MyWindow;
        if (win == null)
            return null;

        for (var i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i).gameObject;
            if (child.name == "panel-bg")
            {
                var btn = child.GetComponentInChildren<Button>();
                //close-btn
                if (btn != null)
                {
                    btn.onClick.AddListener(win._Close);
                }
            }
            else if (child.name != "shadow" && child.name != "panel-bg")
            {
                Object.Destroy(child);
            }
        }

        win.SetTitle(title);            
        win._Create();
        if (_initialized)
        {
            win._Init(win.data);
        }
        Windows.Add(win);
        return (T)win;
    }

    /*
    public static void SetRect(ManualBehaviour win, RectTransform rect)
    {
        var rectTransform = win.GetComponent<RectTransform>();
        //rectTransform.position =
        //rectTransform.sizeDelta = rect;
    }
    */

    public static class Patch
    {

        /*
        //_Create -> _Init
        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
        public static void UIGame__OnCreate_Postfix()
        {
        }
        */

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), "_OnDestroy")]
        public static void UIRoot__OnDestroy_Postfix()
        {
            foreach (var win in Windows)
            {
                win._Free();
                win._Destroy();
            }
            Windows.Clear();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), "_OnOpen")]
        public static void UIRoot__OnOpen_Postfix()
        {
            if (_initialized) return;
            foreach (var win in Windows)
            {
                win._Init(win.data);
            }
            _initialized = true;
        }

        /*
        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnFree")]
        public static void UIGame__OnFree_Postfix()
        {
            foreach (var win in Windows)
            {
                win._Free();
            }
        }
        */

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), "_OnUpdate")]
        public static void UIRoot__OnUpdate_Postfix()
        {
            if (GameMain.isPaused || !GameMain.isRunning)
            {
                return;
            }
            foreach (var win in Windows)
            {
                win._Update();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "ShutAllFunctionWindow")]
        public static void UIGame_ShutAllFunctionWindow_Postfix()
        {
            foreach (var win in Windows)
            {
                if (win is MyWindow theWin && theWin.IsWindowFunctional())
                {
                    theWin.TryClose();
                }
            }
        }
    }
}