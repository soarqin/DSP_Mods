using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CompressSave;

class PatchUILoadGame
{
    static UIButton decompressButton;

    [HarmonyPatch(typeof(UILoadGameWindow), "OnSelectedChange"), HarmonyPostfix]
    static void OnSelectedChange(UILoadGameWindow __instance, Text ___prop3Text)
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
        if (!decompressButton) return;
        decompressButton.button.interactable = compressedType != CompressionType.None;
        decompressButton.gameObject.SetActive(compressedType != CompressionType.None);
    }

    [HarmonyPatch(typeof(UILoadGameWindow), "_OnOpen"), HarmonyPostfix]
    static void _OnOpen(UILoadGameWindow __instance, UIButton ___loadButton, GameObject ___loadSandboxGroup, List<UIGameSaveEntry> ___entries)
    {
        if (!decompressButton)
        {
            decompressButton = ___loadButton;

            decompressButton = (__instance.transform.Find("button-decompress")?.gameObject ?? GameObject.Instantiate(___loadButton.gameObject, ___loadButton.transform.parent)).GetComponent<UIButton>();

            ___loadSandboxGroup.transform.Translate(new Vector3(-2.5f, 0, 0));
            decompressButton.gameObject.name = "button-decompress";
            decompressButton.transform.Translate(new Vector3(-2.0f, 0, 0));
            decompressButton.button.image.color = new Color32(0, 0xf4, 0x92, 0x77);
            var localizer = decompressButton.transform.Find("button-text")?.GetComponent<Localizer>();
            var text = decompressButton.transform.Find("button-text")?.GetComponent<Text>();

            if (localizer)
            {
                localizer.stringKey = "Decompress";
                localizer.translation = "Decompress".Translate();
            }
            if (text)
                text.text = "Decompress".Translate();

            decompressButton.onClick += _ =>{ 
                if(SaveUtil.DecompressSave(__instance.selected.saveName, out var newfileName))
                {
                    __instance.RefreshList();
                    __instance.selected = ___entries.First(e => e.saveName == newfileName);
                }
            };
            decompressButton.button.interactable = false;
            decompressButton.gameObject.SetActive(false);
        }
    }
        
    public static void OnDestroy()
    {
        if (decompressButton)
            GameObject.Destroy(decompressButton.gameObject);
        decompressButton = null;
    }
}