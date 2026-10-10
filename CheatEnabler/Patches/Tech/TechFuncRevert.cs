using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheatEnabler.Patches.Tech
{
    /// <summary>
    /// Reverts the fields changed by GameHistoryData.UnlockTechFunction. Lossy, multiplicative,
    /// assignment and boolean functions are recomputed from native defaults and surviving levels.
    /// Package geometry and station limits are reconciled once per click; native unlock side effects
    /// must never be replayed across the whole tech tree.
    /// </summary>
    internal static class TechFuncRevert
    {
        // Base values hard-coded by GameHistoryData.SetForNewGame().
        private const int BaseInserterStackCountObsolete = 1;
        private const int BaseInserterStackInput = 2;
        private const int BaseInserterStackOutput = 1;
        private const int BaseStorageLevel = 2;
        private const int BaseLabLevel = 3;
        private const int BaseStationPilerLevel = 1;
        private const int BaseDysonNodeAbsorbInterval = 360;

        /// <summary>Function ids already reported as having no known inverse (log-once guard).</summary>
        private static readonly HashSet<int> UnknownFuncsWarned = new HashSet<int>();

        /// <summary>The game's own rounding of <c>double</c> tech values to <c>int</c>.</summary>
        internal static int ToInt(double value) => (int)(value > 0.0 ? value + 0.5 : value - 0.5);

        /// <summary>Mirrors <c>Mathf.Round(x * 1000f) / 1000f</c> used by func 21.</summary>
        private static float Round3Mathf(float value) => Mathf.Round(value * 1000f) / 1000f;

        /// <summary>Mirrors <c>(float)(int)(x * 1000.0 + 0.5) / 1000f</c> used by funcs 61-64, 67 and 71.</summary>
        private static float Round3Trunc(float value) => (float)(int)((double)value * 1000.0 + 0.5) / 1000f;

        /// <summary>Mirrors <c>(double)(int)(x * 1000.0 + 0.5) / 1000.0</c> used by func 84.</summary>
        private static double Round3Trunc(double value) => (int)(value * 1000.0 + 0.5) / 1000.0;

        /// <summary>Undo one already applied level of <paramref name="func"/>.</summary>
        public static void Revert(GameHistoryData history, int func, double value, int level)
        {
            Player player = GameMain.mainPlayer;
            Mecha mecha = player != null ? player.mecha : null;
            int num = ToInt(value);

            switch (func)
            {
                case 1:
                    if (mecha != null)
                    {
                        ConstructionModuleComponent module = mecha.constructionModule;
                        module.droneCount = Math.Max(0, module.droneCount - num);
                        module.droneAliveCount = Math.Max(0, Math.Min(module.droneAliveCount - num, module.droneCount));
                        module.droneIdleCount = Math.Max(0, Math.Min(module.droneIdleCount - num, module.droneCount));
                    }

                    break;

                case 2:
                    if (mecha != null)
                    {
                        mecha.reactorPowerGen -= value;
                        if (mecha.reactorPowerGen < 0.0)
                        {
                            mecha.reactorPowerGen = 0.0;
                        }
                    }

                    break;

                case 3:
                    if (mecha != null)
                    {
                        mecha.walkSpeed -= (float)value;
                    }

                    break;

                case 4: // assignment: recomputed from the techs that still grant it
                    TechDowngrade.MarkDirty(func);
                    break;

                case 5: // package geometry: recomputed together with func 38
                    TechDowngrade.MarkDirty(func);
                    break;

                case 6:
                    // coreEnergyCap is additive, coreLevel is an assignment of the tech level -> the
                    // capacity is reverted here and the level is recomputed.
                    if (mecha != null)
                    {
                        mecha.coreEnergyCap -= value;
                        if (mecha.coreEnergyCap < 0.0)
                        {
                            mecha.coreEnergyCap = 0.0;
                        }
                        mecha.coreEnergy = Math.Min(mecha.coreEnergy, mecha.coreEnergyCap);
                    }

                    TechDowngrade.MarkDirty(func);
                    break;

                case 7:
                    if (mecha != null)
                    {
                        mecha.replicateSpeed -= (float)value;
                    }

                    break;

                case 8: // one-way flag: recomputed
                    TechDowngrade.MarkDirty(func);
                    break;

                case 9:
                    history.constructionDroneMovement = Math.Max(0, history.constructionDroneMovement - num);
                    break;

                case 10:
                    history.constructionDroneSpeed -= (float)value;
                    break;

                case 11:
                    if (mecha != null)
                    {
                        mecha.maxSailSpeed -= (float)value;
                    }

                    break;

                case 12:
                    history.solarSailLife -= (float)value;
                    break;

                case 13: // multiplicative: recomputed (no float drift)
                    TechDowngrade.MarkDirty(func);
                    break;

                case 14: // assignment: recomputed
                    TechDowngrade.MarkDirty(func);
                    TechDowngrade.MarkInserterDirty();
                    break;

                case 15:
                    history.logisticDroneSpeedScale -= (float)value;
                    break;

                case 16:
                    history.logisticShipSpeedScale -= (float)value;
                    break;

                case 17: // one-way flag: recomputed, and existing warpers are never removed
                    TechDowngrade.MarkDirty(func);
                    break;

                case 18:
                    history.logisticDroneCarries = Math.Max(0, history.logisticDroneCarries - num);
                    break;

                case 19:
                    history.logisticShipCarries = Math.Max(0, history.logisticShipCarries - num);
                    break;

                case 20: // multiplicative with a 0 clamp: recomputed
                    TechDowngrade.MarkDirty(func);
                    break;

                case 21:
                    history.miningSpeedScale = Round3Mathf(history.miningSpeedScale - (float)value);
                    break;

                case 22:
                    history.techSpeed = Math.Max(1, history.techSpeed - num);
                    break;

                case 23: // assignment
                    TechDowngrade.MarkDirty(func);
                    break;

                case 24:
                    history.storageLevel = Math.Max(BaseStorageLevel, history.storageLevel - num);
                    break;

                case 25:
                    history.labLevel = Math.Max(BaseLabLevel, history.labLevel - num);
                    break;

                case 26:
                    history.dysonNodeLatitude -= (float)value;
                    break;

                case 27:
                    if (mecha != null)
                    {
                        mecha.maxWarpSpeed -= (float)(value * 40000.0);
                    }

                    break;

                case 28: // assignment
                    TechDowngrade.MarkDirty(func);
                    break;

                case 29:
                    history.stationPilerLevel = Math.Max(BaseStationPilerLevel, history.stationPilerLevel - num);
                    break;

                case 30:
                    // Existing station limits are rescaled once after all levels have been reverted.
                    history.localStationExtraStorage = Math.Max(0, history.localStationExtraStorage - num);
                    break;

                case 31:
                    history.remoteStationExtraStorage = Math.Max(0, history.remoteStationExtraStorage - num);
                    break;

                case 32:
                    if (player != null && player.deliveryPackage != null)
                    {
                        player.deliveryPackage.colCount = Math.Max(0, player.deliveryPackage.colCount - num);
                        player.deliveryPackage.NotifySizeChange();
                    }

                    break;

                case 33:
                    if (player != null && player.deliveryPackage != null)
                    {
                        player.deliveryPackage.stackSizeMultiplier = Math.Max(1, player.deliveryPackage.stackSizeMultiplier - num);
                    }

                    break;

                case 34:
                    history.logisticCourierSpeedScale -= (float)value;
                    break;

                case 35:
                    history.logisticCourierCarries = Math.Max(0, history.logisticCourierCarries - num);
                    break;

                case 36:
                    history.dispenserDeliveryMaxAngle = Math.Max(0f, history.dispenserDeliveryMaxAngle - num);
                    break;

                case 37: // feature keys: recomputed
                    TechDowngrade.MarkDirty(func);
                    break;

                case 38:
                    TechDowngrade.MarkDirty(func);
                    break;

                case 39:
                    TechDowngrade.MarkDirty(func);
                    TechDowngrade.MarkInserterDirty();
                    break;

                case 40:
                    TechDowngrade.MarkDirty(func);
                    TechDowngrade.MarkInserterDirty();
                    break;

                case 41:
                    TechDowngrade.MarkDirty(func);
                    TechDowngrade.MarkInserterDirty();
                    break;

                case 42:
                case 43:
                case 44:
                case 45:
                    TechDowngrade.MarkDirty(func);
                    break;

                case 61:
                    history.kineticDamageScale = Round3Trunc(history.kineticDamageScale - (float)value);
                    break;

                case 62:
                    history.energyDamageScale = Round3Trunc(history.energyDamageScale - (float)value);
                    break;

                case 63:
                    history.blastDamageScale = Round3Trunc(history.blastDamageScale - (float)value);
                    break;

                case 64:
                    history.magneticDamageScale = Round3Trunc(history.magneticDamageScale - (float)value);
                    break;

                case 65:
                    if (mecha != null)
                    {
                        mecha.energyShieldRadius = Math.Max(0f, mecha.energyShieldRadius - (float)value);
                    }

                    break;

                case 66:
                    if (mecha != null)
                    {
                        mecha.energyShieldCapacity = Math.Max(0L, mecha.energyShieldCapacity - num);
                        mecha.energyShieldEnergy = Math.Min(mecha.energyShieldEnergy, mecha.energyShieldCapacity);
                    }

                    break;

                case 67:
                    history.combatDroneDamageRatio = Round3Trunc(history.combatDroneDamageRatio - (float)value);
                    break;

                case 68:
                    history.combatDroneROFRatio -= (float)value;
                    break;

                case 69:
                    history.combatDroneDurabilityRatio -= (float)value;
                    break;

                case 70:
                    history.combatDroneSpeedRatio -= (float)value;
                    history.combatShipSpeedRatio -= (float)value;
                    break;

                case 71:
                    history.combatShipDamageRatio = Round3Trunc(history.combatShipDamageRatio - (float)value);
                    break;

                case 72:
                    history.combatShipROFRatio -= (float)value;
                    break;

                case 73:
                    history.combatShipDurabilityRatio -= (float)value;
                    break;

                case 74: // ratio driven value: recomputed
                    TechDowngrade.MarkDirty(func);
                    break;

                case 75: // one-way flag
                    TechDowngrade.MarkDirty(func);
                    break;

                case 76: // assignment
                    TechDowngrade.MarkDirty(func);
                    break;

                case 77:
                    if (mecha != null && mecha.groundCombatModule != null)
                    {
                        mecha.groundCombatModule.fleetCount = Math.Max(0, mecha.groundCombatModule.fleetCount - (int)value);
                    }

                    break;

                case 78:
                    history.groundFleetPortCount = Math.Max(0, history.groundFleetPortCount - (int)value);
                    break;

                case 79:
                    if (mecha != null && mecha.spaceCombatModule != null)
                    {
                        mecha.spaceCombatModule.fleetCount = Math.Max(0, mecha.spaceCombatModule.fleetCount - (int)value);
                    }

                    break;

                case 80:
                    history.spaceFleetPortCount = Math.Max(0, history.spaceFleetPortCount - (int)value);
                    break;

                case 81:
                    if (mecha != null)
                    {
                        RescaleHp(mecha, () => mecha.hpMaxUpgrade = Math.Max(0, mecha.hpMaxUpgrade - num));
                    }

                    break;

                case 82:
                case 83:
                    TechDowngrade.MarkDirty(func);
                    break;

                case 84:
                    if (mecha != null)
                    {
                        RescaleHp(mecha, () =>
                            history.globalHpEnhancement = (float)Round3Trunc(history.globalHpEnhancement - (float)value));
                    }

                    break;

                case 85:
                    history.enemyDropScale -= (float)value;
                    break;

                case 86: // one-way flag
                    TechDowngrade.MarkDirty(func);
                    break;

                case 99: // one-way flag
                    TechDowngrade.MarkDirty(func);
                    break;

                default:
                    // Unknown / newly added function id: warn instead of guessing. Only once per id -
                    // a downgrade can walk thousands of levels and must not flood the log.
                    if (UnknownFuncsWarned.Add(func))
                    {
                        TechLog.Warn($"revert: no inverse known for tech function {func} (value={value}, level={level}); " +
                                   "the field was left untouched. A game update may have added a new function.");
                    }

                    break;
            }
        }

        /// <summary>
        /// Recomputes the functions that cannot be inverted step by step. Called once per downgrade
        /// click after every level has been reverted.
        /// </summary>
        public static void Recompute(GameHistoryData history, int func)
        {
            Player player = GameMain.mainPlayer;
            Mecha mecha = player != null ? player.mecha : null;
            ModeConfig freeMode = Configs.freeMode;
            if (freeMode == null)
            {
                TechLog.Warn($"recompute of tech function {func} skipped: Configs.freeMode is not available");
                return;
            }

            switch (func)
            {
                case 5:
                case 38:
                    ReconcilePackage(history);
                    break;

                case 4:
                    if (mecha != null)
                    {
                        mecha.thrusterLevel = Math.Max(freeMode.mechaThrusterLevel,
                            PickMax(history, func, freeMode.mechaThrusterLevel));
                    }

                    break;

                case 6:
                    if (mecha != null)
                    {
                        // func 6 assigns `coreLevel = level` (the tech's own level), not the value.
                        mecha.coreLevel = Math.Max(freeMode.mechaCoreLevel, PickMaxLevel(history, func));
                    }

                    break;

                case 8:
                    history.useIonLayer = IsGrantedByAnyTech(history, func) || freeMode.useIonLayer;
                    break;

                case 13:
                    history.solarEnergyLossRate = freeMode.solarEnergyLossRate * ProductOfAppliedValues(history, func);
                    break;

                case 14:
                    history.inserterStackCountObsolete = Math.Max(BaseInserterStackCountObsolete,
                        PickMax(history, func, BaseInserterStackCountObsolete));
                    break;

                case 17:
                    // Never revoke the warp capability from stations that already have warpers: only the
                    // history flag is recalculated. `SetAllStationsMaxWarperCount` has no inverse that
                    // would not destroy existing warpers.
                    history.logisticShipWarpDrive = IsGrantedByAnyTech(history, func) || freeMode.logisticShipWarpDrive;
                    break;

                case 20:
                    float costRate = freeMode.miningCostRate * ProductOfAppliedValues(history, func);
                    // Mirrors the clamp inside GameHistoryData.UnlockTechFunction case 20.
                    if (costRate < TechProto.MINING_COST_CRITICAL_POINT)
                    {
                        costRate = 0f;
                    }

                    history.miningCostRate = costRate;
                    break;

                case 23:
                    history.universeObserveLevel = Math.Max(freeMode.universeObserveLevel,
                        PickMax(history, func, freeMode.universeObserveLevel));
                    break;

                case 28:
                    history.blueprintLimit = Math.Max(freeMode.blueprintLimit,
                        PickMax(history, func, freeMode.blueprintLimit));
                    break;

                case 37:
                    RecomputeFeatureKeys(history);
                    break;

                case 39:
                    history.inserterStackOutput = Math.Max(BaseInserterStackOutput,
                        PickMax(history, func, BaseInserterStackOutput));
                    break;

                case 40:
                    history.inserterBidirectional = IsGrantedByAnyTech(history, func);
                    break;

                case 41:
                    history.inserterStackInput = Math.Max(BaseInserterStackInput,
                        PickMax(history, func, BaseInserterStackInput));
                    break;

                case 42:
                    history.beltVerticalConstruction = IsGrantedByAnyTech(history, func);
                    break;

                case 43:
                    // A shorter absorption interval is the upgrade, so the smallest granted value wins.
                    history.dysonNodeAbsorbInterval = PickMin(history, func, BaseDysonNodeAbsorbInterval);
                    break;

                case 44:
                    history.bpReformLimit = PickMax(history, func, 0);
                    break;

                case 45:
                    history.manualPlantVegetation = IsGrantedByAnyTech(history, func);
                    break;

                case 74:
                    RecomputePlanetaryAtField(history, freeMode);
                    break;

                case 75:
                    if (mecha != null)
                    {
                        mecha.energyShieldUnlocked = IsGrantedByAnyTech(history, func) || freeMode.unlockEnergyShield;
                    }

                    break;

                case 76:
                    int speed = 0;
                    ForEachAppliedFunction(history, func, (proto, lastApplied, value) =>
                        speed = Math.Max(speed, (int)value));
                    history.autoReconstructSpeed = speed;
                    break;

                case 82:
                case 83:
                    RecomputeLaser(history, freeMode, mecha, func);
                    break;

                case 86:
                    if (mecha != null)
                    {
                        mecha.energyShieldBurstUnlocked =
                            IsGrantedByAnyTech(history, func) || freeMode.unlockEnergyShieldBurst;
                    }

                    break;

                case 99:
                    history.missionAccomplished = IsGrantedByAnyTech(history, func);
                    break;
            }
        }

        /// <summary>
        /// func 74 keeps the field expressed as <c>base / ratio</c>; the ratio grows by <c>value</c> per
        /// level and is rounded to two decimals each time. Rebuilding it from <c>1 + sum(values)</c>
        /// reproduces exactly what the sequence of forward operations produced, because the rounding is
        /// a no-op for two-decimal increments.
        /// </summary>
        private static void RecomputePlanetaryAtField(GameHistoryData history, ModeConfig freeMode)
        {
            double ratio = 1.0;
            ForEachAppliedFunction(history, 74, (proto, lastApplied, value) =>
                ratio += value * (lastApplied - proto.Level + 1));
            ratio = Math.Round(ratio, 2);
            if (ratio <= 0.0)
            {
                ratio = 1.0;
            }

            long rate = (long)((double)freeMode.planetaryATFieldEnergyRate / ratio + 0.5);
            history.planetaryATFieldEnergyRate = Math.Max(1L, rate);
        }

        /// <summary>
        /// func 37 registers <c>1600000 + value</c> as a feature key. A key is kept while any tech that
        /// grants it still has a level applied, and removed otherwise.
        /// </summary>
        private static void RecomputeFeatureKeys(GameHistoryData history)
        {
            var allKeys = new HashSet<int>();
            var stillGranted = new HashSet<int>();
            TechProto[] all = LDB.techs.dataArray;
            for (int i = 0; i < all.Length; i++)
            {
                TechProto proto = all[i];
                if (proto?.UnlockFunctions == null)
                {
                    continue;
                }

                for (int j = 0; j < proto.UnlockFunctions.Length; j++)
                {
                    if (proto.UnlockFunctions[j] != 37)
                    {
                        continue;
                    }

                    int key = 1600000 + ToInt(proto.UnlockValues[j]);
                    allKeys.Add(key);
                    if (TechDowngrade.HasAnyAppliedLevel(history, proto))
                    {
                        stillGranted.Add(key);
                    }
                }
            }

            foreach (int key in allKeys)
            {
                if (stillGranted.Contains(key))
                {
                    history.RegFeatureKey(key);
                }
                else
                {
                    history.UnregFeatureKey(key);
                }
            }
        }

        private static float ProductOfAppliedValues(GameHistoryData history, int func)
        {
            float product = 1f;
            ForEachAppliedFunction(history, func, (proto, lastApplied, value) =>
            {
                int count = lastApplied - proto.Level + 1;
                for (int i = 0; i < count; i++)
                {
                    product *= (float)value;
                }
            });
            return product;
        }

        private static int PickMax(GameHistoryData history, int func, int fallback)
        {
            int best = fallback;
            bool found = false;
            ForEachAppliedFunction(history, func, (proto, lastApplied, value) =>
            {
                int candidate = ToInt(value);
                if (!found || candidate > best)
                {
                    best = candidate;
                    found = true;
                }
            });
            return found ? best : fallback;
        }

        private static int PickMin(GameHistoryData history, int func, int fallback)
        {
            int best = fallback;
            bool found = false;
            ForEachAppliedFunction(history, func, (proto, lastApplied, value) =>
            {
                int candidate = ToInt(value);
                if (!found || candidate < best)
                {
                    best = candidate;
                    found = true;
                }
            });
            return found ? best : fallback;
        }

        /// <summary>Largest "last applied level" among all techs granting <paramref name="func"/>.</summary>
        private static int PickMaxLevel(GameHistoryData history, int func)
        {
            int best = 0;
            ForEachAppliedFunction(history, func, (proto, lastApplied, value) =>
            {
                if (lastApplied > best)
                {
                    best = lastApplied;
                }
            });
            return best;
        }

        private static bool IsGrantedByAnyTech(GameHistoryData history, int func)
        {
            bool found = false;
            ForEachAppliedFunction(history, func, (proto, lastApplied, value) => found = true);
            return found;
        }

        internal static void ForEachAppliedFunction(GameHistoryData history, int func, Action<TechProto, int, double> visit,
            IReadOnlyDictionary<int, int> targets = null)
        {
            if (history.techStates == null)
            {
                return;
            }

            TechProto[] all = LDB.techs.dataArray;
            for (int i = 0; i < all.Length; i++)
            {
                TechProto proto = all[i];
                if (proto?.UnlockFunctions == null)
                {
                    continue;
                }

                int lastApplied;
                if (targets != null && targets.TryGetValue(proto.ID, out int target))
                {
                    lastApplied = target;
                    if (lastApplied < proto.Level) continue;
                }
                else if (!TechDowngrade.TryGetLastAppliedLevel(history, proto, out lastApplied))
                {
                    continue;
                }

                for (int j = 0; j < proto.UnlockFunctions.Length; j++)
                {
                    if (proto.UnlockFunctions[j] == func)
                    {
                        visit(proto, lastApplied, proto.UnlockValues[j]);
                    }
                }
            }
        }

        /// <summary>
        /// func 81 and 84 keep the current HP ratio while <c>hpMaxApplied</c> changes.
        /// </summary>
        private static void RescaleHp(Mecha mecha, Action changeMaxUpgrade)
        {
            int maxApplied = mecha.hpMaxApplied;
            float ratio = maxApplied > 0 ? (float)mecha.hp / maxApplied : 1f;
            changeMaxUpgrade();
            maxApplied = mecha.hpMaxApplied;
            mecha.hp = (int)(maxApplied * ratio + 0.5f);
            if (mecha.hp > maxApplied)
            {
                mecha.hp = maxApplied;
            }

            mecha.hp = Math.Max(0, mecha.hp);
        }

        /// <summary>
        /// Both ranges use the factor computed from the local range. Damage and energy use native
        /// integer rounding on every level, so division cannot recover the previous values exactly.
        /// </summary>
        private static void RecomputeLaser(GameHistoryData history, ModeConfig config, Mecha mecha, int func)
        {
            if (mecha == null)
            {
                return;
            }

            if (func == 82)
            {
                mecha.laserLocalAttackRange = config.mechaLocalLaserAttackRange;
                mecha.laserSpaceAttackRange = config.mechaSpaceLaserAttackRange;
            }
            else
            {
                mecha.laserLocalDamage = config.mechaLocalLaserDamage;
                mecha.laserSpaceDamage = config.mechaSpaceLaserDamage;
                mecha.laserEnergyCapacity = config.mechaLaserEnergyCapacity;
                mecha.laserLocalEnergyCost = config.mechaLocalLaserEnergyCost;
                mecha.laserSpaceEnergyCost = config.mechaSpaceLaserEnergyCost;
            }

            ForEachAppliedFunction(history, func, (proto, lastApplied, value) =>
            {
                for (int level = proto.Level; level <= lastApplied; level++)
                {
                    if (func == 82)
                    {
                        float factor = 1f + (float)(value / mecha.laserLocalAttackRange);
                        mecha.laserLocalAttackRange *= factor;
                        mecha.laserSpaceAttackRange *= factor;
                    }
                    else
                    {
                        float factor = 1f + (float)ToInt(value) / mecha.laserLocalDamage;
                        mecha.laserLocalDamage = (int)(mecha.laserLocalDamage * factor + 0.5f);
                        mecha.laserSpaceDamage = (int)(mecha.laserSpaceDamage * factor + 0.5f);
                        mecha.laserEnergyCapacity = (int)(mecha.laserEnergyCapacity * factor + 0.5f);
                        mecha.laserLocalEnergyCost = (int)(mecha.laserLocalEnergyCost * factor + 0.5f);
                        mecha.laserSpaceEnergyCost = (int)(mecha.laserSpaceEnergyCost * factor + 0.5f);
                    }
                }
            });
            if (func == 83) mecha.laserEnergy = Math.Min(mecha.laserEnergy, mecha.laserEnergyCapacity);
        }

        /// <summary>Rejects the whole cascade before any state changes if capacity would hide live contents.</summary>
        internal static bool CanRevert(GameHistoryData history, IReadOnlyDictionary<int, int> targets)
        {
            Player player = GameMain.mainPlayer;
            if (Configs.freeMode == null || history.gameData == null || player?.mecha == null ||
                player.package?.grids == null || player.deliveryPackage?.grids == null)
            {
                TechLog.Warn("downgrade skipped: game data is not ready");
                return false;
            }

            var removed = new Dictionary<int, int>();
            bool deliveryUnlockRemoved = false;
            foreach (var pair in targets)
            {
                TechProto proto = LDB.techs.Select(pair.Key);
                int count = TechDowngrade.GetLastAppliedLevel(history.techStates[pair.Key]) - pair.Value;
                deliveryUnlockRemoved |= pair.Value < proto.Level &&
                    TechDowngrade.Contains(proto.UnlockRecipes, TechDowngrade.DeliveryPackageRecipeId);
                for (int j = 0; j < proto.UnlockFunctions.Length; j++)
                {
                    int func = proto.UnlockFunctions[j];
                    if (!(func is >= 1 and <= 45 or >= 61 and <= 86 or 99))
                    {
                        TechLog.Warn($"downgrade skipped: unsupported tech function {func} in tech {proto.ID}");
                        UIRealtimeTip.Popup(Localization.TechDowngradeUnsupportedFunction.Translate());
                        return false;
                    }

                    int value = func is >= 77 and <= 80 ? (int)proto.UnlockValues[j] : ToInt(proto.UnlockValues[j]);
                    removed.TryGetValue(func, out int previous);
                    removed[func] = previous + value * count;
                }
            }

            int packageCols = player.GetPackageColumnCount();
            int packageRows = (player.package.size - 1) / packageCols + 1;
            bool resizePackage = removed.ContainsKey(5) || removed.ContainsKey(38);
            if (resizePackage)
            {
                GetPackageGeometry(history, targets, out packageCols, out packageRows);
                if (!CanResizePackage(player, packageCols, packageRows))
                {
                    UIRealtimeTip.Popup(Localization.PackageResizeSkippedOccupiedCells.Translate());
                    return false;
                }
            }

            DeliveryPackage delivery = player.deliveryPackage;
            removed.TryGetValue(32, out int deliveryColsRemoved);
            int deliveryCols = Math.Max(0, delivery.colCount - deliveryColsRemoved);
            int deliveryRows = resizePackage ? Math.Min(DeliveryPackage.MAX_ROWCOUNT, packageRows) : delivery.rowCount;
            bool deliveryLocked = deliveryUnlockRemoved &&
                !TechDowngrade.IsRecipeGranted(history, TechDowngrade.DeliveryPackageRecipeId, targets);
            for (int i = 0; i < delivery.grids.Length; i++)
            {
                if (!delivery.IsGridActive(i) || (!deliveryLocked &&
                    i / DeliveryPackage.MAX_COLCOUNT < deliveryRows &&
                    DeliveryPackage.MAX_COLCOUNT - 1 - i % DeliveryPackage.MAX_COLCOUNT < deliveryCols)) continue;
                if (delivery.grids[i].count > 0 || delivery.grids[i].ordered != 0)
                {
                    UIRealtimeTip.Popup(Localization.TechDowngradeDeliveryOccupied.Translate());
                    return false;
                }
            }

            removed.TryGetValue(1, out int dronesRemoved);
            if (dronesRemoved > player.mecha.constructionModule.droneIdleCount)
            {
                UIRealtimeTip.Popup(Localization.TechDowngradeDronesBusy.Translate());
                return false;
            }

            removed.TryGetValue(77, out int groundFleetsRemoved);
            removed.TryGetValue(79, out int spaceFleetsRemoved);
            if (!CanRemoveFleets(player.mecha.groundCombatModule, groundFleetsRemoved) ||
                !CanRemoveFleets(player.mecha.spaceCombatModule, spaceFleetsRemoved))
            {
                UIRealtimeTip.Popup(Localization.TechDowngradeFleetsOccupied.Translate());
                return false;
            }

            return true;
        }

        private static bool CanRemoveFleets(CombatModuleComponent module, int removed)
        {
            if (removed <= 0 || module?.moduleFleets == null) return true;
            int target = Math.Max(0, module.fleetCount - removed);
            for (int i = target; i < Math.Min(module.fleetCount, module.moduleFleets.Length); i++)
            {
                ModuleFleet fleet = module.moduleFleets[i];
                if (fleet.fleetId != 0) return false;
                if (fleet.fighters == null) continue;
                foreach (ModuleFighter fighter in fleet.fighters)
                {
                    if (fighter.count > 0 || fighter.craftId != 0) return false;
                }
            }

            return true;
        }

        internal static void ReconcileStationStorage(GameHistoryData history, int localBefore, int remoteBefore)
        {
            int localDelta = history.localStationExtraStorage - localBefore;
            int remoteDelta = history.remoteStationExtraStorage - remoteBefore;
            if (localDelta == 0 && remoteDelta == 0) return;
            GameData data = history.gameData;
            for (int i = 0; i < data.factoryCount; i++)
            {
                PlanetTransport transport = data.factories[i]?.transport;
                if (transport == null) continue;
                // Native scaling uses the already updated capacity and the signed change. Passing a
                // negative delta restores the configured station limits without removing stored items.
                if (localDelta != 0) transport.OnTechFunctionUnlocked(30, localDelta, 0);
                if (remoteDelta != 0) transport.OnTechFunctionUnlocked(31, remoteDelta, 0);
            }
        }

        private static void GetPackageGeometry(GameHistoryData history, IReadOnlyDictionary<int, int> targets,
            out int columns, out int rows)
        {
            const int baseColumns = 10;
            int baseSize = Configs.freeMode.playerPackageSize;
            int columnCount = baseColumns;
            int rowCount = (baseSize - 1) / baseColumns + 1;
            ForEachAppliedFunction(history, 38, (proto, last, value) =>
                columnCount += ToInt(value) * (last - proto.Level + 1), targets);
            ForEachAppliedFunction(history, 5, (proto, last, value) =>
                rowCount += ToInt(value) * (last - proto.Level + 1), targets);
            columns = columnCount;
            rows = rowCount;
        }

        private static bool CanResizePackage(Player player, int columns, int rows)
        {
            StorageComponent package = player.package;
            int oldColumns = player.GetPackageColumnCount();
            for (int i = 0; i < package.size; i++)
            {
                if (package.grids[i].itemId != 0 && (i / oldColumns >= rows || i % oldColumns >= columns))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Native SetSize cannot shrink columns safely. Preserve each retained cell's position and
        /// metadata; CanRevert has already checked every cell that the new layout would remove.
        /// </summary>
        private static void ReconcilePackage(GameHistoryData history)
        {
            Player player = GameMain.mainPlayer;
            GetPackageGeometry(history, null, out int targetCols, out int targetRows);
            int targetSize = targetRows * targetCols;
            StorageComponent package = player.package;
            int oldCols = player.GetPackageColumnCount();
            if (oldCols == targetCols && package.size == targetSize) return;

            var newGrid = new StorageComponent.GRID[targetSize];
            for (int i = 0; i < package.size; i++)
            {
                int row = i / oldCols;
                int col = i % oldCols;
                if (row < targetRows && col < targetCols) newGrid[row * targetCols + col] = package.grids[i];
            }

            player.packageColCount = targetCols;
            package.grids = newGrid;
            package.size = targetSize;
            package.SetBans(Math.Min(package.bans, targetSize));
            package.ResetOptimizationFlags();
            package.NotifyStorageChange();
            package.NotifyStorageSizeChange();
        }
    }
}
