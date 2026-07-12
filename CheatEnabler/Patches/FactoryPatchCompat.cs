namespace CheatEnabler.Patches;

/// <summary>
/// Preserves the pre-refactor type and method identity used by mods that locate FactoryPatch through reflection.
/// </summary>
public static class FactoryPatch
{
    public static void ArrivePlanet(PlanetFactory factory)
    {
        Factory.FactoryPatch.ArrivePlanet(factory);
    }
}
