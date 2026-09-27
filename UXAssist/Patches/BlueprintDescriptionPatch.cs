using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Production;
using UXAssist.UI;
using Util = UXAssist.UI.Util;

namespace UXAssist.Patches;

public class BlueprintDescriptionPatch : PatchImpl<BlueprintDescriptionPatch>
{
    private static readonly Dictionary<UIBlueprintInspector, InspectorControls> Inspectors = new();

    public static void Start() => Enable(true);

    public static void Uninit() => Enable(false);

    protected override void OnDisable()
    {
        foreach (var controls in Inspectors.Values) controls.Destroy();
        Inspectors.Clear();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), "_OnCreate")]
    private static void OnCreate(UIBlueprintInspector __instance)
    {
        GetControls(__instance);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), "_OnOpen")]
    private static void OnOpen(UIBlueprintInspector __instance)
    {
        GetControls(__instance)?.ResetSession();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), "_OnClose")]
    private static void OnClose(UIBlueprintInspector __instance)
    {
        if (Inspectors.TryGetValue(__instance, out var controls)) controls.ResetSession();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), "_OnFree")]
    private static void OnFree(UIBlueprintInspector __instance)
    {
        if (!Inspectors.TryGetValue(__instance, out var controls)) return;
        controls.Destroy();
        Inspectors.Remove(__instance);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), "_OnDestroy")]
    private static void OnDestroy(UIBlueprintInspector __instance)
    {
        OnFree(__instance);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector.Refresh))]
    private static void Refresh(UIBlueprintInspector __instance)
    {
        try
        {
            GetControls(__instance)?.Refresh();
        }
        catch (Exception exception)
        {
            UXAssist.Logger.LogError($"Blueprint description inspector refresh failed: {exception}");
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector.OnLanguageChanged))]
    private static void OnLanguageChanged(UIBlueprintInspector __instance)
    {
        try
        {
            if (Inspectors.TryGetValue(__instance, out var controls)) controls.UpdateLabels();
        }
        catch (Exception exception)
        {
            UXAssist.Logger.LogError($"Blueprint description localization refresh failed: {exception}");
        }
    }

    private static InspectorControls GetControls(UIBlueprintInspector inspector)
    {
        if (!inspector) return null;
        if (Inspectors.TryGetValue(inspector, out var controls)) return controls;
        try
        {
            controls = new InspectorControls(inspector);
            Inspectors.Add(inspector, controls);
            return controls;
        }
        catch (Exception exception)
        {
            UXAssist.Logger.LogError($"Blueprint description controls could not be created: {exception}");
            return null;
        }
    }

    private sealed class InspectorControls
    {
        private const float Left = 22f;
        private const float ControlsGap = 12f;

        private readonly UIBlueprintInspector _inspector;
        private readonly float _controlsHeight;
        private UIButton _button;
        private Text _buttonText;
        private MyCheckBox _checkbox;
        private BlueprintData _currentBlueprint;
        private bool _manualProliferation;
        private bool _layoutApplied;

        public InspectorControls(UIBlueprintInspector inspector)
        {
            _inspector = inspector;
            try
            {
                _button = LayoutHelper.AddButton(Left, 0, inspector.group1,
                    I18NKeys.BlueprintAutoFillDescription, 15, "blueprint-auto-fill-description", Generate);
                _buttonText = _button.transform.Find("button-text").GetComponent<Text>();
                _checkbox = MyCheckBox.CreateCheckBox(Left, 0, inspector.group1, false,
                    I18NKeys.BlueprintProliferation.Translate(), 15).WithSmallerBox(22f);
                _checkbox.gameObject.name = "blueprint-proliferation";
                _checkbox.OnChecked += OnProliferationChecked;
                UpdateLabels();
                _controlsHeight = Math.Max(_button.transform.GetComponent<RectTransform>().rect.height,
                    _checkbox.rectTrans.rect.height) + 10f;
            }
            catch
            {
                if (_button) UnityEngine.Object.Destroy(_button.gameObject);
                if (_checkbox) UnityEngine.Object.Destroy(_checkbox.gameObject);
                throw;
            }
        }

        public void ResetSession()
        {
            _currentBlueprint = null;
            _manualProliferation = false;
            if (_inspector.blueprint != null) SyncDefault();
            else _checkbox.Checked = false;
        }

        public void Refresh()
        {
            SyncDefault();
            if (!_layoutApplied)
                _inspector.group1.anchoredPosition += new Vector2(0, -_controlsHeight);
            PositionControls();
            _inspector.group2.anchoredPosition += new Vector2(0, -_controlsHeight);
            _inspector.group3.anchoredPosition += new Vector2(0, -_controlsHeight);
            _inspector.group4.anchoredPosition += new Vector2(0, -_controlsHeight);
            _inspector.contentTrans.sizeDelta += new Vector2(0, _controlsHeight);
            _layoutApplied = true;
        }

        public void UpdateLabels()
        {
            _buttonText.text = I18NKeys.BlueprintAutoFillDescription.Translate();
            _checkbox.SetLabelText(I18NKeys.BlueprintProliferation.Translate());
            var width = Math.Max(160f, Util.GetPreferredWidth(_buttonText) + 26f);
            _button.transform.GetComponent<RectTransform>().sizeDelta =
                new Vector2(width, _button.transform.GetComponent<RectTransform>().sizeDelta.y);
            if (_layoutApplied) PositionControls();
        }

        public void Destroy()
        {
            if (_layoutApplied && _inspector)
            {
                _inspector.group1.anchoredPosition += new Vector2(0, _controlsHeight);
                _inspector.group2.anchoredPosition += new Vector2(0, _controlsHeight);
                _inspector.group3.anchoredPosition += new Vector2(0, _controlsHeight);
                _inspector.group4.anchoredPosition += new Vector2(0, _controlsHeight);
                _inspector.contentTrans.sizeDelta += new Vector2(0, -_controlsHeight);
                _layoutApplied = false;
            }

            if (_button)
            {
                _button.button.onClick.RemoveListener(Generate);
                UnityEngine.Object.Destroy(_button.gameObject);
            }

            if (_checkbox)
            {
                _checkbox.OnChecked -= OnProliferationChecked;
                UnityEngine.Object.Destroy(_checkbox.gameObject);
            }
        }

        private void PositionControls()
        {
            var buttonRect = _button.transform.GetComponent<RectTransform>();
            var top = 4f - _controlsHeight;
            Util.NormalizeRectWithTopLeft(_button, Left, top, _inspector.group1);
            Util.NormalizeRectWithTopLeft(_checkbox, Left + buttonRect.rect.width + ControlsGap,
                top + (buttonRect.rect.height - _checkbox.rectTrans.rect.height) / 2f, _inspector.group1);
        }

        private void SyncDefault()
        {
            var blueprint = _inspector.blueprint;
            if (!ReferenceEquals(blueprint, _currentBlueprint))
            {
                _currentBlueprint = blueprint;
                _manualProliferation = false;
            }

            if (!_manualProliferation)
            {
                var models = LDB.models;
                _checkbox.Checked = blueprint?.buildings?.Any(building => building != null &&
                    models?.Select(building.modelIndex)?.prefabDesc?.isSpraycoster == true) == true;
            }
        }

        private void OnProliferationChecked()
        {
            _manualProliferation = true;
        }

        private void Generate()
        {
            var blueprint = _inspector.blueprint;
            if (blueprint == null || !blueprint.isValid)
            {
                UIRealtimeTip.Popup(I18NKeys.BlueprintInvalid.Translate());
                return;
            }

            if (blueprint.buildings == null || blueprint.buildings.Length == 0)
            {
                UIRealtimeTip.Popup(I18NKeys.BlueprintEmpty.Translate());
                return;
            }

            var catalog = ProductionCatalogService.Current;
            if (catalog == null)
            {
                UIRealtimeTip.Popup(I18NKeys.BlueprintDataUnavailable.Translate());
                return;
            }

            try
            {
                if (!BlueprintDescriptionFields.IsNativeFormatValid(blueprint.externalFields))
                {
                    UIRealtimeTip.Popup(I18NKeys.BlueprintInvalidFields.Translate());
                    return;
                }

                _inspector.OnCustomFieldEndEdit(null);
                blueprint = _inspector.blueprint;
                var capture = BlueprintSelectionReader.FromBlueprint(blueprint.buildings);
                if (capture.Diagnostics.Any(diagnostic => diagnostic.Code == ProductionDiagnosticCode.InvalidRequest ||
                    diagnostic.Code == ProductionDiagnosticCode.UnknownItem ||
                    diagnostic.Code == ProductionDiagnosticCode.DataNotReady))
                {
                    UIRealtimeTip.Popup(I18NKeys.BlueprintInvalid.Translate());
                    return;
                }

                var report = new FactoryBlackBoxAnalyzer(catalog).Analyze(capture.CreateRequest(_checkbox.Checked));
                if (report.Status == ProductionStatus.Failed || report.Status == ProductionStatus.DataNotReady)
                {
                    UIRealtimeTip.Popup((report.Status == ProductionStatus.DataNotReady
                        ? I18NKeys.BlueprintDataUnavailable : I18NKeys.BlueprintCalculationFailed).Translate());
                    return;
                }

                var labels = new BlueprintDescriptionLabels(I18NKeys.BlueprintFactoryPower.Translate(),
                    I18NKeys.BlueprintLogisticsPower.Translate(), I18NKeys.BlueprintMissing.Translate(),
                    I18NKeys.BlueprintExcess.Translate(), I18NKeys.Unknown.Translate(),
                    I18NKeys.BlueprintKnownPortion.Translate(), I18NKeys.BlueprintInputLine.Translate(),
                    I18NKeys.BlueprintOutputLine.Translate(), I18NKeys.BlueprintResearchLine.Translate());
                var description = BlueprintDescriptionFormatter.Format(report,
                    itemId => LDB.signals.IconTag(itemId, includeName: true), labels);
                if (description.Description.Length > BlueprintDescriptionText.MaximumDescriptionLength)
                {
                    UIRealtimeTip.Popup(I18NKeys.BlueprintDescriptionTooLong.Translate());
                    return;
                }

                var fields = new[]
                {
                    new BlueprintDescriptionField(I18NKeys.BlueprintPower.Translate(),
                        description.Power.Length > 0 ? description.Power : null,
                        I18NKeys.BlueprintPowerEnglish, I18NKeys.BlueprintPowerChinese)
                };
                var result = BlueprintDescriptionFields.TryUpdate(blueprint.externalFields, fields,
                    UIBlueprintInspector.kMaxFieldCount,
                    value => BlueprintData.Validate(blueprint.ExternalFieldEscape(value)),
                    blueprint.ExternalFieldUnescape, out var updated);
                if (result != BlueprintFieldUpdateResult.Updated)
                {
                    UIRealtimeTip.Popup(result == BlueprintFieldUpdateResult.CapacityExceeded
                        ? string.Format(I18NKeys.BlueprintFieldLimit.Translate(), UIBlueprintInspector.kMaxFieldCount)
                        : I18NKeys.BlueprintInvalidFields.Translate());
                    return;
                }

                var previousFields = blueprint.externalFields;
                var previousDescription = blueprint.desc;
                var previousInput = _inspector.descTextInput.text;
                try
                {
                    blueprint.externalFields = updated;
                    blueprint.desc = description.Description;
                    _inspector.descTextInput.text = description.Description;
                    _inspector.Refresh(forModify: true, refreshComponent: false, refreshCode: true);
                }
                catch
                {
                    blueprint.externalFields = previousFields;
                    blueprint.desc = previousDescription;
                    _inspector.descTextInput.text = previousInput;
                    throw;
                }

                if (!report.MaterialComplete || !report.PowerBreakdown.FactoryComplete ||
                    !report.PowerBreakdown.LogisticsComplete)
                    UIRealtimeTip.Popup(I18NKeys.BlueprintPartial.Translate());
            }
            catch (Exception exception)
            {
                UXAssist.Logger.LogError($"Blueprint description generation failed: {exception}");
                UIRealtimeTip.Popup(I18NKeys.BlueprintCalculationFailed.Translate());
            }
        }
    }
}
