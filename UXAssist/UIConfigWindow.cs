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
            return value == 0 ? I18NKeys.Max.Translate() : base.FormatValue(format, value);
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
            return value == 0 ? I18NKeys.UseTechMaxForPiler.Translate().Trim() : value.ToString();
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
        wnd.AddTabGroup(trans, I18NKeys.UXAssist, "tab-group-uxassist");
        var tab1 = wnd.AddTab(trans, "General");
        var x = 0f;
        var y = 10f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.EnableWindowResizeEnabled, I18NKeys.EnableGameWindowResize);
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.LoadLastWindowRectEnabled, I18NKeys.RemeberWindowPositionAndSizeOnLastExit);
        /*
        y += 30f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.AutoSaveOptEnabled, "Better auto-save mechanism");
        x = 200f;
        y += 6f;
        wnd.AddTipsButton2(x, y, tab1, "Better auto-save mechanism", "Better auto-save mechanism tips", "auto-save-opt-tips");
        x = 0f;
        */
        y += 36f;
        wnd.AddCheckBox(x, y, tab1, GamePatch.ConvertSavesFromPeaceEnabled, I18NKeys.ConvertOldSavesToCombatModeOnLoading);
        MyCheckBox checkBoxForMeasureTextWidth;
        if (WindowFunctions.ProfileName != null)
        {
            y += 36f;
            checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedSaveFolderEnabled, I18NKeys.ProfileBasedSaveFolder);
            wnd.AddTipsButton2(checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab1, I18NKeys.ProfileBasedSaveFolder, I18NKeys.ProfileBasedSaveFolderTips, "btn-profile-based-save-folder-tips");
            y += 36f;
            checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab1, GamePatch.ProfileBasedOptionEnabled, I18NKeys.ProfileBasedOption);
            wnd.AddTipsButton2(checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab1, I18NKeys.ProfileBasedOption, I18NKeys.ProfileBasedOptionTips, "btn-profile-based-option-tips");
            y += 36f;
            wnd.AddText2(x + 2f, y, tab1, I18NKeys.DefaultProfileName, 15, "text-default-profile-name");
            y += 24f;
            wnd.AddInputField(x + 2f, y, 200f, tab1, GamePatch.DefaultProfileName, 15, "input-profile-save-folder");
            y += 18f;
        }
        y += 36f;
        wnd.AddButton(x, y, 200f, tab1, I18NKeys.ShowRecentMilkywayUploadResults, 16, "button-show-recent-milkyway-upload-results", () => UIFunctions.ShowRecentMilkywayUploadResults());
        if (!BulletTimeWrapper.HasBulletTime)
        {
            y += 36f;
            txt = wnd.AddText2(x + 2f, y, tab1, I18NKeys.LogicalFrameRate, 15, "game-frame-rate");
            x += txt.preferredWidth + 7f;
            wnd.AddSlider(x, y + 6f, tab1, GamePatch.GameUpsFactor, new UpsMapper(), "0.0x", 100f).WithSmallerHandle();
            var btn = wnd.AddFlatButton(x + 104f, y + 6f, tab1, I18NKeys.Reset, 13, "reset-game-frame-rate", () => GamePatch.GameUpsFactor.Value = 1.0f);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(40f, 20f);
            x = 0f;
        }
        y += 36f;
        txt = wnd.AddText2(x + 2f, y, tab1, I18NKeys.ProcessPriority, 15, "process-priority");
        wnd.AddComboBox(x + 7f + txt.preferredWidth, y, tab1).WithItems(I18NKeys.High, I18NKeys.AboveNormal, I18NKeys.Normal, I18NKeys.BelowNormal, I18NKeys.Idle).WithSize(100f, 0f).WithConfigEntry(WindowFunctions.ProcessPriority);

        var tab2 = wnd.AddTab(trans, I18NKeys.Factory);
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.RemoveSomeConditionEnabled, I18NKeys.RemoveSomeBuildConditions);
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.RemoveBuildRangeLimitEnabled, I18NKeys.RemoveBuildRangeLimit);
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.NightLightEnabled, I18NKeys.NightLight);
        x += checkBoxForMeasureTextWidth.Width + 5f + 10f;
        txt = wnd.AddText2(x, y + 2f, tab2, I18NKeys.AngleX, 13, "text-nightlight-angle-x");
        x += txt.preferredWidth + 5f;
        wnd.AddSlider(x, y + 7f, tab2, FactoryConfigProvider.NightLightAngleX, new AngleMapper(), "0", 60f).WithSmallerHandle();
        x += 70f;
        txt = wnd.AddText2(x, y + 2f, tab2, "Y:", 13, "text-nightlight-angle-y");
        wnd.AddSlider(x + txt.preferredWidth + 5f, y + 7f, tab2, FactoryConfigProvider.NightLightAngleY, new AngleMapper(), "0", 60f).WithSmallerHandle();
        x = 0;
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.LargerAreaForUpgradeAndDismantleEnabled, I18NKeys.LargerAreaForUpgradeAndDismantle);
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.LargerAreaForTerraformEnabled, I18NKeys.LargerAreaForTerraform);
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.OffGridBuildingEnabled, I18NKeys.OffGridBuildingAndSteppedRotation);
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.CutConveyorBeltEnabled, I18NKeys.CutConveyorBeltWithShortcutKey);
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.TreatStackingAsSingleEnabled, I18NKeys.TreatStackItemsAsSingleInMonitorComponents);
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.QuickBuildAndDismantleLabsEnabled, I18NKeys.QuickBuildAndDismantleStackingLabs);

        {
            y += 36f;
            var cb = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.TankFastFillInAndTakeOutEnabled, I18NKeys.FastFillInToAndTakeOutFromTanks);
            x += cb.Width + 5f;
            txt = wnd.AddText2(x, y + 2f, tab2, I18NKeys.SpeedRatio, 13, "text-tank-fast-fill-speed-ratio");
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
        {
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.DoNotRenderEntitiesEnabled, I18NKeys.DoNotRenderFactoryEntities);
            y += 27f;
            var hideSortersCheckBox = wnd.AddCheckBox(x + 20f, y, tab2, FactoryConfigProvider.DoNotRenderEntitiesHideSortersEnabled, I18NKeys.HideSortersToo, 13);
            FactoryConfigProvider.DoNotRenderEntitiesEnabled.SettingChanged += DoNotRenderEntitiesEnabledChanged;
            wnd.OnFree += () => { FactoryConfigProvider.DoNotRenderEntitiesEnabled.SettingChanged -= DoNotRenderEntitiesEnabledChanged; };
            DoNotRenderEntitiesEnabledChanged(null, null);

            void DoNotRenderEntitiesEnabledChanged(object o, EventArgs e)
            {
                hideSortersCheckBox.SetEnable(FactoryConfigProvider.DoNotRenderEntitiesEnabled.Value);
            }
        }
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.ShortcutKeysForBlueprintCopyEnabled, I18NKeys.ShortcutKeysForBlueprintCopyMode);
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab2, I18NKeys.ShortcutKeysForBlueprintCopyMode, I18NKeys.ShortcutKeysForBlueprintCopyModeTips, "shortcut-keys-for-blueprint-copy-mode-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.BeltSignalsForBuyOutEnabled, I18NKeys.BeltSignalsForBuyOutDarkFogItemsAutomatically);

        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.ProtectVeinsFromExhaustionEnabled, I18NKeys.ProtectVeinsFromExhaustion);
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab2, I18NKeys.ProtectVeinsFromExhaustion, I18NKeys.ProtectVeinsFromExhaustionTips, "protect-veins-tips");
        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.DragBuildPowerPolesEnabled, I18NKeys.DragBuildingPowerPolesInMaximumConnectionRange);
            y += 27f;
            var alternatelyCheckBox = wnd.AddCheckBox(x + 20f, y, tab2, FactoryConfigProvider.DragBuildPowerPolesAlternatelyEnabled, I18NKeys.BuildTeslaTowerAndWirelessPowerTowerAlternately, 13);
            FactoryConfigProvider.DragBuildPowerPolesEnabled.SettingChanged += AlternatelyCheckBoxChanged;
            wnd.OnFree += () => { FactoryConfigProvider.DragBuildPowerPolesEnabled.SettingChanged -= AlternatelyCheckBoxChanged; };
            AlternatelyCheckBoxChanged(null, null);

            void AlternatelyCheckBoxChanged(object o, EventArgs e)
            {
                alternatelyCheckBox.SetEnable(FactoryConfigProvider.DragBuildPowerPolesEnabled.Value);
            }
        }

        y += 36f;
        wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.AutoConstructButtonEnabled, I18NKeys.AutoConstructButton);

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.PressShiftToTakeWholeBeltItemsEnabled, I18NKeys.CtrlShiftClickToPickItemsFromWholeBelts);
            y += 27f;
            var includeBranches = wnd.AddCheckBox(x + 10, y, tab2, FactoryConfigProvider.PressShiftToTakeWholeBeltItemsIncludeBranches, I18NKeys.IncludeBranchesOfBelts, 13);
            y += 27f;
            var includeInserters = wnd.AddCheckBox(x + 10, y, tab2, FactoryConfigProvider.PressShiftToTakeWholeBeltItemsIncludeInserters, I18NKeys.IncludeConnectedInserters, 13);
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
        wnd.AddButton(x, y, tab2, I18NKeys.InitializeThisPlanet, 16, "button-init-planet", () =>
            UIMessageBox.Show(I18NKeys.InitializeThisPlanet.Translate(), I18NKeys.InitializeThisPlanetConfirm.Translate(), I18NKeys.Cancel.Translate(), I18NKeys.OK.Translate(), UIMessageBox.QUESTION, null,
                () => { PlanetFunctions.RecreatePlanet(true); })
        );
        y += 24f;
        wnd.AddCheckBox(x + 10f, y, tab2, PlanetFunctions.ReturnBuildingsOnInitializeEnabled, I18NKeys.ReturnBuildingsToPlayerWhenInitializingPlanet, 13);
        y += 24f;
        wnd.AddCheckBox(x + 10f, y, tab2, PlanetFunctions.ReturnLogisticStorageItemsOnInitializeEnabled, I18NKeys.ReturnLogisticStorageItemsToPlayerWhenInitializingPlanet, 13);
        y += 24f;
        wnd.AddCheckBox(x + 10f, y, tab2, PlanetFunctions.ReturnBeltAFactoryItemsOnInitializeEnabled, I18NKeys.ReturnBeltAndFactoryItemsToPlayerWhenInitializingPlanet, 13);

        y += 36f;
        wnd.AddButton(x, y, tab2, I18NKeys.DismantleAllBuildings, 16, "button-dismantle-all", () =>
            UIMessageBox.Show(I18NKeys.DismantleAllBuildings.Translate(), I18NKeys.DismantleAllBuildingsConfirm.Translate(), I18NKeys.Cancel.Translate(), I18NKeys.OK.Translate(), UIMessageBox.QUESTION, null,
                () => { PlanetFunctions.DismantleAll(false); })
        );
        y += 72f;
        wnd.AddButton(x, y, 200, tab2, I18NKeys.QuickBuildOrbitalCollectors, 16, "button-init-planet", PlanetFunctions.BuildOrbitalCollectors);
        y += 30f;
        txt = wnd.AddText2(x + 10f, y, tab2, I18NKeys.MaximumCountToBuild, 15, "text-oc-build-count");
        wnd.AddSlider(x + 10f + txt.preferredWidth + 5f, y + 6f, tab2, PlanetFunctions.OrbitalCollectorMaxBuildCount, new OcMapper(), "G", 160f);

        y += 18f;

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab2, FactoryConfigProvider.TweakBuildingBufferEnabled, I18NKeys.TweakBuildingBuffers);
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.AssemblerBufferTimeMultiplierInSeconds, 13);
            var nx1 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.AssemblerBufferMinimumMultiplier, 13);
            var nx2 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.BufferCountForAssemblingInLabs, 13);
            var nx3 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.ExtraBufferCountForSelfEvolutionLabs, 13);
            var nx4 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.BufferCountForResearchingInLabs, 13);
            var nx5 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.RayReceiverGravitonLensBufferCount, 13);
            var nx6 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.EjectorSolarSailsBufferCount, 13);
            var nx7 = txt.preferredWidth + 5f;
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab2, I18NKeys.SiloRocketsBufferCount, 13);
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

        var tab3 = wnd.AddTab(trans, I18NKeys.Logistics);
        x = 0f;
        y = 10f;

        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.LogisticsCapacityTweaksEnabled, I18NKeys.EnhanceControlForLogisticStorageCapacities);
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab3, I18NKeys.EnhanceControlForLogisticStorageCapacities, I18NKeys.EnhanceControlForLogisticStorageCapacitiesTips, "enhanced-logistic-capacities-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.AllowOverflowInLogisticsEnabled, I18NKeys.AllowOverflowForLogisticStationsAndAdvancedMiningMachines);
        y += 30f;
        wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.GreaterPowerUsageInLogisticsEnabled, I18NKeys.IncreaseMaximumPowerUsageInLogisticStationsAndAdvancedMiningMachines);
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.LogisticsConstrolPanelImprovementEnabled, I18NKeys.LogisticsControlPanelImprovement);
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab3, I18NKeys.LogisticsControlPanelImprovement, I18NKeys.LogisticsControlPanelImprovementTips, "lcp-improvement-tips");
        {
            y += 36f;
            var realtimeLogisticsInfoPanelCheckBox = wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.RealtimeLogisticsInfoPanelEnabled, I18NKeys.RealTimeLogisticStationsInfoPanel);
            y += 27f;
            var realtimeLogisticsInfoPanelBarsCheckBox = wnd.AddCheckBox(x + 20f, y, tab3, LogisticsConfigProvider.RealtimeLogisticsInfoPanelBarsEnabled, I18NKeys.ShowStatusBarsForStorageItems, 13);
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
        wnd.AddCheckBox(x, y, tab3, LogisticsConfigProvider.AutoConfigLogisticsEnabled, I18NKeys.AutoConfigLogisticStations);
        y += 26f;
        wnd.AddCheckBox(x + 10f, y, tab3, LogisticsConfigProvider.AutoConfigLimitAutoReplenishCount, I18NKeys.LimitAutoReplenishCountToValuesBelow, 13).WithSmallerBox();
        y += 18f;
        wnd.AddCheckBox(x + 10f, y, tab3, LogisticsConfigProvider.SetDefaultRemoteLogicToStorage, I18NKeys.SetDefaultRemoteLogicToStorage, 13).WithSmallerBox();
        y += 16f;
        var maxWidth = 0f;
        wnd.AddText2(10f, y, tab3, I18NKeys.Dispenser, 14, "text-dispenser");
        var dispenserCatY = y;
        y += 18f;
        var oy = y;
        x = 20f;
        var textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MaxChargingPower, 13, "text-dispenser-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.CountOfBotsFilled, 13, "text-dispenser-count-of-bots-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, I18NKeys.BattlefieldAnalysisBase, 14, "text-battlefield-analysis-base");
        var battleBaseCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MaxChargingPower, 13, "text-battlefield-analysis-base-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, I18NKeys.PLS, 14, "text-pls");
        var plsCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MaxChargingPower, 13, "text-pls-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.DroneTransportRange, 13, "text-pls-drone-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MinLoadOfDrones, 13, "text-pls-min-load-of-drones");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.OutgoingIntegrationCount, 13, "text-pls-outgoing-integration-count");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.CountOfDronesFilled, 13, "text-pls-count-of-drones-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, I18NKeys.ILS, 14, "text-ils");
        var ilsCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MaxChargingPower, 13, "text-ils-max-charging-power");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.DroneTransportRange, 13, "text-ils-drone-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.VesselTransportRange, 13, "text-ils-vessel-transport-range");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.WarpDistance, 13, "text-ils-warp-distance");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MinLoadOfDrones, 13, "text-ils-min-load-of-drones");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MinLoadOfVessels, 13, "text-ils-min-load-of-vessels");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.OutgoingIntegrationCount, 13, "text-ils-outgoing-integration-count");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.CountOfDronesFilled, 13, "text-ils-count-of-drones-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.CountOfVesselsFilled, 13, "text-ils-count-of-vessels-filled");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        wnd.AddText2(10f, y, tab3, I18NKeys.AdvancedMiningMachine, 14, "text-amm");
        var ammCatY = y;
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.CollectingSpeed, 13, "text-amm-collecting-speed");
        maxWidth = Mathf.Max(maxWidth, textForMeasureTextWidth.preferredWidth);
        y += 18f;
        textForMeasureTextWidth = wnd.AddText2(x, y, tab3, I18NKeys.MinPilerValue, 13, "text-amm-min-piler-value");
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
            wnd.AddFlatButton(bx, rowY + 6f, tab3, I18NKeys.ApplyConfigToPlanet, applyBtnFontSize, objName, onClick).WithSize(applyBtnWidth, applyBtnHeight)
                .WithFontSize(applyBtnFontSize).WithTip(I18NKeys.ApplyConfigToPlanetTips.Translate());

        void AddApplyButton(float rowY, string objName, UnityAction onClick) => AddApplyButtonAt(applyBtnX, rowY, objName, onClick);

        void AddCategoryApplyButton(float catY, string objName, UnityAction onClick) =>
            wnd.AddFlatButton(applyAllBtnX, catY + 7f, tab3, I18NKeys.ApplyAllConfigToPlanet, applyBtnFontSize, objName, onClick).WithSize(applyAllBtnWidth, applyBtnHeight)
                .WithFontSize(applyBtnFontSize).WithTip(I18NKeys.ApplyAllConfigToPlanetTips.Translate());

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
        var includeOrbitCollectorCheckBox = wnd.AddCheckBox(checkBoxX, y + 4f, tab3, LogisticsConfigProvider.AutoConfigILSIncludeOrbitCollector, I18NKeys.IncludeOrbitalCollector, 13).WithSmallerBox();
        var includeOrbitCollectorY = y;
        y += 18f;
        wnd.AddSideSlider(nx, y, tab3, LogisticsConfigProvider.AutoConfigILSWarperDistance, new AutoConfigILSWarperDistanceMapper(), "G", 150f, -100f).WithFontSize(13);
        AddApplyButton(y, "btn-apply-ils-warp-distance", LogisticsPatch.ApplyILSWarpDistance);
        var warperNecessaryCheckBox = wnd.AddCheckBox(checkBoxX, y + 4f, tab3, LogisticsConfigProvider.AutoConfigILSWarperNecessary, I18NKeys.WarpersRequired, 13).WithSmallerBox();
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

        var tab4 = wnd.AddTab(trans, I18NKeys.PlayerMecha);
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab4, FactoryConfigProvider.UnlimitInteractiveEnabled, I18NKeys.UnlimitedInteractiveRange);
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlanetPatch.PlayerActionsInGlobeViewEnabled, I18NKeys.EnablePlayerActionsInGlobeView);
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlayerPatch.HideTipsForSandsChangesEnabled, I18NKeys.HideTipsForSoilPilesChanges);
        y += 36f;
        checkBoxForMeasureTextWidth = wnd.AddCheckBox(x, y, tab4, PlayerPatch.EnhancedMechaForgeCountControlEnabled, I18NKeys.EnhancedCountControlForHandMake);
        wnd.AddTipsButton2(x + checkBoxForMeasureTextWidth.Width + 5f, y + 6f, tab4, I18NKeys.EnhancedCountControlForHandMake, I18NKeys.EnhancedCountControlForHandMakeTips, "enhanced-count-control-tips");
        y += 36f;
        wnd.AddCheckBox(x, y, tab4, PlayerPatch.ShortcutKeysForStarsNameEnabled, I18NKeys.ShortcutKeysForShowingStarsName);

        {
            y += 36f;
            wnd.AddCheckBox(x, y, tab4, PlayerPatch.AutoNavigationEnabled, I18NKeys.AutoNavigationOnSailings);
            y += 27f;
            var autoCruiseCheckBox = wnd.AddCheckBox(x + 20f, y, tab4, PlayerPatch.AutoCruiseEnabled, "Enable auto-cruise", 13);
            y += 27f;
            var autoBoostCheckBox = wnd.AddCheckBox(x + 20f, y, tab4, PlayerPatch.AutoBoostEnabled, I18NKeys.AutoBoost, 13);
            y += 27f;
            txt = wnd.AddText2(x + 20f, y, tab4, I18NKeys.DistanceToUseWarp, 15, "text-distance-to-warp");
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

        var tab5 = wnd.AddTab(trans, I18NKeys.DysonSphere);
        x = 0f;
        y = 10f;
        wnd.AddCheckBox(x, y, tab5, DysonSpherePatch.StopEjectOnNodeCompleteEnabled, I18NKeys.StopEjectorsWhenAvailableNodesAreAllFilledUp);
        y += 36f;
        wnd.AddCheckBox(x, y, tab5, DysonSpherePatch.OnlyConstructNodesEnabled, I18NKeys.ConstructOnlyStructurePointsButFrames);
        x = 400f;
        y = 10f;
        _dysonInitBtn = wnd.AddButton(x, y, tab5, I18NKeys.InitializeDysonSphere, 16, "init-dyson-sphere", () =>
            UIMessageBox.Show(I18NKeys.InitializeDysonSphere.Translate(), I18NKeys.InitializeDysonSphereConfirm.Translate(), I18NKeys.Cancel.Translate(), I18NKeys.OK.Translate(), UIMessageBox.QUESTION, null,
                () => { DysonSphereFunctions.InitCurrentDysonLayer(null, -1); })
        );
        y += 36f;
        wnd.AddText2(x, y, tab5, I18NKeys.ClickToDismantleSelectedLayer, 16, "text-dismantle-layer");
        y += 27f;
        for (var i = 0; i < 10; i++)
        {
            var id = i + 1;
            var btn = wnd.AddFlatButton(x, y, tab5, id.ToString(), 12, "dismantle-layer-" + id, () =>
                {
                    var star = DysonSphereFunctions.CurrentStarForDysonSystem();
                    UIMessageBox.Show(I18NKeys.DismantleSelectedLayer.Translate(), I18NKeys.DismantleSelectedLayerConfirm.Translate(), I18NKeys.Cancel.Translate(), I18NKeys.OK.Translate(), UIMessageBox.QUESTION, null,
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
        txt = wnd.AddText2(x, y, tab5, I18NKeys.AutoFastBuildSpeedMultiplier, 15, "text-auto-fast-build-multiplier");
        wnd.AddSlider(x + txt.preferredWidth + 5f, y + 6f, tab5, DysonSpherePatch.AutoConstructMultiplier, [1, 2, 5, 10, 20, 50, 100], "0", 100f);
        _dysonTab = tab5;

        var tab6 = wnd.AddTab(trans, I18NKeys.TechCombatUI);
        x = 10;
        y = 10;
        wnd.AddCheckBox(x, y, tab6, UIPatch.PlanetVeinUtilizationEnabled, I18NKeys.PlanetVeinUtilization);
        y += 72f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.BatchBuyoutTechEnabled, I18NKeys.BuyOutTechsWithTheirPrerequisites);
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.SorterCargoStackingEnabled, I18NKeys.RestoreUpgradesOfSorterCargoStackingOnPanel);
        y += 36f;
        wnd.AddCheckBox(x, y, tab6, TechPatch.DisableBattleRelatedTechsInPeaceModeEnabled, I18NKeys.DisableBattleRelatedTechsInPeaceMode);
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, I18NKeys.SetSorterCargoStackingToUnresearchedState, 16, "button-remove-cargo-stacking", TechFunctions.RemoveCargoStackingTechs);
        y += 36f;
        wnd.AddButton(x, y, 300f, tab6, I18NKeys.UnlockAllTechsWithMetadata, 16, "button-unlock-all-techs-with-metadata", TechFunctions.UnlockAllProtoWithMetadataAndPrompt);
        y += 72f;
        wnd.AddButton(x, y, 300f, tab6, I18NKeys.OpenDarkFogCommunicator, 16, "button-open-df-communicator", () =>
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