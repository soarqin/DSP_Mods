using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CompressSave;

static class PatchUISaveGame
{
    public static void OnDestroy()
    {
        if (_context.ButtonCompress)
            Object.Destroy(_context.ButtonCompress.gameObject);
        if (_context.Window)
        {
            _context.SaveButton.onClick -= WrapClick;
            _context.SaveButton.onClick += _context.Window.OnSaveClick;
        }
        _OnDestroy();
    }

    [HarmonyPatch(typeof(UISaveGameWindow), "OnSelectedChange"), HarmonyPostfix]
    static void OnSelectedChange(UISaveGameWindow __instance)
    {
        var selected = __instance.selected;
        var compressedType = SaveUtil.SaveGetCompressType(selected == null ? null : selected.saveName);
        var prop3Text = __instance.prop3Text;
        prop3Text.text = compressedType switch
        {
            CompressionType.LZ4 => "(LZ4)" + prop3Text.text,
            CompressionType.Zstd => "(ZSTD)" + prop3Text.text,
            _ => "(N)" + prop3Text.text
        };
    }

    [HarmonyPatch(typeof(UISaveGameWindow), "_OnDestroy"), HarmonyPostfix]
    private static void _OnDestroy()
    {
        //Console.WriteLine("OnCreate");
        _context = new UIContext();
    }
 
    [HarmonyPatch(typeof(UISaveGameWindow), "OnSaveClick"), HarmonyReversePatch]
    private static void OSaveGameAs(this UISaveGameWindow ui, int data) { }

    [HarmonyPatch(typeof(UISaveGameWindow), "CheckAndSetSaveButtonEnable"), HarmonyPostfix]
    private static void CheckAndSetSaveButtonEnable(UISaveGameWindow __instance)
    {
        _OnOpen(__instance);
        if (_context.SaveButtonText && _context.SaveButton)
            SetButtonState(_context.SaveButtonText.text, _context.SaveButton.button.interactable);
    }

    private static void SetButtonState(string text, bool interactable)
    {
        _context.ButtonCompress.button.interactable = interactable;
        _context.ButtonCompressText.text = text;
    }

    private class UIContext
    {
        public UIButton ButtonCompress;
        public UIButton SaveButton;
        public Text ButtonCompressText;
        public Text SaveButtonText;
        public UISaveGameWindow Window;
    }

    [HarmonyPatch(typeof(UISaveGameWindow), "OnSaveClick"), HarmonyPrefix]
    private static void OnSaveClick()
    {
        PatchSave.UseCompressSave = true;
    }

    private static UIContext _context = new UIContext();

    [HarmonyPatch(typeof(UISaveGameWindow), "_OnOpen"), HarmonyPostfix]
    private static void _OnOpen(UISaveGameWindow __instance)
    {
        if (_context.ButtonCompress) return;
        _context.SaveButton = __instance.saveButton;
        _context.SaveButtonText = __instance.saveButtonText;

        _context.Window = __instance;
        var gameObj = __instance.transform.Find("button-compress")?.gameObject;
        if (gameObj == null)
            gameObj = Object.Instantiate(__instance.saveButton.gameObject, __instance.saveButton.transform.parent);
        _context.ButtonCompress = gameObj.GetComponent<UIButton>();

        _context.ButtonCompress.gameObject.name = "button-compress";
        _context.ButtonCompress.transform.Translate(new Vector3(-2.0f, 0, 0));
        _context.ButtonCompress.button.image.color = new Color32(0xfc, 0x6f, 00, 0x77);
        _context.ButtonCompressText = _context.ButtonCompress.transform.Find("button-text")?.GetComponent<Text>();

        _context.ButtonCompress.onClick += __instance.OnSaveClick;
        _context.SaveButton.onClick -= __instance.OnSaveClick;
        _context.SaveButton.onClick += WrapClick;
    }

    private static void WrapClick(int data)
    {
        PatchSave.UseCompressSave = false;
        _context.Window.OSaveGameAs(data);
    }

}