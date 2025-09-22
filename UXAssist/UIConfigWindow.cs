using System;
using System.Text;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Functions;
using UXAssist.ModsCompat;
using UXAssist.Patches;
using UXAssist.UI;

namespace UXAssist;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;
    private static RectTransform _dysonTab;
    private static UIButton _dysonInitBtn;
    private static readonly UIButton[] DysonLayerBtn = new UIButton[10];

    public static void Init()
    {
        I18N.Add("UXAssist", "UXAssist", "UX助手");
        I18N.Add("General", "General", "常规");
        I18N.Add("Factory", "Factory", "工厂");
        I18N.Add("Logistics", "Logistics", "物流");
        I18N.Add("Player/Mecha", "Player/Mecha", "玩家/机甲");
        I18N.Add("Dyson Sphere", "Dyson Sphere", "戴森球");
        I18N.Add("Tech/Combat/UI", "Tech/Combat/UI", "科研/战斗/UI");
        I18N.Add("Enable game window resize", "Enable game window resize (maximum box and thick frame)", "可调整游戏窗口大小(可最大化和拖动边框)");
        I18N.Add("Remeber window position and size on last exit", "Remeber window position and size on last exit", "记住上次退出时的窗口位置和大小");
        /*
        I18N.Add("Better auto-save mechanism", "Better auto-save mechanism", "更好的自动存档机制");
        I18N.Add("Better auto-save mechanism tips", "Auto saves are stored in 'Save\\AutoSaves' folder, filenames are combined with cluster address and date-time", "自动存档会以星区地址和日期时间组合为文件名存储在'Save\\AutoSaves'文件夹中");
        */
        I18N.Add("Convert old saves to Combat Mode on loading", "Convert old saves to Combat Mode on loading (Use settings in new game panel)", "读取旧档时转为战斗模式(使用新游戏面板的战斗难度设置)");
        I18N.Add("Profile-based save folder", "Mod manager profile based save folder", "基于mod管理器配置档案名的存档文件夹");
        I18N.Add("Profile-based save folder tips", "Save files are stored in 'Save\\<ProfileName>' folder.\nWill use original save location if matching default profile name",
            "存档文件会存储在'Save\\<ProfileName>'文件夹中\n如果匹配默认配置档案名则使用原始存档位置");
        I18N.Add("Profile-based option", "Mod manager profile based option", "基于mod管理器配置档案名的选项设置");
        I18N.Add("Profile-based option tips", "Options are stored in 'Option\\<ProfileName>.xml'.\nWill use original location if matching default profile name",
            "配置选项会存储在'Option\\<ProfileName>.xml'里\n如果匹配默认配置档案名则使用原始位置");
        I18N.Add("Default profile name", "Default profile name", "默认配置档案名");
        I18N.Add("Logical Frame Rate", "Logical Frame Rate", "逻辑帧倍率");
        I18N.Add("Reset", "Reset", "重置");
        I18N.Add("Process priority", "Process priority", "进程优先级");
        I18N.Add("High", "High", "高");
        I18N.Add("Above Normal", "Above Normal", "高于正常");
        I18N.Add("Normal", "Normal", "正常");
        I18N.Add("Below Normal", "Below Normal", "低于正常");
        I18N.Add("Idle", "Idle", "空闲");
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
        I18N.Add("Enhanced count control for hand-make", "Enhanced count control for hand-make", "手动制造物品的数量控制改进");
        I18N.Add("Enhanced count control for hand-make tips", "Maximum count is increased to 1000.\nHold Ctrl/Shift/Alt to change the count rapidly.", "最大数量提升至1000\n按住Ctrl/Shift/Alt可快速改变数量");
        I18N.Add("Quick build and dismantle stacking labs", "Quick build and dismantle stacking labs/storages/tanks(hold shift)", "快速建造和拆除堆叠研究站/储物仓/储液罐(按住shift)");
        I18N.Add("Fast fill in to and take out from tanks", "Fast fill in to and take out from tanks", "储液罐快速注入和抽取液体");
        I18N.Add("Speed Ratio", "Speed Ratio", "速度倍率");
        I18N.Add("Cut conveyor belt (with shortcut key)", "Cut conveyor belt (with shortcut key)", "切割传送带(使用快捷键)");
        I18N.Add("Protect veins from exhaustion", "Protect veins from exhaustion", "保护矿脉不会耗尽");
        I18N.Add("Protect veins from exhaustion tips",
            "By default, the vein amount is protected at 100, and oil speed is protected at 1.0/s, you can set them yourself in config file.\nWhen reach the protection value, veins/oils steeps will not be mined/extracted any longer.\nClose this function to resume mining and pumping, usually when you have enough level on `Veins Utilization`",
            "默认矿脉数量保护于剩余100，采油速保护于速度1.0/s，你可以在配置文件中自行设置。\n当达到保护值时，矿脉和油井将不再被开采。\n关闭此功能以恢复开采，一般是当你在`矿物利用`上有足够的等级时。\n");
        I18N.Add("Do not render factory entities", "Do not render factory entities (except belts and sorters)", "不渲染工厂建筑实体(除了传送带和分拣器)");
        I18N.Add("Drag building power poles in maximum connection range", "Drag building power poles in maximum connection range", "拖动建造电线杆时自动使用最大连接距离间隔");
        I18N.Add("Build Tesla Tower and Wireless Power Tower alternately", "Build Tesla Tower and Wireless Power Tower alternately", "交替建造电力感应塔和无线输电塔");
        I18N.Add("Belt signals for buy out dark fog items automatically", "Belt signals for buy out dark fog items automatically", "用于自动购买黑雾物品的传送带信号");
        I18N.Add("Auto-config logistic stations", "Auto-config logistic stations", "自动配置物流设施");
        I18N.Add("Limit auto-replenish count to values below", "Limit auto-replenish count to values below", "限制自动补充数量为下面配置的值");
        I18N.Add("Dispenser", "Logistics Distributor", "物流配送器");
        I18N.Add("Battlefield Analysis Base", "Battlefield Analysis Base", "战场分析基站");
        I18N.Add("PLS", "PLS", "行星物流站");
        I18N.Add("ILS", "ILS", "星际物流站");
        I18N.Add("Advanced Mining Machine", "Advanced Mining Machine", "大型采矿机");
        I18N.Add("Set default remote logic to storage", "Set default remote logic to storage", "设置默认远程逻辑为仓储");
        I18N.Add("Max. Charging Power", "Max. Charging Power", "最大充能功率");
        I18N.Add("Count of Bots filled", "Count of Bots filled", "填充的配送机数量");
        I18N.Add("Drone transport range", "Drone transport range", "运输机最远路程");
        I18N.Add("Min. Load of Drones", "Min. Load of Drones", "运输机起送量");
        I18N.Add("Outgoing integration count", "Outgoing integration count", "输出货物集装数量");
        I18N.Add("Count of Drones filled", "Count of Drones filled", "填充的运输机数量");
        I18N.Add("Vessel transport range", "Vessel transport range", "运输船最远路程");
        I18N.Add("Warp distance", "Warp distance", "曲速启用路程");
        I18N.Add("Min. Load of Vessels", "Min. Load of Vessels", "运输船起送量");
        I18N.Add("Outgoing integration count", "Outgoing integration count", "输出货物集装数量");
        I18N.Add("Include Orbital Collector", "Include Orbital Collector", "包含轨道采集器");
        I18N.Add("Warpers required", "Warpers required", "翘曲器必备");
        I18N.Add("Count of Vessels filled", "Count of Vessels filled", "填充的运输船数量");
        I18N.Add("Collecting Speed", "Collecting Speed", "开采速度");
        I18N.Add("Min. Piler Value", "Outgoing integration count", "输出货物集装数量");

        I18N.Add("Allow overflow for Logistic Stations and Advanced Mining Machines", "Allow overflow for Logistic Stations and Advanced Mining Machines", "允许物流站和大型采矿机物品溢出");
        I18N.Add("Enhance control for logistic storage capacities", "Enhance control for logistic storage capacities", "物流塔存储容量控制改进");
        I18N.Add("Enhance control for logistic storage capacities tips",
            "Logistic storage capacity limits are not scaled on upgrading 'Logistics Carrier Capacity', if they are not set to maximum capacity or already greater than upgraded maximum capacity.\nUse arrow keys to adjust logistic storage capacities:\n  \u2190/\u2192: -/+10  \u2193\u2191: -/+100",
            "当升级'运输机舱扩容'时，不会对各种物流塔的存储容量按比例提升，除非设置为最大允许容量或者已经超过升级后的最大容量。\n你可以使用方向键微调物流塔存储容量：\n  \u2190\u2192: -/+10  \u2193\u2191: -/+100");
        I18N.Add("Logistics Control Panel Improvement", "Logistics Control Panel Improvement", "物流控制面板改进");
        I18N.Add("Logistics Control Panel Improvement tips",
            "Auto apply filter with item under mouse cursor while opening the panel\nQuick-set item filter while right-clicking item icons in storage list on the panel",
            "打开面板时自动将鼠标指向物品设为筛选条件\n在控制面板物流塔列表中右键点击物品图标快速设置为筛选条件");
        I18N.Add("Real-time logistic stations info panel", "Real-time logistic stations info panel", "物流运输站实时信息面板");
        I18N.Add("Show status bars for storage items", "Show status bars for storage items", "显示存储物品状态条");
        I18N.Add("Tweak building buffers", "Tweak building buffers", "调整建筑输入缓冲");
        I18N.Add("Assembler buffer time multiplier(in seconds)", "Assembler buffer time multiplier(in seconds)", "工厂配方缓冲时间倍率(秒)");
        I18N.Add("Assembler buffer minimum multiplier", "Assembler buffer minimum multiplier", "工厂配方缓冲最小倍率");
        I18N.Add("Buffer count for assembling in labs", "Buffer count for assembling in labs", "研究站矩阵合成模式缓存数量");
        I18N.Add("Extra buffer count for Self-evolution Labs", "Extra buffer count for Self-evolution Labs", "自演化研究站矩阵额外缓冲数量");
        I18N.Add("Buffer count for researching in labs", "Buffer count for researching in labs", "研究站科研模式缓存数量");
        I18N.Add("Ray Receiver Graviton Lens buffer count", "Ray Receiver Graviton Lens buffer count", "射线接收器透镜缓冲数量");
        I18N.Add("Ejector Solar Sails buffer count", "Ejector Solar Sails buffer count", "弹射器太阳能帆缓冲数量");
        I18N.Add("Silo Rockets buffer count", "Silo Rockets buffer count", "发射井火箭缓冲数量");
        I18N.Add("Shortcut keys for Blueprint Copy mode", "Shortcut keys for Blueprint Copy mode", "蓝图复制模式快捷键");
        I18N.Add("Shortcut keys for Blueprint Copy mode tips", "You can set 2 shortcut keys in Settings panel:\n  1. Select all buildings\n  2. Dismantle selected buildings", "你可以在设置面板中设置2个快捷键：\n  1. 选择所有建筑\n  2. 拆除选中的建筑");
        I18N.Add("Shortcut keys for showing stars' name", "Shortcut keys for showing stars' name", "启用显示所有星系名称的快捷键");
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
        I18N.Add("Disable battle-related techs in Peace mode", "Disable battle-related techs in Peace mode", "在和平模式下隐藏战斗相关科技");
        I18N.Add("Buy out techs with their prerequisites", "Buy out techs with their prerequisites", "购买科技也同时购买所有前置科技");
        I18N.Add("Set \"Sorter Cargo Stacking\" to unresearched state", "Set \"Sorter Cargo Stacking\" to unresearched state", "将\"分拣器货物叠加\"设为未研究状态");
        I18N.Add("Unlock all techs with metadata", "Unlock all techs with metadata", "使用元数据解锁所有科技");
        I18N.Add("Open Dark Fog Communicator", "Open Dark Fog Communicator", "打开黑雾通讯器");
        I18N.Add("Planet vein utilization", "Planet vein utilization in star map", "宇宙视图行星/星系矿脉数量显示");
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

    private class AutoConfigDispenserChargePowerMapper() : MyWindow.RangeValueMapper<int>(3, 30)
    {
        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 300000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigBattleBaseChargePowerMapper() : MyWindow.RangeValueMapper<int>(4, 40)
    {

        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 300000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigPLSChargePowerMapper() : MyWindow.RangeValueMapper<int>(2, 20)
    {

        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 3000000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigCarrierMinDeliverMapper() : MyWindow.RangeValueMapper<int>(0, 10)
    {
        public override string FormatValue(string format, int value)
        {
            return (value == 0 ? 1 : (value * 10)).ToString("0\\%");
        }
    }

    private class AutoConfigMinPilerValueMapper() : MyWindow.RangeValueMapper<int>(0, 4)
    {
        public override string FormatValue(string format, int value)
        {
            return value == 0 ? "集装使用科技上限".Translate().Trim() : value.ToString();
        }
    }

    private class AutoConfigILSChargePowerMapper() : MyWindow.RangeValueMapper<int>(2, 20)
    {
        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 15000000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigILSMaxTripShipMapper() : MyWindow.RangeValueMapper<int>(1, 41)
    {
        public override string FormatValue(string format, int value)
        {
            return value switch
            {
                <= 20 => value.ToString("0LY"),
                <= 40 => (value * 2 - 20).ToString("0LY"),
                _ => "∞",
            };
        }
    }

    private class AutoConfigILSWarperDistanceMapper() : MyWindow.RangeValueMapper<int>(2, 21)
    {
        public override string FormatValue(string format, int value)
        {
            return value switch
            {
                <= 7 => (value * 0.5 - 0.5).ToString("0.0AU"),
                <= 13 => (value - 4.0).ToString("0.0AU"),
                <= 16 => (value - 4).ToString("0AU"),
                <= 20 => (value * 2 - 20).ToString("0AU"),
                _ => "60AU",
            };
        }
    }

    private class AutoConfigVeinCollectorHarvestSpeedMapper() : MyWindow.RangeValueMapper<int>(0, 20)
    {
        public override string FormatValue(string format, int value)
        {
            return (100 + value * 10).ToString("0\\%");
        }
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        UnityEngine.UI.Text txt;
        _windowTrans = trans;
        wnd.AddTabGroup(trans, "UXAssist", "tab-group-uxassist");
        var tab1 = wnd.AddTab(trans, "General");
        var x = 0f;
        var y = 10f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.EnableWindowResizeEnabled, "Enable game window resize");
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.LoadLastWindowRectEnabled, "Remeber window position and size on last exit");
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
        MyCheckBox checkBoxForMeasureTextWidth;
        if (WindowFunctions.ProfileName != null)
        {
            y += 36f;
            checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedSaveFolderEnabled, "Profile-based save folder");
            wnd.AddTipsButton2(checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab1, "Profile-based save folder", "Profile-based save folder tips", "btn-profile-based-save-folder-tips");
            y += 36f;
            checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedOptionEnabled, "Profile-based option");
            wnd.AddTipsButton2(checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab1, "Profile-based option", "Profile-based option tips", "btn-profile-based-option-tips");
            y += 36f;
            wnd.AddText2(x + 2f, y, tab1, "Default profile name", 15, "text-default-profile-name");
            y += 24f;
            wnd.AddInputField(x + 2f, y, 200f, tab1, GamePatch.DefaultProfileName, 15, "input-profile-save-folder");
            y += 18f;
        }
        if (!BulletTimeWrapper.HasBulletTime)
        {
            y += 36f;
            txt = wnd.AddText2(x + 2f, y, tab1, "Logical Frame Rate", 15, "game-frame-rate");
            x += txt.preferredWidth + 7f;
            wnd.AddSlider(x, y + 6f, tab1, GamePatch.GameUpsFactor, new UpsMapper(), "0.0x", 100f).WithSmallerHandle();
            var btn = wnd.AddFlatButton(x + 104f, y + 6f, tab1, "Reset", 13, "reset-game-frame-rate", () => GamePatch.GameUpsFactor.Value = 1.0f);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(40f, 20f);
            x = 0f;
        }
        y += 36f;
        wnd.AddComboBox(x + 2f, y, tab1, "Process priority").WithItems("High", "Above Normal", "Normal", "Below Normal", "Idle").WithSize(100f, 0f).WithConfigEntry(WindowFunctions.ProcessPriority);

        var tab2 = wnd.AddTab(trans, "Factory");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.RemoveSomeConditionEnabled, "Remove some build conditions");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.RemoveBuildRangeLimitEnabled, "Remove build range limit");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryPatch.NightLightEnabled, "Night Light");
        x += checkBoxForMeasureTextWidth.Width + 5f + 10f;
        txt = wnd.AddText2(x, y + 2f, tab2, "Angle X:", 13, "text-nightlight-angle-x");
        x += txt.preferredWidth + 5f;
        wnd.AddSlider(x, y + 7f, tab2, FactoryPatch.NightLightAngleX, new AngleMapper(), "0", 60f).WithSmallerHandle();
        x += 70f;
        txt = wnd.AddText2(x, y + 2f, tab2, "Y:", 13, "text-nightlight-angle-y");
        wnd.AddSlider(x + txt.preferredWidth + 5f, y + 7f, tab2, FactoryPatch.NightLightAngleY, new AngleMapper(), "0", 60f).WithSmallerHandle();
        x = 0;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled, "Larger area for upgrade and dismantle");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.LargerAreaForTerraformEnabled, "Larger area for terraform");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.OffGridBuildingEnabled, "Off-grid building and stepped rotation");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.CutConveyorBeltEnabled, "Cut conveyor belt (with shortcut key)");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryPatch.TreatStackingAsSingleEnabled, "Treat stack items as single in monitor components");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.QuickBuildAndDismantleLabsEnabled, "Quick build and dismantle stacking labs");

        {
            y += 36f;
            var cb = wnd.AddCheckBox(x, y, tab2, FactoryPatch.TankFastFillInAndTakeOutEnabled, "Fast fill in to and take out from tanks");
            x += cb.Width + 5f;
            txt = wnd.AddText2(x, y + 2f, tab2, "Speed Ratio", 13, "text-tank-fast-fill-speed-ratio");
            var tankSlider = wnd.AddSlider(x + txt.preferredWidth + 5f, y + 7f, tab2, FactoryPatch.TankFastFillInAndTakeOutMultiplier, [2, 5, 10, 20, 50, 100, 500, 1000], "G", 100f).WithSmallerHandle();
            FactoryPatch.TankFastFillInAndTakeOutEnabled.SettingChanged += TankSettingChanged;
            wnd.OnFree += () => { FactoryPatch.TankFastFillInAndTakeOutEnabled.SettingChanged -= TankSettingChanged; };
            TankSettingChanged(null, null);

            void TankSettingChanged(object o, EventArgs e)
            {
                tankSlider.SetEnable(FactoryPatch.TankFastFillInAndTakeOutEnabled.Value);
            }
        }

        x = 0;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.DoNotRenderEntitiesEnabled, "Do not render factory entities");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryPatch.ShortcutKeysForBlueprintCopyEnabled, "Shortcut keys for Blueprint Copy mode");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab2, "Shortcut keys for Blueprint Copy mode", "Shortcut keys for Blueprint Copy mode tips", "shortcut-keys-for-blueprint-copy-mode-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalsForBuyOutEnabled, "Belt signals for buy out dark fog items automatically");

        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryPatch.ProtectVeinsFromExhaustionEnabled, "Protect veins from exhaustion");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab2, "Protect veins from exhaustion", "Protect veins from exhaustion tips", "protect-veins-tips");
        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryPatch.DragBuildPowerPolesEnabled, "Drag building power poles in maximum connection range");
            y += 27f;
            var alternatelyCheckBox = wnd.AddCheckBox(x + 20f, y, tab2, FactoryPatch.DragBuildPowerPolesAlternatelyEnabled, "Build Tesla Tower and Wireless Power Tower alternately", 13);
            FactoryPatch.DragBuildPowerPolesEnabled.SettingChanged += AlternatelyCheckBoxChanged;
            wnd.OnFree += () => { FactoryPatch.DragBuildPowerPolesEnabled.SettingChanged -= AlternatelyCheckBoxChanged; };
            AlternatelyCheckBoxChanged(null, null);

            void AlternatelyCheckBoxChanged(object o, EventArgs e)
            {
                alternatelyCheckBox.SetEnable(FactoryPatch.DragBuildPowerPolesEnabled.Value);
            }
        }

        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab2, "Initialize This Planet", 16, "button-init-planet", () =>
            UIMessageBox.Show("Initialize This Planet".Translate(), "Initialize This Planet Confirm".Translate(), "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null,
                () => { PlanetFunctions.RecreatePlanet(true); })
        );
        y += 36f;
        wnd.AddButton(x, y, tab2, "Dismantle All Buildings", 16, "button-dismantle-all", () =>
            UIMessageBox.Show("Dismantle All Buildings".Translate(), "Dismantle All Buildings Confirm".Translate(), "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null,
                () => { PlanetFunctions.DismantleAll(false); })
        );
        y += 72f;
        wnd.AddButton(x, y, 200, tab2, "Quick build Orbital Collectors", 16, "button-init-planet", PlanetFunctions.BuildOrbitalCollectors);
        y += 30f;
        txt = wnd.AddText2(x + 10f, y, tab2, "Maximum count to build", 15, "text-oc-build-count");
        wnd.AddSlider(x + 10f + txt.preferredWidth + 5f, y + 6f, tab2, PlanetFunctions.OrbitalCollectorMaxBuildCount, new OcMapper(), "G", 160f);

        y += 18f;

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryPatch.TweakBuildingBufferEnabled, "Tweak building buffers");
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Assembler buffer time multiplier(in seconds)", 13);
            var nx1 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Assembler buffer minimum multiplier", 13);
            var nx2 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Buffer count for assembling in labs", 13);
            var nx3 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Extra buffer count for Self-evolution Labs", 13);
            var nx4 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Buffer count for researching in labs", 13);
            var nx5 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Ray Receiver Graviton Lens buffer count", 13);
            var nx6 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Ejector Solar Sails buffer count", 13);
            var nx7 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Silo Rockets buffer count", 13);
            var nx8 = txt.preferredWidth + 5f;
            y -= 189f;
            var mx = Mathf.Max(nx1, nx2, nx3, nx4, nx5, nx6, nx7, nx8) + 20f;
            var assemblerBufferTimeMultiplierSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.AssemblerBufferTimeMultiplier, new MyWindow.RangeValueMapper<int>(2, 10), "0", 120f).WithSmallerHandle();
            y += 27f;
            var assemblerBufferMininumMultiplierSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.AssemblerBufferMininumMultiplier, new MyWindow.RangeValueMapper<int>(2, 10), "0", 120f).WithSmallerHandle();
            y += 27f;
            var labBufferMaxCountForAssembleSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.LabBufferMaxCountForAssemble, new MyWindow.RangeValueMapper<int>(2, 20), "0", 120f).WithSmallerHandle();
            y += 27f;
            var labBufferExtraCountForAdvancedAssembleSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.LabBufferExtraCountForAdvancedAssemble, new MyWindow.RangeValueMapper<int>(1, 10), "0", 120f).WithSmallerHandle();
            y += 27f;
            var labBufferMaxCountForResearchSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.LabBufferMaxCountForResearch, new MyWindow.RangeValueMapper<int>(2, 20), "0", 120f).WithSmallerHandle();
            y += 27f;
            var receiverBufferCountSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.ReceiverBufferCount, new MyWindow.RangeValueMapper<int>(1, 20), "0", 120f).WithSmallerHandle();
            y += 27f;
            var ejectorBufferCountSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.EjectorBufferCount, new MyWindow.RangeValueWithMultiplierMapper<int>(1, 80, 5), "0", 120f).WithSmallerHandle();
            y += 27f;
            var siloBufferCountSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryPatch.SiloBufferCount, new MyWindow.RangeValueMapper<int>(1, 40), "0", 120f).WithSmallerHandle();
            FactoryPatch.TweakBuildingBufferEnabled.SettingChanged += TweakBuildingBufferChanged;
            wnd.OnFree += () => { FactoryPatch.TweakBuildingBufferEnabled.SettingChanged -= TweakBuildingBufferChanged; };
            TweakBuildingBufferChanged(null, null);

            void TweakBuildingBufferChanged(object o, EventArgs e)
            {
                assemblerBufferTimeMultiplierSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                assemblerBufferMininumMultiplierSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                labBufferMaxCountForAssembleSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                labBufferExtraCountForAdvancedAssembleSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                labBufferMaxCountForResearchSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                receiverBufferCountSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                ejectorBufferCountSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
                siloBufferCountSlider.SetEnable(FactoryPatch.TweakBuildingBufferEnabled.Value);
            }
        }

        var tab3 = wnd.AddTab(trans, "Logistics");
        x = 0f;
        y = 10f;

        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab3, LogisticsPatch.LogisticsCapacityTweaksEnabled, "Enhance control for logistic storage capacities");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab3, "Enhance control for logistic storage capacities", "Enhance control for logistic storage capacities tips", "enhanced-logistic-capacities-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, LogisticsPatch.AllowOverflowInLogisticsEnabled, "Allow overflow for Logistic Stations and Advanced Mining Machines");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab3, LogisticsPatch.LogisticsConstrolPanelImprovementEnabled, "Logistics Control Panel Improvement");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab3, "Logistics Control Panel Improvement", "Logistics Control Panel Improvement tips", "lcp-improvement-tips");
        {
            y += 36f;
            var realtimeLogisticsInfoPanelCheckBox = wnd.AddCheckBox(x, y, tab3, LogisticsPatch.RealtimeLogisticsInfoPanelEnabled, "Real-time logistic stations info panel");
            y += 27f;
            var realtimeLogisticsInfoPanelBarsCheckBox = wnd.AddCheckBox(x + 20f, y, tab3, LogisticsPatch.RealtimeLogisticsInfoPanelBarsEnabled, "Show status bars for storage items", 13);
            if (AuxilaryfunctionWrapper.ShowStationInfo != null)
            {
                AuxilaryfunctionWrapper.ShowStationInfo.SettingChanged += RealtimeLogisticsInfoPanelChanged;
                wnd.OnFree += () => { AuxilaryfunctionWrapper.ShowStationInfo.SettingChanged -= RealtimeLogisticsInfoPanelChanged; };
            }
            LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.SettingChanged += RealtimeLogisticsInfoPanelChanged;
            wnd.OnFree += () => { LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.SettingChanged -= RealtimeLogisticsInfoPanelChanged; };
            RealtimeLogisticsInfoPanelChanged(null, null);

            void RealtimeLogisticsInfoPanelChanged(object o, EventArgs e)
            {
                if (AuxilaryfunctionWrapper.ShowStationInfo == null)
                {
                    realtimeLogisticsInfoPanelCheckBox.SetEnable(true);
                    realtimeLogisticsInfoPanelBarsCheckBox.SetEnable(LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.Value);
                    return;
                }

                var on = !AuxilaryfunctionWrapper.ShowStationInfo.Value;
                realtimeLogisticsInfoPanelCheckBox.SetEnable(on);
                realtimeLogisticsInfoPanelBarsCheckBox.SetEnable(on & LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.Value);
                if (!on)
                {
                    LogisticsPatch.RealtimeLogisticsInfoPanelEnabled.Value = false;
                }
            }
        }
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, LogisticsPatch.AutoConfigLogisticsEnabled, "Auto-config logistic stations");
        y += 26f;
        wnd.AddCheckBox(x + 10f, y, tab3, LogisticsPatch.AutoConfigLimitAutoReplenishCount, "Limit auto-replenish count to values below", 13).WithSmallerBox();
        y += 18f;
        wnd.AddCheckBox(x + 10f, y, tab3, LogisticsPatch.SetDefaultRemoteLogicToStorage, "Set default remote logic to storage", 13).WithSmallerBox();
        y += 16f;
        var maxWidth = 0f;
        wnd.AddText2(10f, y, tab3, "Dispenser", 14, "text-dispenser");
        y += 18f;
        var oy = y;
        x = 20f;
        var textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-dispenser-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Bots filled", 13, "text-dispenser-count-of-bots-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "Battlefield Analysis Base", 14, "text-battlefield-analysis-base");
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-battlefield-analysis-base-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "PLS", 14, "text-pls");
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-pls-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Drone transport range", 13, "text-pls-drone-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Load of Drones", 13, "text-pls-min-load-of-drones");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Outgoing integration count", 13, "text-pls-outgoing-integration-count");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Drones filled", 13, "text-pls-count-of-drones-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "ILS", 14, "text-ils");
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-ils-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Drone transport range", 13, "text-ils-drone-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Vessel transport range", 13, "text-ils-vessel-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        wnd.AddCheckBox(x + 360f, y + 6f, tab3, LogisticsPatch.AutoConfigILSIncludeOrbitCollector, "Include Orbital Collector", 13).WithSmallerBox();
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Warp distance", 13, "text-ils-warp-distance");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        wnd.AddCheckBox(x + 360f, y + 6f, tab3, LogisticsPatch.AutoConfigILSWarperNecessary, "Warpers required", 13).WithSmallerBox();
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Load of Drones", 13, "text-ils-min-load-of-drones");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Load of Vessels", 13, "text-ils-min-load-of-vessels");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Outgoing integration count", 13, "text-ils-outgoing-integration-count");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Drones filled", 13, "text-ils-count-of-drones-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Vessels filled", 13, "text-ils-count-of-vessels-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "Advanced Mining Machine", 14, "text-amm");
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Collecting Speed", 13, "text-amm-collecting-speed");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Piler Value", 13, "text-amm-min-piler-value");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y = oy + 1;
        var nx = x + maxWidth + 5f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigDispenserChargePower, new AutoConfigDispenserChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigDispenserCourierCount, new MyWindow.RangeValueMapper<int>(0, 10), "G", 150f, -100f).WithFontSize(13);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigBattleBaseChargePower, new AutoConfigBattleBaseChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigPLSChargePower, new AutoConfigPLSChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigPLSMaxTripDrone, new MyWindow.RangeValueMapper<int>(1, 180), "0°", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigPLSDroneMinDeliver, new AutoConfigCarrierMinDeliverMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigPLSMinPilerValue, new AutoConfigMinPilerValueMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigPLSDroneCount, new MyWindow.RangeValueMapper<int>(0, 50), "G", 150f, -100f).WithFontSize(13);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSChargePower, new AutoConfigILSChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSMaxTripDrone, new MyWindow.RangeValueMapper<int>(1, 180), "0°", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSMaxTripShip, new AutoConfigILSMaxTripShipMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSWarperDistance, new AutoConfigILSWarperDistanceMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSDroneMinDeliver, new AutoConfigCarrierMinDeliverMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSShipMinDeliver, new AutoConfigCarrierMinDeliverMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSMinPilerValue, new AutoConfigMinPilerValueMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSDroneCount, new MyWindow.RangeValueMapper<int>(0, 100), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigILSShipCount, new MyWindow.RangeValueMapper<int>(0, 10), "G", 150f, -100f).WithFontSize(13);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigVeinCollectorHarvestSpeed, new AutoConfigVeinCollectorHarvestSpeedMapper(), "G", 150f, -100f).WithFontSize(13);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsPatch.AutoConfigVeinCollectorMinPilerValue, new AutoConfigMinPilerValueMapper(), "G", 150f, -100f).WithFontSize(13);
        x = 0f;

        var tab4 = wnd.AddTab(trans, "Player/Mecha");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab4, FactoryPatch.UnlimitInteractiveEnabled, "Unlimited interactive range");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlanetPatch.PlayerActionsInGlobeViewEnabled, "Enable player actions in globe view");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlayerPatch.HideTipsForSandsChangesEnabled, "Hide tips for soil piles changes");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab4, PlayerPatch.EnhancedMechaForgeCountControlEnabled, "Enhanced count control for hand-make");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab4, "Enhanced count control for hand-make", "Enhanced count control for hand-make tips", "enhanced-count-control-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlayerPatch.ShortcutKeysForStarsNameEnabled, "Shortcut keys for showing stars' name");

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab4, PlayerPatch.AutoNavigationEnabled, "Auto navigation on sailings");
            y += 27f;
            var autoCruiseCheckBox = wnd.AddCheckBox(x + 20f, y, tab4, PlayerPatch.AutoCruiseEnabled, "Enable auto-cruise", 13);
            y += 27f;
            var autoBoostCheckBox = wnd.AddCheckBox(x + 20f, y, tab4, PlayerPatch.AutoBoostEnabled, "Auto boost", 13);
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab4, "Distance to use warp", 15, "text-distance-to-warp");
            var navDistanceSlider = wnd.AddSlider(x + 20f + txt.preferredWidth + 5f, y + 6f, tab4, PlayerPatch.DistanceToWarp, new DistanceMapper(), "0.0", 100f);
            PlayerPatch.AutoNavigationEnabled.SettingChanged += NavSettingChanged;
            wnd.OnFree += () => { PlayerPatch.AutoNavigationEnabled.SettingChanged -= NavSettingChanged; };
            NavSettingChanged(null, null);

            void NavSettingChanged(object o, EventArgs e)
            {
                autoCruiseCheckBox.SetEnable(PlayerPatch.AutoNavigationEnabled.Value);
                autoBoostCheckBox.SetEnable(PlayerPatch.AutoNavigationEnabled.Value);
                navDistanceSlider.SetEnable(PlayerPatch.AutoNavigationEnabled.Value);
            }
        }

        var tab5 = wnd.AddTab(trans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab5, DysonSpherePatch.StopEjectOnNodeCompleteEnabled, "Stop ejectors when available nodes are all filled up");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, DysonSpherePatch.OnlyConstructNodesEnabled, "Construct only structure points but frames");
        x = 400f;
        y = 10f;
        _dysonInitBtn = wnd.AddButton(x, y, tab5, "Initialize Dyson Sphere", 16, "init-dyson-sphere", () =>
            UIMessageBox.Show("Initialize Dyson Sphere".Translate(), "Initialize Dyson Sphere Confirm".Translate(), "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null,
                () => { DysonSphereFunctions.InitCurrentDysonLayer(null, -1); })
        );
        y += 36f;
        wnd.AddText2(x, y, tab5, "Click to dismantle selected layer", 16, "text-dismantle-layer");
        y += 27f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = wnd.AddFlatButton(x, y, tab5, id.ToString(), 12, "dismantle-layer-" + id, () =>
                {
                    var star = DysonSphereFunctions.CurrentStarForDysonSystem();
                    UIMessageBox.Show("Dismantle selected layer".Translate(), "Dismantle selected layer Confirm".Translate(), "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null,
                        () => { DysonSphereFunctions.InitCurrentDysonLayer(star, id); });
                }
            ).WithSize(40f, 20f);
            DysonLayerBtn[i] = btn.uiButton;
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
        txt = wnd.AddText2(x, y, tab5, "Auto Fast Build Speed Multiplier", 15, "text-auto-fast-build-multiplier");
        wnd.AddSlider(x + txt.preferredWidth + 5f, y + 6f, tab5, DysonSpherePatch.AutoConstructMultiplier, [1, 2, 5, 10, 20, 50, 100], "0", 100f);
        _dysonTab = tab5;

        var tab6 = wnd.AddTab(trans, "Tech/Combat/UI");
        x = 10;
        y = 10;
        wnd.AddCheckBox(x, y, tab6, UIPatch.PlanetVeinUtilizationEnabled, "Planet vein utilization");
        y += 36f;
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.BatchBuyoutTechEnabled, "Buy out techs with their prerequisites");
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.SorterCargoStackingEnabled, "Restore upgrades of \"Sorter Cargo Stacking\" on panel");
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.DisableBattleRelatedTechsInPeaceModeEnabled, "Disable battle-related techs in Peace mode");
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, "Set \"Sorter Cargo Stacking\" to unresearched state", 16, "button-remove-cargo-stacking", TechFunctions.RemoveCargoStackingTechs);
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, "Unlock all techs with metadata", 16, "button-unlock-all-techs-with-metadata", TechFunctions.UnlockAllProtoWithMetadataAndPrompt);
        y += 36f;
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, "Open Dark Fog Communicator", 16, "button-open-df-communicator", () =>
        {
            if (!(GameMain.data?.gameDesc.isCombatMode ?? false)) return;
            var uiGame = UIRoot.instance.uiGame;
            uiGame.ShutPlayerInventory();
            uiGame.CloseEnemyBriefInfo();
            uiGame.OpenCommunicatorWindow(5);
        });
    }

    private static void UpdateUI()
    {
        UpdateDysonShells();
    }

    private static void UpdateDysonShells()
    {
        if (!_dysonTab.gameObject.activeSelf) return;
        var star = DysonSphereFunctions.CurrentStarForDysonSystem();
        if (star == null)
        {
            for (var i = 0; i < 10; i++)
            {
                DysonLayerBtn[i].button.interactable = false;
            }
            return;
        }
        var dysonSpheres = GameMain.data?.dysonSpheres;
        if (dysonSpheres?[star.index] == null) return;
        var ds = dysonSpheres[star.index];
        for (var i = 1; i <= 10; i++)
        {
            var layer = ds.layersIdBased[i];
            DysonLayerBtn[i - 1].button.interactable = layer != null && layer.id == i;
        }
    }
}