using UXAssist.Common;

namespace UniverseGenTweaks;

public static class Localization
{
    public const string Micro = "Micro";
    public const string EpicDifficultyLabel = "Epic Difficulty !!";
    public const string VeryHard = "Very Hard";
    public const string StarDistanceMin = "Star Distance Min";
    public const string StepDistanceMin = "Step Distance Min";
    public const string StepDistanceMax = "Step Distance Max";
    public const string Flatness = "Flatness";
    public const string UniverseGen = "UniverseGen";
    public const string BirthStar = "Birth Star";
    public const string EnableMoreSettingsOnUniverseGen = "Enable more settings on UniverseGen";
    public const string RequiresGameRestartToTakeEffect = "* Requires game restart to take effect";
    public const string MaximumStarCount = "Maximum star count";
    public const string EnableEpicDifficulty = "Enable Epic difficulty";
    public const string ResourceMultiplier = "Resource multiplier";
    public const string OilMultiplierRelativeToVeryHard = "Oil multiplier (relative to Very Hard)";
    public const string SiliconTitaniumOnBirthPlanet = "Silicon/Titanium on birth planet";
    public const string FireIceOnBirthPlanet = "Fire ice on birth planet";
    public const string KimberliteOnBirthPlanet = "Kimberlite on birth planet";
    public const string FractalSiliconOnBirthPlanet = "Fractal silicon on birth planet";
    public const string OrganicCrystalOnBirthPlanet = "Organic crystal on birth planet";
    public const string OpticalGratingCrystalOnBirthPlanet = "Optical grating crystal on birth planet";
    public const string SpiniformStalagmiteCrystalOnBirthPlanet = "Spiniform stalagmite crystal on birth planet";
    public const string UnipolarMagnetOnBirthPlanet = "Unipolar magnet on birth planet";
    public const string BirthPlanetIsSolidFlatNoWaterAtAll = "Birth planet is solid flat (no water at all)";
    public const string BirthStarHasHighLuminosity = "Birth star has high luminosity";
    public const string PropertyMultiplier = "Property multiplier";
    public const string Unlimited = "Unlimited";
    public const string Scarce = "Scarce";
    public const string AggressivenessNormal = "Normal";
    public const string AggressivenessSittingDuck = "Sitting Duck";
    public const string AggressivenessNegative = "Negative";
    public const string AggressivenessRampage = "Rampage";
    public const string AggressivenessAggressive = "Aggressive";
    public const string AggressivenessPassive = "Passive";
    public const string DifficultyValueFormat = "Difficulty value: {0}";

    public static void Register()
    {
        I18N.Add(Micro, "Micro", "究极少");
        I18N.Add(EpicDifficultyLabel, "Epic Difficulty !!", "史诗难度 !!");
        I18N.Add(VeryHard, "Very Hard", "非常困难");
        I18N.Add(StarDistanceMin, "Star Distance Min", "恒星最小距离");
        I18N.Add(StepDistanceMin, "Step Distance Min", "步进最小距离");
        I18N.Add(StepDistanceMax, "Step Distance Max", "步进最大距离");
        I18N.Add(Flatness, "Flatness", "扁平度");
        I18N.Add(UniverseGen, "UniverseGen", "宇宙生成");
        I18N.Add(BirthStar, "Birth Star", "母星系");
        I18N.Add(EnableMoreSettingsOnUniverseGen, "Enable more settings on UniverseGen", "启用更多宇宙生成设置");
        I18N.Add(RequiresGameRestartToTakeEffect, "* Requires game restart to take effect", "* 需要重启游戏才能生效");
        I18N.Add(MaximumStarCount, "Maximum star count", "最大恒星数");
        I18N.Add(EnableEpicDifficulty, "Enable Epic difficulty", "启用史诗难度");
        I18N.Add(ResourceMultiplier, "Resource multiplier", "资源倍率");
        I18N.Add(OilMultiplierRelativeToVeryHard, "Oil multiplier (relative to Very Hard)", "石油倍率（相对于非常困难）");
        I18N.Add(SiliconTitaniumOnBirthPlanet, "Silicon/Titanium on birth planet", "母星有硅和钛");
        I18N.Add(FireIceOnBirthPlanet, "Fire ice on birth planet", "母星有可燃冰");
        I18N.Add(KimberliteOnBirthPlanet, "Kimberlite on birth planet", "母星有金伯利矿");
        I18N.Add(FractalSiliconOnBirthPlanet, "Fractal silicon on birth planet", "母星有分形硅");
        I18N.Add(OrganicCrystalOnBirthPlanet, "Organic crystal on birth planet", "母星有有机晶体");
        I18N.Add(OpticalGratingCrystalOnBirthPlanet, "Optical grating crystal on birth planet", "母星有光栅石");
        I18N.Add(SpiniformStalagmiteCrystalOnBirthPlanet, "Spiniform stalagmite crystal on birth planet", "母星有刺笋结晶");
        I18N.Add(UnipolarMagnetOnBirthPlanet, "Unipolar magnet on birth planet", "母星有单极磁石");
        I18N.Add(BirthPlanetIsSolidFlatNoWaterAtAll, "Birth planet is solid flat (no water at all)", "母星是纯平的（没有水）");
        I18N.Add(BirthStarHasHighLuminosity, "Birth star has high luminosity", "母星系恒星高亮");
        I18N.Add(PropertyMultiplier, "Property multiplier", "元数据生成倍率");
        I18N.Add(Unlimited, "Unlimited", "无限");
        I18N.Add(Scarce, "Scarce", "极少");
        I18N.Add(AggressivenessNormal, "Normal", "正常");
        I18N.Add(AggressivenessSittingDuck, "Sitting Duck", "活靶子");
        I18N.Add(AggressivenessNegative, "Negative", "消极");
        I18N.Add(AggressivenessRampage, "Rampage", "狂暴");
        I18N.Add(AggressivenessAggressive, "Aggressive", "积极");
        I18N.Add(AggressivenessPassive, "Passive", "被动");
        I18N.Add(DifficultyValueFormat, "Difficulty value: {0}", "难度系数值：{0}");
    }
}
