using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效管理。Singleton。每种音效有独立字段，拖入 Clip 即可使用。
/// 未拖入 Clip 时自动生成占位 beep 音效。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("配置")]
    public GameConfigSO config;

    [Header("BGM（拖入多个，启动时随机选一个播放）")]
    public AudioClip[] bgmClips;

    [Header("SFX — 拖入音效资源（空则自动生成占位音）")]
    public AudioClip catchClip;
    public AudioClip missClip;
    public AudioClip dangerClip;
    public AudioClip newbestClip;
    public AudioClip jumpClip;

    AudioSource _sfxSource;
    AudioSource _bgmSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;

        // 自动生成占位音效
        GeneratePlaceholders();
    }

    void Start()
    {
        if (bgmClips != null && bgmClips.Length > 0)
        {
            var clip = bgmClips[Random.Range(0, bgmClips.Length)];
            if (clip != null) PlayBGM(clip);
        }
    }

    // ========== 音效 Play 方法 ==========

    public void PlayCatch()   { Play(GetClip(catchClip, 523f, 0.12f)); }   // C5
    public void PlayMiss()    { Play(GetClip(missClip, 220f, 0.2f)); }     // A3
    public void PlayDanger()  { Play(GetClip(dangerClip, 150f, 0.35f)); }  // D3 长警报
    public void PlayNewBest() { Play(GetClip(newbestClip, 784f, 0.3f)); }  // G5 高音庆祝
    public void PlayJump()    { Play(GetClip(jumpClip, 440f, 0.06f)); }    // A4 短弹

    // ========== 通用 ==========

    void Play(AudioClip clip)
    {
        if (_sfxSource == null) return;
        if (clip == null) return;
        _sfxSource.volume = GetSfxVolume();
        _sfxSource.PlayOneShot(clip);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || _bgmSource == null) return;
        _bgmSource.clip = clip;
        _bgmSource.volume = GetBgmVolume();
        _bgmSource.Play();
    }

    public float GetMasterVolume() => config != null ? config.masterVolume : 1f;
    public float GetSfxVolume()   => GetMasterVolume() * (config != null ? config.sfxVolume : 1f);
    public float GetBgmVolume()   => GetMasterVolume() * (config != null ? config.bgmVolume : 1f);

    public void UpdateVolume()
    {
        if (_sfxSource != null) _sfxSource.volume = GetSfxVolume();
        if (_bgmSource != null) _bgmSource.volume = IsDucked ? GetBgmVolume() * 0.3f : GetBgmVolume();
    }

    /// <summary>GameOver 时降低 BGM 音量</summary>
    public bool IsDucked { get; private set; }
    public void DuckBGM(bool duck)
    {
        IsDucked = duck;
        UpdateVolume();
    }

    // ========== 占位音效生成 ==========

    /// <summary>返回已拖入的 clip，否则返回缓存/生成的占位 clip</summary>
    AudioClip GetClip(AudioClip userClip, float freq, float duration)
    {
        if (userClip != null) return userClip;
        string key = $"beep_{freq}_{duration}";
        if (!_placeholderCache.ContainsKey(key))
            _placeholderCache[key] = GenerateSineClip(freq, duration, 0.3f);
        return _placeholderCache[key];
    }

    Dictionary<string, AudioClip> _placeholderCache = new Dictionary<string, AudioClip>();

    AudioClip GenerateSineClip(float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Beep_" + frequency, samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            // 正弦波 + 淡出包络（避免 click 声）
            float envelope = 1f - (float)i / samples;
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
        }
        clip.SetData(data, 0);
        return clip;
    }

    void GeneratePlaceholders()
    {
        // 缓存池懒加载，调用 PlayXxx 时自动生成
        _placeholderCache = new Dictionary<string, AudioClip>();
    }
}
