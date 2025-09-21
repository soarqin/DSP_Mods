using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using Random = UnityEngine.Random;

namespace UserCloak;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UserCloak : BaseUnityPlugin
{
    private static Harmony _patch;
    private static ConfigEntry<int> _mode;
    private static ConfigEntry<int> _fakeUserId;
    private static ConfigEntry<string> _fakeUsername;

    private void Awake()
    {
        _mode = Config.Bind("General", "Mode", 0, "Cloak Mode:\n" +
                                                      "  0: Disable cloaking.\n" +
                                                      "  1: Fake user account info.\n" +
                                                      "  2: Completely hide user account info. This also disables Milkyway completely.\n");
        _fakeUserId = Config.Bind("General", "FakeUserId", 0, "Fake Steam user ID");
        _fakeUsername = Config.Bind("General", "FakeUsername", "anonymous", "Fake Steam username");

        if (_mode.Value == 1 && _fakeUserId.Value == 0)
        {
            _fakeUserId.Value = Random.Range(10000, 2000000000);
        }

        if (_mode.Value != 0)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(UserCloak));
        }
    }

    private void OnDestroy()
    {
        _patch?.UnpatchSelf();
        _patch = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
    private static bool SteamManager_Awake_Prefix(SteamManager __instance)
    {
        if (_mode.Value == 0)
            return true;
        if (SteamManager.s_instance != null)
        {
            UnityEngine.Object.Destroy(__instance.gameObject);
            return false;
        }
        SteamManager.s_instance = __instance;
        if (SteamManager.s_EverInitialized)
        {
            throw new Exception("Tried to Initialize the SteamAPI twice in one session!");
        }
        __instance.m_bInitialized = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(STEAMX), nameof(STEAMX.Awake))]
    private static bool STEAMX_Awake_Prefix(STEAMX __instance)
    {
        switch (_mode.Value)
        {
            case 1:
                STEAMX.instance = __instance;
                STEAMX.userId = new CSteamID(0x0110000100000001UL | (uint)_fakeUserId.Value);
                return false;
            case 2:
                return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.OnEnable))]
    private static bool SteamManager_OnEnable_Prefix(SteamManager __instance)
    {
        if (_mode.Value == 0)
            return true;
        if (SteamManager.s_instance == null)
        {
            SteamManager.s_instance = __instance;
        }
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
    private static bool SteamManager_Update_Prefix()
    {
        return _mode.Value == 0;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PARTNER), nameof(PARTNER.STEAMWORKS), MethodType.Getter)]
    private static bool PARTNER_STEAMWORKS_Prefix(ref bool __result)
    {
        if (_mode.Value != 2)
            return true;
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PARTNER), nameof(PARTNER.UserLogin))]
    private static bool PARTNER_UserLogin_Prefix()
    {
        switch (_mode.Value)
        {
            case 1:
                AccountData.me.platform = ESalePlatform.Steam;
                AccountData.me.userId = 0x0110000100000001UL | (uint)_fakeUserId.Value;
                AccountData.me.detail.userName = _fakeUsername.Value;
                PARTNER.logined = true;
                return false;
            case 2:
                return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamLeaderboardManager_ClusterGeneration), nameof(SteamLeaderboardManager_ClusterGeneration.Start))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_ClusterGeneration), nameof(SteamLeaderboardManager_ClusterGeneration.Update))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_ClusterGeneration), nameof(SteamLeaderboardManager_ClusterGeneration.UploadScore))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_PowerConsumption), nameof(SteamLeaderboardManager_PowerConsumption.Start))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_PowerConsumption), nameof(SteamLeaderboardManager_PowerConsumption.Update))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_PowerConsumption), nameof(SteamLeaderboardManager_PowerConsumption.UploadScore))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_SolarSail), nameof(SteamLeaderboardManager_SolarSail.Start))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_SolarSail), nameof(SteamLeaderboardManager_SolarSail.Update))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_SolarSail), nameof(SteamLeaderboardManager_SolarSail.UploadScore))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_UniverseMatrix), nameof(SteamLeaderboardManager_UniverseMatrix.Start))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_UniverseMatrix), nameof(SteamLeaderboardManager_UniverseMatrix.Update))]
    [HarmonyPatch(typeof(SteamLeaderboardManager_UniverseMatrix), nameof(SteamLeaderboardManager_UniverseMatrix.UploadScore))]
    [HarmonyPatch(typeof(SteamAchievementManager), nameof(SteamAchievementManager.Start))]
    [HarmonyPatch(typeof(SteamAchievementManager), nameof(SteamAchievementManager.Update))]
    [HarmonyPatch(typeof(SteamAchievementManager), nameof(SteamAchievementManager.UnlockAchievement))]
    private static bool SteamLeaderBoard_functions_Prefix()
    {
        return _mode.Value == 0;
    }
}
