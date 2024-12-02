using System;
using UXAssist.Common;

namespace LogisticHub.Module;

public class PlanetVeins
{
    
}

public class VeinManager: PatchImpl<VeinManager>
{
    private static PlanetVeins[] _veins;

    public void Init()
    {
        Enable(true);
    }

    public void Uninit()
    {
        Enable(false);
    }

    private static PlanetVeins VeinsByPlanet(int planetId)
    {
        if (_veins == null || _veins.Length <= planetId)
            Array.Resize(ref _veins, planetId + 1);
        var veins = _veins[planetId];
        if (veins != null) return veins;
        veins = new PlanetVeins();
        _veins[planetId] = veins;
        return veins;
    }
}
