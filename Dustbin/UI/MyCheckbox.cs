using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dustbin.UI;

// MyCheckBox modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyCheckBox.cs
public class MyCheckBox : MonoBehaviour
{
    public UIButton uiButton;
    public Image checkImage;
    public RectTransform rectTrans;
    public Text labelText;

    public event Action OnChecked;
    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            checkImage.enabled = value;
        }
    }

    private bool _checked;

    public static MyCheckBox CreateCheckBox(bool check, Transform parent = null, float x = 0f, float y = 0f, string label = "", int fontSize = 15)
    {
        var buildMenu = UIRoot.instance.uiGame.buildMenu;
        var src = buildMenu.uxFacilityCheck;

        var go = Instantiate(src.gameObject);
        var rect = go.transform as RectTransform;
        if (rect == null)
        {
            return null;
        }
        go.name = "my-checkbox";
        var cb = go.AddComponent<MyCheckBox>();
        cb._checked = check;
        if (parent != null)
        {
            rect.SetParent(parent);
        }
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition3D = new Vector3(x, -y, 0f);

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
            labelText.text = val;
        }
    }

    public void OnClick(int obj)
    {
        _checked = !_checked;
        checkImage.enabled = _checked;
        OnChecked?.Invoke();
    }
}
