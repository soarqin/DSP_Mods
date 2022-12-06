using System;
using System.IO;
using HarmonyLib;

namespace Dustbin;

using IsDustbinIndexer = DynamicObjectArray<DynamicObjectArray<DynamicValueArray<bool>>>;

public class DynamicValueArray<T> where T: struct
{
    private T[] _store = new T[16];

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _store.Length) return default;
            return _store[index];
        }
        set
        {
            if (index >= _store.Length)
            {
                Array.Resize(ref _store, _store.Length * 2);
            }
            _store[index] = value;
        }
    }

    public void Reset()
    {
        _store = new T[16];
    }

    public delegate void ForEachFunc(int id, T value);
    public void ForEach(ForEachFunc func)
    {
        var len = _store.Length;
        for (var i = 0; i < len; i++)
        {
            func(i, _store[i]);
        }
    }
}

public class DynamicObjectArray<T> where T: class, new()
{
    private T[] _store = new T[16];

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _store.Length) return null;
            var result = _store[index];
            if (result == null)
            {
                result = new T();
                _store[index] = result;
            }
            return result;
        }
    }

    public void Reset()
    {
        _store = new T[16];
    }

    public delegate void ForEachFunc(int id, T value);
    public void ForEach(ForEachFunc func)
    {
        var len = _store.Length;
        for (var i = 0; i < len; i++)
        {
            func(i, _store[i]);
        }
    }
}

[HarmonyPatch]
public static class TankPatch
{
    private static MyCheckBox _tankDustbinCheckBox;
    private static int lastTankId;
    private static IsDustbinIndexer tankIsDustbin = new();

    public static void Reset()
    {
        lastTankId = 0;
        tankIsDustbin.Reset();
    }

    public static void Export(BinaryWriter w)
    {
        var tempStream = new MemoryStream();
        var tempWriter = new BinaryWriter(tempStream);
        tankIsDustbin.ForEach((i, star) =>
        {
            star?.ForEach((j, planet) =>
            {
                var count = 0;
                planet?.ForEach((id, v) =>
                {
                    if (!v) return;
                    tempWriter.Write(id);
                    count++;
                });
                if (count == 0) return;

                tempWriter.Flush();
                tempStream.Position = 0;
                w.Write((byte)2);
                var planetId = i * 100 + j;
                w.Write(planetId);
                w.Write(count);
                /* FixMe: May BinaryWriter not sync with its BaseStream while subclass overrides Write()? */
                tempStream.CopyTo(w.BaseStream);
                tempStream.SetLength(0);
            });
        });
        tempWriter.Dispose();
        tempStream.Dispose();
    }

    public static void Import(BinaryReader r)
    {
        while (r.PeekChar() == 2)
        {
            r.ReadByte();
            var planetId = r.ReadInt32();
            var data = tankIsDustbin[planetId / 100][planetId % 100];
            for (var count = r.ReadInt32(); count > 0; count--)
            {
                data[r.ReadInt32()] = true;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITankWindow), "_OnCreate")]
    private static void UITankWindow__OnCreate_Postfix(UITankWindow __instance)
    {
        _tankDustbinCheckBox = MyCheckBox.CreateCheckBox(false, __instance.transform, 120f, 20f, Localization.language == Language.zhCN ? "垃圾桶" : "Dustbin");
        var window = __instance;
        _tankDustbinCheckBox.OnChecked += () =>
        {
            var tankId = window.tankId;
            if (tankId <= 0 || window.storage.tankPool[tankId].id != tankId) return;
            var planetId = window.storage.planet.id;
            tankIsDustbin[planetId / 100][planetId % 100][tankId] = _tankDustbinCheckBox.Checked;
        };
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITankWindow), "_OnUpdate")]
    private static void UITankWindow__OnUpdate_Postfix(UITankWindow __instance)
    {
        var tankId = __instance.tankId;
        if (lastTankId == tankId) return;
        lastTankId = tankId;
        if (tankId > 0 && __instance.storage.tankPool[tankId].id == tankId)
        {
            var planetId = __instance.storage.planet.id;
            _tankDustbinCheckBox.Checked = tankIsDustbin[planetId / 100][planetId % 100][tankId];
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TankComponent), "GameTick")]
    private static void TankComponent_GameTick_Prefix(ref TankComponent __instance, out long __state, PlanetFactory factory)
    {
        var planetId = factory.planetId;
        if (tankIsDustbin[planetId / 100][planetId % 100][__instance.id])
        {
            __state = ((long)__instance.fluidInc << 36) | ((long)__instance.fluidCount << 16) | (uint)__instance.fluidId;
            __instance.fluidId = __instance.fluidCount = __instance.fluidInc = 0;
        }
        else
        {
            __state = -1;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TankComponent), "GameTick")]
    private static void TankComponent_GameTick_Postfix(ref TankComponent __instance, long __state)
    {
        if (__state < 0) return;
        __instance.fluidId = (int)(__state & 0xFFFFL);
        __instance.fluidCount = (int)((__state >> 16) & 0xFFFFFL);
        __instance.fluidInc = (int)(__state >> 36);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FactoryStorage), "RemoveTankComponent")]
    private static void FactoryStorage_RemoveTankComponent_Prefix(FactoryStorage __instance, int id)
    {
        if (__instance.tankPool[id].id != 0)
        {
            var planetId = __instance.planet.id;
            tankIsDustbin[planetId / 100][planetId % 100][id] = false;
        }
    }
}
