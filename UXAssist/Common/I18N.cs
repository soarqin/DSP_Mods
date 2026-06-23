using System;
using System.Collections.Generic;
using HarmonyLib;

namespace UXAssist.Common;

/// <summary>
/// Provides bilingual localization helpers that integrate with the game's <see cref="Localization"/> system.
/// </summary>
public static class I18N
{
    private static bool _initialized;
    private static bool _dirty;

    /// <summary>
    /// Invoked after the game has finished loading the active language and all registered strings have been applied.
    /// </summary>
    public static Action OnInitialized;

    /// <summary>
    /// Registers the localization hooks with Harmony.
    /// </summary>
    public static void Init()
    {
        Harmony.CreateAndPatchAll(typeof(I18N));
    }

    /// <summary>
    /// Returns whether the localization system has finished its initial load.
    /// </summary>
    /// <returns><c>true</c> if localization is initialized; otherwise <c>false</c>.</returns>
    public static bool Initialized() => _initialized;

    /// <summary>
    /// Holds the English and Simplified Chinese localized values for a single entry.
    /// </summary>
    private sealed class LocalizedString
    {
        public string En { get; }
        public string Zh { get; }

        public LocalizedString(string en, string zh)
        {
            En = en;
            Zh = zh;
        }
    }

    private static readonly Dictionary<string, LocalizedString> Entries = new();

    /// <summary>
    /// Registers a localized string pair.
    /// </summary>
    /// <param name="key">The lookup key.</param>
    /// <param name="en">The English text.</param>
    /// <param name="zh">The Simplified Chinese text.</param>
    public static void Add(string key, string en, string zh)
    {
        if (zh is null && key == en) return;
        Entries[key] = new LocalizedString(en, string.IsNullOrEmpty(zh) ? en : zh);
        _dirty = true;
    }

    private static void ApplyIndexers()
    {
        var indexer = Localization.namesIndexer;
        var index = indexer.Count;
        foreach (var key in Entries.Keys)
        {
            if (indexer.TryGetValue(key, out _)) continue;
            indexer[key] = index;
            index++;
        }
        _dirty = false;
        var strings = Localization.strings;
        if (strings == null) return;
        var len = strings.Length;
        for (var i = 0; i < len; i++)
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

        var isChinese = Localization.Languages[index].lcId == 2052;
        foreach (var entry in Entries)
        {
            if (!Localization.namesIndexer.TryGetValue(entry.Key, out var idx)) continue;
            strs[idx] = isChinese ? entry.Value.Zh : entry.Value.En;
        }
    }

    /// <summary>
    /// Applies any pending localization changes to the game's localization tables.
    /// </summary>
    public static void Apply()
    {
        if (!_initialized) return;
        if (Entries.Count == 0) return;
        if (!_dirty) return;
        ApplyIndexers();
    }

    /// <summary>
    /// Returns the localized text for the given key in the currently active language,
    /// falling back to the key itself when no entry is registered.
    /// </summary>
    /// <param name="key">The lookup key.</param>
    /// <returns>The localized text, or <paramref name="key"/> if no entry exists.</returns>
    public static string Translate(string key)
    {
        if (!Entries.TryGetValue(key, out var loc)) return key;
        var languageIndex = Localization.currentLanguageIndex;
        if (languageIndex >= 0 && languageIndex < Localization.Languages.Length && Localization.Languages[languageIndex].lcId == 2052)
            return loc.Zh;
        return loc.En;
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

    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(Localization), nameof(Localization.NotifyLanguageChange))]
    private static void Localization_NotifyLanguageChange_Postfix()
    {
        if (!_initialized) return;
        OnInitialized?.Invoke();
    }
}
