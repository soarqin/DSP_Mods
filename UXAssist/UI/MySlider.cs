using System;
using UnityEngine;
using UnityEngine.UI;

namespace UXAssist.UI;

// MySlider modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MySlider.cs

public class MySlider : MonoBehaviour
{
    public RectTransform rectTrans;
    public Slider slider;
    public RectTransform handleSlideArea;
    public Text labelText;
    public string labelFormat;
    public event Action OnValueChanged;

    public static MySlider CreateSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f)
    {
        return CreateSlider(x, y, parent, width).WithLabelFormat(format).WithMinMaxValue(minValue, maxValue).WithValue(value);
    }

    public static MySlider CreateSlider(float x, float y, RectTransform parent, float width = 0f)
    {
        var optionWindow = UIRoot.instance.optionWindow;
        var src = optionWindow.audioVolumeComp;

        var go = Instantiate(src.gameObject);
        //sizeDelta = 240, 20
        go.name = "my-slider";
        go.SetActive(true);
        var sl = go.AddComponent<MySlider>();
        var rect = Util.NormalizeRectWithTopLeft(sl, x, y, parent);
        sl.rectTrans = rect;
        if (width > 0)
        {
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

        sl.slider = go.GetComponent<Slider>();
        sl.slider.minValue = 0f;
        sl.slider.maxValue = 100f;
        sl.slider.onValueChanged.RemoveAllListeners();
        sl.slider.onValueChanged.AddListener(sl.SliderChanged);
        sl.Value = 0f;
        sl.labelText = sl.slider.handleRect.Find("Text")?.GetComponent<Text>();
        if (sl.labelText)
        {
            sl.labelText.fontSize = 14;
            if (sl.labelText.transform is RectTransform rectTrans)
            {
                rectTrans.sizeDelta = new Vector2(22f, 22f);
            }
        }
        sl.labelFormat = "G";

        sl.handleSlideArea = sl.transform.Find("Handle Slide Area")?.GetComponent<RectTransform>();

        var bg = sl.slider.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        var fill = sl.slider.fillRect.GetComponent<Image>();
        if (fill != null)
        {
            fill.color = new Color(1f, 1f, 1f, 0.28f);
        }
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

    public MySlider WithValue(float value)
    {
        Value = value;
        return this;
    }

    public MySlider WithMinMaxValue(float min, float max)
    {
        slider.minValue = min;
        slider.maxValue = max;
        return this;
    }

    public MySlider WithLabelFormat(string format)
    {
        if (format == labelFormat) return this;
        labelFormat = format;
        UpdateLabel();
        return this;
    }

    public MySlider WithEnable(bool on)
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

    public MySlider WithSmallerHandle(float deltaX = 10f, float deltaY = 0f)
    {
        var oldSize = slider.handleRect.sizeDelta;
        slider.handleRect.sizeDelta = new Vector2(oldSize.x - deltaX, oldSize.y - deltaY);
        handleSlideArea.offsetMin = new Vector2(handleSlideArea.offsetMin.x - deltaX / 2, handleSlideArea.offsetMin.y);
        handleSlideArea.offsetMax = new Vector2(handleSlideArea.offsetMax.x + deltaX / 2, handleSlideArea.offsetMax.y);
        return this;
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
