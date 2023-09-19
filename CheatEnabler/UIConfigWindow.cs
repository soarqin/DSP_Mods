using UnityEngine;

namespace CheatEnabler;

public class UIConfigWindow : UI.MyWindowWithTabs
{
    private RectTransform _windowTrans;

    private readonly UIButton[] _dysonLayerBtn = new UIButton[10];

    static UIConfigWindow()
    {
        I18N.Add("General", "General", "常规");
        I18N.Add("Enable Dev Shortcuts", "Enable Dev Shortcuts", "开发模式快捷键");
        I18N.Add("Disable Abnormal Checks", "Disable Abnormal Checks", "关闭数据异常检查");
        I18N.Add("Hotkey", "Hotkey", "快捷键");
        I18N.Add("Unlock Tech with Key-Modifiers", "Unlock Tech with Key-Modifiers", "使用组合键点击解锁科技");
        I18N.Add("Dev Shortcuts", "Dev Shortcuts", "开发模式快捷键");
        I18N.Add("Dev Shortcuts Tips",
            "Caution: Some function may trigger abnormal check!\nNumpad 1: Gets all items and extends bag.\nNumpad 2: Boosts walk speed, gathering speed and mecha energy restoration.\nNumpad 3: Fills planet with foundations and bury all veins.\nNumpad 4: +1 construction drone.\nNumpad 5: Upgrades drone engine tech to full.\nNumpad 6: Unlocks researching tech.\nNumpad 7: Unlocks Drive Engine 1.\nNumpad 8: Unlocks Drive Engine 2 and maximize energy.\nNumpad 9: Unlocks ability to warp.\nNumpad 0: No costs for Logistic Storages' output.\nLCtrl + T: Unlocks all techs (not upgrades).\nLCtrl + A: Resets all local achievements.\nLCtrl + Q: Adds 10000 to every metadata.\nLCtrl + W: Enters Sandbox Mode.\nLCtrl + Shift + W: Leaves Sandbox Mode.\nNumpad *: Proliferates items on hand.\nNumpad /: Removes proliferations from items on hand.\nPageDown: Remembers Pose of game camera.\nPageUp: Locks game camera using remembered Pose.",
            "警告：某些功能可能触发异常检查!\n小键盘1：获得所有物品并扩展背包\n小键盘2：加快行走速度及采集速度，加快能量恢复速度\n小键盘3：将地基铺设整个星球并掩埋所有矿物\n小键盘4：建设机器人 +1\n小键盘5：建设机器人满级\n小键盘6：解锁当前科技\n小键盘7：解锁驱动技术I\n小键盘8：解锁驱动技术II 最大化能量\n小键盘9：机甲曲速解锁\n小键盘0：物流站通过传送带出物品无消耗\n左Ctrl + T：解锁所有非升级科技\n左Ctrl + A：重置所有本地成就\n左Ctrl + Q：增加各项元数据10000点\n左Ctrl + W：进入沙盒模式\n左Ctrl + Shift + W：离开沙盒模式\n小键盘乘号 *：给手上物品喷涂增产剂\n小键盘除号 /：清除手上物品的增产剂\nPageDown：记录摄像机当前的Pose\nPageUp：用记录的Pose锁定摄像机");
        I18N.Add("Unlock Tech with Key-Modifiers Tips",
            "Click tech on tree while holding:\n  Shift: Tech level + 1\n  Ctrl: Tech level + 10\n  Ctrl + Shift: Tech level + 100\n  Alt: Tech level to MAX\n\nNote: all direct prerequisites will be unlocked as well.",
            "按住以下组合键点击科技树：\n  Shift：科技等级+1\n  Ctrl：科技等级+10\n  Ctrl+Shift：科技等级+100\n  Alt：科技等级升到最大\n\n注意：所有直接前置科技也会被解锁");
        I18N.Add("Factory", "Factory", "工厂");
        I18N.Add("Finish build immediately", "Finish build immediately", "建造秒完成");
        I18N.Add("Architect mode", "Architect mode", "建筑师模式");
        I18N.Add("Unlimited interactive range", "Unlimited interactive range", "无限交互距离");
        I18N.Add("Build without condition", "Build without condition check", "无条件建造");
        I18N.Add("No collision", "No collision", "无碰撞");
        I18N.Add("Belt signal generator", "Belt signal generator", "传送带信号物品生成");
        I18N.Add("Count all raws and intermediates in statistics","Count all raw materials in statistics", "统计信息里计算所有原料和中间产物");
        I18N.Add("Night Light", "Sunlight at night", "夜间日光灯");
        I18N.Add("Remove power space limit", "Remove space limit for winds and geothermals", "移除风力发电和地热发电的间距限制");
        I18N.Add("Boost wind power", "Boost wind power(x100,000)", "提升风力发电(x100,000)");
        I18N.Add("Boost solar power", "Boost solar power(x100,000)", "提升太阳能发电(x100,000)");
        I18N.Add("Boost fuel power", "Boost fuel power(x50,000)", "提升燃料发电(x50,000)");
        I18N.Add("Boost fuel power 2", "(x20,000 for deuteron, x10,000 for antimatter)", "(氘核燃料棒x20,000，反物质燃料棒x10,000)");
        I18N.Add("Boost geothermal power", "Boost geothermal power(x50,000)", "提升地热发电(x50,000)");
        I18N.Add("Planet", "Planet", "行星");
        I18N.Add("Infinite Natural Resources", "Infinite Natural Resources", "自然资源采集不消耗");
        I18N.Add("Fast Mining", "Fast Mining", "高速采集");
        I18N.Add("Pump Anywhere", "Pump Anywhere", "平地抽水");
        I18N.Add("Initialize This Planet", "Initialize This Planet", "初始化本行星");
        I18N.Add("Dismantle All Buildings", "Dismantle All Buildings", "拆除所有建筑");
        I18N.Add("Dyson Sphere", "Dyson Sphere", "戴森球");
        I18N.Add("Skip bullet period", "Skip bullet period", "跳过子弹阶段");
        I18N.Add("Skip absorption period", "Skip absorption period", "跳过吸收阶段");
        I18N.Add("Quick absorb", "Quick absorb", "快速吸收");
        I18N.Add("Eject anyway", "Eject anyway", "全球弹射");
        I18N.Add("Overclock Ejectors", "Overclock Ejectors (10x)", "高速弹射器(10倍射速)");
        I18N.Add("Overclock Silos", "Overclock Silos (10x)", "高速发射井(10倍射速)");
        I18N.Add("Terraform without enough sands", "Terraform without enough sands", "沙土不够时依然可以整改地形");
        I18N.Add("Initialize Dyson Sphere", "Initialize Dyson Sphere", "初始化戴森球");
        I18N.Add("Click to dismantle selected layer", "Click to dismantle selected layer", "点击拆除对应的戴森壳");
        I18N.Add("Birth", "Birth Sys", "母星系");
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
        I18N.Apply();
    }

    public static UIConfigWindow CreateInstance()
    {
        return UI.MyWindowManager.CreateWindow<UIConfigWindow>("CEConfigWindow", "CheatEnabler Config");
    }

    public override void _OnCreate()
    {
        _windowTrans = GetComponent<RectTransform>();
        _windowTrans.sizeDelta = new Vector2(580f, 400f);

        CreateUI();
    }

    private void CreateUI()
    {
        // General tab
        var x = 0f;
        var y = 10f;
        var tab1 = AddTab(36f, 0, _windowTrans, "General");
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, DevShortcuts.Enabled, "Enable Dev Shortcuts");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, AbnormalDisabler.Enabled, "Disable Abnormal Checks");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab1, TechPatch.Enabled, "Unlock Tech with Key-Modifiers");
        y += 118f;
        UI.MyKeyBinder.CreateKeyBinder(x, y, tab1, CheatEnabler.Hotkey, "Hotkey");
        x = 156f;
        y = 16f;
        AddTipsButton(x, y, tab1, "Dev Shortcuts", "Dev Shortcuts Tips", "dev-shortcuts-tips");
        x += 52f;
        y += 72f;
        AddTipsButton(x, y, tab1, "Unlock Tech with Key-Modifiers", "Unlock Tech with Key-Modifiers Tips", "unlock-tech-tips");

        var tab2 = AddTab(136f, 1, _windowTrans, "Factory");
        x = 0f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.ImmediateEnabled, "Finish build immediately");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.ArchitectModeEnabled, "Architect mode");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.UnlimitInteractiveEnabled, "Unlimited interactive range");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.NoConditionEnabled, "Build without condition");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.NoCollisionEnabled, "No collision");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.NightLightEnabled, "Night Light");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.BeltSignalGeneratorEnabled, "Belt signal generator");
        y += 26f;
        x += 26f;
        var cb = UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountRecipeEnabled, "Count all raws and intermediates in statistics", 13);
        cb.gameObject.SetActive(FactoryPatch.BeltSignalGeneratorEnabled.Value);
        FactoryPatch.BeltSignalGeneratorEnabled.SettingChanged += (_, _) =>
        {
            cb.gameObject.SetActive(FactoryPatch.BeltSignalGeneratorEnabled.Value);
        };
        x = 240f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.RemovePowerSpaceLimitEnabled, "Remove power space limit");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.BoostWindPowerEnabled, "Boost wind power");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.BoostSolarPowerEnabled, "Boost solar power");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.BoostGeothermalPowerEnabled, "Boost geothermal power");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.BoostFuelPowerEnabled, "Boost fuel power");
        x += 32f;
        y += 26f;
        AddText(x, y, tab2, "Boost fuel power 2", 13);

        // Planet Tab
        var tab3 = AddTab(236f, 2, _windowTrans, "Planet");
        x = 0f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, ResourcePatch.InfiniteEnabled, "Infinite Natural Resources");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, ResourcePatch.FastEnabled, "Fast Mining");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, WaterPumperPatch.Enabled, "Pump Anywhere");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab3, TerraformPatch.Enabled, "Terraform without enough sands");
        x = 300f;
        y = 10f;
        AddButton(x, y, tab3, "矿物掩埋标题", 16, "button-bury-all", () => { PlanetFunctions.BuryAllVeins(true); });
        y += 36f;
        AddButton(x, y, tab3, "矿物还原标题", 16, "button-bury-restore-all", () => { PlanetFunctions.BuryAllVeins(false); });
        y += 36f;
        AddButton(x, y, tab3, "铺满地基提示", 16, "button-reform-all", () =>
        {
            var player = GameMain.mainPlayer;
            if (player == null) return;
            var reformTool = player.controller.actionBuild.reformTool;
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformAll(reformTool.brushType, reformTool.brushColor, reformTool.buryVeins);
        });
        y += 36f;
        AddButton(x, y, tab3, "还原地形提示", 16, "button-reform-revert-all", () =>
        {
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformRevert();
        });
        y += 36f;
        AddButton(x, y, tab3, "Initialize This Planet", 16, "button-init-planet", () => { PlanetFunctions.RecreatePlanet(true); });
        y += 36f;
        AddButton(x, y, tab3, "Dismantle All Buildings", 16, "button-dismantle-all", () => { PlanetFunctions.DismantleAll(false); });

        var tab4 = AddTab(336f, 3, _windowTrans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab4, DysonSpherePatch.SkipBulletEnabled, "Skip bullet period");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab4, DysonSpherePatch.SkipAbsorbEnabled, "Skip absorption period");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab4, DysonSpherePatch.QuickAbsortEnabled, "Quick absorb");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab4, DysonSpherePatch.EjectAnywayEnabled, "Eject anyway");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab4, DysonSpherePatch.OverclockEjectorEnabled, "Overclock Ejectors");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab4, DysonSpherePatch.OverclockSiloEnabled, "Overclock Silos");
        x = 300f;
        y = 10f;
        AddButton(x, y, tab4, "Initialize Dyson Sphere", 16, "init-dyson-sphere", () => { DysonSpherePatch.InitCurrentDysonSphere(-1); });
        y += 36f;
        AddText(x, y, tab4, "Click to dismantle selected layer", 16, "text-dismantle-layer");
        y += 26f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = AddFlatButton(x, y, tab4, id.ToString(), 12, "dismantle-layer-" + id, () => { DysonSpherePatch.InitCurrentDysonSphere(id); });
            ((RectTransform)btn.transform).sizeDelta = new Vector2(40f, 20f);
            _dysonLayerBtn[i] = btn;
            if (i == 4)
            {
                x -= 160f;
                y += 20f;
            }
            else
            {
                x += 40f;
            }
        }

        var tab5 = AddTab(436f, 4, _windowTrans, "Birth");
        x = 0f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.SitiVeinsOnBirthPlanet, "Silicon/Titanium on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.FireIceOnBirthPlanet, "Fire ice on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.KimberliteOnBirthPlanet, "Kimberlite on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.FractalOnBirthPlanet, "Fractal silicon on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.OrganicOnBirthPlanet, "Organic crystal on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.OpticalOnBirthPlanet, "Optical grating crystal on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.SpiniformOnBirthPlanet, "Spiniform stalagmite crystal on birth planet");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.UnipolarOnBirthPlanet, "Unipolar magnet on birth planet");
        x = 200f;
        y = 10f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.FlatBirthPlanet, "Birth planet is solid flat (no water at all)");
        y += 36f;
        UI.MyCheckBox.CreateCheckBox(x, y, tab5, BirthPlanetPatch.HighLuminosityBirthStar, "Birth star has high luminosity");

        SetCurrentTab(0);
        UpdateUI();
    }

    public void UpdateUI()
    {
        UpdateDysonShells();
    }

    private void UpdateDysonShells()
    {
        if (!Tabs[3].Item1.gameObject.activeSelf) return;
        var star = GameMain.localStar;
        if (star != null)
        {
            var dysonSpheres = GameMain.data?.dysonSpheres;
            if (dysonSpheres?[star.index] != null)
            {
                var ds = dysonSpheres[star.index];
                for (var i = 1; i <= 10; i++)
                {
                    var layer = ds.layersIdBased[i];
                    _dysonLayerBtn[i - 1].button.interactable = layer != null && layer.id == i;
                }

                return;
            }
        }

        for (var i = 0; i < 10; i++)
        {
            _dysonLayerBtn[i].button.interactable = false;
        }
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
        base._OnUpdate();
        if (VFInput.escape && !VFInput.inputing)
        {
            VFInput.UseEscape();
            _Close();
            return;
        }

        UpdateUI();
    }
}