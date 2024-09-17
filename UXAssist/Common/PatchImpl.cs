using System;
using System.Reflection;
using HarmonyLib;

namespace UXAssist.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PatchImplGuidAttribute(string guid) : Attribute
{
    public string Guid { get; } = guid;
}

public class PatchImpl<T> where T : new()
{
    private static T Instance { get; } = new();
    
    private Harmony _patch;
    
    public static void Enable(bool enable)
    {
        if (Instance is not PatchImpl<T> thisInstance)
        {
            UXAssist.Logger.LogError($"PatchImpl<{typeof(T).Name}> is not inherited correctly");
            return;
        }
        if (enable)
        {
            var guid = typeof(T).GetCustomAttribute<PatchImplGuidAttribute>()?.Guid;
            thisInstance._patch ??= Harmony.CreateAndPatchAll(typeof(T), guid);
            thisInstance.OnEnable();
            return;
        }
        thisInstance.OnDisable();
        thisInstance._patch?.UnpatchSelf();
        thisInstance._patch = null;
    }

    public static Harmony GetHarmony() => (Instance as PatchImpl<T>)?._patch;

    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}
