using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UXAssist.Common;
using UXAssist.UI;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Functions.UI;

internal static class StarmapFilterUI
{
    private static int _cornerComboBoxIndex;
    private static string[] _starOrderNames;
    private static bool _starmapFilterInitialized;
    private static ulong[] _starmapStarFilterValues;
    private static bool _starFilterEnabled;
    public static MyCheckButton StarmapFilterToggler;
    public static bool[] ShowStarName;

    private static readonly Sprite[] PlanetIcons = [
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/07.png"),
        null,
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/09.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/10.png"),
        null,
        null,
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/13.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/14.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/15.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/16.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/17.png"),
        null,
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/19.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/20.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/21.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/22.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/23.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/24.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/25.png")
    ];

    private static readonly (int, int)[] FilterVeinIds = [(9, 0), (10, 0), (11, 0), (12, 0), (13, 0), (14, 0), (7, 0), (0, 1116), (0, 1000), (8, 1011), (0, 1120), (0, 1121)];
    private static readonly int[] FilterPlanetThemes = [16, 23, 15, 22, 25, 21, 14, 17, 19, 7, 10, 20, 24, 9, 13];
    private static readonly Dictionary<int, int> ItemToVeinBitFlagMap = new()
    {
        {1011, 8},
        {1120, 20},
        {1121, 21},
        {1116, 22},
        {1000, 23},
    };

    public static void Init()
    {
                                                                    }

    public static void Start()
    {
    }

    public static void Uninit()
    {
    }

    public static void OnInputUpdate()
    {
    }

    public static void OnUpdate()
    {
    }

    public static void InitStarmapButtons()
    {
        var uiRoot = UIRoot.instance;
        if (!uiRoot) return;

        var rect = uiRoot.uiGame.starmap.transform as RectTransform;
        var panel = new GameObject("uxassist-starmap-panel");
        var rtrans = panel.AddComponent<RectTransform>();
        panel.transform.SetParent(rect);
        rtrans.sizeDelta = new Vector2(0f, 0f);
        rtrans.localScale = new Vector3(1f, 1f, 1f);
        rtrans.anchorMax = new Vector2(1f, 1f);
        rtrans.anchorMin = new Vector2(0f, 0f);
        rtrans.pivot = new Vector2(0f, 1f);
        rtrans.anchoredPosition3D = new Vector3(0, 0, 0f);

        var cornerComboBox = MyCornerComboBox.CreateComboBox(135, 0, rtrans, true).WithItems(I18NKeys.ShowOriginalName.Translate(), I18NKeys.ShowDistance.Translate(), I18NKeys.ShowPlanetCount.Translate(), I18NKeys.ShowAllInformation.Translate());
        cornerComboBox.SetIndex(CornerComboBoxIndex);
        cornerComboBox.OnSelChanged += (index) =>
        {
            CornerComboBoxIndex = index;
        };
        StarmapFilterToggler = MyCheckButton.CreateCheckButton(0, 0, rtrans, false, ">>").WithSize(24, 24);
        MyCheckButton[] buttons = [
            MyCheckButton.CreateCheckButton(24, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Kimberlite
            MyCheckButton.CreateCheckButton(48, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Fractal Silicon
            MyCheckButton.CreateCheckButton(72, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Organic Crystal
            MyCheckButton.CreateCheckButton(96, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Grating Crystal
            MyCheckButton.CreateCheckButton(120, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Stalagmite Crystal
            MyCheckButton.CreateCheckButton(144, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Unipolar Magnet
            MyCheckButton.CreateCheckButton(168, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Crude Oil
            MyCheckButton.CreateCheckButton(192, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Fire Ice
            MyCheckButton.CreateCheckButton(216, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Sulfuric Acid
            MyCheckButton.CreateCheckButton(240, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Water
            MyCheckButton.CreateCheckButton(264, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Hydrogen
            MyCheckButton.CreateCheckButton(288, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Deuterium

            MyCheckButton.CreateCheckButton(24, 24, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 48, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 72, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 96, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 120, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 144, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 168, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),

            MyCheckButton.CreateCheckButton(24, 210, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 234, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 258, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 282, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 306, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 330, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 354, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            MyCheckButton.CreateCheckButton(24, 378, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
        ];
        var unionCheckBox = MyCheckBox.CreateCheckBox(312, 0, rtrans, false, I18NKeys.UnionResults.Translate(), 15).WithSmallerBox(24f);
        unionCheckBox.gameObject.SetActive(false);
        unionCheckBox.OnChecked += () =>
        {
            UpdateStarmapStarFilters();
        };
        var allOresText = MyWindow.AddText(20, 190, rtrans, I18NKeys.All6BasicOres.Translate(), 12);
        allOresText.gameObject.SetActive(false);
        StarmapFilterToggler.OnChecked += UpdateButtons;
        foreach (var button in buttons)
        {
            button.OnChecked += () =>
            {
                if (button.Checked && !VFInput.shift && !VFInput.control)
                {
                    foreach (var b in buttons)
                    {
                        if (b != button) b.Checked = false;
                    }
                }
                UpdateStarmapStarFilters();
            };
        }

        I18N.OnInitialized += UpdateI18N;
        GameLogicProc.OnDataLoaded += () =>
        {
            VeinProto veinProto;
            ItemProto itemProto;
            for (int i = 0; i < 12; i++)
            {
                var (veinProtoId, itemProtoId) = FilterVeinIds[i];
                if (itemProtoId != 0)
                {
                    itemProto = LDB.items.Select(itemProtoId);
                    buttons[i].SetIcon(itemProto.iconSprite);
                }
                else if (veinProtoId != 0)
                {
                    veinProto = LDB.veins.Select(veinProtoId);
                    buttons[i].SetIcon(veinProto.iconSprite);
                }
            }

            for (int i = 0; i < FilterPlanetThemes.Length; i++)
            {
                buttons[12 + i].SetIcon(PlanetIcons[FilterPlanetThemes[i]]);
            }
            UpdateI18N();
        };

        GameLogicProc.OnGameBegin += () =>
        {
            if (DSPGame.IsMenuDemo) return;

            var galaxy = GameMain.data.galaxy;
            ShowStarName = new bool[galaxy.starCount];
            _starOrderNames = new string[galaxy.starCount];
            _starmapStarFilterValues = new ulong[galaxy.starCount];
            StarData[] stars = [.. galaxy.stars.Where(star => star != null)];
            Array.Sort(stars, (a, b) =>
            {
                int res = a.position.sqrMagnitude.CompareTo(b.position.sqrMagnitude);
                if (res != 0) return res;
                return a.index.CompareTo(b.index);
            });
            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                _starOrderNames[star.index] = star.displayName;
            }
            int[] spectrCount = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                var index = star.index;
                switch (star.type)
                {
                    case EStarType.MainSeqStar:
                        switch (star.spectr)
                        {
                            case ESpectrType.M:
                                _starOrderNames[index] = String.Format("M{0}", ++spectrCount[0]);
                                break;
                            case ESpectrType.K:
                                _starOrderNames[index] = String.Format("K{0}", ++spectrCount[1]);
                                break;
                            case ESpectrType.G:
                                _starOrderNames[index] = String.Format("G{0}", ++spectrCount[2]);
                                break;
                            case ESpectrType.F:
                                _starOrderNames[index] = String.Format("F{0}", ++spectrCount[3]);
                                break;
                            case ESpectrType.A:
                                _starOrderNames[index] = String.Format("A{0}", ++spectrCount[4]);
                                break;
                            case ESpectrType.B:
                                _starOrderNames[index] = String.Format("B{0}", ++spectrCount[5]);
                                break;
                            case ESpectrType.O:
                                _starOrderNames[index] = String.Format("O{0}", ++spectrCount[6]);
                                break;
                        }
                        break;
                    case EStarType.GiantStar:
                        _starOrderNames[index] = String.Format("GS{0}", ++spectrCount[7]);
                        break;
                    case EStarType.WhiteDwarf:
                        _starOrderNames[index] = String.Format("WD{0}", ++spectrCount[8]);
                        break;
                    case EStarType.NeutronStar:
                        _starOrderNames[index] = String.Format("NS{0}", ++spectrCount[9]);
                        break;
                    case EStarType.BlackHole:
                        _starOrderNames[index] = String.Format("BH{0}", ++spectrCount[10]);
                        break;
                }
            }
            StarmapFilterToggler.gameObject.SetActive(false);
            StarmapFilterToggler.Checked = false;
            UpdateButtons();
            SetStarFilterEnabled(false);
            foreach (var star in galaxy.stars)
            {
                if (star != null) PlanetModelingManager.RequestScanStar(star);
            }
            _starmapFilterInitialized = true;
            if (PlanetModelingManager.scnPlanetReqList.Count == 0)
            {
                OnPlanetScanEnded();
            }
        };
        GameLogicProc.OnGameEnd += () =>
        {
            _starOrderNames = null;
            ShowStarName = null;
            _starmapStarFilterValues = null;
            _starmapFilterInitialized = false;
        };
        void UpdateI18N()
        {
            if (cornerComboBox != null)
            {
                var items = cornerComboBox.Items;
                cornerComboBox.UpdateLabelText();
                items[0] = I18NKeys.ShowOriginalName.Translate();
                items[1] = I18NKeys.ShowDistance.Translate();
                items[2] = I18NKeys.ShowPlanetCount.Translate();
                items[3] = I18NKeys.ShowAllInformation.Translate();
            }
            if (buttons != null)
            {
                VeinProto veinProto;
                ItemProto itemProto;
                for (int i = 0; i < 12; i++)
                {
                    var (veinProtoId, itemProtoId) = FilterVeinIds[i];
                    if (itemProtoId != 0)
                    {
                        itemProto = LDB.items.Select(itemProtoId);
                        buttons[i].WithTip(itemProto.Name);
                    }
                    else if (veinProtoId != 0)
                    {
                        veinProto = LDB.veins.Select(veinProtoId);
                        buttons[i].WithTip(veinProto.Name);
                    }
                }
                for (int i = 0; i < FilterPlanetThemes.Length; i++)
                {
                    var theme = FilterPlanetThemes[i];
                    var themeProto = LDB.themes.Select(theme);
                    switch (i)
                    {
                        case 5:
                            buttons[12 + i].SetLabelText($"{themeProto.DisplayName.Translate()} ({I18NKeys.HighYield.Translate()})");
                            break;
                        case 6:
                            buttons[12 + i].SetLabelText($"{themeProto.DisplayName.Translate()} ({I18NKeys.Perfect.Translate()})");
                            break;
                        default:
                            buttons[12 + i].SetLabelText(themeProto.DisplayName.Translate());
                            break;
                    }
                }
            }
            if (allOresText != null) allOresText.text = I18NKeys.All6BasicOres.Translate();
        }
        void UpdateButtons()
        {
            var chk = StarmapFilterToggler.Checked;
            foreach (var button in buttons)
            {
                if (chk)
                    button.gameObject.SetActive(true);
                else
                {
                    button.gameObject.SetActive(false);
                    button.Checked = false;
                }
            }
            unionCheckBox.gameObject.SetActive(chk);
            allOresText.gameObject.SetActive(chk);
            StarmapFilterToggler.SetLabelText(chk ? "X" : ">>");
            if (!chk)
            {
                UpdateStarmapStarFilters();
            }
            UIRoot.instance.uiGame.dfMonitor.transform.parent.gameObject.SetActive(!chk);
        }
        void UpdateStarmapStarFilters()
        {
            var filterValue = 0UL;
            var union = unionCheckBox.Checked;
            if (StarmapFilterToggler.Checked)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (buttons[i].Checked)
                    {
                        var (veinProtoId, itemProtoId) = FilterVeinIds[i];
                        if (veinProtoId != 0)
                        {
                            filterValue |= 1UL << veinProtoId;
                        }
                        else if (itemProtoId != 0)
                        {
                            filterValue |= 1UL << ItemToVeinBitFlagMap[itemProtoId];
                        }
                    }
                }
                for (int i = 0; i < FilterPlanetThemes.Length; i++)
                {
                    if (buttons[12 + i].Checked)
                    {
                        filterValue |= 1UL << (30 + FilterPlanetThemes[i]);
                    }
                }
            }
            if (filterValue == 0UL)
            {
                for (int i = 0; i < ShowStarName.Length; i++)
                {
                    ShowStarName[i] = false;
                }
                SetStarFilterEnabled(false);
                return;
            }
            for (int i = _starmapStarFilterValues.Length - 1; i >= 0; i--)
            {
                ShowStarName[i] = union ? (_starmapStarFilterValues[i] & filterValue) != 0 : (_starmapStarFilterValues[i] & filterValue) == filterValue;
            }
            SetStarFilterEnabled(true);
        }
    }

    public static void OnPlanetScanEnded()
    {
        if (!_starmapFilterInitialized || StarmapFilterToggler == null || StarmapFilterToggler.gameObject.activeSelf) return;
        StarmapUpdateFilterValues();
        StarmapFilterToggler.gameObject.SetActive(true);
    }

    private static void StarmapUpdateFilterValues()
    {
        var galaxy = GameMain.data.galaxy;
        var stars = galaxy.stars;
        for (int i = 0; i < galaxy.starCount; i++)
        {
            var star = stars[i];
            if (star == null) continue;
            var value = 0UL;
            foreach (var planet in star.planets)
            {
                if (planet == null) continue;
                if (!planet.scanned)
                {
                    PlanetModelingManager.RequestScanPlanet(planet);
                    continue;
                }
                var planetValue = 0UL;
                if (planet.type == EPlanetType.Gas)
                {
                    foreach (var n in planet.gasItems)
                    {
                        if (ItemToVeinBitFlagMap.TryGetValue(n, out var bitFlag))
                        {
                            planetValue |= 1UL << bitFlag;
                        }
                    }
                }
                else
                {
                    if (planet.runtimeVeinGroups != null)
                    {
                        foreach (var group in planet.runtimeVeinGroups)
                        {
                            if (group.amount > 0)
                            {
                                planetValue |= 1UL << (int)group.type;
                            }
                        }
                    }
                    if (ItemToVeinBitFlagMap.TryGetValue(planet.waterItemId, out var bitFlag))
                    {
                        planetValue |= 1UL << bitFlag;
                    }
                }
                if ((value & (1UL << (30 + planet.theme))) == 0)
                {
                    switch (planet.theme)
                    {
                        case 7:
                        case 9:
                        case 10:
                        case 13:
                        case 17:
                        case 19:
                        case 20:
                        case 24:
                            {
                                const ulong needed = 0x7EUL;
                                if ((planetValue & needed) == needed)
                                {
                                    value |= 1UL << (30 + planet.theme);
                                }
                                break;
                            }
                        case 14:
                            {
                                const ulong needed = 0x2200UL;
                                if ((planetValue & needed) == needed)
                                {
                                    value |= 1UL << (30 + planet.theme);
                                }
                                break;
                            }
                        case 15:
                        case 16:
                        case 21:
                        case 22:
                        case 23:
                        case 25:
                            value |= 1UL << (30 + planet.theme);
                            break;
                    }
                }
                value |= planetValue;
            }
            _starmapStarFilterValues[i] = value;
        }
    }

    public static int CornerComboBoxIndex
    {
        get => _cornerComboBoxIndex;
        set
        {
            _cornerComboBoxIndex = value;
            Patches.PlayerPatch.ShortcutKeysForStarsName.ForceShowAllStarsNameExternal = _cornerComboBoxIndex != 0 && !_starFilterEnabled;
            UpdateStarmapStarNames();
        }
    }

    private static void SetStarFilterEnabled(bool enabled)
    {
        if (_starFilterEnabled == enabled) return;
        _starFilterEnabled = enabled;
        if (!enabled) Patches.PlayerPatch.ShortcutKeysForStarsName.ShowAllStarsNameStatus = 0;
        Patches.PlayerPatch.ShortcutKeysForStarsName.ForceShowAllStarsNameExternal = _cornerComboBoxIndex != 0 && !_starFilterEnabled;
        UpdateStarmapStarNames();
    }

    private static void UpdateStarmapStarNames()
    {
        foreach (var starUI in UIRoot.instance.uiGame.starmap.starUIs)
        {
            var star = starUI?.star;
            if (star == null) continue;
            switch (_cornerComboBoxIndex)
            {
                case 1:
                    starUI.nameText.text = String.Format("{0:00}-{1}-{2:0.00}", star.index + 1, _starOrderNames[star.index], GetStarDist(star));
                    break;
                case 2:
                    {
                        var (nongas, total) = GetStarPlanetCount(star);
                        starUI.nameText.text = String.Format("{0:00}-{1}-{2}-{3}", star.index + 1, _starOrderNames[star.index], nongas, total);
                        break;
                    }
                case 3:
                    {
                        var (nongas, total) = GetStarPlanetCount(star);
                        starUI.nameText.text = String.Format("{0:00}-{1}-{2:0.00}-{3}-{4}", star.index + 1, _starOrderNames[star.index], GetStarDist(star), nongas, total);
                        break;
                    }
                default:
                    starUI.nameText.text = star.displayName;
                    break;
            }
        }
        return;

        double GetStarDist(StarData star)
        {
            return star.position.magnitude;
        }

        (int, int) GetStarPlanetCount(StarData star)
        {
            int total = 0;
            int nongas = 0;
            foreach (var planet in star.planets)
            {
                if (planet == null) continue;
                if (planet.type != EPlanetType.Gas) nongas++;
                total++;
            }
            return (nongas, total);
        }
    }
}
