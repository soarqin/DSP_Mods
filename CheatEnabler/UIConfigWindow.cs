using UnityEngine;

namespace CheatEnabler;

public class UIConfigWindow : UI.MyWindowWithTabs
{
    private RectTransform _windowTrans;

    public static UIConfigWindow CreateInstance()
    {
        return UI.MyWindowManager.CreateWindow<UIConfigWindow>("CEConfigWindow", "CheatEnabler Config".Translate());
    }

    public override void _OnCreate()
    {
        _windowTrans = GetComponent<RectTransform>();
        _windowTrans.sizeDelta = new Vector2(640f, 428f);

        CreateUI();
    }

    private void CreateUI()
    {
        // General tab
        var x = 0f;
        var y = 0f;
        var tab1 = AddTab(36f, 0, _windowTrans, "General".Translate());
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, DevShortcuts.Enabled, "Enable Dev Shortcuts".Translate());
        y += 26f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, AbnormalDisabler.Enabled, "Disable Abnormal Checks".Translate());

        // Planet Tab
        var tab2 = AddTab(136f, 1, _windowTrans, "Planet".Translate());
        x = 0f;
        y = 0f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, ResourcePatch.InfiniteEnabled, "Infinite Natural Resources".Translate());
        y += 26f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, ResourcePatch.FastEnabled, "Fast Mining".Translate());
        y += 26f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, WaterPumperPatch.Enabled, "Pump Anywhere".Translate());
        SetCurrentTab(0);
    }

    public override void _OnDestroy()
    {
    }

    public override bool _OnInit()
    {
        _windowTrans.anchoredPosition = new Vector2(0, 0);
        return true;
    }

    public override void _OnFree()
    {
    }

    public override void _OnOpen()
    {
    }

    public override void _OnClose()
    {
    }

    public override void _OnUpdate()
    {
        if (!VFInput.escape || VFInput.inputing) return;
        VFInput.UseEscape();
        _Close();
    }
}
