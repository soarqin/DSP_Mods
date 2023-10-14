using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;

namespace UniverseGenTweaks;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;

    public static void Init()
    {
        I18N.Add("GalaxyGen", "+UniverseGen+", "+宇宙生成+");
        I18N.Add("Enable more settings on UniverseGen", "Enable more settings on UniverseGen", "启用更多宇宙生成设置");
        I18N.Add("* Requires game restart to take effect", "* Requires game restart to take effect", "* 需要重启游戏才能生效");
        I18N.Add("Enable Epic difficulty", "Enable Epic difficulty", "启用史诗难度");
        I18N.Add("Resource multiplier", "Resource multiplier", "资源倍率");
        I18N.Add("Oil multiplier (relative to Very Hard)", "Oil multiplier (relative to Very Hard)", "石油倍率（相对于非常困难）");
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
        MyConfigWindow.OnUICreated += CreateUI;
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        _windowTrans = trans;
        // General tab
        var x = 0f;
        var y = 10f;
        var tab = wnd.AddTab(_windowTrans, "GalaxyGen");
        MyCheckBox.CreateCheckBox(x, y, tab, MoreSettings.Enabled, "Enable more settings on UniverseGen");
        x += 20f;
        y += 26f;
        MyWindow.AddText(x, y, tab, "* Requires game restart to take effect", 13);
        x -= 20f;
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, EpicDifficulty.Enabled, "Enable Epic difficulty");
        y += 36f;
        x += 10f;
        MyWindow.AddText(x, y, tab, "Resource multiplier", 16);
        x += 20f;
        y += 26f;
        var index = EpicDifficulty.ResourceMultiplierToIndex(EpicDifficulty.ResourceMultiplier.Value);
        var sl = MySlider.CreateSlider(x, y, tab, index, 0f, (float)EpicDifficulty.ResourceMultipliersCount() - 1, "G", 240f);
        sl.SetLabelText(EpicDifficulty.IndexToResourceMultiplier(Mathf.RoundToInt(sl.Value)).ToString(sl.labelFormat));
        sl.OnValueChanged += () =>
        {
            var val = EpicDifficulty.IndexToResourceMultiplier(Mathf.RoundToInt(sl.Value));
            EpicDifficulty.ResourceMultiplier.Value = val;
            sl.SetLabelText(val.ToString(sl.labelFormat));
        };
        x -= 30f;
        y += 31f;
        x += 10f;
        MyWindow.AddText(x, y, tab, "Oil multiplier (relative to Very Hard)", 16);
        x += 20f;
        y += 26f;
        index = EpicDifficulty.OilMultiplierToIndex(EpicDifficulty.OilMultiplier.Value);
        var sl2 = MySlider.CreateSlider(x, y, tab, index, 0f, (float)EpicDifficulty.OilMultipliersCount() - 1, "G", 240f);
        sl2.SetLabelText(EpicDifficulty.IndexToOilMultiplier(Mathf.RoundToInt(sl2.Value)).ToString(sl2.labelFormat));
        sl2.OnValueChanged += () =>
        {
            var val = EpicDifficulty.IndexToOilMultiplier(Mathf.RoundToInt(sl2.Value));
            EpicDifficulty.OilMultiplier.Value = val; 
            sl2.SetLabelText(val.ToString(sl2.labelFormat));
        };
        x = 300f;
        y = 10f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.SitiVeinsOnBirthPlanet, "Silicon/Titanium on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.FireIceOnBirthPlanet, "Fire ice on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.KimberliteOnBirthPlanet, "Kimberlite on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.FractalOnBirthPlanet, "Fractal silicon on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.OrganicOnBirthPlanet, "Organic crystal on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.OpticalOnBirthPlanet, "Optical grating crystal on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.SpiniformOnBirthPlanet, "Spiniform stalagmite crystal on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.UnipolarOnBirthPlanet, "Unipolar magnet on birth planet");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.FlatBirthPlanet, "Birth planet is solid flat (no water at all)");
        y += 36f;
        MyCheckBox.CreateCheckBox(x, y, tab, BirthPlanetPatch.HighLuminosityBirthStar, "Birth star has high luminosity");
    }
}