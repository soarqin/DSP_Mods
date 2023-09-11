using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler.UI;

// MyCheckBox modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyCheckBox.cs
public class MyCheckBox : MonoBehaviour
{
    public UIButton uiButton;
    public Image checkImage;
    public RectTransform rectTrans;
    public Text labelText;
    public event Action OnChecked;
    private bool _checked;
    private ConfigEntry<bool> _configAssigned;

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            checkImage.enabled = value;
        }
    }

    public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)
    {
        var cb = CreateCheckBox(x, y, parent, config.Value, label, fontSize);
        cb._configAssigned = config;
        cb.OnChecked += () => config.Value = !config.Value;
        config.SettingChanged += (_, _) => cb.Checked = config.Value;
        return cb;
    }

    public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, bool check, string label = "", int fontSize = 15)
    {
        var buildMenu = UIRoot.instance.uiGame.buildMenu;
        var src = buildMenu.uxFacilityCheck;

        var go = Instantiate(src.gameObject);
        go.name = "my-checkbox";
        var cb = go.AddComponent<MyCheckBox>();
        cb._checked = check;
        var rect = Util.NormalizeRectWithTopLeft(cb, x, y, parent);

        cb.rectTrans = rect;
        cb.uiButton = go.GetComponent<UIButton>();
        cb.checkImage = go.transform.Find("checked")?.GetComponent<Image>();

        var child = go.transform.Find("text");
        if (child != null)
        {
            DestroyImmediate(child.GetComponent<Localizer>());
            cb.labelText = child.GetComponent<Text>();
            cb.labelText.fontSize = fontSize;
            cb.SetLabelText(label);
        }

        //value
        cb.uiButton.onClick += cb.OnClick;
        if (cb.checkImage != null)
        {
            cb.checkImage.enabled = check;
        }

        return cb;
    }

    public void SetLabelText(string val)
    {
        if (labelText != null)
        {
            labelText.text = val.Translate();
        }
    }

    public void OnClick(int obj)
    {
        _checked = !_checked;
        checkImage.enabled = _checked;
        OnChecked?.Invoke();
    }
}
