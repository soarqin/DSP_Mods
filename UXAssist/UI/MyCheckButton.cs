using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace UXAssist.UI;

// MyCheckButton modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyCheckButton.cs
public class MyCheckButton : MonoBehaviour
{
    public RectTransform rectTrans;
    public UIButton uiButton;
    public Image icon;
    public Text labelText;
    public event Action OnChecked;
    private bool _checked;
    private float _iconWidth = 28f;

    private static GameObject _baseObject;

	private static Color openMouseOverColor;
	private static Color openPressColor;
	private static Color openNormalColor;
	private static Color closeMouseOverColor;
	private static Color closePressColor;
	private static Color closeNormalColor;

    public static void InitBaseObject()
    {
        if (_baseObject) return;
        var tankWindow = UIRoot.instance.uiGame.tankWindow;
        openMouseOverColor = tankWindow.openMouseOverColor;
        openPressColor = tankWindow.openPressColor;
        openNormalColor = tankWindow.openNormalColor;
        closeMouseOverColor = tankWindow.closeMouseOverColor;
        closePressColor = tankWindow.closePressColor;
        closeNormalColor = tankWindow.closeNormalColor;

        var go = Instantiate(UIRoot.instance.uiGame.beltWindow.reverseButton.gameObject);
        go.name = "my-checkbutton";
        go.SetActive(false);
        var comp = go.transform.Find("text");
        if (comp)
        {
            var txt = comp.GetComponent<Text>();
            if (txt)
            {
                txt.text = "";
                txt.alignment = TextAnchor.MiddleCenter;
                txt.rectTransform.anchorMax = new Vector2(0f, 1f);
                txt.rectTransform.anchorMin = new Vector2(0f, 1f);
                txt.rectTransform.pivot = new Vector2(0f, 1f);
                txt.rectTransform.localPosition = new Vector3(0f, 0f, 0f);
            }
            var localizer = comp.GetComponent<Localizer>();
            if (localizer) DestroyImmediate(localizer);
        }
        _baseObject = go;
    }

    protected void OnDestroy()
    {
        if (_config != null) _config.SettingChanged -= _configChanged;
    }

    public static MyCheckButton CreateCheckButton(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)
    {
        return CreateCheckButton(x, y, parent, config.Value, label, fontSize).WithConfigEntry(config);
    }

    public static MyCheckButton CreateCheckButton(float x, float y, RectTransform parent, bool check, string label = "", int fontSize = 15)
    {
        return CreateCheckButton(x, y, parent, fontSize).WithCheck(check).WithLabelText(label);
    }

    public static MyCheckButton CreateCheckButton(float x, float y, RectTransform parent, int fontSize = 15)
    {
        var go = Instantiate(_baseObject);
        go.name = "my-checkbutton";
        go.SetActive(true);
        var cb = go.AddComponent<MyCheckButton>();
        var rect = Util.NormalizeRectWithTopLeft(cb, x, y, parent);

        cb.rectTrans = rect;
        cb.uiButton = go.GetComponent<UIButton>();

        var child = go.transform.Find("text");
        if (child != null)
        {
            cb.labelText = child.GetComponent<Text>();
            if (cb.labelText)
            {
                cb.labelText.text = "";
                cb.labelText.fontSize = fontSize;
            }
        }

        cb._iconWidth = Mathf.Min(cb._iconWidth > 0f ? cb._iconWidth : 32f, rect.sizeDelta.y);
        cb.UpdateCheckColor();
        cb.uiButton.onClick += cb.OnClick;
        cb.UpdateSize();
        return cb;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            UpdateCheckColor();
        }
    }

    public void SetLabelText(string val)
    {
        if (labelText != null)
        {
            labelText.text = val.Translate();
        }
    }

    private EventHandler _configChanged;
    private Action _checkedChanged;
    private ConfigEntry<bool> _config;
    public void SetConfigEntry(ConfigEntry<bool> config)
    {
        if (_checkedChanged != null) OnChecked -= _checkedChanged;
        if (_configChanged != null) config.SettingChanged -= _configChanged;

        _config = config;
        _checkedChanged = () => config.Value = !config.Value;
        OnChecked += _checkedChanged;
        _configChanged = (_, _) => Checked = config.Value;
        config.SettingChanged += _configChanged;
    }

    public MyCheckButton WithLabelText(string val)
    {
        SetLabelText(val);
        return this;
    }

    private void UpdateSize()
    {
        var width = rectTrans.sizeDelta.x;
        var height = rectTrans.sizeDelta.y;
        labelText.rectTransform.localPosition = new Vector3(icon != null ? _iconWidth : 0f, 0f, 0f);
        labelText.rectTransform.sizeDelta = new Vector2(icon != null ? width - _iconWidth : width, height);
        if (icon != null)
        {
            icon.rectTransform.sizeDelta = new Vector2(_iconWidth, _iconWidth);
            icon.rectTransform.localPosition = new Vector3(0f, -height * 0.5f, 0f);
        }
    }

    public MyCheckButton WithSize(float width, float height)
    {
        rectTrans.sizeDelta = new Vector2(width, height);
        if (height < _iconWidth) _iconWidth = height;
        UpdateSize();
        return this;
    }

    public MyCheckButton WithIconWidth(float width)
    {
        if (_iconWidth == width) return this;
        var height = rectTrans.sizeDelta.y;
        if (width > height)
        {
            width = height;
            if (_iconWidth == width) return this;
        }
        _iconWidth = width;
        if (icon != null) UpdateSize();
        return this;
    }

    public MyCheckButton WithIcon(Sprite sprite = null)
    {
        if (icon == null)
        {
            var iconGo = new GameObject("icon");
            var rect = iconGo.AddComponent<RectTransform>();
            (icon = iconGo.AddComponent<Image>()).sprite = sprite;
            iconGo.transform.SetParent(rectTrans);
            rect.sizeDelta = new Vector2(_iconWidth, _iconWidth);
            rect.localScale = new Vector3(1f, 1f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            var height = rectTrans.sizeDelta.y;
            rect.localPosition = new Vector3(0f, -height * 0.5f, 0f);
            iconGo.SetActive(sprite != null);
            UpdateSize();
        }
        else
        {
            SetIcon(sprite);
        }
        return this;
    }

    public MyCheckButton WithTip(string tip, float delay = 1f)
    {
        uiButton.tips.type = UIButton.ItemTipType.Other;
        uiButton.tips.topLevel = true;
        uiButton.tips.tipTitle = tip;
        uiButton.tips.tipText = null;
        uiButton.tips.delay = delay;
        uiButton.tips.corner = 2;
        uiButton.UpdateTip();
        return this;
    }

    public void SetIcon(Sprite sprite = null)
    {
        icon.sprite = sprite;
        icon.gameObject.SetActive(sprite != null);
    }

    public MyCheckButton WithCheck(bool check)
    {
        Checked = check;
        return this;
    }

    public MyCheckButton WithConfigEntry(ConfigEntry<bool> config)
    {
        SetConfigEntry(config);
        return this;
    }

    public void OnClick(int obj)
    {
        _checked = !_checked;
        UpdateCheckColor();
        OnChecked?.Invoke();
    }

    public float Width => rectTrans.sizeDelta.x + labelText.rectTransform.sizeDelta.x;
    public float Height => Math.Max(rectTrans.sizeDelta.y, labelText.rectTransform.sizeDelta.y);

    private void UpdateCheckColor()
    {
        if (_checked)
        {
            uiButton.transitions[0].mouseoverColor = openMouseOverColor;
            uiButton.transitions[0].pressedColor = openPressColor;
            uiButton.transitions[0].normalColor = openNormalColor;
        }
        else
        {
            uiButton.transitions[0].mouseoverColor = closeMouseOverColor;
            uiButton.transitions[0].pressedColor = closePressColor;
            uiButton.transitions[0].normalColor = new Color(0.6557f, 0.9145f, 1f, 0.0627f);
        }
        uiButton.RefreshTransitionsImmediately();
    }
}
