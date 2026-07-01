using UnityEngine;
using UnityEngine.UI;

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
    public MaterialProvider materialProvider;

    // 运行时缓存
    Sprite _birdSprite;

    [Header("运行时状态")]
    public GameState state = GameState.Ready;
    public int score;
    public int combo;
    public int missCount;
    public int highestCombo;
    public int totalCaught;
    int _bestScore;

    public Bird currentBird { get; private set; }

    // 准备界面的装饰鸟
    GameObject _readyBird;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _bestScore = PlayerPrefs.GetInt("BestScore", 0);

        if (materialProvider == null)
            materialProvider = FindObjectOfType<MaterialProvider>();
        if (materialProvider == null)
        {
            var go = new GameObject("MaterialProvider", typeof(MaterialProvider));
            materialProvider = go.GetComponent<MaterialProvider>();
        }
        materialProvider.Init(config);
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
            if (Input.GetKeyDown(KeyCode.Space))
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

    void SpawnReadyBird()
    {
        if (_birdSprite == null) _birdSprite = Bird.CreateCircleSprite(128);

        _readyBird = new GameObject("ReadyBird", typeof(RectTransform));
        _readyBird.transform.SetParent(mainCanvas != null ? mainCanvas.transform : transform, false);

        Image img = _readyBird.AddComponent<Image>();
        img.sprite = _birdSprite;
        img.color = Color.black;
        img.raycastTarget = false;

        var mat = materialProvider != null ? materialProvider.GetMaterial() : null;
        if (mat != null) img.material = mat;

        // 放在键盘中间上方
        RectTransform rt = _readyBird.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchoredPosition = new Vector2(0, 200);
    }

    // ========== Playing ==========

    void OnKeyPressed(KeyCode key)
    {
        if (state != GameState.Playing || currentBird == null) return;

        if (currentBird.IsDangerKey(key))
        {
            keyboardDisplay.FlashDangerKey(key);
            AudioManager.Instance?.PlayDanger();
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

        Bird oldBird = currentBird;
        SpawnBird();
        oldBird.FlyToBranch(uiManager.GetBranchContainer(), uiManager.GetNextBranchX());
    }

    public void SpawnBird()
    {
        int dangerKeyCount = GetDangerKeyCount();
        KeyCode[] dangerKeys = PickDangerKeys(dangerKeyCount);

        KeyCode birdKey = PickRandomKeyExcluding(dangerKeys);
        if (birdKey == KeyCode.None) birdKey = config.validKeys[0];

        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        Color birdColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);

        if (_birdSprite == null) _birdSprite = Bird.CreateCircleSprite(128);
        GameObject go = new GameObject("Bird", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.sprite = _birdSprite;
        img.color = birdColor;
        img.raycastTarget = false;
        go.AddComponent<Bird>();

        currentBird = go.GetComponent<Bird>();
        currentBird.Init(birdKey, dangerKeys, config, _birdSprite, birdColor, isActive: true);
        currentBird.JumpInterval = GetBirdInterval(score);
        currentBird.PickNextKey = excludes => PickRandomKeyExcluding(excludes);
        currentBird.OnJumped += (bird, newKey) =>
        {
            keyboardDisplay.ClearHighlight();
            bird.transform.position = keyboardDisplay.GetKeyPosition(newKey);
            keyboardDisplay.HighlightKey(newKey);
            AudioManager.Instance?.PlayJump();
            OnBirdJumped();
        };

        var mat = materialProvider != null ? materialProvider.GetMaterial() : null;
        if (mat != null) img.material = mat;

        go.transform.position = keyboardDisplay.GetKeyPosition(birdKey);

        keyboardDisplay.HighlightKey(birdKey);
        uiManager.UpdateDangerKeys(dangerKeys);
    }

    // ========== 游戏流程 ==========

    void StartGame()
    {
        state = GameState.Playing;
        AudioManager.Instance?.DuckBGM(false);

        score = 0;
        combo = 0;
        missCount = 0;
        highestCombo = 0;
        totalCaught = 0;

        // 清理 ready bird
        if (_readyBird != null) Destroy(_readyBird);

        // 清理旧鸟
        if (currentBird != null) Destroy(currentBird.gameObject);

        uiManager.OnGameStart();
        uiManager.UpdateBestScore(_bestScore);
        SpawnBird();
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
        GameOverReason.DangerKey => "Danger Key!",
        GameOverReason.MissLimit => "Bird flew away!",
        _ => "Game Over",
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

    KeyCode[] PickDangerKeys(int count)
    {
        if (count <= 0) return new KeyCode[0];
        if (count >= config.validKeys.Length) count = config.validKeys.Length - 1;

        KeyCode[] pool = (KeyCode[])config.validKeys.Clone();
        for (int i = 0; i < count; i++)
        {
            int r = Random.Range(i, pool.Length);
            (pool[i], pool[r]) = (pool[r], pool[i]);
        }
        KeyCode[] result = new KeyCode[count];
        System.Array.Copy(pool, result, count);
        return result;
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
