using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;

namespace UXAssist;

public static class TechPatch
{
    public static ConfigEntry<bool> SorterCargoStackingEnabled;
    
    public static void Init()
    {
        I18N.Add("分拣器运货量", "Sorter Mk.III cargo stacking : ", "极速分拣器每次可运送 ");
        SorterCargoStackingEnabled.SettingChanged += (_, _) => SorterCargoStacking.Enable(SorterCargoStackingEnabled.Value);
        SorterCargoStacking.Enable(SorterCargoStackingEnabled.Value);
    }
    
    public static void Uninit()
    {
        SorterCargoStacking.Enable(false);
    }
    
    private static class SorterCargoStacking
    {
        private static Harmony _patch;
        private static bool _protoPatched;
        
        public static void Enable(bool on)
        {
            TryPatchProto(on);
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(SorterCargoStacking));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static void TryPatchProto(bool on)
        {
            var techs = LDB.techs;
            if (techs == null || techs.dataArray == null || techs.dataArray.Length == 0) return;
            if (on)
            {
                var delim = -26.0f;
                var tp3301 = techs.Select(3301);
                if (tp3301 != null && tp3301.IsObsolete)
                {
                    _protoPatched = false;
                    delim = tp3301.Position.y + 1.0f;
                }
                if (_protoPatched) return;

                foreach (var tp in techs.dataArray)
                {
                    switch (tp.ID)
                    {
                        case >= 3301 and <= 3305:
                            tp.UnlockValues[0] = tp.ID - 3300 + 1;
                            tp.IsObsolete = false;
                            continue;
                        case 3306:
                            tp.PreTechs = [];
                            continue;
                    }

                    if (tp.Position.y > delim) continue;
                    tp.Position.y -= 4.0f;
                }

                _protoPatched = true;
            }
            else
            {
                var delim = -28.0f;
                var tp3301 = techs.Select(3301);
                if (tp3301 != null && !tp3301.IsObsolete)
                {
                    _protoPatched = true;
                    delim = tp3301.Position.y - 1.0f;
                }
                if (!_protoPatched) return;
                foreach (var tp in techs.dataArray)
                {
                    if (tp.ID is >= 3301 and <= 3306)
                    {
                        tp.IsObsolete = true;
                        continue;
                    }

                    if (tp.Position.y > delim) continue;
                    tp.Position.y += 4.0f;
                }

                _protoPatched = false;
            }
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            TryPatchProto(true);
        }
    }
}
