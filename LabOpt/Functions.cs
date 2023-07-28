using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace LabOpt;

public class LabOptPatchFunctions
{
    private static readonly FieldInfo RootLabIdField = AccessTools.Field(typeof(LabComponent), "rootLabId");
    private const int RequireCountForAssemble = 15;
    private const int RequireCountForResearch = 54000;

    public static void SetRootLabIdForStacking(FactorySystem factorySystem, int labId, int nextEntityId)
    {
        var rootId = (int)RootLabIdField.GetValue(factorySystem.labPool[labId]);
        var targetLabId = factorySystem.factory.entityPool[nextEntityId].labId;
        if (rootId <= 0) rootId = labId;
        RootLabIdField.SetValueDirect(__makeref(factorySystem.labPool[targetLabId]), rootId);
        LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {targetLabId} to {rootId}");
    }

    public static void SetRootLabIdOnLoading(FactorySystem factorySystem)
    {
        var labCursor = factorySystem.labCursor;
        var labPool = factorySystem.labPool;
        var parentDict = new Dictionary<int, int>();
        for (var id = 1; id < labCursor; id++)
        {
            if (labPool[id].id != id) continue;
            ref var lab = ref labPool[id];
            if (lab.researchMode)
            {
                var len = lab.matrixIncServed.Length;
                for (var i = 0; i < len; i++)
                {
                    if (lab.matrixIncServed[i] < 0)
                    {
                        lab.matrixIncServed[i] = 0;
                    }
                }
            }
            else
            {
                var len = lab.incServed.Length;
                for (var i = 0; i < len; i++)
                {
                    if (lab.incServed[i] < 0)
                    {
                        lab.incServed[i] = 0;
                    }
                }
            }
            if (lab.nextLabId != 0) parentDict[lab.nextLabId] = id;
        }

        foreach (var pair in parentDict)
        {
            var rootId = pair.Value;
            while (parentDict.TryGetValue(rootId, out var parentId)) rootId = parentId;
            RootLabIdField.SetValueDirect(__makeref(labPool[pair.Key]), rootId);

            ref var rootLab = ref labPool[rootId];
            ref var thisLab = ref labPool[pair.Key];
            int len;
            if (rootLab.researchMode)
            {
                len = Math.Min(rootLab.matrixServed.Length, thisLab.matrixServed.Length);
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.matrixServed[i] != 0)
                    {
                        rootLab.matrixServed[i] += thisLab.matrixServed[i];
                        thisLab.matrixServed[i] = 0;
                    }
                    if (thisLab.matrixIncServed[i] != 0)
                    {
                        rootLab.matrixIncServed[i] += thisLab.matrixIncServed[i];
                        thisLab.matrixIncServed[i] = 0;
                    }
                }
            }
            else
            {
                len = Math.Min(rootLab.produced.Length, thisLab.produced.Length);
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.produced[i] == 0) continue;
                    rootLab.produced[i] += thisLab.produced[i];
                    thisLab.produced[i] = 0;
                }
                len = Math.Min(rootLab.served.Length, thisLab.served.Length);
                for (var i = 0; i < len; i++)
                {
                    if (thisLab.served[i] != 0)
                    {
                        rootLab.served[i] += thisLab.served[i];
                        thisLab.served[i] = 0;
                    }
                    if (thisLab.incServed[i] != 0)
                    {
                        rootLab.incServed[i] += thisLab.incServed[i];
                        thisLab.incServed[i] = 0;
                    }
                }
            }

            LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {pair.Key} to {rootId}");
        }
    }

    public static uint InternalUpdateAssembleNew(ref LabComponent lab, float power, int[] productRegister, int[] consumeRegister, LabComponent[] labPool)
    {
        if (power < 0.1f)
        {
            return 0U;
        }

        var extraPassed = lab.extraTime >= lab.extraTimeSpend;
        var timePassed = lab.time >= lab.timeSpend;
        if (extraPassed || timePassed || !lab.replicating)
        {
            var rootLabId = (int)RootLabIdField.GetValue(lab);
            ref var rootLab = ref rootLabId > 0 ? ref labPool[rootLabId] : ref lab;
            if (extraPassed)
            {
                int len = lab.products.Length;
                lock (rootLab.produced)
                {
                    if (len == 1)
                    {
                        rootLab.produced[0] += lab.productCounts[0];
                        lock (productRegister)
                        {
                            productRegister[lab.products[0]] += lab.productCounts[0];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < len; i++)
                        {
                            rootLab.produced[i] += lab.productCounts[i];
                            lock (productRegister)
                            {
                                productRegister[lab.products[i]] += lab.productCounts[i];
                            }
                        }
                    }
                }

                lab.extraTime -= lab.extraTimeSpend;
            }

            if (timePassed)
            {
                lab.replicating = false;
                int len = lab.products.Length;
                lock (rootLab.produced)
                {
                    if (len == 1)
                    {
                        if (rootLab.produced[0] + lab.productCounts[0] > 30)
                        {
                            return 0U;
                        }

                        rootLab.produced[0] += lab.productCounts[0];
                        lock (productRegister)
                        {
                            productRegister[lab.products[0]] += lab.productCounts[0];
                        }
                    }
                    else
                    {
                        for (int j = 0; j < len; j++)
                        {
                            if (rootLab.produced[j] + lab.productCounts[j] > 30)
                            {
                                return 0U;
                            }
                        }

                        for (int k = 0; k < len; k++)
                        {
                            rootLab.produced[k] += lab.productCounts[k];
                            lock (productRegister)
                            {
                                productRegister[lab.products[k]] += lab.productCounts[k];
                            }
                        }
                    }
                }

                lab.extraSpeed = 0;
                lab.speedOverride = lab.speed;
                lab.extraPowerRatio = 0;
                lab.time -= lab.timeSpend;
            }

            if (!lab.replicating)
            {
                int len = lab.requireCounts.Length;
                int incLevel;
                if (len > 0)
                {
                    var served = rootLab.served;
                    lock (served)
                    {
                        for (int l = 0; l < len; l++)
                        {
                            if (served[l] < lab.requireCounts[l] || served[l] == 0)
                            {
                                lab.time = 0;
                                return 0U;
                            }
                        }

                        incLevel = 10;
                        for (int m = 0; m < len; m++)
                        {
                            int splittedIncLevel = lab.split_inc_level(ref served[m], ref rootLab.incServed[m], lab.requireCounts[m]);
                            if (splittedIncLevel < incLevel) incLevel = splittedIncLevel;
                            if (served[m] == 0)
                            {
                                rootLab.incServed[m] = 0;
                            }
                            rootLab.needs[m] = served[m] < RequireCountForAssemble ? rootLab.requires[0] : 0;
                            lock (consumeRegister)
                            {
                                consumeRegister[lab.requires[m]] += lab.requireCounts[m];
                            }
                        }

                        if (incLevel < 0)
                        {
                            incLevel = 0;
                        }
                    }
                }
                else
                {
                    incLevel = 0;
                }

                if (lab.productive && !lab.forceAccMode)
                {
                    lab.extraSpeed = (int)((double)lab.speed * Cargo.incTableMilli[incLevel] * 10.0 + 0.1);
                    lab.speedOverride = lab.speed;
                    lab.extraPowerRatio = Cargo.powerTable[incLevel];
                }
                else
                {
                    lab.extraSpeed = 0;
                    lab.speedOverride = (int)((double)lab.speed * (1.0 + Cargo.accTableMilli[incLevel]) + 0.1);
                    lab.extraPowerRatio = Cargo.powerTable[incLevel];
                }

                lab.replicating = true;
            }
        }

        if (lab.replicating && lab.time < lab.timeSpend && lab.extraTime < lab.extraTimeSpend)
        {
            lab.time += (int)(power * (float)lab.speedOverride);
            lab.extraTime += (int)(power * (float)lab.extraSpeed);
        }

        if (!lab.replicating)
        {
            return 0U;
        }

        return (uint)(lab.products[0] - LabComponent.matrixIds[0] + 1);
    }

    public static uint InternalUpdateResearchNew(ref LabComponent lab, float power, float speed, int[] consumeRegister, ref TechState ts, ref int techHashedThisFrame, ref long uMatrixPoint, ref long hashRegister, LabComponent[] labPool)
    {
        if (power < 0.1f)
        {
            return 0U;
        }
        var rootLabId = (int)RootLabIdField.GetValue(lab);
        ref var rootLab = ref rootLabId > 0 ? ref labPool[rootLabId] : ref lab;

        int multiplier = (int)(speed + 2f);
        var matrixServed = rootLab.matrixServed;
        for (var i = 0; i < 6; i++)
        {
            if (lab.matrixPoints[i] <= 0) continue;
            int mult = matrixServed[i] / lab.matrixPoints[i];
            if (mult < multiplier)
            {
                multiplier = mult;
                if (multiplier == 0)
                {
                    lab.replicating = false;
                    return 0U;
                }
            }
        }

        lab.replicating = true;
        if (multiplier < speed) speed = multiplier;
        int hashBytes = (int)(power * 10000f * speed + 0.5f);
        lab.hashBytes += hashBytes;
        long count = lab.hashBytes / 10000;
        lab.hashBytes -= (int)count * 10000;
        long maxNeeded = ts.hashNeeded - ts.hashUploaded;
        if (maxNeeded < count) count = maxNeeded;
        if (multiplier < count) count = multiplier;
        int icount = (int)count;
        if (icount > 0)
        {
            int len = matrixServed.Length;
            int incLevel = ((len == 0) ? 0 : 10);
            for (int i = 0; i < len; i++)
            {
                if (lab.matrixPoints[i] <= 0) continue;
                int matrixBefore = matrixServed[i] / 3600;
                int splittedIncLevel = lab.split_inc_level(ref matrixServed[i], ref rootLab.matrixIncServed[i], lab.matrixPoints[i] * icount);
                incLevel = incLevel < splittedIncLevel ? incLevel : splittedIncLevel;
                if (matrixServed[i] <= 0)
                {
                    rootLab.matrixIncServed[i] = 0;
                }
                rootLab.needs[i] = matrixServed[i] < RequireCountForResearch ? LabComponent.matrixIds[i] : 0;
                consumeRegister[LabComponent.matrixIds[i]] += matrixBefore - matrixServed[i] / 3600;
            }

            if (incLevel < 0)
            {
                incLevel = 0;
            }

            lab.extraSpeed = (int)(10000.0 * Cargo.incTableMilli[incLevel] * 10.0 + 0.1);
            lab.extraPowerRatio = Cargo.powerTable[incLevel];
            lab.extraHashBytes += (int)(power * lab.extraSpeed * speed + 0.5f);
            long extraCount = lab.extraHashBytes / 100000;
            lab.extraHashBytes -= (int)extraCount * 100000;
            if (extraCount < 0L) extraCount = 0L;
            int iextraCount = (int)extraCount;
            ts.hashUploaded += count + extraCount;
            hashRegister += count + extraCount;
            uMatrixPoint += ts.uPointPerHash * count;
            techHashedThisFrame += icount + iextraCount;
            if (ts.hashUploaded >= ts.hashNeeded)
            {
                TechProto techProto = LDB.techs.Select(lab.techId);
                if (ts.curLevel >= ts.maxLevel)
                {
                    ts.curLevel = ts.maxLevel;
                    ts.hashUploaded = ts.hashNeeded;
                    ts.unlocked = true;
                }
                else
                {
                    ts.curLevel++;
                    ts.hashUploaded = 0L;
                    ts.hashNeeded = techProto.GetHashNeeded(ts.curLevel);
                }
            }
        }
        else
        {
            lab.extraSpeed = 0;
            lab.extraPowerRatio = 0;
        }

        return 1U;
    }

    public static void UpdateNeedsAssembleSingle(ref LabComponent lab, int m)
    {
        lab.needs[m] = lab.served[m] < RequireCountForAssemble ? lab.requires[m] : 0;
    }

    public static void UpdateNeedsResearchSingle(ref LabComponent lab, int m)
    {
        lab.needs[m] = lab.matrixServed[m] < RequireCountForResearch ? LabComponent.matrixIds[m] : 0;
    }
}
