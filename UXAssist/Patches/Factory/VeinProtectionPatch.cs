using System;
using System.Collections.Generic;
using HarmonyLib;
using UXAssist.Common;

namespace UXAssist.Patches.Factory;

internal static class VeinProtectionPatch
{
    public static void Enable(bool enable)
    {
        ProtectVeinsFromExhaustion.Enable(enable);
    }

    public static void InitConfig()
    {
        ProtectVeinsFromExhaustion.InitConfig();
    }

    internal class ProtectVeinsFromExhaustion : PatchImpl<ProtectVeinsFromExhaustion>
    {
        public static int KeepVeinAmount = 100;
        public static float KeepOilSpeed = 1f;
        private static int _keepOilAmount;

        public static void InitConfig()
        {
            _keepOilAmount = Math.Max((int)(KeepOilSpeed / 0.00004f + 0.5f), 2500);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
        private static bool MinerComponent_InternalUpdate_Prefix(PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister,
            ref MinerComponent __instance, out uint __result)
        {
            if (power < 0.1f)
            {
                __result = 0U;
                return false;
            }

            var res = 0U;
            int veinId;
            int times;
            switch (__instance.type)
            {
                case EMinerType.Vein:
                    var veinCount = __instance.veinCount;
                    if (veinCount <= 0)
                        break;

                    if (__instance.time <= __instance.period)
                    {
                        __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * veinCount);
                        res = 1U;
                    }

                    if (__instance.time < __instance.period)
                    {
                        break;
                    }

                    var currentVeinIndex = __instance.currentVeinIndex;
                    veinId = __instance.veins[currentVeinIndex];
                    lock (veinPool)
                    {
                        if (veinPool[veinId].id == 0)
                        {
                            __instance.RemoveVeinFromArray(currentVeinIndex);
                            __instance.GetMinimumVeinAmount(factory, veinPool);
                            veinCount = __instance.veinCount;
                            __instance.currentVeinIndex = veinCount > 1 ? currentVeinIndex % veinCount : 0;
                            __result = 0U;
                            return false;
                        }

                        if (__instance.productCount < 50 && (__instance.productId == 0 || __instance.productId == veinPool[veinId].productId))
                        {
                            __instance.productId = veinPool[veinId].productId;
                            times = __instance.time / __instance.period;
                            var outputCount = 0;
                            var amount = veinPool[veinId].amount;
                            if (miningRate > 0f)
                            {
                                if (amount > KeepVeinAmount)
                                {
                                    var usedCount = 0;
                                    var maxAllowed = amount - KeepVeinAmount;
                                    var add = miningRate * (double)times;
                                    __instance.costFrac += add;
                                    var estimateUses = (int)__instance.costFrac;
                                    if (estimateUses < maxAllowed)
                                    {
                                        outputCount = times;
                                        usedCount = estimateUses;
                                        __instance.costFrac -= estimateUses;
                                    }
                                    else
                                    {
                                        usedCount = maxAllowed;
                                        var oldFrac = __instance.costFrac - add;
                                        var ratio = (usedCount - oldFrac) / add;
                                        var realCost = times * ratio;
                                        outputCount = (int)(Math.Ceiling(realCost) + 0.01);
                                        __instance.costFrac = miningRate * (outputCount - realCost);
                                    }
                                    if (usedCount > 0)
                                    {
                                        var groupIndex = (int)veinPool[veinId].groupIndex;
                                        amount -= usedCount;
                                        veinPool[veinId].amount = amount;
                                        if (amount < __instance.minimumVeinAmount)
                                        {
                                            __instance.minimumVeinAmount = amount;
                                        }

                                        factory.veinGroups[groupIndex].amount -= usedCount;
                                        factory.veinAnimPool[veinId].time = amount >= 20000 ? 0f : 1f - 0.00005f;
                                        if (amount <= 0)
                                        {
                                            var venType = (int)veinPool[veinId].type;
                                            var pos = veinPool[veinId].pos;
                                            factory.RemoveVeinWithComponents(veinId);
                                            factory.RecalculateVeinGroup(groupIndex);
                                            factory.NotifyVeinExhausted(venType, groupIndex, pos);
                                            veinCount = __instance.veinCount;
                                        }
                                        else
                                        {
                                            currentVeinIndex++;
                                        }
                                    }
                                }
                                else
                                {
                                    if (amount <= 0)
                                    {
                                        __instance.RemoveVeinFromArray(currentVeinIndex);
                                        __instance.GetMinimumVeinAmount(factory, veinPool);
                                        veinCount = __instance.veinCount;
                                    }
                                    else
                                    {
                                        currentVeinIndex++;
                                    }
                                    __instance.currentVeinIndex = veinCount > 1 ? currentVeinIndex % veinCount : 0;
                                    __instance.time -= __instance.period * times;
                                    break;
                                }
                            }
                            else
                            {
                                outputCount = times;
                            }
                            __instance.productCount += outputCount;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += outputCount;
                                factory.AddMiningFlagUnsafe(veinPool[veinId].type);
                                factory.AddVeinMiningFlagUnsafe(veinPool[veinId].type);
                            }
                            __instance.time -= __instance.period * outputCount;
                            __instance.currentVeinIndex = veinCount > 1 ? currentVeinIndex % veinCount : 0;
                        }
                    }

                    break;
                case EMinerType.Oil:
                    if (__instance.veinCount <= 0)
                        break;

                    veinId = __instance.veins[0];
                    lock (veinPool)
                    {
                        var amount = veinPool[veinId].amount;
                        var workCount = amount * VeinData.oilSpeedMultiplier;
                        if (__instance.time < __instance.period)
                        {
                            __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * workCount + 0.5f);
                            res = 1U;
                        }

                        if (__instance.time >= __instance.period && __instance.productCount < 50)
                        {
                            __instance.productId = veinPool[veinId].productId;
                            times = __instance.time / __instance.period;
                            if (times <= 0) break;
                            var outputCount = 0;
                            if (miningRate > 0f)
                            {
                                if (amount > _keepOilAmount)
                                {
                                    var usedCount = 0;
                                    var maxAllowed = amount - _keepOilAmount;
                                    var add = miningRate * (double)times;
                                    __instance.costFrac += add;
                                    var estimateUses = (int)__instance.costFrac;
                                    if (estimateUses < maxAllowed)
                                    {
                                        outputCount = times;
                                        usedCount = estimateUses;
                                        __instance.costFrac -= estimateUses;
                                    }
                                    else
                                    {
                                        usedCount = maxAllowed;
                                        var oldFrac = __instance.costFrac - add;
                                        var ratio = (usedCount - oldFrac) / add;
                                        var realCost = times * ratio;
                                        outputCount = (int)(Math.Ceiling(realCost) + 0.01);
                                        __instance.costFrac = miningRate * (outputCount - realCost);
                                    }
                                    if (usedCount > 0)
                                    {
                                        if (usedCount > maxAllowed)
                                        {
                                            usedCount = maxAllowed;
                                        }

                                        amount -= usedCount;
                                        veinPool[veinId].amount = amount;
                                        var groupIndex = veinPool[veinId].groupIndex;
                                        factory.veinGroups[groupIndex].amount -= usedCount;
                                        factory.veinAnimPool[veinId].time = amount >= 25000 ? 0f : 1f - amount * VeinData.oilSpeedMultiplier;
                                        if (amount <= 2500)
                                        {
                                            factory.NotifyVeinExhausted((int)veinPool[veinId].type, groupIndex, veinPool[veinId].pos);
                                        }
                                    }
                                }
                                else if (_keepOilAmount <= 2500)
                                {
                                    outputCount = times;
                                }
                                else
                                {
                                    __instance.time -= __instance.period * times;
                                    break;
                                }
                            }
                            else
                            {
                                outputCount = times;
                            }

                            __instance.productCount += outputCount;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += outputCount;
                            }

                            __instance.time -= __instance.period * outputCount;
                        }
                    }

                    break;

                case EMinerType.Water:
                    if (__instance.time < __instance.period)
                    {
                        __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed);
                        res = 1U;
                    }

                    if (__instance.time < __instance.period) break;
                    times = __instance.time / __instance.period;
                    if (__instance.productCount >= 50) break;
                    __instance.productId = factory.planet.waterItemId;
                    do
                    {
                        if (__instance.productId > 0)
                        {
                            __instance.productCount += times;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += times;
                                break;
                            }
                        }

                        __instance.productId = 0;
                    } while (false);

                    __instance.time -= __instance.period * times;
                    break;
            }

            if (__instance is { productCount: > 0, insertTarget: > 0, productId: > 0 })
            {
                var multiplier = 36000000.0 / __instance.period * miningSpeed;
                if (__instance.type == EMinerType.Vein)
                {
                    multiplier *= __instance.veinCount;
                }
                else if (__instance.type == EMinerType.Oil)
                {
                    multiplier *= veinPool[__instance.veins[0]].amount * VeinData.oilSpeedMultiplier;
                }

                var count = (int)(multiplier - 0.01) / 1800 + 1;
                count = count < 4 ? count < 1 ? 1 : count : 4;
                var stack = __instance.productCount < count ? __instance.productCount : count;
                var outputCount = factory.InsertInto(__instance.insertTarget, 0, __instance.productId, (byte)stack, 0, out _);
                __instance.productCount -= outputCount;
                if (__instance is { productCount: 0, type: EMinerType.Vein })
                {
                    __instance.productId = 0;
                }
            }

            __result = res;
            return false;
        }
    }
}
