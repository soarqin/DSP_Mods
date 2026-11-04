using System.IO;
using UXAssist.Common.ModFeatures;
using UXAssist.Functions.UI;

namespace UXAssist.Functions;

[ModFeature("UI", Order = 12)]
public static class UIFunctions
{
    public static void Init()
    {
        MenuButtonUI.Init();
        MilkyWayUI.Init();

        UXAssist.RegisterExporter(ExportClusterUploadResults);
        UXAssist.RegisterImporter((r, version) => ImportClusterUploadResults(r, version));
    }

    public static void Start()
    {
    }

    public static void Uninit()
    {
        MilkyWayUI.Uninit();
    }

    public static void OnInputUpdate()
    {
        MenuButtonUI.OnInputUpdate();
    }

    public static void OnUpdate()
    {
        AutoCruiseUI.OnUpdate();
        AutoConstructUI.OnUpdate();
    }

    public static void InitMenuButtons()
    {
        MenuButtonUI.InitMenuButtons();
    }

    public static void InitMilkyWayTopTenPlayers()
    {
        MilkyWayUI.InitMilkyWayTopTenPlayers();
    }

    public static void UpdateGlobeButtonPosition(UIPlanetGlobe planetGlobe)
    {
        MenuButtonUI.UpdateGlobeButtonPosition(planetGlobe);
    }

    public static void UpdateToggleAutoCruiseCheckButtonVisiblility()
    {
        AutoCruiseUI.UpdateToggleAutoCruiseCheckButtonVisiblility();
    }

    public static void UpdateToggleAutoConstructCheckButtonVisiblility()
    {
        AutoConstructUI.UpdateToggleAutoConstructCheckButtonVisiblility();
    }

    public static void UpdateConstructCountText(int count)
    {
        AutoConstructUI.UpdateConstructCountText(count);
    }

    public static void OnPlanetScanEnded()
    {
        StarmapFilterUI.OnPlanetScanEnded();
    }

    public static void AddClusterUploadResult(int result, float requestTime)
    {
        MilkyWayUI.AddClusterUploadResult(result, requestTime);
    }

    public static void ExportClusterUploadResults(BinaryWriter w)
    {
        MilkyWayUI.Export(w);
    }

    public static void ImportClusterUploadResults(BinaryReader r, ushort version)
    {
        if (version > 1)
            MilkyWayUI.Import(r);
        else
            MilkyWayUI.ClearClusterUploadResults();
    }

    public static void ClearClusterUploadResults()
    {
        MilkyWayUI.ClearClusterUploadResults();
    }

    public static void ShowRecentMilkywayUploadResults()
    {
        MilkyWayUI.ShowRecentMilkywayUploadResults();
    }

    public static void SetTopPlayerCount(int count)
    {
        MilkyWayUI.SetTopPlayerCount(count);
    }

    public static void SetTopPlayerData(int index, ref ClusterPlayerData playerData)
    {
        MilkyWayUI.SetTopPlayerData(index, ref playerData);
    }

    public static void UpdateMilkyWayTopTenPlayers()
    {
        MilkyWayUI.UpdateMilkyWayTopTenPlayers();
    }
}
