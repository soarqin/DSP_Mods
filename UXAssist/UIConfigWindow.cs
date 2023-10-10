using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;

namespace UXAssist;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;
    private static RectTransform _tab;
    private static readonly UIButton[] _dysonLayerBtn = new UIButton[10];

    public static void Init()
    {
        I18N.Add("UXAssist", "UXAssist", "UX助手");
        I18N.Add("Unlimited interactive range", "Unlimited interactive range", "无限交互距离");
        I18N.Add("Night Light", "Sunlight at night", "夜间日光灯");
        I18N.Add("Remove some build conditions", "Remove some build conditions", "移除部分不影响游戏逻辑的建造条件");
        I18N.Add("Remove build range limit", "Remove build count and range limit", "移除建造数量和距离限制");
        I18N.Add("Larger area for upgrade and dismantle", "Larger area for upgrade and dismantle", "范围升级和拆除的最大区域扩大");
        I18N.Add("Larger area for terraform", "Larger area for terraform", "范围铺设地基的最大区域扩大");
        I18N.Add("Enable player actions in globe view", "Enable player actions in globe view", "在行星视图中允许玩家操作");
        I18N.Add("Enhanced count control for hand-make", "Enhanced count control for hand-make", "手动制造物品的数量控制改进");
        I18N.Add("Enhanced count control for hand-make tips", "Maximum count is increased to 1000.\nHold Ctrl/Shift/Alt to change the count rapidly.", "最大数量提升至1000\n按住Ctrl/Shift/Alt可快速改变数量");
        I18N.Add("Initialize This Planet", "Initialize this planet", "初始化本行星");
        I18N.Add("Dismantle All Buildings", "Dismantle all buildings", "拆除所有建筑");
        I18N.Add("Stop ejectors when available nodes are all filled up", "Stop ejectors when available nodes are all filled up", "可用节点全部造完时停止弹射");
        I18N.Add("Construct only nodes but frames", "Construct only nodes but frames", "只造节点不造框架");
        I18N.Add("Initialize Dyson Sphere", "Initialize Dyson Sphere", "初始化戴森球");
        I18N.Add("Click to dismantle selected layer", "Click to dismantle selected layer", "点击拆除对应的戴森壳");
        I18N.Apply();
        MyConfigWindow.OnUICreated += CreateUI;
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        _windowTrans = trans;
        var tab1 = wnd.AddTab(_windowTrans, "UXAssist");
        var x = 0f;
        var y = 10f;
        MyCheckBox.CreateCheckBox(x, y, tab1, FactoryPatch.UnlimitInteractiveEnabled, "Unlimited interactive range");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, FactoryPatch.NightLightEnabled, "Night Light");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, FactoryPatch.RemoveSomeConditionEnabled, "Remove some build conditions");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, FactoryPatch.RemoveBuildRangeLimitEnabled, "Remove build range limit");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled, "Larger area for upgrade and dismantle");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, FactoryPatch.LargerAreaForTerraformEnabled, "Larger area for terraform");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, PlanetPatch.PlayerActionsInGlobeViewEnabled, "Enable player actions in globe view");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, PlayerPatch.EnhancedMechaForgeCountControlEnabled, "Enhanced count control for hand-make");
        x = 240f;
        y += 6f;
        MyWindow.AddTipsButton(x, y, tab1, "Enhanced count control for hand-make", "Enhanced count control for hand-make tips", "enhanced-count-control-tips");
        x = 0f;
        y += 30f;
        MyCheckBox.CreateCheckBox(x, y, tab1, DysonSpherePatch.StopEjectOnNodeCompleteEnabled, "Stop ejectors when available nodes are all filled up");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab1, DysonSpherePatch.OnlyConstructNodesEnabled, "Construct only nodes but frames");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab1, "Initialize This Planet", 16, "button-init-planet", () => { PlanetFunctions.RecreatePlanet(true); });
        y += 36f;
        wnd.AddButton(x, y, tab1, "Dismantle All Buildings", 16, "button-dismantle-all", () => { PlanetFunctions.DismantleAll(false); });
        y += 36f;
        y += 36f;
        wnd.AddButton(x, y, tab1, "Initialize Dyson Sphere", 16, "init-dyson-sphere", () => { DysonSpherePatch.InitCurrentDysonSphere(-1); });
        y += 36f;
        MyWindow.AddText(x, y, tab1, "Click to dismantle selected layer", 16, "text-dismantle-layer");
        y += 26f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = wnd.AddFlatButton(x, y, tab1, id.ToString(), 12, "dismantle-layer-" + id, () => { DysonSpherePatch.InitCurrentDysonSphere(id); });
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
        x = 400f;
        y += 72f;
        MyKeyBinder.CreateKeyBinder(x, y, tab1, UXAssist.Hotkey, "Hotkey");

        _tab = tab1;
    }

    private static void UpdateUI()
    {
        UpdateDysonShells();
    }

    private static void UpdateDysonShells()
    {
        if (!_tab.gameObject.activeSelf) return;
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
}
