using System;
using System.Collections.Generic;
using HarmonyLib;

public static class I18N
{
    private static bool _initialized;
    private static bool _dirty;

    public static void Init()
    {
        Harmony.CreateAndPatchAll(typeof(I18N));
    }

    public static bool Initialized() => _initialized;
    private static readonly List<Tuple<string, string, int>> Keys = [];
    private static readonly Dictionary<int, List<string>> Strings = [];

    public static void Add(string key, string enus, string zhcn = null)
    {
        if (zhcn == null && key == enus) return;
        Keys.Add(Tuple.Create(key, enus, -1));
        if (Strings.TryGetValue(2052, out var zhcnList))
        {
            zhcnList.Add(string.IsNullOrEmpty(zhcn) ? enus : zhcn);
        }
        else
        {
            Strings.Add(2052, [string.IsNullOrEmpty(zhcn) ? enus : zhcn]);
        }
        _dirty = true;
    }

    private static void ApplyIndexers()
    {
        var indexer = Localization.namesIndexer;
        var index = indexer.Count;
        var len = Keys.Count;
        for (var i = 0; i < len; i++)
        {
            var (key, def, value) = Keys[i];
            if (value >= 0) continue;
            if (indexer.TryGetValue(key, out var idx))
            {
                Keys[i] = Tuple.Create(key, def, idx);
                continue;
            }
            indexer[key] = index;
            Keys[i] = Tuple.Create(key, def, index);
            index++;
        }
        _dirty = false;
        var strings = Localization.strings;
        if (strings == null) return;
        var len2 = strings.Length;
        for (var i = 0; i < len2; i++)
        {
            ApplyLanguage(i);
            if (i != Localization.currentLanguageIndex) continue;
            Localization.currentStrings = Localization.strings[i];
            Localization.currentFloats = Localization.floats[i];
        }
    }

    private static void ApplyLanguage(int index)
    {
        var indexerLength = Localization.namesIndexer.Count;
        var strs = Localization.strings[index];
        if (strs == null) return;
        if (strs.Length < indexerLength)
        {
            var newStrs = new string[indexerLength];
            Array.Copy(strs, newStrs, strs.Length);
            strs = newStrs;
            Localization.strings[index] = strs;
        }
        var floats = Localization.floats[index];
        if (floats != null)
        {
            if (floats.Length < indexerLength)
            {
                var newFloats = new float[indexerLength];
                Array.Copy(floats, newFloats, floats.Length);
                floats = newFloats;
                Localization.floats[index] = floats;
            }
        }

        var keyLength = Keys.Count;
        if (Strings.TryGetValue(Localization.Languages[index].lcId, out var list))
        {
            for (var j = 0; j < keyLength; j++)
            {
                strs[Keys[j].Item3] = list[j];
            }
        }
        else
        {
            for (var j = 0; j < keyLength; j++)
            {
                strs[Keys[j].Item3] = Keys[j].Item2;
            }
        }
    }

    public static void Apply()
    {
        if (!_initialized) return;
        if (Keys.Count == 0) return;
        if (!_dirty) return;
        ApplyIndexers();
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(Localization), nameof(Localization.LoadSettings))]
    private static void Localization_LoadSettings_Postfix()
    {
        if (_initialized) return;
        _initialized = true;
        Apply();
    }
    
    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(Localization), nameof(Localization.LoadLanguage))]
    private static void Localization_LoadLanguage_Postfix(int index)
    {
        if (!_initialized) return;
        ApplyLanguage(index);
    }
}