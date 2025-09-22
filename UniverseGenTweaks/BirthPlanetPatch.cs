using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UniverseGenTweaks;
public static class BirthPlanetPatch
{
    public static ConfigEntry<bool> SitiVeinsOnBirthPlanet;
    public static ConfigEntry<bool> FireIceOnBirthPlanet;
    public static ConfigEntry<bool> KimberliteOnBirthPlanet;
    public static ConfigEntry<bool> FractalOnBirthPlanet;
    public static ConfigEntry<bool> OrganicOnBirthPlanet;
    public static ConfigEntry<bool> OpticalOnBirthPlanet;
    public static ConfigEntry<bool> SpiniformOnBirthPlanet;
    public static ConfigEntry<bool> UnipolarOnBirthPlanet;
    public static ConfigEntry<bool> FlatBirthPlanet;
    public static ConfigEntry<bool> HighLuminosityBirthStar;

    private static BackupData _backupData;
    private static bool _initialized;
    private static Harmony _patch;

    private struct BackupData
    {
        public void FromTheme(ThemeProto theme)
        {
            _algos = theme.Algos.Clone() as int[];
            _veinSpot = theme.VeinSpot.Clone() as int[];
            _veinCount = theme.VeinCount.Clone() as float[];
            _veinOpacity = theme.VeinOpacity.Clone() as float[];
            _rareVeins = theme.RareVeins.Clone() as int[];
            _rareSettings = theme.RareSettings.Clone() as float[];
            _specifyBirthStarMass = StarGen.specifyBirthStarMass;
            _specifyBirthStarAge = StarGen.specifyBirthStarAge;
            _inited = true;
        }

        public void ToTheme(ThemeProto theme)
        {
            if (!_inited) return;
            theme.Algos = _algos.Clone() as int[];
            theme.VeinSpot = _veinSpot.Clone() as int[];
            theme.VeinCount = _veinCount.Clone() as float[];
            theme.VeinOpacity = _veinOpacity.Clone() as float[];
            theme.RareVeins = _rareVeins.Clone() as int[];
            theme.RareSettings = _rareSettings.Clone() as float[];
            StarGen.specifyBirthStarMass = _specifyBirthStarMass;
            StarGen.specifyBirthStarAge = _specifyBirthStarAge;
        }

        private bool _inited;
        private int[] _algos;
        private int[] _veinSpot;
        private float[] _veinCount;
        private float[] _veinOpacity;
        private int[] _rareVeins;
        private float[] _rareSettings;
        private float _specifyBirthStarMass;
        private float _specifyBirthStarAge;
    }

    public static void Init()
    {
        SitiVeinsOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        FireIceOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        KimberliteOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        FractalOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        OrganicOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        OpticalOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        SpiniformOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        UnipolarOnBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        FlatBirthPlanet.SettingChanged += (_, _) => PatchBirthThemeData();
        HighLuminosityBirthStar.SettingChanged += (_, _) => PatchBirthThemeData();
        PatchBirthThemeData();
        _patch ??= Harmony.CreateAndPatchAll(typeof(BirthPlanetPatch));
        GameLogicProc.OnDataLoaded += VFPreload_InvokeOnLoadWorkEnded_Postfix;
    }

    public static void Uninit()
    {
        GameLogicProc.OnDataLoaded -= VFPreload_InvokeOnLoadWorkEnded_Postfix;
        _patch?.UnpatchSelf();
        _patch = null;
    }

    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        PatchBirthThemeData();
    }

    private static void PatchBirthThemeData()
    {
        var theme = LDB.themes.Select(1);
        if (!_initialized)
        {
            _backupData.FromTheme(theme);
        }
        else
        {
            _backupData.ToTheme(theme);
        }

        if (FlatBirthPlanet.Value)
        {
            theme.Algos[0] = 2;
        }

        if (SitiVeinsOnBirthPlanet.Value)
        {
            theme.VeinSpot[2] = 2;
            theme.VeinSpot[3] = 2;
            theme.VeinCount[2] = 0.7f;
            theme.VeinCount[3] = 0.7f;
            theme.VeinOpacity[2] = 1f;
            theme.VeinOpacity[3] = 1f;
        }

        List<int> veins = [];
        List<float> settings = [];
        if (FireIceOnBirthPlanet.Value)
        {
            veins.Add(8);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (KimberliteOnBirthPlanet.Value)
        {
            veins.Add(9);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (FractalOnBirthPlanet.Value)
        {
            veins.Add(10);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (OrganicOnBirthPlanet.Value)
        {
            veins.Add(11);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (OpticalOnBirthPlanet.Value)
        {
            veins.Add(12);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (SpiniformOnBirthPlanet.Value)
        {
            veins.Add(13);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (UnipolarOnBirthPlanet.Value)
        {
            veins.Add(14);
            settings.AddRange(new[] { 1f, 1f, 0.5f, 1f });
        }

        if (veins.Count > 0)
        {
            theme.RareVeins = veins.ToArray();
            theme.RareSettings = settings.ToArray();
        }

        if (HighLuminosityBirthStar.Value)
        {
            StarGen.specifyBirthStarMass = 53.81f;
            StarGen.specifyBirthStarAge = 0.01f;
        }

        _initialized = true;
    }
}
