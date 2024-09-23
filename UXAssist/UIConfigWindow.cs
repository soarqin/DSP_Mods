using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;
using UXAssist.Functions;
using UXAssist.Patches;

namespace UXAssist;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;
    private static RectTransform _dysonTab;
    private static readonly UIButton[] DysonLayerBtn = new UIButton[10];

    public static void Init()
    {
        I18N.Add("UXAssist", "UXAssist", "UX助手");
        I18N.Add("General", "General", "常规");
        I18N.Add("Planet/Factory", "Planet/Factory", "行星/工厂");
        I18N.Add("Player/Mecha", "Player/Mecha", "玩家/机甲");
        I18N.Add("Dyson Sphere", "Dyson Sphere", "戴森球");
        I18N.Add("Tech/Combat", "Tech/Combat", "科研/战斗");
        I18N.Add("Enable game window resize", "Enable game window resize (maximum box and thick frame)", "可调整游戏窗口大小(可最大化和拖动边框)");
        I18N.Add("Remeber window position and size on last exit", "Remeber window position and size on last exit", "记住上次退出时的窗口位置和大小");
        I18N.Add("Scale up mouse cursor", "Scale up mouse cursor", "放大鼠标指针");
        /*
        I18N.Add("Better auto-save mechanism", "Better auto-save mechanism", "更好的自动存档机制");
        I18N.Add("Better auto-save mechanism tips", "Auto saves are stored in 'Save\\AutoSaves' folder, filenames are combined with cluster address and date-time", "自动存档会以星区地址和日期时间组合为文件名存储在'Save\\AutoSaves'文件夹中");
        */
        I18N.Add("Convert old saves to Combat Mode on loading", "Convert old saves to Combat Mode on loading (Use settings in new game panel)", "读取旧档时转为战斗模式(使用新游戏面板的战斗难度设置)");
        I18N.Add("Profile-based save folder", "Mod manager profile based save folder", "基于mod管理器配置档案名的存档文件夹");
        I18N.Add("Profile-based save folder tips", "Save files are stored in 'Save\\<ProfileName>' folder.\nWill use original save location if matching default profile name",
            "存档文件会存储在'Save\\<ProfileName>'文件夹中\n如果匹配默认配置档案名则使用原始存档位置");
        I18N.Add("Default profile name", "Default profile name", "默认配置档案名");
        I18N.Add("Logical Frame Rate", "Logical Frame Rate", "逻辑帧倍率");
        I18N.Add("Reset", "Reset", "重置");
        I18N.Add("Unlimited interactive range", "Unlimited interactive range", "无限交互距离");
        I18N.Add("Night Light", "Sunlight at night", "夜间日光灯");
        I18N.Add("Angle X:", "Angle X:", "入射角度X:");
        I18N.Add("Remove some build conditions", "Remove some build conditions", "移除部分不影响游戏逻辑的建造条件");
        I18N.Add("Remove build range limit", "Remove build count and range limit", "移除建造数量和距离限制");
        I18N.Add("Larger area for upgrade and dismantle", "Larger area for upgrade and dismantle", "范围升级和拆除的最大区域扩大");
        I18N.Add("Larger area for terraform", "Larger area for terraform", "范围铺设地基的最大区域扩大");
        I18N.Add("Off-grid building and stepped rotation", "Off-grid building and stepped rotation (Hold Shift)", "脱离网格建造以及小角度旋转(按住Shift)");
        I18N.Add("Enable player actions in globe view", "Enable player actions in globe view", "在行星视图中允许玩家操作");
        I18N.Add("Hide tips for soil piles changes", "Hide tips for soil piles changes", "隐藏沙土数量变动的提示");
        I18N.Add("Enhance control for logistic storage limits", "Enhance control for logistic storage limits", "物流塔存储限制控制改进");
        I18N.Add("Enhance control for logistic storage limits tips",
            "Logistic storage limits are not scaled on upgrading 'Logistics Carrier Capacity', if they are not set to maximum capacity.\nUse arrow keys to adjust logistic storage limits:\n  \u2190/\u2192: -/+10  \u2193\u2191: -/+100",
            "当升级'运输机舱扩容'时，不会对各种物流塔的存储限制按比例提升，除非设置为最大允许容量。\n你可以使用方向键微调物流塔存储限制：\n  \u2190\u2192: -/+10  \u2193\u2191: -/+100");
        I18N.Add("Enhanced count control for hand-make", "Enhanced count control for hand-make", "手动制造物品的数量控制改进");
        I18N.Add("Enhanced count control for hand-make tips", "Maximum count is increased to 1000.\nHold Ctrl/Shift/Alt to change the count rapidly.", "最大数量提升至1000\n按住Ctrl/Shift/Alt可快速改变数量");
        I18N.Add("Quick build and dismantle stacking labs", "Quick build and dismantle stacking labs/storages/tanks(hold shift)", "快速建造和拆除堆叠研究站/储物仓/储液罐(按住shift)");
        I18N.Add("Protect veins from exhaustion", "Protect veins from exhaustion", "保护矿脉不会耗尽");
        I18N.Add("Protect veins from exhaustion tips",
            "By default, the vein amount is protected at 100, and oil speed is protected at 1.0/s, you can set them yourself in config file.\nWhen reach the protection value, veins/oils steeps will not be mined/extracted any longer.\nClose this function to resume mining and pumping, usually when you have enough level on `Veins Utilization`",
            "默认矿脉数量保护于剩余100，采油速保护于速度1.0/s，你可以在配置文件中自行设置。\n当达到保护值时，矿脉和油井将不再被开采。\n关闭此功能以恢复开采，一般是当你在`矿物利用`上有足够的等级时。\n");
        I18N.Add("Do not render factory entities", "Do not render factory entities (except belts and sorters)", "不渲染工厂建筑实体(除了传送带和分拣器)");
        I18N.Add("Drag building power poles in maximum connection range", "Drag building power poles in maximum connection range", "拖动建造电线杆时自动使用最大连接距离间隔");
        I18N.Add("Allow overflow for Logistic Stations and Advanced Mining Machines", "Allow overflow for Logistic Stations and Advanced Mining Machines", "允许物流站和大型采矿机物品溢出");
        I18N.Add("Belt signals for buy out dark fog items automatically", "Belt signals for buy out dark fog items automatically", "用于自动购买黑雾物品的传送带信号");
        I18N.Add("Logistics Control Panel Improvement", "Logistics Control Panel Improvement", "物流控制面板改进");
        I18N.Add("Logistics Control Panel Improvement tips",
            "Auto apply filter with item under mouse cursor while opening the panel\nQuick-set item filter while right-clicking item icons in storage list on the panel",
            "打开面板时自动将鼠标指向物品设为筛选条件\n在控制面板物流塔列表中右键点击物品图标快速设置为筛选条件");
        I18N.Add("Real-time logistic stations info panel", "Real-time logistic stations info panel", "物流运输站实时信息面板");
        I18N.Add("Show status bars for storage items", "Show status bars for storage items", "显示存储物品状态条");
        I18N.Add("Auto navigation on sailings", "Auto navigation on sailings", "宇宙航行时自动导航");
        I18N.Add("Enable auto-cruise", "Enable auto-cruise", "启用自动巡航");
        I18N.Add("Auto boost", "Auto boost", "自动加速");
        I18N.Add("Distance to use warp", "Distance to use warp (AU)", "使用曲速的距离(AU)");
        I18N.Add("Treat stack items as single in monitor components", "Treat stack items as single in monitor components", "在流速计中将堆叠物品视为单个物品");
        I18N.Add("Initialize This Planet", "Initialize this planet", "初始化本行星");
        I18N.Add("Initialize This Planet Confirm", "This operation will destroy all buildings and revert terrains on this planet, are you sure?", "此操作将会摧毁本行星上的所有建筑并恢复地形，确定吗？");
        I18N.Add("Dismantle All Buildings", "Dismantle all buildings", "拆除所有建筑");
        I18N.Add("Dismantle All Buildings Confirm", "This operation will dismantle all buildings on this planet, are you sure?", "此操作将会拆除本行星上的所有建筑，确定吗？");
        I18N.Add("Quick build Orbital Collectors", "Quick build Orbital Collectors", "快速建造轨道采集器");
        I18N.Add("Maximum count to build", "Maximum count to build", "最大建造数量");
        I18N.Add("max", "max", "最大");
        I18N.Add("Stop ejectors when available nodes are all filled up", "Stop ejectors when available nodes are all filled up", "可用节点全部造完时停止弹射");
        I18N.Add("Construct only structure points but frames", "Construct only structure points but frames", "只造节点不造框架");
        I18N.Add("Initialize Dyson Sphere", "Initialize Dyson Sphere", "初始化戴森球");
        I18N.Add("Initialize Dyson Sphere Confirm", "This operation will destroy all layers on this dyson sphere, are you sure?", "此操作将会摧毁戴森球上的所有层级，确定吗？");
        I18N.Add("Click to dismantle selected layer", "Click to dismantle selected layer", "点击拆除对应的戴森壳");
        I18N.Add("Dismantle selected layer", "Dismantle selected layer", "拆除选中的戴森壳");
        I18N.Add("Dismantle selected layer Confirm", "This operation will dismantle selected layer, are you sure?", "此操作将会拆除选中的戴森壳，确定吗？");
        I18N.Add("Auto Fast Build Speed Multiplier", "Auto Fast Build Speed Multiplier", "自动快速建造速度倍率");
        I18N.Add("Restore upgrades of \"Sorter Cargo Stacking\" on panel", "Restore upgrades of \"Sorter Cargo Stacking\" on panel", "在升级面板上恢复\"分拣器货物叠加\"的升级");
        I18N.Add("Buy out techs with their prerequisites", "Buy out techs with their prerequisites", "购买科技也同时购买所有前置科技");
        I18N.Add("Set \"Sorter Cargo Stacking\" to unresearched state", "Set \"Sorter Cargo Stacking\" to unresearched state", "将\"分拣器货物叠加\"设为未研究状态");
        I18N.Add("Open Dark Fog Communicator", "Open Dark Fog Communicator", "打开黑雾通讯器");
        I18N.Apply();
        MyConfigWindow.OnUICreated += CreateUI;
        MyConfigWindow.OnUpdateUI += UpdateUI;
    }

    private class OcMapper : MyWindow.ValueMapper<int>
    {
        public override int Min => 0;
        public override int Max => 40;

        public override string FormatValue(string format, int value)
        {
            return value == 0 ? "max".Translate() : base.FormatValue(format, value);
        }
    }

    private class AngleMapper : MyWindow.ValueMapper<float>
    {
        public override int Min => 0;
        public override int Max => 20;
        public override float IndexToValue(int index) => index - 10f;
        public override int ValueToIndex(float value) => Mathf.RoundToInt(value + 10f);
    }

    private class DistanceMapper : MyWindow.ValueMapper<double>
    {
        public override int Min => 1;
        public override int Max => 40;
        public override double IndexToValue(int index) => index * 0.5;
        public override int ValueToIndex(double value) => Mathf.RoundToInt((float)(value * 2.0));
    }

    private class UpsMapper : MyWindow.ValueMapper<double>
    {
        public override int Min => 1;
        public override int Max => 100;
        public override double IndexToValue(int index) => index * 0.1;
        public override int ValueToIndex(double value) => Mathf.RoundToInt((float)(value * 10.0));
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        MyCheckBox checkBoxForMeasureTipsPos;

        _windowTrans = trans;
        wnd.AddTabGroup(trans, "UXAssist", "tab-group-uxassist");
        var tab1 = wnd.AddTab(trans, "General");
        var x = 0f;
        var y = 10f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.EnableWindowResizeEnabled, "Enable game window resize");
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.LoadLastWindowRectEnabled, "Remeber window position and size on last exit");
        y += 36f;
        var txt = wnd.AddText2(x + 2f, y, tab1, "Scale up mouse cursor", 15, "text-scale-up-mouse-cursor");
        x += txt.preferredWidth + 5f;
        wnd.AddSlider(x + 2f, y + 6f, tab1, GamePatch.MouseCursorScaleUpMultiplier, [1, 2, 3, 4], "0x", 100f);
        x = 0f;
        /*
        y += 30f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.AutoSaveOptEnabled, "Better auto-save mechanism");
        x = 200f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Better auto-save mechanism", "Better auto-save mechanism tips", "auto-save-opt-tips");
        x = 0f;
        */
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.ConvertSavesFromPeaceEnabled, "Convert old saves to Combat Mode on loading");
        if (WindowFunctions.ProfileName != null)
        {
            y += 36f;
            checkBoxForMeasureTipsPos = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedSaveFolderEnabled, "Profile-based save folder");
            x += checkBoxForMeasureTipsPos.Width + 5f;
            y += 6f;
            wnd.AddTipsButton2(x, y, tab1, "Profile-based save folder", "Profile-based save folder tips", "btn-profile-based-save-folder-tips");
            x = 0;
            y += 24f;
            wnd.AddText2(x, y, tab1, "Default profile name", 15, "text-default-profile-name");
            y += 24f;
            wnd.AddInputField(x, y, 200f, tab1, GamePatch.DefaultProfileName, 15, "input-profile-save-folder");
            y += 18f;
        }
        /*
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab1, "Show CPU Info", 16, "button-show-cpu-info", WindowFunctions.ShowCPUInfo);
        */
        if (!ModsCompat.BulletTimeWrapper.HasBulletTime)
        {
            y += 36f;
            txt = wnd.AddText2(x + 2f, y, tab1, "Logical Frame Rate", 15, "game-frame-rate");
            x += txt.preferredWidth + 5f;
            wnd.AddSlider(x + 2f, y + 6f, tab1, GamePatch.GameUpsFactor, new UpsMapper(), "0.0x", 200f);
            var btn = wnd.AddFlatButton(x + 204f, y + 6f, tab1, "Reset", 13, "reset-game-frame-rate", () => GamePatch.GameUpsFactor.Value = 1.0f);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(40f, 20f);
        }

        var tab2 = wnd.AddTab(trans, "Planet/Factory");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.RemoveSomeConditionEnabled, "Remove some build conditions");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.RemoveBuildRangeLimitEnabled, "Remove build range limit");
        y += 36f;
        var cb = wnd.AddCheckBox(x, y, tab2, FactoryPatch.NightLightEnabled, "Night Light");
        x += cb.Width + 5f + 10f;
        txt = wnd.AddText2(x, y + 2, tab2, "Angle X:", 13, "text-nightlight-angle-x");
        x += txt.preferredWidth + 5f;
        wnd.AddSlider(x, y + 7, tab2, FactoryPatch.NightLightAngleX, new AngleMapper(), "0", 80f);
        x += 90f;
        txt = wnd.AddText2(x, y + 2, tab2, "Y:", 13, "text-nightlight-angle-y");
        x += txt.preferredWidth + 5f;
        wnd.AddSlider(x, y + 7, tab2, FactoryPatch.NightLightAngleY, new AngleMapper(), "0", 80f);
        x = 0;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled, "Larger area for upgrade and dismantle");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.LargerAreaForTerraformEnabled, "Larger area for terraform");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.OffGridBuildingEnabled, "Off-grid building and stepped rotation");
        y += 36f;
        checkBoxForMeasureTipsPos = wnd.AddCheckBox(x, y, tab2, FactoryPatch.TreatStackingAsSingleEnabled, "Treat stack items as single in monitor components");
        x += checkBoxForMeasureTipsPos.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab2, "Enhance control for logistic storage limits", "Enhance control for logistic storage limits tips", "enhanced-logistic-limit-tips");
        x = 0f;
        y += 30f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.QuickBuildAndDismantleLabsEnabled, "Quick build and dismantle stacking labs");
        y += 36f;
        checkBoxForMeasureTipsPos = wnd.AddCheckBox(x, y, tab2, FactoryPatch.ProtectVeinsFromExhaustionEnabled, "Protect veins from exhaustion");
        x += checkBoxForMeasureTipsPos.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab2, "Protect veins from exhaustion", "Protect veins from exhaustion tips", "protect-veins-tips");
        x = 0f;
        y += 30f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.DragBuildPowerPolesEnabled, "Drag building power poles in maximum connection range");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.DoNotRenderEntitiesEnabled, "Do not render factory entities");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalsForBuyOutEnabled, "Belt signals for buy out dark fog items automatically");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab2, "Initialize This Planet", 16, "button-init-planet", () =>
            UIMessageBox.Show("Initialize This Planet".Translate(), "Initialize This Planet Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null,
                () => { PlanetFunctions.RecreatePlanet(true); })
        );
        y += 36f;
        wnd.AddButton(x, y, tab2, "Dismantle All Buildings", 16, "button-dismantle-all", () =>
            UIMessageBox.Show("Dismantle All Buildings".Translate(), "Dismantle All Buildings Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null,
                () => { PlanetFunctions.DismantleAll(false); })
        );
        y += 72f;
        wnd.AddButton(x, y, 200, tab2, "Quick build Orbital Collectors", 16, "button-init-planet", PlanetFunctions.BuildOrbitalCollectors);
        x += 10f;
        y += 30f;
        wnd.AddText2(x, y, tab2, "Maximum count to build", 15, "text-oc-build-count");
        y += 24f;
        wnd.AddSlider(x, y, tab2, PlanetFunctions.OrbitalCollectorMaxBuildCount, new OcMapper(), "G", 200f);
        x = 400f;
        y += 54f;
        wnd.AddCheckBox(x, y, tab2, LogisticsPatch.LogisticsCapacityTweaksEnabled, "Enhance control for logistic storage limits");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, LogisticsPatch.AllowOverflowInLogisticsEnabled, "Allow overflow for Logistic Stations and Advanced Mining Machines");
        y += 36f;
        checkBoxForMeasureTipsPos = wnd.AddCheckBox(x, y, tab2, LogisticsPatch.LogisticsConstrolPanelImprovementEnabled, "Logistics Control Panel Improvement");
        wnd.AddTipsButton2(x + checkBoxForMeasureTipsPos.Width + 5f, y + 6f, tab2, "Logistics Control Panel Improvement", "Logistics Control Panel Improvement tips", "lcp-improvement-tips");
        y += 36f;
        var cb0 = wnd.AddCheckBox(x, y, tab2, LogisticsPatch.RealtimeLogisticsInfoPanelEnabled, "Real-time logistic stations info panel");
        var cb1 = wnd.AddCheckBox(x + 26f, y + 26f, tab2, LogisticsPatch.RealtimeLogisticsInfoPanelBarsEnabled, "Show status bars for storage items", 13);
        if (ModsCompat.AuxilaryfunctionWrapper.ShowStationInfo != null)
        {
            ModsCompat.AuxilaryfunctionWrapper.ShowStationInfo.SettingChanged += (_, _) => { OnAuxilaryInfoPanelChanged(); };
            OnAuxilaryInfoPanelChanged();
        }

        var tab3 = wnd.AddTab(trans, "Player/Mecha");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab3, FactoryPatch.UnlimitInteractiveEnabled, "Unlimited interactive range");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlanetPatch.PlayerActionsInGlobeViewEnabled, "Enable player actions in globe view");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.HideTipsForSandsChangesEnabled, "Hide tips for soil piles changes");
        y += 36f;
        checkBoxForMeasureTipsPos = wnd.AddCheckBox(x, y, tab3, PlayerPatch.EnhancedMechaForgeCountControlEnabled, "Enhanced count control for hand-make");
        x += checkBoxForMeasureTipsPos.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab3, "Enhanced count control for hand-make", "Enhanced count control for hand-make tips", "enhanced-count-control-tips");
        x = 0f;
        y += 30f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.AutoNavigationEnabled, "Auto navigation on sailings");
        x = 20f;
        y += 26f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.AutoCruiseEnabled, "Enable auto-cruise", 13);
        x = 10f;
        y += 26f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.AutoBoostEnabled, "Auto boost", 13);
        y += 32f;
        wnd.AddText2(x, y, tab3, "Distance to use warp", 15, "text-distance-to-warp");
        y += 24f;
        wnd.AddSlider(x, y, tab3, PlayerPatch.DistanceToWarp, new DistanceMapper(), "0.0", 200f);

        var tab4 = wnd.AddTab(trans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.StopEjectOnNodeCompleteEnabled, "Stop ejectors when available nodes are all filled up");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.OnlyConstructNodesEnabled, "Construct only structure points but frames");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab4, "Initialize Dyson Sphere", 16, "init-dyson-sphere", () =>
            UIMessageBox.Show("Initialize Dyson Sphere".Translate(), "Initialize Dyson Sphere Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null,
                () => { DysonSpherePatch.InitCurrentDysonSphere(-1); })
        );
        y += 36f;
        wnd.AddText2(x, y, tab4, "Click to dismantle selected layer", 16, "text-dismantle-layer");
        y += 26f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = wnd.AddFlatButton(x, y, tab4, id.ToString(), 12, "dismantle-layer-" + id, () =>
                UIMessageBox.Show("Dismantle selected layer".Translate(), "Dismantle selected layer Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null,
                    () => { DysonSpherePatch.InitCurrentDysonSphere(id); })
            );
            ((RectTransform)btn.transform).sizeDelta = new Vector2(40f, 20f);
            DysonLayerBtn[i] = btn;
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

        x = 400f;
        y += 36f;
        wnd.AddText2(x, y, tab4, "Auto Fast Build Speed Multiplier", 15, "text-auto-fast-build-multiplier");
        y += 24f;
        wnd.AddSlider(x, y, tab4, DysonSpherePatch.AutoConstructMultiplier, [1, 2, 5, 10, 20, 50, 100], "0", 200f);
        _dysonTab = tab4;

        var tab5 = wnd.AddTab(_windowTrans, "Tech/Combat");
        x = 10;
        y = 10;
        wnd.AddCheckBox(x, y, tab5, TechPatch.BatchBuyoutTechEnabled, "Buy out techs with their prerequisites");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, TechPatch.SorterCargoStackingEnabled, "Restore upgrades of \"Sorter Cargo Stacking\" on panel");
        y += 36f;
        wnd.AddButton(x, y, 300f, tab5, "Set \"Sorter Cargo Stacking\" to unresearched state", 16, "button-remove-cargo-stacking", () =>
        {
            var history = GameMain.data?.history;
            if (history == null) return;
            history.inserterStackCountObsolete = 1;
            for (var id = 3301; id <= 3305; id++)
            {
                history.techStates.TryGetValue(id, out var state);
                if (!state.unlocked) continue;
                state.unlocked = false;
                state.hashUploaded = 0;
                history.techStates[id] = state;
            }
        });
        y += 36f;
        y += 36f;
        wnd.AddButton(x, y, 300f, tab5, "Open Dark Fog Communicator", 16, "button-open-df-communicator", () =>
        {
            if (!(GameMain.data?.gameDesc.isCombatMode ?? false)) return;
            var uiGame = UIRoot.instance.uiGame;
            uiGame.ShutPlayerInventory();
            uiGame.CloseEnemyBriefInfo();
            uiGame.OpenCommunicatorWindow(5);
        });
        return;

        void OnAuxilaryInfoPanelChanged()
        {
            if (ModsCompat.AuxilaryfunctionWrapper.ShowStationInfo == null)
            {
                cb0.gameObject.SetActive(true);
                cb1.gameObject.SetActive(true);
                return;
            }

            var on = !ModsCompat.AuxilaryfunctionWrapper.ShowStationInfo.Value;
            cb0.gameObject.SetActive(on);
            cb1.gameObject.SetActive(on);
            if (!on)
            {
                LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.Value = false;
            }
        }
    }

    private static void UpdateUI()
    {
        UpdateDysonShells();
    }

    private static void UpdateDysonShells()
    {
        if (!_dysonTab.gameObject.activeSelf) return;
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
                    DysonLayerBtn[i - 1].button.interactable = layer != null && layer.id == i;
                }

                return;
            }
        }

        for (var i = 0; i < 10; i++)
        {
            DysonLayerBtn[i].button.interactable = false;
        }
    }
}