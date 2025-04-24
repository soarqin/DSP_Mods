namespace UXAssist.Functions;

using UI;
using Common;
using CommonAPI.Systems;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading;

public static class UIFunctions
{
    private static bool _initialized;
    private static PressKeyBind _toggleKey;
    private static bool _configWinInitialized;
    private static MyConfigWindow _configWin;
    private static GameObject _buttonOnPlanetGlobe;
    private static int _cornerComboBoxIndex;
    private static string[] _starOrderNames;
    private static bool _starmapFilterInitialized;
    private static ulong[] _starmapStarFilterValues;
    private static bool _starFilterEnabled;
    private static UI.MyCheckButton _starmapFilterToggler;
    public static bool[] ShowStarName;

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
        I18N.Add("High yield", "High yield", "高产");
        I18N.Add("Perfect", "Perfect", "完美");
        I18N.Add("All 6 Basic Ores", "All 6 Basic Ores", "六种基础矿物齐全");
        I18N.Add("Show original name", "Show original name", "显示原始名称");
        I18N.Add("Show distance", "Show distance", "显示距离");
        I18N.Add("Show planet count", "Show planet count", "显示行星数");
        I18N.Add("Show all information", "Show all information", "显示全部信息");
        I18N.OnInitialized += RecreateConfigWindow;
    }

    public static void OnUpdate()
    {
        if (!_starmapFilterInitialized || _starmapFilterToggler == null || _starmapFilterToggler.gameObject.activeSelf) return;
        if (PlanetModelingManager.scnPlanetReqList.Count == 0)
        {
            StarmapUpdateFilterValues();
            _starmapFilterToggler.gameObject.SetActive(true);
        }
    }

    public static void OnInputUpdate()
    {
        if (_toggleKey.keyValue)
        {
            ToggleConfigWindow();
        }
    }

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
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/18.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/19.png"),
        null,
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/21.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/22.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/23.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/24.png"),
        Common.Util.LoadEmbeddedSprite("assets/planet_icon/25.png")
    ];
    private static readonly int[] FilterPlanetThemes = [16, 23, 10, 15, 18, 22, 25, 21, 14, 17, 19, 7, 24, 9, 13];
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
        {
            var rtrans = uiRoot.uiGame.starmap.transform as RectTransform;
            var cornerComboBox = UI.MyCornerComboBox.CreateComboBox(135, 5, rtrans, true).WithItems("Show original name".Translate(), "Show distance".Translate(), "Show planet count".Translate(), "Show all information".Translate());
            cornerComboBox.SetIndex(Functions.UIFunctions.CornerComboBoxIndex);
            cornerComboBox.OnSelChanged += (index) =>
            {
                Functions.UIFunctions.CornerComboBoxIndex = index;
            };
            _starmapFilterToggler = UI.MyCheckButton.CreateCheckButton(5, 5, rtrans, false, ">>").WithSize(24, 24);
            MyCheckButton[] buttons = [
                UI.MyCheckButton.CreateCheckButton(29, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Kimberlite
                UI.MyCheckButton.CreateCheckButton(53, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Fractal Silicon
                UI.MyCheckButton.CreateCheckButton(77, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Organic Crystal
                UI.MyCheckButton.CreateCheckButton(101, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Grating Crystal
                UI.MyCheckButton.CreateCheckButton(125, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Stalagmite Crystal
                UI.MyCheckButton.CreateCheckButton(149, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Unipolar Magnet
                UI.MyCheckButton.CreateCheckButton(173, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Crude Oil
                UI.MyCheckButton.CreateCheckButton(197, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Fire Ice
                UI.MyCheckButton.CreateCheckButton(221, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Sulfuric Acid
                UI.MyCheckButton.CreateCheckButton(245, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Water
                UI.MyCheckButton.CreateCheckButton(269, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Hydrogen
                UI.MyCheckButton.CreateCheckButton(293, 5, rtrans, false).WithIcon().WithSize(24, 24).WithIconWidth(24), // Deuterium

                UI.MyCheckButton.CreateCheckButton(29, 29, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 53, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 77, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 101, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 125, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 149, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 173, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 197, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 221, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),

                UI.MyCheckButton.CreateCheckButton(29, 263, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 287, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 311, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 335, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 359, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
                UI.MyCheckButton.CreateCheckButton(29, 383, rtrans, false).WithIcon().WithSize(150, 24).WithIconWidth(24),
            ];
            var allOresText = MyWindow.AddText(25, 243, rtrans, "All 6 Basic Ores".Translate(), 12);
            allOresText.gameObject.SetActive(false);
            _starmapFilterToggler.OnChecked += UpdateButtons;
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
                for (int i = 0; i < 6; i++)
                {
                    veinProto = LDB.veins.Select(i + 9);
                    buttons[i].SetIcon(veinProto.iconSprite);
                }
                var itemProto = LDB.items.Select(1007);
                buttons[6].SetIcon(itemProto.iconSprite);
                veinProto = LDB.veins.Select(8);
                buttons[7].SetIcon(veinProto.iconSprite);
                itemProto = LDB.items.Select(1116);
                buttons[8].SetIcon(itemProto.iconSprite);
                itemProto = LDB.items.Select(1000);
                buttons[9].SetIcon(itemProto.iconSprite);
                itemProto = LDB.items.Select(1120);
                buttons[10].SetIcon(itemProto.iconSprite);
                itemProto = LDB.items.Select(1121);
                buttons[11].SetIcon(itemProto.iconSprite);

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
                _starmapFilterToggler.gameObject.SetActive(false);
                _starmapFilterToggler.Checked = false;
                UpdateButtons();
                SetStarFilterEnabled(false);
                foreach (var star in galaxy.stars)
                {
                    if (star != null) PlanetModelingManager.RequestScanStar(star);
                }
                _starmapFilterInitialized = true;
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
                    for (int i = 0; i < 6; i++)
                    {
                        veinProto = LDB.veins.Select(i + 9);
                        buttons[i].WithTip(veinProto.Name);
                    }
                    var itemProto = LDB.items.Select(1007);
                    buttons[6].WithTip(itemProto.Name);
                    veinProto = LDB.veins.Select(8);
                    buttons[7].WithTip(veinProto.Name);
                    itemProto = LDB.items.Select(1116);
                    buttons[8].WithTip(itemProto.Name);
                    itemProto = LDB.items.Select(1000);
                    buttons[9].WithTip(itemProto.Name);
                    itemProto = LDB.items.Select(1120);
                    buttons[10].WithTip(itemProto.Name);
                    itemProto = LDB.items.Select(1121);
                    buttons[11].WithTip(itemProto.Name);

                    for (int i = 0; i < FilterPlanetThemes.Length; i++)
                    {
                        var theme = FilterPlanetThemes[i];
                        var themeProto = LDB.themes.Select(theme);
                        switch (i)
                        {
                            case 7:
                                buttons[12 + i].SetLabelText($"{themeProto.DisplayName.Translate()} ({"High yield".Translate()})");
                                break;
                            case 8:
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
                var chk = _starmapFilterToggler.Checked;
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
                allOresText.gameObject.SetActive(chk);
                _starmapFilterToggler.SetLabelText(chk ? "X" : ">>");
                if (!chk)
                {
                    UpdateStarmapStarFilters();
                }
            }
            void UpdateStarmapStarFilters()
            {
                var filterValue = 0UL;
                if (_starmapFilterToggler.Checked)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (buttons[i].Checked)
                        {
                            filterValue |= 1UL << (i + 9);
                        }
                    }
                    if (buttons[6].Checked)
                    {
                        filterValue |= 1UL << 7;
                    }
                    if (buttons[7].Checked)
                    {
                        filterValue |= 1UL << 8;
                    }
                    if (buttons[8].Checked)
                    {
                        filterValue |= 1UL << 22;
                    }
                    if (buttons[9].Checked)
                    {
                        filterValue |= 1UL << 23;
                    }
                    if (buttons[10].Checked)
                    {
                        filterValue |= 1UL << 20;
                    }
                    if (buttons[11].Checked)
                    {
                        filterValue |= 1UL << 21;
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
                    ShowStarName[i] = (_starmapStarFilterValues[i] & filterValue) == filterValue;
                }
                SetStarFilterEnabled(true);
            }
        }
        _initialized = true;
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
                while (planet.scanning)
                {
                    Thread.Sleep(50);
                }
                var planetValue = 0UL;
                if (planet.type == EPlanetType.Gas)
                {
                    foreach (var n in planet.gasItems)
                    {
                        switch (n)
                        {
                            case 1011:
                                planetValue |= 1UL << 8;
                                break;
                            case 1120:
                                planetValue |= 1UL << 20;
                                break;
                            case 1121:
                                planetValue |= 1UL << 21;
                                break;
                        }
                    }
                }
                else
                {
                    foreach (var group in planet.veinGroups)
                    {
                        if (group.amount > 0)
                        {
                            planetValue |= 1UL << (int)group.type;
                        }
                    }
                    switch (planet.waterItemId)
                    {
                        case 1116:
                            planetValue |= 1UL << 22;
                            break;
                        case 1000:
                            planetValue |= 1UL << 23;
                            break;
                    }
                }
                if ((value & (1UL << (30 + planet.theme))) == 0)
                {
                    switch (planet.theme)
                    {
                        case 7:
                        case 9:
                        case 13:
                        case 17:
                        case 19:
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
                        case 10:
                        case 15:
                        case 16:
                        case 18:
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

    public static int CornerComboBoxIndex
    {
        get => _cornerComboBoxIndex;
        set
        {
            _cornerComboBoxIndex = value;
            Patches.PlayerPatch.ShortcutKeysForStarsName.SetForceShowAllStarsNameExternal(_cornerComboBoxIndex != 0 && !_starFilterEnabled);
            UpdateStarmapStarNames();
        }
    }

    private static void SetStarFilterEnabled(bool enabled)
    {
        if (_starFilterEnabled == enabled) return;
        _starFilterEnabled = enabled;
        if (!enabled) Patches.PlayerPatch.ShortcutKeysForStarsName.SetShowAllStarsNameStatus(0);
        Patches.PlayerPatch.ShortcutKeysForStarsName.SetForceShowAllStarsNameExternal(_cornerComboBoxIndex != 0 && !_starFilterEnabled);
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
}
