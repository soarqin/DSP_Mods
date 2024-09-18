using System;
using System.Reflection;
using HarmonyLib;

namespace UXAssist.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PatchImplGuidAttribute(string guid) : Attribute
{
    public string Guid { get; } = guid;
}

public class PatchImpl<T> where T : PatchImpl<T>, new()
{
    private static T Instance { get; } = new();
    
    private Harmony _patch;

    public static void Enable(bool enable)
    {
        var thisInstance = Instance;
        if (enable)
        {
            var guid = typeof(T).GetCustomAttribute<PatchImplGuidAttribute>()?.Guid ?? $"PatchImpl.{typeof(T).FullName ?? typeof(T).ToString()}";
            thisInstance._patch ??= Harmony.CreateAndPatchAll(typeof(T), guid);
            thisInstance.OnEnable();
            return;
        }
        thisInstance.OnDisable();
        thisInstance._patch?.UnpatchSelf();
        thisInstance._patch = null;
    }

    public static Harmony GetHarmony() => Instance._patch;

    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}
