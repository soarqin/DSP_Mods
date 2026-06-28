# UXAssist.UI / UXAssist.Common Public API Surface

This document inventories the public and protected types and members in
`UXAssist.UI` and `UXAssist.Common`. These signatures must remain stable while
refactoring UXAssist.

Only `public` and `protected` members are listed. Non-public implementation
details are omitted even when they appear on otherwise-public types.

## UXAssist.Common

### `I18N`

```csharp
public static class I18N
```

- `public static Action OnInitialized`
- `public static void Init()`
- `public static bool Initialized()`
- `public static void Add(string key, string enus, string zhcn = null)`
- `public static void Apply()`

### `GameLogic`

```csharp
public class GameLogic : PatchImpl<GameLogic>
```

- `public static Action OnDataLoaded`
- `public static Action OnGameBegin`
- `public static Action OnGameEnd`

Harmony patch methods:

- `public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()`
- `public static void GameMain_Begin_Postfix()`
- `public static void GameMain_End_Postfix()`

Inherited from `PatchImpl<GameLogic>`:

- `public static void Enable(bool enable)`
- `public static Harmony GetHarmony()`
- `protected virtual void OnEnable()`
- `protected virtual void OnDisable()`

### `GameEvent`

```csharp
public static class GameEvent
```

- `public static void InvokeSafe(this Action action, ManualLogSource logger, string name)`

### `PatchGuidAttribute`

```csharp
public class PatchGuidAttribute(string guid) : Attribute
```

- `public string Guid { get; }`

### `PatchCallbackFlag`

```csharp
public enum PatchCallbackFlag
```

- `CallOnEnableBeforePatch`
- `CallOnDisableAfterUnpatch`

### `PatchSetCallbackFlagAttribute`

```csharp
public class PatchSetCallbackFlagAttribute(PatchCallbackFlag flag) : Attribute
```

- `public PatchCallbackFlag Flag { get; }`

### `PatchImpl<T>`

```csharp
public class PatchImpl<T> where T : PatchImpl<T>, new()
```

- `public static void Enable(bool enable)`
- `public static Harmony GetHarmony()`
- `protected static T Instance { get; }`
- `protected Harmony _patch`
- `protected virtual void OnEnable()`
- `protected virtual void OnDisable()`

### `Util`

```csharp
public static class Util
```

- `public static Type[] GetTypesFiltered(Assembly assembly, Func<Type, bool> predicate)`
- `public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)`
- `public static byte[] LoadEmbeddedResource(string path, Assembly assembly = null)`
- `public static Texture2D LoadTexture(string path)`
- `public static Sprite LoadSprite(string path)`
- `public static Texture2D LoadEmbeddedTexture(string path, Assembly assembly = null)`
- `public static Sprite LoadEmbeddedSprite(string path, Assembly assembly = null)`
- `public static string PluginFolder(Assembly assembly = null)`

### `KeyBindings`

```csharp
public static class KeyBindings
```

- `public static PressKeyBind RegisterKeyBinding(BuiltinKey key)`
- `public static CombineKey FromKeyboardShortcut(KeyboardShortcut shortcut)`
- `public static bool IsKeyPressing(this PressKeyBind keyBind)`

### `KnownItemId`

```csharp
public enum KnownItemId : int
```

- `Drone = 5001`
- `Ship = 5002`
- `Bot = 5003`
- `Warper = 1210`

### `WinApi`

```csharp
public static class WinApi
```

Window style constants:

- `public const int WS_BORDER = 0x00800000`
- `public const int WS_CAPTION = 0x00C00000`
- `public const int WS_CHILD = 0x40000000`
- `public const int WS_CHILDWINDOW = 0x40000000`
- `public const int WS_CLIPCHILDREN = 0x02000000`
- `public const int WS_CLIPSIBLINGS = 0x04000000`
- `public const int WS_DISABLED = 0x08000000`
- `public const int WS_DLGFRAME = 0x00400000`
- `public const int WS_GROUP = 0x00020000`
- `public const int WS_HSCROLL = 0x00100000`
- `public const int WS_ICONIC = 0x20000000`
- `public const int WS_MAXIMIZE = 0x01000000`
- `public const int WS_MAXIMIZEBOX = 0x00010000`
- `public const int WS_MINIMIZE = 0x20000000`
- `public const int WS_MINIMIZEBOX = 0x00020000`
- `public const int WS_OVERLAPPED = 0x00000000`
- `public const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX`
- `public const int WS_POPUP = unchecked((int)0x80000000)`
- `public const int WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU`
- `public const int WS_SIZEBOX = 0x00040000`
- `public const int WS_SYSMENU = 0x00080000`
- `public const int WS_TABSTOP = 0x00010000`
- `public const int WS_THICKFRAME = 0x00040000`
- `public const int WS_TILED = 0x00000000`
- `public const int WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX`
- `public const int WS_VISIBLE = 0x10000000`
- `public const int WS_VSCROLL = 0x00200000`

`GetWindowLong` / `SetWindowLong` indices:

- `public const int GWL_EXSTYLE = -20`
- `public const int GWLP_HINSTANCE = -6`
- `public const int GWLP_HWNDPARENT = -8`
- `public const int GWLP_ID = -12`
- `public const int GWL_STYLE = -16`
- `public const int GWLP_USERDATA = -21`
- `public const int DWLP_DLGPROC = 0x4`
- `public const int DWLP_MSGRESULT = 0`
- `public const int DWLP_USER = 0x8`

Priority class constants:

- `public const int HIGH_PRIORITY_CLASS = 0x00000080`
- `public const int ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000`
- `public const int NORMAL_PRIORITY_CLASS = 0x00000020`
- `public const int BELOW_NORMAL_PRIORITY_CLASS = 0x00004000`
- `public const int IDLE_PRIORITY_CLASS = 0x00000040`

Window message constants:

- `public const int WM_CREATE = 0x0001`
- `public const int WM_DESTROY = 0x0002`
- `public const int WM_MOVE = 0x0003`
- `public const int WM_SIZE = 0x0005`
- `public const int WM_ACTIVATE = 0x0006`
- `public const int WM_SETFOCUS = 0x0007`
- `public const int WM_KILLFOCUS = 0x0008`
- `public const int WM_ENABLE = 0x000A`
- `public const int WM_CLOSE = 0x0010`
- `public const int WM_QUIT = 0x0012`
- `public const int WM_SYSCOMMAND = 0x0112`
- `public const int WM_SIZING = 0x0214`
- `public const int WM_MOVING = 0x0216`
- `public const long SC_MOVE = 0xF010L`

Nested struct:

- `public struct Rect`
  - `public int Left`
  - `public int Top`
  - `public int Right`
  - `public int Bottom`

P/Invoke functions:

- `public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName)`
- `public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId)`
- `public static extern int GetWindowLong(IntPtr hwnd, int nIndex)`
- `public static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong)`
- `public static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect)`
- `public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags)`
- `public static extern bool SetWindowText(IntPtr hwnd, string lpString)`
- `public static extern IntPtr MonitorFromRect([In] ref Rect lpRect, uint dwFlags)`

## New public types introduced in Phase 1

### `UXAssist.Common.ModFeatures`

#### `ModFeatureAttribute`

```csharp
public sealed class ModFeatureAttribute : Attribute
```

- `public string Name { get; }`
- `public int Order { get; set; }`
- `public ModFeatureAttribute(string name = null)`

#### `IModFeature`

```csharp
public interface IModFeature
```

- `void Init()`
- `void Start()`
- `void Uninit()`
- `void OnInputUpdate()`
- `void OnUpdate()`

#### `ModFeatureRegistry`

```csharp
public static class ModFeatureRegistry
```

- `public static void Discover(Assembly assembly)` — register + eagerly `Init` features; called by every mod (host + dependents) in `Awake`
- `public static void Register<T>() where T : class, IModFeature, new()` — register + eagerly `Init` an instance feature
- `internal static void StartAll()` — host-only deferred lifecycle driver (UXAssist), idempotent per feature
- `internal static void UninitAll()` — host-only lifecycle driver (UXAssist)
- `internal static void OnInputUpdateAll()` — host-only per-frame driver (UXAssist), guarded per-frame
- `internal static void OnUpdateAll()` — host-only per-frame driver (UXAssist), guarded per-frame

> `Init` runs eagerly when a feature is registered (via `Discover`/`Register`), preserving the original
> `Awake`-phase timing that keybind registration and other early setup rely on (the game's
> `UIOptionWindow._OnCreate` copies registered keybinds only after all plugins have loaded). `Start` is
> the only deferred phase: UXAssist calls `StartAll` in its own `Start` so all dependents have registered
> first (BepInEx runs every plugin's `Awake` before any plugin's `Start`). The deferred dispatchers are
> `internal` so only UXAssist (the host, same assembly) can drive them; no `InternalsVisibleTo` is
> declared, so cross-assembly calls are rejected at compile time. Runtime idempotency (per-feature
> start-once) and per-frame re-entrancy guards (`Time.frameCount`) act as defense-in-depth.

### `UXAssist.Common.Config`

#### `FactoryConfigProvider`

```csharp
public static class FactoryConfigProvider
```

- `public static ConfigEntry<bool> UnlimitInteractiveEnabled { get; }`
- `public static ConfigEntry<bool> RemoveSomeConditionEnabled { get; }`
- `public static ConfigEntry<bool> NightLightEnabled { get; }`
- `public static ConfigEntry<float> NightLightAngleX { get; }`
- `public static ConfigEntry<float> NightLightAngleY { get; }`
- `public static ConfigEntry<bool> RemoveBuildRangeLimitEnabled { get; }`
- `public static ConfigEntry<bool> LargerAreaForUpgradeAndDismantleEnabled { get; }`
- `public static ConfigEntry<bool> LargerAreaForTerraformEnabled { get; }`
- `public static ConfigEntry<bool> OffGridBuildingEnabled { get; }`
- `public static ConfigEntry<bool> TreatStackingAsSingleEnabled { get; }`
- `public static ConfigEntry<bool> QuickBuildAndDismantleLabsEnabled { get; }`
- `public static ConfigEntry<bool> ProtectVeinsFromExhaustionEnabled { get; }`
- `public static ConfigEntry<bool> DoNotRenderEntitiesEnabled { get; }`
- `public static ConfigEntry<bool> DragBuildPowerPolesEnabled { get; }`
- `public static ConfigEntry<bool> DragBuildPowerPolesAlternatelyEnabled { get; }`
- `public static ConfigEntry<bool> AutoConstructButtonEnabled { get; }`
- `public static ConfigEntry<bool> BeltSignalsForBuyOutEnabled { get; }`
- `public static ConfigEntry<bool> TankFastFillInAndTakeOutEnabled { get; }`
- `public static ConfigEntry<int> TankFastFillInAndTakeOutMultiplier { get; }`
- `public static ConfigEntry<bool> CutConveyorBeltEnabled { get; }`
- `public static ConfigEntry<bool> TweakBuildingBufferEnabled { get; }`
- `public static ConfigEntry<int> AssemblerBufferTimeMultiplier { get; }`
- `public static ConfigEntry<int> AssemblerBufferMininumMultiplier { get; }`
- `public static ConfigEntry<int> LabBufferMaxCountForAssemble { get; }`
- `public static ConfigEntry<int> LabBufferExtraCountForAdvancedAssemble { get; }`
- `public static ConfigEntry<int> LabBufferMaxCountForResearch { get; }`
- `public static ConfigEntry<int> ReceiverBufferCount { get; }`
- `public static ConfigEntry<int> EjectorBufferCount { get; }`
- `public static ConfigEntry<int> SiloBufferCount { get; }`
- `public static ConfigEntry<bool> ShortcutKeysForBlueprintCopyEnabled { get; }`
- `public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsEnabled { get; }`
- `public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeBranches { get; }`
- `public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeInserters { get; }`

#### `LogisticsConfigProvider`

```csharp
public static class LogisticsConfigProvider
```

- `public static ConfigEntry<bool> AutoConfigLogisticsEnabled { get; }`
- `public static ConfigEntry<bool> AutoConfigLimitAutoReplenishCount { get; }`
- `public static ConfigEntry<int> AutoConfigDispenserChargePower { get; }`
- `public static ConfigEntry<int> AutoConfigDispenserCourierCount { get; }`
- `public static ConfigEntry<int> AutoConfigBattleBaseChargePower { get; }`
- `public static ConfigEntry<int> AutoConfigPLSChargePower { get; }`
- `public static ConfigEntry<int> AutoConfigPLSMaxTripDrone { get; }`
- `public static ConfigEntry<int> AutoConfigPLSDroneMinDeliver { get; }`
- `public static ConfigEntry<int> AutoConfigPLSMinPilerValue { get; }`
- `public static ConfigEntry<int> AutoConfigPLSDroneCount { get; }`
- `public static ConfigEntry<bool> SetDefaultRemoteLogicToStorage { get; }`
- `public static ConfigEntry<int> AutoConfigILSChargePower { get; }`
- `public static ConfigEntry<int> AutoConfigILSMaxTripDrone { get; }`
- `public static ConfigEntry<int> AutoConfigILSMaxTripShip { get; }`
- `public static ConfigEntry<int> AutoConfigILSWarperDistance { get; }`
- `public static ConfigEntry<int> AutoConfigILSDroneMinDeliver { get; }`
- `public static ConfigEntry<int> AutoConfigILSShipMinDeliver { get; }`
- `public static ConfigEntry<int> AutoConfigILSMinPilerValue { get; }`
- `public static ConfigEntry<bool> AutoConfigILSIncludeOrbitCollector { get; }`
- `public static ConfigEntry<bool> AutoConfigILSWarperNecessary { get; }`
- `public static ConfigEntry<int> AutoConfigILSDroneCount { get; }`
- `public static ConfigEntry<int> AutoConfigILSShipCount { get; }`
- `public static ConfigEntry<int> AutoConfigVeinCollectorHarvestSpeed { get; }`
- `public static ConfigEntry<int> AutoConfigVeinCollectorMinPilerValue { get; }`
- `public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled { get; }`
- `public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled { get; }`
- `public static ConfigEntry<bool> GreaterPowerUsageInLogisticsEnabled { get; }`
- `public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled { get; }`
- `public static ConfigEntry<bool> RealtimeLogisticsInfoPanelEnabled { get; }`
- `public static ConfigEntry<bool> RealtimeLogisticsInfoPanelBarsEnabled { get; }`

## UXAssist.UI

### `MyWindowManager`

```csharp
public abstract class MyWindowManager
```

- `public static bool Initialized { get; private set; }`
- `public static void Enable(bool on)`
- `public static void InitBaseObjects()`
- `public static T CreateWindow<T>(string name, string title = "") where T : MyWindow`
- `public static void DestroyWindow(ManualBehaviour win)`

Nested type:

- `public class Patch : PatchImpl<Patch>`
  - `protected override void OnEnable()`
  - `public static void UIRoot__OnDestroy_Postfix()`
  - `public static void UIRoot__OnOpen_Postfix()`
  - `public static void UIRoot__OnUpdate_Postfix()`
  - `public static void UIGame_ShutAllFunctionWindow_Postfix()`

### `MyWindow`

```csharp
public class MyWindow : ManualBehaviour
```

- `public event Action OnFree`
- `public static void InitBaseObject()`
- `public static T Create<T>(string name, string title = "") where T : MyWindow`
- `public override void _OnOpen()`
- `public override void _OnFree()`
- `public virtual void TryClose()`
- `public virtual bool IsWindowFunctional()`
- `public void Open()`
- `public void Close()`
- `public void SetTitle(string title)`
- `public void AutoFitWindowSize()`
- `public static Text AddText(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")`
- `public Text AddText2(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")`
- `public static UIButton AddTipsButton(float x, float y, RectTransform parent, string label, string tip, string content, string objName = "tips-button")`
- `public UIButton AddTipsButton2(float x, float y, RectTransform parent, string label, string tip, string content, string objName = "tips-button")`
- `public UIButton AddButton(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)`
- `public UIButton AddButton(float x, float y, float width, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)`
- `public MyFlatButton AddFlatButton(float x, float y, RectTransform parent, string text = "", int fontSize = 12, string objName = "button", UnityAction onClick = null)`
- `public MyCheckBox AddCheckBox(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)`
- `public MyComboBox AddComboBox(float x, float y, RectTransform parent, int fontSize = 15)`
- `public MyCornerComboBox AddCornerComboBox(float x, float y, RectTransform parent, int fontSize = 15)`
- `public MySlider AddSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f)`
- `public MySideSlider AddSideSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f, float textWidth = 0f)`
- `public MySlider AddSlider<T>(float x, float y, RectTransform parent, ConfigEntry<T> config, ValueMapper<T> valueMapper, string format = "G", float width = 0f)`
- `public MySideSlider AddSideSlider<T>(float x, float y, RectTransform parent, ConfigEntry<T> config, ValueMapper<T> valueMapper, string format = "G", float width = 0f, float textWidth = 0f)`
- `public MySlider AddSlider<T>(float x, float y, RectTransform parent, ConfigEntry<T> config, T[] valueList, string format = "G", float width = 0f)`
- `public MySideSlider AddSideSlider<T>(float x, float y, RectTransform parent, ConfigEntry<T> config, T[] valueList, string format = "G", float width = 0f)`
- `public InputField AddInputField(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "input", UnityAction<string> onChanged = null, UnityAction<string> onEditEnd = null)`
- `public InputField AddInputField(float x, float y, float width, RectTransform parent, ConfigEntry<string> config, int fontSize = 16, string objName = "input")`
- `protected float MaxY`
- `protected const float TitleHeight = 48f`
- `protected const float TabWidth = 105f`
- `protected const float TabHeight = 27f`
- `protected const float Margin = 30f`
- `protected const float Spacing = 10f`

Nested helper types:

- `public class ValueMapper<T>`
  - `public virtual int Min { get; }`
  - `public virtual int Max { get; }`
  - `public virtual int ValueToIndex(T value)`
  - `public virtual T IndexToValue(int index)`
  - `public virtual string FormatValue(string format, T value)`
- `public class RangeValueMapper<T>(int min, int max) : ValueMapper<T>`
  - `public override int Min { get; }`
  - `public override int Max { get; }`
- `public class RangeValueWithMultiplierMapper<T>(int min, int max, T multiplier) : ValueMapper<T>`
  - `public override int Min { get; }`
  - `public override int Max { get; }`
  - `public override T IndexToValue(int index)`
  - `public override int ValueToIndex(T value)`

### `MyWindowWithTabs`

```csharp
public class MyWindowWithTabs : MyWindow
```

- `public override void TryClose()`
- `public override bool IsWindowFunctional()`
- `public RectTransform AddTab(RectTransform parent, string label)`
- `public void AddSplitter(RectTransform parent, float spacing)`
- `public void AddTabGroup(RectTransform parent, string label, string objName = "tabl-group-label")`
- `protected void SetCurrentTab(int index)`

### `MyConfigWindow`

```csharp
public class MyConfigWindow : MyWindowWithTabs
```

- `public static Action<MyConfigWindow, RectTransform> OnUICreated`
- `public static Action OnUpdateUI`
- `public static MyConfigWindow CreateInstance()`
- `public static void DestroyInstance(MyConfigWindow win)`
- `public override void _OnCreate()`
- `public override void _OnDestroy()`
- `public override bool _OnInit()`
- `public override void _OnUpdate()`

### `MyCheckBox`

```csharp
public class MyCheckBox : MonoBehaviour
```

- `public RectTransform rectTrans`
- `public UIButton uiButton`
- `public Image boxImage`
- `public Image checkImage`
- `public Text labelText`
- `public event Action OnChecked`
- `public bool Checked { get; set; }`
- `public float Width { get; }`
- `public float Height { get; }`
- `public static void InitBaseObject()`
- `public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)`
- `public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, bool check, string label = "", int fontSize = 15)`
- `public static MyCheckBox CreateCheckBox(float x, float y, RectTransform parent, int fontSize = 15)`
- `public void SetLabelText(string val)`
- `public void SetEnable(bool on)`
- `public void SetConfigEntry(ConfigEntry<bool> config)`
- `public MyCheckBox WithLabelText(string val)`
- `public MyCheckBox WithCheck(bool check)`
- `public MyCheckBox WithSmallerBox(float boxSize = 20f)`
- `public MyCheckBox WithEnable(bool on)`
- `public MyCheckBox WithConfigEntry(ConfigEntry<bool> config)`
- `public void OnClick(int obj)`
- `protected void OnDestroy()`

### `MyCheckButton`

```csharp
public class MyCheckButton : MonoBehaviour
```

- `public RectTransform rectTrans`
- `public UIButton uiButton`
- `public Image icon`
- `public Text labelText`
- `public event Action OnChecked`
- `public bool Checked { get; set; }`
- `public float Width { get; }`
- `public float Height { get; }`
- `public static void InitBaseObject()`
- `public static MyCheckButton CreateCheckButton(float x, float y, RectTransform parent, ConfigEntry<bool> config, string label = "", int fontSize = 15)`
- `public static MyCheckButton CreateCheckButton(float x, float y, RectTransform parent, bool check, string label = "", int fontSize = 15)`
- `public static MyCheckButton CreateCheckButton(float x, float y, RectTransform parent, int fontSize = 15)`
- `public void SetCheckedWithEvent(bool check)`
- `public void SetLabelText(string val)`
- `public void SetConfigEntry(ConfigEntry<bool> config)`
- `public MyCheckButton WithLabelText(string val)`
- `public MyCheckButton WithSize(float width, float height)`
- `public MyCheckButton WithIconWidth(float width)`
- `public MyCheckButton WithIcon(Sprite sprite = null)`
- `public MyCheckButton WithTip(string tip, float delay = 1f)`
- `public void SetIcon(Sprite sprite = null)`
- `public MyCheckButton WithCheck(bool check)`
- `public MyCheckButton WithConfigEntry(ConfigEntry<bool> config)`
- `public void OnClick(int obj)`
- `protected void OnDestroy()`

### `MySlider`

```csharp
public class MySlider : MonoBehaviour
```

- `public RectTransform rectTrans`
- `public Slider slider`
- `public RectTransform handleSlideArea`
- `public Text labelText`
- `public string labelFormat`
- `public event Action OnValueChanged`
- `public float Value { get; set; }`
- `public static MySlider CreateSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f)`
- `public static MySlider CreateSlider(float x, float y, RectTransform parent, float width = 0f)`
- `public void SetEnable(bool on)`
- `public void UpdateLabel()`
- `public void SetLabelText(string text)`
- `public MySlider WithValue(float value)`
- `public MySlider WithMinMaxValue(float min, float max)`
- `public MySlider WithLabelFormat(string format)`
- `public MySlider WithEnable(bool on)`
- `public MySlider WithSmallerHandle(float deltaX = 10f, float deltaY = 0f)`
- `public void SliderChanged(float val)`

### `MySideSlider`

```csharp
public class MySideSlider : MonoBehaviour
```

- `public RectTransform rectTrans`
- `public Slider slider`
- `public Text labelText`
- `public string labelFormat`
- `public event Action OnValueChanged`
- `public float Value { get; set; }`
- `public static MySideSlider CreateSlider(float x, float y, RectTransform parent, float value, float minValue, float maxValue, string format = "G", float width = 0f, float textWidth = 0f)`
- `public static MySideSlider CreateSlider(float x, float y, RectTransform parent, float width = 0f, float textWidth = 0f)`
- `public void SetEnable(bool on)`
- `public void UpdateLabel()`
- `public void SetLabelText(string text)`
- `public MySideSlider WithValue(float value)`
- `public MySideSlider WithMinMaxValue(float min, float max)`
- `public MySideSlider WithLabelFormat(string format)`
- `public MySideSlider WithFontSize(int fontSize)`
- `public MySideSlider WithEnable(bool on)`
- `public void SliderChanged(float val)`

### `MyComboBox`

```csharp
public class MyComboBox : MonoBehaviour
```

- `public Action<int> OnSelChanged`
- `public float Width { get; }`
- `public float Height { get; }`
- `public static void InitBaseObject()`
- `public static MyComboBox CreateComboBox(float x, float y, RectTransform parent)`
- `public void SetFontSize(int size)`
- `public void SetItems(params string[] items)`
- `public void SetIndex(int index)`
- `public void SetSize(float width, float height)`
- `public void AddOnSelChanged(Action<int> action)`
- `public void SetConfigEntry(ConfigEntry<int> config)`
- `public MyComboBox WithFontSize(int size)`
- `public MyComboBox WithItems(params string[] items)`
- `public MyComboBox WithIndex(int index)`
- `public MyComboBox WithSize(float width, float height)`
- `public MyComboBox WithOnSelChanged(params Action<int>[] action)`
- `public MyComboBox WithConfigEntry(ConfigEntry<int> config)`
- `protected void OnDestroy()`

### `MyCornerComboBox`

```csharp
public class MyCornerComboBox : MonoBehaviour
```

- `public Action<int> OnSelChanged`
- `public List<string> Items { get; }`
- `public float Width { get; }`
- `public float Height { get; }`
- `public static void InitBaseObject()`
- `public static MyCornerComboBox CreateComboBox(float x, float y, RectTransform parent, bool topRight = false)`
- `public void SetFontSize(int size)`
- `public void SetItems(params string[] items)`
- `public void UpdateLabelText()`
- `public void SetIndex(int index)`
- `public void SetSize(float width, float height)`
- `public void AddOnSelChanged(Action<int> action)`
- `public void SetConfigEntry(ConfigEntry<int> config)`
- `public MyCornerComboBox WithFontSize(int size)`
- `public MyCornerComboBox WithItems(params string[] items)`
- `public MyCornerComboBox WithIndex(int index)`
- `public MyCornerComboBox WithSize(float width, float height)`
- `public MyCornerComboBox WithOnSelChanged(params Action<int>[] action)`
- `public MyCornerComboBox WithConfigEntry(ConfigEntry<int> config)`
- `protected void OnDestroy()`

### `MyFlatButton`

```csharp
public class MyFlatButton : MonoBehaviour
```

- `public RectTransform rectTrans`
- `public UIButton uiButton`
- `public Text labelText`
- `public float Width { get; }`
- `public float Height { get; }`
- `public static void InitBaseObject()`
- `public static MyFlatButton CreateFlatButton(float x, float y, RectTransform parent, string label = "", int fontSize = 15, Action<int> onClick = null)`
- `public static MyFlatButton CreateFlatButton(float x, float y, RectTransform parent, int fontSize = 15, Action<int> onClick = null)`
- `public void SetLabelText(string val)`
- `public MyFlatButton WithLabelText(string val)`
- `public MyFlatButton WithSize(float width, float height)`
- `public MyFlatButton WithFontSize(int fontSize)`
- `public MyFlatButton WithTip(string tip, float delay = 1f)`

### `MyKeyBinder`

```csharp
public class MyKeyBinder : MonoBehaviour
```

- `[SerializeField] public Text functionText`
- `[SerializeField] public Text keyText`
- `[SerializeField] public InputField setTheKeyInput`
- `[SerializeField] public Toggle setTheKeyToggle`
- `[SerializeField] public RectTransform rectTrans`
- `[SerializeField] public UIButton inputUIButton`
- `[SerializeField] public Text conflictText`
- `[SerializeField] public Text waitingText`
- `[SerializeField] public UIButton setDefaultUIButton`
- `[SerializeField] public UIButton setNoneKeyUIButton`
- `protected event Action OnFree`
- `public static RectTransform CreateKeyBinder(float x, float y, RectTransform parent, ConfigEntry<KeyboardShortcut> config, string label = "", int fontSize = 17)`
- `public void Reset()`
- `public void OnInputUIButtonClick(int data)`
- `public void OnSetDefaultKeyClick(int data)`
- `public void OnSetNoneKeyClick(int data)`
- `public void SettingChanged()`
- `protected void OnDestroy()`

### `ConfigTabGroup`

```csharp
public class ConfigTabGroup
```

- `public IReadOnlyList<Group> Groups { get; }`
- `public int CurrentTabIndex { get; }`
- `public int TabCount { get; }`
- `public void AddGroup(string label)`
- `public int AddTab(RectTransform rectTransform, UIButton button)`
- `public void SetCurrentTab(int index)`

Nested types:

- `public class Tab`
  - `public RectTransform RectTransform { get; set; }`
  - `public UIButton Button { get; set; }`
- `public class Group`
  - `public string Label { get; set; }`
  - `public List<Tab> Tabs { get; }`

### `LayoutHelper`

```csharp
public static class LayoutHelper
```

- `public static Text AddText(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")`
- `public static UIButton AddTipsButton(float x, float y, RectTransform parent, string label, string tip, string content, string objName = "tips-button")`
- `public static UIButton AddButton(float x, float y, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)`
- `public static UIButton AddButton(float x, float y, float width, RectTransform parent, string text = "", int fontSize = 16, string objName = "button", UnityAction onClick = null)`

### `Util`

```csharp
public static class Util
```

- `public static RectTransform NormalizeRectWithTopLeft(Component cmp, float left, float top, Transform parent = null)`
- `public static RectTransform NormalizeRectWithTopRight(Component cmp, float right, float top, Transform parent = null)`
- `public static RectTransform NormalizeRectWithBottomLeft(Component cmp, float left, float bottom, Transform parent = null)`
- `public static RectTransform NormalizeRectWithMargin(Component cmp, float top, float left, float bottom, float right, Transform parent = null)`
- `public static RectTransform NormalizeRectCenter(GameObject go, float width = 0, float height = 0)`
