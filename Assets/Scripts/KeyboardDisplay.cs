using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 管理虚拟键盘。右键组件 → "Setup Keys" 生成 37 键。
/// </summary>
[ExecuteInEditMode]
public class KeyboardDisplay : MonoBehaviour
{
    [Header("按键尺寸")]
    public float keyWidth = 84f;
    public float keyHeight = 84f;
    public float spacing = 12f;
    public float fontSize = 26f;

    [Header("字体")]
    public TMP_FontAsset keyFont;

    [Header("描边")]
    public float outlineWidth = 2f;

    [Header("高亮颜色")]
    public Color normalBg = Color.white;
    public Color highlightBg = Color.yellow;

    static readonly KeyCode[][] Rows = new KeyCode[][] {
        new[] { KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,
                KeyCode.Alpha6,KeyCode.Alpha7,KeyCode.Alpha8,KeyCode.Alpha9,KeyCode.Alpha0 },
        new[] { KeyCode.Q,KeyCode.W,KeyCode.E,KeyCode.R,KeyCode.T,KeyCode.Y,KeyCode.U,KeyCode.I,KeyCode.O,KeyCode.P },
        new[] { KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.F,KeyCode.G,KeyCode.H,KeyCode.J,KeyCode.K,KeyCode.L },
        new[] { KeyCode.Z,KeyCode.X,KeyCode.C,KeyCode.V,KeyCode.B,KeyCode.N,KeyCode.M },
        new[] { KeyCode.Space },
    };

    Dictionary<KeyCode, Image> _keyMap = new Dictionary<KeyCode, Image>();
    Dictionary<Image, Coroutine> _activeFlashes = new Dictionary<Image, Coroutine>();
    Image _currentHighlight;

    static Sprite _whiteSquareSprite; // 带透明边距的白色方块，共享

    // ========== Editor 一键生成 ==========

    [ContextMenu("Setup Keys")]
    void SetupKeys()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        _keyMap.Clear();

        float totalH = Rows.Length * (keyHeight + spacing) - spacing;
        float y = totalH * 0.5f - keyHeight * 0.5f;

        for (int r = 0; r < Rows.Length; r++)
        {
            bool isSpace = (r == Rows.Length - 1);
            float w = isSpace ? keyWidth * 5f : keyWidth;
            float rowW = Rows[r].Length * (w + spacing) - spacing;
            float x = -rowW * 0.5f + w * 0.5f;

            for (int i = 0; i < Rows[r].Length; i++)
            {
                CreateKey(Rows[r][i], x, y, w);
                x += w + spacing;
            }
            y -= keyHeight + spacing;
        }
    }

    void CreateKey(KeyCode key, float x, float y, float width)
    {
        GameObject go = new GameObject(key.ToString(), typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, keyHeight);
        rt.anchoredPosition = new Vector2(x, y);

        Image img = go.AddComponent<Image>();
        img.color = normalBg;
        img.raycastTarget = false;

        // 使用带透明边距的 Sprite，让 Shader 可检测到 alpha 边缘
        if (_whiteSquareSprite == null)
            _whiteSquareSprite = GenerateMarginSprite(Mathf.RoundToInt(width), Mathf.RoundToInt(keyHeight), 8);
        img.sprite = _whiteSquareSprite;

        // 黑色描边
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(outlineWidth, -outlineWidth);

        _keyMap[key] = img;

        // TMP 标签
        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = KeyToString(key);
        tmp.font = keyFont;
        tmp.fontSize = (key == KeyCode.Space) ? fontSize - 4f : fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
    }

    // ========== Runtime ==========

    void Awake()
    {
        if (_keyMap.Count == 0) RebuildMap();
    }

    void Start()
    {
    }

    /// <summary>生成带透明边距的白色方块 Sprite（alpha 边缘 → Shader 可描边）</summary>
    static Sprite GenerateMarginSprite(int innerW, int innerH, int margin)
    {
        int w = innerW + margin * 2;
        int h = innerH + margin * 2;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool edge = x < margin || x >= w - margin || y < margin || y >= h - margin;
                pixels[y * w + x] = edge ? Color.clear : Color.white;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    void RebuildMap()
    {
        _keyMap.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Image img = child.GetComponent<Image>();
            if (img == null) continue;
            if (System.Enum.TryParse<KeyCode>(child.name, true, out KeyCode key))
                _keyMap[key] = img;
        }
    }

    // ========== 公共接口 ==========

    public Vector3 GetKeyPosition(KeyCode key)
    {
        if (_keyMap.TryGetValue(key, out var img) && img != null)
            return img.transform.position + Vector3.up * 25f;
        return Vector3.zero;
    }

    public void HighlightKey(KeyCode key)
    {
        ClearHighlight();
        if (_keyMap.TryGetValue(key, out var img) && img != null)
        {
            img.color = highlightBg;
            _currentHighlight = img;
        }
    }

    /// <summary>按对键 — 浅绿</summary>
    public void FlashCorrectKey(KeyCode key)
    {
        if (_keyMap.TryGetValue(key, out var img) && img != null)
            StartFlash(img, new Color(0.5f, 1f, 0.5f));
    }

    /// <summary>按错键 — 浅红</summary>
    public void FlashWrongKey(KeyCode key)
    {
        if (_keyMap.TryGetValue(key, out var img) && img != null)
            StartFlash(img, new Color(1f, 0.6f, 0.6f));
    }

    /// <summary>按到 DangerKey — 深红</summary>
    public void FlashDangerKey(KeyCode key)
    {
        if (_keyMap.TryGetValue(key, out var img) && img != null)
            StartFlash(img, new Color(0.8f, 0.2f, 0.2f));
    }

    void StartFlash(Image img, Color flashColor)
    {
        if (_activeFlashes.TryGetValue(img, out var old))
        {
            StopCoroutine(old);
            _activeFlashes.Remove(img);
        }
        _activeFlashes[img] = StartCoroutine(FlashRoutine(img, flashColor));
    }

    System.Collections.IEnumerator FlashRoutine(Image img, Color flashColor)
    {
        img.color = flashColor;
        yield return new WaitForSeconds(0.25f);
        if (!_activeFlashes.ContainsKey(img) || img != _currentHighlight)
            img.color = normalBg;
        _activeFlashes.Remove(img);
    }

    public void ClearHighlight()
    {
        if (_currentHighlight != null)
        {
            _currentHighlight.color = normalBg;
            _currentHighlight = null;
        }
    }

    string KeyToString(KeyCode key) => KeyCodeUtility.ToDisplayString(key);
}
