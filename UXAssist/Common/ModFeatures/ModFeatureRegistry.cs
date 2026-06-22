using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UXAssist.Common.ModFeatures;

public static class ModFeatureRegistry
{
    private static readonly List<Type> _staticFeatures = [];
    private static readonly List<IModFeature> _instanceFeatures = [];
    private static readonly HashSet<Assembly> _discoveredAssemblies = [];

    public static void Discover(Assembly assembly)
    {
        if (!_discoveredAssemblies.Add(assembly)) return;

        var staticTypes = Util.GetTypesFiltered(assembly, t =>
            t.IsClass && t.IsAbstract && t.IsSealed &&
            Attribute.IsDefined(t, typeof(ModFeatureAttribute)));

        foreach (var type in staticTypes.OrderBy(GetOrder))
        {
            if (!_staticFeatures.Contains(type))
                _staticFeatures.Add(type);
        }
    }

    public static void Register<T>() where T : class, IModFeature, new()
    {
        var instance = new T();
        _instanceFeatures.Add(instance);
    }

    public static void InitAll()
    {
        ForEachStatic("Init");
        foreach (var f in _instanceFeatures) f.Init();
    }

    public static void StartAll()
    {
        ForEachStatic("Start");
        foreach (var f in _instanceFeatures) f.Start();
    }

    public static void UninitAll()
    {
        ForEachStatic("Uninit");
        foreach (var f in _instanceFeatures) f.Uninit();
    }

    public static void OnInputUpdateAll()
    {
        ForEachStatic("OnInputUpdate");
        foreach (var f in _instanceFeatures) f.OnInputUpdate();
    }

    public static void OnUpdateAll()
    {
        ForEachStatic("OnUpdate");
        foreach (var f in _instanceFeatures) f.OnUpdate();
    }

    private static void ForEachStatic(string methodName)
    {
        foreach (var type in _staticFeatures)
        {
            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, null);
        }
    }

    private static int GetOrder(Type type)
    {
        return type.GetCustomAttribute<ModFeatureAttribute>()?.Order ?? 0;
    }
}
