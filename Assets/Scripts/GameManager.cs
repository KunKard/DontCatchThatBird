using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 游戏状态机。唯一的大脑：管理输入监听、分数/连击、Miss计数、
/// DangerKey、三种GameOver条件、重启。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Ready, Playing, GameOver }

    [Header("引用")]
    public GameConfigSO config;
    public KeyboardDisplay keyboardDisplay;
    public UIManager uiManager;
    public Canvas mainCanvas;

    [Header("小鸟 Sprite（拖入美术资源）")]
    public BirdSprites birdSprites;

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

    // 准备界面的装饰鸟（按空格后直接变第一只游戏鸟）
    GameObject _readyBird;
    KeyCode _readyBirdKey;
    KeyCode[] _readyDangerKeys;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _bestScore = PlayerPrefs.GetInt("BestScore", 0);

        // 自动创建 ParticleManager
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
        // Alt 键按住时显示鼠标
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            Cursor.visible = true;
        else
            Cursor.visible = false;

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

        // Playing: 遍历 37 键输入
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
        // 预选第一只鸟的键位和 DangerKeys
        int dangerCount = GetDangerKeyCount();
        _readyBirdKey = PickRandomKeyExcluding(new KeyCode[0]);
        _readyDangerKeys = PickDangerKeysNear(dangerCount, _readyBirdKey);
        if (_readyBirdKey == KeyCode.None) _readyBirdKey = config.validKeys[0];

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

        // 放在键盘中间上方
        RectTransform rt = _readyBird.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(128, 128);
        rt.anchoredPosition = new Vector2(0, 280);
    }

    // ========== Playing ==========

    void OnKeyPressed(KeyCode key)
    {
        if (state != GameState.Playing || currentBird == null) return;

        if (currentBird.IsDangerKey(key))
        {
            keyboardDisplay.FlashDangerKey(key);
            AudioManager.Instance?.PlayDanger();
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
        else
        {
            combo = 0;
            missCount++;
            uiManager.UpdateCombo(combo);
            uiManager.UpdateMiss(missCount);
            keyboardDisplay.FlashWrongKey(key);
            AudioManager.Instance?.PlayMiss();
            if (missCount >= config.maxMissCount)
                TriggerGameOver(GameOverReason.MissLimit);
        }
    }

    void CatchBird()
    {
        score += config.baseScore + combo * config.comboMultiplier;
        combo++;
        missCount = 0;
        totalCaught++;

        if (combo > highestCombo) highestCombo = combo;

        uiManager.UpdateScore(score);
        uiManager.UpdateCombo(combo);
        uiManager.UpdateMiss(missCount);

        if (currentBird != null)
            ParticleManager.Instance?.SpawnCatchParticles(currentBird.transform.position);

        Bird oldBird = currentBird;
        SpawnBird();
        float branchX = uiManager.GetNextBranchX();
        if (branchX == BranchDisplay.NO_SLOT)
        {
            Destroy(oldBird.gameObject); // 树枝已满，鸟消失
        }
        else
        {
            Transform branchContainer = uiManager.GetBranchContainer();
            oldBird.FlyToBranch(branchContainer, branchX);
        }
    }

    public void SpawnBird()
    {
        int dangerKeyCount = GetDangerKeyCount();

        // 先选鸟键，再围绕鸟键选 DangerKeys
        KeyCode birdKey = PickRandomKeyExcluding(new KeyCode[0]);
        if (birdKey == KeyCode.None) birdKey = config.validKeys[0];
        KeyCode[] dangerKeys = PickDangerKeysNear(dangerKeyCount, birdKey);

        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;

        // 使用美术 Sprite（有拖入时），否则 fallback 圆形
        bool hasArt = birdSprites.normal != null;
        Sprite normalSprite = hasArt ? birdSprites.normal : GetFallbackCircle();
        Color birdColor = hasArt ? Color.white : Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);

        GameObject go = new GameObject("Bird", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.sprite = normalSprite;
        img.color = birdColor;
        img.raycastTarget = false;
        img.preserveAspect = true;
        go.AddComponent<Bird>();

        currentBird = go.GetComponent<Bird>();
        currentBird.JumpInterval = GetBirdInterval(score);
        currentBird.Init(birdKey, dangerKeys, config,
            normalSprite, birdSprites.fly, birdSprites.stand, birdColor, isActive: true);
        WireUpBird(currentBird);

        go.transform.position = keyboardDisplay.GetKeyPosition(birdKey);

        keyboardDisplay.HighlightKey(birdKey);
        uiManager.UpdateDangerKeys(dangerKeys);
    }

    void WireUpBird(Bird bird)
    {
        bird.PickNextKey = excludes => PickRandomKeyExcluding(excludes);
        bird.OnJumped += (b, newKey) =>
        {
            keyboardDisplay.ClearHighlight();
            b.transform.position = keyboardDisplay.GetKeyPosition(newKey);
            keyboardDisplay.HighlightKey(newKey);
            AudioManager.Instance?.PlayJump();
            OnBirdJumped();
        };
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

        if (currentBird != null && currentBird.gameObject != _readyBird)
            Destroy(currentBird.gameObject);

        if (_readyBird != null)
        {
            Image img = _readyBird.GetComponent<Image>();
            if (img != null) img.sprite = birdSprites.normal ?? GetFallbackCircle();

            var bird = _readyBird.AddComponent<Bird>();
            bird.JumpInterval = GetBirdInterval(0);
            bird.Init(_readyBirdKey, _readyDangerKeys, config,
                birdSprites.normal, birdSprites.fly, birdSprites.stand, Color.white, isActive: false);
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
            t += Time.deltaTime;
            float p = t / duration;
            float ease = 1f - (1f - p) * (1f - p);
            bird.transform.position = Vector3.Lerp(startPos, targetPos, ease);
            yield return null;
        }

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
        if (currentBird != null) currentBird.SetActive(false);
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

    /// <summary>围绕 birdKey 选 DangerKeys（物理键盘四周的键）</summary>
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
}

[System.Serializable]
public struct BirdSprites
{
    public Sprite normal;   // 键盘上正常
    public Sprite fly;      // 被抓时飞起
    public Sprite stand;    // 树枝上停留
}

