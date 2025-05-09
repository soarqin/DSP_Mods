namespace UXAssist.Functions;

using UI;
using Common;
using CommonAPI.Systems;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;

public static class UIFunctions
{
    private static bool _initialized;
    private static PressKeyBind _toggleKey;
    private static bool _configWinInitialized;
    private static MyConfigWindow _configWin;
    private static GameObject _buttonOnPlanetGlobe;

    public static void Init()
    {
        _toggleKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.BackQuote, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "OpenUXAssistConfigWindow",
            canOverride = true
        });
        I18N.Add("KEYOpenUXAssistConfigWindow", "[UXA] Open UXAssist Config Window", "[UXA] 打开UX助手设置面板");

        I18N.Add("Enable auto-cruise", "Enable auto-cruise", "启用自动巡航");
        I18N.Add("Disable auto-cruise", "Disable auto-cruise", "禁用自动巡航");
        I18N.Add("High yield", "High yield", "高产");
        I18N.Add("Perfect", "Perfect", "完美");
        I18N.Add("Union results", "Union results", "结果取并集");
        I18N.Add("All 6 Basic Ores", "All 6 Basic Ores", "六种基础矿物齐全");
        I18N.Add("Show original name", "Show original name", "显示原始名称");
        I18N.Add("Show distance", "Show distance", "显示距离");
        I18N.Add("Show planet count", "Show planet count", "显示行星数");
        I18N.Add("Show all information", "Show all information", "显示全部信息");
        I18N.OnInitialized += RecreateConfigWindow;
    }

    public static void OnInputUpdate()
    {
        if (_toggleKey.keyValue)
        {
            ToggleConfigWindow();
        }
    }

    #region ConfigWindow
    public static void ToggleConfigWindow()
    {
        if (!_configWinInitialized)
        {
            if (!I18N.Initialized()) return;
            _configWinInitialized = true;
            _configWin = MyConfigWindow.CreateInstance();
        }

        if (_configWin.active)
        {
            _configWin._Close();
        }
        else
        {
            _configWin.Open();
        }
    }

    public static void InitMenuButtons()
    {
        if (_initialized) return;
        var uiRoot = UIRoot.instance;
        if (!uiRoot) return;
        {
            var mainMenu = uiRoot.uiMainMenu;
            var src = mainMenu.newGameButton;
            var parent = src.transform.parent;
            var btn = GameObject.Instantiate(src, parent);
            btn.name = "button-uxassist-config";
            var l = btn.text.GetComponent<Localizer>();
            if (l != null)
            {
                l.stringKey = "UXAssist Config";
                l.translation = "UXAssist Config".Translate();
            }

            btn.text.text = "UXAssist Config".Translate();
            btn.text.fontSize = btn.text.fontSize * 7 / 8;
            I18N.OnInitialized += () => { btn.text.text = "UXAssist Config".Translate(); };
            var vec = ((RectTransform)mainMenu.exitButton.transform).anchoredPosition3D;
            var vec2 = ((RectTransform)mainMenu.creditsButton.transform).anchoredPosition3D;
            var transform1 = (RectTransform)btn.transform;
            transform1.anchoredPosition3D = new Vector3(vec.x, vec.y + (vec.y - vec2.y) * 2, vec.z);
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(Functions.UIFunctions.ToggleConfigWindow);
        }
        {
            var panel = uiRoot.uiGame.planetGlobe;
            var src = panel.button2;
            var sandboxMenu = uiRoot.uiGame.sandboxMenu;
            var icon = sandboxMenu.categoryButtons[6].transform.Find("icon")?.GetComponent<Image>()?.sprite;
            var b = GameObject.Instantiate(src, src.transform.parent);
            _buttonOnPlanetGlobe = b.gameObject;
            var rect = (RectTransform)_buttonOnPlanetGlobe.transform;
            var btn = _buttonOnPlanetGlobe.GetComponent<UIButton>();
            var img = _buttonOnPlanetGlobe.transform.Find("button-2/icon")?.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = icon;
            }

            if (_buttonOnPlanetGlobe != null && btn != null)
            {
                _buttonOnPlanetGlobe.name = "open-uxassist-config";
                rect.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                rect.anchoredPosition3D = new Vector3(64f, -5f, 0f);
                b.onClick.RemoveAllListeners();
                btn.onClick += _ => { Functions.UIFunctions.ToggleConfigWindow(); };
                btn.tips.tipTitle = "UXAssist Config";
                I18N.OnInitialized += () => { btn.tips.tipTitle = "UXAssist Config".Translate(); };
                btn.tips.tipText = null;
                btn.tips.corner = 9;
                btn.tips.offset = new Vector2(-20f, -20f);
                _buttonOnPlanetGlobe.SetActive(true);
            }
        }
        InitToggleAutoCruiseCheckButton();
        InitStarmapButtons();
        _initialized = true;
    }

    public static void RecreateConfigWindow()
    {
        if (!_configWinInitialized) return;
        var wasActive = _configWin.active;
        if (wasActive) _configWin._Close();
        MyConfigWindow.DestroyInstance(_configWin);
        _configWinInitialized = false;
        if (wasActive) ToggleConfigWindow();
    }

    public static void UpdateGlobeButtonPosition(UIPlanetGlobe planetGlobe)
    {
        if (_buttonOnPlanetGlobe == null) return;
        var rect = (RectTransform)_buttonOnPlanetGlobe.transform;
        if (planetGlobe.dysonSphereSystemUnlocked || planetGlobe.logisticsSystemUnlocked)
        {
            rect.anchoredPosition3D = new Vector3(64f, -5f, 0f);
        }
        else
        {
            rect.anchoredPosition3D = new Vector3(128f, -100f, 0f);
        }
    }
    #endregion

    #region ToggleAutoCruiseCheckButton
    public static UI.MyCheckButton ToggleAutoCruise;

    public static void InitToggleAutoCruiseCheckButton()
    {
        var lowGroup = GameObject.Find("UI Root/Overlay Canvas/In Game/Low Group");
        ToggleAutoCruise = MyCheckButton.CreateCheckButton(0, 0, lowGroup.GetComponent<RectTransform>(), Patches.PlayerPatch.AutoCruiseEnabled).WithSize(120f, 40f);
        UpdateToggleAutoCruiseCheckButtonVisiblility();
        ToggleAutoCruiseChecked();
        ToggleAutoCruise.OnChecked += ToggleAutoCruiseChecked;
        var rectTrans = ToggleAutoCruise.rectTrans;
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 185f, 0f);
        static void ToggleAutoCruiseChecked()
        {
            if (ToggleAutoCruise.Checked)
            {
                ToggleAutoCruise.SetLabelText("Disable auto-cruise");
            }
            else
            {
                ToggleAutoCruise.SetLabelText("Enable auto-cruise");
            }
        }
    }

    public static void UpdateToggleAutoCruiseCheckButtonVisiblility()
    {
        if (ToggleAutoCruise == null) return;
        var active = Patches.PlayerPatch.AutoNavigationEnabled.Value && Patches.PlayerPatch.AutoNavigation.IndicatorAstroId > 0;
        ToggleAutoCruise.gameObject.SetActive(active);
    }
    #endregion

    #region StarMapButtons
    private static int _cornerComboBoxIndex;
    private static string[] _starOrderNames;
    private static bool _starmapFilterInitialized;
    private static ulong[] _starmapStarFilterValues;
    private static bool _starFilterEnabled;
    public static UI.MyCheckButton StarmapFilterToggler;
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

        var cornerComboBox = UI.MyCornerComboBox.CreateComboBox(135, 0, rtrans, true).WithItems("Show original name".Translate(), "Show distance".Translate(), "Show planet count".Translate(), "Show all information".Translate());
        cornerComboBox.SetIndex(Functions.UIFunctions.CornerComboBoxIndex);
        cornerComboBox.OnSelChanged += (index) =>
        {
            Functions.UIFunctions.CornerComboBoxIndex = index;
        };
        StarmapFilterToggler = UI.MyCheckButton.CreateCheckButton(0, 0, rtrans, false, ">>").WithSize(24, 24);
        MyCheckButton[] buttons = [
            UI.MyCheckButton.CreateCheckButton(24, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Kimberlite
            UI.MyCheckButton.CreateCheckButton(48, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Fractal Silicon
            UI.MyCheckButton.CreateCheckButton(72, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Organic Crystal
            UI.MyCheckButton.CreateCheckButton(96, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Grating Crystal
            UI.MyCheckButton.CreateCheckButton(120, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Stalagmite Crystal
            UI.MyCheckButton.CreateCheckButton(144, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Unipolar Magnet
            UI.MyCheckButton.CreateCheckButton(168, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Crude Oil
            UI.MyCheckButton.CreateCheckButton(192, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Fire Ice
            UI.MyCheckButton.CreateCheckButton(216, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Sulfuric Acid
            UI.MyCheckButton.CreateCheckButton(240, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Water
            UI.MyCheckButton.CreateCheckButton(264, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Hydrogen
            UI.MyCheckButton.CreateCheckButton(288, 0, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Deuterium

            UI.MyCheckButton.CreateCheckButton(24, 24, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 48, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 72, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 96, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 120, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 144, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 168, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),

            UI.MyCheckButton.CreateCheckButton(24, 210, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 234, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 258, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 282, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 306, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 330, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 354, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            UI.MyCheckButton.CreateCheckButton(24, 378, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
        ];
        var unionCheckBox = UI.MyCheckBox.CreateCheckBox(312, 0, rtrans, false, "Union results".Translate(), 15).WithSmallerBox(24f);
        unionCheckBox.gameObject.SetActive(false);
        unionCheckBox.OnChecked += () =>
        {
            UpdateStarmapStarFilters();
        };
        var allOresText = MyWindow.AddText(20, 190, rtrans, "All 6 Basic Ores".Translate(), 12);
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
        GameLogic.OnDataLoaded += () =>
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

        GameLogic.OnGameBegin += () =>
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
        GameLogic.OnGameEnd += () =>
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
                items[0] = "Show original name".Translate();
                items[1] = "Show distance".Translate();
                items[2] = "Show planet count".Translate();
                items[3] = "Show all information".Translate();
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
                            buttons[12 + i].SetLabelText($"{themeProto.DisplayName.Translate()} ({"High yield".Translate()})");
                            break;
                        case 6:
                            buttons[12 + i].SetLabelText($"{themeProto.DisplayName.Translate()} ({"Perfect".Translate()})");
                            break;
                        default:
                            buttons[12 + i].SetLabelText(themeProto.DisplayName.Translate());
                            break;
                    }
                }
            }
            if (allOresText != null) allOresText.text = "All 6 Basic Ores".Translate();
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
                    starUI.nameText.text = String.Format("{0}-{1:0.00}", _starOrderNames[star.index], GetStarDist(star));
                    break;
                case 2:
                    {
                        var (nongas, total) = GetStarPlanetCount(star);
                        starUI.nameText.text = String.Format("{0}-{1}-{2}", _starOrderNames[star.index], nongas, total);
                        break;
                    }
                case 3:
                    {
                        var (nongas, total) = GetStarPlanetCount(star);
                        starUI.nameText.text = String.Format("{0}-{1:0.00}-{2}-{3}", _starOrderNames[star.index], GetStarDist(star), nongas, total);
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
    #endregion
}
