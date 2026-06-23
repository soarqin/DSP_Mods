using System;
using System.Linq;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using UXAssist.Common.ModFeatures;

namespace CheatEnabler.Functions;

[ModFeature("PlayerFunctions")]
public static class PlayerFunctions
{
    public static void Init()
    {
        I18N.Add("ClearAllMetadataConsumptionDetails",
            """
            Metadata consumption records of all gamesaves are about to be cleared.
            You will gain following metadata back:
            """,
            """
            所有存档的元数据消耗记录即将被清除，
            此操作将返还以下元数据：
            """);
        I18N.Add("ClearCurrentMetadataConsumptionDetails",
            """
            Metadata consumption records of current gamesave are about to be cleared.
            You will gain following metadata back:
            """,
            """
            当前存档的元数据消耗记录即将被清除，
            此操作将返还以下元数据：
            """);
        I18N.Add("NoMetadataConsumptionRecord",
            "No metadata consumption records found.",
            "未找到元数据消耗记录。");
    }

    public static void TeleportToOuterSpace()
    {
        var maxSqrDistance = 0.0;
        var starPosition = VectorLF3.zero;
        if (GameMain.galaxy == null) return;
        foreach (var star in GameMain.galaxy.stars)
        {
            var sqrDistance = star.position.sqrMagnitude;
            if (sqrDistance > maxSqrDistance)
            {
                maxSqrDistance = sqrDistance;
                starPosition = star.position;
            }
        }
        if (starPosition == VectorLF3.zero) return;
        var distance = Math.Sqrt(maxSqrDistance);
        GameMain.mainPlayer.controller.actionSail.StartFastTravelToUPosition((starPosition + starPosition.normalized * 50) * GalaxyData.LY);
    }

    public static void TeleportToSelectedAstronomical()
    {
        var starmap = UIRoot.instance?.uiGame?.starmap;
        if (starmap == null) return;
        if (starmap.focusPlanet != null)
        {
            GameMain.mainPlayer.controller.actionSail.StartFastTravelToPlanet(starmap.focusPlanet.planet);
            return;
        }
        var targetUPos = VectorLF3.zero;
        if (starmap.focusStar != null)
        {
            var star = starmap.focusStar.star;
            targetUPos = star.uPosition + VectorLF3.unit_x * star.physicsRadius;
        }
        else if (starmap.focusHive != null)
        {
            var hive = starmap.focusHive.hive;
            var id = hive.hiveAstroId - 1000000;
            if (id > 0 && id < starmap.spaceSector.astros.Length)
            {
                ref var astro = ref starmap.spaceSector.astros[id];
                targetUPos = astro.uPos + VectorLF3.unit_x * astro.uRadius;
            }
        }
        GameMain.mainPlayer.controller.actionSail.StartFastTravelToUPosition(targetUPos);
    }

    private static void PurgePropertySystem(PropertySystem propertySystem)
    {
        var propertyDatas = propertySystem.propertyDatas;
        for (var i = 0; i < propertyDatas.Count;)
        {
            if (propertyDatas[i].totalProduction.Any(idcnt => idcnt.count > 0) || propertyDatas[i].totalConsumption.Any(idcnt => idcnt.count > 0))
            {
                i++;
            }
            else
            {
                propertyDatas.RemoveAt(i);
            }
        }
    }

    public static void RemoveAllMetadataConsumptions()
    {
        var propertySystem = DSPGame.propertySystem;
        if (propertySystem == null) return;
        PurgePropertySystem(propertySystem);
        var itemCnt = new int[ItemIds.Metaverse - ItemIds.HydrogenFuelRod + 1];
        foreach (var cons in propertySystem.propertyDatas.SelectMany(data => data.totalConsumption.Where(cons => cons.id is >= ItemIds.HydrogenFuelRod and <= ItemIds.Metaverse)))
        {
            itemCnt[cons.id - ItemIds.HydrogenFuelRod] += cons.count;
        }

        if (itemCnt.All(cnt => cnt == 0))
        {
            UIMessageBox.Show("Remove all metadata consumption records".Translate(), "NoMetadataConsumptionRecord".Translate(), "OK".Translate(), UIMessageBox.INFO);
            return;
        }
        var msg = "ClearAllMetadataConsumptionDetails".Translate();
        for (var i = 0; i < itemCnt.Length; i++)
        {
            if (itemCnt[i] > 0)
            {
                msg += $"\n  {LDB.items.Select(i + ItemIds.HydrogenFuelRod).propertyName} x{itemCnt[i]}";
            }
        }
        UIMessageBox.Show("Remove all metadata consumption records".Translate(), msg, "Cancel".Translate(), "OK".Translate(), UIMessageBox.QUESTION, null, () =>
        {
            foreach (var data in propertySystem.propertyDatas)
            {
                for (var i = 0; i < data.totalConsumption.Count; i++)
                {
                    if (data.totalConsumption[i].count == 0) continue;
                    var id = data.totalConsumption[i].id;
                    data.totalConsumption[i] = new IDCNT(id, 0);
                }
            }
            PurgePropertySystem(propertySystem);
            propertySystem.SaveToFile();
        });
    }

    public static void RemoveCurrentMetadataConsumptions()
    {
        var propertySystem = DSPGame.propertySystem;
        if (propertySystem == null) return;
        PurgePropertySystem(propertySystem);
        var itemCnt = new int[ItemIds.Metaverse - ItemIds.HydrogenFuelRod + 1];
        var seedKey = DSPGame.GameDesc.seedKey64;
        var clusterPropertyData = propertySystem.propertyDatas.FirstOrDefault(cpd => cpd.seedKey == seedKey);
        if (clusterPropertyData == null)
        {
            UIMessageBox.Show("Remove metadata consumption record in current game".Translate(), "NoMetadataConsumptionRecord".Translate(), "OK".Translate(), UIMessageBox.INFO);
            return;
        }
        var currentGamePropertyData = GameMain.data.history.propertyData;
        foreach (var cons in currentGamePropertyData.totalConsumption.Where(cons => cons.id is >= ItemIds.HydrogenFuelRod and <= ItemIds.Metaverse))
        {
            itemCnt[cons.id - ItemIds.HydrogenFuelRod] += cons.count;
        }

        if (itemCnt.All(cnt => cnt == 0))
        {
            UIMessageBox.Show("Remove metadata consumption record in current game".Translate(), "NoMetadataConsumptionRecord".Translate(), "OK".Translate(), UIMessageBox.INFO);
            return;
        }
        var msg = "ClearCurrentMetadataConsumptionDetails".Translate();
        for (var i = 0; i < itemCnt.Length; i++)
        {
            if (itemCnt[i] > 0)
            {
                msg += $"\n  {LDB.items.Select(i + ItemIds.HydrogenFuelRod).propertyName} x{itemCnt[i]}";
            }
        }
        UIMessageBox.Show("Remove metadata consumption record in current game".Translate(), msg, "Cancel".Translate(), "OK".Translate(), UIMessageBox.QUESTION, null, () =>
        {
            for (var i = 0; i < clusterPropertyData.totalConsumption.Count; i++)
            {
                if (clusterPropertyData.totalConsumption[i].count == 0) continue;
                var id = clusterPropertyData.totalConsumption[i].id;
                if (id < ItemIds.HydrogenFuelRod || id > ItemIds.Metaverse) continue;
                var currentGameCount = itemCnt[id - ItemIds.HydrogenFuelRod];
                if (currentGameCount == 0) continue;
                var totalCount = clusterPropertyData.totalConsumption[i].count;
                clusterPropertyData.totalConsumption[i] = new IDCNT(id, totalCount > currentGameCount ? totalCount - currentGameCount : 0);
            }
            PurgePropertySystem(propertySystem);
            propertySystem.SaveToFile();
        });
    }

    public static void ClearMetadataBanAchievements()
    {
        GameMain.history.hasUsedPropertyBanAchievement = false;
    }
}
