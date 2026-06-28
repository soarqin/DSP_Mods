namespace UXAssist.Common.ModFeatures;

/// <summary>
/// Interface implemented by instance mod features registered with <see cref="ModFeatureRegistry"/>.
/// The registry drives the lifecycle; individual mods never call these methods directly.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Timing contract.</strong> The registry guarantees the following relative ordering, which
/// feature implementations may rely on:
/// </para>
/// <list type="bullet">
/// <item><see cref="Init"/> runs <strong>eagerly</strong> at registration time — synchronously inside
/// <see cref="ModFeatureRegistry.Register{T}"/>, during the registering mod's BepInEx <c>Awake</c> phase.
/// It therefore completes <em>before</em> the game scene loads, <em>before</em> any game object's
/// <c>Start</c>, and <em>before</em> the host mod's <see cref="Start"/>. This is the only phase where
/// early setup that must precede game initialization (e.g. keybind registration via CommonAPI's
/// <c>CustomKeyBindSystem</c>, whose registered bindings are copied by the game's
/// <c>UIOptionWindow._OnCreate</c> only after all plugins finish loading) can safely run. Implementations
/// must not depend on the game being loaded here.</item>
/// <item><see cref="Start"/> runs <strong>once</strong> during the host mod's (UXAssist) <c>Start</c>,
/// which is guaranteed to occur after <em>every</em> mod's <c>Awake</c> has finished (BepInEx runs all
/// plugins' <c>Awake</c> synchronously during load, before Unity dispatches any <c>Start</c>). This is
/// the phase for activating behavior that requires the game/runtime to be ready. It is driven solely by
/// UXAssist; dependent mods must not start features themselves.</item>
/// <item><see cref="Uninit"/> runs during the host's teardown (<c>OnDestroy</c>) and resets the feature
/// so it could be started again.</item>
/// <item><see cref="OnInputUpdate"/> and <see cref="OnUpdate"/> are called every frame by UXAssist; the
/// registry guarantees at most one invocation per frame even if multiple drivers exist.</item>
/// </list>
/// <para>
/// The same timing contract applies to static features discovered via <see cref="ModFeatureAttribute"/>;
/// see that attribute for details.
/// </para>
/// </remarks>
public interface IModFeature
{
    /// <summary>
    /// Called eagerly at registration time, during the registering mod's <c>Awake</c> phase, before the
    /// game scene loads and before any plugin's <see cref="Start"/>. Use this for early setup that must
    /// precede game initialization (e.g. keybind registration). Do not depend on the game being loaded
    /// here. Runs at most once per registration.
    /// </summary>
    void Init();

    /// <summary>
    /// Called once during the host mod's (UXAssist) <c>Start</c>, after all mods have finished
    /// <c>Awake</c> (and thus after every feature's <see cref="Init"/>). Use this to activate behavior
    /// that requires the game/runtime to be ready. Driven solely by UXAssist; runs at most once
    /// (a repeated driver call is a no-op for an already-started feature).
    /// </summary>
    void Start();

    /// <summary>
    /// Called during the host's teardown (<c>OnDestroy</c>). Reset any state created in
    /// <see cref="Init"/>/<see cref="Start"/> so the feature could be re-started. Runs on every
    /// registered feature.
    /// </summary>
    void Uninit();

    /// <summary>
    /// Called every frame for input handling. Should be lightweight. The registry guarantees at most
    /// one invocation per frame.
    /// </summary>
    void OnInputUpdate();

    /// <summary>
    /// Called every frame for general updates. Should be lightweight. The registry guarantees at most
    /// one invocation per frame.
    /// </summary>
    void OnUpdate();
}