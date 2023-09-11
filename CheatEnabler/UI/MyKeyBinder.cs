using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler.UI;

// MyKeyBinder modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyKeyBinder.cs
public class MyKeyBinder : MonoBehaviour
{
    private ConfigEntry<KeyboardShortcut> _config;

    [SerializeField]
    public Text functionText;

    [SerializeField]
    public Text keyText;

    [SerializeField]
    public InputField setTheKeyInput;

    [SerializeField]
    public Toggle setTheKeyToggle;

    [SerializeField]
    public RectTransform rectTrans;

    [SerializeField]
    public UIButton inputUIButton;

    [SerializeField]
    public Text conflictText;

    [SerializeField]
    public Text waitingText;

    [SerializeField]
    public UIButton setDefaultUIButton;

    [SerializeField]
    public UIButton setNoneKeyUIButton;

    private bool _nextNotOn;

    public static RectTransform CreateKeyBinder(float x, float y, RectTransform parent, ConfigEntry<KeyboardShortcut> config, string label = "", int fontSize = 17)
    {
        var optionWindow = UIRoot.instance.optionWindow;
        var uikeyEntry = Instantiate(optionWindow.entryPrefab);
        GameObject go;
        (go = uikeyEntry.gameObject).SetActive(true);
        go.name = "my-keybinder";
        var kb = go.AddComponent<MyKeyBinder>();
        kb._config = config;

        kb.functionText = uikeyEntry.functionText;
        kb.keyText = uikeyEntry.keyText;
        kb.setTheKeyInput = uikeyEntry.setTheKeyInput;
        kb.setTheKeyToggle = uikeyEntry.setTheKeyToggle;
        kb.rectTrans = uikeyEntry.rectTrans;
        kb.inputUIButton = uikeyEntry.inputUIButton;
        kb.conflictText = uikeyEntry.conflictText;
        kb.waitingText = uikeyEntry.waitingText;
        kb.setDefaultUIButton = uikeyEntry.setDefaultUIButton;
        kb.setNoneKeyUIButton = uikeyEntry.setNoneKeyUIButton;


        kb.functionText.text = label.Translate();
        kb.functionText.fontSize = 17;

        ((RectTransform)kb.keyText.transform).anchoredPosition = new Vector2(20f, -27f);
        //kb.keyText.alignment = TextAnchor.MiddleRight;
        kb.keyText.fontSize = 17;
        ((RectTransform)kb.inputUIButton.transform.parent.transform).anchoredPosition = new Vector2(0f + 20f, -57f);
        ((RectTransform)kb.setDefaultUIButton.transform).anchoredPosition = new Vector2(140f + 20f, -57f);
        ((RectTransform)kb.setNoneKeyUIButton.transform).anchoredPosition = new Vector2(240f + 20f, -57f);

        var rect = Util.NormalizeRectWithTopLeft(kb, x, y, parent);
        kb.rectTrans = rect;

        //rect.sizeDelta = new Vector2(240f, 64f);
        Destroy(uikeyEntry);
        kb.setNoneKeyUIButton.gameObject.SetActive(false);

        kb.SettingChanged();
        config.SettingChanged += (_, _) => {
            kb.SettingChanged();
        };
        kb.inputUIButton.onClick += kb.OnInputUIButtonClick;
        kb.setDefaultUIButton.onClick += kb.OnSetDefaultKeyClick;
        //kb.setNoneKeyUIButton.onClick += kb.OnSetNoneKeyClick;
        return go.transform as RectTransform;
    }

    private void Update()
    {
        if (!setTheKeyToggle.isOn && inputUIButton.highlighted)
        {
            setTheKeyToggle.isOn = true;
        }

        if (!setTheKeyToggle.isOn) return;
        if (!inputUIButton._isPointerEnter && Input.GetKeyDown(KeyCode.Mouse0))
        {
            inputUIButton.highlighted = false;
            setTheKeyToggle.isOn = false;
            Reset();
        }
        else if (!this.inputUIButton.highlighted)
        {
            setTheKeyToggle.isOn = false;
            Reset();
        }
        else
        {
            waitingText.gameObject.SetActive(true);
            if (!TrySetValue()) return;
            setTheKeyToggle.isOn = false;
            inputUIButton.highlighted = false;
            Reset();
        }
    }

    
    public bool TrySetValue()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            VFInput.UseEscape();
            return true;
        }
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
        {
            return true;
        }
        var anyKey = GetIunptKeys();
        if (anyKey || _lastKey == KeyCode.None) return false;
        var k = GetPressedKey();
        if (string.IsNullOrEmpty(k))
        {
            return false;
        }
        _lastKey = KeyCode.None;

        _config.Value = KeyboardShortcut.Deserialize(k);
        //keyText.text = k;
        return true;

    }

    private KeyCode _lastKey;
    private static readonly KeyCode[] ModKeys = { KeyCode.RightShift, KeyCode.LeftShift,
             KeyCode.RightControl, KeyCode.LeftControl,
             KeyCode.RightAlt, KeyCode.LeftAlt,
             KeyCode.LeftCommand,  KeyCode.LeftApple, KeyCode.LeftWindows,
             KeyCode.RightCommand,  KeyCode.RightApple, KeyCode.RightWindows };

    public string GetPressedKey()
    {
        var key = _lastKey.ToString();
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }
        var mod = "";
        foreach (var modKey in ModKeys)
        {
            if (Input.GetKey(modKey))
            {
                mod += "+" + modKey.ToString();
            }
        }

        if (!string.IsNullOrEmpty(mod))
        {
            key += mod;
        }
        return key;
    }

    //通常キーが押されているかチェック _lastKey に保存
    public bool GetIunptKeys()
    {
        var anyKey = false;

        foreach (KeyCode item in Enum.GetValues(typeof(KeyCode)))
        {
            if (item == KeyCode.None || ModKeys.Contains(item) || !Input.GetKey(item)) continue;
            _lastKey = item;
            anyKey = true;
        }
        return anyKey;

    }

    public void Reset()
    {
        conflictText.gameObject.SetActive(false);
        waitingText.gameObject.SetActive(false);
        setDefaultUIButton.button.Select(); // InputFieldのフォーカス外す
        _lastKey = KeyCode.None;
    }

    public void OnInputUIButtonClick(int data)
    {
        inputUIButton.highlighted = true;

        if (!_nextNotOn) return;
        _nextNotOn = false;
        inputUIButton.highlighted = false;
        setTheKeyToggle.isOn = false;
        waitingText.gameObject.SetActive(false);
    }

    public void OnSetDefaultKeyClick(int data)
    {
        _config.Value = (KeyboardShortcut)_config.DefaultValue;
        keyText.text = _config.Value.Serialize();
    }

    public void OnSetNoneKeyClick(int data)
    {
        _config.Value = (KeyboardShortcut)_config.DefaultValue;
        keyText.text = _config.Value.Serialize();
    }

    public void SettingChanged()
    {
        keyText.text = _config.Value.Serialize();
    }
}
