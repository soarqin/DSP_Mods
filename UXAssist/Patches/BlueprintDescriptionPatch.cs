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

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnCreate))]
    private static void OnCreate(UIBlueprintInspector __instance)
    {
        GetControls(__instance);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnOpen))]
    private static void OnOpen(UIBlueprintInspector __instance)
    {
        GetControls(__instance)?.ResetSession();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnClose))]
    private static void OnClose(UIBlueprintInspector __instance)
    {
        if (Inspectors.TryGetValue(__instance, out var controls)) controls.ResetSession();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnFree))]
    private static void OnFree(UIBlueprintInspector __instance)
    {
        if (!Inspectors.TryGetValue(__instance, out var controls)) return;
        controls.Destroy();
        Inspectors.Remove(__instance);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnDestroy))]
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

    [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnUpdate))]
    private static void OnUpdate(UIBlueprintInspector __instance)
    {
        if (Inspectors.TryGetValue(__instance, out var controls)) controls.HandleInput();
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
        private const float Left = 12f;
        private const float ControlsGap = 10f;

        private readonly UIBlueprintInspector _inspector;
        private RectTransform _panel;
        private UIButton _entry;
        private Text _entryText;
        private UIButton _button;
        private Text _buttonText;
        private UIButton _closeButton;
        private Text _title;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Canvas _canvas;
        private readonly RectTransform _viewport;
        private bool _optionsOpen;
        private MyCheckBox _checkbox;
        private MyCornerComboBox _basisCombo;
        private Text _basisLabel;
        private UIButton _basisHelp;
        private BlueprintData _currentBlueprint;
        private bool _manualProliferation;

        public InspectorControls(UIBlueprintInspector inspector)
        {
            _inspector = inspector;
            _canvas = inspector.rectTrans.GetComponentInParent<Canvas>().rootCanvas;
            _viewport = inspector.descGroupTrans.GetComponentInParent<ScrollRect>()?.viewport;
            try
            {
                // The native description has a 22px title row above its editable text.
                _entry = LayoutHelper.AddButton(0, 0, inspector.descGroupTrans,
                    I18NKeys.BlueprintDescriptionOptions, 13, "blueprint-description-options", ToggleOptions);
                _entryText = _entry.transform.Find("button-text").GetComponent<Text>();
                _entry.tips = inspector.tipButton2.tips;
                _entry.tips.topLevel = true;
                Util.NormalizeRectWithTopRight(_entry, 0, 0, inspector.descGroupTrans);

                // Reuse the native background, including its blur material, only while options are open.
                var source = UIRoot.instance.uiGame.inserterWindow.transform.Find("panel-bg").GetComponent<Image>();
                _panel = UnityEngine.Object.Instantiate(source, inspector.rectTrans).rectTransform;
                _panel.gameObject.name = "uxassist-blueprint-description-options";
                for (var i = _panel.childCount - 1; i >= 0; i--)
                {
                    var child = _panel.GetChild(i).gameObject;
                    child.SetActive(false);
                    UnityEngine.Object.Destroy(child);
                }
                _panel.anchorMin = _panel.anchorMax = new Vector2(0, 1);
                _panel.pivot = new Vector2(0, 1);
                _panel.GetComponent<Image>().raycastTarget = true;
                _title = LayoutHelper.AddText(Left, 10, _panel, I18NKeys.BlueprintAutoFillDescription, 16);
                _title.color = _entryText.color;
                var closeSource = UIRoot.instance.uiGame.inserterWindow.transform
                    .Find("panel-bg/btn-box/close-wnd-btn").GetComponent<UIButton>();
                _closeButton = UnityEngine.Object.Instantiate(closeSource, _panel);
                Util.ResetButton(_closeButton);
                // The source's persistent callback closes the original inserter window.
                _closeButton.button.onClick = new Button.ButtonClickedEvent();
                _closeButton.button.onClick.AddListener(CloseOptions);
                Util.NormalizeRectWithTopRight(_closeButton, 0, 0, _panel);
                _button = LayoutHelper.AddButton(0, 0, _panel,
                    I18NKeys.BlueprintGenerateDescription, 15, "blueprint-auto-fill-description", Generate);
                _buttonText = _button.transform.Find("button-text").GetComponent<Text>();
                _checkbox = MyCheckBox.CreateCheckBox(Left, 42, _panel, false,
                    I18NKeys.BlueprintProliferation.Translate(), 15).WithSmallerBox(20f);
                _checkbox.gameObject.name = "blueprint-proliferation";
                _checkbox.OnChecked += OnProliferationChecked;
                _basisLabel = LayoutHelper.AddText(Left, 68, _panel, I18NKeys.BlueprintProductionBasis, 14);
                _basisCombo = MyCornerComboBox.CreateComboBox(0, 68, _panel).WithFontSize(14);
                // The star-panel localizer would replace the stretched width on every enable or language change.
                var localizer = _basisCombo.GetComponent<Localizer>();
                if (localizer)
                {
                    localizer.enabled = false;
                    UnityEngine.Object.Destroy(localizer);
                }
                var combo = _basisCombo.GetComponent<UIComboBox>();
                combo.CanInput = false;
                combo.autoWidth = false;
                combo.ItemHeight = 24;
                Util.NormalizeRectWithMargin(_basisCombo.transform.Find("Main Button"), 0, 0, 0, 0);
                combo.m_Text.alignment = TextAnchor.MiddleLeft;
                combo.m_ListItemRes.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                _basisHelp = LayoutHelper.AddTipsButton(Left, 68, _panel,
                    I18NKeys.BlueprintProductionBasis.Translate(), I18NKeys.BlueprintProductionBasisTip.Translate(), "");
                UpdateLabels();
                _panel.gameObject.SetActive(false);
            }
            catch
            {
                Destroy();
                throw;
            }
        }

        public void ResetSession()
        {
            _currentBlueprint = null;
            _manualProliferation = false;
            _basisCombo.SetIndex(0);
            if (_inspector.blueprint != null) SyncDefault();
            else _checkbox.Checked = false;
            CloseOptions();
            _entry.gameObject.SetActive(_inspector.blueprint != null);
        }

        public void Refresh()
        {
            SyncDefault();
            _entry.gameObject.SetActive(_inspector.blueprint != null);
        }

        private void ToggleOptions()
        {
            if (_optionsOpen)
            {
                CloseOptions();
                return;
            }
            _optionsOpen = true;
            _panel.SetAsLastSibling();
            _panel.gameObject.SetActive(true);
            PositionOptions();
            if (_optionsOpen) Canvas.willRenderCanvases += PositionOptions;
        }

        private void CloseOptions()
        {
            Canvas.willRenderCanvases -= PositionOptions;
            _optionsOpen = false;
            if (_panel) _panel.gameObject.SetActive(false);
        }

        public void HandleInput()
        {
            if (!_optionsOpen) return;
            var combo = _basisCombo.GetComponent<UIComboBox>();
            if (VFInput.escape)
            {
                if (combo.isDroppedDown)
                {
                    combo.isDroppedDown = false;
                    combo.m_DropDownList.gameObject.SetActive(false);
                }
                else CloseOptions();
                VFInput.UseEscape();
                return;
            }
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2)) return;
            var mouse = (Vector2)Input.mousePosition;
            if (RectTransformUtility.RectangleContainsScreenPoint(_panel, mouse, _canvas.worldCamera) ||
                RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_entry.transform, mouse, _canvas.worldCamera) ||
                combo.isDroppedDown && RectTransformUtility.RectangleContainsScreenPoint(combo.m_DropDownList, mouse, _canvas.worldCamera))
                return;
            CloseOptions();
        }

        private Rect BoundsInPopupParent(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in _corners)
            {
                var point = _panel.parent.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void PositionOptions()
        {
            if (!_optionsOpen) return;
            if (!_entry || !_entry.gameObject.activeInHierarchy)
            {
                CloseOptions();
                return;
            }
            var anchor = BoundsInPopupParent((RectTransform)_entry.transform);
            var screen = BoundsInPopupParent((RectTransform)_canvas.transform);
            if (!anchor.Overlaps(screen) || _viewport && !anchor.Overlaps(BoundsInPopupParent(_viewport)))
            {
                CloseOptions();
                return;
            }
            var combo = _basisCombo.GetComponent<UIComboBox>();
            var extra = combo.isDroppedDown ? combo.DropDownCount * combo.ItemHeight + 2 * combo.ItemPadding + 2 : 0;
            var footerTop = 112 + extra;
            _panel.sizeDelta = new Vector2(_panel.sizeDelta.x, footerTop + 30 + Left);
            Util.NormalizeRectWithTopRight(_button, Left, footerTop, _panel);
            if (!TryGetPopupPosition(anchor, screen, _panel.sizeDelta, out var position))
            {
                CloseOptions();
                return;
            }
            var parent = (RectTransform)_panel.parent;
            _panel.anchoredPosition = position - new Vector2(parent.rect.xMin, parent.rect.yMax);
        }

        private static bool TryGetPopupPosition(Rect anchor, Rect screen, Vector2 size, out Vector2 position)
        {
            const float gap = 6;
            const float margin = 8;
            position = default;
            if (size.x > screen.width - margin * 2 || size.y > screen.height - margin * 2) return false;
            var left = Math.Max(screen.xMin + margin,
                Math.Min(anchor.xMax - size.x, screen.xMax - margin - size.x));
            var top = anchor.yMin - gap;
            if (top - size.y < screen.yMin + margin)
                top = anchor.yMax + gap + size.y;
            top = Math.Min(screen.yMax - margin, Math.Max(screen.yMin + margin + size.y, top));
            position = new Vector2(left, top);
            return true;
        }

        public void UpdateLabels()
        {
            _entryText.text = I18NKeys.BlueprintDescriptionOptions.Translate();
            ((RectTransform)_entry.transform).sizeDelta =
                new Vector2(Util.GetPreferredWidth(_entryText) + 16, 20);
            _entry.tips.tipTitle = I18NKeys.BlueprintAutoFillDescription.Translate();
            _entry.tips.tipText = I18NKeys.BlueprintDescriptionOptionsTip.Translate();
            _entry.UpdateTip();
            _title.text = I18NKeys.BlueprintAutoFillDescription.Translate();
            _buttonText.text = I18NKeys.BlueprintGenerateDescription.Translate();
            _checkbox.SetLabelText(I18NKeys.BlueprintProliferation.Translate());
            _basisLabel.text = I18NKeys.BlueprintProductionBasis.Translate();
            var combo = _basisCombo.GetComponent<UIComboBox>();
            var index = Math.Max(0, combo.itemIndex);
            _basisCombo.SetItems(I18NKeys.BlueprintFinalProducts, I18NKeys.BlueprintBottleneckIntermediate);
            _basisHelp.tips.tipTitle = I18NKeys.BlueprintProductionBasis.Translate();
            _basisHelp.tips.tipText = I18NKeys.BlueprintProductionBasisTip.Translate();
            _basisHelp.UpdateTip();
            var width = Math.Max(90f, Util.GetPreferredWidth(_buttonText) + 20f);
            ((RectTransform)_button.transform).sizeDelta = new Vector2(width, 30);
            var labelWidth = Util.GetPreferredWidth(_basisLabel);
            _basisLabel.rectTransform.sizeDelta = new Vector2(labelWidth, 28);
            var helpRect = (RectTransform)_basisHelp.transform;
            Util.NormalizeRectWithTopLeft(_basisHelp, Left + labelWidth + ControlsGap,
                68 + (28 - helpRect.rect.height) / 2, _panel);
            var comboLeft = Left + labelWidth + ControlsGap + helpRect.rect.width + ControlsGap;
            var comboWidth = 180f;
            foreach (var key in new[] { I18NKeys.BlueprintFinalProducts, I18NKeys.BlueprintBottleneckIntermediate })
            {
                combo.m_Text.text = key.Translate();
                comboWidth = Math.Max(comboWidth, Util.GetPreferredWidth(combo.m_Text) + 40);
            }
            _basisCombo.SetIndex(index);
            Util.NormalizeRectWithTopLeft(_basisCombo, comboLeft, 68, _panel);
            _basisCombo.SetSize(comboWidth, 28);
            var popupWidth = Math.Max(320, Math.Max(comboLeft + comboWidth + Left,
                Util.GetPreferredWidth(_title) + Left * 2 + 50));
            _panel.sizeDelta = new Vector2(popupWidth, _panel.sizeDelta.y);
            _title.rectTransform.sizeDelta = new Vector2(popupWidth - Left * 2 - 50, 24);
            PositionOptions();
        }

        public void Destroy()
        {
            CloseOptions();
            if (_entry)
            {
                _entry.button.onClick.RemoveListener(ToggleOptions);
                UnityEngine.Object.Destroy(_entry.gameObject);
            }
            if (_button)
                _button.button.onClick.RemoveListener(Generate);
            if (_closeButton)
                _closeButton.button.onClick.RemoveListener(CloseOptions);
            if (_checkbox)
                _checkbox.OnChecked -= OnProliferationChecked;
            if (!_panel) return;
            _panel.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_panel.gameObject);
        }

        private void SyncDefault()
        {
            var blueprint = _inspector.blueprint;
            if (!ReferenceEquals(blueprint, _currentBlueprint))
            {
                CloseOptions();
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

                var request = capture.CreateRequest(_checkbox.Checked);
                request.ProductionBasis = (BlueprintProductionBasis)_basisCombo.GetComponent<UIComboBox>().itemIndex;
                var report = new FactoryBlackBoxAnalyzer(catalog).Analyze(request);
                if (report.Diagnostics.Any(diagnostic => diagnostic.Code == ProductionDiagnosticCode.BlueprintBasisUnavailable))
                {
                    UIRealtimeTip.Popup(I18NKeys.BlueprintBasisUnavailable.Translate());
                    return;
                }
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

                var powerField = new BlueprintDescriptionField(I18NKeys.BlueprintPower.Translate(),
                    description.Power.Length > 0 ? description.Power : null,
                    I18NKeys.BlueprintPowerEnglish, I18NKeys.BlueprintPowerChinese);
                var result = BlueprintDescriptionFields.TryUpdate(blueprint.externalFields, powerField,
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

                CloseOptions();
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
