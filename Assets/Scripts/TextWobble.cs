using UnityEngine;

/// <summary>
/// 挂在任意 RectTransform 上（TMP/Image 等），模拟手绘抖动。
/// 比改 TMP Shader 简单得多，效果等效。
/// </summary>
public class TextWobble : MonoBehaviour
{
    public float intensity = 2f;   // 抖动幅度（像素）
    public float speed = 8f;       // 抖动速度

    RectTransform _rt;
    Vector2 _originalPos;
    float _seedX, _seedY;

    void Start()
    {
        _rt = GetComponent<RectTransform>();
        _originalPos = _rt.anchoredPosition;
        _seedX = Random.Range(0f, 100f);
        _seedY = Random.Range(0f, 100f);
    }

    void Update()
    {
        float x = (Mathf.PerlinNoise(_seedX + Time.time * speed, 0f) - 0.5f) * intensity * 2f;
        float y = (Mathf.PerlinNoise(0f, _seedY + Time.time * speed) - 0.5f) * intensity * 2f;
        _rt.anchoredPosition = _originalPos + new Vector2(x, y);
    }
}
