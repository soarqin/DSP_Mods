using HarmonyLib;

namespace UXAssist.Common;

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
            thisInstance._patch ??= Harmony.CreateAndPatchAll(typeof(T));
            thisInstance.OnEnable();
            return;
        }
        thisInstance.OnDisable();
        thisInstance._patch?.UnpatchSelf();
        thisInstance._patch = null;
    }

    protected static Harmony GetPatch() => (Instance as PatchImpl<T>)?._patch;

    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}
