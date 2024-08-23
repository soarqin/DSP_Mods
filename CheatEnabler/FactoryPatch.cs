using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;

namespace CheatEnabler;

public static class FactoryPatch
{
    public static ConfigEntry<bool> ImmediateEnabled;
    public static ConfigEntry<bool> ArchitectModeEnabled;
    public static ConfigEntry<bool> NoConditionEnabled;
    public static ConfigEntry<bool> NoCollisionEnabled;
    public static ConfigEntry<bool> BeltSignalGeneratorEnabled;
    public static ConfigEntry<bool> BeltSignalNumberAltFormat;
    public static ConfigEntry<bool> BeltSignalCountGenEnabled;
    public static ConfigEntry<bool> BeltSignalCountRemEnabled;
    public static ConfigEntry<bool> BeltSignalCountRecipeEnabled;
    public static ConfigEntry<bool> RemovePowerSpaceLimitEnabled;
    public static ConfigEntry<bool> BoostWindPowerEnabled;
    public static ConfigEntry<bool> BoostSolarPowerEnabled;
    public static ConfigEntry<bool> BoostFuelPowerEnabled;
    public static ConfigEntry<bool> BoostGeothermalPowerEnabled;
    public static ConfigEntry<bool> GreaterPowerUsageInLogisticsEnabled;
    public static ConfigEntry<bool> ControlPanelRemoteLogisticsEnabled;

    private static Harmony _factoryPatch;
    private static PressKeyBind _noConditionKey;
    private static PressKeyBind _noCollisionKey;

    public static void Init()
    {
        if (_factoryPatch != null) return;
        _noConditionKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
            {
                key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
                conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
                name = "ToggleNoCondition",
                canOverride = true
            }
        );
        _noCollisionKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
            {
                key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
                conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
                name = "ToggleNoCollision",
                canOverride = true
            }
        );
        I18N.Add("KEYToggleNoCondition", "Toggle No Condition Build", "切换无条件建造");
        I18N.Add("KEYToggleNoCollision", "Toggle No Collision", "切换无碰撞");
        I18N.Add("NoConditionOn", "No condition build is enabled!", "无条件建造已启用");
        I18N.Add("NoConditionOff", "No condition build is disabled!", "无条件建造已禁用");
        I18N.Add("NoCollisionOn", "No collision is enabled!", "无碰撞已启用");
        I18N.Add("NoCollisionOff", "No collision is disabled!", "无碰撞已禁用");
        ImmediateEnabled.SettingChanged += (_, _) => ImmediateBuild.Enable(ImmediateEnabled.Value);
        ArchitectModeEnabled.SettingChanged += (_, _) => ArchitectMode.Enable(ArchitectModeEnabled.Value);
        NoConditionEnabled.SettingChanged += (_, _) => NoConditionBuild.Enable(NoConditionEnabled.Value);
        NoCollisionEnabled.SettingChanged += (_, _) => NoCollisionValueChanged();
        BeltSignalGeneratorEnabled.SettingChanged += (_, _) => BeltSignalGenerator.Enable(BeltSignalGeneratorEnabled.Value);
        BeltSignalNumberAltFormat.SettingChanged += (_, _) => BeltSignalGenerator.OnAltFormatChanged();
        RemovePowerSpaceLimitEnabled.SettingChanged += (_, _) => RemovePowerSpaceLimit.Enable(RemovePowerSpaceLimitEnabled.Value);
        BoostWindPowerEnabled.SettingChanged += (_, _) => BoostWindPower.Enable(BoostWindPowerEnabled.Value);
        BoostSolarPowerEnabled.SettingChanged += (_, _) => BoostSolarPower.Enable(BoostSolarPowerEnabled.Value);
        BoostFuelPowerEnabled.SettingChanged += (_, _) => BoostFuelPower.Enable(BoostFuelPowerEnabled.Value);
        BoostGeothermalPowerEnabled.SettingChanged += (_, _) => BoostGeothermalPower.Enable(BoostGeothermalPowerEnabled.Value);
        GreaterPowerUsageInLogisticsEnabled.SettingChanged += (_, _) => GreaterPowerUsageInLogistics.Enable(GreaterPowerUsageInLogisticsEnabled.Value);
        ControlPanelRemoteLogisticsEnabled.SettingChanged += (_, _) => ControlPanelRemoteLogistics.Enable(ControlPanelRemoteLogisticsEnabled.Value);
        ImmediateBuild.Enable(ImmediateEnabled.Value);
        ArchitectMode.Enable(ArchitectModeEnabled.Value);
        NoConditionBuild.Enable(NoConditionEnabled.Value);
        NoCollisionValueChanged();
        BeltSignalGenerator.Enable(BeltSignalGeneratorEnabled.Value);
        RemovePowerSpaceLimit.Enable(RemovePowerSpaceLimitEnabled.Value);
        BoostWindPower.Enable(BoostWindPowerEnabled.Value);
        BoostSolarPower.Enable(BoostSolarPowerEnabled.Value);
        BoostFuelPower.Enable(BoostFuelPowerEnabled.Value);
        BoostGeothermalPower.Enable(BoostGeothermalPowerEnabled.Value);
        GreaterPowerUsageInLogistics.Enable(GreaterPowerUsageInLogisticsEnabled.Value);
        ControlPanelRemoteLogistics.Enable(ControlPanelRemoteLogisticsEnabled.Value);
        _factoryPatch = Harmony.CreateAndPatchAll(typeof(FactoryPatch));
    }

    public static void Uninit()
    {
        _factoryPatch?.UnpatchSelf();
        _factoryPatch = null;
        ImmediateBuild.Enable(false);
        ArchitectMode.Enable(false);
        NoConditionBuild.Enable(false);
        BeltSignalGenerator.Enable(false);
        RemovePowerSpaceLimit.Enable(false);
        BoostWindPower.Enable(false);
        BoostSolarPower.Enable(false);
        BoostFuelPower.Enable(false);
        BoostGeothermalPower.Enable(false);
        GreaterPowerUsageInLogistics.Enable(false);
        ControlPanelRemoteLogistics.Enable(false);
    }

    public static void OnUpdate()
    {
        if (_noConditionKey.keyValue)
        {
            NoConditionEnabled.Value = !NoConditionEnabled.Value;
            if (!DSPGame.IsMenuDemo && GameMain.isRunning)
            {
                UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead((NoConditionEnabled.Value ? "NoConditionOn" : "NoConditionOff").Translate());
            }
        }
        if (_noCollisionKey.keyValue)
        {
            NoCollisionEnabled.Value = !NoCollisionEnabled.Value;
            if (!DSPGame.IsMenuDemo && GameMain.isRunning)
            {
                UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead((NoCollisionEnabled.Value ? "NoCollisionOn" : "NoCollisionOff").Translate());
            }
        }
    }

    private static void NoCollisionValueChanged()
    {
        var coll = ColliderPool.instance;
        if (coll == null) return;
        var obj = coll.gameObject;
        if (obj == null) return;
        obj.gameObject.SetActive(!NoCollisionEnabled.Value);
    }

    public static void ArrivePlanet(PlanetFactory factory)
    {
        var imm = ImmediateEnabled.Value;
        var architect = ArchitectModeEnabled.Value;
        if ((!imm && !architect) || GameMain.gameScenario == null) return;
        var prebuilds = factory.prebuildPool;
        if (imm) factory.BeginFlattenTerrain();
        for (var i = factory.prebuildCursor - 1; i > 0; i--)
        {
            if (prebuilds[i].id != i) continue;
            if (prebuilds[i].itemRequired > 0)
            {
                if (!architect) continue;
                prebuilds[i].itemRequired = 0;
                if (imm)
                    factory.BuildFinally(GameMain.mainPlayer, i, false);
                else
                    factory.AlterPrebuildModelState(i);
            }
            else
            {
                if (imm)
                    factory.BuildFinally(GameMain.mainPlayer, i, false);
            }
        }

        if (imm) factory.EndFlattenTerrain();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.NotifyFactoryLoaded))]
    private static void PlanetData_NotifyFactoryLoaded_Postfix(PlanetData __instance)
    {
        var main = GameMain.instance;
        if (main != null && main._running)
        {
            ArrivePlanet(__instance.factory);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
    private static void GameMain_Begin_Postfix_For_ImmBuild()
    {
        var factory = GameMain.mainPlayer?.factory;
        if (factory != null)
        {
            ArrivePlanet(factory);
        }
        GameMain.data?.warningSystem?.UpdateCriticalWarningText();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.hasCriticalWarning), MethodType.Getter)]
    private static IEnumerable<CodeInstruction> WarningSystem_hasCriticalWarning_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.End().MatchBack(false,
            new CodeMatch(OpCodes.Ret)
        );
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(NoConditionEnabled))),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
            new CodeInstruction(OpCodes.Or)
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.UpdateCriticalWarningText))]
    private static IEnumerable<CodeInstruction> WarningSystem_UpdateCriticalWarningText_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldstr),
            new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(WarningSystem), nameof(WarningSystem.criticalWarningTexts)))
        );
        matcher.Repeat(m =>
        {
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            m.Advance(1).Labels.Add(label1);
            m.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(NoConditionEnabled))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Ldstr, "Build without condition is enabled!"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Localization), nameof(Localization.Translate), new[] { typeof(string) })),
                new CodeInstruction(OpCodes.Ldstr, "\r\n"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new[] { typeof(string), typeof(string) })),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(WarningSystem), nameof(WarningSystem.criticalWarningTexts)))
            );
            if (m.InstructionAt(2).opcode == OpCodes.Ret)
            {
                m.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WarningSystem), nameof(WarningSystem.onCriticalWarningTextChanged))),
                    new CodeInstruction(OpCodes.Brfalse_S, label2),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WarningSystem), nameof(WarningSystem.onCriticalWarningTextChanged))),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Action), nameof(Action.Invoke)))
                );
            }
            m.InsertAndAdvance(
                new CodeInstruction(OpCodes.Br, label2)
            ).Advance(2).Labels.Add(label2);
        });
        return matcher.InstructionEnumeration();
    }

    private static class ImmediateBuild
    {
        private static Harmony _immediatePatch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                if (_immediatePatch != null) return;
                var factory = GameMain.mainPlayer?.factory;
                if (factory != null)
                {
                    ArrivePlanet(factory);
                }
                _immediatePatch = Harmony.CreateAndPatchAll(typeof(ImmediateBuild));
                return;
            }
            _immediatePatch?.UnpatchSelf();
            _immediatePatch = null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.End().MatchBack(false,
                new CodeMatch(OpCodes.Ret)
            );
            if (matcher.IsInvalid)
            {
                CheatEnabler.Logger.LogWarning($"Failed to patch CreatePrebuilds");
                return matcher.InstructionEnumeration();
            }

            matcher.Advance(-1);
            if (matcher.Opcode != OpCodes.Nop && (matcher.Opcode != OpCodes.Call || !matcher.Instruction.OperandIs(AccessTools.Method(typeof(GC), nameof(GC.Collect)))))
            {
                CheatEnabler.Logger.LogWarning($"Failed to patch CreatePrebuilds: last instruction is not `Nop` or `Call GC.Collect()`: {matcher.Instruction}");
                return matcher.InstructionEnumeration();
            }

            var labels = matcher.Labels;
            matcher.Labels = [];
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool.factory))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FactoryPatch), nameof(ArrivePlanet)))
            );
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UXAssist.PlanetFunctions), nameof(UXAssist.PlanetFunctions.BuildOrbitalCollectors))]
        private static void UXAssist_PlanetFunctions_BuildOrbitalCollectors_Postfix()
        {
            var factory = GameMain.mainPlayer?.factory;
            if (factory != null)
            {
                ArrivePlanet(factory);
            }
        }
    }

    private static class ArchitectMode
    {
        private static Harmony _patch;
        private static bool[] _canBuildItems;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                if (_patch != null) return;
                var factory = GameMain.mainPlayer?.factory;
                if (factory != null)
                {
                    ArrivePlanet(factory);
                }
                _patch = Harmony.CreateAndPatchAll(typeof(ArchitectMode));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), new[] { typeof(int), typeof(int), typeof(int), typeof(bool) },
            new[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal })]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), new[] { typeof(int), typeof(int), typeof(int[]), typeof(int), typeof(bool) },
            new[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
        public static bool TakeTailItemsPatch(StorageComponent __instance, int itemId)
        {
            if (__instance == null || __instance.id != GameMain.mainPlayer.package.id) return true;
            if (itemId <= 0) return true;
            if (_canBuildItems == null)
            {
                DoInit();
            }

            return itemId >= 12000 || !_canBuildItems[itemId];
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StorageComponent), "GetItemCount", typeof(int))]
        public static void GetItemCountPatch(StorageComponent __instance, int itemId, ref int __result)
        {
            if (__result > 99) return;
            if (__instance == null || __instance.id != GameMain.mainPlayer.package.id) return;
            if (itemId <= 0) return;
            if (_canBuildItems == null)
            {
                DoInit();
            }
            if (itemId < 12000 && _canBuildItems[itemId]) __result = 100;
        }

        private static void DoInit()
        {
            _canBuildItems = new bool[12000];
            foreach (var ip in LDB.items.dataArray)
            {
                if ((ip.Type == EItemType.Logistics || ip.CanBuild) && ip.ID < 12000) _canBuildItems[ip.ID] = true;
            }
        }
    }

    private static class NoConditionBuild
    {
        private static Harmony _noConditionPatch;
        public static void Enable(bool on)
        {
            GameMain.data?.warningSystem?.UpdateCriticalWarningText();
            if (on)
            {
                _noConditionPatch ??= Harmony.CreateAndPatchAll(typeof(NoConditionBuild));
                return;
            }
            _noConditionPatch?.UnpatchSelf();
            _noConditionPatch = null;
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.Start().InsertAndAdvance(
                new CodeInstruction(OpCodes.Br, label1)
            );
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.buildPreviews))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<BuildPreview>), nameof(List<BuildPreview>.Count))),
                new CodeMatch(ci => ci.IsStloc())
            );
            matcher.Labels.Add(label1);
            matcher.Advance(4).InsertAndAdvance(
                new CodeInstruction(OpCodes.Br, label2)
            );
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.waitForConfirm))),
                new CodeMatch(ci => ci.Branches(out _))
            );
            var operand = matcher.Operand;
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(label2),
                new CodeInstruction(OpCodes.Stloc_S, operand)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            matcher.Start().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NoConditionBuild), nameof(CheckForMiner))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret)
            );
            matcher.Labels.Add(label1);
            return matcher.InstructionEnumeration();
        }

        public static bool CheckForMiner(BuildTool tool)
        {
            var previews = tool.buildPreviews;
            foreach (var preview in previews)
            {
                var desc = preview?.item?.prefabDesc;
                if (desc == null) continue;
                if (desc.veinMiner || desc.oilMiner) return false;
            }

            return true;
        }
    }

    public static class BeltSignalGenerator
    {
        private static Harmony _beltSignalPatch;
        private static Dictionary<int, BeltSignal>[] _signalBelts;
        private static Dictionary<long, int> _portalFrom;
        private static Dictionary<int, HashSet<long>> _portalTo;
        private static int _signalBeltsCapacity;
        private static bool _initialized;

        private class BeltSignal
        {
            public int SignalId;
            public int SpeedLimit;
            public byte Stack;
            public byte Inc;
            public int Progress;
            public Tuple<int, float>[] Sources;
            public float[] SourceProgress;
        }

        public static void Enable(bool on)
        {
            if (on)
            {
                InitSignalBelts();
                _beltSignalPatch ??= Harmony.CreateAndPatchAll(typeof(BeltSignalGenerator));
                return;
            }
            _beltSignalPatch?.UnpatchSelf();
            _beltSignalPatch = null;
            _initialized = false;
            _signalBelts = null;
            _signalBeltsCapacity = 0;
        }

        public static void OnAltFormatChanged()
        {
            if (_signalBelts == null) return;
            var factories = GameMain.data?.factories;
            if (factories == null) return;
            var factoryCount = GameMain.data.factoryCount;
            var altFormat = BeltSignalNumberAltFormat.Value;
            for (var i = Math.Min(_signalBelts.Length, factoryCount) - 1; i >= 0; i--)
            {
                var factory = factories[i];
                var cargoTraffic = factory?.cargoTraffic;
                if (cargoTraffic == null) continue;
                var entitySignPool = factory.entitySignPool;
                if (entitySignPool == null) continue;
                var belts = _signalBelts[i];
                if (belts == null) continue;
                foreach (var pair in belts)
                {
                    var beltId = pair.Key;
                    ref var belt = ref cargoTraffic.beltPool[beltId];
                    if (belt.id != beltId) continue;
                    ref var signal = ref entitySignPool[belt.entityId];
                    if (signal.iconId0 < 1000) continue;
                    var signalBelt = pair.Value;
                    var inc = signalBelt.Inc / signalBelt.Stack;
                    if (altFormat)
                        signal.count0 = signalBelt.SpeedLimit + signalBelt.Stack * 10000 + inc * 100000;
                    else
                        signal.count0 = signalBelt.SpeedLimit * 100 + signalBelt.Stack + inc * 10;
                }
            }
        }

        private static void InitSignalBelts()
        {
            if (!GameMain.isRunning) return;
            _signalBelts = new Dictionary<int, BeltSignal>[64];
            _signalBeltsCapacity = 64;
            _portalFrom = new Dictionary<long, int>();
            _portalTo = new Dictionary<int, HashSet<long>>();

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
                    if (signalId == 0U) continue;
                    var number = Mathf.RoundToInt(signal.count0);
                    switch (signalId)
                    {
                        case 404:
                            SetSignalBelt(factory.index, i, (int)signalId, 0);
                            continue;
                        case 600:
                        case >= 1000 and < 20000:
                            if (number > 0)
                                SetSignalBelt(factory.index, i, (int)signalId, number);
                            continue;
                        case >= 601 and <= 609:
                            if (number > 0)
                                SetSignalBeltPortalTo(factory.index, i, number);
                            continue;
                    }
                }
            }

            _initialized = true;
        }

        private static Dictionary<int, BeltSignal> GetOrCreateSignalBelts(int index)
        {
            Dictionary<int, BeltSignal> obj;
            if (index < 0) return null;
            if (index >= _signalBeltsCapacity)
            {
                var newCapacity = _signalBeltsCapacity * 2;
                var newSignalBelts = new Dictionary<int, BeltSignal>[newCapacity];
                Array.Copy(_signalBelts, newSignalBelts, _signalBeltsCapacity);
                _signalBelts = newSignalBelts;
                _signalBeltsCapacity = newCapacity;
            }
            else
            {
                obj = _signalBelts[index];
                if (obj != null) return obj;
            }

            obj = new Dictionary<int, BeltSignal>();
            _signalBelts[index] = obj;
            return obj;
        }

        private static Dictionary<int, BeltSignal> GetSignalBelts(int index)
        {
            return index >= 0 && index < _signalBeltsCapacity ? _signalBelts[index] : null;
        }

        private static void SetSignalBelt(int factory, int beltId, int signalId, int number)
        {
            int stack;
            int inc;
            int speedLimit;
            if (signalId >= 1000)
            {
                if (!BeltSignalNumberAltFormat.Value)
                {
                    stack = Mathf.Clamp(number % 10, 1, 4);
                    inc = number / 10 % 10 * stack;
                    speedLimit = number / 100;
                }
                else
                {
                    stack = Mathf.Clamp(number / 10000 % 10, 1, 4);
                    inc = number / 100000 % 10 * stack;
                    speedLimit = number % 10000;
                }
            }
            else
            {
                stack = 0;
                inc = 0;
                speedLimit = number;
            }
            
            if (speedLimit > 3600) speedLimit = 3600;

            var signalBelts = GetOrCreateSignalBelts(factory);
            if (signalBelts.TryGetValue(beltId, out var oldBeltSignal))
            {
                oldBeltSignal.SpeedLimit = speedLimit;
                oldBeltSignal.Stack = (byte)stack;
                oldBeltSignal.Inc = (byte)inc;
                oldBeltSignal.Progress = 0;
                if (oldBeltSignal.SignalId == signalId) return;
                oldBeltSignal.SignalId = signalId;
                AddSourcesToBeltSignal(oldBeltSignal, signalId);
                return;
            }

            var beltSignal = new BeltSignal
            {
                SignalId = signalId,
                SpeedLimit = speedLimit,
                Stack = (byte)stack,
                Inc = (byte)inc
            };
            if (signalId >= 1000)
            {
                AddSourcesToBeltSignal(beltSignal, signalId);
            }

            signalBelts[beltId] = beltSignal;
        }

        private static void AddSourcesToBeltSignal(BeltSignal beltSignal, int itemId)
        {
            var result = new Dictionary<int, float>();
            var extra = new Dictionary<int, float>();
            CalculateAllProductions(result, extra, itemId);
            foreach (var p in extra)
            {
                if (result.TryGetValue(itemId, out var v) && v >= p.Value) continue;
                result[itemId] = p.Value;
            }

            result.Remove(itemId);
            var cnt = result.Count;
            if (cnt == 0)
            {
                beltSignal.Sources = null;
                beltSignal.SourceProgress = null;
                return;
            }

            var items = new Tuple<int, float>[cnt];
            var progress = new float[cnt];
            foreach (var p in result)
            {
                items[--cnt] = Tuple.Create(p.Key, p.Value);
            }

            beltSignal.Sources = items;
            beltSignal.SourceProgress = progress;
        }

        private static void SetSignalBeltPortalTo(int factory, int beltId, int number)
        {
            var v = ((long)factory << 32) | (uint)beltId;
            _portalFrom[v] = number;
            if (!_portalTo.TryGetValue(number, out var set))
            {
                set = [];
                _portalTo[number] = set;
            }

            set.Add(v);
        }

        private static void RemoveSignalBelt(int factory, int beltId)
        {
            GetSignalBelts(factory)?.Remove(beltId);
        }

        private static void RemovePlanetSignalBelts(int factory)
        {
            GetSignalBelts(factory)?.Clear();
        }

        private static void RemoveSignalBeltPortalEnd(int factory, int beltId)
        {
            var v = ((long)factory << 32) | (uint)beltId;
            if (!_portalFrom.TryGetValue(v, out var number)) return;
            _portalFrom.Remove(beltId);
            if (!_portalTo.TryGetValue(number, out var set)) return;
            set.Remove(v);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Postfix()
        {
            if (BeltSignalGeneratorEnabled.Value) InitSignalBelts();
            InitItemSources();
        }
    
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DigitalSystem), MethodType.Constructor, typeof(PlanetData))]
        private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
        {
            if (!BeltSignalGeneratorEnabled.Value) return; 
            var player = GameMain.mainPlayer;
            if (player == null) return;
            var factory = _planet?.factory;
            if (factory == null) return;
            RemovePlanetSignalBelts(factory.index);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
        public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
        {
            if (!_initialized) return;
            var planet = GameMain.localPlanet;
            if (planet == null) return;
            RemoveSignalBeltPortalEnd(planet.factoryIndex, id);
            RemoveSignalBelt(planet.factoryIndex, id);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalIcon))]
        public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int signalId, int entityId)
        {
            if (!_initialized) return;
            var planet = GameMain.localPlanet;
            if (planet == null) return;
            var factory = __instance.factory;
            int number;
            var needAdd = false;
            switch (signalId)
            {
                case 404:
                    number = 0;
                    needAdd = true;
                    break;
                case 600:
                case >= 1000 and < 20000:
                    number = Mathf.RoundToInt(factory.entitySignPool[entityId].count0);
                    if (number > 0)
                        needAdd = true;
                    break;
                case >= 601 and <= 609:
                    number = Mathf.RoundToInt(factory.entitySignPool[entityId].count0);
                    var factoryIndex = planet.factoryIndex;
                    var beltId = factory.entityPool[entityId].beltId;
                    if (number > 0)
                        SetSignalBeltPortalTo(factoryIndex, beltId, number);
                    RemoveSignalBelt(factoryIndex, beltId);
                    return;
                default:
                    number = 0;
                    break;
            }

            {
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                if (needAdd)
                {
                    SetSignalBelt(factoryIndex, beltId, signalId, number);
                }
                else
                {
                    RemoveSignalBelt(factoryIndex, beltId);
                }

                RemoveSignalBeltPortalEnd(factoryIndex, beltId);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalNumber))]
        public static void CargoTraffic_SetBeltSignalNumber_Postfix(CargoTraffic __instance, float number, int entityId)
        {
            if (!_initialized) return;
            var planet = GameMain.localPlanet;
            if (planet == null) return;
            var factory = __instance.factory;
            var entitySignPool = factory.entitySignPool;
            uint signalId;
            if (entitySignPool[entityId].iconType == 0U || (signalId = entitySignPool[entityId].iconId0) == 0U) return;
            switch (signalId)
            {
                case 404:
                    return;
                case 600:
                case >= 1000 and < 20000:
                    break;
                case >= 601 and <= 609:
                    var factoryIndex = planet.factoryIndex;
                    var beltId = factory.entityPool[entityId].beltId;
                    RemoveSignalBeltPortalEnd(factoryIndex, beltId);
                    SetSignalBeltPortalTo(factoryIndex, beltId, Mathf.RoundToInt(number));
                    return;
                default:
                    return;
            }

            {
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                var n = Mathf.RoundToInt(number);
                if (n == 0)
                {
                    RemoveSignalBelt(factoryIndex, beltId);
                }
                else
                {
                    SetSignalBelt(factoryIndex, beltId, (int)signalId, n);
                }
            }
        }

        public static void ProcessBeltSignals()
        {
            if (!_initialized) return;
            var factories = GameMain.data?.factories;
            if (factories == null) return;
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
            foreach (var factory in factories)
            {
                if (factory == null) continue;
                var belts = GetSignalBelts(factory.index);
                if (belts == null || belts.Count == 0) continue;
                var factoryProductionStat = GameMain.statistics.production.factoryStatPool[factory.index];
                var productRegister = factoryProductionStat.productRegister;
                var consumeRegister = factoryProductionStat.consumeRegister;
                var countRecipe = BeltSignalCountRecipeEnabled.Value;
                var cargoTraffic = factory.cargoTraffic;
                foreach (var pair in belts)
                {
                    var beltSignal = pair.Value;
                    var signalId = beltSignal.SignalId;
                    switch (signalId)
                    {
                        case 404:
                        {
                            var beltId = pair.Key;
                            ref var belt = ref cargoTraffic.beltPool[beltId];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            if (cargoPath == null) continue;
                            int itemId;
                            if ((itemId = cargoPath.TryPickItem(belt.segIndex + belt.segPivotOffset - 5, 12, out var stack, out _)) > 0)
                            {
                                if (BeltSignalCountRemEnabled.Value) consumeRegister[itemId] += stack;
                            }

                            continue;
                        }
                        case 600:
                        {
                            if (!_portalTo.TryGetValue(beltSignal.SpeedLimit, out var set)) continue;
                            var beltId = pair.Key;
                            ref var belt = ref cargoTraffic.beltPool[beltId];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            if (cargoPath == null) continue;
                            var segIndex = belt.segIndex + belt.segPivotOffset;
                            if (!cargoPath.GetCargoAtIndex(segIndex, out var cargo, out var cargoId, out var _)) break;
                            var itemId = cargo.item;
                            var cargoPool = cargoPath.cargoContainer.cargoPool;
                            var inc = cargoPool[cargoId].inc;
                            var stack = cargoPool[cargoId].stack;
                            foreach (var n in set)
                            {
                                var cargoTraffic1 = factories[(int)(n >> 32)].cargoTraffic;
                                ref var belt1 = ref cargoTraffic1.beltPool[(int)(n & 0x7FFFFFFF)];
                                cargoPath = cargoTraffic1.GetCargoPath(belt1.segPathId);
                                if (cargoPath == null) continue;
                                if (!cargoPath.TryInsertItem(belt1.segIndex + belt1.segPivotOffset, itemId, stack, inc)) continue;
                                cargoPath.TryPickItem(segIndex - 5, 12, out var stack1, out var inc1);
                                if (inc1 != inc || stack1 != stack)
                                    cargoPath.TryPickItem(segIndex - 5, 12, out _, out _);
                                break;
                            }

                            continue;
                        }
                        case >= 1000 and < 20000:
                        {
                            var hasSpeedLimit = beltSignal.SpeedLimit > 0;
                            if (hasSpeedLimit)
                            {
                                beltSignal.Progress += beltSignal.SpeedLimit;
                                switch (beltSignal.Progress)
                                {
                                    case < 3600:
                                        continue;
                                    case > 18000:
                                        beltSignal.Progress = 14400;
                                        break;
                                }
                            }

                            var beltId = pair.Key;
                            ref var belt = ref cargoTraffic.beltPool[beltId];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            if (cargoPath == null) continue;
                            var stack = beltSignal.Stack;
                            var inc = beltSignal.Inc;
                            if (!cargoPath.TryInsertItem(belt.segIndex + belt.segPivotOffset, signalId, stack, inc)) continue;
                            if (hasSpeedLimit) beltSignal.Progress -= 3600;
                            if (BeltSignalCountGenEnabled.Value) productRegister[signalId] += stack;
                            if (!countRecipe) continue;
                            var sources = beltSignal.Sources;
                            if (sources == null) continue;
                            var progress = beltSignal.SourceProgress;
                            var stackf = (float)stack;
                            for (var i = sources.Length - 1; i >= 0; i--)
                            {
                                var newCnt = progress[i] + sources[i].Item2 * stackf;
                                if (newCnt > 0)
                                {
                                    var itemId = sources[i].Item1;
                                    var cnt = Mathf.CeilToInt(newCnt);
                                    productRegister[itemId] += cnt;
                                    consumeRegister[itemId] += cnt;
                                    progress[i] = newCnt - cnt;
                                }
                                else
                                {
                                    progress[i] = newCnt;
                                }
                            }

                            continue;
                        }
                    }
                }
            }

            PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static IEnumerable<CodeInstruction> GameData_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), nameof(PerformanceMonitor.EndSample)))
            ).Advance(1).Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BeltSignalGenerator), nameof(ProcessBeltSignals)))
            );
            return matcher.InstructionEnumeration();
        }


        /* BEGIN: Item sources calculation */
        private static readonly Dictionary<int, ItemSource> ItemSources = new();
        private static bool _itemSourcesInitialized;

        private class ItemSource
        {
            public float Count;
            public Dictionary<int, float> From;
            public Dictionary<int, float> Extra;
        }

        private static void InitItemSources()
        {
            if (_itemSourcesInitialized) return;
            foreach (var vein in LDB.veins.dataArray)
            {
                ItemSources[vein.MiningItem] = new ItemSource { Count = 1 };
            }

            foreach (var ip in LDB.items.dataArray)
            {
                if (!string.IsNullOrEmpty(ip.MiningFrom))
                {
                    ItemSources[ip.ID] = new ItemSource { Count = 1 };
                }
            }

            ItemSources[1208] = new ItemSource { Count = 1 };
            var recipes = LDB.recipes.dataArray;
            foreach (var recipe in recipes)
            {
                if (!recipe.Explicit || recipe.ID == 58 || recipe.ID == 121) continue;
                var res = recipe.Results;
                var rescnt = recipe.ResultCounts;
                var len = res.Length;
                for (var i = 0; i < len; i++)
                {
                    if (ItemSources.ContainsKey(res[i])) continue;
                    var rs = new ItemSource { Count = rescnt[i], From = new Dictionary<int, float>() };
                    var it = recipe.Items;
                    var itcnt = recipe.ItemCounts;
                    var len2 = it.Length;
                    for (var j = 0; j < len2; j++)
                    {
                        rs.From[it[j]] = itcnt[j];
                    }

                    if (len > 1)
                    {
                        rs.Extra = new Dictionary<int, float>();
                        for (var k = 0; k < len; k++)
                        {
                            if (i != k)
                            {
                                rs.Extra[res[k]] = rescnt[k];
                            }
                        }
                    }

                    ItemSources[res[i]] = rs;
                }
            }

            foreach (var recipe in recipes)
            {
                if (recipe.Explicit) continue;
                var res = recipe.Results;
                var rescnt = recipe.ResultCounts;
                var len = res.Length;
                for (var i = 0; i < len; i++)
                {
                    if (ItemSources.ContainsKey(res[i])) continue;
                    var rs = new ItemSource { Count = rescnt[i], From = new Dictionary<int, float>() };
                    var it = recipe.Items;
                    var itcnt = recipe.ItemCounts;
                    var len2 = it.Length;
                    for (var j = 0; j < len2; j++)
                    {
                        rs.From[it[j]] = itcnt[j];
                    }

                    if (len > 1)
                    {
                        rs.Extra = new Dictionary<int, float>();
                        for (var k = 0; k < len; k++)
                        {
                            if (i != k)
                            {
                                rs.Extra[res[k]] = rescnt[k];
                            }
                        }
                    }

                    ItemSources[res[i]] = rs;
                }
            }

            _itemSourcesInitialized = true;
        }

        private static void CalculateAllProductions(IDictionary<int, float> result, IDictionary<int, float> extra, int itemId, float count = 1f)
        {
            if (!ItemSources.TryGetValue(itemId, out var itemSource))
            {
                return;
            }

            var times = 1f;
            if (Math.Abs(count - itemSource.Count) > 0.000001f)
            {
                times = count / itemSource.Count;
            }

            {
                result.TryGetValue(itemId, out var oldCount);
                result[itemId] = oldCount + count;
            }
            if (itemSource.Extra != null)
            {
                foreach (var p in itemSource.Extra)
                {
                    extra.TryGetValue(p.Key, out var oldCount);
                    extra[p.Key] = oldCount + times * p.Value;
                }
            }

            if (itemSource.From == null) return;
            foreach (var p in itemSource.From)
            {
                CalculateAllProductions(result, extra, p.Key, times * p.Value);
            }
        }
        /* END: Item sources calculation */
    }

    private static class RemovePowerSpaceLimit
    {
        private static Harmony _patch;
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(RemovePowerSpaceLimit));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Ldc_R4, 110.25f)
            );
            if (matcher.IsValid)
            {
                matcher.Repeat(codeMatcher => codeMatcher.SetAndAdvance(
                    OpCodes.Ldc_R4, 1f
                ));
            }
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Ldc_R4, 144f)
            );
            if (matcher.IsValid)
            {
                matcher.Repeat(codeMatcher => codeMatcher.SetAndAdvance(
                    OpCodes.Ldc_R4, 1f
                ));
            }
            return matcher.InstructionEnumeration();
        }
    }

    private static class BoostWindPower
    {
        private static Harmony _patch;
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BoostWindPower));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Wind))]
        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_Wind_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().RemoveInstructions(matcher.Length);
            matcher.Insert(
                // this.currentStrength = windStrength
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.currentStrength))),
                // this.capacityCurrentTick = 500000000L
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_I8, 500000000L),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick))),
                // return 500000000L
                new CodeInstruction(OpCodes.Ldc_I8, 500000000L),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }
    }
    
    private static class BoostSolarPower
    {
        private static Harmony _patch;
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BoostSolarPower));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_PV))]
        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_PV_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().RemoveInstructions(matcher.Length).Insert(
                // this.currentStrength = lumino
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_S, 4),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.currentStrength))),
                // this.capacityCurrentTick = 600000000L
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_I8, 600000000L),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick))),
                // return 600000000L
                new CodeInstruction(OpCodes.Ldc_I8, 600000000L),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class BoostFuelPower
    {
        private static Harmony _patch;
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BoostFuelPower));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Fuel))]
        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_Fuel_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            var label3 = generator.DefineLabel();
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick)))
            );
            var labels = matcher.Labels;
            matcher.Labels = [];
            matcher.Insert(
                // if (this.fuelMask == 4)
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.fuelMask))),
                new CodeInstruction(OpCodes.Ldc_I4_4),
                new CodeInstruction(OpCodes.Bne_Un_S, label1),
                // multiplier = 10000L
                new CodeInstruction(OpCodes.Ldc_I8, 10000L),
                new CodeInstruction(OpCodes.Br_S, label3),
                // else if (this.fuelMask == 2)
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.fuelMask))),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Bne_Un_S, label2),
                // multiplier = 20000L
                new CodeInstruction(OpCodes.Ldc_I8, 20000L),
                new CodeInstruction(OpCodes.Br_S, label3),
                // else multiplier = 50000L
                new CodeInstruction(OpCodes.Ldc_I8, 50000L).WithLabels(label2),
                // do multiplier before store to this.capacityCurrentTick
                new CodeInstruction(OpCodes.Mul).WithLabels(label3)
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class BoostGeothermalPower
    {
        private static Harmony _patch;
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BoostGeothermalPower));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_GTH))]
        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_GTH_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().RemoveInstructions(matcher.Length).Insert(
                // this.currentStrength = this.gthStrength
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.gthStrength))),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.currentStrength))),
                // this.capacityCurrentTick = 2000000000L
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_I8, 2000000000L),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick))),
                // return 2000000000L
                new CodeInstruction(OpCodes.Ldc_I8, 2000000000L),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class GreaterPowerUsageInLogistics
    {
        private static Harmony _patch;
        
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(GreaterPowerUsageInLogistics));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
            var window = UIRoot.instance?.uiGame?.stationWindow;
            if (window == null) return;
            window._Close();
            window.maxMiningSpeedSlider.maxValue = enable ? 27f : 20f;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnStationIdChange))]
        private static IEnumerable<CodeInstruction> UIStationWindow_OnStationIdChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((UIStationWindow window) =>
                {
                    window.maxMiningSpeedSlider.maxValue = 27f;
                })
            ).MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.maxChargePowerSlider))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(0xC350)),
                new CodeMatch(OpCodes.Conv_I8)
            );
            var pos = matcher.Pos + 1;
            matcher.Advance(5).MatchForward(false,
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Slider), nameof(Slider.value)))
            );
            var pos2 = matcher.Pos + 2;
            matcher.Start().Advance(pos);
            var ldvar = matcher.InstructionAt(1).Clone();
            var locWorkEnergyPerTick = matcher.InstructionAt(-2).operand;
            matcher.RemoveInstructions(pos2 - pos).InsertAndAdvance(
                ldvar,
                new CodeInstruction(OpCodes.Ldloc_S, locWorkEnergyPerTick),
                Transpilers.EmitDelegate((UIStationWindow window, long maxWorkEnergy, long workEnergyPerTick) =>
                {
                    var maxSliderValue = maxWorkEnergy / 50000L;
                    window.maxChargePowerSlider.maxValue = maxSliderValue + 9;
                    window.maxChargePowerSlider.minValue = maxWorkEnergy / 500000L;
                    if (workEnergyPerTick <= maxWorkEnergy)
                        window.maxChargePowerSlider.Set(workEnergyPerTick / 50000L, false);
                    else
                        window.maxChargePowerSlider.Set(maxSliderValue + (workEnergyPerTick - 1) / maxWorkEnergy + 1, false);
                })
            );

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.maxMiningSpeedSlider))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.factorySystem))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.minerPool))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.minerId))),
                new CodeMatch(OpCodes.Ldelema, typeof(MinerComponent)),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MinerComponent), nameof(MinerComponent.speed)))
            );
            pos = matcher.Pos + 9;
            matcher.Advance(5).MatchForward(false,
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Slider), nameof(Slider.value)))
            );
            pos2 = matcher.Pos;
            matcher.Start().Advance(pos).RemoveInstructions(pos2 - pos).Insert(
                Transpilers.EmitDelegate((int speed) =>
                {
                    if (speed <= 30000)
                        return (speed - 10000) / 1000;
                    return (speed - 30000) / 10000 + 20;
                })
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnMaxMiningSpeedChange))]
        private static IEnumerable<CodeInstruction> UIStationWindow_OnMaxMiningSpeedChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(10000)),
                new CodeMatch(OpCodes.Ldarg_1)
            );
            var pos = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            );
            var pos2 = matcher.Pos;
            matcher.Start().Advance(pos);
            var labels = matcher.Labels;
            matcher.RemoveInstructions(pos2 - pos);
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
                Transpilers.EmitDelegate((float value) =>
                {
                    var intval = (int)(value + 0.5f);
                    if (intval <= 20)
                        return intval * 1000 + 10000;
                    return (intval - 20) * 10000 + 30000;
                })
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnMaxChargePowerSliderValueChange))]
        private static IEnumerable<CodeInstruction> UIStationWindow_OnMaxChargePowerSliderValueChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIStationWindow), nameof(UIStationWindow.factory))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.powerSystem))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PowerSystem), nameof(PowerSystem.consumerPool)))
            );
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate((UIStationWindow window, float value) =>
                {
                    float prevMax = window.workEnergyPrefab * 5L / 50000L;
                    if (value <= prevMax)
                    {
                        return value;
                    }

                    return prevMax * (value - prevMax + 1);
                }),
                new CodeInstruction(OpCodes.Starg_S, 1)
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class ControlPanelRemoteLogistics
    {
        private static Harmony _patch;
        
        public static void Enable(bool enable)
        {
            if (enable)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(ControlPanelRemoteLogistics));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.OnItemIconMouseDown))]
        [HarmonyPatch(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.OnHoldupItemClick))]
        [HarmonyPatch(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.OnCourierIconClick))]
        private static IEnumerable<CodeInstruction> UIControlPanelDispenserInspector_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            Label? branch = null;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.isLocal))),
                new CodeMatch(ci => ci.Branches(out branch))
            ).Repeat(
                m =>
                {
                    if (branch == null)
                    {
                        m.Advance(3);
                        return;
                    }
                    var labels = m.Labels;
                    m.RemoveInstructions(3).InsertAndAdvance(
                        new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                    );
                }
            );
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.OnShipIconClick))]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.OnWarperIconClick))]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.OnDroneIconClick))]
        private static IEnumerable<CodeInstruction> UIControlPanelStationInspector_OnShipIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            Label? branch = null;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.isLocal))),
                new CodeMatch(ci => ci.Branches(out branch))
            ).Repeat(
                m =>
                {
                    if (branch == null)
                    {
                        m.Advance(3);
                        return;
                    }
                    var labels = m.Labels;
                    m.RemoveInstructions(3).InsertAndAdvance(
                        new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                    );
                }
            );
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnItemIconMouseDown))]
        private static IEnumerable<CodeInstruction> UIControlPanelStationStorage_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            Label? branch = null;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.isLocal))),
                new CodeMatch(ci => ci.Branches(out branch))
            ).Repeat(
                m =>
                {
                    if (branch == null)
                    {
                        m.Advance(3);
                        return;
                    }
                    var labels = m.Labels;
                    m.RemoveInstructions(3).InsertAndAdvance(
                        new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                    );
                }
            );
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIControlPanelVeinCollectorPanel), nameof(UIControlPanelVeinCollectorPanel.OnProductIconClick))]
        private static IEnumerable<CodeInstruction> UIControlPanelVeinCollectorPanel_OnProductIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            Label? branch = null;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelVeinCollectorPanel), "isLocal")),
                new CodeMatch(ci => ci.Branches(out branch))
            ).Repeat(
                m =>
                {
                    if (branch == null)
                    {
                        m.Advance(3);
                        return;
                    }
                    var labels = m.Labels;
                    m.RemoveInstructions(3).InsertAndAdvance(
                        new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                    );
                }
            );
            return matcher.InstructionEnumeration();
        }
    }
}