using BepInEx.Configuration;
using CheatEnabler.Functions.DysonSphere;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;

namespace CheatEnabler.Functions;

[ModFeature("CheatDysonSphere", Order = 20)]
public static class DysonSphereFunctions
{
    public static ConfigEntry<bool> IllegalDysonShellFunctionsEnabled;
    public static ConfigEntry<int> ShellsCountForFunctions;

    public static void Init()
    {
                                                    }

    public static void CompleteShellsInstantly() => ShellCompletionFunctions.CompleteShellsInstantly();
    public static void RemoveAllFrames() => FrameRemovalFunctions.RemoveAllFrames();
    public static void DuplicateShellsWithHighestProduction() => IllegalShellFunctions.DuplicateShellsWithHighestProduction();
    public static void KeepMaxProductionShells() => IllegalShellFunctions.KeepMaxProductionShells();
    public static void CreateIllegalDysonShellQuickly(int triangleCount) => IllegalShellFunctions.CreateIllegalDysonShellQuickly(triangleCount);
    public static void CreateIllegalDysonShellWithMaxOutput() => IllegalShellFunctions.CreateIllegalDysonShellWithMaxOutput();
    public static void CreateIllegalDysonShellWithMaxOutputForAllLayers() => IllegalShellFunctions.CreateIllegalDysonShellWithMaxOutputForAllLayers();
    public static void CreateIllegalDysonShellsSpecially() => IllegalShellFunctions.CreateIllegalDysonShellsSpecially();
}
