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
    private static void OnSelectedChange(UISaveGameWindow __instance)
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
        if (_context.SaveButton)
            _context.ButtonCompress.button.interactable = _context.SaveButton.button.interactable;
    }

    private class UIContext
    {
        public UIButton ButtonCompress;
        public UIButton SaveButton;
        public GameObject ManualSaveTypeComboBox;
        public GameObject AutoSaveTypeComboBox;
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
        RectTransform rtrans;
        Vector3 pos;
        _context.SaveButton = __instance.saveButton;
        _context.SaveButtonText = __instance.saveButtonText;
        _context.Window = __instance;
        var gameObj = __instance.transform.Find("button-compress")?.gameObject;
        var created = false;
        if (gameObj == null)
        {
            gameObj = Object.Instantiate(__instance.saveButton.gameObject, __instance.saveButton.transform.parent);
            created = true;
        }
        _context.ButtonCompress = gameObj.GetComponent<UIButton>();
        if (created)
        {
            _context.ButtonCompress.gameObject.name = "button-compress";
            rtrans = (RectTransform)_context.ButtonCompress.transform;
            pos = rtrans.anchoredPosition3D;
            rtrans.anchoredPosition3D = new Vector3(pos.x - 180, pos.y, pos.z);
            _context.ButtonCompress.button.image.color = new Color32(0xfc, 0x6f, 00, 0x77);
            var textTrans = _context.ButtonCompress.transform.Find("button-text");
            _context.ButtonCompressText = textTrans.GetComponent<Text>();
            _context.ButtonCompress.onClick += __instance.OnSaveClick;
            _context.SaveButton.onClick -= __instance.OnSaveClick;
            _context.SaveButton.onClick += WrapClick;
            _context.ButtonCompressText.text = "Save with Compression".Translate();
            var localizer = textTrans.GetComponent<Localizer>();
            if (localizer)
            {
                localizer.stringKey = "Save with Compression";
                localizer.translation = "Save with Compression".Translate();
            }
        }

        created = false;
        gameObj = __instance.transform.Find("manual-save-type-combobox")?.gameObject;
        if (gameObj == null)
        {
            gameObj = Object.Instantiate(UIRoot.instance.optionWindow.resolutionComp.transform.parent.gameObject, __instance.saveButton.transform.parent);
            created = true;
        }
        _context.ManualSaveTypeComboBox = gameObj;
        if (created)
        {
            gameObj.name = "manual-save-type-combobox";
            rtrans = (RectTransform)gameObj.transform;
            var rtrans2 = (RectTransform)_context.ButtonCompress.transform;
            pos = rtrans2.anchoredPosition3D;
            rtrans.anchorMin = rtrans2.anchorMin;
            rtrans.anchorMax = rtrans2.anchorMax;
            rtrans.pivot = rtrans2.pivot;
            rtrans.anchoredPosition3D = new Vector3(pos.x + 100, pos.y + 45, pos.z);
            var cbctrl = rtrans.transform.Find("ComboBox");
            var content = cbctrl.Find("Dropdown List ScrollBox")?.Find("Mask")?.Find("Content Panel");
            if (content != null)
            {
                for (var i = content.childCount - 1; i >= 0; i--)
                {
                    var theTrans = content.GetChild(i);
                    if (theTrans.name == "Item Button(Clone)")
                    {
                        Object.Destroy(theTrans.gameObject);
                    }
                }
            }
            var cb = cbctrl.GetComponent<UIComboBox>();
            cb.onSubmit.RemoveAllListeners();
            cb.onItemIndexChange.RemoveAllListeners();
            cb.Items = ["Store".Translate(), "LZ4", "Zstd"];
            cb.itemIndex = (int)PatchSave.CompressionTypeForSaves;
            cb.onItemIndexChange.AddListener(()=>
            {
                PatchSave.CompressionTypeForSaves = (CompressionType)cb.itemIndex;
                PatchSave.CompressionTypeForSavesConfig.Value = CompressSave.StringFromCompresstionType(PatchSave.CompressionTypeForSaves);
            });
            rtrans = (RectTransform)cb.transform;
            pos = rtrans.anchoredPosition3D;
            rtrans.anchoredPosition3D = new Vector3(pos.x - 50, pos.y, pos.z);
            var size = rtrans.sizeDelta;
            rtrans.sizeDelta = new Vector2(150f, size.y);
            var txt = gameObj.GetComponent<Text>();
            txt.text = "Compression for manual saves".Translate();
            var localizer = gameObj.GetComponent<Localizer>();
            if (localizer != null)
            {
                localizer.stringKey = "Compression for manual saves";
                localizer.translation = "Compression for manual saves".Translate();
            }
        }

        created = false;
        gameObj = __instance.transform.Find("auto-save-type-combobox")?.gameObject;
        if (gameObj == null)
        {
            gameObj = Object.Instantiate(UIRoot.instance.optionWindow.resolutionComp.transform.parent.gameObject, __instance.saveButton.transform.parent);
            created = true;
        }
        _context.AutoSaveTypeComboBox = gameObj;
        if (created)
        {
            gameObj.name = "auto-save-type-combobox";
            rtrans = (RectTransform)gameObj.transform;
            var rtrans2 = (RectTransform)_context.ButtonCompress.transform;
            pos = rtrans2.anchoredPosition3D;
            rtrans.anchorMin = rtrans2.anchorMin;
            rtrans.anchorMax = rtrans2.anchorMax;
            rtrans.pivot = rtrans2.pivot;
            rtrans.anchoredPosition3D = new Vector3(pos.x + 510, pos.y + 45, pos.z);
            var cbctrl = rtrans.transform.Find("ComboBox");
            var content = cbctrl.Find("Dropdown List ScrollBox")?.Find("Mask")?.Find("Content Panel");
            if (content != null)
            {
                for (var i = content.childCount - 1; i >= 0; i--)
                {
                    var theTrans = content.GetChild(i);
                    if (theTrans.name == "Item Button(Clone)")
                    {
                        Object.Destroy(theTrans.gameObject);
                    }
                }
            }
            var cb = cbctrl.GetComponent<UIComboBox>();
            cb.onSubmit.RemoveAllListeners();
            cb.onItemIndexChange.RemoveAllListeners();
            cb.Items = ["已停用".Translate(), "Store".Translate(), "LZ4", "Zstd"];
            cb.itemIndex = PatchSave.EnableForAutoSaves.Value ? (int)PatchSave.CompressionTypeForAutoSaves + 1 : 0;
            cb.onItemIndexChange.AddListener(() =>
            {
                var idx = cb.itemIndex;
                if (idx == 0)
                {
                    PatchSave.EnableForAutoSaves.Value = false;
                }
                else
                {
                    PatchSave.EnableForAutoSaves.Value = true;
                    PatchSave.CompressionTypeForAutoSaves = (CompressionType)idx - 1;
                    PatchSave.CompressionTypeForAutoSavesConfig.Value = CompressSave.StringFromCompresstionType(PatchSave.CompressionTypeForAutoSaves);
                }
            });
            rtrans = (RectTransform)cb.transform;
            pos = rtrans.anchoredPosition3D;
            rtrans.anchoredPosition3D = new Vector3(pos.x - 50, pos.y, pos.z);
            var size = rtrans.sizeDelta;
            rtrans.sizeDelta = new Vector2(150f, size.y);
            var txt = gameObj.GetComponent<Text>();
            txt.text = "Compression for auto saves".Translate();
            var localizer = gameObj.GetComponent<Localizer>();
            if (localizer != null)
            {
                localizer.stringKey = "Compression for auto saves";
                localizer.translation = "Compression for auto saves".Translate();
            }
        }
    }

    private static void WrapClick(int data)
    {
        PatchSave.UseCompressSave = false;
        _context.Window.OSaveGameAs(data);
    }
}
