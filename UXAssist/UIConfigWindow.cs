using System;
using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;

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
        I18N.Add("Dyson Sphere", "Dyson Sphere", "戴森球");
        I18N.Add("Enable game window resize", "Enable game window resize (maximum box and thick frame)", "可调整游戏窗口大小(可最大化和拖动边框)");
        I18N.Add("Remeber window position and size on last exit", "Remeber window position and size on last exit", "记住上次退出时的窗口位置和大小");
        /*
        I18N.Add("Better auto-save mechanism", "Better auto-save mechanism", "更好的自动存档机制");
        I18N.Add("Better auto-save mechanism tips", "Auto saves are stored in 'Save\\AutoSaves' folder, filenames are combined with cluster address and date-time", "自动存档会以星区地址和日期时间组合为文件名存储在'Save\\AutoSaves'文件夹中");
        */
        I18N.Add("Convert old saves to Combat Mode on loading", "Convert old saves to Combat Mode on loading (Use settings in new game panel)", "读取旧档时转为战斗模式(使用新游戏面板的战斗难度设置)");
        I18N.Add("Unlimited interactive range", "Unlimited interactive range", "无限交互距离");
        I18N.Add("Night Light", "Sunlight at night", "夜间日光灯");
        I18N.Add("Remove some build conditions", "Remove some build conditions", "移除部分不影响游戏逻辑的建造条件");
        I18N.Add("Remove build range limit", "Remove build count and range limit", "移除建造数量和距离限制");
        I18N.Add("Larger area for upgrade and dismantle", "Larger area for upgrade and dismantle", "范围升级和拆除的最大区域扩大");
        I18N.Add("Larger area for terraform", "Larger area for terraform", "范围铺设地基的最大区域扩大");
        I18N.Add("Off-grid building and stepped rotation", "Off-grid building and stepped rotation (Hold Shift)", "脱离网格建造以及小角度旋转(按住Shift)");
        I18N.Add("Enable player actions in globe view", "Enable player actions in globe view", "在行星视图中允许玩家操作");
        I18N.Add("Hide tips for soil piles changes", "Hide tips for soil piles changes", "隐藏沙土数量变动的提示");
        I18N.Add("Enhance control for logistic storage limits", "Enhance control for logistic storage limits", "物流塔存储限制控制改进");
        I18N.Add("Enhance control for logistic storage limits tips", "Logistic storage limits are not scaled on upgrading 'Logistics Carrier Capacity', if they are not set to maximum capacity.\nUse arrow keys to adjust logistic storage limits:\n  \u2190/\u2192: -/+10  \u2193\u2191: -/+100", "当升级'运输机舱扩容'时，不会对各种物流塔的存储限制按比例提升，除非设置为最大允许容量。\n你可以使用方向键微调物流塔存储限制：\n  \u2190\u2192: -/+10  \u2193\u2191: -/+100");
        I18N.Add("Enhanced count control for hand-make", "Enhanced count control for hand-make", "手动制造物品的数量控制改进");
        I18N.Add("Enhanced count control for hand-make tips", "Maximum count is increased to 1000.\nHold Ctrl/Shift/Alt to change the count rapidly.", "最大数量提升至1000\n按住Ctrl/Shift/Alt可快速改变数量");
        I18N.Add("Auto navigation on sailings", "Auto navigation on sailings", "宇宙航行时自动导航");
        I18N.Add("Enable auto-cruise", "Enable auto-cruise", "启用自动巡航");
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
        I18N.Add("Construct only nodes but frames", "Construct only nodes but frames", "只造节点不造框架");
        I18N.Add("Initialize Dyson Sphere", "Initialize Dyson Sphere", "初始化戴森球");
        I18N.Add("Initialize Dyson Sphere Confirm", "This operation will destroy all layers on this dyson sphere, are you sure?", "此操作将会摧毁戴森球上的所有层级，确定吗？");
        I18N.Add("Click to dismantle selected layer", "Click to dismantle selected layer", "点击拆除对应的戴森壳");
        I18N.Add("Dismantle selected layer", "Dismantle selected layer", "拆除选中的戴森壳");
        I18N.Add("Dismantle selected layer Confirm", "This operation will dismantle selected layer, are you sure?", "此操作将会拆除选中的戴森壳，确定吗？");
        I18N.Apply();
        MyConfigWindow.OnUICreated += CreateUI;
        MyConfigWindow.OnUpdateUI += UpdateUI;
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        _windowTrans = trans;
        wnd.AddTabGroup(trans, "UXAssist", "tab-group-uxassist");
        var tab1 = wnd.AddTab(trans, "General");
        var x = 0f;
        var y = 10f;
        MyCheckBox.CreateCheckBox(x, y, tab1, GamePatch.EnableWindowResizeEnabled, "Enable game window resize");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, GamePatch.LoadLastWindowRectEnabled, "Remeber window position and size on last exit");
        y += 36f;
        /*
        MyCheckBox.CreateCheckBox(x, y, tab1, GamePatch.AutoSaveOptEnabled, "Better auto-save mechanism");
        x = 200f;
        y += 6f;
        MyWindow.AddTipsButton(x, y, tab1, "Better auto-save mechanism", "Better auto-save mechanism tips", "auto-save-opt-tips");
        x = 0f;
        y += 30f;
        */
        MyCheckBox.CreateCheckBox(x, y, tab1, GamePatch.ConvertSavesFromPeaceEnabled, "Convert old saves to Combat Mode on loading");
        var tab2 = wnd.AddTab(trans, "Planet/Factory");
        x = 0f;
        y = 10f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.UnlimitInteractiveEnabled, "Unlimited interactive range");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.RemoveSomeConditionEnabled, "Remove some build conditions");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.RemoveBuildRangeLimitEnabled, "Remove build range limit");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.NightLightEnabled, "Night Light");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled, "Larger area for upgrade and dismantle");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.LargerAreaForTerraformEnabled, "Larger area for terraform");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.OffGridBuildingEnabled, "Off-grid building and stepped rotation");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.TreatStackingAsSingleEnabled, "Treat stack items as single in monitor components");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, PlanetPatch.PlayerActionsInGlobeViewEnabled, "Enable player actions in globe view");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, PlayerPatch.HideTipsForSandsChangesEnabled, "Hide tips for soil piles changes");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, FactoryPatch.LogisticsCapacityTweaksEnabled, "Enhance control for logistic storage limits");
        x = 270f;
        y += 6f;
        MyWindow.AddTipsButton(x, y, tab2, "Enhance control for logistic storage limits", "Enhance control for logistic storage limits tips", "enhanced-logistic-limit-tips");
        x = 0f;
        y += 30f;
        MyCheckBox.CreateCheckBox(x, y, tab2, PlayerPatch.EnhancedMechaForgeCountControlEnabled, "Enhanced count control for hand-make");
        x = 270f;
        y += 6f;
        MyWindow.AddTipsButton(x, y, tab2, "Enhanced count control for hand-make", "Enhanced count control for hand-make tips", "enhanced-count-control-tips");
        x = 350f;
        y -= 108f;
        MyCheckBox.CreateCheckBox(x, y, tab2, PlayerPatch.AutoNavigationEnabled, "Auto navigation on sailings");
        x += 20f;
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab2, PlayerPatch.AutoCruiseEnabled, "Enable auto-cruise", 14);
        x -= 20f;
        y += 36f;
        MyWindow.AddText(x, y, tab2, "Distance to use warp", 15, "text-distance-to-warp");
        y += 24f;
        var distanceToWarp = MySlider.CreateSlider(x, y, tab2, (float)Math.Round(PlayerPatch.DistanceToWarp.Value * 2.0), 1f, 40f, "0.0", 200f);
        if (PlanetFunctions.OrbitalCollectorMaxBuildCount.Value == 0)
        {
            distanceToWarp.SetLabelText(PlayerPatch.DistanceToWarp.Value.ToString("0.0"));
        }
        distanceToWarp.OnValueChanged += () =>
        {
            PlayerPatch.DistanceToWarp.Value = Math.Round(distanceToWarp.Value) * 0.5;
            distanceToWarp.SetLabelText(PlayerPatch.DistanceToWarp.Value.ToString("0.0"));
        };
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab2, "Initialize This Planet", 16, "button-init-planet", () =>
            UIMessageBox.Show("Initialize This Planet".Translate(), "Initialize This Planet Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null, () =>
            {
                PlanetFunctions.RecreatePlanet(true);
            })
        );
        y += 36f;
        wnd.AddButton(x, y, tab2, "Dismantle All Buildings", 16, "button-dismantle-all", () =>
            UIMessageBox.Show("Dismantle All Buildings".Translate(), "Dismantle All Buildings Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null, () =>
            {
                PlanetFunctions.DismantleAll(false);
            })
        );
        y += 72f;
        wnd.AddButton(x, y, 200, tab2, "Quick build Orbital Collectors", 16, "button-init-planet", PlanetFunctions.BuildOrbitalCollectors);
        x += 10f;
        y += 30f;
        MyWindow.AddText(x, y, tab2, "Maximum count to build", 15, "text-oc-build-count");
        y += 24f;
        var ocBuildSlider = MySlider.CreateSlider(x, y, tab2, PlanetFunctions.OrbitalCollectorMaxBuildCount.Value, 0f, 40f, "G", 200f);
        if (PlanetFunctions.OrbitalCollectorMaxBuildCount.Value == 0)
        {
            ocBuildSlider.SetLabelText("max".Translate());
        }
        ocBuildSlider.OnValueChanged += () =>
        {
            PlanetFunctions.OrbitalCollectorMaxBuildCount.Value = Mathf.RoundToInt(ocBuildSlider.Value);
            if (PlanetFunctions.OrbitalCollectorMaxBuildCount.Value == 0)
            {
                ocBuildSlider.SetLabelText("max".Translate());
            }
        };

        var tab3 = wnd.AddTab(trans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        MyCheckBox.CreateCheckBox(x, y, tab3, DysonSpherePatch.StopEjectOnNodeCompleteEnabled, "Stop ejectors when available nodes are all filled up");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab3, DysonSpherePatch.OnlyConstructNodesEnabled, "Construct only nodes but frames");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab3, "Initialize Dyson Sphere", 16, "init-dyson-sphere", () =>
            UIMessageBox.Show("Initialize Dyson Sphere".Translate(), "Initialize Dyson Sphere Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null, () =>
            {
                DysonSpherePatch.InitCurrentDysonSphere(-1);
            })
        );
        y += 36f;
        MyWindow.AddText(x, y, tab3, "Click to dismantle selected layer", 16, "text-dismantle-layer");
        y += 26f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = wnd.AddFlatButton(x, y, tab3, id.ToString(), 12, "dismantle-layer-" + id, () =>
                UIMessageBox.Show("Dismantle selected layer".Translate(), "Dismantle selected layer Confirm".Translate(), "取消".Translate(), "确定".Translate(), 2, null, () =>
                {
                    DysonSpherePatch.InitCurrentDysonSphere(id);
                })
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
        _dysonTab = tab3;
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
