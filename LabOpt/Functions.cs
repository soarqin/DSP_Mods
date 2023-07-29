using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace LabOpt;

public static class LabOptPatchFunctions
{
    private static readonly FieldInfo RootLabIdField = AccessTools.Field(typeof(LabComponent), "rootLabId");

    public static void SetRootLabIdForStacking(FactorySystem factorySystem, int labId, int nextEntityId)
    {
        var labPool = factorySystem.labPool;
        var rootId = (int)RootLabIdField.GetValue(labPool[labId]);
        var targetLabId = factorySystem.factory.entityPool[nextEntityId].labId;
        if (rootId <= 0) rootId = labId;
        do
        {
            RootLabIdField.SetValueDirect(__makeref(labPool[targetLabId]), rootId);
            LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {targetLabId} to {rootId}");
        } while ((targetLabId = labPool[targetLabId].nextLabId) > 0);
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
            else if (lab.recipeId > 0)
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
            LabOptPatch.Logger.LogDebug($"Set rootLabId of lab {pair.Key} to {rootId}");
            AssignRootLabValues(ref labPool[rootId], ref labPool[pair.Key]);
        }
    }

    public static void AssignRootLabValues(ref LabComponent rootLab, ref LabComponent thisLab)
    {
        int len;
        if (rootLab.researchMode)
        {
            len = Math.Min(rootLab.matrixServed.Length, thisLab.matrixServed.Length);
            for (var i = 0; i < len; i++)
            {
                if (thisLab.matrixServed[i] > 0)
                {
                    rootLab.matrixServed[i] += thisLab.matrixServed[i];
                    thisLab.matrixServed[i] = 0;
                }
                if (thisLab.matrixIncServed[i] > 0)
                {
                    rootLab.matrixIncServed[i] += thisLab.matrixIncServed[i];
                    thisLab.matrixIncServed[i] = 0;
                }
            }
        }
        else if (rootLab.recipeId > 0)
        {
            len = Math.Min(rootLab.produced.Length, thisLab.produced.Length);
            for (var i = 0; i < len; i++)
            {
                if (thisLab.produced[i] <= 0) continue;
                rootLab.produced[i] += thisLab.produced[i];
                thisLab.produced[i] = 0;
            }
            len = Math.Min(rootLab.served.Length, thisLab.served.Length);
            for (var i = 0; i < len; i++)
            {
                if (thisLab.served[i] > 0)
                {
                    rootLab.served[i] += thisLab.served[i];
                    thisLab.served[i] = 0;
                }
                if (thisLab.incServed[i] > 0)
                {
                    rootLab.incServed[i] += thisLab.incServed[i];
                    thisLab.incServed[i] = 0;
                }
            }
        }
        thisLab.needs = rootLab.needs;
        thisLab.requires = rootLab.requires;
        thisLab.requireCounts = rootLab.requireCounts;
        thisLab.products = rootLab.products;
        thisLab.productCounts = rootLab.productCounts;
        thisLab.produced = rootLab.produced;
        thisLab.served = rootLab.served;
        thisLab.incServed = rootLab.incServed;
        thisLab.matrixPoints = rootLab.matrixPoints;
        thisLab.matrixServed = rootLab.matrixServed;
        thisLab.matrixIncServed = rootLab.matrixIncServed;
        thisLab.techId = rootLab.techId;
    }

    public static void LabExportZero(ref LabComponent lab, BinaryWriter w)
    {
        if (lab.matrixMode)
        {
            w.Write(lab.timeSpend);
            w.Write(lab.extraTimeSpend);
            w.Write(lab.requires.Length);
            foreach (var n in lab.requires)
            {
                w.Write(n);
            }
            w.Write(lab.requireCounts.Length);
            foreach (var n in lab.requireCounts)
            {
                w.Write(n);
            }
            w.Write(lab.served.Length);
            for (var i = 0; i < lab.served.Length; i++)
            {
                w.Write(0);
            }
            w.Write(lab.incServed.Length);
            for (var i = 0; i < lab.incServed.Length; i++)
            {
                w.Write(0);
            }
            w.Write(lab.needs.Length);
            for (var i = 0; i < lab.needs.Length; i++)
            {
                w.Write(0);
            }
            w.Write(lab.products.Length);
            foreach (var n in lab.products)
            {
                w.Write(n);
            }
            w.Write(lab.productCounts.Length);
            foreach (var n in lab.productCounts)
            {
                w.Write(n);
            }
            w.Write(lab.produced.Length);
            for (var i = 0; i < lab.produced.Length; i++)
            {
                w.Write(0);
            }
        }
        if (lab.researchMode)
        {
            w.Write(lab.matrixPoints.Length);
            foreach (var n in lab.matrixPoints)
            {
                w.Write(n);
            }
            w.Write(lab.matrixServed.Length);
            for (var i = 0; i < lab.matrixServed.Length; i++)
            {
                w.Write(0);
            }
            w.Write(lab.needs.Length);
            for (var i = 0; i < lab.needs.Length; i++)
            {
                w.Write(0);
            }
            w.Write(lab.matrixIncServed.Length);
            for (var i = 0; i < lab.matrixIncServed.Length; i++)
            {
                w.Write(0);
            }
        }
    }
    public static uint InternalUpdateAssembleNew(ref LabComponent lab, float power, int[] productRegister, int[] consumeRegister)
    {
        if (power < 0.1f)
        {
            return 0U;
        }

        if (lab.extraTime >= lab.extraTimeSpend)
        {
            var len = lab.products.Length;
            lock (lab.produced)
            {
                if (len == 1)
                {
                    lab.produced[0] += lab.productCounts[0];
                    lock (productRegister)
                    {
                        productRegister[lab.products[0]] += lab.productCounts[0];
                    }
                }
                else
                {
                    for (var i = 0; i < len; i++)
                    {
                        lab.produced[i] += lab.productCounts[i];
                        lock (productRegister)
                        {
                            productRegister[lab.products[i]] += lab.productCounts[i];
                        }
                    }
                }
            }

            lab.extraTime -= lab.extraTimeSpend;
        }

        if (lab.time >= lab.timeSpend)
        {
            lab.replicating = false;
            var len = lab.products.Length;
            lock (lab.produced)
            {
                if (len == 1)
                {
                    if (lab.produced[0] + lab.productCounts[0] > 30)
                    {
                        return 0U;
                    }

                    lab.produced[0] += lab.productCounts[0];
                    lock (productRegister)
                    {
                        productRegister[lab.products[0]] += lab.productCounts[0];
                    }
                }
                else
                {
                    for (var j = 0; j < len; j++)
                    {
                        if (lab.produced[j] + lab.productCounts[j] > 30)
                        {
                            return 0U;
                        }
                    }

                    for (var k = 0; k < len; k++)
                    {
                        lab.produced[k] += lab.productCounts[k];
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
            var len = lab.requireCounts.Length;
            int incLevel;
            if (len > 0)
            {
                var served = lab.served;
                lock (served)
                {
                    for (int l = 0; l < len; l++)
                    {
                        if (served[l] >= lab.requireCounts[l] && served[l] != 0) continue;
                        lab.time = 0;
                        return 0U;
                    }

                    incLevel = 10;
                    for (var m = 0; m < len; m++)
                    {
                        var splittedIncLevel = lab.split_inc_level(ref served[m], ref lab.incServed[m], lab.requireCounts[m]);
                        if (splittedIncLevel < incLevel) incLevel = splittedIncLevel;
                        if (served[m] == 0)
                        {
                            lab.incServed[m] = 0;
                        }
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
                lab.extraSpeed = (int)(lab.speed * Cargo.incTableMilli[incLevel] * 10.0 + 0.1);
                lab.speedOverride = lab.speed;
                lab.extraPowerRatio = Cargo.powerTable[incLevel];
            }
            else
            {
                lab.extraSpeed = 0;
                lab.speedOverride = (int)(lab.speed * (1.0 + Cargo.accTableMilli[incLevel]) + 0.1);
                lab.extraPowerRatio = Cargo.powerTable[incLevel];
            }

            lab.replicating = true;
        }

        switch (lab.replicating)
        {
            case true when lab.time < lab.timeSpend && lab.extraTime < lab.extraTimeSpend:
                lab.time += (int)(power * lab.speedOverride);
                lab.extraTime += (int)(power * lab.extraSpeed);
                break;
            case false:
                return 0U;
        }

        return (uint)(lab.products[0] - LabComponent.matrixIds[0] + 1);
    }

    public static void SetFunctionNew(ref LabComponent lab, bool researchMode, int recpId, int techId, SignData[] signPool, LabComponent[] labPool)
    {
        LabOptPatch.Logger.LogDebug($"SetFunctionNew: {lab.id} {(int)RootLabIdField.GetValue(lab)} {researchMode} {recpId} {techId}");
		lab.replicating = false;
		lab.time = 0;
		lab.hashBytes = 0;
		lab.extraHashBytes = 0;
		lab.extraTime = 0;
		lab.extraSpeed = 0;
		lab.extraPowerRatio = 0;
		lab.productive = false;
		if (researchMode)
		{
			lab.forceAccMode = false;
			lab.researchMode = true;
			lab.recipeId = 0;
			lab.techId = 0;
			lab.timeSpend = 0;
			lab.extraTimeSpend = 0;
			lab.requires = null;
			lab.requireCounts = null;
			lab.served = null;
			lab.incServed = null;
			lab.products = null;
			lab.productCounts = null;
			lab.produced = null;
			lab.productive = true;
            var rootLabId = (int)RootLabIdField.GetValue(lab);
            if (rootLabId > 0)
            {
                ref var rootLab = ref labPool[rootLabId];
                lab.needs = rootLab.needs;
                lab.matrixPoints = rootLab.matrixPoints;
                lab.matrixServed = rootLab.matrixServed;
                lab.matrixIncServed = rootLab.matrixIncServed;
                lab.techId = rootLab.techId;
            }
            else
            {
                if (lab.needs == null || lab.needs.Length != LabComponent.matrixIds.Length)
                {
                    lab.needs = new int[LabComponent.matrixIds.Length];
                }

                Array.Copy(LabComponent.matrixIds, lab.needs, LabComponent.matrixIds.Length);
                if (lab.matrixPoints == null)
                {
                    lab.matrixPoints = new int[LabComponent.matrixIds.Length];
                }
                else
                {
                    Array.Clear(lab.matrixPoints, 0, lab.matrixPoints.Length);
                }

                lab.matrixServed ??= new int[LabComponent.matrixIds.Length];

                lab.matrixIncServed ??= new int[LabComponent.matrixIds.Length];

                TechProto techProto = LDB.techs.Select(techId);
                if (techProto != null && techProto.IsLabTech)
                {
                    lab.techId = techId;
                    for (var i = 0; i < techProto.Items.Length; i++)
                    {
                        var index = techProto.Items[i] - LabComponent.matrixIds[0];
                        if (index >= 0 && index < lab.matrixPoints.Length)
                        {
                            lab.matrixPoints[index] = techProto.ItemPoints[i];
                        }
                    }
                }
            }
            signPool[lab.entityId].iconId0 = (uint)lab.techId;
			signPool[lab.entityId].iconType = lab.techId == 0 ? 0U : 3U;
			return;
		}
		lab.researchMode = false;
		lab.recipeId = 0;
		lab.techId = 0;
		lab.matrixPoints = null;
		lab.matrixServed = null;
		lab.matrixIncServed = null;
		RecipeProto recipeProto = null;
		if (recpId > 0)
		{
			recipeProto = LDB.recipes.Select(recpId);
		}
		if (recipeProto != null && recipeProto.Type == ERecipeType.Research)
		{
			lab.recipeId = recipeProto.ID;
			lab.speed = 10000;
			lab.speedOverride = lab.speed;
			lab.timeSpend = recipeProto.TimeSpend * 10000;
			lab.extraTimeSpend = recipeProto.TimeSpend * 100000;
			lab.productive = recipeProto.productive;
			lab.forceAccMode &= lab.productive;
            var rootLabId = (int)RootLabIdField.GetValue(lab);
            if (rootLabId > 0)
            {
                ref var rootLab = ref labPool[rootLabId];
                lab.needs = rootLab.needs;
                lab.requires = rootLab.requires;
                lab.requireCounts = rootLab.requireCounts;
                lab.products = rootLab.products;
                lab.productCounts = rootLab.productCounts;
                lab.produced = rootLab.produced;
                lab.served = rootLab.served;
                lab.incServed = rootLab.incServed;
            }
            else
            {
                lab.requires = new int[recipeProto.Items.Length];
                Array.Copy(recipeProto.Items, lab.requires, lab.requires.Length);
                lab.requireCounts = new int[recipeProto.ItemCounts.Length];
                Array.Copy(recipeProto.ItemCounts, lab.requireCounts, lab.requireCounts.Length);
                lab.served = new int[lab.requireCounts.Length];
                lab.incServed = new int[lab.requireCounts.Length];
                Assert.True(lab.requires.Length == lab.requireCounts.Length);
                if (lab.needs == null || lab.needs.Length != 6)
                {
                    lab.needs = new int[6];
                }
                else
                {
                    Array.Clear(lab.needs, 0, 6);
                }

                lab.products = new int[recipeProto.Results.Length];
                Array.Copy(recipeProto.Results, lab.products, lab.products.Length);
                lab.productCounts = new int[recipeProto.ResultCounts.Length];
                Array.Copy(recipeProto.ResultCounts, lab.productCounts, lab.productCounts.Length);
                Assert.True(lab.products.Length == lab.productCounts.Length);
                lab.produced = new int[lab.productCounts.Length];
            }
        }
		else
		{
			lab.forceAccMode = false;
			lab.recipeId = 0;
			lab.speed = 0;
			lab.speedOverride = 0;
			lab.timeSpend = 0;
			lab.extraTimeSpend = 0;
			lab.requires = null;
			lab.requireCounts = null;
			lab.served = null;
			lab.incServed = null;
			lab.needs = null;
			lab.products = null;
			lab.productCounts = null;
			lab.produced = null;
		}
		signPool[lab.entityId].iconId0 = (uint)lab.recipeId;
		signPool[lab.entityId].iconType = lab.recipeId == 0 ? 0U : 2U;
	}
}
