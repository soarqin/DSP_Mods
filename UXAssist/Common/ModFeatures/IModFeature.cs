namespace UXAssist.Common.ModFeatures;

/// <summary>
/// Interface implemented by instance mod features registered with <see cref="ModFeatureRegistry"/>.
/// All lifecycle methods are invoked by the registry in the order described below.
/// </summary>
public interface IModFeature
{
    /// <summary>
    /// Called once when the mod is initialized.
    /// </summary>
    void Init();

    /// <summary>
    /// Called once after initialization, when the mod should begin active behavior.
    /// </summary>
    void Start();

    /// <summary>
    /// Called once when the mod is being shut down or re-initialized.
    /// </summary>
    void Uninit();

    /// <summary>
    /// Called every frame for input handling. Should be lightweight.
    /// </summary>
    void OnInputUpdate();

    /// <summary>
    /// Called every frame for general updates. Should be lightweight.
    /// </summary>
    void OnUpdate();
}
