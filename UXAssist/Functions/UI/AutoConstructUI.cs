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

    public static void Init()
    {
        I18N.Add("Enable auto-construct", "Enable auto-construct", "启用自动建造");
        I18N.Add("Disable auto-construct", "Disable auto-construct", "禁用自动建造");
        I18N.Add("Buildings to construct: {0}", "Buildings to construct: {0}", "待建造数量: {0}");
    }

    public static void Start()
    {
    }

    public static void Uninit()
    {
    }

    public static void OnInputUpdate()
    {
    }

    public static void OnUpdate()
    {
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
        ConstructCountText.text = String.Format("Buildings to construct: {0}".Translate(), 0);
        ConstructCountText.color = new Color(1f, 1f, 1f, 0.4f);
        ConstructCountText.alignment = TextAnchor.MiddleLeft;
        ConstructCountText.fontSize = 16;
        rectTrans = ConstructCountText.rectTransform;
        rectTrans.SetParent(ConstructCountPanel.transform);
        rectTrans.sizeDelta = new Vector2(150f, 20f);
        rectTrans.anchorMax = new Vector2(0.5f, 0.5f);
        rectTrans.anchorMin = new Vector2(0.5f, 0.5f);
        rectTrans.pivot = new Vector2(0.5f, 0.5f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 0f, 0f);
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
        var active = localPlanet != null && localPlanet.factoryLoaded && localPlanet.factory.prebuildCount > 0 && Patches.Factory.FactoryPatch.AutoConstructButtonEnabled.Value;
        ToggleAutoConstruct.gameObject.SetActive(active);
        ConstructCountPanel.gameObject.SetActive(active);
    }

    public static void UpdateConstructCountText(int count)
    {
        if (ConstructCountText == null) return;
        ConstructCountText.text = String.Format("Buildings to construct: {0}".Translate(), count);
    }
}
