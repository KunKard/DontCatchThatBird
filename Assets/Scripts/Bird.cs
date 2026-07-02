using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 小鸟：UI Image + 自动跳跃 + DangerKey 判定 + 飞向树枝动画。
/// 不再直接依赖 GameManager.Instance。
/// </summary>
public class Bird : MonoBehaviour
{
    public KeyCode CurrentKey { get; private set; }
    public float TimeUntilJump { get; private set; }

    public Func<KeyCode[], KeyCode> PickNextKey { get; set; }
    public event Action<Bird, KeyCode> OnJumped; // sender, newKey

    public (float min, float max) JumpInterval { get; set; }

    GameConfigSO _config;
    KeyCode[] _dangerKeys;
    float _jumpTimer;
    RectTransform _rectTransform;
    Image _image;
    bool _isActive;

    Sprite _normalSprite;
    Sprite _flySprite;
    Sprite _standSprite;

    public void Init(KeyCode startKey, KeyCode[] dangerKeys, GameConfigSO config,
        Sprite normalSprite, Sprite flySprite, Sprite standSprite, Color color, bool isActive)
    {
        CurrentKey = startKey;
        _dangerKeys = dangerKeys;
        _config = config;
        _isActive = isActive;

        _normalSprite = normalSprite;
        _flySprite = flySprite;
        _standSprite = standSprite;

        _rectTransform = GetComponent<RectTransform>();
        _rectTransform.sizeDelta = new Vector2(128, 128);
        _rectTransform.localScale = Vector3.one;

        _image = GetComponent<Image>();
        _image.sprite = normalSprite;
        _image.color = color;
        _image.raycastTarget = false;
        _image.preserveAspect = true;

        ResetJumpTimer();
    }

    public void SetActive(bool active) => _isActive = active;

    void Update()
    {
        if (!_isActive || _config == null) return;

        _jumpTimer -= Time.deltaTime;
        TimeUntilJump = Mathf.Max(0, _jumpTimer);

        if (_jumpTimer <= 0f)
            JumpToRandomKey();
    }

    void JumpToRandomKey()
    {
        // 只排除当前键（不排除 DangerKeys，小鸟可能跳到危险键上）
        KeyCode[] excludes = new KeyCode[] { CurrentKey };

        KeyCode newKey = PickNextKey != null ? PickNextKey.Invoke(excludes) : CurrentKey;
        if (newKey == CurrentKey || newKey == KeyCode.None) { ResetJumpTimer(); return; }

        CurrentKey = newKey;
        ResetJumpTimer();

        // 跳到 DangerKey 不触发 Miss
        bool isDanger = false;
        for (int i = 0; i < _dangerKeys.Length; i++)
            if (_dangerKeys[i] == newKey) { isDanger = true; break; }

        if (!isDanger)
            OnJumped?.Invoke(this, newKey);
    }

    void ResetJumpTimer()
    {
        _jumpTimer = UnityEngine.Random.Range(JumpInterval.min, JumpInterval.max);
    }

    public bool IsDangerKey(KeyCode key)
    {
        for (int i = 0; i < _dangerKeys.Length; i++)
            if (_dangerKeys[i] == key) return true;
        return false;
    }

    public void FlyToBranch(Transform branchParent, float localX)
    {
        _isActive = false;
        StopAllCoroutines();
        StartCoroutine(FlyRoutine(branchParent, localX));
    }

    IEnumerator FlyRoutine(Transform branchParent, float localX)
    {
        Vector3 startPos = _rectTransform.position;
        Vector3 targetPos = branchParent.TransformPoint(new Vector3(localX, 20f, 0));

        float t = 0f;
        float duration = 0.6f;
        Vector3 startSize = _rectTransform.sizeDelta;

        // 切换到飞行 Sprite
        if (_flySprite != null) _image.sprite = _flySprite;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float ease = 1f - (1f - p) * (1f - p);
            _rectTransform.position = Vector3.Lerp(startPos, targetPos, ease);
            _rectTransform.sizeDelta = Vector2.Lerp(startSize, new Vector2(80, 80), ease);
            yield return null;
        }

        _rectTransform.position = targetPos;
        _rectTransform.sizeDelta = new Vector2(80, 80);
        _rectTransform.SetParent(branchParent, true);

        // 落地后切换到树枝 Sprite
        if (_standSprite != null) _image.sprite = _standSprite;
    }

    public static Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float radius = size * 0.45f;
        float cx = size * 0.5f, cy = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) < radius
                    ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
