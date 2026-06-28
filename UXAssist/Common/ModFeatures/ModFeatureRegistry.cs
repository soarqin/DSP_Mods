using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UXAssist.Common.ModFeatures;

/// <summary>
/// Registry that discovers mod feature classes marked with <see cref="ModFeatureAttribute"/> from assemblies
/// and holds registered instance mod features. Discovered features invoke static lifecycle methods;
/// static lifecycle methods are optional and are skipped if missing.
/// </summary>
/// <remarks>
/// <para>
/// The registry uses shared static collections that accumulate features from all mods. To avoid duplicate
/// lifecycle execution, <strong>only UXAssist</strong> (the host mod) drives the deferred lifecycle phases
/// (<see cref="StartAll"/>, <see cref="UninitAll"/>, <see cref="OnInputUpdateAll"/>,
/// <see cref="OnUpdateAll"/>). <see cref="Init"/> runs eagerly when a feature is registered via
/// <see cref="Discover"/>/<see cref="Register{T}"/>, preserving the original BepInEx <c>Awake</c> timing
/// that keybind registration and other early setup depend on (the game's <c>UIOptionWindow._OnCreate</c>
/// copies registered keybinds, which happens only after all plugins have finished loading).
/// </para>
/// <para>
/// The deferred dispatchers are <c>internal</c> to enforce host-only driving at compile time (no
/// <c>InternalsVisibleTo</c> is declared, so cross-assembly callers are rejected by the compiler), and
/// each carries runtime idempotency / per-frame guards as defense-in-depth.
/// </para>
/// </remarks>
public static class ModFeatureRegistry
{
    private sealed class StaticFeature
    {
        public Type Type { get; }
        public Action Start { get; }
        public Action Uninit { get; }
        public Action OnInputUpdate { get; }
        public Action OnUpdate { get; }
        public bool Started { get; set; }

        public StaticFeature(Type type)
        {
            Type = type;
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

    private sealed class InstanceFeature
    {
        public IModFeature Feature { get; }
        public bool Started { get; set; }

        public InstanceFeature(IModFeature feature)
        {
            Feature = feature;
        }
    }

    private static readonly List<StaticFeature> _staticFeatures = [];
    private static readonly List<InstanceFeature> _instanceFeatures = [];
    private static readonly HashSet<Type> _registeredInstanceTypes = [];
    private static readonly HashSet<Assembly> _discoveredAssemblies = [];

    private static int _lastInputUpdateFrame = -1;
    private static int _lastUpdateFrame = -1;

    /// <summary>
    /// Discovers mod feature classes marked with <see cref="ModFeatureAttribute"/> in the given assembly,
    /// and initializes each one immediately (calling its static <c>Init</c> method if present). Each
    /// assembly is only discovered once. Dependent mods call this in their <c>Awake</c>; the host
    /// (UXAssist) drives the deferred lifecycle phases (<see cref="StartAll"/> etc.).
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public static void Discover(Assembly assembly)
    {
        if (!_discoveredAssemblies.Add(assembly)) return;

        var staticTypes = Util.GetTypesFiltered(assembly, t =>
            t.IsClass &&
            Attribute.IsDefined(t, typeof(ModFeatureAttribute)) &&
            !typeof(IModFeature).IsAssignableFrom(t));

        foreach (var type in staticTypes.OrderBy(GetOrder))
        {
            if (_staticFeatures.All(f => f.Type != type))
            {
                var feature = new StaticFeature(type);
                _staticFeatures.Add(feature);
                // Init eagerly at registration time, preserving the original Awake-phase timing that
                // keybind registration and other early setup rely on.
                InitStatic(type);
            }
        }
    }

    /// <summary>
    /// Registers a new instance mod feature, initializing it immediately. If an instance of the same
    /// type is already registered, this is a no-op. Dependent mods call this in their <c>Awake</c>; the
    /// host (UXAssist) drives the deferred lifecycle phases.
    /// </summary>
    /// <typeparam name="T">The mod feature type to register.</typeparam>
    public static void Register<T>() where T : class, IModFeature, new()
    {
        var type = typeof(T);
        if (!_registeredInstanceTypes.Add(type)) return;

        var instance = new T();
        _instanceFeatures.Add(new InstanceFeature(instance));
        // Init eagerly at registration time, preserving the original Awake-phase timing.
        instance.Init();
    }

    /// <summary>
    /// Calls <see cref="IModFeature.Start"/> on all registered instance features
    /// and invokes the cached static <c>Start</c> methods on all discovered mod feature classes.
    /// Each feature is started at most once; subsequent calls are no-ops for already-started features.
    /// </summary>
    internal static void StartAll()
    {
        foreach (var f in _staticFeatures)
        {
            if (f.Started) continue;
            f.Start?.Invoke();
            f.Started = true;
        }
        foreach (var f in _instanceFeatures)
        {
            if (f.Started) continue;
            f.Feature.Start();
            f.Started = true;
        }
    }

    /// <summary>
    /// Calls <see cref="IModFeature.Uninit"/> on all registered instance features
    /// and invokes the cached static <c>Uninit</c> methods on all discovered mod feature classes,
    /// then resets their state so they can be re-started.
    /// </summary>
    internal static void UninitAll()
    {
        foreach (var f in _staticFeatures)
        {
            f.Uninit?.Invoke();
            f.Started = false;
        }
        foreach (var f in _instanceFeatures)
        {
            f.Feature.Uninit();
            f.Started = false;
        }
    }

    /// <summary>
    /// Calls <see cref="IModFeature.OnInputUpdate"/> on all registered instance features
    /// and invokes the cached static <c>OnInputUpdate</c> delegates on all discovered mod feature classes.
    /// This method is meant to be called every frame; no reflection is performed here.
    /// Guarded per-frame to prevent duplicate execution within the same frame.
    /// </summary>
    internal static void OnInputUpdateAll()
    {
        var frame = Time.frameCount;
        if (frame == _lastInputUpdateFrame) return;
        _lastInputUpdateFrame = frame;
        foreach (var f in _staticFeatures) f.OnInputUpdate?.Invoke();
        foreach (var f in _instanceFeatures) f.Feature.OnInputUpdate();
    }

    /// <summary>
    /// Calls <see cref="IModFeature.OnUpdate"/> on all registered instance features
    /// and invokes the cached static <c>OnUpdate</c> delegates on all discovered mod feature classes.
    /// This method is meant to be called every frame; no reflection is performed here.
    /// Guarded per-frame to prevent duplicate execution within the same frame.
    /// </summary>
    internal static void OnUpdateAll()
    {
        var frame = Time.frameCount;
        if (frame == _lastUpdateFrame) return;
        _lastUpdateFrame = frame;
        foreach (var f in _staticFeatures) f.OnUpdate?.Invoke();
        foreach (var f in _instanceFeatures) f.Feature.OnUpdate();
    }

    private static void InitStatic(Type type)
    {
        var init = type.GetMethod("Init",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null, Type.EmptyTypes, null);
        init?.Invoke(null, null);
    }

    private static int GetOrder(Type type)
    {
        return type.GetCustomAttribute<ModFeatureAttribute>()?.Order ?? 0;
    }
}
