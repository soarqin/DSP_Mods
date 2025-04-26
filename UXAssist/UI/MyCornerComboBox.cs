using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace UXAssist.UI;

public class MyCornerComboBox : MonoBehaviour
{
    private RectTransform _rectTrans;
    private UIComboBox _comboBox;
    public Action<int> OnSelChanged;

    private static GameObject _baseObject;

    public static void InitBaseObject()
    {
        if (_baseObject) return;
        var go = Instantiate(UIRoot.instance.uiGame.starDetail.displayCombo.gameObject);
        go.name = "my-small-combobox";
        go.SetActive(false);

        var cbctrl = go.transform.GetComponent<UIComboBox>();
        cbctrl.onSubmit.RemoveAllListeners();
        cbctrl.onItemIndexChange.RemoveAllListeners();
        foreach (var button in cbctrl.ItemButtons)
        {
            Destroy(button.gameObject);
        }
        cbctrl.Items.Clear();
        cbctrl.ItemButtons.Clear();
        _baseObject = go;
    }

    public static MyCornerComboBox CreateComboBox(float x, float y, RectTransform parent, bool topRight = false)
    {
        var gameObject = Instantiate(_baseObject);
        gameObject.name = "my-combobox";
        gameObject.SetActive(true);
        var cb = gameObject.AddComponent<MyCornerComboBox>();
        RectTransform rtrans;
        if (topRight)
        {
            rtrans = Util.NormalizeRectWithTopRight(cb, x, y, parent);
        }
        else
        {
            rtrans = Util.NormalizeRectWithTopLeft(cb, x, y, parent);
        }
        cb._rectTrans = rtrans;
        var box = rtrans.GetComponent<UIComboBox>();
        cb._comboBox = box;
        box.onItemIndexChange.AddListener(() => { cb.OnSelChanged?.Invoke(box.itemIndex); });

        return cb;
    }

    protected void OnDestroy()
    {
        _config.SettingChanged -= _configChanged;
    }

    public void SetFontSize(int size)
    {
        var textComp = _comboBox.transform.Find("Main Button")?.GetComponentInChildren<Text>();
        if (textComp) textComp.fontSize = size;
        _comboBox.ItemButtons.ForEach(b => b.GetComponentInChildren<Text>().fontSize = size);
        _comboBox.m_ListItemRes.GetComponentInChildren<Text>().fontSize = size;
    }

    public void SetItems(params string[] items)
    {
        _comboBox.Items = [.. items.Select(s => s.Translate())];
        _comboBox.StartItemIndex = 0;
        _comboBox.DropDownCount = items.Length;
    }

    public List<string> Items => _comboBox.Items;

    public void UpdateLabelText()
    {
        var textComp = _comboBox.transform.Find("Main Button")?.GetComponentInChildren<Text>();
        if (textComp) textComp.text = _comboBox.Items[_comboBox.itemIndex];
    }

    public void SetIndex(int index) => _comboBox.itemIndex = index;

    public void SetSize(float width, float height)
    {
        _rectTrans.sizeDelta = new Vector2(width > 0f ? width : _rectTrans.sizeDelta.x, height > 0f ? height : _rectTrans.sizeDelta.y);
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

    public MyCornerComboBox WithFontSize(int size)
    {
        SetFontSize(size);
        return this;
    }

    public MyCornerComboBox WithItems(params string[] items)
    {
        SetItems(items);
        return this;
    }

    public MyCornerComboBox WithIndex(int index)
    {
        SetIndex(index);
        return this;
    }

    public MyCornerComboBox WithSize(float width, float height)
    {
        SetSize(width, height);
        return this;
    }

    public MyCornerComboBox WithOnSelChanged(params Action<int>[] action)
    {
        foreach (var act in action)
            AddOnSelChanged(act);
        return this;
    }

    public MyCornerComboBox WithConfigEntry(ConfigEntry<int> config)
    {
        SetConfigEntry(config);
        return this;
    }

    public float Width => _rectTrans.sizeDelta.x;
    public float Height => _rectTrans.sizeDelta.y;
}
