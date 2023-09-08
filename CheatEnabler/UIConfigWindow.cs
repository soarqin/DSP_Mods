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
        I18N.Add("Birth", "Birth Sys", "母星系");
        I18N.Add("Terraform without enought sands", "Terraform without enough sands", "沙土不够时依然可以整改地形");
        I18N.Add("Silicon/Titanium on birth planet", "Silicon/Titanium on birth planet", "母星有硅和钛");
        I18N.Add("Fire ice on birth planet", "Fire ice on birth planet", "母星有可燃冰");
        I18N.Add("Kimberlite on birth planet", "Kimberlite on birth planet", "母星有金伯利矿");
        I18N.Add("Fractal silicon on birth planet", "Fractal silicon on birth planet", "母星有分形硅");
        I18N.Add("Organic crystal on birth planet", "Organic crystal on birth planet", "母星有有机晶体");
        I18N.Add("Optical grating crystal on birth planet", "Optical grating crystal on birth planet", "母星有光栅石");
        I18N.Add("Spiniform stalagmite crystal on birth planet", "Spiniform stalagmite crystal on birth planet", "母星有刺笋结晶");
        I18N.Add("Unipolar magnet on birth planet", "Unipolar magnet on birth planet", "母星有单极磁石");
        I18N.Add("Birth planet is solid flat (no water at all)", "Birth planet is solid flat (no water at all)", "母星是纯平的（没有水）");
        I18N.Add("Birth star has high luminosity", "Birth star has high luminosity", "母星系恒星高亮");
        I18N.Add("Initialize This Planet", "Initialize This Planet", "初始化本行星");
        I18N.Add("Dismantle All Buildings", "Dismantle All Buildings", "拆除所有建筑");
        I18N.Apply();
    }

    public static UIConfigWindow CreateInstance()
    {
        return UI.MyWindowManager.CreateWindow<UIConfigWindow>("CEConfigWindow", "CheatEnabler Config".Translate());
    }

    public override void _OnCreate()
    {
        _windowTrans = GetComponent<RectTransform>();
        _windowTrans.sizeDelta = new Vector2(480f, 331f);

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
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, TerraformPatch.Enabled, "Terraform without enought sands".Translate());
        x = 300f;
        y = 10f;
        AddButton(x, y, tab2, "矿物掩埋标题", 16, "button-bury-all", () =>
        {
            PlanetFunctions.BuryAllVeins(true);
        });
        y += 36f;
        AddButton(x, y, tab2, "矿物还原标题", 16, "button-bury-restore-all", () =>
        {
            PlanetFunctions.BuryAllVeins(false);
        });
        y += 36f;
        AddButton(x, y, tab2, "铺满地基提示", 16, "button-reform-all", () =>
        {
            var player = GameMain.mainPlayer;
            if (player == null) return;
            var reformTool = player.controller.actionBuild.reformTool;
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformAll(reformTool.brushType, reformTool.brushColor, reformTool.buryVeins);
        });
        y += 36f;
        AddButton(x, y, tab2, "还原地形提示", 16, "button-reform-revert-all", () =>
        {
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformRevert();
        });
        y += 36f;
        AddButton(x, y, tab2, "Initialize This Planet", 16, "button-init-planet", () =>
        {
            PlanetFunctions.RecreatePlanet(true);
        });
        y += 36f;
        AddButton(x, y, tab2, "Dismantle All Buildings", 16, "button-dismantle-all", () =>
        {
            PlanetFunctions.DismantleAll(false);
        });

        var tab3 = AddTab(236f, 2, _windowTrans, "Birth".Translate());
        x = 0f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.SitiVeinsOnBirthPlanet, "Silicon/Titanium on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.FireIceOnBirthPlanet, "Fire ice on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.KimberliteOnBirthPlanet, "Kimberlite on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.FractalOnBirthPlanet, "Fractal silicon on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.OrganicOnBirthPlanet, "Organic crystal on birth planet".Translate());
        x = 200f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.OpticalOnBirthPlanet, "Optical grating crystal on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.SpiniformOnBirthPlanet, "Spiniform stalagmite crystal on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.UnipolarOnBirthPlanet, "Unipolar magnet on birth planet".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.FlatBirthPlanet, "Birth planet is solid flat (no water at all)".Translate());
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, BirthPlanetPatch.HighLuminosityBirthStar, "Birth star has high luminosity".Translate());
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