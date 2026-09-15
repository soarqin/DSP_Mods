using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Common.Patching;
using Object = UnityEngine.Object;

namespace UXAssist.Patches;

public class PlayerPatch : PatchImpl<PlayerPatch>
{
    public static ConfigEntry<bool> EnhancedMechaForgeCountControlEnabled;
    public static ConfigEntry<bool> HideTipsForSandsChangesEnabled;
    public static ConfigEntry<bool> ShortcutKeysForStarsNameEnabled;
    private static PressKeyBind _showAllStarsNameKey;
    private static PressKeyBind _toggleAllStarsNameKey;
    private static PressKeyBind _autoDriveKey;

    public static ConfigEntry<bool>   AutoCruiseEnabled         { get; set; } = null!;
    public static ConfigEntry<bool>   StopOnArrivalAndInput     { get; set; } = null!;
    public static ConfigEntry<bool>   UseWarper                 { get; set; } = null!;
    public static ConfigEntry<double> UseWarperMinimalEnergy    { get; set; } = null!;
    public static ConfigEntry<double> UseWarperDistance         { get; set; } = null!;
    public static ConfigEntry<bool>   UseSpeedUp                { get; set; } = null!;
    public static ConfigEntry<double> UseSpeedUpMinimalEnergy   { get; set; } = null!;
    public static ConfigEntry<double> DFHiveFollowDistance      { get; set; } = null!;
    public static ConfigEntry<double> DFCarrierFollowDistance   { get; set; } = null!;

    public static void Init()
    {
        _showAllStarsNameKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.UI | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ShowAllStarsName",
            canOverride = true
        }
        );
        _toggleAllStarsNameKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.Tab, 0, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.UI | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleAllStarsName",
            canOverride = true
        }
        );
        _autoDriveKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.K, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleAutoCruise",
            canOverride = true
        });
        EnhancedMechaForgeCountControlEnabled.SettingChanged += (_, _) => EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChangesEnabled.SettingChanged += (_, _) => HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        ShortcutKeysForStarsNameEnabled.SettingChanged += (_, _) => ShortcutKeysForStarsName.Enable(ShortcutKeysForStarsNameEnabled.Value);
        AutoNavigation.Init();
    }

    public static void Start()
    {
        EnhancedMechaForgeCountControl.Enable(EnhancedMechaForgeCountControlEnabled.Value);
        HideTipsForSandsChanges.Enable(HideTipsForSandsChangesEnabled.Value);
        ShortcutKeysForStarsName.Enable(ShortcutKeysForStarsNameEnabled.Value);
        AutoNavigation.Enable(AutoCruiseEnabled.Value);
        Enable(true);
    }

    public static void OnInputUpdate()
    {
        ShortcutKeysForStarsName.OnInputUpdate();
        if (_autoDriveKey.keyValue)
            AutoNavigation.Toggle();
    }

    public static void Uninit()
    {
        Enable(false);
        EnhancedMechaForgeCountControl.Enable(false);
        HideTipsForSandsChanges.Enable(false);
        ShortcutKeysForStarsName.Enable(false);
        AutoNavigation.Enable(false);
    }
    // Harmony transpiler: UIStarmapStar__OnLateUpdate_Transpiler
    // Target: UIStarmapStar._OnLateUpdate
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIStarmapStar), nameof(UIStarmapStar._OnLateUpdate))]
    private static IEnumerable<CodeInstruction> UIStarmapStar__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        Label? jumpPos = null;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapStar), nameof(UIStarmapStar.projectedCoord))),
            new CodeMatch(OpCodes.Ldarg_0)
        ).Advance(2).MatchForward(false,
            new CodeMatch(ci => ci.IsStloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.Branches(out jumpPos))
        ).Advance(3);
        var labels = matcher.Labels;
        matcher.Labels = [];
        matcher.CreateLabel(out var jumpPos2);
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(ShortcutKeysForStarsName.ShowAllStarsNameStatus))).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ceq),
            new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch.ShortcutKeysForStarsName), nameof(ShortcutKeysForStarsName.ForceShowAllStarsName))),
            new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Functions.UI.StarmapFilterUI), nameof(Functions.UI.StarmapFilterUI.ShowStarName))),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapStar), nameof(UIStarmapStar.star))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StarData), nameof(StarData.index))),
            new CodeInstruction(OpCodes.Ldelem_I1),
            new CodeInstruction(OpCodes.Brtrue, jumpPos.Value),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(ShortcutKeysForStarsName.ShowAllStarsNameStatus))),
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Ceq),
            new CodeInstruction(OpCodes.Brfalse, jumpPos2),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Stloc_1),
            new CodeInstruction(OpCodes.Br, jumpPos.Value)
        );
        return matcher.InstructionEnumeration();
    }


    private class EnhancedMechaForgeCountControl : PatchImpl<EnhancedMechaForgeCountControl>
    {
        // Harmony transpiler: UIReplicatorWindow_OnOkButtonClick_Transpiler
        // Target: UIReplicatorWindow.OnOkButtonClick
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnOkButtonClick))]
        private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnOkButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 1000));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: UIReplicatorWindow_OnPlusButtonClick_Transpiler
        // Target: UIReplicatorWindow.OnPlusButtonClick, UIReplicatorWindow.OnMinusButtonClick
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnPlusButtonClick))]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnMinusButtonClick))]
        private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnPlusButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            var label3 = generator.DefineLabel();
            var label4 = generator.DefineLabel();
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(o => o.opcode == OpCodes.Add || o.opcode == OpCodes.Sub)
            ).Advance(1).RemoveInstruction().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.control))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Ldc_I4_S, 10),
                new CodeInstruction(OpCodes.Br_S, label4),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))).WithLabels(label1),
                new CodeInstruction(OpCodes.Brfalse_S, label2),
                new CodeInstruction(OpCodes.Ldc_I4_S, 100),
                new CodeInstruction(OpCodes.Br_S, label4),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.alt))).WithLabels(label2),
                new CodeInstruction(OpCodes.Brfalse_S, label3),
                new CodeInstruction(OpCodes.Ldc_I4, 1000),
                new CodeInstruction(OpCodes.Br_S, label4),
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(label3)
            ).Labels.Add(label4);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 1000));
            return matcher.InstructionEnumeration();
        }
    }

    private class HideTipsForSandsChanges : PatchImpl<HideTipsForSandsChanges>
    {
        // Harmony transpiler: Player_SetSandCount_Transpiler
        // Target: Player.SetSandCount
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.SetSandCount))]
        private static IEnumerable<CodeInstruction> Player_SetSandCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(Player), nameof(Player.sandCount)))
            ).Advance(1).Insert(new CodeInstruction(OpCodes.Ret));
            return matcher.InstructionEnumeration();
        }
    }

    public class ShortcutKeysForStarsName : PatchImpl<ShortcutKeysForStarsName>
    {
        public static int ShowAllStarsNameStatus;
        public static bool ForceShowAllStarsName;
        public static bool ForceShowAllStarsNameExternal;

        public static void ToggleAllStarsName()
        {
            ShowAllStarsNameStatus = (ShowAllStarsNameStatus + 1) % 3;
        }

        public static void OnInputUpdate()
        {
            if (!UIRoot.instance.uiGame.starmap.active) return;
            var enabled = ShortcutKeysForStarsNameEnabled.Value;
            if (!enabled)
            {
                ForceShowAllStarsName = ForceShowAllStarsNameExternal;
                return;
            }
            if (_toggleAllStarsNameKey.keyValue)
            {
                ToggleAllStarsName();
            }
            ForceShowAllStarsName = ForceShowAllStarsNameExternal || _showAllStarsNameKey.IsKeyPressing();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnOpen))]
        private static void UIStarmap__OnOpen_Prefix()
        {
            ShowAllStarsNameStatus = 0;
        }
        /*
                // Harmony transpiler: UIStarmapPlanet__OnLateUpdate_Transpiler
                // Target: UIStarmapPlanet._OnLateUpdate
                // Fallback: None — patch will fail loudly if the target method body changes.
                [HarmonyTranspiler]
                [HarmonyPatch(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet._OnLateUpdate))]
                private static IEnumerable<CodeInstruction> UIStarmapPlanet__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
                {
                    var matcher = new CodeMatcher(instructions, generator);
                    matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldloc_3),
                        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet.projected)))
                    );
                    matcher.Advance(3);
                    matcher.CreateLabel(out var jumpPos1);
                    matcher.InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))),
                        new CodeInstruction(OpCodes.Ldc_I4_2),
                        new CodeInstruction(OpCodes.Ceq),
                        new CodeInstruction(OpCodes.Brfalse, jumpPos1),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Stloc_3)
                    );
                    matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet.gameHistory))),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapPlanet), nameof(UIStarmapPlanet.planet))),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.id))),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.GetPlanetPin))),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(ci => ci.Branches(out _)),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(ci => ci.IsStloc()),
                        new CodeMatch(ci => ci.Branches(out _))
                    );
                    matcher.CreateLabelAt(matcher.Pos + 8, out var jumpPos);
                    var labels = matcher.Labels;
                    matcher.Labels = [];
                    matcher.InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))).WithLabels(labels),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ceq),
                        new CodeInstruction(OpCodes.Brtrue, jumpPos),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch), nameof(_showAllStarsNameKey))),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                        new CodeInstruction(OpCodes.Brtrue, jumpPos)
                    );
                    return matcher.InstructionEnumeration();
                }
                // Harmony transpiler: UIStarmapDFHive__OnLateUpdate_Transpiler
                // Target: UIStarmapDFHive._OnLateUpdate
                // Fallback: None — patch will fail loudly if the target method body changes.
                [HarmonyTranspiler]
                [HarmonyPatch(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive._OnLateUpdate))]
                private static IEnumerable<CodeInstruction> UIStarmapDFHive__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
                {
                    var matcher = new CodeMatcher(instructions, generator);
                    matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(ci => ci.IsLdloc()),
                        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive.projected)))
                    );
                    matcher.Advance(3);
                    matcher.CreateLabel(out var jumpPos1);
                    matcher.InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))),
                        new CodeInstruction(OpCodes.Ldc_I4_2),
                        new CodeInstruction(OpCodes.Ceq),
                        new CodeInstruction(OpCodes.Brfalse, jumpPos1),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Stloc_S, 4)
                    );
                    matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive.gameHistory))),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStarmapDFHive), nameof(UIStarmapDFHive.hive))),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.hiveStarId))),
                        new CodeMatch(OpCodes.Ldc_I4, 1000000),
                        new CodeMatch(OpCodes.Sub),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.GetHivePin))),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(ci => ci.Branches(out _)),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(ci => ci.IsStloc()),
                        new CodeMatch(ci => ci.Branches(out _))
                    );
                    matcher.CreateLabelAt(matcher.Pos + 10, out var jumpPos);
                    var labels = matcher.Labels;
                    matcher.Labels = [];
                    matcher.InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ShortcutKeysForStarsName), nameof(_showAllStarsNameStatus))).WithLabels(labels),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ceq),
                        new CodeInstruction(OpCodes.Brtrue, jumpPos),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPatch), nameof(_showAllStarsNameKey))),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                        new CodeInstruction(OpCodes.Brtrue, jumpPos)
                    );
                    return matcher.InstructionEnumeration();
                }
        */
    }


    #region Auto-cruise navigation

    public class AutoNavigation : PatchImpl<AutoNavigation>
    {
        private const double Epsilon                     = 1e-12;
        private const int    DarkFogAstroIdStart         = 1000000;
        private const int    StarArriveThresholdRadius   = 4000;
        private const int    PlanetArriveThresholdRadius = 100;
        private const double StarSafeRadius              = 2000;
        private const double PlanetSafeRadius            = 2000;
        private const double HiveSafeRadius              = GalaxyData.AU * 0.5;
        private const double FollowModeUseWarpDistance       = GalaxyData.AU * 1.5;
        private const double ResidualCompensationMaxDistance = GalaxyData.AU * 0.5;
        private const double ResidualCompensationSpeedRatio  = 0.5;
        private const double ResidualCompensationMinSpeed    = 100;
        private const float  WarpTurnDegreesPerTick          = 1.6f;

        // Reference-frame blending, mirroring PlayerMove_Sail.GameTick: the planet frame velocity
        // fades out between 150 m and 600 m of altitude and is fully gone above that.
        private const double ReferenceFrameFadeAltitude = 600.0;
        private const double ReferenceFrameFadeRange    = 450.0;

        // Approach braking. PlayerMove_Sail decelerates by 0.8% of the current velocity per tick, so
        // slowing from v to a standstill covers v / (0.008 * 60) = 2.08 * v metres. The constant below
        // carries a safety margin on top of that so the brake starts early enough.
        private const double SailBrakeRatePerTick        = 0.008;
        private const double SailBrakeSecondsPerSpeed    = 2.6;
        // PlayerMove_Sail only hands over to PlayerMove_Fly below 75 m/s of planet-relative speed, so
        // a planet approach has to end well under that or the mecha can never land.
        private const double PlanetApproachArrivalSpeed  = 60.0;
        private const double StarApproachArrivalSpeed    = 300.0;

        private static bool _enabled;
        private static Text _uiTipText;

        private static int             _targetId;
        private static VectorLF3       _targetUniversePosition;
        private static VectorLF3       _targetUniverseVelocity;
        private static ESpaceGuideType _targetType;
        private static double          _targetArriveThresholdRadius;

        private static readonly List<Obstacle> Obstacles = new(64);
        private static bool _wasSailing;
        private static bool _followModeLock;
        private static bool _warpSteeringPending;
        private static Quaternion _warpHeadingBeforeRotation;
        private static VectorLF3 _warpTargetDirection;


        public static bool IsActive => _enabled;

        protected override void OnEnable()
        {
            EnsureUiTip();
            global::UXAssist.Common.GameLogic.OnGameBegin += EnsureUiTip;
            global::UXAssist.Common.GameLogic.OnGameEnd += ResetState;
        }

        protected override void OnDisable()
        {
            global::UXAssist.Common.GameLogic.OnGameBegin -= EnsureUiTip;
            global::UXAssist.Common.GameLogic.OnGameEnd -= ResetState;
            ResetState();
            if (_uiTipText != null)
            {
                Object.Destroy(_uiTipText.gameObject);
                _uiTipText = null;
            }
        }

        public static void Awake(ConfigFile config)
        {
            AutoCruiseEnabled = config.Bind(
                "Player",
                "AutoCruise",
                true,
                "Enable auto-cruise");
            StopOnArrivalAndInput = config.Bind("Player", nameof(StopOnArrivalAndInput), true, "Stop auto-cruise on arrival or manual input");
            UseWarper             = config.Bind("Player", nameof(UseWarper),             true, "Use warp during auto-cruise");
            UseWarperMinimalEnergy = config.Bind<double>(
                "Player",
                nameof(UseWarperMinimalEnergy),
                800,
                new ConfigDescription("Minimum energy required to use warp (MJ)", new AcceptableValueRange<double>(50, 1000)));
            UseWarperDistance = config.Bind("Player", nameof(UseWarperDistance), 2.0,
                new ConfigDescription("Minimum distance to use warp (AU)", new AcceptableValueRange<double>(0.5, 20)));
            UseSpeedUp        = config.Bind("Player", nameof(UseSpeedUp), true, "Use automatic acceleration during auto-cruise");
            UseSpeedUpMinimalEnergy = config.Bind<double>(
                "Player",
                nameof(UseSpeedUpMinimalEnergy),
                100,
                new ConfigDescription("Minimum energy required for automatic acceleration (MJ)", new AcceptableValueRange<double>(50, 1000)));
            DFHiveFollowDistance = config.Bind(
                "Player",
                nameof(DFHiveFollowDistance),
                0.5,
                new ConfigDescription("Dark Fog Hive follow distance (AU)", new AcceptableValueRange<double>(0.1, 5)));
            DFCarrierFollowDistance = config.Bind<double>(
                "Player",
                nameof(DFCarrierFollowDistance),
                1000,
                new ConfigDescription("Dark Fog Carrier follow distance (m)", new AcceptableValueRange<double>(100, 3000)));
        }

        public static void Init()
        {
            AutoCruiseEnabled.SettingChanged += NavigationModeChanged;
        }

        public static void EnsureUiTip()
        {
            if (_uiTipText != null)
            {
                UpdateUiTip();
                return;
            }
            if (UIRoot.instance?.uiGame?.generalTips?.modeText == null)
                return;
            Text originText = UIRoot.instance.uiGame.generalTips.modeText;
            _uiTipText = Object.Instantiate(originText, originText.transform.parent);
            _uiTipText.gameObject.SetActive(false);
            _uiTipText.rectTransform.anchoredPosition = new Vector2(0f, 160f);
            _uiTipText.fontStyle = FontStyle.Normal;
            UpdateUiTip();
        }

        public static void Toggle()
        {
            if (!AutoCruiseEnabled.Value)
                return;
            if (_enabled)
                StopAutoNavigation();
            else
                StartAutoNavigation();
        }

        private static void StartAutoNavigation()
        {
            if (DSPGame.IsMenuDemo || !GameMain.isRunning)
                return;

            var player = GameMain.mainPlayer;
            if (player == null || player.navigation.navigating)
                return;

            // Without this check the next tick would immediately fail to resolve a target and stop
            // again, producing a "started" and a "stopped" popup back to back.
            if (!HasNavigationTarget())
            {
                UIRealtimeTip.Popup(I18NKeys.AutoCruiseNoTarget.Translate(), sound: false);
                return;
            }

            _enabled = true;
            ResetTargetState();
            UIRealtimeTip.Popup(I18NKeys.AutoCruiseStarted.Translate(), sound: false);
            UpdateUiTip();
        }

        private static void StopAutoNavigation(bool showTip = true)
        {
            bool wasEnabled = _enabled;
            _enabled = false;
            ResetTargetState();
            if (wasEnabled && showTip && !DSPGame.IsMenuDemo && GameMain.isRunning)
                UIRealtimeTip.Popup(I18NKeys.AutoCruiseStopped.Translate(), sound: false);
            UpdateUiTip();
        }

        private static void ResetTargetState()
        {
            _targetId                    = 0;
            _wasSailing                  = false;
            _followModeLock              = false;
            _warpSteeringPending         = false;
            _warpHeadingBeforeRotation   = Quaternion.identity;
            _warpTargetDirection         = default;
            _targetUniversePosition      = default;
            _targetUniverseVelocity      = default;
            _targetType                  = default;
            _targetArriveThresholdRadius = 0;
            Obstacles.Clear();
        }

        private static void ResetState()
        {
            StopAutoNavigation(false);
        }

        private static void NavigationModeChanged(object sender, EventArgs args)
        {
            if (!AutoCruiseEnabled.Value)
                StopAutoNavigation(false);
            // Follow the config with the Harmony patches themselves, so a user who turns auto-cruise off
            // does not keep a transpiler on PlayerController.GameTick and two postfixes installed.
            Enable(AutoCruiseEnabled.Value);
            if (AutoCruiseEnabled.Value)
                UpdateUiTip();
            global::UXAssist.Functions.UIFunctions.UpdateToggleAutoCruiseCheckButtonVisiblility();
        }

        public static void UpdateUiTip()
        {
            if (_uiTipText == null)
                return;

            bool showTip = GameMain.isRunning && !DSPGame.IsMenuDemo && AutoCruiseEnabled.Value && (_enabled || HasNavigationTarget());
            _uiTipText.gameObject.SetActive(showTip);
            if (!showTip)
                return;

            _uiTipText.text = _enabled
                ? I18NKeys.AutoCruiseActive.Translate()
                : string.Format(I18NKeys.AutoCruiseEnableHint.Translate(), KeyBindings.GetKeyBindingText(_autoDriveKey));
        }

        public static bool HasNavigationTarget()
        {
            Player player = GameMain.mainPlayer;
            if (player == null || !TryResolveNavigationTarget(player, out NavigationTarget target))
                return false;

            return target.Type != ESpaceGuideType.Planet || GameMain.localPlanet == null || target.Id != GameMain.localPlanet.astroId;
        }

        /// <summary>
        /// Performs precondition checks and refreshes the current navigation target.
        /// </summary>
        /// <returns>True when auto-cruise may drive the mecha this tick.</returns>
        private static bool TryRefreshNavigationTarget(Player player)
        {
            if (!AutoCruiseEnabled.Value)
            {
                StopAutoNavigation();
                return false;
            }
            if (!_enabled)
                return false;
            if (player.mecha.thrusterLevel < 2)
            {
                StopAutoNavigation();
                return false;
            }
            // The game's own autopilot zeroes the movement input inside PlayerMove_Sail/PlayerMove_Fly
            // and drives uVelocity itself. Yield to it instead of fighting over the same state, the same
            // way AutoConstructPatch does.
            if (player.navigation.navigating)
            {
                StopAutoNavigation();
                return false;
            }
            if (global::UXAssist.Common.Utils.PlayerInputUtil.HasManualMovementInput())
            {
                if (StopOnArrivalAndInput.Value)
                    StopAutoNavigation();
                return false;
            }

            if (!TryResolveNavigationTarget(player, out NavigationTarget target))
            {
                StopAutoNavigation();
                return false;
            }

            bool targetChanged = _targetId != target.Id || _targetType != target.Type;
            _targetId                    = target.Id;
            _targetType                  = target.Type;
            _targetUniversePosition      = target.Position;
            _targetUniverseVelocity      = target.Velocity;
            _targetArriveThresholdRadius = target.ArrivalRadius;
            if (targetChanged)
            {
                _followModeLock = false;
            }
            return true;
        }

        private static bool TryResolveNavigationTarget(Player player, out NavigationTarget target)
        {
            target = default;
            PlayerNavigation navigation = player.navigation;
            int indicatorAstroId = navigation.indicatorAstroId;
            if (indicatorAstroId > DarkFogAstroIdStart)
            {
                int astroIndex = indicatorAstroId - DarkFogAstroIdStart;
                if (astroIndex < 0 || astroIndex >= GameMain.spaceSector.astros.Length)
                    return false;

                AstroData astro = GameMain.spaceSector.astros[astroIndex];
                VectorLF3 localPosition = default;
                astro.VelocityU(ref localPosition, out Vector3 velocity);
                target = new NavigationTarget(
                    indicatorAstroId,
                    ESpaceGuideType.DFHive,
                    astro.uPos,
                    velocity,
                    DFHiveFollowDistance.Value * GalaxyData.AU);
                return true;
            }

            if (indicatorAstroId != 0)
            {
                if (indicatorAstroId < 0 || indicatorAstroId >= GameMain.galaxy.astrosData.Length)
                    return false;
                if (indicatorAstroId % 100 == 0)
                {
                    StarData star = GameMain.galaxy.StarById(indicatorAstroId / 100);
                    if (star == null)
                        return false;
                    target = new NavigationTarget(
                        indicatorAstroId,
                        ESpaceGuideType.Star,
                        star.uPosition,
                        default,
                        GameMain.galaxy.astrosData[indicatorAstroId].uRadius + StarArriveThresholdRadius);
                } else
                {
                    PlanetData planet = GameMain.galaxy.PlanetById(indicatorAstroId);
                    if (planet == null)
                        return false;
                    target = new NavigationTarget(
                        indicatorAstroId,
                        ESpaceGuideType.Planet,
                        planet.uPosition,
                        default,
                        planet.realRadius + PlanetArriveThresholdRadius);
                }
                return true;
            }

            int enemyId = navigation.indicatorEnemyId;
            if (enemyId <= 0 || enemyId >= GameMain.data.spaceSector.enemyPool.Length)
                return false;
            EnemyData enemy = GameMain.data.spaceSector.enemyPool[enemyId];
            if (enemy.id != enemyId)
                return false;

            VectorLF3 localPositionForVelocity = enemy.pos;
            Vector3 localVelocity = enemy.vel;
            GameMain.data.spaceSector.TransformVelocityFromAstro_ref(
                enemy.astroId,
                out Vector3 enemyVelocity,
                ref localPositionForVelocity,
                ref localVelocity);
            GameMain.data.spaceSector.TransformFromAstro_ref(enemy.astroId, out VectorLF3 position, ref enemy.pos);
            target = new NavigationTarget(enemyId, ESpaceGuideType.DFCarrier, position, enemyVelocity, DFCarrierFollowDistance.Value);
            return true;
        }

        /// <summary>
        /// Steers toward the target while avoiding nearby obstacles.
        /// </summary>
        private static void SailToTarget(PlayerController controller, double distance)
        {
            CollectObstacles();

            Player    player       = controller.player;
            double    currentSpeed = player.uVelocity.magnitude;
            VectorLF3 preliminaryDir = SafeNorm(_targetUniversePosition - player.uPosition, default);
            VectorLF3 currentDir     = SafeNorm(player.uVelocity, preliminaryDir);
            VectorLF3 targetDir    = ComputeDirection(player.uPosition, currentDir, Obstacles);
            if (player.warping)
            {
                // Leave warp early enough that the ordinary sail brake can still settle the mecha before
                // the arrival radius. Mirrors the 0.1 s look-ahead PlayerMove_Sail uses for its own
                // planet warp guard, which does not cover star targets at all.
                PlayerMove_Sail sail = controller.actionSail;
                double warpExitDistance = _targetArriveThresholdRadius
                                          + sail.currentWarpSpeed * 0.1
                                          + player.mecha.maxSailSpeed * SailBrakeSecondsPerSpeed;
                if (distance <= warpExitDistance)
                    player.warpCommand = false;
                UpdateSailVelocity(controller, currentDir, currentSpeed, targetDir, currentSpeed);
                return;
            }

            Mecha mecha = player.mecha;
            bool canWarp = UseWarper.Value
                           && mecha.coreEnergy           > UseWarperMinimalEnergy.Value * 1000 * 1000
                           && mecha.coreEnergy           > mecha.warpStartPowerPerSpeed * controller.actionSail.maxWarpSpeed
                           && player.mecha.thrusterLevel >= 3
                           && player.mecha.HasWarper()
                           && GameMain.localPlanet == null
                           && distance             > GalaxyData.AU * UseWarperDistance.Value;
            if (canWarp && player.mecha.UseWarper())
            {
                UpdateSailVelocity(controller, currentDir, currentSpeed, targetDir, currentSpeed);
                player.warpCommand = true;
                VFAudio.Create("warp-begin", player.transform, Vector3.zero, true);
                return;
            }

            UpdateSailVelocity(controller, currentDir, currentSpeed, targetDir, GetApproachSpeedLimit(mecha, distance));
        }

        /// <summary>
        /// Caps the cruise speed so the sail brake can still bring the mecha down to a usable arrival
        /// speed by the time it reaches the arrival radius of a fixed target.
        /// </summary>
        private static double GetApproachSpeedLimit(Mecha mecha, double distance)
        {
            double arrivalSpeed = _targetType == ESpaceGuideType.Planet
                ? PlanetApproachArrivalSpeed
                : StarApproachArrivalSpeed;
            double remaining = distance - _targetArriveThresholdRadius;
            if (remaining <= 0.0)
                return arrivalSpeed;
            return Math.Min(mecha.maxSailSpeed, arrivalSpeed + remaining / SailBrakeSecondsPerSpeed);
        }

        /// <summary>
        /// Planet frame velocity blended by altitude, matching <c>PlayerMove_Sail.GameTick</c> and
        /// <c>PlayerNavigation.DetermineSailVelocity</c>. Subtracting the unblended planet velocity
        /// would corrupt <c>visual_uvel</c>, which the game turns into the mecha's heading and the
        /// speed readout.
        /// </summary>
        private static VectorLF3 GetReferenceFrameVelocity(Player player)
        {
            PlanetData localPlanet = GameMain.localPlanet;
            if (localPlanet == null)
                return default;

            double altitude = (player.uPosition - localPlanet.uPosition).magnitude - localPlanet.realRadius;
            double weight = Clamp((ReferenceFrameFadeAltitude - altitude) / ReferenceFrameFadeRange, 0.0, 1.0);
            if (weight <= 0.0)
                return default;

            return localPlanet.GetUniversalVelocityAtLocalPoint(GameMain.gameTime, player.position) * weight;
        }

        private static void CollectObstacles()
        {
            Obstacles.Clear();
            StarData localStar = GameMain.localStar;
            if (localStar == null)
                return;

            if (localStar.astroId != _targetId)
                Obstacles.Add(new Obstacle(
                    localStar.uPosition,
                    GameMain.galaxy.astrosData[localStar.astroId].uRadius,
                    StarSafeRadius));

            foreach (PlanetData planet in localStar.planets)
            {
                if (planet.astroId != _targetId)
                    Obstacles.Add(new Obstacle(planet.uPosition, planet.realRadius, PlanetSafeRadius));
            }

            if (_targetType == ESpaceGuideType.DFHive)
                return;

            // SpaceSector.Init does not allocate dfHives; only SetForNewGame does, and Import only does
            // so when the save actually carries hive data. The game null-checks it everywhere for that
            // reason, and this runs on every sailing tick.
            SpaceSector sector = GameMain.spaceSector;
            EnemyDFHiveSystem[] hives = sector.dfHives;
            if (hives == null || localStar.index < 0 || localStar.index >= hives.Length)
                return;

            AstroData[] astros = sector.astros;
            for (EnemyDFHiveSystem hiveSys = hives[localStar.index]; hiveSys != null; hiveSys = hiveSys.nextSibling)
            {
                if (hiveSys is not { realized: true, hiveAstroId: > DarkFogAstroIdStart }
                    || hiveSys.hiveAstroId == _targetId)
                    continue;

                int astroIndex = hiveSys.hiveAstroId - DarkFogAstroIdStart;
                if (astros == null || astroIndex >= astros.Length)
                    continue;

                Obstacles.Add(new Obstacle(astros[astroIndex].uPos, 0, HiveSafeRadius));
            }
        }

        /// <summary>
        /// Computes a target direction with obstacle avoidance.
        /// </summary>
        /// <param name="playerPos">Player position.</param>
        /// <param name="obstacles">Nearby obstacles.</param>
        /// <returns></returns>
        private static VectorLF3 ComputeDirection(VectorLF3 playerPos, VectorLF3 currentDirection, List<Obstacle> obstacles)
        {
            VectorLF3 toTarget = _targetUniversePosition - playerPos;
            double    toTargetSqr = toTarget.sqrMagnitude;
            if (toTargetSqr < Epsilon)
                return default;

            VectorLF3 baseDir = toTarget.normalized;
            foreach (Obstacle obstacle in obstacles)
            {
                double pathRadius = GetReachablePathRadius(obstacle);
                if (pathRadius <= 0.0)
                    continue;

                VectorLF3 toObstacle = obstacle.Position - playerPos;
                double obstacleDistance = toObstacle.magnitude;
                if (obstacleDistance < pathRadius)
                    return ComputeEscapeDirection(currentDirection, toObstacle, baseDir);
                if (IsSegmentBlocked(playerPos, _targetUniversePosition, obstacle.Position, pathRadius))
                    return ComputeTangentDirection(currentDirection, toObstacle, pathRadius, baseDir);
            }

            return baseDir;
        }

        private static double GetReachablePathRadius(Obstacle obstacle)
        {
            double targetClearance = (obstacle.Position - _targetUniversePosition).magnitude - _targetArriveThresholdRadius;
            return Math.Min(obstacle.PathRadius, targetClearance);
        }

        private static bool IsSegmentBlocked(
            VectorLF3 start,
            VectorLF3 end,
            VectorLF3 center,
            double    pathRadius)
        {
            VectorLF3 segment = end - start;
            double    segmentLength = segment.magnitude;
            if (segmentLength < Epsilon)
                return false;

            VectorLF3 toCenter = center - start;
            double    projection = VectorLF3.Dot(toCenter, segment) / segmentLength;
            double    closestDistance = projection <= 0.0
                ? toCenter.magnitude
                : projection >= segmentLength
                    ? (center - end).magnitude
                    : Math.Sqrt(Math.Max(0.0, toCenter.sqrMagnitude - projection * projection));
            return closestDistance < pathRadius;
        }

        private static VectorLF3 ComputeEscapeDirection(VectorLF3 currentDirection, VectorLF3 toObstacle, VectorLF3 fallback)
        {
            VectorLF3 outward = SafeNorm(-toObstacle, -fallback);
            VectorLF3 tangent = currentDirection - outward * VectorLF3.Dot(currentDirection, outward);
            tangent = SafeNorm(tangent, GetPerpendicularDirection(outward, VectorLF3.unit_y));
            return SafeNorm(outward + tangent * 0.5, outward);
        }

        private static VectorLF3 ComputeTangentDirection(
            VectorLF3 currentDirection,
            VectorLF3 toObstacle,
            double    pathRadius,
            VectorLF3 fallback)
        {
            double obstacleDistance = toObstacle.magnitude;
            if (obstacleDistance < Epsilon)
                return ComputeEscapeDirection(currentDirection, toObstacle, fallback);

            VectorLF3 radial = toObstacle / obstacleDistance;
            double    cosAngle = Clamp(pathRadius / obstacleDistance, 0.0, 1.0);
            double    sinAngle = Math.Sqrt(Math.Max(0.0, 1.0 - cosAngle * cosAngle));
            VectorLF3 side = currentDirection - radial * VectorLF3.Dot(currentDirection, radial);
            if (side.sqrMagnitude < Epsilon)
                side = _targetUniverseVelocity - radial * VectorLF3.Dot(_targetUniverseVelocity, radial);
            if (side.sqrMagnitude < Epsilon)
                side = fallback - radial * VectorLF3.Dot(fallback, radial);
            if (side.sqrMagnitude < Epsilon)
                side = GetPerpendicularDirection(radial, VectorLF3.unit_y);

            return SafeNorm(radial * cosAngle + side.normalized * sinAngle, fallback);
        }

        /// <summary>
        /// Steers toward and follows a moving target without avoidance after close-range lock.
        /// <param name="distance">Distance to the target.</param>
        /// </summary>
        private static void FollowMovingTarget(PlayerController controller, double distance)
        {
            double arriveRadius            = _targetArriveThresholdRadius;
            double exitLockRadius          = arriveRadius * 1.05;
            double axialTolerance          = arriveRadius * 0.8;
            double lateralTolerance        = arriveRadius * 0.8;
            double axialSlowDownDistance   = arriveRadius * 10;
            double lateralSlowDownDistance = arriveRadius * 5;

            Player    player            = controller.player;
            double    mechaMaxSailSpeed = player.mecha.maxSailSpeed;
            VectorLF3 playerPos         = player.uPosition;
            VectorLF3 playerVelocity    = player.uVelocity;
            double    playerSpeed       = playerVelocity.magnitude;
            VectorLF3 targetPos         = _targetUniversePosition;
            VectorLF3 targetVelocity    = _targetUniverseVelocity;
            VectorLF3 toTarget          = targetPos - playerPos;
            if (distance < Epsilon)
                return;
            VectorLF3 toTargetDir       = toTarget / distance;
            VectorLF3 targetVelocityDir = SafeNorm(targetVelocity, toTargetDir);

            if (distance > FollowModeUseWarpDistance)
            {
                SailToTarget(controller, distance);
                return;
            }
            if (player.warping)
            {
                player.warpCommand = false;
                return;
            }
            if (distance > exitLockRadius)
                _followModeLock = false;
            else if (distance <= arriveRadius)
                _followModeLock = true;

            // Match the target velocity after entering the follow sphere.
            if (_followModeLock)
            {
                UpdateSailVelocity(
                    controller: controller,
                    currentDir: SafeNorm(playerVelocity, targetVelocityDir),
                    currentSpeed: playerSpeed,
                    targetDir: targetVelocityDir,
                    targetSpeed: targetVelocity.magnitude);
                return;
            }

            // Resolve the position error into axial and lateral components.
            double    axialError    = VectorLF3.Dot(toTarget, targetVelocityDir);
            VectorLF3 lateralVector = toTarget - targetVelocityDir * axialError;
            double    lateralError  = lateralVector.magnitude;
            VectorLF3 lateralDir    = lateralError > Epsilon ? lateralVector / lateralError : default;

            // Axial approach.
            double axialEffectiveError = Math.Max(Math.Abs(axialError) - axialTolerance, 0.0);
            double axialApproachSpeed = mechaMaxSailSpeed * axialEffectiveError / (axialEffectiveError + axialSlowDownDistance);
            VectorLF3 axialDir = axialError >= 0 ? targetVelocityDir : - targetVelocityDir;

            // Lateral approach.
            double lateralEffectiveError = Math.Max(lateralError - lateralTolerance, 0.0);
            double lateralApproachSpeed =
                mechaMaxSailSpeed * lateralEffectiveError / (lateralEffectiveError + lateralSlowDownDistance);

            VectorLF3 relativeVelocityDesired = axialDir * axialApproachSpeed + lateralDir * lateralApproachSpeed;

            // Residual velocity compensation.
            if (distance > arriveRadius)
            {
                double cWeight = (distance - arriveRadius) / ResidualCompensationMaxDistance;
                cWeight = Clamp(cWeight, 0, 1);
                cWeight = cWeight * cWeight * (3.0 - 2.0 * cWeight);
                double cSpeed = player.mecha.maxSailSpeed * cWeight * ResidualCompensationSpeedRatio
                                + ResidualCompensationMinSpeed;
                double dot = VectorLF3.Dot(relativeVelocityDesired, toTargetDir);
                if (dot < cSpeed)
                    relativeVelocityDesired += toTargetDir * cSpeed;
            }

            // Combine target and approach velocities.
            VectorLF3 desiredVelocity  = targetVelocity + relativeVelocityDesired;
            double    desiredSpeed     = Math.Min(desiredVelocity.magnitude, mechaMaxSailSpeed);
            VectorLF3 desiredDirection = SafeNorm(desiredVelocity, toTargetDir);
            VectorLF3 playerDir        = SafeNorm(playerVelocity,  desiredDirection);
            UpdateSailVelocity(
                controller: controller,
                currentDir: playerDir,
                currentSpeed: playerSpeed,
                targetDir: desiredDirection,
                targetSpeed: desiredSpeed);
        }

        private static VectorLF3 SafeNorm(VectorLF3 vector, VectorLF3 fallback) =>
            vector.sqrMagnitude < Epsilon ? fallback : vector.normalized;

        private static VectorLF3 GetPerpendicularDirection(VectorLF3 direction, VectorLF3 reference)
        {
            VectorLF3 perpendicular = reference - direction * VectorLF3.Dot(reference, direction);
            if (perpendicular.sqrMagnitude < Epsilon)
                perpendicular = VectorLF3.Cross(direction, VectorLF3.unit_x);
            if (perpendicular.sqrMagnitude < Epsilon)
                perpendicular = VectorLF3.Cross(direction, VectorLF3.unit_z);
            return perpendicular.sqrMagnitude < Epsilon ? VectorLF3.unit_y : perpendicular.normalized;
        }

        /// <summary>
        /// Updates sail acceleration, braking, steering and the visual velocity the game derives the
        /// mecha heading from.
        /// </summary>
        private static void UpdateSailVelocity(
            PlayerController controller,
            VectorLF3        currentDir,
            double           currentSpeed,
            VectorLF3        targetDir,
            double           targetSpeed)
        {
            PlayerMove_Sail sail         = controller.actionSail;
            Player          player       = controller.player;
            Mecha           mecha        = player.mecha;
            VectorLF3       currentVel   = currentDir * currentSpeed;
            double          desiredSpeed = Math.Min(targetSpeed, mecha.maxSailSpeed);
            double          stepSpeed    = currentSpeed;
            bool            brake        = desiredSpeed < currentSpeed && !player.warping;

            // Acceleration mirrors PlayerMove_Sail.GameTick: the step is folded into the interpolated
            // target velocity below, so the turn interpolation damps it exactly like the original does.
            bool speedUp = UseSpeedUp.Value && mecha.coreEnergy > UseSpeedUpMinimalEnergy.Value * 1000 * 1000;
            if (speedUp && desiredSpeed > currentSpeed)
            {
                double dSpeed = Clamp(currentSpeed * 0.02, 7.0, sail.max_acc);
                dSpeed = Math.Min(dSpeed, desiredSpeed       - currentSpeed);
                dSpeed = Math.Min(dSpeed, mecha.maxSailSpeed - currentSpeed);
                if (dSpeed > 0)
                    stepSpeed = currentSpeed + dSpeed * sail.UseSailEnergy(dSpeed);
            }

            _warpSteeringPending = player.warping && targetDir.sqrMagnitude > Epsilon;
            _warpHeadingBeforeRotation = player.uRotation;
            _warpTargetDirection = _warpSteeringPending ? targetDir : default;

            AdaptWarpSpeedControl(controller, targetDir);

            // Smooth velocity changes.
            VectorLF3 targetVelocity = targetDir * stepSpeed;
            float           angle           = Vector3.Angle(targetVelocity, currentVel);
            var             t               = 1.6f / Mathf.Max(10f, angle);
            VectorLF3       smoothedVelocity = SmoothVelocity(currentVel, targetVelocity, t, sail.sailPoser.targetURot * Vector3.up);
            VectorLF3       dVelocity       = smoothedVelocity - currentVel;
            sail.UseSailEnergy(ref dVelocity, 0.36);
            VectorLF3 newVelocity = currentVel + dVelocity;

            // Braking mirrors PlayerMove_Sail.GameTick's dedicated deceleration branch, which applies the
            // full per-tick decay directly. Folding it into the turn interpolation instead would weaken
            // the brake by a factor of up to 1/t (roughly 6x while flying straight at the target), which
            // is far too weak to settle below the 75 m/s the game requires before it hands over to
            // PlayerMove_Fly.
            if (brake)
            {
                VectorLF3 brakeVelocity = newVelocity * SailBrakeRatePerTick;
                sail.UseSailEnergy(ref brakeVelocity, 1.5);
                VectorLF3 brakedVelocity = newVelocity - brakeVelocity;
                newVelocity = brakedVelocity.magnitude < desiredSpeed
                    ? SafeNorm(brakedVelocity, targetDir) * desiredSpeed
                    : brakedVelocity;
            }

            sail.input_aff_1 = 1.0;
            player.uVelocity = newVelocity;
            UpdateSailVisualVelocity(player, sail, newVelocity);
        }

        [HarmonyPatch(typeof(PlayerController), "UpdateRotation")]
        [HarmonyPostfix]
        private static void PlayerController_UpdateRotation_Postfix(PlayerController __instance)
        {
            if (!_enabled || !_warpSteeringPending)
                return;

            _warpSteeringPending = false;
            Player player = __instance.player;
            if (player == null || __instance.movementStateInFrame != EMovementState.Sail || !player.warping)
                return;

            Vector3 previousDirection = _warpHeadingBeforeRotation * Vector3.forward;
            Vector3 currentDirection = player.uRotation * Vector3.forward;
            Vector3 targetDirection = (Vector3)_warpTargetDirection;
            float appliedAngle = Vector3.Angle(previousDirection, currentDirection);
            float targetAngle = Vector3.Angle(currentDirection, targetDirection);
            float remainingAngle = Mathf.Min(targetAngle, Mathf.Max(0f, WarpTurnDegreesPerTick - appliedAngle));
            if (remainingAngle < 0.01f)
                return;

            Vector3 turnAxis = GetStableTurnAxis(currentDirection, targetDirection);
            player.uRotation = Quaternion.AngleAxis(remainingAngle, turnAxis) * player.uRotation;
        }

        private static void AdaptWarpSpeedControl(PlayerController controller, VectorLF3 targetDir)
        {
            Player player = controller.player;
            if (!player.warping || VFInput._sailSpeedUp || controller.input0.y < 0f)
                return;

            Vector3 warpDirection = player.uRotation * Vector3.forward;
            float turnAngle = Vector3.Angle(warpDirection, targetDir);
            double turnSeverity = Clamp((turnAngle - 15.0) / 45.0, 0.0, 1.0);
            double targetControl = 1.0 - turnSeverity * turnSeverity * (3.0 - 2.0 * turnSeverity) * 0.8;
            double controlDelta = Clamp(
                targetControl - controller.actionSail.warpSpeedControl,
                -0.02,
                0.02);
            controller.actionSail.warpSpeedControl = Clamp(
                controller.actionSail.warpSpeedControl + controlDelta,
                0.2,
                1.0);
        }

        private static VectorLF3 SmoothVelocity(
            VectorLF3 currentVelocity,
            VectorLF3 targetVelocity,
            float     interpolation,
            VectorLF3 turnReference)
        {
            double currentMagnitude = currentVelocity.magnitude;
            double targetMagnitude  = targetVelocity.magnitude;
            if (currentMagnitude < Epsilon && targetMagnitude < Epsilon)
                return VectorLF3.zero;

            Vector3 currentDirection = currentMagnitude >= Epsilon
                ? (Vector3)(currentVelocity / currentMagnitude)
                : (targetMagnitude >= Epsilon ? (Vector3)(targetVelocity / targetMagnitude) : Vector3.forward);
            Vector3 targetDirection = targetMagnitude >= Epsilon
                ? (Vector3)(targetVelocity / targetMagnitude)
                : currentDirection;
            Vector3 direction;
            if (Vector3.Dot(currentDirection, targetDirection) < -0.9999f)
            {
                float angle = Vector3.Angle(currentDirection, targetDirection);
                Vector3 axis = GetStableTurnAxis(currentDirection, (Vector3)turnReference);
                direction = Quaternion.AngleAxis(angle * interpolation, axis) * currentDirection;
            }
            else
            {
                direction = Vector3.Slerp(currentDirection, targetDirection, interpolation);
            }

            double magnitude = currentMagnitude + (targetMagnitude - currentMagnitude) * interpolation;
            return (VectorLF3)(direction.normalized * (float)magnitude);
        }

        private static Vector3 GetStableTurnAxis(Vector3 direction, Vector3 reference)
        {
            Vector3 projectedReference = reference - direction * Vector3.Dot(reference, direction);
            if (projectedReference.sqrMagnitude < 1e-10f)
            {
                projectedReference = Vector3.Cross(direction, Vector3.right);
                if (projectedReference.sqrMagnitude < 1e-10f)
                    projectedReference = Vector3.Cross(direction, Vector3.up);
            }
            return Vector3.Cross(direction, projectedReference).normalized;
        }

        /// <summary>
        /// Keeps <c>PlayerMove_Sail.visual_uvel</c> consistent with the universal velocity we just
        /// wrote.
        /// </summary>
        /// <remarks>
        /// This intentionally does not touch <c>player.uRotation</c>. The game's own
        /// <c>PlayerController.UpdateRotation</c> runs later in the same tick, after the whole action
        /// loop, and recomputes the sailing rotation from <c>visual_uvel</c> anyway, so any rotation
        /// written here would simply be overwritten. The warp heading, which the game does not steer
        /// fast enough on its own, is topped up afterwards in
        /// <see cref="PlayerController_UpdateRotation_Postfix"/>.
        /// </remarks>
        private static void UpdateSailVisualVelocity(Player player, PlayerMove_Sail sail, VectorLF3 newVelocity)
        {
            sail.visual_uvel = newVelocity - GetReferenceFrameVelocity(player);
        }

        private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        // Harmony transpiler: PlayerController_GameTick_Transpiler
        // Target: PlayerController.GameTick
        // Fallback: Return the original instructions if the insertion point cannot be found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerController_GameTick_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator                  generator)
        {
            var original = new List<CodeInstruction>(instructions);
            var matcher = new CodeMatcher(original, generator);
            matcher.MatchForward(
                false,
                new CodeMatch(
                    OpCodes.Callvirt,
                    AccessTools.Method(typeof(BuildModel), nameof(BuildModel.EarlyGameTickIgnoreActive)))
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((PlayerController controller) =>
                {
                    EMovementState movementState = controller.movementStateInFrame;
                    if (movementState is not (EMovementState.Walk or EMovementState.Drift or EMovementState.Fly))
                        return;
                    if (!TryRefreshNavigationTarget(controller.player) || HasArrivedLocally())
                        return;
                    if (movementState == EMovementState.Fly)
                    {
                        // PlayerController.GetInput maps input0.y to forward movement and input1.y to the
                        // vertical thruster, so this is exactly manual "hold forward and climb".
                        controller.input1.y = 1f;
                        controller.input0.y = 1f;
                    }
                    else
                        // input0.z is the jump axis. PlayerMove_Walk turns the first press into a jump and
                        // the next one into SwitchToFly, so holding it lifts off within two ticks.
                        controller.input0.z = 1f;
                })
            );
            return matcher.Finish(original, UXAssist.Logger, nameof(PlayerController_GameTick_Transpiler));

            // Detect arrival while walking or flying.
            static bool HasArrivedLocally()
            {
                if (_targetId != 0 && (_targetType != ESpaceGuideType.Planet || GameMain.localPlanet?.astroId != _targetId))
                    return false;
                StopAutoNavigation();
                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerMove_Sail), nameof(PlayerMove_Sail.GameTick)), HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void PlayerMoveSailPostfix(PlayerMove_Sail __instance)
        {
            PlayerController controller = __instance.controller;
            Player           player     = __instance.player;
            if (!TryRefreshNavigationTarget(player))
                return;
            bool isSailing = controller.movementStateInFrame == EMovementState.Sail;
            bool enteredSailing = isSailing && !_wasSailing;
            _wasSailing = isSailing;
            if (!isSailing)
                return;
            VectorLF3 playerPos      = player.uPosition;
            VectorLF3 targetVector   = _targetUniversePosition - playerPos;
            double    distance       = targetVector.magnitude;
            if (enteredSailing && targetVector.sqrMagnitude >= Epsilon)
                __instance.sailPoser.targetURotWanted = Quaternion.LookRotation(targetVector);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (_targetType)
            {
                case ESpaceGuideType.Star:
                case ESpaceGuideType.Planet:
                    // Stop after reaching a fixed target.
                    if (distance < _targetArriveThresholdRadius)
                    {
                        if (player.warping)
                            player.warpCommand = false;
                        StopAutoNavigation();
                        return;
                    }
                    SailToTarget(controller, distance);
                    break;
                case ESpaceGuideType.DFHive:
                case ESpaceGuideType.DFCarrier:
                    FollowMovingTarget(controller, distance);
                    break;
            }
        }
    }

    private readonly struct NavigationTarget(
        int id,
        ESpaceGuideType type,
        VectorLF3 position,
        VectorLF3 velocity,
        double arrivalRadius)
    {
        public int             Id            { get; } = id;
        public ESpaceGuideType Type          { get; } = type;
        public VectorLF3       Position      { get; } = position;
        public VectorLF3       Velocity      { get; } = velocity;
        public double          ArrivalRadius { get; } = arrivalRadius;
    }

    private readonly struct Obstacle(VectorLF3 position, double radius, double safetyMargin)
    {
        public VectorLF3 Position   { get; } = position;
        public double    PathRadius { get; } = radius + safetyMargin;
    }

    #endregion
}
