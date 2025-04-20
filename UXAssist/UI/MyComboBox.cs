using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace UXAssist.UI;

public class MyComboBox : MonoBehaviour
{
    private RectTransform _rectTrans;
    private UIComboBox _comboBox;
    private Text _text;
    public Action<int> OnSelChanged;

    private static GameObject _baseObject;

    public static void InitBaseObject()
    {
        if (_baseObject) return;
        var fontSource = UIRoot.instance.uiGame.buildMenu.uxFacilityCheck.transform.Find("text")?.GetComponent<Text>();
        var go = Instantiate(UIRoot.instance.optionWindow.resolutionComp.transform.parent.gameObject);
        go.name = "my-combobox";
        go.SetActive(false);

        var txt = go.GetComponent<Text>();
        if (txt) txt.text = "";
        if (txt && fontSource)
        {
            txt.font = fontSource.font;
            txt.fontSize = fontSource.fontSize;
            txt.fontStyle = fontSource.fontStyle;
            txt.color = new Color(1f, 1f, 1f, 0.6f);
        }
        var localizer = go.GetComponent<Localizer>();
        if (localizer) DestroyImmediate(localizer);

        var rect = (RectTransform)go.transform;
        var cbctrl = rect.transform.Find("ComboBox").GetComponent<UIComboBox>();
        foreach (var button in cbctrl.ItemButtons)
        {
            Destroy(button.gameObject);
        }
        cbctrl.Items.Clear();
        cbctrl.ItemButtons.Clear();
        if (fontSource)
        {
            var txtComp = cbctrl.m_ListItemRes.GetComponentInChildren<Text>();
            if (txtComp)
            {
                txtComp.font = fontSource.font;
                txtComp.fontSize = fontSource.fontSize;
                txtComp.fontStyle = fontSource.fontStyle;
            }
            txtComp = cbctrl.transform.Find("Main Button")?.GetComponentInChildren<Text>();
            if (txtComp)
            {
                txtComp.font = fontSource.font;
                txtComp.fontSize = fontSource.fontSize;
                txtComp.fontStyle = fontSource.fontStyle;
            }
        }
        cbctrl.onSubmit.RemoveAllListeners();
        cbctrl.onItemIndexChange.RemoveAllListeners();
        _baseObject = go;
    }

    public static MyComboBox CreateComboBox(float x, float y, RectTransform parent)
    {
        var gameObject = Instantiate(_baseObject);
        gameObject.name = "my-combobox";
        gameObject.SetActive(true);
        var cb = gameObject.AddComponent<MyComboBox>();
        var rtrans = Util.NormalizeRectWithTopLeft(cb, x, y, parent);
        cb._rectTrans = rtrans;
        cb._text = gameObject.GetComponent<Text>();
        var box = rtrans.Find("ComboBox").GetComponent<UIComboBox>();
        cb._comboBox = box;
        box.onItemIndexChange.AddListener(() => { cb.OnSelChanged?.Invoke(box.itemIndex); });
        cb.UpdateComboBoxPosition();

        return cb;
    }

    protected void OnDestroy()
    {
        _config.SettingChanged -= _configChanged;
    }

    private void UpdateComboBoxPosition()
    {
        var rtrans = (RectTransform)_comboBox.transform;
        var oldPosition = rtrans.localPosition;
        var pwidth = _text.preferredWidth;
        rtrans.localPosition = new Vector3(pwidth + 5f, oldPosition.y, oldPosition.z);
        _rectTrans.sizeDelta = new Vector2(rtrans.localPosition.x + rtrans.sizeDelta.x, _rectTrans.sizeDelta.y);
    }

    public void SetPrompt(string prompt)
    {
        _text.text = prompt.Translate();
        UpdateComboBoxPosition();
    }

    public void SetFontSize(int size)
    {
        _text.fontSize = size;
        _comboBox.ItemButtons.ForEach(b => b.GetComponentInChildren<Text>().fontSize = size);
        _comboBox.m_ListItemRes.GetComponentInChildren<Text>().fontSize = size;
        var txtComp = _comboBox.transform.Find("Main Button")?.GetComponentInChildren<Text>();
        if (txtComp) txtComp.fontSize = size;
        UpdateComboBoxPosition();
    }

    public void SetItems(params string[] items) => _comboBox.Items = items.Select(s => s.Translate()).ToList();

    public void SetIndex(int index) => _comboBox.itemIndex = index;

    public void SetSize(float width, float height)
    {
        var rtrans = (RectTransform)_comboBox.transform;
        rtrans.sizeDelta = new Vector2(width > 0f ? width : rtrans.sizeDelta.x, height > 0f ? height : rtrans.sizeDelta.y);
        _rectTrans.sizeDelta = new Vector2(rtrans.localPosition.x + rtrans.sizeDelta.x, _rectTrans.sizeDelta.y);
    }

    public void AddOnSelChanged(Action<int> action) => OnSelChanged += action;

    private EventHandler _configChanged;
    private Action<int> _selChanged;
    private ConfigEntry<int> _config;
    public void SetConfigEntry(ConfigEntry<int> config)
    {
        if (_selChanged != null) OnSelChanged -= _selChanged;
        if (_configChanged != null) config.SettingChanged -= _configChanged;

        _comboBox.itemIndex = config.Value;
        _config = config;
        _selChanged = value => config.Value = value;
        OnSelChanged += _selChanged;
        _configChanged = (_, _) => SetIndex(config.Value);
        config.SettingChanged += _configChanged;
    }

    public MyComboBox WithPrompt(string prompt)
    {
        SetPrompt(prompt);
        return this;
    }

    public MyComboBox WithFontSize(int size)
    {
        SetFontSize(size);
        return this;
    }

    public MyComboBox WithItems(params string[] items)
    {
        SetItems(items);
        return this;
    }

    public MyComboBox WithIndex(int index)
    {
        SetIndex(index);
        return this;
    }

    public MyComboBox WithSize(float width, float height)
    {
        SetSize(width, height);
        return this;
    }

    public MyComboBox WithOnSelChanged(params Action<int>[] action)
    {
        foreach (var act in action)
            AddOnSelChanged(act);
        return this;
    }

    public MyComboBox WithConfigEntry(ConfigEntry<int> config)
    {
        SetConfigEntry(config);
        return this;
    }

    public float Width => _rectTrans.sizeDelta.x;
    public float Height => _rectTrans.sizeDelta.y;
}