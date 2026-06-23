using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UXAssist.Common;
using UXAssist.Functions;
using UXAssist.ModsCompat;
using UXAssist.Patches;
using UXAssist.Patches.Logistics;
using UXAssist.Common.Config;
using UXAssist.UI;

namespace UXAssist;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;
    private static RectTransform _dysonTab;
    private static UIButton _dysonInitBtn;
    private static readonly UIButton[] DysonLayerBtn = new UIButton[10];

    public static void Init()
    {
                                                                                /*
                        */
        MyConfigWindow.OnUICreated += CreateUI;
        MyConfigWindow.OnUpdateUI += UpdateUI;
    }

    private class OcMapper : MyWindow.ValueMapper<int>
    {
        public override int Min => 0;
        public override int Max => 40;

        public override string FormatValue(string format, int value)
        {
            return value == 0 ? "max".Translate() : base.FormatValue(format, value);
        }
    }

    private class AngleMapper : MyWindow.ValueMapper<float>
    {
        public override int Min => 0;
        public override int Max => 20;
        public override float IndexToValue(int index) => index - 10f;
        public override int ValueToIndex(float value) => Mathf.RoundToInt(value + 10f);
    }

    private class DistanceMapper : MyWindow.ValueMapper<double>
    {
        public override int Min => 1;
        public override int Max => 40;
        public override double IndexToValue(int index) => index * 0.5;
        public override int ValueToIndex(double value) => Mathf.RoundToInt((float)(value * 2.0));
    }

    private class UpsMapper : MyWindow.ValueMapper<double>
    {
        public override int Min => 1;
        public override int Max => 100;
        public override double IndexToValue(int index) => index * 0.1;
        public override int ValueToIndex(double value) => Mathf.RoundToInt((float)(value * 10.0));
    }

    private class AutoConfigDispenserChargePowerMapper() : MyWindow.RangeValueMapper<int>(3, 30)
    {
        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 300000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigBattleBaseChargePowerMapper() : MyWindow.RangeValueMapper<int>(4, 40)
    {

        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 300000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigPLSChargePowerMapper() : MyWindow.RangeValueMapper<int>(2, 20)
    {

        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 3000000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigCarrierMinDeliverMapper() : MyWindow.RangeValueMapper<int>(0, 10)
    {
        public override string FormatValue(string format, int value)
        {
            return (value == 0 ? 1 : (value * 10)).ToString("0\\%");
        }
    }

    private class AutoConfigMinPilerValueMapper() : MyWindow.RangeValueMapper<int>(0, 4)
    {
        public override string FormatValue(string format, int value)
        {
            return value == 0 ? "Use tech max for piler".Translate().Trim() : value.ToString();
        }
    }

    private class AutoConfigILSChargePowerMapper() : MyWindow.RangeValueMapper<int>(2, 20)
    {
        public override string FormatValue(string format, int value)
        {
            var sb = new StringBuilder("         ");
            StringBuilderUtility.WriteKMG(sb, 8, value * 15000000L, false);
            sb.Append('W');
            return sb.ToString().Trim();
        }
    }

    private class AutoConfigILSMaxTripShipMapper() : MyWindow.RangeValueMapper<int>(1, 41)
    {
        public override string FormatValue(string format, int value)
        {
            return value switch
            {
                <= 20 => value.ToString("0LY"),
                <= 40 => (value * 2 - 20).ToString("0LY"),
                _ => "∞",
            };
        }
    }

    private class AutoConfigILSWarperDistanceMapper() : MyWindow.RangeValueMapper<int>(2, 21)
    {
        public override string FormatValue(string format, int value)
        {
            return value switch
            {
                <= 7 => (value * 0.5 - 0.5).ToString("0.0AU"),
                <= 13 => (value - 4.0).ToString("0.0AU"),
                <= 16 => (value - 4).ToString("0AU"),
                <= 20 => (value * 2 - 20).ToString("0AU"),
                _ => "60AU",
            };
        }
    }

    private class AutoConfigVeinCollectorHarvestSpeedMapper() : MyWindow.RangeValueMapper<int>(0, 20)
    {
        public override string FormatValue(string format, int value)
        {
            return (100 + value * 10).ToString("0\\%");
        }
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        UnityEngine.UI.Text txt;
        _windowTrans = trans;
        wnd.AddTabGroup(trans, "UXAssist", "tab-group-uxassist");
        var tab1 = wnd.AddTab(trans, "General");
        var x = 0f;
        var y = 10f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.EnableWindowResizeEnabled, "Enable game window resize");
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.LoadLastWindowRectEnabled, "Remeber window position and size on last exit");
        /*
        y += 30f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.AutoSaveOptEnabled, "Better auto-save mechanism");
        x = 200f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Better auto-save mechanism", "Better auto-save mechanism tips", "auto-save-opt-tips");
        x = 0f;
        */
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.ConvertSavesFromPeaceEnabled, "Convert old saves to Combat Mode on loading");
        MyCheckBox checkBoxForMeasureTextWidth;
        if (WindowFunctions.ProfileName != null)
        {
            y += 36f;
            checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedSaveFolderEnabled, "Profile-based save folder");
            wnd.AddTipsButton2(checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab1, "Profile-based save folder", "Profile-based save folder tips", "btn-profile-based-save-folder-tips");
            y += 36f;
            checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedOptionEnabled, "Profile-based option");
            wnd.AddTipsButton2(checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab1, "Profile-based option", "Profile-based option tips", "btn-profile-based-option-tips");
            y += 36f;
            wnd.AddText2(x + 2f, y, tab1, "Default profile name", 15, "text-default-profile-name");
            y += 24f;
            wnd.AddInputField(x + 2f, y, 200f, tab1, GamePatch.DefaultProfileName, 15, "input-profile-save-folder");
            y += 18f;
        }
        y += 36f;
        wnd.AddButton(x, y, 200f, tab1, "Show recent milkyway upload results", 16, "button-show-recent-milkyway-upload-results", () => UIFunctions.ShowRecentMilkywayUploadResults());
        if (!BulletTimeWrapper.HasBulletTime)
        {
            y += 36f;
            txt = wnd.AddText2(x + 2f, y, tab1, "Logical Frame Rate", 15, "game-frame-rate");
            x += txt.preferredWidth + 7f;
            wnd.AddSlider(x, y + 6f, tab1, GamePatch.GameUpsFactor, new UpsMapper(), "0.0x", 100f).WithSmallerHandle();
            var btn = wnd.AddFlatButton(x + 104f, y + 6f, tab1, "Reset", 13, "reset-game-frame-rate", () => GamePatch.GameUpsFactor.Value = 1.0f);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(40f, 20f);
            x = 0f;
        }
        y += 36f;
        txt = wnd.AddText2(x + 2f, y, tab1, "Process priority", 15, "process-priority");
        wnd.AddComboBox(x + 7f + txt.preferredWidth, y, tab1).WithItems("High", "Above Normal", "Normal", "Below Normal", "Idle").WithSize(100f, 0f).WithConfigEntry(WindowFunctions.ProcessPriority);

        var tab2 = wnd.AddTab(trans, "Factory");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.RemoveSomeConditionEnabled, "Remove some build conditions");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.RemoveBuildRangeLimitEnabled, "Remove build range limit");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.NightLightEnabled, "Night Light");
        x += checkBoxForMeasureTextWidth.Width + 5f + 10f;
        txt = wnd.AddText2(x, y + 2f, tab2, "Angle X:", 13, "text-nightlight-angle-x");
        x += txt.preferredWidth + 5f;
        wnd.AddSlider(x, y + 7f, tab2, FactoryConfigProvider.NightLightAngleX, new AngleMapper(), "0", 60f).WithSmallerHandle();
        x += 70f;
        txt = wnd.AddText2(x, y + 2f, tab2, "Y:", 13, "text-nightlight-angle-y");
        wnd.AddSlider(x + txt.preferredWidth + 5f, y + 7f, tab2, FactoryConfigProvider.NightLightAngleY, new AngleMapper(), "0", 60f).WithSmallerHandle();
        x = 0;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.LargerAreaForUpgradeAndDismantleEnabled, "Larger area for upgrade and dismantle");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.LargerAreaForTerraformEnabled, "Larger area for terraform");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.OffGridBuildingEnabled, "Off-grid building and stepped rotation");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.CutConveyorBeltEnabled, "Cut conveyor belt (with shortcut key)");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.TreatStackingAsSingleEnabled, "Treat stack items as single in monitor components");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.QuickBuildAndDismantleLabsEnabled, "Quick build and dismantle stacking labs");

        {
            y += 36f;
            var cb = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.TankFastFillInAndTakeOutEnabled, "Fast fill in to and take out from tanks");
            x += cb.Width + 5f;
            txt = wnd.AddText2(x, y + 2f, tab2, "Speed Ratio", 13, "text-tank-fast-fill-speed-ratio");
            var tankSlider = wnd.AddSlider(x + txt.preferredWidth + 5f, y + 7f, tab2, FactoryConfigProvider.TankFastFillInAndTakeOutMultiplier, [2, 5, 10, 20, 50, 100, 500, 1000], "G", 100f).WithSmallerHandle();
            FactoryConfigProvider.TankFastFillInAndTakeOutEnabled.SettingChanged += TankSettingChanged;
            wnd.OnFree += () => { FactoryConfigProvider.TankFastFillInAndTakeOutEnabled.SettingChanged -= TankSettingChanged; };
            TankSettingChanged(null, null);

            void TankSettingChanged(object o, EventArgs e)
            {
                tankSlider.SetEnable(FactoryConfigProvider.TankFastFillInAndTakeOutEnabled.Value);
            }
        }

        x = 0;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.DoNotRenderEntitiesEnabled, "Do not render factory entities");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.ShortcutKeysForBlueprintCopyEnabled, "Shortcut keys for Blueprint Copy mode");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab2, "Shortcut keys for Blueprint Copy mode", "Shortcut keys for Blueprint Copy mode tips", "shortcut-keys-for-blueprint-copy-mode-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.BeltSignalsForBuyOutEnabled, "Belt signals for buy out dark fog items automatically");

        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.ProtectVeinsFromExhaustionEnabled, "Protect veins from exhaustion");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab2, "Protect veins from exhaustion", "Protect veins from exhaustion tips", "protect-veins-tips");
        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.DragBuildPowerPolesEnabled, "Drag building power poles in maximum connection range");
            y += 27f;
            var alternatelyCheckBox = wnd.AddCheckBox(x + 20f, y, tab2, FactoryConfigProvider.DragBuildPowerPolesAlternatelyEnabled, "Build Tesla Tower and Wireless Power Tower alternately", 13);
            FactoryConfigProvider.DragBuildPowerPolesEnabled.SettingChanged += AlternatelyCheckBoxChanged;
            wnd.OnFree += () => { FactoryConfigProvider.DragBuildPowerPolesEnabled.SettingChanged -= AlternatelyCheckBoxChanged; };
            AlternatelyCheckBoxChanged(null, null);

            void AlternatelyCheckBoxChanged(object o, EventArgs e)
            {
                alternatelyCheckBox.SetEnable(FactoryConfigProvider.DragBuildPowerPolesEnabled.Value);
            }
        }

        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.AutoConstructButtonEnabled, "Auto-construct button");

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.PressShiftToTakeWholeBeltItemsEnabled, "Ctrl+Shift+Click to pick items from whole belts");
            y += 27f;
            var includeBranches = wnd.AddCheckBox(x + 10, y, tab2, FactoryConfigProvider.PressShiftToTakeWholeBeltItemsIncludeBranches, "Include branches of belts", 13);
            y += 27f;
            var includeInserters = wnd.AddCheckBox(x + 10, y, tab2, FactoryConfigProvider.PressShiftToTakeWholeBeltItemsIncludeInserters, "Include connected inserters", 13);
            FactoryConfigProvider.PressShiftToTakeWholeBeltItemsEnabled.SettingChanged += PressShiftToTakeWholeBeltItemsEnabledChanged;
            wnd.OnFree += () => { FactoryConfigProvider.PressShiftToTakeWholeBeltItemsEnabled.SettingChanged -= PressShiftToTakeWholeBeltItemsEnabledChanged; };
            PressShiftToTakeWholeBeltItemsEnabledChanged(null, null);

            void PressShiftToTakeWholeBeltItemsEnabledChanged(object o, EventArgs e)
            {
                includeBranches.SetEnable(FactoryConfigProvider.PressShiftToTakeWholeBeltItemsEnabled.Value);
                includeInserters.SetEnable(FactoryConfigProvider.PressShiftToTakeWholeBeltItemsEnabled.Value);
            }
        }

        x = 400f;
        y = 10f;
        wnd.AddButton(x, y, tab2, "Initialize This Planet", 16, "button-init-planet", () =>
            UIMessageBox.Show("Initialize This Planet".Translate(), "Initialize This Planet Confirm".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.QUESTION, null,
                () => { PlanetFunctions.RecreatePlanet(true); })
        );
        y += 24f;
        wnd.AddCheckBox(x + 10f, y, tab2, PlanetFunctions.ReturnBuildingsOnInitializeEnabled, "Return buildings to player when initializing planet", 13);
        y += 24f;
        wnd.AddCheckBox(x + 10f, y, tab2, PlanetFunctions.ReturnLogisticStorageItemsOnInitializeEnabled, "Return logistic storage items to player when initializing planet", 13);
        y += 24f;
        wnd.AddCheckBox(x + 10f, y, tab2, PlanetFunctions.ReturnBeltAFactoryItemsOnInitializeEnabled, "Return belt and factory items to player when initializing planet", 13);

        y += 36f;
        wnd.AddButton(x, y, tab2, "Dismantle All Buildings", 16, "button-dismantle-all", () =>
            UIMessageBox.Show("Dismantle All Buildings".Translate(), "Dismantle All Buildings Confirm".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.QUESTION, null,
                () => { PlanetFunctions.DismantleAll(false); })
        );
        y += 72f;
        wnd.AddButton(x, y, 200, tab2, "Quick build Orbital Collectors", 16, "button-init-planet", PlanetFunctions.BuildOrbitalCollectors);
        y += 30f;
        txt = wnd.AddText2(x + 10f, y, tab2, "Maximum count to build", 15, "text-oc-build-count");
        wnd.AddSlider(x + 10f + txt.preferredWidth + 5f, y + 6f, tab2, PlanetFunctions.OrbitalCollectorMaxBuildCount, new OcMapper(), "G", 160f);

        y += 18f;

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.TweakBuildingBufferEnabled, "Tweak building buffers");
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Assembler buffer time multiplier(in seconds)", 13);
            var nx1 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Assembler buffer minimum multiplier", 13);
            var nx2 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Buffer count for assembling in labs", 13);
            var nx3 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Extra buffer count for Self-evolution Labs", 13);
            var nx4 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Buffer count for researching in labs", 13);
            var nx5 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Ray Receiver Graviton Lens buffer count", 13);
            var nx6 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Ejector Solar Sails buffer count", 13);
            var nx7 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, "Silo Rockets buffer count", 13);
            var nx8 = txt.preferredWidth + 5f;
            y -= 189f;
            var mx = Mathf.Max(nx1, nx2, nx3, nx4, nx5, nx6, nx7, nx8) + 20f;
            var assemblerBufferTimeMultiplierSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.AssemblerBufferTimeMultiplier, new MyWindow.RangeValueMapper<int>(2, 10), "0", 120f).WithSmallerHandle();
            y += 27f;
            var assemblerBufferMininumMultiplierSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.AssemblerBufferMininumMultiplier, new MyWindow.RangeValueMapper<int>(2, 10), "0", 120f).WithSmallerHandle();
            y += 27f;
            var labBufferMaxCountForAssembleSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.LabBufferMaxCountForAssemble, new MyWindow.RangeValueMapper<int>(2, 20), "0", 120f).WithSmallerHandle();
            y += 27f;
            var labBufferExtraCountForAdvancedAssembleSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.LabBufferExtraCountForAdvancedAssemble, new MyWindow.RangeValueMapper<int>(1, 10), "0", 120f).WithSmallerHandle();
            y += 27f;
            var labBufferMaxCountForResearchSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.LabBufferMaxCountForResearch, new MyWindow.RangeValueMapper<int>(2, 20), "0", 120f).WithSmallerHandle();
            y += 27f;
            var receiverBufferCountSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.ReceiverBufferCount, new MyWindow.RangeValueMapper<int>(1, 20), "0", 120f).WithSmallerHandle();
            y += 27f;
            var ejectorBufferCountSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.EjectorBufferCount, new MyWindow.RangeValueWithMultiplierMapper<int>(1, 80, 5), "0", 120f).WithSmallerHandle();
            y += 27f;
            var siloBufferCountSlider = wnd.AddSlider(x + mx, y + 5f, tab2, FactoryConfigProvider.SiloBufferCount, new MyWindow.RangeValueMapper<int>(1, 40), "0", 120f).WithSmallerHandle();
            FactoryConfigProvider.TweakBuildingBufferEnabled.SettingChanged += TweakBuildingBufferChanged;
            wnd.OnFree += () => { FactoryConfigProvider.TweakBuildingBufferEnabled.SettingChanged -= TweakBuildingBufferChanged; };
            TweakBuildingBufferChanged(null, null);
            void TweakBuildingBufferChanged(object o, EventArgs e)
            {
                assemblerBufferTimeMultiplierSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                assemblerBufferMininumMultiplierSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                labBufferMaxCountForAssembleSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                labBufferExtraCountForAdvancedAssembleSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                labBufferMaxCountForResearchSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                receiverBufferCountSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                ejectorBufferCountSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
                siloBufferCountSlider.SetEnable(FactoryConfigProvider.TweakBuildingBufferEnabled.Value);
            }
        }

        var tab3 = wnd.AddTab(trans, "Logistics");
        x = 0f;
        y = 10f;

        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.LogisticsCapacityTweaksEnabled, "Enhance control for logistic storage capacities");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab3, "Enhance control for logistic storage capacities", "Enhance control for logistic storage capacities tips", "enhanced-logistic-capacities-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.AllowOverflowInLogisticsEnabled, "Allow overflow for Logistic Stations and Advanced Mining Machines");
        y += 30f;
        wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.GreaterPowerUsageInLogisticsEnabled, "Increase maximum power usage in Logistic Stations and Advanced Mining Machines");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.LogisticsConstrolPanelImprovementEnabled, "Logistics Control Panel Improvement");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab3, "Logistics Control Panel Improvement", "Logistics Control Panel Improvement tips", "lcp-improvement-tips");
        {
            y += 36f;
            var realtimeLogisticsInfoPanelCheckBox = wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled, "Real-time logistic stations info panel");
            y += 27f;
            var realtimeLogisticsInfoPanelBarsCheckBox = wnd.AddCheckBox(x + 20f, y, tab3, LogisticsConfigProvider.RealtimeLogisticsInfoPanelBarsEnabled, "Show status bars for storage items", 13);
            if (AuxilaryfunctionWrapper.ShowStationInfo != null)
            {
                AuxilaryfunctionWrapper.ShowStationInfo.SettingChanged += RealtimeLogisticsInfoPanelChanged;
                wnd.OnFree += () => { AuxilaryfunctionWrapper.ShowStationInfo.SettingChanged -= RealtimeLogisticsInfoPanelChanged; };
            }
            LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled.SettingChanged += RealtimeLogisticsInfoPanelChanged;
            wnd.OnFree += () => { LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled.SettingChanged -= RealtimeLogisticsInfoPanelChanged; };
            RealtimeLogisticsInfoPanelChanged(null, null);

            void RealtimeLogisticsInfoPanelChanged(object o, EventArgs e)
            {
                if (AuxilaryfunctionWrapper.ShowStationInfo == null)
                {
                    realtimeLogisticsInfoPanelCheckBox.SetEnable(true);
                    realtimeLogisticsInfoPanelBarsCheckBox.SetEnable(LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled.Value);
                    return;
                }

                var on = !AuxilaryfunctionWrapper.ShowStationInfo.Value;
                realtimeLogisticsInfoPanelCheckBox.SetEnable(on);
                realtimeLogisticsInfoPanelBarsCheckBox.SetEnable(on & LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled.Value);
                if (!on)
                {
                    LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled.Value = false;
                }
            }
        }
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.AutoConfigLogisticsEnabled, "Auto-config logistic stations");
        y += 26f;
        wnd.AddCheckBox(x + 10f, y, tab3, LogisticsConfigProvider.AutoConfigLimitAutoReplenishCount, "Limit auto-replenish count to values below", 13).WithSmallerBox();
        y += 18f;
        wnd.AddCheckBox(x + 10f, y, tab3, LogisticsConfigProvider.SetDefaultRemoteLogicToStorage, "Set default remote logic to storage", 13).WithSmallerBox();
        y += 16f;
        var maxWidth = 0f;
        wnd.AddText2(10f, y, tab3, "Dispenser", 14, "text-dispenser");
        var dispenserCatY = y;
        y += 18f;
        var oy = y;
        x = 20f;
        var textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-dispenser-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Bots filled", 13, "text-dispenser-count-of-bots-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "Battlefield Analysis Base", 14, "text-battlefield-analysis-base");
        var battleBaseCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-battlefield-analysis-base-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "PLS", 14, "text-pls");
        var plsCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-pls-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Drone transport range", 13, "text-pls-drone-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Load of Drones", 13, "text-pls-min-load-of-drones");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Outgoing integration count", 13, "text-pls-outgoing-integration-count");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Drones filled", 13, "text-pls-count-of-drones-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "ILS", 14, "text-ils");
        var ilsCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Max. Charging Power", 13, "text-ils-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Drone transport range", 13, "text-ils-drone-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Vessel transport range", 13, "text-ils-vessel-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Warp distance", 13, "text-ils-warp-distance");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Load of Drones", 13, "text-ils-min-load-of-drones");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Load of Vessels", 13, "text-ils-min-load-of-vessels");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Outgoing integration count", 13, "text-ils-outgoing-integration-count");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Drones filled", 13, "text-ils-count-of-drones-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Count of Vessels filled", 13, "text-ils-count-of-vessels-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, "Advanced Mining Machine", 14, "text-amm");
        var ammCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Collecting Speed", 13, "text-amm-collecting-speed");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, "Min. Piler Value", 13, "text-amm-min-piler-value");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y = oy + 1;
        var nx = x + maxWidth + 5f + 10f;
        const float applyBtnWidth = 44f;
        const float applyBtnHeight = 15f;
        const int applyBtnFontSize = 11;
        const float applyAllBtnWidth = 58f;
        var applyBtnX = nx + 265f;
        // Right-align the wider "Apply All" buttons with the right edge of the per-option "Apply" buttons.
        var applyAllBtnX = applyBtnX + applyBtnWidth - applyAllBtnWidth;
        var checkBoxX = applyBtnX + applyBtnWidth + 12f;

        // Buttons are vertically centered on their row (NormalizeRectWithTopLeft places y at the top edge).
        void AddApplyButtonAt(float bx, float rowY, string objName, UnityAction onClick) =>
            wnd.AddFlatButton(bx, rowY + 6f, tab3, "Apply config to planet", applyBtnFontSize, objName, onClick).WithSize(applyBtnWidth, applyBtnHeight)
                .WithFontSize(applyBtnFontSize).WithTip("Apply config to planet tips".Translate());

        void AddApplyButton(float rowY, string objName, UnityAction onClick) => AddApplyButtonAt(applyBtnX, rowY, objName, onClick);

        void AddCategoryApplyButton(float catY, string objName, UnityAction onClick) =>
            wnd.AddFlatButton(applyAllBtnX, catY + 7f, tab3, "Apply all config to planet", applyBtnFontSize, objName, onClick).WithSize(applyAllBtnWidth, applyBtnHeight)
                .WithFontSize(applyBtnFontSize).WithTip("Apply all config to planet tips".Translate());

        AddCategoryApplyButton(dispenserCatY, "btn-apply-all-dispenser", LogisticsPatch.ApplyAllDispenser);
        AddCategoryApplyButton(battleBaseCatY, "btn-apply-all-battle-base", LogisticsPatch.ApplyAllBattleBase);
        AddCategoryApplyButton(plsCatY, "btn-apply-all-pls", LogisticsPatch.ApplyAllPLS);
        AddCategoryApplyButton(ilsCatY, "btn-apply-all-ils", LogisticsPatch.ApplyAllILS);
        AddCategoryApplyButton(ammCatY, "btn-apply-all-amm", LogisticsPatch.ApplyAllVeinCollector);

        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigDispenserChargePower, new AutoConfigDispenserChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-dispenser-charge-power", LogisticsPatch.ApplyDispenserChargePower);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigDispenserCourierCount, new MyWindow.RangeValueMapper<int>(0, 10), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-dispenser-courier-count", LogisticsPatch.ApplyDispenserCourierCount);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigBattleBaseChargePower, new AutoConfigBattleBaseChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-battle-base-charge-power", LogisticsPatch.ApplyBattleBaseChargePower);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigPLSChargePower, new AutoConfigPLSChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-pls-charge-power", LogisticsPatch.ApplyPLSChargePower);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigPLSMaxTripDrone, new MyWindow.RangeValueMapper<int>(1, 180), "0°", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-pls-trip-range-drones", LogisticsPatch.ApplyPLSTripRangeDrones);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigPLSDroneMinDeliver, new AutoConfigCarrierMinDeliverMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-pls-drone-min-deliver", LogisticsPatch.ApplyPLSDroneMinDeliver);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigPLSMinPilerValue, new AutoConfigMinPilerValueMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-pls-min-piler-value", LogisticsPatch.ApplyPLSMinPilerValue);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigPLSDroneCount, new MyWindow.RangeValueMapper<int>(0, 50), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-pls-drone-count", LogisticsPatch.ApplyPLSDroneCount);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSChargePower, new AutoConfigILSChargePowerMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-charge-power", LogisticsPatch.ApplyILSChargePower);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSMaxTripDrone, new MyWindow.RangeValueMapper<int>(1, 180), "0°", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-trip-range-drones", LogisticsPatch.ApplyILSTripRangeDrones);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSMaxTripShip, new AutoConfigILSMaxTripShipMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-trip-range-ships", LogisticsPatch.ApplyILSTripRangeShips);
        var includeOrbitCollectorCheckBox = wnd.AddCheckBox(checkBoxX, y + 4f, tab3, LogisticsConfigProvider.AutoConfigILSIncludeOrbitCollector, "Include Orbital Collector", 13).WithSmallerBox();
        var includeOrbitCollectorY = y;
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSWarperDistance, new AutoConfigILSWarperDistanceMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-warp-distance", LogisticsPatch.ApplyILSWarpDistance);
        var warperNecessaryCheckBox = wnd.AddCheckBox(checkBoxX, y + 4f, tab3, LogisticsConfigProvider.AutoConfigILSWarperNecessary, "Warpers required", 13).WithSmallerBox();
        // Align both checkbox "Apply" buttons in a common column after the widest checkbox label.
        var checkBoxApplyBtnX = checkBoxX + Mathf.Max(includeOrbitCollectorCheckBox.Width, warperNecessaryCheckBox.Width) + 10f;
        AddApplyButtonAt(checkBoxApplyBtnX, includeOrbitCollectorY, "btn-apply-ils-include-orbit-collector", LogisticsPatch.ApplyILSIncludeOrbitCollector);
        AddApplyButtonAt(checkBoxApplyBtnX, y, "btn-apply-ils-warper-necessary", LogisticsPatch.ApplyILSWarperNecessary);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSDroneMinDeliver, new AutoConfigCarrierMinDeliverMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-drone-min-deliver", LogisticsPatch.ApplyILSDroneMinDeliver);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSShipMinDeliver, new AutoConfigCarrierMinDeliverMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-ship-min-deliver", LogisticsPatch.ApplyILSShipMinDeliver);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSMinPilerValue, new AutoConfigMinPilerValueMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-min-piler-value", LogisticsPatch.ApplyILSMinPilerValue);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSDroneCount, new MyWindow.RangeValueMapper<int>(0, 100), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-drone-count", LogisticsPatch.ApplyILSDroneCount);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSShipCount, new MyWindow.RangeValueMapper<int>(0, 10), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-ship-count", LogisticsPatch.ApplyILSShipCount);
        y += 36f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigVeinCollectorHarvestSpeed, new AutoConfigVeinCollectorHarvestSpeedMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-amm-harvest-speed", LogisticsPatch.ApplyVeinCollectorHarvestSpeed);
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigVeinCollectorMinPilerValue, new AutoConfigMinPilerValueMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-amm-min-piler-value", LogisticsPatch.ApplyVeinCollectorMinPilerValue);
        x = 0f;

        var tab4 = wnd.AddTab(trans, "Player/Mecha");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab4, FactoryConfigProvider.UnlimitInteractiveEnabled, "Unlimited interactive range");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlanetPatch.PlayerActionsInGlobeViewEnabled, "Enable player actions in globe view");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlayerPatch.HideTipsForSandsChangesEnabled, "Hide tips for soil piles changes");
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab4, PlayerPatch.EnhancedMechaForgeCountControlEnabled, "Enhanced count control for hand-make");
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab4, "Enhanced count control for hand-make", "Enhanced count control for hand-make tips", "enhanced-count-control-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlayerPatch.ShortcutKeysForStarsNameEnabled, "Shortcut keys for showing stars' name");

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab4, PlayerPatch.AutoNavigationEnabled, "Auto navigation on sailings");
            y += 27f;
            var autoCruiseCheckBox = wnd.AddCheckBox(x + 20f, y, tab4, PlayerPatch.AutoCruiseEnabled, "Enable auto-cruise", 13);
            y += 27f;
            var autoBoostCheckBox = wnd.AddCheckBox(x + 20f, y, tab4, PlayerPatch.AutoBoostEnabled, "Auto boost", 13);
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab4, "Distance to use warp", 15, "text-distance-to-warp");
            var navDistanceSlider = wnd.AddSlider(x + 20f + txt.preferredWidth + 5f, y + 6f, tab4, PlayerPatch.DistanceToWarp, new DistanceMapper(), "0.0", 100f);
            PlayerPatch.AutoNavigationEnabled.SettingChanged += NavSettingChanged;
            wnd.OnFree += () => { PlayerPatch.AutoNavigationEnabled.SettingChanged -= NavSettingChanged; };
            NavSettingChanged(null, null);

            void NavSettingChanged(object o, EventArgs e)
            {
                autoCruiseCheckBox.SetEnable(PlayerPatch.AutoNavigationEnabled.Value);
                autoBoostCheckBox.SetEnable(PlayerPatch.AutoNavigationEnabled.Value);
                navDistanceSlider.SetEnable(PlayerPatch.AutoNavigationEnabled.Value);
            }
        }

        var tab5 = wnd.AddTab(trans, "Dyson Sphere");
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab5, DysonSpherePatch.StopEjectOnNodeCompleteEnabled, "Stop ejectors when available nodes are all filled up");
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, DysonSpherePatch.OnlyConstructNodesEnabled, "Construct only structure points but frames");
        x = 400f;
        y = 10f;
        _dysonInitBtn = wnd.AddButton(x, y, tab5, "Initialize Dyson Sphere", 16, "init-dyson-sphere", () =>
            UIMessageBox.Show("Initialize Dyson Sphere".Translate(), "Initialize Dyson Sphere Confirm".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.QUESTION, null,
                () => { DysonSphereFunctions.InitCurrentDysonLayer(null, -1); })
        );
        y += 36f;
        wnd.AddText2(x, y, tab5, "Click to dismantle selected layer", 16, "text-dismantle-layer");
        y += 27f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = wnd.AddFlatButton(x, y, tab5, id.ToString(), 12, "dismantle-layer-" + id, () =>
                {
                    var star = DysonSphereFunctions.CurrentStarForDysonSystem();
                    UIMessageBox.Show("Dismantle selected layer".Translate(), "Dismantle selected layer Confirm".Translate(), "Cancel".Translate(), "OK".Translate(), UIMessageBox.QUESTION, null,
                        () => { DysonSphereFunctions.InitCurrentDysonLayer(star, id); });
                }
            ).WithSize(40f, 20f);
            DysonLayerBtn[i] = btn.uiButton;
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
        y += 36f;
        txt = wnd.AddText2(x, y, tab5, "Auto Fast Build Speed Multiplier", 15, "text-auto-fast-build-multiplier");
        wnd.AddSlider(x + txt.preferredWidth + 5f, y + 6f, tab5, DysonSpherePatch.AutoConstructMultiplier, [1, 2, 5, 10, 20, 50, 100], "0", 100f);
        _dysonTab = tab5;

        var tab6 = wnd.AddTab(trans, "Tech/Combat/UI");
        x = 10;
        y = 10;
        wnd.AddCheckBox(x, y, tab6, UIPatch.PlanetVeinUtilizationEnabled, "Planet vein utilization");
        y += 72f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.BatchBuyoutTechEnabled, "Buy out techs with their prerequisites");
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.SorterCargoStackingEnabled, "Restore upgrades of \"Sorter Cargo Stacking\" on panel");
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.DisableBattleRelatedTechsInPeaceModeEnabled, "Disable battle-related techs in Peace mode");
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, "Set \"Sorter Cargo Stacking\" to unresearched state", 16, "button-remove-cargo-stacking", TechFunctions.RemoveCargoStackingTechs);
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, "Unlock all techs with metadata", 16, "button-unlock-all-techs-with-metadata", TechFunctions.UnlockAllProtoWithMetadataAndPrompt);
        y += 72f;
        wnd.AddButton(x, y, 300f, tab6, "Open Dark Fog Communicator", 16, "button-open-df-communicator", () =>
        {
            if (!(GameMain.data?.gameDesc.isCombatMode ?? false)) return;
            var uiGame = UIRoot.instance.uiGame;
            uiGame.ShutPlayerInventory();
            uiGame.CloseEnemyBriefInfo();
            uiGame.OpenCommunicatorWindow(5);
        });
    }

    private static void UpdateUI()
    {
        UpdateDysonShells();
    }

    private static void UpdateDysonShells()
    {
        if (!_dysonTab.gameObject.activeSelf) return;
        var star = DysonSphereFunctions.CurrentStarForDysonSystem();
        if (star == null)
        {
            for (var i = 0; i < 10; i++)
            {
                DysonLayerBtn[i].button.interactable = false;
            }
            return;
        }
        var dysonSpheres = GameMain.data?.dysonSpheres;
        if (dysonSpheres?[star.index] == null) return;
        var ds = dysonSpheres[star.index];
        for (var i = 1; i <= 10; i++)
        {
            var layer = ds.layersIdBased[i];
            DysonLayerBtn[i - 1].button.interactable = layer != null && layer.id == i;
        }
    }
}