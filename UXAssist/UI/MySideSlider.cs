using System;
using UnityEngine;
using UnityEngine.UI;

namespace UXAssist.UI;

// MySlider modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MySlider.cs

public class MySideSlider : MonoBehaviour
{
    public RectTransform rectTrans;
    public Slider slider;
    public Text labelText;
    public string labelFormat;
    public event Action OnValueChanged;

    public static MySideSlider CreateSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f, float textWidth = 0f)
    {
        return CreateSlider(x, y, parent, width, textWidth).WithLabelFormat(format).WithMinMaxValue(minValue, maxValue).WithValue(value);
    }

    public static MySideSlider CreateSlider(float x, float y, RectTransform parent, float width = 0f, float textWidth = 0f)
    {
        var go = Instantiate(UIRoot.instance.uiGame.stationWindow.maxMiningSpeedGroup.gameObject);
        //sizeDelta = 240, 20
        go.name = "my-side-slider";
        Destroy(go.transform.Find("label").gameObject);
        Destroy(go.GetComponent<UIButton>());
        go.SetActive(true);
        var sl = go.AddComponent<MySideSlider>();
        var rect = Util.NormalizeRectWithTopLeft(sl, x, y, parent);
        sl.rectTrans = rect;

        sl.slider = go.transform.Find("slider").GetComponent<Slider>();
        sl.slider.minValue = 0f;
        sl.slider.maxValue = 100f;
        sl.slider.onValueChanged.RemoveAllListeners();
        sl.slider.onValueChanged.AddListener(sl.SliderChanged);
        if (width == 0) width = 160f;
        if (sl.slider.transform is RectTransform rectTrans)
        {
            rectTrans.localPosition = new Vector3(width, rectTrans.localPosition.y, rectTrans.localPosition.z);
            rectTrans.sizeDelta = new Vector2(textWidth <= 0f ? width + 5f : width, rectTrans.sizeDelta.y);
        }
        sl.Value = 0f;

        sl.labelText = go.transform.Find("value").GetComponent<Text>();
        sl.labelText.alignment = textWidth <= 0f ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        if (sl.labelText.transform is RectTransform rectTrans2)
        {
            if (textWidth > 0f)
            {
                rectTrans2.sizeDelta = new Vector2(textWidth, rectTrans2.sizeDelta.y);
            }
            rectTrans2.pivot = new Vector2(0f, 1f);
            rectTrans2.localPosition = new Vector3(width, rectTrans2.localPosition.y, rectTrans2.localPosition.z);
        }
        sl.labelFormat = "G";

        // var bg = sl.slider.transform.Find("Background")?.GetComponent<Image>();
        // if (bg != null)
        // {
        //     bg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        // }
        // var fill = sl.slider.fillRect.GetComponent<Image>();
        // if (fill != null)
        // {
        //     fill.color = new Color(1f, 1f, 1f, 0.28f);
        // }

        sl.UpdateLabel();

        return sl;
    }

    public void SetEnable(bool on)
    {
        lock (this)
        {
            if (slider) slider.interactable = on;
        }
    }

    public float Value
    {
        get => slider.value;
        set
        {
            var sliderVal = value;
            if (sliderVal.Equals(slider.value)) return;
            if (sliderVal > slider.maxValue)
            {
                sliderVal = slider.maxValue;
            }
            else if (sliderVal < slider.minValue)
            {
                sliderVal = slider.minValue;
            }

            slider.value = sliderVal;
            UpdateLabel();
        }
    }

    public MySideSlider WithValue(float value)
    {
        Value = value;
        return this;
    }

    public MySideSlider WithMinMaxValue(float min, float max)
    {
        slider.minValue = min;
        slider.maxValue = max;
        return this;
    }

    public MySideSlider WithLabelFormat(string format)
    {
        if (format == labelFormat) return this;
        labelFormat = format;
        UpdateLabel();
        return this;
    }

    public MySideSlider WithEnable(bool on)
    {
        SetEnable(on);
        return this;
    }

    public void UpdateLabel()
    {
        if (labelText != null)
        {
            labelText.text = slider.value.ToString(labelFormat);
        }
    }

    public void SetLabelText(string text)
    {
        if (labelText != null)
        {
            labelText.text = text;
        }
    }

    public void SliderChanged(float val)
    {
        lock (this)
        {
            UpdateLabel();
            OnValueChanged?.Invoke();
        }
    }
}
