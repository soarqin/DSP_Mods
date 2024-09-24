using System;
using System.Linq;
using UnityEngine;

namespace UXAssist.UI;

public class MyComboBox : MonoBehaviour
{
    private RectTransform _rectTrans;
    private UIComboBox _comboBox;
    private Action<int> _onSelChanged;

    private static GameObject _baseObject;

    public static MyComboBox CreateComboBox(float x, float y, RectTransform parent)
    {
        if (!_baseObject)
        {
            var go = Instantiate(UIRoot.instance.optionWindow.resolutionComp.transform.parent.gameObject);
            go.name = "my-combobox";
            var rect = (RectTransform)go.transform;
            var cbctrl = rect.transform.Find("ComboBox");
            var content = cbctrl.Find("Dropdown List ScrollBox")?.Find("Mask")?.Find("Content Panel");
            if (content != null)
            {
                for (var i = content.childCount - 1; i >= 0; i--)
                {
                    var theTrans = content.GetChild(i);
                    if (theTrans.name == "Item Button(Clone)")
                    {
                        Destroy(theTrans.gameObject);
                    }
                }
            }
            var comboBox = cbctrl.GetComponent<UIComboBox>();
            comboBox.onSubmit.RemoveAllListeners();
            comboBox.onItemIndexChange.RemoveAllListeners();
            _baseObject = go;
        }
        var gameObject = Instantiate(_baseObject);
        gameObject.name = "my-combobox";
        var cb = gameObject.AddComponent<MyComboBox>();
        var rtrans = Util.NormalizeRectWithTopLeft(cb, x, y, parent);
        cb._rectTrans = rtrans;
            
        var box = rtrans.transform.Find("ComboBox").GetComponent<UIComboBox>();
        cb._comboBox = box;
        box.onItemIndexChange.AddListener(() => { cb._onSelChanged?.Invoke(box.itemIndex); });

        return cb;
    }
    
    public MyComboBox SetItems(string[] items)
    {
        _comboBox.Items = items.ToList();
        return this;
    }
    
    public MyComboBox SetIndex(int index)
    {
        _comboBox.itemIndex = index;
        return this;
    }
    
    public MyComboBox AddOnSelChanged(Action<int> action)
    {
        _onSelChanged += action;
        return this;
    }

    public MyComboBox SetSize(float width, float height)
    {
        _rectTrans.sizeDelta = new Vector2(width, height);
        return this;
    }
}