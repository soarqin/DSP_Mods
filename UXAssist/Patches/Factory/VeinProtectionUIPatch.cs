using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UXAssist.Common.Patching;

namespace UXAssist.Patches.Factory;

internal static partial class VeinProtectionPatch
{
    internal partial class ProtectVeinsFromExhaustion
    {
        private static bool IsProtectedVein(ref VeinData vein, float miningRate)
        {
            if (miningRate <= 0f || vein.id <= 0 || vein.amount <= 0) return false;
            return vein.type == EVeinType.Oil
                ? _keepOilAmount > 2500 && vein.amount <= _keepOilAmount
                : vein.amount <= KeepVeinAmount;
        }

        private static int GetMiningVeinCount(ref MinerComponent miner, PlanetFactory factory)
        {
            var miningRate = FactoryPatch.GetMiningCostRate(factory.gameData.history);
            var count = 0;
            for (var i = 0; i < miner.veinCount; i++)
            {
                ref var vein = ref factory.veinPool[miner.veins[i]];
                if (vein.id > 0 && vein.amount > 0 && !IsProtectedVein(ref vein, miningRate)) count++;
            }
            return count;
        }

        private static int GetMiningVeinAmount(ref VeinData vein, PlanetFactory factory)
        {
            return IsProtectedVein(ref vein, FactoryPatch.GetMiningCostRate(factory.gameData.history)) ? 0 : vein.amount;
        }

        // Harmony transpiler: MiningSpeed_Transpiler
        // Target: ProductionExtraInfoCalculator.CalculateFactory, UIReferenceSpeedTip.AddEntryDataWithFactory, UIMinerWindow._OnUpdate
        // Fallback: Return the original instructions if the native mining-speed multiplications cannot be found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ProductionExtraInfoCalculator), nameof(ProductionExtraInfoCalculator.CalculateFactory))]
        [HarmonyPatch(typeof(UIReferenceSpeedTip), nameof(UIReferenceSpeedTip.AddEntryDataWithFactory))]
        [HarmonyPatch(typeof(UIMinerWindow), nameof(UIMinerWindow._OnUpdate))]
        private static IEnumerable<CodeInstruction> MiningSpeed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            var original = instructions.ToList();
            var matcher = new CodeMatcher(original, generator);
            var factoryParameter = __originalMethod.GetParameters().Select((p, i) => (p, i)).FirstOrDefault(v => v.p.ParameterType == typeof(PlanetFactory));
            var factoryLoad = factoryParameter.p != null
                ? new[] { new CodeInstruction(OpCodes.Ldarg, factoryParameter.i + (__originalMethod.IsStatic ? 0 : 1)) }
                : new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIMinerWindow), nameof(UIMinerWindow.factory))) };
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MinerComponent), nameof(MinerComponent.veinCount))),
                new CodeMatch(ci => ci.opcode == OpCodes.Conv_R8 || ci.opcode == OpCodes.Conv_R4));
            if (matcher.IsInvalid) return matcher.Finish(original, UXAssist.Logger, nameof(MiningSpeed_Transpiler));
            matcher.Repeat(m => ReplaceRead(m, factoryLoad, nameof(GetMiningVeinCount)));

            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.amount))),
                new CodeMatch(OpCodes.Conv_R8),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.oilSpeedMultiplier))));
            var oilMatched = matcher.IsValid;
            if (oilMatched) matcher.Repeat(m => ReplaceRead(m, factoryLoad, nameof(GetMiningVeinAmount)));
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.amount))),
                new CodeMatch(OpCodes.Conv_R8),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.oilSpeedMultiplier))));
            if (matcher.IsValid)
            {
                oilMatched = true;
                matcher.Repeat(m => ReplaceRead(m, factoryLoad, nameof(GetMiningVeinAmount)));
            }
            if (!oilMatched) return matcher.Finish(original, UXAssist.Logger, nameof(MiningSpeed_Transpiler));
            return matcher.InstructionEnumeration();
        }

        private static void ReplaceRead(CodeMatcher matcher, CodeInstruction[] factoryLoad, string helper)
        {
            var labels = matcher.Labels.ToArray();
            var blocks = matcher.Instruction.blocks.ToArray();
            matcher.RemoveInstruction();
            var replacement = factoryLoad.Select(ci => ci.Clone()).Append(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(ProtectVeinsFromExhaustion), helper))).ToArray();
            replacement[0].labels.AddRange(labels);
            replacement[0].blocks.AddRange(blocks);
            matcher.InsertAndAdvance(replacement);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityBriefInfo), nameof(EntityBriefInfo.SetBriefInfo))]
        private static void EntityBriefInfo_SetBriefInfo_Postfix(EntityBriefInfo __instance, PlanetFactory _factory, int _entityId)
        {
            if (_factory == null || _entityId <= 0) return;
            var minerId = _factory.entityPool[_entityId].minerId;
            if (minerId <= 0) return;
            ref var miner = ref _factory.factorySystem.minerPool[minerId];
            if (miner.type == EMinerType.Water) return;
            var miningRate = FactoryPatch.GetMiningCostRate(_factory.gameData.history);
            for (var i = 0; i < miner.veinCount; i++)
            {
                ref var vein = ref _factory.veinPool[miner.veins[i]];
                if (IsProtectedVein(ref vein, miningRate)) __instance.equipment.Add((int)vein.type, -1, 0);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AstroResourceStatPlan), nameof(AstroResourceStatPlan.AddPlanetResources))]
        private static void AstroResourceStatPlan_AddPlanetResources_Postfix(AstroResourceStatPlan __instance, PlanetData planet, int veinAmountDisplayFilter)
        {
            var factory = planet?.factory;
            if (factory == null || planet.type == EPlanetType.Gas || veinAmountDisplayFilter is not (1 or 2)) return;
            var miningRate = FactoryPatch.GetMiningCostRate(factory.gameData.history);
            if (miningRate <= 0f) return;
            var miners = factory.factorySystem.minerPool;
            var consumers = factory.powerSystem.consumerPool;
            var seen = AstroResourceStatPlan.hashes;
            seen.Clear();
            var sign = veinAmountDisplayFilter == 1 ? -1 : 1;
            for (var i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                ref var miner = ref miners[i];
                if (miner.id != i || miner.type == EMinerType.Water || consumers[miner.pcId].networkId <= 0) continue;
                for (var j = 0; j < miner.veinCount; j++)
                {
                    var veinId = miner.veins[j];
                    ref var vein = ref factory.veinPool[veinId];
                    if (!IsProtectedVein(ref vein, miningRate) || !seen.Add(veinId)) continue;
                    var type = vein.type == EVeinType.Oil ? AstroResourceStatPlan.EAstroResourceType.Oil : AstroResourceStatPlan.EAstroResourceType.Vein;
                    double amount = vein.amount;
                    if (vein.type == EVeinType.Oil)
                    {
                        amount = (float)vein.amount * VeinData.oilSpeedMultiplier;
                        if (veinAmountDisplayFilter == 1) amount *= __instance.gameData.history.miningSpeedScale;
                    }
                    __instance.AddStatData(type, (int)vein.type, sign, sign * amount);
                }
            }
            seen.Clear();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroResourceStatPlan), nameof(AstroResourceStatPlan.AddPlanetDetailedResources))]
        private static bool AstroResourceStatPlan_AddPlanetDetailedResources_Prefix(AstroResourceStatPlan __instance, PlanetData planet, int veinAmountDisplayFilter, ref bool __result)
        {
            var factory = planet?.factory;
            if (factory == null || planet.type == EPlanetType.Gas || veinAmountDisplayFilter is not (1 or 2)) return true;
            var miningRate = FactoryPatch.GetMiningCostRate(factory.gameData.history);
            var veins = planet.runtimeVeinPool;
            if (miningRate <= 0f || veins == null) return true;
            var seen = AstroResourceStatPlan.hashes;
            seen.Clear();
            var miners = factory.factorySystem.minerPool;
            var consumers = factory.powerSystem.consumerPool;
            __result = true;
            for (var i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                ref var miner = ref miners[i];
                if (miner.id != i || miner.type != EMinerType.Vein || consumers[miner.pcId].networkId <= 0) continue;
                for (var j = 0; j < miner.veinCount; j++)
                {
                    var id = miner.veins[j];
                    if (id <= 0) continue;
                    ref var vein = ref veins[id];
                    if (vein.id != id || vein.type == EVeinType.Oil || IsProtectedVein(ref vein, miningRate) || !seen.Add(id)) continue;
                    if (veinAmountDisplayFilter == 1 && !__instance.AddDetailedStatData(planet.astroId, (int)vein.type, vein.groupIndex, vein.amount))
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            if (veinAmountDisplayFilter == 2)
            {
                for (var i = 1; i < planet.runtimeVeinCursor; i++)
                {
                    ref var vein = ref veins[i];
                    if (vein.id != i || vein.type == EVeinType.Oil || seen.Contains(i)) continue;
                    if (__instance.AddDetailedStatData(planet.astroId, (int)vein.type, vein.groupIndex, vein.amount)) continue;
                    __result = false;
                    break;
                }
            }
            return false;
        }
    }
}
