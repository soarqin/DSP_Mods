using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheatEnabler.Patches.Tech
{
    /// <summary>
    /// F4 - the downgrade side of <c>GameHistoryData.UnlockTechFunction</c>.
    ///
    /// <para><b>Chosen approach: A (per-function inverse operation), with recomputation for the
    /// functions where an inverse is either impossible or not drift free.</b></para>
    ///
    /// <para>Why A rather than B (reset to base plus full replay): a replay has to re-run every side
    /// effect of <c>UnlockTechFunction</c> on the live game objects. Several of those are ratchets that
    /// cannot be replayed safely - <c>func 30/31</c> rescale existing logistics station storage
    /// proportionally through <c>PlanetTransport.OnTechFunctionUnlocked</c>, and <c>func 5/38</c> resize
    /// the player's package. Replaying them would double-apply the scaling instead of restoring it.
    /// Inverse operations touch exactly the field that was changed and nothing else, and they need no
    /// snapshot, so they also survive save/load.</para>
    ///
    /// <para>Where an inverse is not available the value is recomputed from
    /// <c>Configs.freeMode</c> - the very table <c>GameHistoryData.SetForNewGame</c> uses to
    /// initialise the field - plus the set of levels still applied across all techs. That covers the
    /// multiplicative functions (no float drift, and <c>func 20</c> recovers from the game's 0 clamp,
    /// which a division can never undo), the assignment functions (a plain subtraction is wrong when
    /// the value came from a different tech) and the boolean / one-way functions (there is no
    /// "unset" call in the game).</para>
    ///
    /// <para>Every case of the original switch (1-45, 61-86, 99 - ids 46-60 and 87-98 do not exist)
    /// is handled below or explicitly delegated to <see cref="Recompute"/>.</para>
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
                    // PlanetTransport.OnTechFunctionUnlocked rescaled every local station's storage when
                    // this was applied; shrinking the *count* back is the inverse the game itself uses
                    // when the field is lowered, so no extra rescale is performed here.
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
                    history.dispenserDeliveryMaxAngle = Math.Max(0f, history.dispenserDeliveryMaxAngle - (float)value);
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
                        mecha.groundCombatModule.fleetCount = Math.Max(0, mecha.groundCombatModule.fleetCount - num);
                    }

                    break;

                case 78:
                    history.groundFleetPortCount = Math.Max(0, history.groundFleetPortCount - num);
                    break;

                case 79:
                    if (mecha != null && mecha.spaceCombatModule != null)
                    {
                        mecha.spaceCombatModule.fleetCount = Math.Max(0, mecha.spaceCombatModule.fleetCount - num);
                    }

                    break;

                case 80:
                    history.spaceFleetPortCount = Math.Max(0, history.spaceFleetPortCount - num);
                    break;

                case 81:
                    if (mecha != null)
                    {
                        RescaleHp(mecha, () => mecha.hpMaxUpgrade = Math.Max(0, mecha.hpMaxUpgrade - num));
                    }

                    break;

                case 82:
                    // The forward operation is `range *= 1 + value / range`, which is exactly
                    // `range += value`; the inverse is therefore a plain subtraction.
                    if (mecha != null)
                    {
                        mecha.laserLocalAttackRange = Math.Max(0f, mecha.laserLocalAttackRange - (float)value);
                        mecha.laserSpaceAttackRange = Math.Max(0f, mecha.laserSpaceAttackRange - (float)value);
                    }

                    break;

                case 83:
                    RevertLaserDamage(mecha, num);
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
                    ReconcilePackage(history, freeMode);
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
                    history.autoReconstructSpeed = PickMax(history, func, 0);
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

        internal static void ForEachAppliedFunction(GameHistoryData history, int func, Action<TechProto, int, double> visit)
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

                if (!TechDowngrade.TryGetLastAppliedLevel(history, proto, out int lastApplied))
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

            if (mecha.hp < 1)
            {
                mecha.hp = 1;
            }
        }

        /// <summary>
        /// func 83 scales every laser stat by <c>1 + num / laserLocalDamage</c> measured *before* the
        /// change. The integer rounding makes the transform lossy, so the previous damage value is
        /// recovered as <c>laserLocalDamage - num</c> and the other stats are divided by the same
        /// factor.
        /// </summary>
        private static void RevertLaserDamage(Mecha mecha, int num)
        {
            if (mecha == null)
            {
                return;
            }

            int damageAfter = mecha.laserLocalDamage;
            int damageBefore = damageAfter - num;
            if (damageBefore < 1)
            {
                damageBefore = 1;
            }

            float inverseFactor = damageAfter > 0 ? (float)damageBefore / damageAfter : 1f;
            mecha.laserLocalDamage = damageBefore;
            mecha.laserSpaceDamage = (int)(mecha.laserSpaceDamage * inverseFactor + 0.5f);
            mecha.laserEnergyCapacity = (int)(mecha.laserEnergyCapacity * inverseFactor + 0.5f);
            mecha.laserLocalEnergyCost = (int)(mecha.laserLocalEnergyCost * inverseFactor + 0.5f);
            mecha.laserSpaceEnergyCost = (int)(mecha.laserSpaceEnergyCost * inverseFactor + 0.5f);
        }

        /// <summary>
        /// func 5 and func 38 both resize the player's package: func 5 adds whole rows
        /// (<c>SetSize(size + num * colCount)</c>) and func 38 changes the column count, which re-lays-out
        /// every row.
        ///
        /// Neither can be undone by simply calling the game's resize in reverse:
        /// <c>StorageComponent.SetSize(int, int, int)</c> iterates the *whole* backing array and computes
        /// <c>row * newcol + col</c>, so shrinking the column count runs past the new size and throws
        /// <c>IndexOutOfRangeException</c> (verified in game). The geometry is therefore recomputed from
        /// the base values and the levels still applied, and the grid is re-laid-out cell by cell.
        ///
        /// If an occupied cell would fall outside the target layout the resize is refused and reported -
        /// no item is ever destroyed, and the tech level is still reverted.
        /// </summary>
        private static void ReconcilePackage(GameHistoryData history, ModeConfig freeMode)
        {
            Player player = GameMain.mainPlayer;
            if (player?.package == null)
            {
                return;
            }

            // Player.SetForNewGame(): packageColCount = 10, package = Configs.freeMode.playerPackageSize cells.
            const int baseCols = 10;
            int baseSize = freeMode.playerPackageSize;
            if (baseSize <= 0)
            {
                baseSize = baseCols * 4;
            }

            int baseRows = (baseSize - 1) / baseCols + 1;
            int targetCols = baseCols;
            int targetRows = baseRows;

            // Each applied level adds its value: func 38 to the column count, func 5 to the row count.
            ForEachAppliedFunction(history, 38, (proto, lastApplied, value) =>
                targetCols += ToInt(value) * (lastApplied - proto.Level + 1));
            ForEachAppliedFunction(history, 5, (proto, lastApplied, value) =>
                targetRows += ToInt(value) * (lastApplied - proto.Level + 1));

            if (targetCols < baseCols)
            {
                targetCols = baseCols;
            }

            if (targetRows < baseRows)
            {
                targetRows = baseRows;
            }

            int targetSize = targetRows * targetCols;

            StorageComponent package = player.package;
            StorageComponent.GRID[] oldGrid = package.grids;
            if (oldGrid == null)
            {
                return;
            }

            int oldCols = player.packageColCount < 1 ? baseCols : player.packageColCount;
            int oldSize = package.size;
            if (oldSize <= 0)
            {
                return;
            }

            int oldRows = (oldSize - 1) / oldCols + 1;
            if (oldCols == targetCols && oldSize == targetSize)
            {
                return;
            }

            for (int r = 0; r < oldRows; r++)
            {
                for (int c = 0; c < oldCols; c++)
                {
                    int index = r * oldCols + c;
                    if (index >= oldGrid.Length || index >= oldSize || oldGrid[index].itemId == 0)
                    {
                        continue;
                    }

                    if (r < targetRows && c < targetCols)
                    {
                        continue;
                    }

                    TechLog.Warn($"package resize skipped: cell {index} (row {r}, column {c}) is not empty and would " +
                               "fall outside the new layout, so no item is destroyed. The tech level was still reverted.");
                    UIRealtimeTip.Popup(Localization.PackageResizeSkippedOccupiedCells.Translate());
                    return;
                }
            }

            var newGrid = new StorageComponent.GRID[targetSize];
            for (int r = 0; r < oldRows; r++)
            {
                for (int c = 0; c < oldCols; c++)
                {
                    if (r >= targetRows || c >= targetCols)
                    {
                        continue;
                    }

                    int from = r * oldCols + c;
                    if (from >= oldGrid.Length || from >= oldSize)
                    {
                        continue;
                    }

                    newGrid[r * targetCols + c] = oldGrid[from];
                }
            }

            player.packageColCount = targetCols;
            package.grids = newGrid;
            package.size = targetSize;
            package.searchStart = 0;
            package.ResetOptimizationFlags();
            package.NotifyStorageChange();
            package.NotifyStorageSizeChange();
        }
    }
}
