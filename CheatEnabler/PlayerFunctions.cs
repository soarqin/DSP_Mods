using System;

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
}
