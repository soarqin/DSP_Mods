using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UXAssist.UI;

// MyWindow modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyWindowCtl.cs

public class MyWindow : ManualBehaviour
{
    private readonly Dictionary<InputField, Tuple<UnityAction<string>, UnityAction<string>>> _inputFields = new();
    private readonly Dictionary<UIButton, UnityAction> _buttons = new();
    protected bool EventRegistered { get; private set; }
    private float _maxX;
    protected float MaxY;
    protected const float TitleHeight = 48f;
    protected const float TabWidth = 105f;
    protected const float TabHeight = 27f;
    protected const float Margin = 30f;
    protected const float Spacing = 10f;

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
            txt.text = title.Translate();
        }
    }

    public void AutoFitWindowSize()
    {
        var trans = GetComponent<RectTransform>();
        trans.sizeDelta = new Vector2(_maxX + Margin + TabWidth + Spacing + Margin, MaxY + TitleHeight + Margin);
    }

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
        var txt = Instantiate(src);
        txt.gameObject.name = objName;
        txt.text = label.Translate();
        txt.color = new Color(1f, 1f, 1f, 0.4f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.fontSize = fontSize;
        txt.rectTransform.sizeDelta = new Vector2(txt.preferredWidth + 8f, txt.preferredHeight + 8f);
        AddElement(x, y, txt.rectTransform, parent);
        return txt;
    }

    public Text AddText2(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")
    {
        var text = AddText(x, y, parent, label, fontSize, objName);
        _maxX = Math.Max(_maxX, x + text.rectTransform.sizeDelta.x);
        MaxY = Math.Max(MaxY, y + text.rectTransform.sizeDelta.y);
        return text;
    }

    public static UIButton AddTipsButton(float x, float y, RectTransform parent, string label, string tip, string content, string objName = "tips-button")
    {
        var src = UIRoot.instance.galaxySelect.sandboxToggle.gameObject.transform.parent.Find("tip-button");
        var dst = Instantiate(src);
        dst.gameObject.name = objName;
        var btn = dst.GetComponent<UIButton>();
        Util.NormalizeRectWithTopLeft(btn, x, y, parent);
        btn.tips.topLevel = true;
        btn.tips.tipTitle = label;
        btn.tips.tipText = tip;
        btn.UpdateTip();
        return btn;
    }

    public UIButton AddTipsButton2(float x, float y, RectTransform parent, string label, string tip, string content, string objName = "tips-button")
    {
        var tipsButton = AddTipsButton(x, y, parent, label, tip, content, objName);
        var rect = tipsButton.transform as RectTransform;
        if (rect != null)
        {
            _maxX = Math.Max(_maxX, x + rect.sizeDelta.x);
            MaxY = Math.Max(MaxY, y + rect.sizeDelta.y);
        }

        return tipsButton;
    }

    public UIButton AddButton(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)
    {
        return AddButton(x, y, 150f, parent, text, fontSize, objName, onClick);
    }

    public UIButton AddButton(float x, float y, float width, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)
    {
        var panel = UIRoot.instance.uiGame.statWindow.performancePanelUI;
        var btn = Instantiate(panel.cpuActiveButton);
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
        btn.button.onClick.RemoveAllListeners();
        btn.tip = null;
        btn.tips = new UIButton.TipSettings();
        _buttons[btn] = onClick;
        if (EventRegistered)
        {
            if (onClick != null)
                btn.button.onClick.AddListener(onClick);
        }

        _maxX = Math.Max(_maxX, x + rect.sizeDelta.x);
        MaxY = Math.Max(MaxY, y + rect.sizeDelta.y);
        return btn;
    }

    public UIButton AddFlatButton(float x, float y, RectTransform parent, string text = "", int fontSize = 12, string objName = "button", UnityAction onClick = null)
    {
        var panel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.layerPanel;
        var btn = Instantiate(panel.layerButtons[0]);
        btn.gameObject.name = objName;
        btn.highlighted = false;
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = panel.buttonDefaultSprite;
            img.color = new Color(img.color.r, img.color.g, img.color.b, 13f / 255f);
        }

        img = btn.gameObject.transform.Find("frame")?.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
        }

        var rect = Util.NormalizeRectWithTopLeft(btn, x, y, parent);
        var t = btn.gameObject.transform.Find("Text")?.GetComponent<Text>();
        if (t != null)
        {
            t.text = text.Translate();
            t.fontSize = fontSize;
        }

        btn.button.onClick.RemoveAllListeners();
        _buttons[btn] = onClick;
        if (EventRegistered && onClick != null)
        {
            btn.button.onClick.AddListener(onClick);
        }

        _maxX = Math.Max(_maxX, x + rect.sizeDelta.x);
        MaxY = Math.Max(MaxY, y + rect.sizeDelta.y);
        return btn;
    }

    public MyCheckBox AddCheckBox(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)
    {
        var cb = MyCheckBox.CreateCheckBox(x, y, parent, config, label, fontSize);
        _maxX = Math.Max(_maxX, x + cb.Width);
        MaxY = Math.Max(MaxY, y + cb.Height);
        return cb;
    }

    public MySlider AddSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f)
    {
        var slider = MySlider.CreateSlider(x, y, parent, value, minValue, maxValue, format, width);
        var rect = slider.rectTrans;
        if (rect != null)
        {
            _maxX = Math.Max(_maxX, x + rect.sizeDelta.x);
            MaxY = Math.Max(MaxY, y + rect.sizeDelta.y);
        }

        return slider;
    }

    public class ValueMapper<T>
    {
        public virtual int Min => 1;
        public virtual int Max => 100;
        public virtual int ValueToIndex(T value) => (int)Convert.ChangeType(value, typeof(int), CultureInfo.InvariantCulture);
        public virtual T IndexToValue(int index) => (T)Convert.ChangeType(index, typeof(T), CultureInfo.InvariantCulture);

        public virtual string FormatValue(string format, T value)
        {
            return string.Format($"{{0:{format}}}", value);
        }
    }

    public MySlider AddSlider<T>(float x, float y, RectTransform parent, ConfigEntry<T> config, ValueMapper<T> valueMapper, string format = "G", float width = 0f)
    {
        var slider = MySlider.CreateSlider(x, y, parent, OnConfigValueChanged(config), valueMapper.Min, valueMapper.Max, format, width);
        slider.SetLabelText(valueMapper.FormatValue(format, config.Value));
        config.SettingChanged += (sender, args) =>
        {
            var index = OnConfigValueChanged(config);
            slider.Value = index;
        };
        slider.OnValueChanged += () =>
        {
            var index = Mathf.RoundToInt(slider.Value);
            config.Value = valueMapper.IndexToValue(index);
            slider.SetLabelText(valueMapper.FormatValue(format, config.Value));
        };

        var rect = slider.rectTrans;
        if (rect != null)
        {
            _maxX = Math.Max(_maxX, x + rect.sizeDelta.x);
            MaxY = Math.Max(MaxY, y + rect.sizeDelta.y);
        }

        return slider;

        int OnConfigValueChanged(ConfigEntry<T> conf)
        {
            var index = valueMapper.ValueToIndex(conf.Value);
            if (index >= 0) return index;
            index = ~index;
            index = Math.Max(0, Math.Min(valueMapper.Max, index));
            conf.Value = valueMapper.IndexToValue(index);
            return index;
        }
    }

    private class ArrayMapper<T> : ValueMapper<T>
    {
        private readonly T[] _values;

        public ArrayMapper(T[] values)
        {
            Array.Sort(values);
            _values = values;
        }

        public override int Min => 0;
        public override int Max => _values.Length - 1;

        public override int ValueToIndex(T value)
        {
            return Array.BinarySearch(_values, value);
        }

        public override T IndexToValue(int index)
        {
            return _values[index >= 0 && index < _values.Length ? index : 0];
        }
    }

    public MySlider AddSlider<T>(float x, float y, RectTransform parent, ConfigEntry<T> config, T[] valueList, string format = "G", float width = 0f)
    {
        return AddSlider(x, y, parent, config, new ArrayMapper<T>(valueList), format, width);
    }

    public InputField AddInputField(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "input", UnityAction<string> onChanged = null,
        UnityAction<string> onEditEnd = null)
    {
        var stationWindow = UIRoot.instance.uiGame.stationWindow;
        //public InputField nameInput;
        var inputField = Instantiate(stationWindow.nameInput);
        inputField.gameObject.name = objName;
        Destroy(inputField.GetComponent<UIButton>());
        inputField.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        var rect = Util.NormalizeRectWithTopLeft(inputField, x, y, parent);
        rect.sizeDelta = new Vector2(210, rect.sizeDelta.y);
        inputField.textComponent.text = text;
        inputField.textComponent.fontSize = fontSize;
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();
        _inputFields[inputField] = Tuple.Create(onChanged, onEditEnd);
        if (EventRegistered)
        {
            if (onChanged != null)
                inputField.onValueChanged.AddListener(onChanged);
            if (onEditEnd != null)
                inputField.onEndEdit.AddListener(onEditEnd);
        }

        _maxX = Math.Max(_maxX, x + rect.sizeDelta.x);
        MaxY = Math.Max(MaxY, y + rect.sizeDelta.y);
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

        foreach (var t in _buttons)
        {
            var btn = t.Key;
            if (t.Value != null)
                btn.button.onClick.AddListener(t.Value);
        }

        EventRegistered = true;
    }

    public override void _OnUnregEvent()
    {
        base._OnUnregEvent();
        if (!EventRegistered) return;
        EventRegistered = false;
        foreach (var t in _buttons)
        {
            var btn = t.Key;
            if (t.Value != null)
                btn.button.onClick.RemoveListener(t.Value);
        }

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
    private readonly List<Tuple<RectTransform, UIButton>> _tabs = [];
    private float _tabY = 54f;

    public override void TryClose()
    {
        _Close();
    }

    public override bool IsWindowFunctional()
    {
        return true;
    }

    private RectTransform AddTabInternal(float y, int index, RectTransform parent, string label)
    {
        var tab = new GameObject();
        var tabRect = tab.AddComponent<RectTransform>();
        Util.NormalizeRectWithMargin(tabRect, TitleHeight, Margin + TabWidth + Spacing, 0f, 0f, parent);
        tab.name = "tab-" + index;
        var swarmPanel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.swarmPanel;
        var src = swarmPanel.orbitButtons[0];
        var btn = Instantiate(src);
        var btnRect = Util.NormalizeRectWithTopLeft(btn, Margin, y, parent);
        btn.name = "tab-btn-" + index;
        btnRect.sizeDelta = new Vector2(TabWidth, TabHeight);
        btn.transform.Find("frame").gameObject.SetActive(false);
        if (btn.transitions.Length >= 3)
        {
            btn.transitions[0].normalColor = new Color(0.1f, 0.1f, 0.1f, 0.68f);
            btn.transitions[0].highlightColorOverride = new Color(0.9906f, 0.5897f, 0.3691f, 0.4f);
            btn.transitions[1].normalColor = new Color(1f, 1f, 1f, 0.6f);
            btn.transitions[1].highlightColorOverride = new Color(0.2f, 0.1f, 0.1f, 0.9f);
        }

        var btnText = btn.transform.Find("Text").GetComponent<Text>();
        btnText.text = label.Translate();
        btnText.fontSize = 16;
        btn.data = index;

        _tabs.Add(Tuple.Create(tabRect, btn));
        if (EventRegistered)
        {
            btn.onClick += OnTabButtonClick;
        }

        MaxY = Math.Max(MaxY, y + TabHeight);
        return tabRect;
    }

    public RectTransform AddTab(RectTransform parent, string label)
    {
        var result = AddTabInternal(_tabY, _tabs.Count, parent, label);
        _tabY += 28f;
        return result;
    }

    public void AddSplitter(RectTransform parent, float spacing)
    {
        var img = Instantiate(UIRoot.instance.optionWindow.transform.Find("tab-line").Find("bar"));
        Destroy(img.Find("tri").gameObject);
        _tabY += spacing;
        var rect = Util.NormalizeRectWithTopLeft(img, 28, _tabY, parent);
        rect.sizeDelta = new Vector2(107, 2);
        _tabY += 2;
    }

    public void AddTabGroup(RectTransform parent, string label, string objName = "tabl-group-label")
    {
        AddText(28, _tabY - 2, parent, label, 16, objName);
        _tabY += 28f;
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
        foreach (var (rectTransform, btn) in _tabs)
        {
            if (btn.data != index)
            {
                btn.highlighted = false;
                rectTransform.gameObject.SetActive(false);
                continue;
            }

            btn.highlighted = true;
            rectTransform.gameObject.SetActive(true);
        }
    }
}

public static class MyWindowManager
{
    private static readonly List<ManualBehaviour> Windows = new(4);
    private static bool _initialized;
    private static Harmony _patch;

    public static void Init()
    {
        _patch ??= Harmony.CreateAndPatchAll(typeof(Patch));
    }

    public static void Uninit()
    {
        _patch?.UnpatchSelf();
        _patch = null;
    }

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

    public static void DestroyWindow(ManualBehaviour win)
    {
        if (win == null) return;
        Windows.Remove(win);
        win._Free();
        win._Destroy();
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

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot._OnDestroy))]
        public static void UIRoot__OnDestroy_Postfix()
        {
            foreach (var win in Windows)
            {
                win._Free();
                win._Destroy();
            }

            Windows.Clear();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot._OnOpen))]
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

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot._OnUpdate))]
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

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), nameof(UIGame.ShutAllFunctionWindow))]
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