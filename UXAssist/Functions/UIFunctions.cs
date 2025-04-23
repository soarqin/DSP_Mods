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
    private static int _cornerComboBoxIndex;
    private static string[] _starOrderNames;
    private static bool _starmapFilterInitialized;
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
        I18N.OnInitialized += RecreateConfigWindow;
        GameLogic.OnGameBegin += () =>
        {
            var galaxy = GameMain.data.galaxy;
            ShowStarName = new bool[galaxy.starCount];
            _starOrderNames = new string[galaxy.starCount];
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
        };
        GameLogic.OnGameEnd += () =>
        {
            _starOrderNames = null;
            ShowStarName = null;
        };
    }

    public static void OnUpdate()
    {
        if (!_starmapFilterInitialized || _starmapFilterToggler == null || _starmapFilterToggler.gameObject.activeSelf) return;
        if (PlanetModelingManager.scnPlanetReqList.Count == 0) _starmapFilterToggler.gameObject.SetActive(true);
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
            var cb = UI.MyCornerComboBox.CreateComboBox(125, 0, rtrans, true).WithItems("显示原始名称", "显示距离", "显示行星数", "显示主要矿物", "显示全部信息");
            cb.SetIndex(Functions.UIFunctions.CornerComboBoxIndex);
            cb.OnSelChanged += (index) =>
            {
                Functions.UIFunctions.CornerComboBoxIndex = index;
            };
            _starmapFilterToggler = UI.MyCheckButton.CreateCheckButton(20, 0, rtrans, false, ">>").WithSize(20, 20);
            MyCheckButton[] buttons = [
                UI.MyCheckButton.CreateCheckButton(40, 0, rtrans, false).WithIcon().WithSize(20, 20), // Kimberlite
                UI.MyCheckButton.CreateCheckButton(60, 0, rtrans, false).WithIcon().WithSize(20, 20), // Fractal Silicon
                UI.MyCheckButton.CreateCheckButton(80, 0, rtrans, false).WithIcon().WithSize(20, 20), // Organic Crystal
                UI.MyCheckButton.CreateCheckButton(100, 0, rtrans, false).WithIcon().WithSize(20, 20), // Grating Crystal
                UI.MyCheckButton.CreateCheckButton(120, 0, rtrans, false).WithIcon().WithSize(20, 20), // Stalagmite Crystal
                UI.MyCheckButton.CreateCheckButton(140, 0, rtrans, false).WithIcon().WithSize(20, 20), // Unipolar Magnet
                UI.MyCheckButton.CreateCheckButton(160, 0, rtrans, false).WithIcon().WithSize(20, 20), // Crude Oil
                UI.MyCheckButton.CreateCheckButton(180, 0, rtrans, false).WithIcon().WithSize(20, 20), // Fire Ice
                UI.MyCheckButton.CreateCheckButton(200, 0, rtrans, false).WithIcon().WithSize(20, 20), // Sulfuric Acid
                UI.MyCheckButton.CreateCheckButton(220, 0, rtrans, false).WithIcon().WithSize(20, 20), // Water
                UI.MyCheckButton.CreateCheckButton(240, 0, rtrans, false).WithIcon().WithSize(20, 20), // Hydrogen
                UI.MyCheckButton.CreateCheckButton(260, 0, rtrans, false).WithIcon().WithSize(20, 20), // Deuterium
            ];
            _starmapFilterToggler.OnChecked += UpdateButtons;
            foreach (var button in buttons)
            {
                button.OnChecked += UpdateStarmapStarFilters;
            }

            GameLogic.OnDataLoaded += () =>
            {
                VeinProto veinProto;
                for (int i = 0; i < 6; i++)
                {
                    veinProto = LDB.veins.Select(i + 9);
                    buttons[i].WithTip(veinProto.Name).SetIcon(veinProto.iconSprite);
                }
                var itemProto = LDB.items.Select(1007);
                buttons[6].WithTip(itemProto.Name).SetIcon(itemProto.iconSprite);
                veinProto = LDB.veins.Select(8);
                buttons[7].WithTip(veinProto.Name).SetIcon(veinProto.iconSprite);
                itemProto = LDB.items.Select(1116);
                buttons[8].WithTip(itemProto.Name).SetIcon(itemProto.iconSprite);
                itemProto = LDB.items.Select(1000);
                buttons[9].WithTip(itemProto.Name).SetIcon(itemProto.iconSprite);
                itemProto = LDB.items.Select(1120);
                buttons[10].WithTip(itemProto.Name).SetIcon(itemProto.iconSprite);
                itemProto = LDB.items.Select(1121);
                buttons[11].WithTip(itemProto.Name).SetIcon(itemProto.iconSprite);
            };
            GameLogic.OnGameBegin += () =>
            {
                if (DSPGame.IsMenuDemo) return;
                _starmapFilterToggler.gameObject.SetActive(false);
                _starmapFilterToggler.Checked = false;
                UpdateButtons();
                SetStarFilterEnabled(false);
                foreach (var star in GameMain.data.galaxy.stars)
                {
                    if (star != null) PlanetModelingManager.RequestScanStar(star);
                }
                _starmapFilterInitialized = true;
            };
            GameLogic.OnGameEnd += () =>
            {
                _starmapFilterInitialized = false;
            };
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
                _starmapFilterToggler.SetLabelText(chk ? "X" : ">>");
                if (!chk)
                {
                    UpdateStarmapStarFilters();
                }
            }
            void UpdateStarmapStarFilters()
            {
                List<(int, int)> filters = [];
                bool showAny = false;
                if (_starmapFilterToggler.Checked)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (buttons[i].Checked)
                        {
                            filters.Add((i + 9, 0));
                            showAny = true;
                        }
                    }
                    if (buttons[6].Checked)
                    {
                        filters.Add((7, 0));
                        showAny = true;
                    }
                    if (buttons[7].Checked)
                    {
                        filters.Add((8, 1011));
                        showAny = true;
                    }
                    if (buttons[8].Checked)
                    {
                        filters.Add((0, 1116));
                        showAny = true;
                    }
                    if (buttons[9].Checked)
                    {
                        filters.Add((0, 1000));
                        showAny = true;
                    }
                    if (buttons[10].Checked)
                    {
                        filters.Add((0, 1120));
                        showAny = true;
                    }
                    if (buttons[11].Checked)
                    {
                        filters.Add((0, 1121));
                        showAny = true;
                    }
                }
                if (!showAny)
                {
                    for (int i = 0; i < ShowStarName.Length; i++)
                    {
                        ShowStarName[i] = false;
                    }
                    SetStarFilterEnabled(false);
                    return;
                }
                var galaxy = GameMain.data.galaxy;
                var stars = galaxy.stars;
                for (int i = 0; i < galaxy.starCount; i++)
                {
                    var star = stars[i];
                    if (star == null) continue;
                    ShowStarName[i] = false;
                    var allMatch = true;
                    foreach (var filter in filters)
                    {
                        var match = false;
                        foreach (var planet in star.planets)
                        {
                            if (planet == null) continue;
                            if (planet.type == EPlanetType.Gas)
                            {
                                if (filter.Item2 != 0)
                                {
                                    foreach (var n in planet.gasItems)
                                    {
                                        if (n == filter.Item2)
                                        {
                                            match = true;
                                            break;
                                        }
                                    }
                                    if (match) break;
                                }
                            }
                            else
                            {
                                if (filter.Item2 != 0)
                                {
                                    if (planet.waterItemId == filter.Item2)
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                                if (filter.Item1 != 0)
                                {
                                    foreach (var group in planet.veinGroups)
                                    {
                                        if (group.amount > 0 && (int)group.type == filter.Item1)
                                        {
                                            match = true;
                                            break;
                                        }
                                    }
                                    if (match) break;
                                }
                            }
                        }
                        if (!match)
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch)
                    {
                        ShowStarName[i] = true;
                    }
                }
                SetStarFilterEnabled(true);
            }
        }
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
                    starUI.nameText.text = String.Format("{0}-{1}", _starOrderNames[star.index], GetStarSpecialOres(star));
                    break;
                case 4:
                    {
                        var (nongas, total) = GetStarPlanetCount(star);
                        starUI.nameText.text = String.Format("{0}-{1:0.00}-{2}-{3}-{4}", _starOrderNames[star.index], GetStarDist(star), GetStarSpecialOres(star), nongas, total);
                        break;
                    }
                default:
                    starUI.nameText.text = star.displayName;
                    break;
            }
            ;
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

        string GetStarSpecialOres(StarData star)
        {
            return "";
        }
    }
}
