using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NebulaAPI;

namespace Dustbin;

[HarmonyPatch]
public static class TankPatch
{
    private static UI.MyCheckBox _tankDustbinCheckBox;
    private static int _lastTankId;

    public static void Reset()
    {
        _lastTankId = 0;
    }

    public static void Export(BinaryWriter w)
    {
        var tempStream = new MemoryStream();
        var tempWriter = new BinaryWriter(tempStream);

        var factories = GameMain.data.factories;
        var factoryCount = GameMain.data.factoryCount;
        for (var i = 0; i < factoryCount; i++)
        {
            var factory = factories[i];
            if (factory == null) continue;
            var storage = factory.factoryStorage;
            var tankPool = storage.tankPool;
            var cursor = storage.tankCursor;
            var count = 0;

            for (var j = 1; j < cursor; j++)
            {
                if (tankPool[j].id != j) continue;
                if (!tankPool[j].IsDustbin) continue;
                tempWriter.Write(j);
                count++;
            }

            if (count == 0) continue;

            tempWriter.Flush();
            tempStream.Position = 0;
            w.Write((byte)2);
            w.Write(factory.planetId);
            w.Write(count);
            /* FixMe: May BinaryWriter not sync with its BaseStream while subclass overrides Write()? */
            tempStream.CopyTo(w.BaseStream);
            tempStream.SetLength(0);
        }

        tempWriter.Dispose();
        tempStream.Dispose();
    }

    public static void Import(BinaryReader r)
    {
        while (r.PeekChar() == 2)
        {
            r.ReadByte();
            var planetId = r.ReadInt32();
            var planet = GameMain.data.galaxy.PlanetById(planetId);
            var tankPool = planet?.factory.factoryStorage.tankPool;
            if (tankPool == null)
            {
                for (var count = r.ReadInt32(); count > 0; count--)
                {
                    r.ReadInt32();
                }

                continue;
            }

            for (var count = r.ReadInt32(); count > 0; count--)
            {
                var id = r.ReadInt32();
                if (id > 0 && id < tankPool.Length && tankPool[id].id == id)
                {
                    tankPool[id].IsDustbin = true;
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITankWindow), "_OnCreate")]
    private static void UITankWindow__OnCreate_Postfix(UITankWindow __instance)
    {
        _tankDustbinCheckBox = UI.MyCheckBox.CreateCheckBox(false, __instance.transform, 120f, 20f, Localization.language == Language.zhCN ? "垃圾桶" : "Dustbin");
        var window = __instance;
        _tankDustbinCheckBox.OnChecked += () =>
        {
            var tankId = window.tankId;
            if (tankId <= 0) return;
            var tankPool = window.storage.tankPool;
            if (tankPool[tankId].id != tankId) return;
            var enabled = _tankDustbinCheckBox.Checked;
            tankPool[tankId].IsDustbin = enabled;
            if (!NebulaModAPI.IsMultiplayerActive) return;
            var planetId = window.factory.planetId;
            NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(new NebulaSupport.Packet.ToggleEvent(planetId, -tankId, enabled));
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITankWindow), "_OnUpdate")]
    private static void UITankWindow__OnUpdate_Postfix(UITankWindow __instance)
    {
        var tankId = __instance.tankId;
        if (_lastTankId == tankId) return;
        _lastTankId = tankId;

        if (tankId <= 0) return;
        var tankPool = __instance.storage.tankPool;
        ref var tank = ref tankPool[tankId];
        if (tank.id != tankId) return;
        _tankDustbinCheckBox.Checked = tank.IsDustbin;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TankComponent), "GameTick")]
    private static bool TankComponent_GameTick_Prefix(ref TankComponent __instance, PlanetFactory factory)
    {
        if (__instance.fluidInc < 0)
            __instance.fluidInc = 0;

        if (!__instance.isBottom)
            return false;

        var cargoTraffic = factory.cargoTraffic;
        var tankPool = factory.factoryStorage.tankPool;
        var belt = __instance.belt0;
        if (belt > 0)
        {
            TankComponentUpdateBelt(ref __instance, belt, __instance.isOutput0, ref cargoTraffic, ref tankPool);
        }

        belt = __instance.belt1;
        if (belt > 0)
        {
            TankComponentUpdateBelt(ref __instance, belt, __instance.isOutput1, ref cargoTraffic, ref tankPool);
        }

        belt = __instance.belt2;
        if (belt > 0)
        {
            TankComponentUpdateBelt(ref __instance, belt, __instance.isOutput2, ref cargoTraffic, ref tankPool);
        }

        belt = __instance.belt3;
        if (belt > 0)
        {
            TankComponentUpdateBelt(ref __instance, belt, __instance.isOutput3, ref cargoTraffic, ref tankPool);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TankComponentUpdateBelt(ref TankComponent thisTank, int belt, bool isOutput, ref CargoTraffic cargoTraffic, ref TankComponent[] tankPool)
    {
        switch (isOutput)
        {
            case true when thisTank.outputSwitch:
            {
                if (thisTank.fluidId <= 0 || thisTank.fluidCount <= 0) return;
                var inc = thisTank.fluidInc == 0 ? 0 : thisTank.fluidInc / thisTank.fluidCount;
                if (!cargoTraffic.TryInsertItemAtHead(belt, thisTank.fluidId, 1, (byte)inc)) return;
                thisTank.fluidCount--;
                thisTank.fluidInc -= inc;
                return;
            }
            case false when thisTank.inputSwitch:
            {
                byte stack;
                byte inc;
                switch (thisTank.fluidId)
                {
                    case > 0 when thisTank.fluidCount < thisTank.fluidCapacity && cargoTraffic.TryPickItemAtRear(belt, thisTank.fluidId, null, out stack, out inc) > 0 &&
                                  !thisTank.IsDustbin:
                        thisTank.fluidCount += stack;
                        thisTank.fluidInc += inc;
                        return;
                    case 0:
                    {
                        var count = cargoTraffic.TryPickItemAtRear(belt, 0, ItemProto.fluids, out stack, out inc);
                        if (count <= 0 || thisTank.IsDustbin) return;
                        thisTank.fluidId = count;
                        thisTank.fluidCount += stack;
                        thisTank.fluidInc += inc;
                        return;
                    }
                }

                if (thisTank.fluidCount < thisTank.fluidCapacity || cargoTraffic.GetItemIdAtRear(belt) != thisTank.fluidId || thisTank.nextTankId <= 0) return;
                ref var targetTank = ref tankPool[thisTank.nextTankId];
                while (targetTank.fluidCount >= targetTank.fluidCapacity)
                {
                    ref var lastTank = ref tankPool[targetTank.lastTankId];
                    if (targetTank.fluidId != lastTank.fluidId)
                    {
                        if (lastTank.id == thisTank.id || targetTank.fluidCount >= targetTank.fluidCapacity) return;
                        targetTank = ref lastTank;
                        break;
                    }

                    if (!targetTank.inputSwitch)
                    {
                        if (lastTank.id == thisTank.id || targetTank.fluidCount >= targetTank.fluidCapacity) return;
                        targetTank = ref lastTank;
                        break;
                    }

                    if (targetTank.nextTankId <= 0)
                    {
                        break;
                    }

                    targetTank = ref tankPool[targetTank.nextTankId];
                }

                ref var lastTank2 = ref tankPool[targetTank.lastTankId];
                if (!targetTank.inputSwitch)
                {
                    targetTank = ref lastTank2;
                }
                else
                {
                    var fluidId = targetTank.fluidId;
                    if (fluidId != 0 && fluidId != lastTank2.fluidId)
                    {
                        if (lastTank2.id == thisTank.id || targetTank.fluidCount >= targetTank.fluidCapacity) return;
                        targetTank = ref lastTank2;
                    }
                    else if (!lastTank2.outputSwitch)
                    {
                        return;
                    }
                }

                if (cargoTraffic.TryPickItemAtRear(belt, thisTank.fluidId, null, out stack, out inc) <= 0 || targetTank.IsDustbin) return;
                if (targetTank.fluidCount == 0)
                {
                    targetTank.fluidId = thisTank.fluidId;
                }

                targetTank.fluidCount += stack;
                targetTank.fluidInc += inc;
                return;
            }
        }
    }
}