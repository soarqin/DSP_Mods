using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CompressSave;

class PatchUILoadGame
{
    static UIButton _decompressButton;

    [HarmonyPatch(typeof(UILoadGameWindow), "OnSelectedChange"), HarmonyPostfix]
    private static void OnSelectedChange(UILoadGameWindow __instance)
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
        if (!_decompressButton) return;
        _decompressButton.button.interactable = compressedType != CompressionType.None;
        _decompressButton.gameObject.SetActive(compressedType != CompressionType.None);
    }

    [HarmonyPatch(typeof(UILoadGameWindow), "_OnOpen"), HarmonyPostfix]
    static void _OnOpen(UILoadGameWindow __instance)
    {
        if (_decompressButton) return;
        var loadButton = __instance.loadButton;

        var created = false;
        var gameObj = __instance.transform.Find("button-decompress")?.gameObject;
        if (gameObj == null)
        {
            gameObj = Object.Instantiate(loadButton.gameObject, loadButton.transform.parent);
            created = true;
        }

        _decompressButton = gameObj.GetComponent<UIButton>();

        if (created)
        {
            var rtrans = (RectTransform)__instance.loadSandboxGroup.transform;
            var anchoredPosition3D = rtrans.anchoredPosition3D;
            _decompressButton.gameObject.name = "button-decompress";
            rtrans = (RectTransform)_decompressButton.transform;
            var pos = anchoredPosition3D;
            anchoredPosition3D = new Vector3(pos.x - 230, pos.y, pos.z);
            rtrans.anchoredPosition3D = anchoredPosition3D;
            _decompressButton.button.image.color = new Color32(0, 0xf4, 0x92, 0x77);
            var textTrans = _decompressButton.transform.Find("button-text");
            var text = textTrans.GetComponent<Text>();
            text.text = "Decompress".Translate();
            var localizer = textTrans.GetComponent<Localizer>();
            if (localizer)
            {
                localizer.stringKey = "Decompress";
                localizer.translation = "Decompress".Translate();
            }

            _decompressButton.onClick += _ =>
            {
                if (!SaveUtil.DecompressSave(__instance.selected.saveName, out var newfileName)) return;
                __instance.RefreshList();
                __instance.selected = __instance.entries.First(e => e.saveName == newfileName);
            };
        }

        _decompressButton.button.interactable = false;
        _decompressButton.gameObject.SetActive(false);
    }
        
    public static void OnDestroy()
    {
        if (_decompressButton)
            Object.Destroy(_decompressButton.gameObject);
        _decompressButton = null;
    }
}