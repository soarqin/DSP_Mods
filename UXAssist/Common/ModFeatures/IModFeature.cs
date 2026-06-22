namespace UXAssist.Common.ModFeatures;

public interface IModFeature
{
    void Init();
    void Start();
    void Uninit();
    void OnInputUpdate();
    void OnUpdate();
}
