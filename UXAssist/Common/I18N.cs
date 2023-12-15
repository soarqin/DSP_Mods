using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace UXAssist.Common;

public static class I18N
{
    private static bool _initialized;

    public static Action OnInitialized;

    public static void Init()
    {
        Harmony.CreateAndPatchAll(typeof(I18N));
    }

    public static bool Initialized() => _initialized;
    private struct Translation
    {
        public string Key;
        public string English;
        public string Chinese;
    }
    private static readonly List<Translation> StringsToAdd = [];
    public static void Add(string key, string enus, string zhcn = null)
    {
        if (zhcn == null && key == enus) return;
        var strProto = new Translation
        {
            Key = key,
            English = enus,
            Chinese = string.IsNullOrEmpty(zhcn) ? enus : zhcn
        };
        StringsToAdd.Add(strProto);
    }

    public static void Apply()
    {
        if (!_initialized) return;
        var indexer = Localization.namesIndexer;
        var enIdx = -1;
        var zhIdx = -1;
        var llen = 0;
        for (var i = 0; i < Localization.strings.Length; i++)
        {
            switch (Localization.Languages[i].lcId)
            {
                case Localization.LCID_ENUS:
                    if (!Localization.LanguageLoaded(i) && Localization.Loaded)
                    {
                        Localization.LoadLanguage(i);
                    }
                    enIdx = i;
                    break;
                case Localization.LCID_ZHCN:
                    if (!Localization.LanguageLoaded(i) && Localization.Loaded)
                    {
                        Localization.LoadLanguage(i);
                    }
                    zhIdx = i;
                    llen = Localization.strings[i].Length;
                    break;
            }
        }
        var enus = new string[StringsToAdd.Count];
        var zhcn = new string[StringsToAdd.Count];
        for (var i = 0; i < StringsToAdd.Count; i++)
        {
            var str = StringsToAdd[i];
            enus[i] = str.English;
            zhcn[i] = str.Chinese;
            indexer[str.Key] = llen + i;
        }

        Localization.strings[enIdx] = Localization.strings[enIdx].Concat(enus).ToArray();
        if (enIdx == Localization.currentLanguageIndex)
        {
            Localization.currentStrings = Localization.strings[enIdx];
        }
        Localization.strings[zhIdx] = Localization.strings[zhIdx].Concat(zhcn).ToArray();
        if (zhIdx == Localization.currentLanguageIndex)
        {
            Localization.currentStrings = Localization.strings[zhIdx];
        }
        StringsToAdd.Clear();
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        if (_initialized) return;
        _initialized = true;
        if (StringsToAdd.Count == 0)
        {
            OnInitialized?.Invoke();
            return;
        }

        Apply();
        OnInitialized?.Invoke();
    }
}