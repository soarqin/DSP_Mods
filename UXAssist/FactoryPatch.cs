using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist;

public static class FactoryPatch
{
    public static ConfigEntry<bool> UnlimitInteractiveEnabled;
    public static ConfigEntry<bool> RemoveSomeConditionEnabled;
    public static ConfigEntry<bool> NightLightEnabled;
    public static ConfigEntry<bool> RemoveBuildRangeLimitEnabled;
    public static ConfigEntry<bool> LargerAreaForUpgradeAndDismantleEnabled;
    public static ConfigEntry<bool> LargerAreaForTerraformEnabled;
    public static ConfigEntry<bool> OffGridBuildingEnabled;
    public static ConfigEntry<bool> TreatStackingAsSingleEnabled;
    public static ConfigEntry<bool> QuickBuildAndDismantleLabsEnabled;
    public static ConfigEntry<bool> ProtectVeinsFromExhaustionEnabled;
    public static ConfigEntry<bool> DoNotRenderEntitiesEnabled;
    public static ConfigEntry<bool> DragBuildPowerPolesEnabled;
    public static ConfigEntry<bool> BeltSignalsForBuyOutEnabled;
    private static PressKeyBind _doNotRenderEntitiesKey;

    private static Harmony _factoryPatch;

    public static void Init()
    {
        _doNotRenderEntitiesKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
            {
                key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
                conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
                name = "ToggleDoNotRenderEntities",
                canOverride = true
            }
        );
        I18N.Add("KEYToggleDoNotRenderEntities", "Toggle Do Not Render Factory Entities", "切换不渲染工厂建筑实体");

        BeltSignalsForBuyOut.InitPersist();
        UnlimitInteractiveEnabled.SettingChanged += (_, _) => UnlimitInteractive.Enable(UnlimitInteractiveEnabled.Value);
        RemoveSomeConditionEnabled.SettingChanged += (_, _) => RemoveSomeConditionBuild.Enable(RemoveSomeConditionEnabled.Value);
        NightLightEnabled.SettingChanged += (_, _) => NightLight.Enable(NightLightEnabled.Value);
        RemoveBuildRangeLimitEnabled.SettingChanged += (_, _) => RemoveBuildRangeLimit.Enable(RemoveBuildRangeLimitEnabled.Value);
        LargerAreaForUpgradeAndDismantleEnabled.SettingChanged += (_, _) => LargerAreaForUpgradeAndDismantle.Enable(LargerAreaForUpgradeAndDismantleEnabled.Value);
        LargerAreaForTerraformEnabled.SettingChanged += (_, _) => LargerAreaForTerraform.Enable(LargerAreaForTerraformEnabled.Value);
        OffGridBuildingEnabled.SettingChanged += (_, _) => OffGridBuilding.Enable(OffGridBuildingEnabled.Value);
        TreatStackingAsSingleEnabled.SettingChanged += (_, _) => TreatStackingAsSingle.Enable(TreatStackingAsSingleEnabled.Value);
        QuickBuildAndDismantleLabsEnabled.SettingChanged += (_, _) => QuickBuildAndDismantleLab.Enable(QuickBuildAndDismantleLabsEnabled.Value);
        ProtectVeinsFromExhaustionEnabled.SettingChanged += (_, _) => ProtectVeinsFromExhaustion.Enable(ProtectVeinsFromExhaustionEnabled.Value);
        DoNotRenderEntitiesEnabled.SettingChanged += (_, _) => DoNotRenderEntities.Enable(DoNotRenderEntitiesEnabled.Value);
        DragBuildPowerPolesEnabled.SettingChanged += (_, _) => DragBuildPowerPoles.Enable(DragBuildPowerPolesEnabled.Value);
        BeltSignalsForBuyOutEnabled.SettingChanged += (_, _) => BeltSignalsForBuyOut.Enable(BeltSignalsForBuyOutEnabled.Value);
        UnlimitInteractive.Enable(UnlimitInteractiveEnabled.Value);
        RemoveSomeConditionBuild.Enable(RemoveSomeConditionEnabled.Value);
        NightLight.Enable(NightLightEnabled.Value);
        RemoveBuildRangeLimit.Enable(RemoveBuildRangeLimitEnabled.Value);
        LargerAreaForUpgradeAndDismantle.Enable(LargerAreaForUpgradeAndDismantleEnabled.Value);
        LargerAreaForTerraform.Enable(LargerAreaForTerraformEnabled.Value);
        OffGridBuilding.Enable(OffGridBuildingEnabled.Value);
        TreatStackingAsSingle.Enable(TreatStackingAsSingleEnabled.Value);
        QuickBuildAndDismantleLab.Enable(QuickBuildAndDismantleLabsEnabled.Value);
        ProtectVeinsFromExhaustion.Enable(ProtectVeinsFromExhaustionEnabled.Value);
        DoNotRenderEntities.Enable(DoNotRenderEntitiesEnabled.Value);
        DragBuildPowerPoles.Enable(DragBuildPowerPolesEnabled.Value);
        BeltSignalsForBuyOut.Enable(BeltSignalsForBuyOutEnabled.Value);

        _factoryPatch ??= Harmony.CreateAndPatchAll(typeof(FactoryPatch));
    }

    public static void Uninit()
    {
        RemoveSomeConditionBuild.Enable(false);
        UnlimitInteractive.Enable(false);
        NightLight.Enable(false);
        RemoveBuildRangeLimit.Enable(false);
        LargerAreaForUpgradeAndDismantle.Enable(false);
        LargerAreaForTerraform.Enable(false);
        OffGridBuilding.Enable(false);
        TreatStackingAsSingle.Enable(false);
        QuickBuildAndDismantleLab.Enable(false);
        ProtectVeinsFromExhaustion.Enable(false);
        DoNotRenderEntities.Enable(false);
        DragBuildPowerPoles.Enable(false);
        BeltSignalsForBuyOut.Enable(false);
        BeltSignalsForBuyOut.UninitPersist();

        _factoryPatch?.UnpatchSelf();
        _factoryPatch = null;
    }

    public static void OnUpdate()
    {
        if (_doNotRenderEntitiesKey.keyValue)
            DoNotRenderEntitiesEnabled.Value = !DoNotRenderEntitiesEnabled.Value;
    }

    public static void Export(BinaryWriter w)
    {
        var storage = BeltSignalsForBuyOut.DarkFogItemsInVoid;
        for (var i = 0; i < 6; i++)
            w.Write(storage[i]);
    }

    public static void Import(BinaryReader r)
    {
        var storage = BeltSignalsForBuyOut.DarkFogItemsInVoid;
        for (var i = 0; i < 6; i++)
            storage[i] = r.ReadInt32();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConnGizmoGraph), MethodType.Constructor)]
    private static IEnumerable<CodeInstruction> ConnGizmoGraph_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(256))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ConnGizmoGraph), nameof(ConnGizmoGraph.SetPointCount))]
    private static IEnumerable<CodeInstruction> ConnGizmoGraph_SetPointCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(256))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path._OnInit))]
    private static IEnumerable<CodeInstruction> BuildTool_Path__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(160))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), MethodType.Constructor)]
    private static IEnumerable<CodeInstruction> BuildTool_Reform_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(100))
        );
        matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 900));
        return matcher.InstructionEnumeration();
    }

    public static class NightLight
    {
        private static Harmony _patch;
        private const float NightLightAngleX = -8;
        private const float NightLightAngleY = -2;
        public static bool Enabled;
        private static bool _nightlightInitialized;
        private static bool _mechaOnEarth;
        private static AnimationState _sail;
        private static Light _sunlight;

        public static void Enable(bool on)
        {
            if (on)
            {
                Enabled = _mechaOnEarth;
                _patch ??= Harmony.CreateAndPatchAll(typeof(NightLight));
                return;
            }

            Enabled = false;
            _patch?.UnpatchSelf();
            _patch = null;
            if (_sunlight == null) return;
            _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
        }

        public static void LateUpdate()
        {
            if (_patch == null) return;

            switch (_nightlightInitialized)
            {
                case false:
                    Ready();
                    break;
                case true:
                    Go();
                    break;
            }
        }

        private static void Ready()
        {
            if (!GameMain.isRunning || !GameMain.mainPlayer.controller.model.gameObject.activeInHierarchy) return;
            if (_sail == null)
            {
                _sail = GameMain.mainPlayer.animator.sails[GameMain.mainPlayer.animator.sailAnimIndex];
            }

            _nightlightInitialized = true;
        }

        private static void Go()
        {
            if (!GameMain.isRunning)
            {
                End();
                return;
            }

            if (_sail && _sail.enabled)
            {
                _mechaOnEarth = false;
                Enabled = false;
                if (!_sunlight || !_sunlight.transform) return;
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
                return;
            }

            if (!_mechaOnEarth)
            {
                if (!_sunlight)
                {
                    var simu = GameMain.universeSimulator;
                    if (simu)
                        _sunlight = simu.LocalStarSimulator()?.sunLight;
                    if (!_sunlight) return;
                }

                _mechaOnEarth = true;
                Enabled = NightLightEnabled.Value;
            }

            if (Enabled && _sunlight)
            {
                _sunlight.transform.rotation =
                    Quaternion.LookRotation(-GameMain.mainPlayer.transform.up + GameMain.mainPlayer.transform.forward * NightLightAngleX / 10f +
                                            GameMain.mainPlayer.transform.right * NightLightAngleY / 10f);
            }
        }

        private static void End()
        {
            _mechaOnEarth = false;
            Enabled = false;
            if (_sunlight != null)
            {
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
            }

            _sail = null;
            _nightlightInitialized = false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StarSimulator), "LateUpdate")]
        private static IEnumerable<CodeInstruction> StarSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // var vec = NightlightEnabled ? GameMain.mainPlayer.transform.up : __instance.transform.forward;
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform)))
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NightLight), nameof(NightLight.Enabled))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
                new CodeInstruction(OpCodes.Stloc_0),
                new CodeInstruction(OpCodes.Br_S, label2)
            );
            matcher.Labels.Add(label1);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_0)
            ).Advance(1).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetSimulator), "LateUpdate")]
        private static IEnumerable<CodeInstruction> PlanetSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // var vec = (NightlightEnabled ? GameMain.mainPlayer.transform.up : (Quaternion.Inverse(localPlanet.runtimeRotation) * (__instance.planetData.star.uPosition - __instance.planetData.uPosition).normalized));
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NightLight), nameof(NightLight.Enabled))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
                new CodeInstruction(OpCodes.Stloc_2),
                new CodeInstruction(OpCodes.Br_S, label2)
            );
            matcher.Labels.Add(label1);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryModel), nameof(FactoryModel.whiteMode0)))
            ).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }
    }

    private static class UnlimitInteractive
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(UnlimitInteractive));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GetObjectSelectDistance))]
        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GetObjectSelectDistance_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_R4, 10000f);
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    private static class RemoveSomeConditionBuild
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(RemoveSomeConditionBuild));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* search for:
             * ldloc.s	V_8 (8)
             * ldfld	class PrefabDesc BuildPreview::desc
             * ldfld	bool PrefabDesc::isInserter
             * brtrue	2358 (1C12) ldloc.s V_8 (8)
             * ldloca.s	V_10 (10)
             * call	instance float32 [UnityEngine.CoreModule]UnityEngine.Vector3::get_magnitude()
             */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isInserter))),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldloca_S),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.magnitude)))
            );
            var jumpPos = matcher.InstructionAt(3).operand;
            var labels = matcher.Labels;
            matcher.Labels = [];
            /* Insert: br   2358 (1C12) ldloc.s V_8 (8)
             */
            matcher.Insert(new CodeInstruction(OpCodes.Br, jumpPos).WithLabels(labels));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* search for:
             * ldloc.s	V_88 (88)
             * ldloc.s	V_120 (120)
             * brtrue.s	2054 (173A) ldc.i4.s 17
             * ldc.i4.s	EBuildCondition.JointCannotLift (19)
             * br.s	2055 (173C) stfld valuetype EBuildCondition BuildPreview::condition
             * ldc.i4.s	EBuildCondition.TooBendToLift (18)
             * stfld	valuetype EBuildCondition BuildPreview::condition
             */
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs((int)EBuildCondition.JointCannotLift)),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs((int)EBuildCondition.TooBendToLift)),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
            );
            if (matcher.IsValid)
            {
                // Remove 7 instructions, if the following instruction is br/br.s, remove it as well
                var labels = matcher.Labels;
                matcher.Labels = [];
                matcher.RemoveInstructions(7);
                var opcode = matcher.Opcode;
                if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                    matcher.RemoveInstruction();
                matcher.Labels.AddRange(labels);
            }

            /* search for:
             * ldloc.s	V_88 (88)
             * ldc.i4.s	EBuildCondition.TooSteep(16)-EBuildCondition.InputConflict(20)
             * stfld	valuetype EBuildCondition BuildPreview::condition
             */
            matcher.Start().MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldloc_S || instr.opcode == OpCodes.Ldloc),
                new CodeMatch(instr => (instr.opcode == OpCodes.Ldc_I4_S || instr.opcode == OpCodes.Ldc_I4) && Convert.ToInt64(instr.operand) is >= (int)EBuildCondition.TooSteep and <= (int)EBuildCondition.InputConflict),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
            );
            if (matcher.IsValid)
            {
                // Remove 3 instructions, if the following instruction is br/br.s, remove it as well
                matcher.Repeat(codeMatcher =>
                {
                    var labels = codeMatcher.Labels;
                    codeMatcher.Labels = [];
                    codeMatcher.RemoveInstructions(3);
                    var opcode = codeMatcher.Opcode;
                    if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                        codeMatcher.RemoveInstruction();
                    codeMatcher.Labels.AddRange(labels);
                });
            }

            return matcher.InstructionEnumeration();
        }
    }

    private static class RemoveBuildRangeLimit
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(RemoveBuildRangeLimit));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }

            var controller = GameMain.mainPlayer?.controller;
            if (controller == null) return;
            controller.actionBuild?.clickTool?._OnInit();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnInit))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(15))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 512));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DetermineMoreChainTargets))]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DetermineMoreChainTargets))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildAreaLimitRemoval_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Patch (player.mecha.buildArea * player.mecha.buildArea) to 100000000 */
            matcher.MatchForward(false,
                new CodeMatch(),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.buildArea))),
                new CodeMatch(),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.buildArea))),
                new CodeMatch(OpCodes.Mul)
            );
            matcher.Repeat(m => m.RemoveInstructions(9).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, 100000000.0f)));
            return matcher.InstructionEnumeration();
        }
    }

    private static class LargerAreaForUpgradeAndDismantle
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(LargerAreaForUpgradeAndDismantle));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTools_CursorSizePatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(11))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4_S, 31));
            return matcher.InstructionEnumeration();
        }
    }

    private static class LargerAreaForTerraform
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(LargerAreaForTerraform));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.brushSize))),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10))
            );
            matcher.Repeat(m => m.Advance(1).SetAndAdvance(OpCodes.Ldc_I4_S, 30));
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(10)),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Reform), nameof(BuildTool_Reform.brushSize)))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4_S, 30));
            return matcher.InstructionEnumeration();
        }
    }

    public static class OffGridBuilding
    {
        private static Harmony _patch;
        private const float SteppedRotationDegrees = 15f;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(OffGridBuilding));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static bool _initialized;

        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), "_OnOpen")]
        public static void UIRoot__OnOpen_Postfix()
        {
            if (_initialized) return;
            UIGeneralTips.instance.buildCursorTextComp.supportRichText = true;
            UIGeneralTips.instance.entityBriefInfo.entityNameText.supportRichText = true;
            _initialized = true;
        }

        private static void CalculateGridOffset(PlanetData planet, Vector3 pos, out float x, out float y, out float z)
        {
            var npos = pos.normalized;
            var segment = planet.aux.activeGrid?.segment ?? 200;
            var latitudeRadPerGrid = BlueprintUtils.GetLatitudeRadPerGrid(segment);
            var longitudeSegmentCount = BlueprintUtils.GetLongitudeSegmentCount(npos, segment);
            var longitudeRadPerGrid = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount, segment);
            var latitudeRad = BlueprintUtils.GetLatitudeRad(npos);
            var longitudeRad = BlueprintUtils.GetLongitudeRad(npos);
            x = longitudeRad / longitudeRadPerGrid;
            y = latitudeRad / latitudeRadPerGrid;
            z = (pos.magnitude - planet.realRadius - 0.2f) / 1.3333333f;
        }

        private static string FormatOffsetFloat(float f)
        {
            return f.ToString("0.0000").TrimEnd('0').TrimEnd('.');
        }

        private static PlanetData _lastPlanet;
        private static Vector3 _lastPos;
        private static string _lastOffsetText;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static void BuildTool_Click_CheckBuildConditions_Postfix(BuildTool __instance)
        {
            var cnt = __instance.buildPreviews.Count;
            if (cnt == 0) return;
            var preview = __instance.buildPreviews[cnt - 1];
            if (preview.desc.isInserter) return;
            var planet = __instance.planet;
            if (_lastPlanet != planet || _lastPos != preview.lpos)
            {
                CalculateGridOffset(__instance.planet, preview.lpos, out var x, out var y, out var z);
                _lastPlanet = planet;
                _lastPos = preview.lpos;
                _lastOffsetText = z is < 0.001f and > -0.001f
                    ? $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>"
                    : $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>,<color=#bfbfffff>{FormatOffsetFloat(z)}</color>";
            }

            __instance.actionBuild.model.cursorText = $"({_lastOffsetText})\n" + __instance.actionBuild.model.cursorText;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIEntityBriefInfo__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo.entityNameText))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(UnityEngine.UI.Text), nameof(UnityEngine.UI.Text.preferredWidth)))
            );
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((UIEntityBriefInfo entityBriefInfo) =>
                    {
                        var entity = entityBriefInfo.factory.entityPool[entityBriefInfo.entityId];
                        if (entity.inserterId > 0) return;
                        var planet = entityBriefInfo.factory.planet;
                        if (_lastPlanet != planet || _lastPos != entity.pos)
                        {
                            CalculateGridOffset(planet, entity.pos, out var x, out var y, out var z);
                            _lastPlanet = planet;
                            _lastPos = entity.pos;
                            _lastOffsetText = $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>,<color=#bfbfffff>{FormatOffsetFloat(z)}</color>";
                        }

                        entityBriefInfo.entityNameText.text += $" ({_lastOffsetText})";
                    }
                )
            );
            return matcher.InstructionEnumeration();
        }

        private static void MatchIgnoreGridAndCheckIfRotatable(CodeMatcher matcher, out Label? ifBlockEntryLabel, out Label? elseBlockEntryLabel)
        {
            Label? thisIfBlockEntryLabel = null;
            Label? thisElseBlockEntryLabel = null;

            matcher.MatchForward(false,
                new CodeMatch(ci => ci.Calls(AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._ignoreGrid)))),
                new CodeMatch(ci => ci.Branches(out thisElseBlockEntryLabel)),
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(ci => ci.LoadsConstant(EMinerType.Vein)),
                new CodeMatch(ci => ci.Branches(out thisIfBlockEntryLabel)),
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld)
            );

            ifBlockEntryLabel = thisIfBlockEntryLabel;
            elseBlockEntryLabel = thisElseBlockEntryLabel;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.UpdateRaycast))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> AllowOffGridConstruction(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            MatchIgnoreGridAndCheckIfRotatable(matcher, out var entryLabel, out _);

            if (matcher.IsInvalid)
                return instructions;

            matcher.Advance(2);
            matcher.Insert(new CodeInstruction(OpCodes.Br, entryLabel.Value));

            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> PreventDraggingWhenOffGrid(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            Label? exitLabel = null;

            matcher.MatchForward(false,
                new CodeMatch(ci => ci.Branches(out exitLabel)),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(ci => ci.LoadsConstant(1)),
                new CodeMatch(ci => ci.StoresField(AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.isDragging))))
            );

            if (matcher.IsInvalid)
                return instructions;

            matcher.Advance(1);
            matcher.Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._ignoreGrid))),
                new CodeInstruction(OpCodes.Brtrue, exitLabel)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateRaycast))]
        public static IEnumerable<CodeInstruction> AllowOffGridConstructionForPath(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.actionBuild))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerAction_Build), nameof(PlayerAction_Build.planetAux))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPos))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castTerrain))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.Snap))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPosSnapped)))
            );

            if (matcher.IsInvalid)
                return matcher.InstructionEnumeration();

            var jmp0 = generator.DefineLabel();
            var jmp1 = generator.DefineLabel();
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._switchModelStyle))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VFInput.InputValue), nameof(VFInput.InputValue.pressing))),
                new CodeInstruction(OpCodes.Brfalse, jmp0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPos))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.normalized))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.planet))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetData), nameof(PlanetData.realRadius))),
                new CodeInstruction(OpCodes.Ldc_R4, 0.2f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", [typeof(Vector3), typeof(float)])),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPosSnapped))),
                new CodeInstruction(OpCodes.Br, jmp1)
            ).Labels.Add(jmp0);
            matcher.Advance(10).Labels.Add(jmp1);

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> PatchToPerformSteppedRotate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            MatchIgnoreGridAndCheckIfRotatable(matcher, out var ifBlockEntryLabel, out var elseBlockEntryLabel);

            if (matcher.IsInvalid)
                return instructions;

            while (!matcher.Labels.Contains(elseBlockEntryLabel.Value))
                matcher.Advance(1);

            Label? ifBlockExitLabel = null;

            matcher.MatchBack(false, new CodeMatch(ci => ci.Branches(out ifBlockExitLabel)));

            if (matcher.IsInvalid)
                return instructions;

            while (!matcher.Labels.Contains(ifBlockEntryLabel.Value))
                matcher.Advance(-1);

            var instructionToClone = matcher.Instruction.Clone();
            var overwriteWith = CodeInstruction.LoadField(typeof(VFInput), nameof(VFInput.control));

            matcher.SetAndAdvance(overwriteWith.opcode, overwriteWith.operand);
            matcher.Insert(instructionToClone);
            matcher.CreateLabel(out var existingEntryLabel);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Brfalse, existingEntryLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(OffGridBuilding), nameof(OffGridBuilding.RotateStepped)),
                new CodeInstruction(OpCodes.Br, ifBlockExitLabel)
            );

            return matcher.InstructionEnumeration();
        }

        public static void RotateStepped(BuildTool_Click instance)
        {
            if (VFInput._rotate.onDown)
            {
                instance.yaw += SteppedRotationDegrees;
                instance.yaw = Mathf.Repeat(instance.yaw, 360f);
                instance.yaw = Mathf.Round(instance.yaw / SteppedRotationDegrees) * SteppedRotationDegrees;
            }

            if (VFInput._counterRotate.onDown)
            {
                instance.yaw -= SteppedRotationDegrees;
                instance.yaw = Mathf.Repeat(instance.yaw, 360f);
                instance.yaw = Mathf.Round(instance.yaw / SteppedRotationDegrees) * SteppedRotationDegrees;
            }
        }
    }
    
    public static class TreatStackingAsSingle
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(TreatStackingAsSingle));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MonitorComponent), nameof(MonitorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> MonitorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MonitorComponent), nameof(MonitorComponent.GetCargoAtIndexByFilter)))
            );
            matcher.Advance(-3);
            var localVar = matcher.Operand;
            matcher.Advance(4).Insert(
                new CodeInstruction(OpCodes.Ldloca, localVar),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Cargo), nameof(Cargo.stack)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class QuickBuildAndDismantleLab
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(QuickBuildAndDismantleLab));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static bool DetermineMoreLabsForDismantle(BuildTool dismantle, int id)
        {
            if (!VFInput._chainReaction) return true;
            var factory = dismantle.factory;
            var proto = dismantle.GetItemProto(id);
            var protoId = proto.ID;
            var prefDesc = proto.prefabDesc;
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return true;
            factory.ReadObjectConn(id, 14, out _, out var nextId, out _);
            /* We keep last lab if selected lab is not the ground one */
            if (nextId > 0)
            {
                while (true)
                {
                    factory.ReadObjectConn(nextId, 14, out _, out var nextNextId, out _);
                    if (nextNextId <= 0) break;
                    var itemProto = dismantle.GetItemProto(nextId);
                    if (itemProto.ID != protoId) break;
                    var desc = itemProto.prefabDesc;
                    var pose = dismantle.GetObjectPose(nextId);
                    var preview = new BuildPreview
                    {
                        item = itemProto,
                        desc = desc,
                        lpos = pose.position,
                        lrot = pose.rotation,
                        lpos2 = pose.position,
                        lrot2 = pose.rotation,
                        objId = nextId,
                        needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                        isConnNode = true
                    };
                    dismantle.buildPreviews.Add(preview);
                    nextId = nextNextId;
                }
            }

            nextId = id;
            while (true)
            {
                factory.ReadObjectConn(nextId, 15, out _, out var nextId2, out _);
                if (nextId2 <= 0)
                {
                    factory.ReadObjectConn(nextId, 13, out _, out nextId2, out _);
                    if (nextId2 <= 0) break;
                }
                nextId = nextId2;
                var itemProto = dismantle.GetItemProto(nextId);
                var desc = itemProto.prefabDesc;
                var pose = dismantle.GetObjectPose(nextId);
                var preview = new BuildPreview
                {
                    item = itemProto,
                    desc = desc,
                    lpos = pose.position,
                    lrot = pose.rotation,
                    lpos2 = pose.position,
                    lrot2 = pose.rotation,
                    objId = nextId,
                    needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                    isConnNode = true
                };
                dismantle.buildPreviews.Add(preview);
            }

            return false;
        }

        private static void BuildLabsToTop(BuildTool_Click click)
        {
            if (!click.multiLevelCovering || !VFInput._chainReaction) return;
            var prefDesc = click.GetPrefabDesc(click.castObjectId);
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return;
            var levelMax = prefDesc.isLab ? GameMain.history.labLevel : GameMain.history.storageLevel;
            var factory = click.factory;
            var currLevel = 2;
            var nid = click.castObjectId;
            do
            {
                factory.ReadObjectConn(nid, 14, out _, out nid, out _);
                if (nid <= 0) break;
                currLevel++;
            } while (true);

            while (currLevel < levelMax)
            {
                click.UpdateRaycast();
                click.DeterminePreviews();
                click.UpdateCollidersForCursor();
                click.UpdateCollidersForGiantBp();
                var model = click.actionBuild.model;
                click.UpdatePreviewModels(model);
                if (!click.CheckBuildConditions())
                {
                    model.ClearAllPreviewsModels();
                    model.EarlyGameTickIgnoreActive();
                    return;
                }

                click.UpdatePreviewModelConditions(model);
                click.UpdateGizmos(model);
                click.CreatePrebuilds();
                currLevel++;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isBattleBase))),
                new CodeMatch(OpCodes.Brfalse)
            ).Advance(-1);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.objId))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(DetermineMoreLabsForDismantle))),
                new CodeInstruction(OpCodes.And)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnTick))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds)))
            ).Advance(2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(BuildLabsToTop)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    public static class ProtectVeinsFromExhaustion
    {
        public static int KeepVeinAmount = 100;
        public static float KeepOilSpeed = 1f;
        private static int _keepOilAmount = Math.Max((int)(KeepOilSpeed / 0.00004f + 0.5f), 2500);

        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(ProtectVeinsFromExhaustion));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
        private static bool MinerComponent_InternalUpdate_Prefix(PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister,
            ref MinerComponent __instance, out uint __result)
        {
            if (power < 0.1f)
            {
                __result = 0U;
                return false;
            }

            var consumeCount = 0U;
            int veinId;
            int times;
            switch (__instance.type)
            {
                case EMinerType.Vein:
                    if (__instance.veinCount <= 0)
                        break;

                    if (__instance.time <= __instance.period)
                    {
                        __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * __instance.veinCount);
                        consumeCount = 1U;
                    }

                    if (__instance.time < __instance.period)
                    {
                        break;
                    }

                    veinId = __instance.veins[__instance.currentVeinIndex];
                    lock (veinPool)
                    {
                        if (veinPool[veinId].id == 0)
                        {
                            __instance.RemoveVeinFromArray(__instance.currentVeinIndex);
                            __instance.GetMinimumVeinAmount(factory, veinPool);
                            if (__instance.veinCount > 1)
                            {
                                __instance.currentVeinIndex %= __instance.veinCount;
                            }
                            else
                            {
                                __instance.currentVeinIndex = 0;
                            }

                            __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * __instance.veinCount);
                            __result = 0U;
                            return false;
                        }

                        if (__instance.productCount < 50 && (__instance.productId == 0 || __instance.productId == veinPool[veinId].productId))
                        {
                            __instance.productId = veinPool[veinId].productId;
                            times = __instance.time / __instance.period;
                            var outputCount = 0;
                            var amount = veinPool[veinId].amount;
                            if (amount > KeepVeinAmount)
                            {
                                if (miningRate > 0f)
                                {
                                    var usedCount = 0;
                                    var maxAllowed = amount - KeepVeinAmount;
                                    if (miningRate < 0.99999f)
                                    {
                                        for (var i = 0; i < times; i++)
                                        {
                                            __instance.seed = (uint)((__instance.seed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
                                            usedCount += __instance.seed / 2147483646.0 < miningRate ? 1 : 0;
                                            outputCount++;
                                            if (usedCount == maxAllowed)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        outputCount = times > maxAllowed ? maxAllowed : times;
                                        usedCount = outputCount;
                                    }

                                    if (usedCount > 0)
                                    {
                                        var groupIndex = (int)veinPool[veinId].groupIndex;
                                        amount = veinPool[veinId].amount -= usedCount;
                                        if (amount < __instance.minimumVeinAmount)
                                        {
                                            __instance.minimumVeinAmount = amount;
                                        }

                                        var veinGroups = factory.veinGroups;
                                        veinGroups[groupIndex].amount -= usedCount;
                                        factory.veinAnimPool[veinId].time = amount >= 20000 ? 0f : 1f - 0.00005f;
                                        if (amount <= 0)
                                        {
                                            var venType = (int)veinPool[veinId].type;
                                            var pos = veinPool[veinId].pos;
                                            factory.RemoveVeinWithComponents(veinId);
                                            factory.RecalculateVeinGroup(groupIndex);
                                            factory.NotifyVeinExhausted(venType, pos);
                                            __instance.RemoveVeinFromArray(__instance.currentVeinIndex);
                                            __instance.GetMinimumVeinAmount(factory, veinPool);
                                        }
                                        else
                                        {
                                            __instance.currentVeinIndex++;
                                        }
                                    }
                                }
                                else
                                {
                                    outputCount = times;
                                }

                                __instance.productCount += outputCount;
                                lock (productRegister)
                                {
                                    productRegister[__instance.productId] += outputCount;
                                    factory.AddMiningFlagUnsafe(veinPool[veinId].type);
                                    factory.AddVeinMiningFlagUnsafe(veinPool[veinId].type);
                                }
                            }
                            else if (amount <= 0)
                            {
                                __instance.RemoveVeinFromArray(__instance.currentVeinIndex);
                                __instance.GetMinimumVeinAmount(factory, veinPool);
                            }
                            else
                            {
                                __instance.currentVeinIndex++;
                            }

                            __instance.time -= __instance.period * outputCount;
                            if (__instance.veinCount > 1)
                            {
                                __instance.currentVeinIndex %= __instance.veinCount;
                            }
                            else
                            {
                                __instance.currentVeinIndex = 0;
                            }
                        }
                    }

                    break;
                case EMinerType.Oil:
                    if (__instance.veinCount <= 0)
                        break;

                    veinId = __instance.veins[0];
                    lock (veinPool)
                    {
                        var amount = veinPool[veinId].amount;
                        var workCount = amount * VeinData.oilSpeedMultiplier;
                        if (__instance.time < __instance.period)
                        {
                            __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed * workCount + 0.5f);
                            consumeCount = 1U;
                        }

                        if (__instance.time >= __instance.period && __instance.productCount < 50)
                        {
                            __instance.productId = veinPool[veinId].productId;
                            times = __instance.time / __instance.period;
                            var outputCount = 0;
                            if (miningRate > 0f && amount > _keepOilAmount)
                            {
                                var usedCount = 0;
                                var maxAllowed = amount - _keepOilAmount;
                                for (var j = 0; j < times; j++)
                                {
                                    __instance.seed = (uint)((__instance.seed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
                                    usedCount += __instance.seed / 2147483646.0 < miningRate ? 1 : 0;
                                    outputCount++;
                                }

                                if (usedCount > 0)
                                {
                                    if (usedCount > maxAllowed)
                                    {
                                        usedCount = maxAllowed;
                                    }

                                    amount = veinPool[veinId].amount -= usedCount;
                                    var veinGroups = factory.veinGroups;
                                    var groupIndex = veinPool[veinId].groupIndex;
                                    veinGroups[groupIndex].amount -= usedCount;
                                    factory.veinAnimPool[veinId].time = amount >= 25000 ? 0f : 1f - amount * VeinData.oilSpeedMultiplier;
                                    if (amount <= 2500)
                                    {
                                        factory.NotifyVeinExhausted((int)veinPool[veinId].type, veinPool[veinId].pos);
                                    }
                                }
                            }
                            else if (_keepOilAmount <= 2500)
                            {
                                outputCount = times;
                            }

                            if (outputCount > 0)
                            {
                                __instance.productCount += outputCount;
                                lock (productRegister)
                                {
                                    productRegister[__instance.productId] += outputCount;
                                }

                                __instance.time -= __instance.period * outputCount;
                            }
                        }
                    }

                    break;

                case EMinerType.Water:
                    if (__instance.time < __instance.period)
                    {
                        __instance.time += (int)(power * __instance.speedDamper * __instance.speed * miningSpeed);
                        consumeCount = 1U;
                    }

                    if (__instance.time < __instance.period) break;
                    times = __instance.time / __instance.period;
                    if (__instance.productCount >= 50) break;
                    __instance.productId = factory.planet.waterItemId;
                    do
                    {
                        if (__instance.productId > 0)
                        {
                            __instance.productCount += times;
                            lock (productRegister)
                            {
                                productRegister[__instance.productId] += times;
                                break;
                            }
                        }

                        __instance.productId = 0;
                    } while (false);

                    __instance.time -= __instance.period * times;
                    break;
            }

            if (__instance.productCount > 0 && __instance.insertTarget > 0 && __instance.productId > 0)
            {
                var multiplier = 36000000.0 / __instance.period * miningSpeed;
                if (__instance.type == EMinerType.Vein)
                {
                    multiplier *= __instance.veinCount;
                }
                else if (__instance.type == EMinerType.Oil)
                {
                    multiplier *= veinPool[__instance.veins[0]].amount * VeinData.oilSpeedMultiplier;
                }

                var count = (int)(multiplier - 0.01) / 1800 + 1;
                count = count < 4 ? count < 1 ? 1 : count : 4;
                var stack = __instance.productCount < count ? __instance.productCount : count;
                var outputCount = factory.InsertInto(__instance.insertTarget, 0, __instance.productId, (byte)stack, 0, out _);
                __instance.productCount -= outputCount;
                if (__instance.productCount == 0 && __instance.type == EMinerType.Vein)
                {
                    __instance.productId = 0;
                }
            }

            __result = consumeCount;
            return false;
        }
    }

    private static class DoNotRenderEntities
    {
        private static Harmony _patch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(DoNotRenderEntities));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ObjectRenderer), nameof(ObjectRenderer.Render))]
        [HarmonyPatch(typeof(DynamicRenderer), nameof(DynamicRenderer.Render))]
        private static bool ObjectRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabRenderer), nameof(LabRenderer.Render))]
        private static bool LabRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GPUInstancingManager), nameof(GPUInstancingManager.Render))]
        private static void FactoryModel_DrawInstancedBatches_Postfix(GPUInstancingManager __instance)
        {
            __instance.renderEntity = true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(RaycastLogic), nameof(RaycastLogic.GameTick))]
        private static IEnumerable<CodeInstruction> RaycastLogic_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Brtrue));
            var branch1 = (Label)matcher.Advance(1).Operand;
            var branch2 = generator.DefineLabel();
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldarg_0)
            ).Advance(8).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, 45),
                new CodeInstruction(OpCodes.Brtrue, branch2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, 33),
                new CodeInstruction(OpCodes.Ldloc_S, 35),
                Transpilers.EmitDelegate((RaycastLogic l, ColliderData[] colliderPool, int j) => l.factory.entityPool[colliderPool[j].objId].inserterId > 0),
                new CodeInstruction(OpCodes.Brfalse, branch1)
            );
            matcher.Labels.Add(branch2);
            return matcher.InstructionEnumeration();
        }
    }

    private static class DragBuildPowerPoles
    {
        private static Harmony _patch;
        private static readonly List<bool> OldDragBuild = [];
        private static readonly List<Vector2> OldDragBuildDist = [];
        private static readonly int[] PowerPoleIds = [2201, 2202, 2212];

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(DragBuildPowerPoles));
                GameLogic.OnGameBegin += GameMain_Begin_Postfix;
                GameLogic.OnGameEnd += GameMain_End_Postfix;
                FixProto();
                return;
            }

            UnfixProto();
            GameLogic.OnGameEnd -= GameMain_End_Postfix;
            GameLogic.OnGameBegin -= GameMain_Begin_Postfix;
            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static bool IsPowerPole(int id)
        {
            return PowerPoleIds.Contains(id);
        }

        private static void FixProto()
        {
            if (DSPGame.IsMenuDemo) return;
            OldDragBuild.Clear();
            OldDragBuildDist.Clear();
            foreach (var id in PowerPoleIds)
            {
                var powerPole = LDB.items.Select(id);
                if (powerPole?.prefabDesc == null) return;
                OldDragBuild.Add(powerPole.prefabDesc.dragBuild);
                OldDragBuildDist.Add(powerPole.prefabDesc.dragBuildDist);
                powerPole.prefabDesc.dragBuild = true;
                var distance = (id == 2201 ? LDB.items.Select(2202) : powerPole).prefabDesc.powerConnectDistance - 0.72f;
                powerPole.prefabDesc.dragBuildDist = new Vector2(distance, distance);
            }
        }

        private static void UnfixProto()
        {
            if (_patch == null || OldDragBuild.Count < 3 || DSPGame.IsMenuDemo) return;
            var i = 0;
            foreach (var id in PowerPoleIds)
            {
                var powerPole = LDB.items.Select(id);
                if (powerPole?.prefabDesc != null)
                {
                    powerPole.prefabDesc.dragBuild = OldDragBuild[i];
                    powerPole.prefabDesc.dragBuildDist = OldDragBuildDist[i];
                }

                i++;
            }

            OldDragBuild.Clear();
            OldDragBuildDist.Clear();
        }

        private static void GameMain_Begin_Postfix()
        {
            FixProto();
        }

        private static void GameMain_End_Postfix()
        {
            UnfixProto();
        }

        private static int PlanetGridSnapDotsNonAllocNotAligned(PlanetGrid planetGrid, Vector3 begin, Vector3 end, Vector2 interval, float yaw, float planetRadius, float gap, Vector3[] snaps)
        {
            begin = begin.normalized;
            end = end.normalized;
            var intervalAll = interval.x;
            var radTotal = Mathf.Acos(Vector3.Dot(begin, end));
            var distTotal = radTotal * planetRadius;
            var ignoreGrid = VFInput._ignoreGrid;

            var maxCount = snaps.Length;
            var finalCount = 0;
            var maxT = 1f - intervalAll * 0.5f / distTotal;
            while (finalCount < maxCount)
            {
                var t = finalCount * intervalAll / distTotal;
                if (ignoreGrid)
                    snaps[finalCount] = Vector3.Slerp(begin, end, t);
                else
                    snaps[finalCount] = planetGrid.SnapTo(Vector3.Slerp(begin, end, t));
                finalCount++;
                if (t > maxT) break;
            }

            return finalCount;
        }

        private static int PlanetAuxDataSnapDotsNonAllocNotAligned(PlanetAuxData aux, Vector3 begin, Vector3 end, Vector2 interval, float yaw, float gap, Vector3[] snaps)
        {
            var num = 0;
            var magnitude = begin.magnitude;
            if (aux.activeGrid != null)
            {
                num = PlanetGridSnapDotsNonAllocNotAligned(aux.activeGrid, begin, end, interval, yaw, aux.planet.realRadius, gap, snaps);
                for (var i = 0; i < num; i++)
                {
                    snaps[i] *= magnitude;
                }
            }
            else
            {
                snaps[num++] = aux.Snap(begin, false);
            }

            return num;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.SnapDotsNonAlloc)))
            );
            matcher.Labels.Add(label1);
            matcher.InstructionAt(1).labels.Add(label2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.control))),
                new CodeInstruction(OpCodes.Brtrue, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handItem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DragBuildPowerPoles), nameof(IsPowerPole))),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DragBuildPowerPoles), nameof(PlanetAuxDataSnapDotsNonAllocNotAligned))),
                new CodeInstruction(OpCodes.Br, label2)
            ).Advance(1).MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handItem))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.item))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handPrefabDesc))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc)))
            ).Advance(2).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 6)).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildTool_Click click, int i) =>
            {
                if ((i & 1) == 0) return click.handItem;
                var id = click.handItem.ID;
                if (id != 2201 && id != 2202) return click.handItem;
                return LDB.items.Select(id ^ 3);
            })).Advance(3).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 6)).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildTool_Click click, int i) =>
            {
                if ((i & 1) == 0) return click.handPrefabDesc;
                var id = click.handItem.ID;
                if (id != 2201 && id != 2202) return click.handPrefabDesc;
                return LDB.items.Select(id ^ 3).prefabDesc;
            }));
            return matcher.InstructionEnumeration();
        }
    }

    private static class BeltSignalsForBuyOut
    {
        private static Harmony _patch;
        private static Harmony _persistPatch;
        private static bool _initialized;
        private static bool _loaded;
        private static long _clusterSeedKey;
        private static readonly int[] DarkFogItemIds = [5201, 5206, 5202, 5204, 5203, 5205];
        private static readonly int[] DarkFogItemExchangeRate = [20, 60, 30, 30, 30, 10];
        public static readonly int[] DarkFogItemsInVoid = [0, 0, 0, 0, 0, 0];
        private static Dictionary<int, uint>[] _signalBelts = new Dictionary<int, uint>[64];
        private static readonly HashSet<int> SignalBeltFactoryIndices = [];

        public static void InitPersist()
        {
            AddBeltSignalProtos();
            _persistPatch = Harmony.CreateAndPatchAll(typeof(Persist));
            GameLogic.OnDataLoaded += Persist.VFPreload_InvokeOnLoadWorkEnded_Postfix;
            GameLogic.OnGameBegin += Persist.GameMain_Begin_Postfix;
        }

        public static void UninitPersist()
        {
            GameLogic.OnGameBegin -= Persist.GameMain_Begin_Postfix;
            GameLogic.OnDataLoaded -= Persist.VFPreload_InvokeOnLoadWorkEnded_Postfix;
            _persistPatch?.UnpatchSelf();
            _persistPatch = null;
        }

        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BeltSignalsForBuyOut));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static void AddBeltSignalProtos()
        {
            if (!_initialized || _loaded) return;
            var pluginfolder = Util.PluginFolder;
            var signals = LDB._signals;
            SignalProto[] protos =
            [
                new SignalProto
                {
                    ID = 301,
                    Name = "存储单元",
                    GridIndex = 3601,
                    IconPath = "assets/signal/memory.png",
                    _iconSprite = Util.LoadSprite($"{pluginfolder}/assets/signal/memory.png"),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 302,
                    Name = "能量碎片",
                    GridIndex = 3602,
                    IconPath = "assets/signal/energy-fragment.png",
                    _iconSprite = Util.LoadSprite($"{pluginfolder}/assets/signal/energy-fragment.png"),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 303,
                    Name = "硅基神经元",
                    GridIndex = 3603,
                    IconPath = "assets/signal/silicon-neuron.png",
                    _iconSprite = Util.LoadSprite($"{pluginfolder}/assets/signal/silicon-neuron.png"),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 304,
                    Name = "负熵奇点",
                    GridIndex = 3604,
                    IconPath = "assets/signal/negentropy.png",
                    _iconSprite = Util.LoadSprite($"{pluginfolder}/assets/signal/negentropy.png"),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 305,
                    Name = "物质重组器",
                    GridIndex = 3605,
                    IconPath = "assets/signal/reassembler.png",
                    _iconSprite = Util.LoadSprite($"{pluginfolder}/assets/signal/reassembler.png"),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 306,
                    Name = "虚粒子",
                    GridIndex = 3606,
                    IconPath = "assets/signal/virtual-particle.png",
                    _iconSprite = Util.LoadSprite($"{pluginfolder}/assets/signal/virtual-particle.png"),
                    SID = ""
                },
            ];
            foreach (var proto in protos)
            {
                proto.name = proto.Name.Translate();
            }

            var index = signals.dataArray.Length;
            signals.dataArray = signals.dataArray.Concat(protos).ToArray();
            foreach (var proto in protos)
            {
                signals.dataIndices[proto.ID] = index;
                index++;
            }

            _loaded = true;
        }

        private static void RemoveBeltSignalProtos()
        {
            if (!_initialized || !_loaded) return;
            var signals = LDB._signals;
            if (signals.dataIndices.TryGetValue(301, out var index))
            {
                signals.dataArray = signals.dataArray.Take(index).Concat(signals.dataArray.Skip(index + 6)).ToArray();
                for (var id = 301; id <= 306; id++)
                    signals.dataIndices.Remove(id);
                var len = signals.dataArray.Length;
                for (; index < len; index++)
                    signals.dataIndices[signals.dataArray[index].ID] = index;
            }

            _loaded = false;
        }

        private static void InitSignalBelts()
        {
            if (!GameMain.isRunning) return;

            var factories = GameMain.data?.factories;
            if (factories == null) return;
            foreach (var factory in factories)
            {
                var entitySignPool = factory?.entitySignPool;
                if (entitySignPool == null) continue;
                var cargoTraffic = factory.cargoTraffic;
                var beltPool = cargoTraffic.beltPool;
                for (var i = cargoTraffic.beltCursor - 1; i > 0; i--)
                {
                    if (beltPool[i].id != i) continue;
                    ref var signal = ref entitySignPool[beltPool[i].entityId];
                    var signalId = signal.iconId0;
                    if (signalId is < 301U or > 306U) continue;
                    SetSignalBelt(factory.index, i, signalId - 301U);
                }
            }
        }

        private static void SetSignalBelt(int factory, int beltId, uint signal)
        {
            var signalBelts = GetOrCreateSignalBelts(factory);
            if (signalBelts.Count == 0)
                SignalBeltFactoryIndices.Add(factory);
            signalBelts[beltId] = signal;
        }

        private static Dictionary<int, uint> GetOrCreateSignalBelts(int index)
        {
            Dictionary<int, uint> obj;
            if (index < 0) return null;
            if (index >= _signalBelts.Length)
            {
                Array.Resize(ref _signalBelts, index * 2);
            }
            else
            {
                obj = _signalBelts[index];
                if (obj != null) return obj;
            }

            obj = new Dictionary<int, uint>();
            _signalBelts[index] = obj;
            return obj;
        }
        
        private static Dictionary<int, uint> GetSignalBelts(int index)
        {
            return index >= 0 && index < _signalBelts.Length ? _signalBelts[index] : null;
        }

        private static void RemoveSignalBelt(int factory, int beltId)
        {
            var signalBelts = GetSignalBelts(factory);
            if (signalBelts == null) return;
            signalBelts.Remove(beltId);
            if (signalBelts.Count == 0)
                SignalBeltFactoryIndices.Remove(factory);
        }

        private static void RemovePlanetSignalBelts(int factory)
        {
            var signalBelts = GetSignalBelts(factory);
            if (signalBelts == null) return;
            signalBelts.Clear();
            SignalBeltFactoryIndices.Remove(factory);
        }

        private static class Persist
        {
            public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
            {
                if (BeltSignalsForBuyOut._initialized) return;
                BeltSignalsForBuyOut._initialized = true;
                BeltSignalsForBuyOut.AddBeltSignalProtos();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DigitalSystem), MethodType.Constructor, typeof(PlanetData))]
            private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
            {
                var player = GameMain.mainPlayer;
                if (player == null) return;
                var factory = _planet?.factory;
                if (factory == null) return;
                RemovePlanetSignalBelts(factory.index);
            }

            public static void GameMain_Begin_Postfix()
            {
                _clusterSeedKey = GameMain.data.GetClusterSeedKey();
                InitSignalBelts();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
            public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
            {
                var planet = GameMain.localPlanet;
                if (planet == null) return;
                RemoveSignalBelt(planet.factoryIndex, id);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalIcon))]
            public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int entityId, int signalId)
            {
                var planet = GameMain.localPlanet;
                if (planet == null) return;
                var factory = __instance.factory;
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                if (signalId is < 301 or > 306)
                {
                    RemoveSignalBelt(factoryIndex, beltId);
                }
                else
                {
                    SetSignalBelt(factoryIndex, beltId, (uint)signalId - 301U);
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static IEnumerable<CodeInstruction> GameData_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), nameof(PerformanceMonitor.EndSample)))
            ).Advance(1).Insert(
                Transpilers.EmitDelegate(() =>
                {
                    var factories = GameMain.data?.factories;
                    if (factories == null) return;
                    var propertySystem = DSPGame.propertySystem;
                    foreach (var factoryIndex in SignalBeltFactoryIndices)
                    {
                        if (factoryIndex >= factories.Length) continue;
                        var signalBelts = GetSignalBelts(factoryIndex);
                        if (signalBelts == null) continue;
                        var factory = factories[factoryIndex];
                        if (factory == null) continue;
                        var cargoTraffic = factory.cargoTraffic;
                        foreach (var kvp in signalBelts)
                        {
                            ref var belt = ref cargoTraffic.beltPool[kvp.Key];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            var itemIdx = kvp.Value;
                            if (cargoPath == null) continue;
                            var itemId = DarkFogItemIds[itemIdx];
                            var consume = (byte)Math.Min(DarkFogItemsInVoid[itemIdx], 4);
                            if (consume < 4)
                            {
                                var metaverse = propertySystem.GetItemAvaliableProperty(_clusterSeedKey, 6006);
                                if (metaverse > 0)
                                {
                                    if (metaverse > 10)
                                        metaverse = 10;
                                    propertySystem.AddItemConsumption(_clusterSeedKey, 6006, metaverse);
                                    var mainPlayer = GameMain.mainPlayer;
                                    GameMain.history.AddPropertyItemConsumption(6006, metaverse, true);
                                    var count = DarkFogItemExchangeRate[itemIdx] * metaverse;
                                    DarkFogItemsInVoid[itemIdx] += count;
                                    consume = (byte)Math.Min(DarkFogItemsInVoid[itemIdx], 4);
                                    mainPlayer.mecha.AddProductionStat(itemId, count, mainPlayer.nearestFactory);
                                }
                            }
                            if (consume > 0 && cargoPath.TryInsertItem(belt.segIndex + belt.segPivotOffset, itemId, consume, 0))
                                DarkFogItemsInVoid[itemIdx] -= consume;
                        }
                    }
                })
            );
            return matcher.InstructionEnumeration();
        }
    }
}