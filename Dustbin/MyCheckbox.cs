using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dustbin;

// MyCheckbox modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/LSTM.cs
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
        UIBuildMenu buildMenu = UIRoot.instance.uiGame.buildMenu;
        UIButton src = buildMenu.uxFacilityCheck;

        GameObject go = GameObject.Instantiate(src.gameObject);
        go.name = "my-checkbox";
        MyCheckBox cb = go.AddComponent<MyCheckBox>();
        cb._checked = check;
        RectTransform rect = go.transform as RectTransform;
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
        //ResetAnchor(cb.checkImage.rectTransform);

        //text
        Transform child = go.transform.Find("text");
        if (child != null)
        {
            //ResetAnchor(child as RectTransform);
            GameObject.DestroyImmediate(child.GetComponent<Localizer>());
            cb.labelText = child.GetComponent<Text>();
            cb.labelText.fontSize = fontSize;
            cb.SetLabelText(label);
        }

        //value
        cb.uiButton.onClick += cb.OnClick;
        cb.checkImage.enabled = check;

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
