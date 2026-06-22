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
        I18N.Add("You are not in any system.", "You are not in any system.", "你不在任何星系中");
        I18N.Add("There is no Dyson Sphere shell on \"{0}\".", "There is no Dyson Sphere shell on \"{0}\".", "“{0}”上没有可建造的戴森壳");
        I18N.Add("There is no Dyson Sphere data on \"{0}\".", "There is no Dyson Sphere data on \"{0}\".", "“{0}”上没有戴森球数据");
        I18N.Add("This will complete all Dyson Sphere shells on \"{0}\" instantly. Are you sure?", "This will complete all Dyson Sphere shells on \"{0}\" instantly. Are you sure?", "这将立即完成“{0}”上的所有戴森壳。你确定吗？");
        I18N.Add("This will remove all frames on \"{0}\". Are you sure?", "This will remove all frames on \"{0}\". Are you sure?", "这将移除“{0}”上的所有框架。你确定吗？");
        I18N.Add("No precalculated shell found for radius {0}.", "No precalculated shell found for radius {0}.", "没有找到适合半径 {0} 的预计算壳面");
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
