using CheatEnabler.Functions;
using CheatEnabler.Patches;
using CheatEnabler.Patches.Factory;
using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;
using System;

namespace CheatEnabler;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;

    private static UIButton _resignGameBtn;
    private static UIButton _clearBanBtn;

    public static void Init()
    {
        MyConfigWindow.OnUICreated += CreateUI;
        MyConfigWindow.OnUpdateUI += UpdateUI;
    }

    class MaxOrbitRadiusValueMapper : MyWindow.RangeValueMapper<float>
    {
        public MaxOrbitRadiusValueMapper() : base(1, 20)
        {
        }

        public override int ValueToIndex(float value)
        {
            int result = Mathf.FloorToInt(value / 500_000f);
            if (result < 1) result = 1;
            if (result > 20) result = 20;
            return result;
        }

        public override float IndexToValue(int index)
        {
            return index * 500_000f;
        }
    }

    class ShellsCountMapper : MyWindow.RangeValueMapper<int>
    {
        public ShellsCountMapper() : base(1, 139)
        {
        }

        public override int ValueToIndex(int value)
        {
            return value switch
            {
                < 4 => value,
                < 64 => value / 4 + 3,
                < 256 => value / 16 + 15,
                < 4096 => value / 64 + 27,
                _ => value / 256 + 75,
            };
        }

        public override int IndexToValue(int index)
        {
            return index switch
            {
                < 4 => index,
                < 19 => (index - 3) * 4,
                < 31 => (index - 15) * 16,
                < 91 => (index - 27) * 64,
                _ => (index - 75) * 256,
            };
        }
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        _windowTrans = trans;
        // General tab
        var x = 0f;
        var y = 10f;
        wnd.AddSplitter(trans, 10f);
        wnd.AddTabGroup(trans, "Cheat Enabler", "tab-group-cheatenabler");
        var tab1 = wnd.AddTab(_windowTrans, "General");
        var cb = wnd.AddCheckBox(x, y, tab1, GamePatch.DevShortcutsEnabled, "Enable Dev Shortcuts");
        x += cb.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Dev Shortcuts", "Dev Shortcuts Tips", "dev-shortcuts-tips");
        x = 0;
        y += 30f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.AbnormalDisablerEnabled, "Disable Abnormal Checks");
        y += 36f;
        cb = wnd.AddCheckBox(x, y, tab1, GamePatch.UnlockTechEnabled, "Unlock Tech with Key-Modifiers");
        x += cb.Width + 5f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Unlock Tech with Key-Modifiers", "Unlock Tech with Key-Modifiers Tips", "unlock-tech-tips");
        x = 0f;
        y += 30f + 36f;
        wnd.AddButton(x, y, 400f, tab1, "Remove all metadata consumption records", 16, "button-remove-all-metadata-consumption", PlayerFunctions.RemoveAllMetadataConsumptions);
        y += 36f;
        wnd.AddButton(x, y, 400f, tab1, "Remove metadata consumption record in current game", 16, "button-remove-current-metadata-consumption", PlayerFunctions.RemoveCurrentMetadataConsumptions);
        y += 36f;
        _clearBanBtn = wnd.AddButton(x, y, 400f, tab1, "Clear metadata flag which bans achievements", 16, "button-clear-ban-list", PlayerFunctions.ClearMetadataBanAchievements);
        x = 300f;
        y = 10f;
        _resignGameBtn = wnd.AddButton(x, y, 300f, tab1, "Assign gamesave to current account", 16, "resign-game-btn", () => { GameMain.data.account = AccountData.me; });

        var tab2 = wnd.AddTab(_windowTrans, "Factory");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.ImmediateEnabled, "Finish build immediately");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.ArchitectModeEnabled, "Architect mode");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.NoConditionEnabled, "Build without condition");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.NoCollisionEnabled, "No collision");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalGeneratorEnabled, "Belt signal generator");
        x += 26f;
        y += 26f;
        var cb1 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountGenEnabled, "Count generations as production in statistics", 13);
        y += 26f;
        var cb2 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountRemEnabled, "Count removals as consumption in statistics", 13);
        y += 26f;
        var cb3 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalCountRecipeEnabled, "Count all raws and intermediates in statistics", 13);
        y += 26f;
        var cb4 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalUseProliferatorEnabled, "Count proliferators used for raws/intermediates and finished products", 13);
        y += 6f;
        var tip1 = wnd.AddTipsButton2(x + cb4.Width + 5f, y, tab2, "Count proliferators used for raws/intermediates and finished products", "Count proliferators used for raws/intermediates and finished products tips", "count-proliferators-used-for-raws-intermediates-and-finished-products-tips");
        y += 20f;
        var cb5 = wnd.AddCheckBox(x, y, tab2, FactoryPatch.BeltSignalNumberAltFormat, "Belt signal alt format", 13);
        y += 6f;
        var tip2 = wnd.AddTipsButton2(x + cb5.Width + 5f, y, tab2, "Belt signal alt format", "Belt signal alt format tips", "belt-signal-alt-format-tips");
        x = 0f;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.ControlPanelRemoteLogisticsEnabled, "Retrieve/Place items from/to remote planets on logistics control panel");
        {
            FactoryPatch.BeltSignalGeneratorEnabled.SettingChanged += OnBeltSignalChanged;
            wnd.OnFree += () => { FactoryPatch.BeltSignalGeneratorEnabled.SettingChanged -= OnBeltSignalChanged; };
            OnBeltSignalChanged(null, null);
            void OnBeltSignalChanged(object o, EventArgs e)
            {
                var on = FactoryPatch.BeltSignalGeneratorEnabled.Value;
                cb1.gameObject.SetActive(on);
                cb2.gameObject.SetActive(on);
                cb3.gameObject.SetActive(on);
                cb4.gameObject.SetActive(on);
                cb5.gameObject.SetActive(on);
                tip1.gameObject.SetActive(on);
                tip2.gameObject.SetActive(on);
            }
        }
        x = 350f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.RemovePowerSpaceLimitEnabled, "Remove power space limit");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.WindTurbinesPowerGlobalCoverageEnabled, "Wind Turbines do global power coverage");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostWindPowerEnabled, "Boost wind power");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostSolarPowerEnabled, "Boost solar power");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostGeothermalPowerEnabled, "Boost geothermal power");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryPatch.BoostFuelPowerEnabled, "Boost fuel power");
        y += 26f;
        wnd.AddText2(x + 32f, y, tab2, "Boost fuel power 2", 13);

        // Planet Tab
        var tab3 = wnd.AddTab(_windowTrans, "Planet");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab3, ResourcePatch.InfiniteResourceEnabled, "Infinite Natural Resources");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, ResourcePatch.FastMiningEnabled, "Fast Mining");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlanetPatch.WaterPumpAnywhereEnabled, "Pump Anywhere");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlanetPatch.TerraformAnywayEnabled, "Terraform without enough soil piles");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.InstantHandCraftEnabled, "Instant hand-craft");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, PlayerPatch.InstantTeleportEnabled, "Instant teleport (like that in Sandbox mode)");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, 200f, tab3, "Bury all veins", 16, "button-bury-all", () => { PlanetFunctions.BuryAllVeins(true); });
        y += 36f;
        wnd.AddButton(x, y, 200f, tab3, "Restore buried veins", 16, "button-bury-restore-all", () => { PlanetFunctions.BuryAllVeins(false); });
        y += 36f;
        wnd.AddButton(x, y, 200f, tab3, "Reform entire planet", 16, "button-reform-all", () =>
        {
            var player = GameMain.mainPlayer;
            if (player == null) return;
            var reformTool = player.controller.actionBuild.reformTool;
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformAll(reformTool.brushType, reformTool.brushColor, reformTool.buryVeins);
        });
        y += 36f;
        wnd.AddButton(x, y, 200f, tab3, "Revert planet terrain", 16, "button-reform-revert-all", () =>
        {
            var factory = GameMain.localPlanet?.factory;
            if (factory == null) return;
            GameMain.localPlanet.factory.PlanetReformRevert();
        });

        var tab4 = wnd.AddTab(_windowTrans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.SkipBulletEnabled, "Skip bullet period");
        y += 26f;
        wnd.AddCheckBox(x + 26f, y, tab4, DysonSpherePatch.FireAllBulletsEnabled, "Fire all bullets at once", 13);
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.SkipAbsorbEnabled, "Skip absorption period");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.QuickAbsorbEnabled, "Quick absorb");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.EjectAnywayEnabled, "Eject anyway");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.OverclockEjectorEnabled, "Overclock Ejectors");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.OverclockSiloEnabled, "Overclock Silos");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, DysonSpherePatch.UnlockMaxOrbitRadiusEnabled, "Unlock Dyson Sphere max orbit radius");
        y += 30f;
        {
            var slider = wnd.AddSlider(x + 20, y, tab4, DysonSpherePatch.UnlockMaxOrbitRadiusValue, new MaxOrbitRadiusValueMapper(), "##,#m").WithSmallerHandle(-40f);
            DysonSpherePatch.UnlockMaxOrbitRadiusEnabled.SettingChanged += UnlockMaxOrbitRadiusChanged;
            wnd.OnFree += () => { DysonSpherePatch.UnlockMaxOrbitRadiusEnabled.SettingChanged -= UnlockMaxOrbitRadiusChanged; };
            UnlockMaxOrbitRadiusChanged(null, null);

            void UnlockMaxOrbitRadiusChanged(object o, EventArgs e)
            {
                slider.slider.enabled = DysonSpherePatch.UnlockMaxOrbitRadiusEnabled.Value;
            }
        }
        x = 300f;
        y = 10f;
        wnd.AddButton(x, y, 300f, tab4, "Complete Dyson Sphere shells instantly", 16, "button-complete-dyson-sphere-shells-instantly", DysonSphereFunctions.CompleteShellsInstantly);
        y += 36f;
        wnd.AddButton(x, y, 300f, tab4, "Remove all frames on Dyson Sphere", 16, "button-remove-all-frames-on-dyson-sphere", DysonSphereFunctions.RemoveAllFrames);
        {
            y += 72f;
            var originalY = y;
            var btn0 = wnd.AddButton(x, y, 300f, tab4, "Generate illegal dyson shell", 16, "button-generate-illegal-dyson-shells", () =>
            {
                UIMessageBox.Show("Generate illegal dyson shell".Translate(), "WARNING: This operation can be very slow, continue?".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.WARNING, null,
                    () => { DysonSphereFunctions.CreateIllegalDysonShellWithMaxOutput(); });
            });
            y += 36f;
            var btn1 = wnd.AddButton(x, y, 300f, tab4, "Generate illegal dyson shell 2", 16, "button-generate-illegal-dyson-shells", () =>
            {
                UIMessageBox.Show("Generate illegal dyson shell 2".Translate(), "WARNING: This operation can be very slow, continue?".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.WARNING, null,
                    () => { DysonSphereFunctions.CreateIllegalDysonShellWithMaxOutputForAllLayers(); });
            });
            y += 36f;
            var btn2 = wnd.AddButton(x, y, 300f, tab4, "Keep max production shells and remove others", 16, "button-keep-max-production-shells", () =>
            {
                UIMessageBox.Show("Keep max production shells and remove others".Translate(), "WARNING: This operation is DANGEROUS, continue?".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.WARNING, null,
                    () => { DysonSphereFunctions.KeepMaxProductionShells(); });
            });
            y += 36f;
            var btn3 = wnd.AddButton(x, y, 300f, tab4, "Duplicate shells from that with highest production", 16, "button-duplicate-shells-from-the-highest-production", () =>
            {
                UIMessageBox.Show("Duplicate shells from that with highest production".Translate(), "WARNING: This operation can be very slow, continue?".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.WARNING, null,
                    () => { DysonSphereFunctions.DuplicateShellsWithHighestProduction(); });
            });
            y += 30f;
            var slider1 = wnd.AddSlider(x + 20f, y, tab4, DysonSphereFunctions.ShellsCountForFunctions, new ShellsCountMapper());

            y = originalY;
            var btn4 = wnd.AddButton(x, y, 300f, tab4, "Generate illegal dyson shell quickly", 16, "button-generate-illegal-dyson-shells-quickly", () =>
            {
                UIMessageBox.Show("Generate illegal dyson shell quickly".Translate(), "WARNING: This operation can be very slow, continue?".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.WARNING, null,
                    () => { DysonSphereFunctions.CreateIllegalDysonShellQuickly(DysonSphereFunctions.ShellsCountForFunctions.Value); });
            });
            y += 30f;
            var txt2 = wnd.AddText2(x, y, tab4, "Shells count", 15, "text-shells-count");
            var slider2 = wnd.AddSlider(x + txt2.preferredWidth + 5f, y + 6f, tab4, DysonSphereFunctions.ShellsCountForFunctions, new ShellsCountMapper());

            Functions.DysonSphereFunctions.IllegalDysonShellFunctionsEnabled.SettingChanged += onIllegalDysonShellFunctionsChanged;
            wnd.OnFree += () => { Functions.DysonSphereFunctions.IllegalDysonShellFunctionsEnabled.SettingChanged -= onIllegalDysonShellFunctionsChanged; };
            onIllegalDysonShellFunctionsChanged(null, null);
            void onIllegalDysonShellFunctionsChanged(object o, EventArgs e)
            {
                var enabled = Functions.DysonSphereFunctions.IllegalDysonShellFunctionsEnabled.Value;
                btn0.gameObject.SetActive(enabled);
                btn1.gameObject.SetActive(enabled);
                btn2.gameObject.SetActive(enabled);
                btn3.gameObject.SetActive(enabled);
                slider1.gameObject.SetActive(enabled);

                btn4.gameObject.SetActive(!enabled);
                txt2.gameObject.SetActive(!enabled);
                slider2.gameObject.SetActive(!enabled);
            }
        }

        var tab5 = wnd.AddTab(_windowTrans, "Mecha/Combat");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab5, CombatPatch.MechaInvincibleEnabled, "Mecha and Drones/Fleets invicible");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, CombatPatch.BuildingsInvincibleEnabled, "Buildings invicible");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, PlayerPatch.WarpWithoutSpaceWarpersEnabled, "Enable warp without space warpers");
        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, 200f, tab5, "Teleport to outer space", 16, "button-teleport-to-outer-space", PlayerFunctions.TeleportToOuterSpace);
        y += 36f;
        wnd.AddButton(x, y, 200f, tab5, "Teleport to selected astronomical", 16, "button-teleport-to-selected-astronomical", PlayerFunctions.TeleportToSelectedAstronomical);
    }

    private static void UpdateUI()
    {
        UpdateButtons();
    }

    private static void UpdateButtons()
    {
        if (_resignGameBtn == null || _clearBanBtn == null) return;
        var data = GameMain.data;
        if (data == null) return;
        var resignEnabled = data.account != AccountData.me;
        if (_resignGameBtn.gameObject.activeSelf != resignEnabled)
        {
            _resignGameBtn.gameObject.SetActive(resignEnabled);
        }

        var history = data.history;
        if (history == null) return;
        var banEnabled = history.hasUsedPropertyBanAchievement;
        if (_clearBanBtn.gameObject.activeSelf != banEnabled)
        {
            _clearBanBtn.gameObject.SetActive(banEnabled);
        }
    }
}