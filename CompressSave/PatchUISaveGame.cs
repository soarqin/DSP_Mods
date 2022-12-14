using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CompressSave;

static class PatchUISaveGame
{
    [HarmonyPatch(typeof(UISaveGameWindow), "OnSelectedChange"), HarmonyPostfix]
    static void OnSelectedChange(UISaveGameWindow __instance, Text ___prop3Text)
    {
        var compressedType = SaveUtil.SaveGetCompressType(__instance.selected?.saveName);
        switch (compressedType)
        {
            case CompressionType.LZ4:
                ___prop3Text.text = "(LZ4)" + ___prop3Text.text;
                break;
            case CompressionType.Zstd:
                ___prop3Text.text = "(ZSTD)" + ___prop3Text.text;
                break;
            default:
                ___prop3Text.text = "(N)" + ___prop3Text.text;
                break;
        }
    }

    [HarmonyPatch(typeof(UISaveGameWindow), "_OnDestroy"), HarmonyPostfix]
    static void _OnDestroy()
    {
        //Console.WriteLine("OnCreate");
        context = new UIContext();
    }
 
    [HarmonyPatch(typeof(UISaveGameWindow), "OnSaveClick"), HarmonyReversePatch]
    static void OSaveGameAs(this UISaveGameWindow ui, int data) { }

    [HarmonyPatch(typeof(UISaveGameWindow), "CheckAndSetSaveButtonEnable"), HarmonyPostfix]
    static void CheckAndSetSaveButtonEnable(UISaveGameWindow __instance, UIButton ___saveButton, Text ___saveButtonText)
    {
        _OnOpen(__instance, ___saveButton, ___saveButtonText);
        if (context.saveButtonText && context.saveButton)
            SetButtonState(context.saveButtonText.text, context.saveButton.button.interactable);
    }

    static void SetButtonState(string text, bool interactable)
    {
        context.buttonCompress.button.interactable = interactable;
        context.buttonCompressText.text = text;
    }

    class UIContext
    {
        public UIButton buttonCompress;
        public UIButton saveButton;
        public Text buttonCompressText;
        public Text saveButtonText;
        public UISaveGameWindow ui;
    }

    [HarmonyPatch(typeof(UISaveGameWindow), "OnSaveClick"), HarmonyPrefix]
    static void OnSaveClick()
    {
        PatchSave.UseCompressSave = true;
    }

    static UIContext context = new UIContext();

    [HarmonyPatch(typeof(UISaveGameWindow), "_OnOpen"), HarmonyPostfix]
    static void _OnOpen(UISaveGameWindow __instance,  UIButton ___saveButton, Text ___saveButtonText)
    {
        if (!context.buttonCompress)
        {
            context.saveButton = ___saveButton;
            context.saveButtonText = ___saveButtonText;

            context.ui = __instance;
            context.buttonCompress = (__instance.transform.Find("button-compress")?.gameObject??GameObject.Instantiate(___saveButton.gameObject, ___saveButton.transform.parent)).GetComponent<UIButton>();
                
            context.buttonCompress.gameObject.name = "button-compress";
            context.buttonCompress.transform.Translate(new Vector3(-2.0f, 0, 0));
            context.buttonCompress.button.image.color = new Color32(0xfc,0x6f,00,0x77);
            context.buttonCompressText = context.buttonCompress.transform.Find("button-text")?.GetComponent<Text>();

            context.buttonCompress.onClick += __instance.OnSaveClick;
            context.saveButton.onClick -= __instance.OnSaveClick;
            context.saveButton.onClick += WrapClick;
        }
    }

    static void WrapClick(int data)
    {
        PatchSave.UseCompressSave = false;
        context.ui.OSaveGameAs(data);
    }


    public static void OnDestroy()
    {
        if (context.buttonCompress)
            GameObject.Destroy(context.buttonCompress.gameObject);
        if (context.ui)
        {
            context.saveButton.onClick -= WrapClick;
            context.saveButton.onClick += context.ui.OnSaveClick;
        }
        _OnDestroy();
    }
}