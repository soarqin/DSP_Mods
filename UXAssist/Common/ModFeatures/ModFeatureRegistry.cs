using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UXAssist.Common.ModFeatures;

/// <summary>
/// Registry that discovers static mod features from assemblies and holds registered instance mod features.
/// Static lifecycle methods are optional; if a method is missing it is simply skipped.
/// </summary>
public static class ModFeatureRegistry
{
    private sealed class StaticFeature
    {
        public Type Type { get; }
        public Action Init { get; }
        public Action Start { get; }
        public Action Uninit { get; }
        public Action OnInputUpdate { get; }
        public Action OnUpdate { get; }

        public StaticFeature(Type type)
        {
            Type = type;
            Init = GetDelegate(type, "Init");
            Start = GetDelegate(type, "Start");
            Uninit = GetDelegate(type, "Uninit");
            OnInputUpdate = GetDelegate(type, "OnInputUpdate");
            OnUpdate = GetDelegate(type, "OnUpdate");
        }

        private static Action GetDelegate(Type type, string methodName)
        {
            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, Type.EmptyTypes, null);
            if (method == null) return null;
            return (Action)Delegate.CreateDelegate(typeof(Action), method);
        }
    }

    private static readonly List<StaticFeature> _staticFeatures = [];
    private static readonly List<IModFeature> _instanceFeatures = [];
    private static readonly HashSet<Type> _registeredInstanceTypes = [];
    private static readonly HashSet<Assembly> _discoveredAssemblies = [];

    /// <summary>
    /// Discovers static mod feature classes marked with <see cref="ModFeatureAttribute"/> in the given assembly.
    /// Each assembly is only discovered once.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public static void Discover(Assembly assembly)
    {
        if (!_discoveredAssemblies.Add(assembly)) return;

        var staticTypes = Util.GetTypesFiltered(assembly, t =>
            t.IsClass && !t.IsInterface &&
            Attribute.IsDefined(t, typeof(ModFeatureAttribute)) &&
            !typeof(IModFeature).IsAssignableFrom(t));

        foreach (var type in staticTypes.OrderBy(GetOrder))
        {
            if (_staticFeatures.All(f => f.Type != type))
                _staticFeatures.Add(new StaticFeature(type));
        }
    }

    /// <summary>
    /// Registers a new instance mod feature if an instance of the same type is not already registered.
    /// </summary>
    /// <typeparam name="T">The mod feature type to register.</typeparam>
    public static void Register<T>() where T : class, IModFeature, new()
    {
        var type = typeof(T);
        if (!_registeredInstanceTypes.Add(type)) return;

        var instance = new T();
        _instanceFeatures.Add(instance);
    }

    /// <summary>
    /// Calls <see cref="IModFeature.Init"/> on all registered instance features
    /// and invokes the cached static <c>Init</c> methods on all discovered static features.
    /// </summary>
    public static void InitAll()
    {
        foreach (var f in _staticFeatures) f.Init?.Invoke();
        foreach (var f in _instanceFeatures) f.Init();
    }

    /// <summary>
    /// Calls <see cref="IModFeature.Start"/> on all registered instance features
    /// and invokes the cached static <c>Start</c> methods on all discovered static features.
    /// </summary>
    public static void StartAll()
    {
        foreach (var f in _staticFeatures) f.Start?.Invoke();
        foreach (var f in _instanceFeatures) f.Start();
    }

    /// <summary>
    /// Calls <see cref="IModFeature.Uninit"/> on all registered instance features
    /// and invokes the cached static <c>Uninit</c> methods on all discovered static features.
    /// </summary>
    public static void UninitAll()
    {
        foreach (var f in _staticFeatures) f.Uninit?.Invoke();
        foreach (var f in _instanceFeatures) f.Uninit();
    }

    /// <summary>
    /// Calls <see cref="IModFeature.OnInputUpdate"/> on all registered instance features
    /// and invokes the cached static <c>OnInputUpdate</c> delegates on all discovered static features.
    /// This method is meant to be called every frame; no reflection is performed here.
    /// </summary>
    public static void OnInputUpdateAll()
    {
        foreach (var f in _staticFeatures) f.OnInputUpdate?.Invoke();
        foreach (var f in _instanceFeatures) f.OnInputUpdate();
    }

    /// <summary>
    /// Calls <see cref="IModFeature.OnUpdate"/> on all registered instance features
    /// and invokes the cached static <c>OnUpdate</c> delegates on all discovered static features.
    /// This method is meant to be called every frame; no reflection is performed here.
    /// </summary>
    public static void OnUpdateAll()
    {
        foreach (var f in _staticFeatures) f.OnUpdate?.Invoke();
        foreach (var f in _instanceFeatures) f.OnUpdate();
    }

    private static int GetOrder(Type type)
    {
        return type.GetCustomAttribute<ModFeatureAttribute>()?.Order ?? 0;
    }
}
