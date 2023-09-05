using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace CheatEnabler;

public class I18N
{
    public static void Init()
    {
        Harmony.CreateAndPatchAll(typeof(I18N));
    }
    private static int _nextID = 0;
    private static readonly List<StringProto> StringsToAdd = new();
    public static void Add(string key, string enus, string zhcn = null, string frfr = null)
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
        var strProto = new StringProto
        {
            Name = key,
            ID = _nextID,
            SID = "",
            ENUS = enus,
            ZHCN = string.IsNullOrEmpty(zhcn) ? enus : zhcn,
            FRFR = string.IsNullOrEmpty(frfr) ? enus : frfr
        };
        StringsToAdd.Add(strProto);
        _nextID++;
    }

    private static bool _initialized = false;
    [HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        if (_initialized) return;
        _initialized = true;
        if (StringsToAdd.Count == 0)
        {
            return;
        }
        var strings = LDB._strings;
        var index = strings.dataArray.Length;
        strings.dataArray = strings.dataArray.Concat(StringsToAdd).ToArray();
        StringsToAdd.Clear();
        var newIndex = strings.dataArray.Length;
        for (; index < newIndex; index++)
        {
            strings.dataIndices[strings.dataArray[index].ID] = index;
            strings.nameIndices[strings.dataArray[index].Name] = index;
        }
    }
}