using System;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.UI;

namespace UXAssist.Functions.UI;

internal static class AutoConstructUI
{
    public static MyCheckButton ToggleAutoConstruct;
    public static GameObject ConstructCountPanel;
    public static Text ConstructCountText;

    public static void OnUpdate()
    {
        // Self-healing state sync: the event-driven refreshes (config SettingChanged handlers,
        // patch OnEnable/OnDisable) are one-shot and can be lost, e.g. when an exception thrown
        // by an earlier handler aborts the delegate chain, or when Harmony patch application
        // fails. Periodically reconcile the button visibility and the pending-construction count
        // with the actual game state so a missed event never leaves the UI stuck. This also
        // works while the game is paused, unlike the PlayerAction_Rts.GameTick polling.
        if (Time.frameCount % 30 != 0) return;
        if (ToggleAutoConstruct == null) return;
        UpdateToggleAutoConstructCheckButtonVisiblility();
        var localPlanet = GameMain.localPlanet;
        if (localPlanet == null || !localPlanet.factoryLoaded) return;
        var prebuildCount = localPlanet.factory.prebuildCount;
        UpdateConstructCountText(prebuildCount);
    }

    public static void InitToggleAutoConstructCheckButton()
    {
        var lowGroup = GameObject.Find("UI Root/Overlay Canvas/In Game/Low Group");
        var parent = lowGroup.GetComponent<RectTransform>();
        ToggleAutoConstruct = MyCheckButton.CreateCheckButton(0, 0, parent, Patches.Factory.FactoryPatch.AutoConstructEnabled).WithSize(160f, 40f);
        var rectTrans = ToggleAutoConstruct.rectTrans;
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(-165f, 185f, 0f);
        rectTrans.localScale = new Vector3(1f, 1f, 1f);

        ConstructCountPanel = new GameObject("uxassist-construct-count-panel");
        rectTrans = ConstructCountPanel.AddComponent<RectTransform>();
        rectTrans.SetParent(parent);
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(-165f, 161f, 0f);
        rectTrans.localScale = new Vector3(1f, 1f, 1f);
        rectTrans.sizeDelta = new Vector2(160f, 24f);
        var bg = ConstructCountPanel.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        ConstructCountText = GameObject.Instantiate(UIRoot.instance.uiGame.assemblerWindow.stateText);
        ConstructCountText.gameObject.name = "construct-count-text";
        // The runtime object is only a style source. A Localizer copied along with it would
        // overwrite the formatted count on the next localization refresh.
        var localizer = ConstructCountText.GetComponent<Localizer>();
        if (localizer != null) GameObject.DestroyImmediate(localizer);
        ConstructCountText.text = String.Format(I18NKeys.BuildingsToConstruct0.Translate(), 0);
        ConstructCountText.color = new Color(1f, 1f, 1f, 0.4f);
        ConstructCountText.alignment = TextAnchor.MiddleLeft;
        ConstructCountText.fontSize = 16;
        ConstructCountText.fontStyle = FontStyle.Normal;
        ConstructCountText.horizontalOverflow = HorizontalWrapMode.Overflow;
        ConstructCountText.verticalOverflow = VerticalWrapMode.Overflow;
        rectTrans = ConstructCountText.rectTransform;
        rectTrans.SetParent(ConstructCountPanel.transform);
        rectTrans.sizeDelta = new Vector2(150f, 20f);
        rectTrans.anchorMax = new Vector2(0.5f, 0.5f);
        rectTrans.anchorMin = new Vector2(0.5f, 0.5f);
        rectTrans.pivot = new Vector2(0.5f, 0.5f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 0f, 0f);
        rectTrans.localScale = new Vector3(1f, 1f, 1f);

        UpdateToggleAutoConstructCheckButtonVisiblility();
        ToggleAutoConstruct.OnChecked += UpdateToggleAutoConstructLabel;
    }

    public static void UpdateToggleAutoConstructCheckButtonVisiblility()
    {
        if (ToggleAutoConstruct == null) return;
        var localPlanet = GameMain.localPlanet;
        var active = localPlanet != null && localPlanet.factoryLoaded && localPlanet.factory.prebuildCount > 0 && Patches.Factory.FactoryPatch.AutoConstructButtonEnabled.Value;
        // The config can also change from outside the button (config file, BepInEx config manager),
        // which only refreshes the colours, so reconcile the label here too.
        UpdateToggleAutoConstructLabel();
        ToggleAutoConstruct.gameObject.SetActive(active);
        ConstructCountPanel.SetActive(active);
    }

    private static void UpdateToggleAutoConstructLabel()
    {
        if (ToggleAutoConstruct == null) return;
        ToggleAutoConstruct.SetLabelText(ToggleAutoConstruct.Checked
            ? I18NKeys.DisableAutoConstruct
            : I18NKeys.EnableAutoConstruct);
    }

    public static void UpdateConstructCountText(int count)
    {
        if (ConstructCountText == null) return;
        ConstructCountText.text = String.Format(I18NKeys.BuildingsToConstruct0.Translate(), count);
    }
}
