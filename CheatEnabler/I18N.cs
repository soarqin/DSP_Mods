using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace CheatEnabler;

public class I18N
{
    private static bool _initialized;

    public static Action OnInitialized;

    public static void Init()
    {
        Harmony.CreateAndPatchAll(typeof(I18N));
    }
    private static int _nextID = 1;
    private static readonly List<StringProto> StringsToAdd = new();
    public static void Add(string key, string enus, string zhcn = null, string frfr = null)
    {
        var strings = LDB._strings;
        var strProto = new StringProto
        {
            Name = key,
            SID = "",
            ENUS = enus,
            ZHCN = string.IsNullOrEmpty(zhcn) ? enus : zhcn,
            FRFR = string.IsNullOrEmpty(frfr) ? enus : frfr
        };
        if (_initialized)
        {
            var index = strings.dataArray.Length;
            strProto.ID = GetNextID();
            strings.dataArray = strings.dataArray.Append(strProto).ToArray();
            strings.dataIndices[strProto.ID] = index;
            strings.nameIndices[strProto.Name] = index;
        }
        else
        {
            StringsToAdd.Add(strProto);
        }
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
        var strings = LDB._strings;
        var index = strings.dataArray.Length;
        strings.dataArray = strings.dataArray.Concat(StringsToAdd).ToArray();
        StringsToAdd.Clear();
        var newIndex = strings.dataArray.Length;
        for (; index < newIndex; index++)
        {
            var strProto = strings.dataArray[index];
            strProto.ID = GetNextID();
            strings.dataIndices[strProto.ID] = index;
            strings.nameIndices[strings.dataArray[index].Name] = index;
        }
        OnInitialized?.Invoke();
    }

    private static int GetNextID()
    {
        var strings = LDB._strings;
        while (_nextID <= 12000)
        {
            if (!strings.dataIndices.ContainsKey(_nextID))
            {
                break;
            }

            _nextID++;
        }

        var result = _nextID;
        _nextID++;
        return result;
    }
}