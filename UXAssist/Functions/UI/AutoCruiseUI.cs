using UnityEngine;
using UXAssist.Common;
using UXAssist.UI;

namespace UXAssist.Functions.UI;

internal static class AutoCruiseUI
{
    public static MyCheckButton ToggleAutoCruise;

    public static void OnUpdate()
    {
        if (Time.frameCount % 30 == 0)
        {
            Patches.PlayerPatch.AutoNavigation.UpdateUiTip();
            UpdateToggleAutoCruiseCheckButtonVisiblility();
        }
    }

    public static void InitToggleAutoCruiseCheckButton()
    {
        var lowGroup = GameObject.Find("UI Root/Overlay Canvas/In Game/Low Group");
        ToggleAutoCruise = MyCheckButton.CreateCheckButton(0, 0, lowGroup.GetComponent<RectTransform>(), Patches.PlayerPatch.AutoNavigation.IsActive).WithSize(160f, 40f);
        var rectTrans = ToggleAutoCruise.rectTrans;
        rectTrans.anchorMax = new Vector2(0.5f, 0f);
        rectTrans.anchorMin = new Vector2(0.5f, 0f);
        rectTrans.pivot = new Vector2(0.5f, 0f);
        rectTrans.anchoredPosition3D = new Vector3(0f, 185f, 0f);
        rectTrans.localScale = new Vector3(1f, 1f, 1f);

        UpdateToggleAutoCruiseCheckButtonVisiblility();
        ToggleAutoCruise.OnChecked += UpdateToggleAutoCruiseLabel;
        ToggleAutoCruise.OnChecked += Patches.PlayerPatch.AutoNavigation.Toggle;
    }

    public static void UpdateToggleAutoCruiseCheckButtonVisiblility()
    {
        if (ToggleAutoCruise == null) return;
        var active = Patches.PlayerPatch.AutoCruiseEnabled.Value
                     && Patches.PlayerPatch.AutoNavigation.HasNavigationTarget();
        // MyCheckButton.Checked only refreshes the colours; the label lives on the OnChecked
        // handlers. Auto-cruise can also be toggled by its shortcut or stopped automatically on
        // arrival, so the label has to be refreshed here as well or it goes stale.
        ToggleAutoCruise.Checked = Patches.PlayerPatch.AutoNavigation.IsActive;
        UpdateToggleAutoCruiseLabel();
        ToggleAutoCruise.gameObject.SetActive(active);
    }

    private static void UpdateToggleAutoCruiseLabel()
    {
        if (ToggleAutoCruise == null) return;
        ToggleAutoCruise.SetLabelText(ToggleAutoCruise.Checked
            ? I18NKeys.DisableAutoCruise
            : I18NKeys.EnableAutoCruise);
    }
}
