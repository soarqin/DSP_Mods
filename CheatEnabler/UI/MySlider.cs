using System;
using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler.UI;

// MySlider modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MySlider.cs

public class MySlider : MonoBehaviour
{
    public Slider slider;
    public RectTransform rectTrans;
    public Text labelText;
    public string labelFormat;
    public event Action OnValueChanged;
    private float _value;
    public float Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueSet();
        }
    }

    public static RectTransform CreateSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "{0}", float width = 0f)
    {
        var optionWindow = UIRoot.instance.optionWindow;
        var src = optionWindow.audioVolumeComp;

        var go = Instantiate(src.gameObject);
        //sizeDelta = 240, 20
        go.name = "my-slider";
        go.SetActive(true);
        var sl = go.AddComponent<MySlider>();
        sl._value = value;
        var rect = Util.NormalizeRectWithTopLeft(sl, x, y, parent);
        sl.rectTrans = rect;
        if (width > 0)
        {
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

        sl.slider = go.GetComponent<Slider>();
        sl.slider.minValue = minValue;
        sl.slider.maxValue = maxValue;
        sl.slider.onValueChanged.RemoveAllListeners();
        sl.slider.onValueChanged.AddListener(sl.SliderChanged);
        sl.labelText = sl.slider.handleRect.Find("Text")?.GetComponent<Text>();
        if (sl.labelText != null)
        {
            sl.labelText.fontSize = 14;
            if (sl.labelText.transform is RectTransform rectTrans)
            {
                rectTrans.sizeDelta = new Vector2(22f, 22f);
            }
        }
        sl.labelFormat = format;

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
        sl.OnValueSet();
        sl.UpdateLabel();

        return sl.rectTrans;
    }
    public void OnValueSet()
    {
        lock (this)
        {
            var sliderVal = _value;
            if (_value.Equals(slider.value)) return;
            if (sliderVal > slider.maxValue)
            {
                _value = sliderVal = slider.maxValue;
            }
            else if (sliderVal < slider.minValue)
            {
                _value = sliderVal = slider.minValue;
            }

            slider.value = sliderVal;
            UpdateLabel();
        }
    }
    public void UpdateLabel()
    {
        if (labelText != null)
        {
            labelText.text = _value.ToString(labelFormat);
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
            var newVal = Mathf.Round(slider.value);
            if (_value.Equals(newVal)) return;
            _value = newVal;
            UpdateLabel();
            OnValueChanged?.Invoke();
        }
    }

}