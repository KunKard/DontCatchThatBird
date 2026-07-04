using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 游戏状态机。管理输入监听、分数/连击、Miss计数、DangerKey、道具系统。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Ready, Playing, GameOver }
    public enum PowerUpType { Freeze, ResetMiss }

    [System.Serializable]
    public struct BirdSprites { public Sprite normal, fly, stand; }

    [Header("引用")]
    public GameConfigSO config;
    public KeyboardDisplay keyboardDisplay;
    public UIManager uiManager;
    public Canvas mainCanvas;

    [Header("小鸟 Sprite")]
    public BirdSprites birdSprites;

    [Header("道具 Sprite（拖入美术资源，空则几何占位）")]
    public Sprite freezeSprite;
    public Sprite invincibleSprite;

    Sprite _fallbackCircle;

    [Header("运行时状态")]
    public GameState state = GameState.Ready;
    public int score;
    public int combo;
    public int missCount;
    public int highestCombo;
    public int totalCaught;
    int _bestScore;

    public Bird currentBird { get; private set; }

    // Ready
    GameObject _readyBird;
    KeyCode _readyBirdKey;
    KeyCode[] _readyDangerKeys;

    // 道具
    GameObject _powerUpIcon;
    KeyCode _powerUpKey = KeyCode.None;
    PowerUpType _powerUpType;
    float _powerUpTimer;
    int _nextPowerUpCycleStart = 8;

    // 双鸟
    Bird _secondBird;
    float _dualBirdTimer;
    bool _dualBirdFirstCaught;
    bool _dualBirdResolved;

    // 冻结状态
    float _freezeTimer;
    bool _isFrozen;
    GameObject _freezeOverlay;

    // 无敌状态
    float _invincibleTimer;
    bool _isInvincible;
    GameObject _invincibleOverlay;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (config == null) { Debug.LogError("[GameManager] config 未赋值，请在 Inspector 中拖入 GameConfigSO。"); enabled = false; return; }
        if (keyboardDisplay == null) { Debug.LogError("[GameManager] keyboardDisplay 未赋值。"); enabled = false; return; }
        if (uiManager == null) { Debug.LogError("[GameManager] uiManager 未赋值。"); enabled = false; return; }
        _bestScore = PlayerPrefs.GetInt("BestScore", 0);

        // 提醒开发者拖入道具 Sprite
        if (birdSprites.normal == null) Debug.LogWarning("[GameManager] birdSprites.normal 未赋值，使用圆形占位。");
        if (freezeSprite == null) Debug.LogWarning("[GameManager] freezeSprite 未赋值，使用几何占位。");
        if (invincibleSprite == null) Debug.LogWarning("[GameManager] invincibleSprite 未赋值，使用几何占位。");
        if (FindObjectOfType<ParticleManager>() == null)
        {
            var go = new GameObject("ParticleManager", typeof(ParticleManager));
            go.GetComponent<ParticleManager>().mainCanvas = mainCanvas;
        }
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        EnterReady();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            Cursor.visible = true;
        else
            Cursor.visible = false;

        // Tab 切换帮助面板
        if (Input.GetKeyDown(KeyCode.Tab))
            uiManager.ToggleHelpPanel();

        if (state == GameState.Ready)
        {
            if (Input.GetKeyDown(KeyCode.Space) && _readyBird != null)
                StartGame();
            return;
        }

        if (state == GameState.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Restart();
            return;
        }

        // Playing（打开帮助面板时暂停按键输入）
        if (uiManager.IsHelpPanelOpen()) return;

        UpdatePowerUp();
        UpdateFreeze();
        UpdateInvincible();
        UpdateDualBird();

        for (int i = 0; i < config.validKeys.Length; i++)
        {
            if (Input.GetKeyDown(config.validKeys[i]))
                OnKeyPressed(config.validKeys[i]);
        }
    }

    // ========== Ready ==========

    void EnterReady()
    {
        state = GameState.Ready;
        uiManager.OnReady();
        SpawnReadyBird();
    }

    Sprite GetFallbackCircle()
    {
        if (_fallbackCircle == null) _fallbackCircle = Bird.CreateCircleSprite(128);
        return _fallbackCircle;
    }

    void SpawnReadyBird()
    {
        int dangerCount = GetDangerKeyCount();
        _readyBirdKey = PickRandomKeyExcluding(new KeyCode[0]);
        _readyDangerKeys = PickDangerKeysNear(dangerCount, _readyBirdKey);

        bool hasArt = birdSprites.normal != null;
        Sprite sprite = hasArt ? birdSprites.normal : GetFallbackCircle();
        Color color = hasArt ? Color.white : Color.black;

        _readyBird = new GameObject("ReadyBird", typeof(RectTransform));
        _readyBird.transform.SetParent(mainCanvas != null ? mainCanvas.transform : transform, false);

        Image img = _readyBird.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;

        RectTransform rt = _readyBird.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(128, 128);
        rt.anchoredPosition = new Vector2(0, 280);
    }

    // ========== Playing ==========

    void OnKeyPressed(KeyCode key)
    {
        if (state != GameState.Playing || currentBird == null) return;

        // 道具激活
        if (key == _powerUpKey && _powerUpIcon != null)
        {
            ActivatePowerUp();
            return;
        }

        if (currentBird.IsDangerKey(key) || (_secondBird != null && _secondBird.IsDangerKey(key)))
        {
            if (_isInvincible)
            {
                keyboardDisplay.FlashWrongKey(key);
                AudioManager.Instance?.PlayMiss();
                return;
            }
            keyboardDisplay.FlashDangerKey(key);
            AudioManager.Instance?.PlayDanger();
            if (_powerUpIcon != null) RemovePowerUp();
            ParticleManager.Instance?.FlashDangerScreen();
            TriggerGameOver(GameOverReason.DangerKey);
            return;
        }

        if (key == currentBird.CurrentKey)
        {
            keyboardDisplay.ClearHighlight();
            keyboardDisplay.FlashCorrectKey(key);
            AudioManager.Instance?.PlayCatch();
            CatchBird();
        }
        else if (_secondBird != null && key == _secondBird.CurrentKey)
        {
            keyboardDisplay.FlashCorrectKey(key);
            AudioManager.Instance?.PlayCatch();
            CatchSecondBird();
        }
        else
        {
            if (!_isInvincible)
            {
                combo = 0;
                missCount++;
                uiManager.UpdateCombo(combo);
                uiManager.UpdateMiss(missCount);
            }
            keyboardDisplay.FlashWrongKey(key);
            AudioManager.Instance?.PlayMiss();
            if (missCount >= config.maxMissCount)
                TriggerGameOver(GameOverReason.MissLimit);
        }
    }

    void CatchBird()
    {
        bool isGolden = currentBird != null && currentBird.isGolden;

        // 如果 UpdateDualBird 已在该帧处理超时并调用了 SpawnBird，则跳过自己的 SpawnBird
        bool skipSpawn = _dualBirdResolved;

        int scoreGained = config.baseScore + combo * config.comboMultiplier;
        if (isGolden) scoreGained *= 2;
        score += scoreGained;
        combo++;
        missCount = 0;
        totalCaught++;

        if (combo > highestCombo) highestCombo = combo;

        // 双鸟：标记首次是否在窗口内捕获
        if (_secondBird != null && !_dualBirdFirstCaught)
        {
            _dualBirdFirstCaught = true;
            _dualBirdTimer = 0.2f;
        }

        uiManager.UpdateScore(score);
        uiManager.UpdateCombo(combo);
        uiManager.UpdateMiss(missCount);

        if (currentBird != null)
        {
            if (isGolden)
                ParticleManager.Instance?.SpawnGoldenParticles(currentBird.transform.position);
            else
                ParticleManager.Instance?.SpawnCatchParticles(currentBird.transform.position);
            ParticleManager.Instance?.SpawnScorePopup(currentBird.transform.position, scoreGained, combo);
        }

        Bird oldBird = currentBird;

        // 双鸟模式：等两只都抓到或超时才刷新
        bool waitingForSecondBird = !_dualBirdResolved && _secondBird != null && _dualBirdFirstCaught && _dualBirdTimer > 0f;
        if (!waitingForSecondBird && !skipSpawn)
            SpawnBird();

        float branchX = uiManager.GetNextBranchX();
        if (branchX == BranchDisplay.NO_SLOT)
        {
            Destroy(oldBird.gameObject);
        }
        else
        {
            Transform branchContainer = uiManager.GetBranchContainer();
            oldBird.FlyToBranch(branchContainer, branchX);
        }
    }

    void CatchSecondBird()
    {
        int scoreGained = config.baseScore + combo * config.comboMultiplier;

        // 在窗口内 → 三倍分！
        bool inWindow = _dualBirdFirstCaught && _dualBirdTimer > 0f;
        if (inWindow) scoreGained *= 3;

        score += scoreGained;
        totalCaught++;

        uiManager.UpdateScore(score);
        uiManager.UpdateCombo(combo);

        if (_secondBird != null)
        {
            if (inWindow)
                ParticleManager.Instance?.SpawnGoldenParticles(_secondBird.transform.position);
            else
                ParticleManager.Instance?.SpawnCatchParticles(_secondBird.transform.position);
            ParticleManager.Instance?.SpawnScorePopup(_secondBird.transform.position, scoreGained, combo);
        }

        // 抓到第二只 → 刷新鸟
        Transform branchContainer = uiManager.GetBranchContainer();
        float branchX = uiManager.GetNextBranchX();
        if (_secondBird != null && branchX != BranchDisplay.NO_SLOT && branchContainer != null)
            _secondBird.FlyToBranch(branchContainer, branchX);
        else if (_secondBird != null)
            Destroy(_secondBird.gameObject);

        DestroySecondBird();
        SpawnBird();
        _dualBirdResolved = false;
    }

    void UpdateDualBird()
    {
        if (_secondBird == null || _dualBirdResolved) return;
        _dualBirdTimer -= Time.deltaTime;
        if (_dualBirdTimer <= 0f && _dualBirdFirstCaught)
        {
            _dualBirdResolved = true;
            Destroy(_secondBird.gameObject);
            _secondBird = null;
            _dualBirdFirstCaught = false;
            _dualBirdTimer = 0f;
            SpawnBird();
            _dualBirdResolved = false;
        }
    }

    void DestroySecondBird()
    {
        if (_secondBird != null)
        {
            _secondBird.SetActive(false);
            Destroy(_secondBird.gameObject);
            _dualBirdResolved = true;
        }
        _secondBird = null;
        _dualBirdFirstCaught = false;
        _dualBirdTimer = 0f;
    }

    public void SpawnBird()
    {
        int dangerKeyCount = GetDangerKeyCount();

        KeyCode birdKey = PickRandomKeyExcluding(new KeyCode[0]);
        if (birdKey == KeyCode.None) birdKey = config.validKeys[0];
        KeyCode[] dangerKeys = PickDangerKeysNear(dangerKeyCount, birdKey);

        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;

        bool isGolden = combo > 0 && combo % 50 == 0;
        bool hasArt = birdSprites.normal != null;
        Sprite normalSprite = hasArt ? birdSprites.normal : GetFallbackCircle();
        Color birdColor;
        if (isGolden)      birdColor = new Color(1f, 0.84f, 0f); // 金色
        else if (hasArt)   birdColor = Color.white;
        else               birdColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);

        GameObject go = new GameObject("Bird", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.sprite = normalSprite;
        img.color = birdColor;
        img.raycastTarget = false;
        img.preserveAspect = true;
        go.AddComponent<Bird>();

        currentBird = go.GetComponent<Bird>();
        currentBird.isGolden = isGolden;
        currentBird.JumpInterval = GetBirdInterval(score);
        currentBird.Init(birdKey, dangerKeys, config,
            normalSprite, birdSprites.fly, birdSprites.stand, birdColor, isActive: !_isFrozen);
        WireUpBird(currentBird);

        go.transform.position = keyboardDisplay.GetKeyPosition(birdKey);
        keyboardDisplay.HighlightKey(birdKey);
        uiManager.UpdateDangerKeys(dangerKeys);

        // 双鸟：分数 > 1000 时 10% 概率生成第二只鸟
        if (score > 1000 && Random.value < 0.1f)
        {
            KeyCode secondKey = PickRandomKeyExcluding(new[] { birdKey });
            if (secondKey != KeyCode.None)
            {
                // 双鸟跳跃间隔 +0.5s
                var dualInterval = GetBirdInterval(score);
                dualInterval.min += 0.5f;
                dualInterval.max += 0.5f;
                currentBird.JumpInterval = dualInterval;

                GameObject go2 = new GameObject("Bird2", typeof(RectTransform));
                go2.transform.SetParent(parent, false);
                Image img2 = go2.AddComponent<Image>();
                img2.sprite = normalSprite;
                img2.color = birdColor;
                img2.raycastTarget = false;
                img2.preserveAspect = true;
                var b2 = go2.AddComponent<Bird>();
                b2.JumpInterval = dualInterval;
                b2.Init(secondKey, dangerKeys, config, normalSprite, birdSprites.fly, birdSprites.stand, birdColor, isActive: !_isFrozen);
                b2.PickNextKey = excludes => PickRandomKeyExcluding(AddPowerUpExclude(excludes));
                b2.OnJumped += (b, newKey) =>
                {
                    keyboardDisplay.ClearHighlight();
                    b.transform.position = keyboardDisplay.GetKeyPosition(newKey);
                    keyboardDisplay.HighlightKey(newKey);
                    AudioManager.Instance?.PlayJump();
                    if (!_isFrozen) OnBirdJumped();
                };
                go2.transform.position = keyboardDisplay.GetKeyPosition(secondKey);
                _secondBird = b2;
            }
        }
    }

    void WireUpBird(Bird bird)
    {
        bird.PickNextKey = excludes => PickRandomKeyExcluding(AddPowerUpExclude(excludes));
        bird.OnJumped += (b, newKey) =>
        {
            keyboardDisplay.ClearHighlight();
            b.transform.position = keyboardDisplay.GetKeyPosition(newKey);
            keyboardDisplay.HighlightKey(newKey);
            AudioManager.Instance?.PlayJump();
            if (!_isFrozen) OnBirdJumped();
        };
    }

    KeyCode[] AddPowerUpExclude(KeyCode[] excludes)
    {
        if (_powerUpKey == KeyCode.None || _powerUpIcon == null) return excludes;
        var list = new List<KeyCode>(excludes) { _powerUpKey };
        return list.ToArray();
    }

    // ========== PowerUp 系统 ==========

    void UpdatePowerUp()
    {
        // 每 8~12 只触发一次道具
        bool inWindow = totalCaught >= _nextPowerUpCycleStart && totalCaught <= _nextPowerUpCycleStart + 4;
        if (inWindow && _powerUpIcon == null)
        {
            if (Random.value < 0.02f)
                SpawnPowerUp();
        }

        if (totalCaught > _nextPowerUpCycleStart + 4)
            _nextPowerUpCycleStart = totalCaught + 8;

        // 道具超时消失
        if (_powerUpIcon != null)
        {
            _powerUpTimer -= Time.deltaTime;
            if (_powerUpTimer <= 0f)
                RemovePowerUp();
        }
    }

    void SpawnPowerUp()
    {
        _nextPowerUpCycleStart = totalCaught + 8;
        _powerUpType = Random.value < 0.5f ? PowerUpType.Freeze : PowerUpType.ResetMiss;

        // 选随机键（排除鸟键、DangerKeys、已有道具键）
        List<KeyCode> excludes = new List<KeyCode>();
        if (currentBird != null) { excludes.Add(currentBird.CurrentKey); excludes.AddRange(currentBird.GetDangerKeys()); }
        if (_powerUpKey != KeyCode.None) excludes.Add(_powerUpKey);
        _powerUpKey = PickRandomKeyExcluding(excludes.ToArray());
        if (_powerUpKey == KeyCode.None) return;

        // 持续时间：分数越高越短
        float duration;
        if (score < 200)           duration = 2.5f;
        else if (score < 1000)     duration = 2f;
        else if (score < 5000)     duration = 1.5f;
        else                       duration = 1f;
        _powerUpTimer = duration;

        // 创建图标
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        _powerUpIcon = new GameObject("PowerUp", typeof(RectTransform));
        _powerUpIcon.transform.SetParent(parent, false);
        _powerUpIcon.transform.position = keyboardDisplay.GetKeyPosition(_powerUpKey);

        Image img = _powerUpIcon.AddComponent<Image>();
        img.raycastTarget = false;

        RectTransform rt = _powerUpIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        if (_powerUpType == PowerUpType.Freeze)
        {
            img.sprite = freezeSprite != null ? freezeSprite : MakeDiamondSprite();
            img.color = freezeSprite != null ? Color.white : new Color(0.4f, 0.7f, 1f);
        }
        else
        {
            img.sprite = invincibleSprite != null ? invincibleSprite : MakeCircleSprite();
            img.color = invincibleSprite != null ? Color.white : new Color(1f, 0.4f, 0.6f);
        }

        UpdateBirdExclusions();
    }

    void RemovePowerUp()
    {
        if (_powerUpIcon != null) Destroy(_powerUpIcon);
        _powerUpIcon = null;
        _powerUpKey = KeyCode.None;
        UpdateBirdExclusions();
    }

    void ActivatePowerUp()
    {
        AudioManager.Instance?.PlayCatch();
        Vector3 pos = _powerUpIcon != null ? _powerUpIcon.transform.position : Vector3.zero;

        if (_powerUpType == PowerUpType.Freeze)
        {
            _isFrozen = true;
            _freezeTimer = 5f;
            if (currentBird != null) currentBird.SetActive(false);
            if (_secondBird != null) _secondBird.SetActive(false);
            ParticleManager.Instance?.SpawnPowerUpParticles(pos, new Color(0.4f, 0.7f, 1f));
            ShowFreezeOverlay(true);
        }
        else
        {
            _isInvincible = true;
            _invincibleTimer = 4f;
            ShowInvincibleOverlay(true);
            ParticleManager.Instance?.SpawnPowerUpParticles(pos, new Color(1f, 0.5f, 0.7f));
        }

        if (_powerUpIcon != null) Destroy(_powerUpIcon);
        _powerUpIcon = null;
        _powerUpKey = KeyCode.None;
        UpdateBirdExclusions();
    }

    void UpdateFreeze()
    {
        if (!_isFrozen) return;
        _freezeTimer -= Time.deltaTime;
        if (_freezeTimer <= 0f)
        {
            _isFrozen = false;
            if (currentBird != null) currentBird.SetActive(true);
            if (_secondBird != null) _secondBird.SetActive(true);
            ShowFreezeOverlay(false);
        }
    }

    void UpdateInvincible()
    {
        if (!_isInvincible) return;
        _invincibleTimer -= Time.deltaTime;
        if (_invincibleTimer <= 0f)
        {
            _isInvincible = false;
            ShowInvincibleOverlay(false);
        }
    }

    void ShowInvincibleOverlay(bool show)
    {
        if (show)
        {
            if (_invincibleOverlay != null) Destroy(_invincibleOverlay);
            Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
            _invincibleOverlay = new GameObject("InvincibleOverlay", typeof(RectTransform));
            _invincibleOverlay.transform.SetParent(parent, false);
            var img = _invincibleOverlay.AddComponent<Image>();
            img.color = new Color(1f, 0.7f, 0.8f, 0.12f);
            img.raycastTarget = false;
            var rt = _invincibleOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
        else
        {
            if (_invincibleOverlay != null) Destroy(_invincibleOverlay);
            _invincibleOverlay = null;
        }
    }

    void ShowFreezeOverlay(bool show)
    {
        if (show)
        {
            if (_freezeOverlay != null) Destroy(_freezeOverlay);
            Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
            _freezeOverlay = new GameObject("FreezeOverlay", typeof(RectTransform));
            _freezeOverlay.transform.SetParent(parent, false);
            var img = _freezeOverlay.AddComponent<Image>();
            img.color = new Color(0.3f, 0.6f, 1f, 0.15f);
            img.raycastTarget = false;
            var rt = _freezeOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
        else
        {
            if (_freezeOverlay != null) Destroy(_freezeOverlay);
            _freezeOverlay = null;
        }
    }

    void UpdateBirdExclusions()
    {
        if (currentBird != null)
        {
            currentBird.PickNextKey = excludes =>
                PickRandomKeyExcluding(AddPowerUpExclude(excludes));
        }
    }

    // ========== 游戏流程 ==========

    void StartGame()
    {
        AudioManager.Instance?.DuckBGM(false);

        score = 0;
        combo = 0;
        missCount = 0;
        highestCombo = 0;
        totalCaught = 0;
        _isFrozen = false;
        _freezeTimer = 0f;
        ShowFreezeOverlay(false);
        _isInvincible = false;
        _invincibleTimer = 0f;
        ShowInvincibleOverlay(false);

        RemovePowerUp();
        _nextPowerUpCycleStart = totalCaught + 8;
        _dualBirdResolved = false;

        if (currentBird != null && currentBird.gameObject != _readyBird)
            Destroy(currentBird.gameObject);
        DestroySecondBird();

        if (_readyBird != null)
        {
            Image img = _readyBird.GetComponent<Image>();
            if (img != null) img.sprite = birdSprites.normal ?? GetFallbackCircle();

            var bird = _readyBird.AddComponent<Bird>();
            bird.JumpInterval = GetBirdInterval(0);
            bird.Init(_readyBirdKey, _readyDangerKeys, config,
                birdSprites.normal, birdSprites.fly, birdSprites.stand, Color.white, isActive: true);
            WireUpBird(bird);
            currentBird = bird;
            Vector3 targetPos = keyboardDisplay.GetKeyPosition(_readyBirdKey);
            _readyBird = null;

            StartCoroutine(FlyToKeyAndStart(bird, targetPos, _readyBirdKey, img));
        }
        else
        {
            uiManager.OnGameStart();
            uiManager.UpdateBestScore(_bestScore);
            state = GameState.Playing;
            SpawnBird();
        }
    }

    System.Collections.IEnumerator FlyToKeyAndStart(Bird bird, Vector3 targetPos, KeyCode key, Image img)
    {
        Vector3 startPos = bird.transform.position;
        float duration = 0.7f;
        float t = 0f;

        if (birdSprites.fly != null && img != null) img.sprite = birdSprites.fly;

        while (t < duration)
        {
            // 防呆：鸟在飞行中被销毁
            if (bird == null) yield break;

            t += Time.deltaTime;
            float p = t / duration;
            float ease = 1f - (1f - p) * (1f - p);
            bird.transform.position = Vector3.Lerp(startPos, targetPos, ease);
            yield return null;
        }

        if (bird == null) yield break;
        bird.transform.position = targetPos;

        if (birdSprites.normal != null && img != null) img.sprite = birdSprites.normal;

        keyboardDisplay.HighlightKey(key);
        uiManager.OnGameStart();
        uiManager.UpdateBestScore(_bestScore);
        uiManager.UpdateDangerKeys(_readyDangerKeys);
        bird.SetActive(true);
        state = GameState.Playing;
    }

    public void OnBirdJumped()
    {
        if (_isInvincible) return;
        missCount++;
        uiManager.UpdateMiss(missCount);
        if (missCount >= config.maxMissCount)
            TriggerGameOver(GameOverReason.MissLimit);
    }

    public enum GameOverReason { DangerKey, MissLimit }

    public static string ReasonToText(GameOverReason reason) => reason switch
    {
        GameOverReason.DangerKey => "危险键！",
        GameOverReason.MissLimit => "小鸟飞走了！",
        _ => "游戏结束",
    };

    void TriggerGameOver(GameOverReason reason)
    {
        state = GameState.GameOver;
        AudioManager.Instance?.DuckBGM(true);
        keyboardDisplay.ClearHighlight();
        if (_secondBird != null) { Destroy(_secondBird.gameObject); _secondBird = null; }
        if (currentBird != null) { Destroy(currentBird.gameObject); currentBird = null; }
        _dualBirdFirstCaught = false;
        _dualBirdTimer = 0f;
        _dualBirdResolved = false;
        ShowFreezeOverlay(false);
        ShowInvincibleOverlay(false);
        if (score > _bestScore)
        {
            _bestScore = score;
            AudioManager.Instance?.PlayNewBest();
            PlayerPrefs.SetInt("BestScore", _bestScore);
            PlayerPrefs.Save();
        }
        uiManager.ShowGameOverText(reason, score, _bestScore, highestCombo, totalCaught);
    }

    public void Restart()
    {
        StartGame();
    }

    // ========== 工具 ==========

    KeyCode[] PickDangerKeysNear(int count, KeyCode birdKey)
    {
        if (count <= 0) return new KeyCode[0];
        if (count >= config.validKeys.Length) count = config.validKeys.Length - 1;

        KeyCode[] result = new KeyCode[count];
        List<KeyCode> nearbyPool = KeyboardGridHelper.GetNearbyKeys(birdKey);

        for (int d = 0; d < count; d++)
        {
            KeyCode candidate;
            int attempts = 0;
            do
            {
                bool nearby = Random.value < 0.7f && nearbyPool.Count > 0;
                if (nearby)
                    candidate = nearbyPool[Random.Range(0, nearbyPool.Count)];
                else
                    candidate = config.validKeys[Random.Range(0, config.validKeys.Length)];
                attempts++;
            }
            while ((candidate == birdKey || ArrayContains(result, candidate)) && attempts < 50);

            result[d] = candidate;
        }
        return result;
    }

    bool ArrayContains(KeyCode[] arr, KeyCode key)
    {
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == key) return true;
        return false;
    }

    int GetDangerKeyCount()
    {
        for (int i = config.dangerKeyThresholds.Length - 1; i >= 0; i--)
        {
            if (score >= config.dangerKeyThresholds[i].score)
                return config.dangerKeyThresholds[i].count;
        }
        return 1;
    }

    public (float min, float max) GetBirdInterval(int score)
    {
        var levels = config.difficultyLevels;
        var best = levels[0];
        for (int i = 0; i < levels.Length; i++)
            if (score >= levels[i].scoreThreshold) best = levels[i];
        return (best.minInterval, best.maxInterval);
    }

    public KeyCode PickRandomKeyExcluding(KeyCode[] excludes)
    {
        KeyCode[] pool = new KeyCode[config.validKeys.Length];
        int poolSize = 0;
        for (int i = 0; i < config.validKeys.Length; i++)
        {
            bool excluded = false;
            for (int j = 0; j < excludes.Length; j++)
            {
                if (excludes[j] == config.validKeys[i]) { excluded = true; break; }
            }
            if (!excluded) pool[poolSize++] = config.validKeys[i];
        }

        if (poolSize == 0) return config.validKeys[0];
        return pool[Random.Range(0, poolSize)];
    }

    // ========== 道具 Sprite 占位 ==========

    static Sprite _cachedDiamond, _cachedCircle;

    static Sprite MakeDiamondSprite()
    {
        if (_cachedDiamond != null) return _cachedDiamond;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = Mathf.Abs(x - cx) + Mathf.Abs(y - cy) < size * 0.4f ? Color.white : Color.clear;
        tex.SetPixels(px); tex.Apply();
        _cachedDiamond = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedDiamond;
    }

    static Sprite MakeCircleSprite()
    {
        if (_cachedCircle != null) return _cachedCircle;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.42f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) < r ? Color.white : Color.clear;
        tex.SetPixels(px); tex.Apply();
        _cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedCircle;
    }
}
