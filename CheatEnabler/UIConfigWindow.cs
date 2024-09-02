using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;

namespace CheatEnabler;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;

    private static UIButton _resignGameBtn;
    private static UIButton _clearBanBtn;

    public static void Init()
    {
        I18N.Add("Factory", "Factory", "工厂");
        I18N.Add("Planet", "Planet", "行星");
        I18N.Add("Mecha/Combat", "Mecha/Combat", "机甲/战斗");
        I18N.Add("Enable Dev Shortcuts", "Enable Dev Shortcuts", "开发模式快捷键");
        I18N.Add("Disable Abnormal Checks", "Disable Abnormal Checks", "关闭数据异常检查");
        I18N.Add("Hotkey", "Hotkey", "快捷键");
        I18N.Add("Unlock Tech with Key-Modifiers", "Unlock Tech with Key-Modifiers", "使用组合键点击解锁科技");
        I18N.Add("Dev Shortcuts", "Dev Shortcuts", "开发模式快捷键");
        I18N.Add("Dev Shortcuts Tips",
            "Caution: Some function may trigger abnormal check!\nNumpad 1: Gets all items and extends bag.\nNumpad 2: Boosts walk speed, gathering speed and mecha energy restoration.\nNumpad 3: Fills planet with foundations and bury all veins.\nNumpad 4: +1 construction drone.\nNumpad 5: Upgrades drone engine tech to full.\nNumpad 6: Unlocks researching tech.\nNumpad 7: Unlocks Drive Engine 1.\nNumpad 8: Unlocks Drive Engine 2 and maximize energy.\nNumpad 9: Unlocks ability to warp.\nNumpad 0: No costs for Logistic Storages' output.\nLCtrl + T: Unlocks all techs (not upgrades).\nLCtrl + Q: Adds 10000 to every metadata.\nLCtrl + W: Enters Sandbox Mode.\nLCtrl + Shift + W: Leaves Sandbox Mode.\nNumpad *: Proliferates items on hand.\nNumpad /: Removes proliferations from items on hand.\nPageDown: Remembers Pose of game camera.\nPageUp: Locks game camera using remembered Pose.",
            "警告：某些功能可能触发异常检查!\n小键盘1：获得所有物品并扩展背包\n小键盘2：加快行走速度及采集速度，加快能量恢复速度\n小键盘3：将地基铺设整个星球并掩埋所有矿物\n小键盘4：建设机器人 +1\n小键盘5：建设机器人满级\n小键盘6：解锁当前科技\n小键盘7：解锁驱动技术I\n小键盘8：解锁驱动技术II 最大化能量\n小键盘9：机甲曲速解锁\n小键盘0：物流站通过传送带出物品无消耗\n左Ctrl + T：解锁所有非升级科技\n左Ctrl + Q：增加各项元数据10000点\n左Ctrl + W：进入沙盒模式\n左Ctrl + Shift + W：离开沙盒模式\n小键盘乘号 *：给手上物品喷涂增产剂\n小键盘除号 /：清除手上物品的增产剂\nPageDown：记录摄像机当前的Pose\nPageUp：用记录的Pose锁定摄像机");
        I18N.Add("Unlock Tech with Key-Modifiers Tips",
            "Click tech on tree while holding:\n  Shift: Tech level + 1\n  Ctrl: Tech level + 10\n  Ctrl + Shift: Tech level + 100\n  Alt: Tech level to MAX\n\nNote: all direct prerequisites will be unlocked as well.",
            "按住以下组合键点击科技树：\n  Shift：科技等级+1\n  Ctrl：科技等级+10\n  Ctrl+Shift：科技等级+100\n  Alt：科技等级升到最大\n\n注意：所有直接前置科技也会被解锁");
        I18N.Add("Remove all metadata consumption records", "Remove all metadata consumption records", "移除所有元数据消耗记录");
        I18N.Add("Remove metadata consumption record in current game", "Remove metadata consumption record in current game", "移除当前存档的元数据消耗记录");
        I18N.Add("Clear metadata flag which bans achievements", "Clear metadata flag which bans achievements in current game", "解除当前存档因使用元数据导致的成就限制");
        I18N.Add("Assign gamesave to current account", "Assign gamesave to current account", "将游戏存档绑定给当前账号");
        I18N.Add("Finish build immediately", "Finish build immediately", "建造秒完成");
        I18N.Add("Architect mode", "Architect mode", "建筑师模式");
        I18N.Add("Build without condition", "Build without condition check", "无条件建造");
        I18N.Add("Build without condition is enabled!", "!!Build without condition is enabled!!", "!!无条件建造已开启！！");
        I18N.Add("No collision", "No collision", "无碰撞");
        I18N.Add("Belt signal generator", "Belt signal generator", "传送带信号物品生成");
        I18N.Add("Belt signal alt format", "Belt signal alt format", "传送带信号替换格式");
        I18N.Add("Belt signal alt format tips",
            "Belt signal number format alternative format:\n  AAAABC by default\n  BCAAAA as alternative\nAAAA=generation speed in minutes, B=proliferate points, C=stack count",
            "传送带信号物品生成数量格式：\n  默认为AAAABC\n  勾选替换为BCAAAA\nAAAA=生成速度，B=增产点数，C=堆叠数量");
        I18N.Add("Count generations as production in statistics", "Count generations as production in statistics", "统计信息里将生成计算为产物");
        I18N.Add("Count removals as consumption in statistics", "Count removals as consumption in statistics", "统计信息里将移除计算为消耗");
        I18N.Add("Count all raws and intermediates in statistics","Count all raw materials in statistics", "统计信息里计算所有原料和中间产物");
        I18N.Add("Remove power space limit", "Remove space limit for winds and geothermals", "移除风力发电和地热发电的间距限制");
        I18N.Add("Boost wind power", "Boost wind power(x100,000)", "提升风力发电(x100,000)");
        I18N.Add("Boost solar power", "Boost solar power(x100,000)", "提升太阳能发电(x100,000)");
        I18N.Add("Boost fuel power", "Boost fuel power(x50,000)", "提升燃料发电(x50,000)");
        I18N.Add("Boost fuel power 2", "(x20,000 for deuteron, x10,000 for antimatter)", "(氘核燃料棒x20,000，反物质燃料棒x10,000)");
        I18N.Add("Boost geothermal power", "Boost geothermal power(x50,000)", "提升地热发电(x50,000)");
        I18N.Add("Increase maximum power usage in Logistic Stations and Advanced Mining Machines", "Increase maximum power usage in Logistic Stations and Advanced Mining Machines", "提升物流塔和大型采矿机的最大功耗");
        I18N.Add("Retrieve/Place items from/to remote planets on logistics control panel", "Retrieve/Place items from/to remote planets on logistics control panel", "在物流总控面板上可以从非本地行星取放物品");
        I18N.Add("Infinite Natural Resources", "Infinite natural resources", "自然资源采集不消耗");
        I18N.Add("Fast Mining", "Fast mining", "高速采集");
        I18N.Add("Pump Anywhere", "Pump anywhere", "平地抽水");
        I18N.Add("Skip bullet period", "Skip bullet period", "跳过子弹阶段");
        I18N.Add("Skip absorption period", "Skip absorption period", "跳过吸收阶段");
        I18N.Add("Quick absorb", "Quick absorb", "快速吸收");
        I18N.Add("Eject anyway", "Eject anyway", "全球弹射");
        I18N.Add("Overclock Ejectors", "Overclock Ejectors (10x)", "高速弹射器(10倍射速)");
        I18N.Add("Overclock Silos", "Overclock Silos (10x)", "高速发射井(10倍射速)");
        I18N.Add("Complete Dyson Sphere shells instantly", "Complete Dyson Sphere shells instantly", "立即完成戴森壳建造");
        I18N.Add("Terraform without enough soil piles", "Terraform without enough soil piles", "沙土不够时依然可以整改地形");
        I18N.Add("Instant teleport (like that in Sandbox mode)", "Instant teleport (like that in Sandbox mode)", "快速传送(和沙盒模式一样)");
        I18N.Add("Mecha and Drones/Fleets invicible", "Mecha and Drones/Fleets invicible", "机甲和战斗无人机无敌");
        I18N.Add("Buildings invicible", "Buildings invincible", "建筑无敌");
        I18N.Add("Enable warp without space warpers", "Enable warp without space warpers", "无需空间翘曲器即可曲速飞行");
        I18N.Add("Teleport to outer space", "Teleport to outer space", "传送到外太空");
        I18N.Add("Teleport to selected astronomical", "Teleport to selected astronomical", "传送到选中的天体");
        I18N.Apply();
        MyConfigWindow.OnUICreated += CreateUI;
        MyConfigWindow.OnUpdateUI += UpdateUI;
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        _windowTrans = trans;
        // General tab
        var x = 0f;
        var y = 10f;
        wnd.AddSplitter(trans, 10f);
        wnd.AddTabGroup(trans, "Cheat Enabler", "tab-group-cheatenabler");
        var tab1 = wnd.AddTab(_windowTrans, "General");
        var cb = wnd.AddCheckBox(x, y, tab1, DevShortcuts.Enabled, "Enable Dev Shortcuts");
        x += cb.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Dev Shortcuts", "Dev Shortcuts Tips", "dev-shortcuts-tips");
        x = 0;
        y += 30f;
        wnd.AddCheckBox(x, y, tab1, AbnormalDisabler.Enabled, "Disable Abnormal Checks");
        y += 36f;
        cb = wnd.AddCheckBox(x, y, tab1, TechPatch.Enabled, "Unlock Tech with Key-Modifiers");
        x += cb.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Unlock Tech with Key-Modifiers", "Unlock Tech with Key-Modifiers Tips", "unlock-tech-tips");
        x = 0f;
        y += 30f + 36f;
        wnd.AddButton(x, y, 400f, tab1, "Remove all metadata consumption records", 16, "button-remove-all-metadata-consumption", PlayerFunctions.RemoveAllMetadataConsumptions);
        y += 36f;
        wnd.AddButton(x, y, 400f, tab1, "Remove metadata consumption record in current game", 16, "button-remove-current-metadata-consumption", PlayerFunctions.RemoveCurrentMetadataConsumptions);
        y += 36f;
        _clearBanBtn = wnd.AddButton(x, y, 400f, tab1, "Clear metadata flag which bans achievements", 16, "button-clear-ban-list", PlayerFunctions.ClearMetadataBanAchievements);
        x = 300f;
        y = 10f;
        _resignGameBtn = wnd.AddButton(x, y, 300f, tab1, "Assign gamesave to current account", 16, "resign-game-btn", () => { GameMain.data.account = AccountData.me; });

        var tab2 = wnd.AddTab(_windowTrans, "Factory");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.ImmediateEnabled, "Finish build immediately");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.ArchitectModeEnabled, "Architect mode");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.NoConditionEnabled, "Build without condition");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.NoCollisionEnabled, "No collision");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalGeneratorEnabled, "Belt signal generator");
        x += 26f;
        y += 26f;
        var cb1 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountGenEnabled, "Count generations as production in statistics", 13);
        y += 26f;
        var cb2 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountRemEnabled, "Count removals as consumption in statistics", 13);
        y += 26f;
        var cb3 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountRecipeEnabled, "Count all raws and intermediates in statistics", 13);
        y += 26f;
        var cb4 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalNumberAltFormat, "Belt signal alt format", 13);
        x += cb4.Width + 5f;
        y += 6f;
        var tip1 = wnd.AddTipsButton2(x, y, tab2, "Belt signal alt format", "Belt signal alt format tips", "belt-signal-alt-format-tips");
        x = 0f;
        y += 30f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.GreaterPowerUsageInLogisticsEnabled, "Increase maximum power usage in Logistic Stations and Advanced Mining Machines");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.ControlPanelRemoteLogisticsEnabled, "Retrieve/Place items from/to remote planets on logistics control panel");

        FactoryPatch.BeltSignalGeneratorEnabled.SettingChanged += (_, _) =>
        {
            OnBeltSignalChanged();
        };
        OnBeltSignalChanged();
        x = 350f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.RemovePowerSpaceLimitEnabled, "Remove power space limit");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostWindPowerEnabled, "Boost wind power");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostSolarPowerEnabled, "Boost solar power");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostGeothermalPowerEnabled, "Boost geothermal power");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostFuelPowerEnabled, "Boost fuel power");
        x += 32f;
        y += 26f;
        wnd.AddText2(x, y, tab2, "Boost fuel power 2", 13);

        // Planet Tab
        var tab3 = wnd.AddTab(_windowTrans, "Planet");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab3, ResourcePatch.InfiniteResourceEnabled, "Infinite Natural Resources");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, ResourcePatch.FastMiningEnabled, "Fast Mining");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlanetPatch.WaterPumpAnywhereEnabled, "Pump Anywhere");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlanetPatch.TerraformAnywayEnabled, "Terraform without enough soil piles");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.InstantTeleportEnabled, "Instant teleport (like that in Sandbox mode)");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, 200f, tab3, "矿物掩埋标题", 16, "button-bury-all", () => { PlanetFunctions.BuryAllVeins(true); });
        y += 36f;
        wnd.AddButton(x, y, 200f, tab3, "矿物还原标题", 16, "button-bury-restore-all", () => { PlanetFunctions.BuryAllVeins(false); });
        y += 36f;
        wnd.AddButton(x, y, 200f, tab3, "铺满地基提示", 16, "button-reform-all", () =>
        {
            var player = GameMain.mainPlayer;
            if (player == null) return;
            var reformTool = player.controller.actionBuild.reformTool;
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformAll(reformTool.brushType, reformTool.brushColor, reformTool.buryVeins);
        });
        y += 36f;
        wnd.AddButton(x, y, 200f, tab3, "还原地形提示", 16, "button-reform-revert-all", () =>
        {
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformRevert();
        });

        var tab4 = wnd.AddTab(_windowTrans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.SkipBulletEnabled, "Skip bullet period");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.SkipAbsorbEnabled, "Skip absorption period");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.QuickAbsorbEnabled, "Quick absorb");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.EjectAnywayEnabled, "Eject anyway");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.OverclockEjectorEnabled, "Overclock Ejectors");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.OverclockSiloEnabled, "Overclock Silos");
        x = 300f;
        y = 10f;
        wnd.AddButton(x, y, 300f, tab4, "Complete Dyson Sphere shells instantly", 16, "button-complete-dyson-sphere-shells-instantly", DysonSphereFunctions.CompleteShellsInstantly);

        var tab5 = wnd.AddTab(_windowTrans, "Mecha/Combat");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab5, CombatPatch.MechaInvincibleEnabled, "Mecha and Drones/Fleets invicible");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, CombatPatch.BuildingsInvincibleEnabled, "Buildings invicible");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, PlayerPatch.WarpWithoutSpaceWarpersEnabled, "Enable warp without space warpers");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, 200f, tab5, "Teleport to outer space", 16, "button-teleport-to-outer-space", PlayerFunctions.TeleportToOuterSpace);
        y += 36f;
        wnd.AddButton(x, y, 200f, tab5, "Teleport to selected astronomical", 16, "button-teleport-to-selected-astronomical", PlayerFunctions.TeleportToSelectedAstronomical);
        return;

        void OnBeltSignalChanged()
        {
            var on = FactoryPatch.BeltSignalGeneratorEnabled.Value;
            cb1.gameObject.SetActive(on);
            cb2.gameObject.SetActive(on);
            cb3.gameObject.SetActive(on);
            cb4.gameObject.SetActive(on);
            tip1.gameObject.SetActive(on);
        }
    }

    private static void UpdateUI()
    {
        UpdateButtons();
    }

    private static void UpdateButtons()
    {
        var data = GameMain.data;
        if (data == null) return;
        var resignEnabled = data.account != AccountData.me;
        if (_resignGameBtn.gameObject.activeSelf != resignEnabled)
        {
            _resignGameBtn.gameObject.SetActive(resignEnabled);
        }
        var history = data.history;
        if (history == null) return;
        var banEnabled = history.hasUsedPropertyBanAchievement;
        if (_clearBanBtn.gameObject.activeSelf != banEnabled)
        {
            _clearBanBtn.gameObject.SetActive(banEnabled);
        }
    }
}