using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CommonAPI.Systems;
using UXAssist.Common;
using UXAssist.UI;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Functions;

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
        I18N.Add("Enable auto-construct", "Enable auto-construct", "启用自动建造");
        I18N.Add("Disable auto-construct", "Disable auto-construct", "禁用自动建造");
        I18N.Add("Buildings to construct: {0}", "Buildings to construct: {0}", "待建造数量: {0}");
        I18N.Add("High yield", "High yield", "高产");
        I18N.Add("Perfect", "Perfect", "完美");
        I18N.Add("Union results", "Union results", "结果取并集");
        I18N.Add("All 6 Basic Ores", "All 6 Basic Ores", "六种基础矿物齐全");
        I18N.Add("Show original name", "Show original name", "显示原始名称");
        I18N.Add("Show distance", "Show distance", "显示距离");
        I18N.Add("Show planet count", "Show planet count", "显示行星数");
        I18N.Add("Show all information", "Show all information", "显示全部信息");
        I18N.Add("No recent milkyway upload results", "No recent milkyway upload results", "没有最近的银河系发电数据上传结果");
        I18N.Add("Success", "Success", "成功");
        I18N.Add("Failure: ", "Failure: ", "失败: ");
        I18N.Add("Show top players", "Show top players", "显示玩家排行榜");
        I18N.Add("Hide top players", "Hide top players", "隐藏玩家排行榜");
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
        InitToggleAutoConstructCheckButton();
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
        var rectTrans = ToggleAutoCruise.rectTrans;
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 185f, 0f);

        UpdateToggleAutoCruiseCheckButtonVisiblility();
        ToggleAutoCruiseChecked();
        ToggleAutoCruise.OnChecked += ToggleAutoCruiseChecked;
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

    #region ToggleAutoConstructCheckButton
    public static UI.MyCheckButton ToggleAutoConstruct;
    public static Text ConstructCountText;

    public static void InitToggleAutoConstructCheckButton()
    {
        var lowGroup = GameObject.Find("UI Root/Overlay Canvas/In Game/Low Group");
        var parent = lowGroup.GetComponent<RectTransform>();
        ToggleAutoConstruct = MyCheckButton.CreateCheckButton(0, 0, parent, Patches.FactoryPatch.AutoConstructEnabled).WithSize(120f, 40f);
        var rectTrans = ToggleAutoConstruct.rectTrans;
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 140f, 0f);

        ConstructCountText = GameObject.Instantiate(UIRoot.instance.uiGame.assemblerWindow.stateText);
        ConstructCountText.gameObject.name = "construct-count-text";
        ConstructCountText.text = String.Format("Buildings to construct: {0}".Translate(), 0);
        ConstructCountText.color = new Color(1f, 1f, 1f, 0.4f);
        ConstructCountText.alignment = TextAnchor.MiddleLeft;
        ConstructCountText.fontSize = 14;
        rectTrans = ConstructCountText.rectTransform;
        rectTrans.SetParent(parent);
        rectTrans.sizeDelta = new Vector2(120, 20);
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 110f, 0f);
        rectTrans.localScale = new Vector3(1f, 1f, 1f);

        UpdateToggleAutoConstructCheckButtonVisiblility();
        ToggleAutoConstructChecked();
        ToggleAutoConstruct.OnChecked += ToggleAutoConstructChecked;
        static void ToggleAutoConstructChecked()
        {
            if (ToggleAutoConstruct.Checked)
            {
                ToggleAutoConstruct.SetLabelText("Disable auto-construct");
            }
            else
            {
                ToggleAutoConstruct.SetLabelText("Enable auto-construct");
            }
        }
    }

    public static void UpdateToggleAutoConstructCheckButtonVisiblility()
    {
        if (ToggleAutoConstruct == null) return;
        var localPlanet = GameMain.localPlanet;
        var active = localPlanet != null && localPlanet.factoryLoaded && localPlanet.factory.prebuildCount > 0;
        ToggleAutoConstruct.gameObject.SetActive(active);
        ConstructCountText.gameObject.SetActive(active);
    }

    public static void UpdateConstructCountText(int count)
    {
        if (ConstructCountText == null) return;
        ConstructCountText.text = String.Format("Buildings to construct: {0}".Translate(), count);
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
    #endregion

    #region MilkyWayClusterUploadResult

    const int ClusterUploadResultKeepCount = 100;
    private static readonly ClusterUploadResult[] _clusterUploadResults = new ClusterUploadResult[ClusterUploadResultKeepCount];
    private static readonly object _clusterUploadResultsLock = new();
    private static int _clusterUploadResultsHead = 0;
    private static int _clusterUploadResultsCount = 0;

    private struct ClusterUploadResult
    {
        public DateTime UploadTime;
        public int Result;
        public float RequestTime;
    }

    public static void AddClusterUploadResult(int result, float requestTime)
    {
        lock (_clusterUploadResultsLock)
        {
            if (_clusterUploadResultsCount >= ClusterUploadResultKeepCount)
            {
                _clusterUploadResults[_clusterUploadResultsHead] = new ClusterUploadResult { UploadTime = DateTime.Now, Result = result, RequestTime = requestTime };
                _clusterUploadResultsHead = (_clusterUploadResultsHead + 1) % ClusterUploadResultKeepCount;
            }
            else
            {
                _clusterUploadResults[(_clusterUploadResultsHead + _clusterUploadResultsCount) % ClusterUploadResultKeepCount] = new ClusterUploadResult { UploadTime = DateTime.Now, Result = result, RequestTime = requestTime };
                _clusterUploadResultsCount++;
            }
        }
    }

    public static void ExportClusterUploadResults(BinaryWriter w)
    {
        lock (_clusterUploadResultsLock)
        {
            w.Write(_clusterUploadResultsCount);
            w.Write(_clusterUploadResultsHead);
            for (var i = 0; i < _clusterUploadResultsCount; i++)
            {
                ref var result = ref _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                w.Write(result.UploadTime.ToBinary());
                w.Write(result.Result);
                w.Write(result.RequestTime);
            }
        }
    }

    public static void ImportClusterUploadResults(BinaryReader r)
    {
        lock (_clusterUploadResultsLock)
        {
            _clusterUploadResultsCount = r.ReadInt32();
            _clusterUploadResultsHead = r.ReadInt32();
            for (var i = 0; i < _clusterUploadResultsCount; i++)
            {
                ref var result = ref _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                result.UploadTime = DateTime.FromBinary(r.ReadInt64());
                result.Result = r.ReadInt32();
                result.RequestTime = r.ReadSingle();
            }
        }
    }

    public static void ClearClusterUploadResults()
    {
        lock (_clusterUploadResultsLock)
        {
            _clusterUploadResultsCount = 0;
            _clusterUploadResultsHead = 0;
        }
    }

    public static void ShowRecentMilkywayUploadResults()
    {
        lock (_clusterUploadResultsLock)
        {
            if (_clusterUploadResultsCount == 0)
            {
                UIMessageBox.Show("UXAssist".Translate(), "No recent milkyway upload results".Translate(), "确定".Translate(), UIMessageBox.INFO, null);
                return;
            }
            StringBuilder sb = new();
            for (var i = Math.Min(_clusterUploadResultsCount, 10) - 1; i >= 0; i--)
            {
                var res = _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                sb.AppendLine($"{res.UploadTime.ToString("yyyy-MM-dd HH:mm:ss")} - {((res.Result is 0 or 20) ? "Success".Translate() : ("Failure: ".Translate() + res.Result.ToString()))} - {res.RequestTime:F2}s");
            }
            UIMessageBox.Show("UXAssist".Translate(), sb.ToString(), "确定".Translate(), UIMessageBox.INFO, null);
        }
    }
    #endregion

    #region MilkyWayTopTenPlayers
    private static ClusterPlayerData[] _topTenPlayerData = null;
    private static readonly StringBuilder _sb = new("         ", 12);

    public static UI.MyCheckButton MilkyWayTopTenPlayersToggler;
    public static event Action OnMilkyWayTopTenPlayersUpdated;

    public static void SetTopPlayerCount(int count)
    {
        _topTenPlayerData = new ClusterPlayerData[count];
    }

    public static void SetTopPlayerData(int index, ref ClusterPlayerData playerData)
    {
        if (index < 0 || index >= _topTenPlayerData.Length) return;
        _topTenPlayerData[index] = playerData;
    }

    public static void UpdateMilkyWayTopTenPlayers()
    {
        if (_topTenPlayerData == null) return;
        OnMilkyWayTopTenPlayersUpdated?.Invoke();
    }

    public static void InitMilkyWayTopTenPlayers()
    {
        var uiRoot = UIRoot.instance;
        if (!uiRoot) return;

        var rect = uiRoot.uiMilkyWay.transform as RectTransform;
        var panel = new GameObject("uxassist-milkyway-top-ten-players-panel");
        var rtrans = panel.AddComponent<RectTransform>();
        rtrans.SetParent(rect);
        rtrans.sizeDelta = new Vector2(0f, 0f);
        rtrans.localScale = new Vector3(1f, 1f, 1f);
        rtrans.anchorMax = new Vector2(1f, 1f);
        rtrans.anchorMin = new Vector2(0f, 0f);
        rtrans.pivot = new Vector2(0f, 1f);
        rtrans.anchoredPosition3D = new Vector3(0, 0, 0f);

        MyFlatButton[] buttons = [];
        Text[] textFields = [];

        MilkyWayTopTenPlayersToggler = UI.MyCheckButton.CreateCheckButton(0, 0, rtrans, false, "Show top players".Translate()).WithSize(120f, 24f);
        MilkyWayTopTenPlayersToggler.OnChecked += UpdateButtons;
        MilkyWayTopTenPlayersToggler.Checked = false;
        UpdateButtons();
        OnMilkyWayTopTenPlayersUpdated += UpdateButtons;

        Text CreateTextField(RectTransform parent)
        {
            var txt = UnityEngine.Object.Instantiate(UIRoot.instance.uiGame.assemblerWindow.stateText, parent);
            txt.gameObject.name = "uxassist-milkyway-top-ten-players-text-field";
            txt.text = "";
            txt.color = new Color(1f, 1f, 1f, 0.4f);
            txt.alignment = TextAnchor.MiddleLeft;
            txt.fontSize = 15;
            txt.rectTransform.sizeDelta = new Vector2(0, 18);
            return txt;
        }

        void UpdateButtons()
        {
            var chk = MilkyWayTopTenPlayersToggler.Checked;
            if (_topTenPlayerData == null)
            {
                MilkyWayTopTenPlayersToggler.gameObject.SetActive(false);
                return;
            }
            var count = _topTenPlayerData.Length;
            MilkyWayTopTenPlayersToggler.gameObject.SetActive(count > 0);
            if (count != buttons.Length)
            {
                for (var i = count; i < buttons.Length; i++)
                {
                    UnityEngine.Object.Destroy(buttons[i].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4 + 1].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4 + 2].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4 + 3].gameObject);
                }
                Array.Resize(ref buttons, count);
                Array.Resize(ref textFields, count * 4);
            }
            float maxWidth0 = 0f;
            float maxWidth1 = 0f;
            float maxWidth2 = 0f;
            float maxWidth3 = 0f;
            for (var i = 0; i < count; i++)
            {
                var button = buttons[i];
                if (chk)
                {
                    if (button == null)
                    {
                        button = MyFlatButton.CreateFlatButton(0f, 20f * i + 24f, rtrans, "").WithSize(20f, 18f);
                        buttons[i] = button;
                        button.uiButton.data = i;
                        button.uiButton.onClick += data =>
                        {
                            if (data < 0 || data >= _topTenPlayerData.Length) return;
                            ref var playerData = ref _topTenPlayerData[data];
                            var seed = playerData.seedKey;
                            int combatValue = (int)(seed % 1000L);
                            int resourceMultiplier = (int)(seed / 1000L % 100L);
                            int starCount = (int)(seed / 100000L % 1000L);
                            int gameSeed = (int)(seed / 100000000L);
                            var uiMilkyWaySearchPanel = UIRoot.instance.uiMilkyWay.uiSearchPanel;
                            uiMilkyWaySearchPanel.selectSeed = gameSeed;
                            uiMilkyWaySearchPanel.selectStarCnt = starCount;
                            uiMilkyWaySearchPanel.selectResMulti = resourceMultiplier;
                            if (combatValue / 100 > 0)
                            {
                                uiMilkyWaySearchPanel.selectMode = 1;
                                uiMilkyWaySearchPanel.selectCombatDiff = combatValue % 100;
                            }
                            else
                            {
                                uiMilkyWaySearchPanel.selectMode = 0;
                                uiMilkyWaySearchPanel.selectCombatDiff = 0;
                            }
                            uiMilkyWaySearchPanel.RefreshInputText();
                            uiMilkyWaySearchPanel.OnSearchButtonClick(0);
                        };
                        textFields[i * 4] = CreateTextField(rtrans);
                        textFields[i * 4].alignment = TextAnchor.MiddleRight;
                        textFields[i * 4 + 1] = CreateTextField(rtrans);
                        textFields[i * 4 + 2] = CreateTextField(rtrans);
                        textFields[i * 4 + 3] = CreateTextField(rtrans);
                        textFields[i * 4 + 3].alignment = TextAnchor.MiddleRight;
                    }
                    button.SetLabelText(">>");
                    textFields[i * 4].text = (i + 1).ToString();
                    textFields[i * 4 + 1].text = _topTenPlayerData[i].name;
                    textFields[i * 4 + 2].text = SeedToString(_topTenPlayerData[i].seedKey);
                    textFields[i * 4 + 3].text = String.Format("{0}W", ToKMG(_topTenPlayerData[i].genCap * 60L));
                    maxWidth0 = Math.Max(maxWidth0, textFields[i * 4].preferredWidth);
                    maxWidth1 = Math.Max(maxWidth1, textFields[i * 4 + 1].preferredWidth);
                    maxWidth2 = Math.Max(maxWidth2, textFields[i * 4 + 2].preferredWidth);
                    maxWidth3 = Math.Max(maxWidth3, textFields[i * 4 + 3].preferredWidth);
                    button.gameObject.SetActive(true);
                    textFields[i * 4].gameObject.SetActive(true);
                    textFields[i * 4 + 1].gameObject.SetActive(true);
                    textFields[i * 4 + 2].gameObject.SetActive(true);
                    textFields[i * 4 + 3].gameObject.SetActive(true);
                }
                else
                {
                    if (button != null)
                    {
                        button.gameObject.SetActive(false);
                        textFields[i * 4].gameObject.SetActive(false);
                        textFields[i * 4 + 1].gameObject.SetActive(false);
                        textFields[i * 4 + 2].gameObject.SetActive(false);
                        textFields[i * 4 + 3].gameObject.SetActive(false);
                    }
                }
            }
            if (chk)
            {
                for (var i = 0; i < count; i++)
                {
                    var y = 20f * i + 24f;
                    UI.Util.NormalizeRectWithTopLeft(textFields[i * 4].rectTransform, 24f + maxWidth0 + 5f, y);
                    UI.Util.NormalizeRectWithTopLeft(textFields[i * 4 + 1].rectTransform, 24f + maxWidth0 + 10f, y);
                    UI.Util.NormalizeRectWithTopLeft(textFields[i * 4 + 2].rectTransform, 24f + maxWidth0 + 10f + maxWidth1 + 5f, y);
                    UI.Util.NormalizeRectWithTopLeft(textFields[i * 4 + 3].rectTransform, 24f + maxWidth0 + 10f + maxWidth1 + 5f + maxWidth2 + 5f + maxWidth3, y);
                }
            }
            MilkyWayTopTenPlayersToggler.SetLabelText(chk ? "Hide top players".Translate() : "Show top players".Translate());

            string ToKMG(long value)
            {
                StringBuilderUtility.WriteKMG(_sb, 8, value, true);
                return _sb.ToString();
            }

            string SeedToString(long seed)
            {
                int combatValue = (int)(seed % 1000L);
                int resourceMultiplier = (int)(seed / 1000L % 100L);
                int starCount = (int)(seed / 100000L % 1000L);
                int gameSeed = (int)(seed / 100000000L);
                string text;
                if (combatValue / 100 > 0)
                {
                    text = String.Format("{0:D8}-{1}-Z{2}-{3:00}", gameSeed, starCount, resourceMultiplier, combatValue % 100);
                }
                else
                {
                    text = String.Format("{0:D8}-{1}-A{2}", gameSeed, starCount, resourceMultiplier);
                }
                return text;
            }
        }
    }
    #endregion
}
