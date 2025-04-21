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
    private bool _checked;

    private static GameObject _baseObject;

    private static readonly Color BoxColor = new Color(1f, 1f, 1f, 100f / 255f);
    private static readonly Color CheckColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color TextColor = new Color(178f / 255f, 178f / 255f, 178f / 255f, 168f / 255f);

    public static void InitBaseObject()
    {
        if (_baseObject) return;
        var go = Instantiate(UIRoot.instance.uiGame.buildMenu.uxFacilityCheck.gameObject);
        go.name = "my-checkbox";
        go.SetActive(false);
        var comp = go.transform.Find("text");
        if (comp)
        {
            var txt = comp.GetComponent<Text>();
            if (txt) txt.text = "";
            var localizer = comp.GetComponent<Localizer>();
            if (localizer) DestroyImmediate(localizer);
        }
        _baseObject = go;
    }

    protected void OnDestroy()
    {
        _config.SettingChanged -= _configChanged;
    }

    public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)
    {
        return CreateCheckBox(x, y, parent, config.Value, label, fontSize).WithConfigEntry(config);
    }

    public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, bool check, string label = "", int fontSize = 15)
    {
        return CreateCheckBox(x, y, parent, fontSize).WithCheck(check).WithLabelText(label);
    }

    public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, int fontSize = 15)
    {
        var go = Instantiate(_baseObject);
        go.name = "my-checkbox";
        go.SetActive(true);
        var cb = go.AddComponent<MyCheckBox>();
        var rect = Util.NormalizeRectWithTopLeft(cb, x, y, parent);

        cb.rectTrans = rect;
        cb.uiButton = go.GetComponent<UIButton>();
        cb.boxImage = go.transform.GetComponent<Image>();
        cb.checkImage = go.transform.Find("checked")?.GetComponent<Image>();
        Util.NormalizeRectWithTopLeft(cb.checkImage, 0f, 0f);

        var child = go.transform.Find("text");
        if (child != null)
        {
            cb.labelText = child.GetComponent<Text>();
            if (cb.labelText)
            {
                cb.labelText.text = "";
                cb.labelText.fontSize = fontSize;
                cb.UpdateLabelTextWidth();
            }
        }

        cb.uiButton.onClick += cb.OnClick;
        return cb;
    }

    private void UpdateLabelTextWidth()
    {
        if (labelText) labelText.rectTransform.sizeDelta = new Vector2(labelText.preferredWidth, labelText.rectTransform.sizeDelta.y);
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

    public void SetLabelText(string val)
    {
        if (labelText != null)
        {
            labelText.text = val.Translate();
            UpdateLabelTextWidth();
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

    public MyCheckBox WithLabelText(string val)
    {
        SetLabelText(val);
        return this;
    }

    public MyCheckBox WithCheck(bool check)
    {
        Checked = check;
        return this;
    }

    public MyCheckBox WithSmallerBox(float boxSize = 20f)
    {
        var oldWidth = rectTrans.sizeDelta.x;
        rectTrans.sizeDelta = new Vector2(boxSize, boxSize);
        checkImage.rectTransform.sizeDelta = new Vector2(boxSize, boxSize);
        labelText.rectTransform.sizeDelta = new Vector2(labelText.rectTransform.sizeDelta.x, boxSize);
        labelText.rectTransform.localPosition = new Vector3(labelText.rectTransform.localPosition.x + boxSize - oldWidth, labelText.rectTransform.localPosition.y, labelText.rectTransform.localPosition.z);
        return this;
    }

    public MyCheckBox WithEnable(bool on)
    {
        SetEnable(on);
        return this;
    }

    public MyCheckBox WithConfigEntry(ConfigEntry<bool> config)
    {
        SetConfigEntry(config);
        return this;
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
