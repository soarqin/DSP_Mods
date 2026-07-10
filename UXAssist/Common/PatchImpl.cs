using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace UXAssist.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PatchGuidAttribute(string guid) : Attribute
{
    public string Guid { get; } = guid;
}

public enum PatchCallbackFlag
{
    // By default, OnEnable() is called After patch applied, set this flag to call it before patch is applied
    CallOnEnableBeforePatch,
    // By default, OnDisable() is called Before patch removed, set this flag to call it after patch is removed
    CallOnDisableAfterUnpatch,
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class PatchSetCallbackFlagAttribute(PatchCallbackFlag flag) : Attribute
{
    public PatchCallbackFlag Flag { get; } = flag;
}

public class PatchImpl<T> where T : PatchImpl<T>, new()
{
    protected static T Instance { get; } = new();

    protected Harmony _patch;

    public static void Enable(bool enable)
    {
        var thisInstance = Instance;
        if (enable)
        {
            if (thisInstance._patch != null) return;
            var guid = typeof(T).GetCustomAttribute<PatchGuidAttribute>()?.Guid ?? $"PatchImpl.{typeof(T).FullName ?? typeof(T).ToString()}";
            var callOnEnableBefore = typeof(T).GetCustomAttributes<PatchSetCallbackFlagAttribute>().Any(n => n.Flag == PatchCallbackFlag.CallOnEnableBeforePatch);
            if (callOnEnableBefore) thisInstance.OnEnable();
            // Fail-soft patch application: applying patches at runtime can fail through no
            // fault of ours (e.g. Harmony re-runs another mod's fragile transpiler on a shared
            // target method). An escaping exception would abort the caller, which is usually a
            // ConfigEntry.SettingChanged handler chain, leaving later handlers (such as config
            // UI state sync) unexecuted. Log the failure and roll back instead of throwing.
            var patch = new Harmony(guid);
            try
            {
                patch.PatchAll(typeof(T));
            }
            catch (Exception e)
            {
                UXAssist.Logger.LogError($"Failed to apply Harmony patches for {typeof(T).FullName}: {e}");
                try
                {
                    patch.UnpatchSelf();
                }
                catch (Exception e2)
                {
                    UXAssist.Logger.LogError($"Failed to roll back partially applied patches for {typeof(T).FullName}: {e2}");
                }
                return;
            }
            thisInstance._patch = patch;
            if (!callOnEnableBefore) thisInstance.OnEnable();
            return;
        }
        if (thisInstance._patch == null) return;
        var callOnDisableAfter = typeof(T).GetCustomAttributes<PatchSetCallbackFlagAttribute>().Any(n => n.Flag == PatchCallbackFlag.CallOnDisableAfterUnpatch);
        if (!callOnDisableAfter) thisInstance.OnDisable();
        thisInstance._patch.UnpatchSelf();
        thisInstance._patch = null;
        if (callOnDisableAfter) thisInstance.OnDisable();
    }

    public static Harmony GetHarmony() => Instance._patch;

    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}
