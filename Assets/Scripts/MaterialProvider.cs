using UnityEngine;

/// <summary>
/// Doodle Shader 材质提供器。Singleton。
/// 每帧同步 GameConfigSO → Shader 参数，Inspector 调节实时生效。
/// </summary>
public class MaterialProvider : MonoBehaviour
{
    public static MaterialProvider Instance { get; private set; }

    Material _doodleMaterial;
    GameConfigSO _config;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // 每帧同步：GameConfigSO Inspector 调参 → Shader 实时生效
        if (_config != null) SyncToShader();
    }

    public void Init(GameConfigSO config)
    {
        Shader shader = Shader.Find("UI/DoodleImage");
        if (shader == null) { Debug.LogWarning("[MaterialProvider] UI/DoodleImage not found"); return; }
        _doodleMaterial = new Material(shader);
        _config = config;
        SyncToShader();
    }

    /// <summary>手动同步（外部队设时调用）</summary>
    public void SyncToShader()
    {
        if (_doodleMaterial == null || _config == null) return;
        var d = _config.doodle;
        _doodleMaterial.SetFloat("_OutlineWidth", d.outlineWidth);
        _doodleMaterial.SetColor("_OutlineColor", Color.black);
        _doodleMaterial.SetFloat("_OutlineDensity", d.outlineDensity);
        _doodleMaterial.SetFloat("_NoiseScale", d.noiseScale);
        _doodleMaterial.SetFloat("_OutlineJitter", d.outlineJitter);
        _doodleMaterial.SetFloat("_OutlineSpeed", d.outlineSpeed);
        _doodleMaterial.SetFloat("_BodyJitter", d.bodyJitter);
        _doodleMaterial.SetFloat("_BodySpeed", d.bodySpeed);
    }

    public Material GetMaterial()
    {
        return _doodleMaterial;
    }
}
