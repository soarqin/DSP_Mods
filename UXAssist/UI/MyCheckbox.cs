using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace UXAssist.UI;

// MyCheckBox modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyCheckBox.cs
public class MyCheckBox : MonoBehaviour
{
    public RectTransform rectTrans;
    public UIButton uiButton;
    public Image boxImage;
    public Image checkImage;
    public Text labelText;
    public event Action OnChecked;
    protected event Action OnFree;
    private bool _checked;

    private static readonly Color BoxColor = new Color(1f, 1f, 1f, 100f / 255f);
    private static readonly Color CheckColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color TextColor = new Color(178f / 255f, 178f / 255f, 178f / 255f, 168f / 255f);
    
    protected void OnDestroy()
    {
        OnFree?.Invoke();
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            checkImage.enabled = value;
        }
    }

    public void SetEnable(bool on)
    {
        if (uiButton) uiButton.enabled = on;
        if (on)
        {
            if (boxImage) boxImage.color = BoxColor;
            if (checkImage) checkImage.color = CheckColor;
            if (labelText) labelText.color = TextColor;
        }
        else
        {
            if (boxImage) boxImage.color = BoxColor.RGBMultiplied(0.5f);
            if (checkImage) checkImage.color = CheckColor.RGBMultiplied(0.5f);
            if (labelText) labelText.color = TextColor.RGBMultiplied(0.5f);
        }
    }

    public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)
    {
        var cb = CreateCheckBox(x, y, parent, config.Value, label, fontSize);
        cb.OnChecked += () => config.Value = !config.Value;
        EventHandler func = (_, _) => cb.Checked = config.Value;
        config.SettingChanged += func;
        cb.OnFree += () => config.SettingChanged -= func;
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
        cb.boxImage = go.transform.GetComponent<Image>();
        cb.checkImage = go.transform.Find("checked")?.GetComponent<Image>();

        var child = go.transform.Find("text");
        if (child != null)
        {
            DestroyImmediate(child.GetComponent<Localizer>());
            cb.labelText = child.GetComponent<Text>();
            cb.labelText.fontSize = fontSize;
            cb.SetLabelText(label);
            var width = cb.labelText.preferredWidth;
            cb.labelText.rectTransform.sizeDelta = new Vector2(width, cb.labelText.rectTransform.sizeDelta.y);
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

    public float Width => rectTrans.sizeDelta.x + labelText.rectTransform.sizeDelta.x;
    public float Height => Math.Max(rectTrans.sizeDelta.y, labelText.rectTransform.sizeDelta.y);
}
