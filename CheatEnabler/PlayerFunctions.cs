using System;
using System.Linq;

namespace CheatEnabler;

public static class PlayerFunctions
{
    public static void TeleportToOuterSpace()
    {
        var maxSqrDistance = 0.0;
        var starPosition = VectorLF3.zero;
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
        var propertySysten = DSPGame.propertySystem;
        if (propertySysten == null) return;
        PurgePropertySystem(propertySysten);
        var itemCnt = new int[6];
        foreach (var cons in propertySysten.propertyDatas.SelectMany(data => data.totalConsumption.Where(cons => cons.id is >= 6001 and <= 6006)))
        {
            itemCnt[cons.id - 6001] += cons.count;
        }

        if (itemCnt.All(cnt => cnt == 0)) return;
        UIMessageBox.Show("Remove all metadata consumption records".Translate(), "".Translate(), "取消".Translate(), "确定".Translate(), 2, null, () =>
        {
            foreach (var data in propertySysten.propertyDatas)
            {
                for (var i = 0; i < data.totalConsumption.Count; i++)
                {
                    if (data.totalConsumption[i].count == 0) continue;
                    var id = data.totalConsumption[i].id;
                    data.totalConsumption[i] = new IDCNT(id, 0);
                }
            }
        });
    }

    public static void RemoveCurrentMetadataConsumptions()
    {
        
    }
}
