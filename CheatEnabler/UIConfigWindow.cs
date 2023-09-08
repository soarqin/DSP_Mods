using UnityEngine;

namespace CheatEnabler;

public class UIConfigWindow : UI.MyWindowWithTabs
{
    private RectTransform _windowTrans;

    static UIConfigWindow()
    {
        I18N.Add("General", "General", "常规");
        I18N.Add("Enable Dev Shortcuts", "Enable Dev Shortcuts", "启用开发模式快捷键");
        I18N.Add("Disable Abnormal Checks", "Disable Abnormal Checks", "关闭数据异常检查");
        I18N.Add("Planet", "Planet", "行星");
        I18N.Add("Infinite Natural Resources", "Infinite Natural Resources", "自然资源采集不消耗");
        I18N.Add("Fast Mining", "Fast Mining", "高速采集");
        I18N.Add("Pump Anywhere", "Pump Anywhere", "平地抽水");
        I18N.Add("Terraform without enought sands", "Terraform without enough sands", "沙土不够时依然可以整改地形");
        I18N.Apply();
    }

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
        var y = 10f;
        var tab1 = AddTab(36f, 0, _windowTrans, "General".Translate());
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, DevShortcuts.Enabled, "Enable Dev Shortcuts".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, AbnormalDisabler.Enabled, "Disable Abnormal Checks".Translate());

        // Planet Tab
        var tab2 = AddTab(136f, 1, _windowTrans, "Planet".Translate());
        x = 0f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, ResourcePatch.InfiniteEnabled, "Infinite Natural Resources".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, ResourcePatch.FastEnabled, "Fast Mining".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, WaterPumperPatch.Enabled, "Pump Anywhere".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, WaterPumperPatch.Enabled, "Terraform without enought sands".Translate());
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
