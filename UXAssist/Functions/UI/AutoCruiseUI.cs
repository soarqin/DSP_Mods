using UnityEngine;
using UXAssist.Common;
using UXAssist.UI;

namespace UXAssist.Functions.UI;

internal static class AutoCruiseUI
{
    public static MyCheckButton ToggleAutoCruise;

    public static void Init()
    {
        I18N.Add("Enable auto-cruise", "Enable auto-cruise", "启用自动巡航");
        I18N.Add("Disable auto-cruise", "Disable auto-cruise", "禁用自动巡航");
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

    public static void InitToggleAutoCruiseCheckButton()
    {
        var lowGroup = GameObject.Find("UI Root/Overlay Canvas/In Game/Low Group");
        ToggleAutoCruise = MyCheckButton.CreateCheckButton(0, 0, lowGroup.GetComponent<RectTransform>(), Patches.PlayerPatch.AutoCruiseEnabled).WithSize(160f, 40f);
        var rectTrans = ToggleAutoCruise.rectTrans;
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 185f, 0f);
        rectTrans.localScale = new Vector3(1f, 1f, 1f);

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
}
