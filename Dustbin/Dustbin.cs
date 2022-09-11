using BepInEx;
using HarmonyLib;

namespace Dustbin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Dustbin : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    private static readonly int[] SandsFactors = { 0, 1, 5, 10, 100 };
    private static readonly bool[] IsFluid = new bool[2000];

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        SandsFactors[1] = Config.Bind("General", "SandsPerItem", SandsFactors[1], "Sands gathered from normal items").Value;
        SandsFactors[0] = Config.Bind("General", "SandsPerFluid", SandsFactors[0], "Sands gathered from fluids").Value;
        SandsFactors[2] = Config.Bind("General", "SandsPerStone", SandsFactors[2], "Sands gathered from stones").Value;
        SandsFactors[3] = Config.Bind("General", "SandsPerSilicon", SandsFactors[3], "Sands gathered from silicon ores").Value;
        SandsFactors[4] = Config.Bind("General", "SandsPerFractal", SandsFactors[4], "Sands gathered from fractal silicon ores").Value;
        Harmony.CreateAndPatchAll(typeof(Dustbin));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(GameDesc))]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(string))]
    private static void OnGameStart()
    {
        foreach (var data in LDB.items.dataArray)
        {
            if (data.ID < 2000 && data.IsFluid)
            {
                IsFluid[data.ID] = true;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StorageComponent), "AddItem",
        new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) },
        new[]
        {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal
        })]
    public static bool AbandonItems(ref int __result, StorageComponent __instance, int itemId, int count, int inc,
        out int remainInc, bool useBan = false)
    {
        remainInc = inc;
        if (!useBan || count == 0 || __instance.id != __instance.top) return true;
        var size = __instance.size;
        if (size == 0 || size != __instance.bans || __instance.grids[0].count > 0) return true;
        __result = count;
        var isFluid = itemId < 2000 && IsFluid[itemId];
        var sandsPerItem = SandsFactors[isFluid
            ? 0
            : itemId switch
            {
                1005 => 2,
                1003 => 3,
                1013 => 4,
                _ => 1,
            }];
        if (sandsPerItem <= 0) return false;
        var player = GameMain.mainPlayer;
        var addCount = count * sandsPerItem;
        player.sandCount += addCount;
        GameMain.history.OnSandCountChange(player.sandCount, addCount);
        /* Following line crashes game, seems that it should not be called in this working thread:
         *   UIRoot.instance.uiGame.OnSandCountChanged(player.sandCount, addCount);
         */
        return false;
    }
}
