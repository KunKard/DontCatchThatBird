using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 程序化生成手绘网格背景 + 线条持续抖动动画。
/// 挂到 Canvas 下，Start 时自动创建。
/// </summary>
[RequireComponent(typeof(Image))]
public class BackgroundGenerator : MonoBehaviour
{
    [Header("网格尺寸")]
    public int textureSize = 512;
    public float cellSize = 64f;
    public float lineWidth = 2f;
    public Color lineColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("线条抖动")]
    [Range(0f, 5f)] public float jitterIntensity = 2f;
    [Range(0f, 10f)] public float jitterSpeed = 3f;

    Material _animMaterial;
    float _time;

    void Start()
    {
        GenerateTexture();
        CreateAnimMaterial();
        StretchToFullScreen();
    }

    void Update()
    {
        // 每帧更新抖动偏移
        if (_animMaterial != null)
        {
            _time += Time.deltaTime * jitterSpeed;
            float ox = Mathf.PerlinNoise(_time * 0.1f, 0f) - 0.5f;
            float oy = Mathf.PerlinNoise(0f, _time * 0.1f) - 0.5f;
            _animMaterial.SetVector("_BodyOffset", new Vector4(ox, oy, 0, 0) * jitterIntensity * 0.002f);
        }
    }

    void GenerateTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color bg = Color.white;
        Color[] pixels = new Color[textureSize * textureSize];

        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

        // 水平线（噪声扭曲）
        for (float y = 0; y < textureSize; y += cellSize)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float no = Mathf.PerlinNoise(x / 8f, y / 8f) * 1.5f;
                int drawY = Mathf.RoundToInt(y + no);
                for (int w = 0; w < lineWidth && drawY + w < textureSize; w++)
                {
                    int idx = (drawY + w) * textureSize + x;
                    if (idx < pixels.Length) pixels[idx] = lineColor;
                }
            }
        }

        // 垂直线（噪声扭曲，不同种子）
        for (float x = 0; x < textureSize; x += cellSize)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float no = Mathf.PerlinNoise(y / 8f + 7.7f, x / 8f + 3.3f) * 1.5f;
                int drawX = Mathf.RoundToInt(x + no);
                for (int w = 0; w < lineWidth && drawX + w < textureSize; w++)
                {
                    int idx = y * textureSize + (drawX + w);
                    if (idx < pixels.Length) pixels[idx] = lineColor;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));

        Image img = GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = false;
    }

    void CreateAnimMaterial()
    {
        // 基于标准 UI/Default 的材质，加 _BodyOffset 支持
        Shader shader = Shader.Find("UI/DoodleImage");
        if (shader == null) return;

        _animMaterial = new Material(shader);
        _animMaterial.SetFloat("_OutlineWidth", 0);       // 背景不要描边
        _animMaterial.SetFloat("_BodyJitter", 0);          // 用我们自己的偏移
        _animMaterial.SetFloat("_BodySpeed", 0);

        Image img = GetComponent<Image>();
        if (img != null) img.material = _animMaterial;
    }

    void StretchToFullScreen()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.SetAsFirstSibling();
    }
}
