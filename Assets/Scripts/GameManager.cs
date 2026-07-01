using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏状态机。唯一的大脑：管理输入监听、分数/连击、Miss计数、
/// DangerKey、三种GameOver条件、计时器、重启。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, GameOver }

    [Header("引用")]
    public GameConfigSO config;
    public KeyboardDisplay keyboardDisplay;
    public UIManager uiManager;
    public Canvas mainCanvas;

    // 运行时缓存
    Sprite _birdSprite;

    [Header("运行时状态")]
    public GameState state = GameState.Playing;
    public int score;
    public int combo;
    public int missCount;
    public int highestCombo;
    public int totalCaught;
    int _bestScore;

    public Bird currentBird { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _bestScore = PlayerPrefs.GetInt("BestScore", 0);
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        if (state == GameState.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Restart();
            return;
        }

        // 遍历 37 键输入
        for (int i = 0; i < config.validKeys.Length; i++)
        {
            if (Input.GetKeyDown(config.validKeys[i]))
                OnKeyPressed(config.validKeys[i]);
        }
    }

    void OnKeyPressed(KeyCode key)
    {
        if (state != GameState.Playing || currentBird == null) return;

        // 检查是否 DangerKey
        if (currentBird.IsDangerKey(key))
        {
            keyboardDisplay.FlashDangerKey(key);
            TriggerGameOver(GameOverReason.DangerKey);
            return;
        }

        // 检查是否 BirdKey
        if (key == currentBird.CurrentKey)
        {
            keyboardDisplay.ClearHighlight();           // 先清掉旧黄色高亮
            keyboardDisplay.FlashCorrectKey(key);       // 再闪绿
            CatchBird();
        }
        else
        {
            // 按错键 → Combo 清零 + Miss++
            combo = 0;
            missCount++;
            uiManager.UpdateCombo(combo);
            uiManager.UpdateMiss(missCount);
            keyboardDisplay.FlashWrongKey(key);
            if (missCount >= config.maxMissCount)
                TriggerGameOver(GameOverReason.MissLimit);
        }
    }

    void CatchBird()
    {
        // 计分
        score += config.baseScore + combo * config.comboMultiplier;
        combo++;
        missCount = 0;
        totalCaught++;

        if (combo > highestCombo) highestCombo = combo;

        // UI 更新
        uiManager.UpdateScore(score);
        uiManager.UpdateCombo(combo);
        uiManager.UpdateMiss(missCount);

        // 旧鸟飞向树枝并停留
        Bird oldBird = currentBird;
        SpawnBird();  // 先生成新鸟，旧鸟同时飞走
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
            OnBirdJumped();
        };

        // 定位到对应键上方
        go.transform.position = keyboardDisplay.GetKeyPosition(birdKey);

        keyboardDisplay.HighlightKey(birdKey);
        uiManager.UpdateDangerKeys(dangerKeys);
    }

    KeyCode[] PickDangerKeys(int count)
    {
        if (count <= 0) return new KeyCode[0];
        if (count >= config.validKeys.Length) count = config.validKeys.Length - 1;

        // 从 validKeys 中随机抽 count 个不重复的
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
        // 构建候选人列表（排除的键不加入）
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

        if (poolSize == 0) return config.validKeys[0]; // 不可能全部排除
        return pool[Random.Range(0, poolSize)];
    }

    public void OnBirdJumped()
    {
        missCount++;
        uiManager.UpdateMiss(missCount);
        if (missCount >= config.maxMissCount)
            TriggerGameOver(GameOverReason.MissLimit);
    }

    void StartGame()
    {
        state = GameState.Playing;
        score = 0;
        combo = 0;
        missCount = 0;
        highestCombo = 0;
        totalCaught = 0;

        // 清理旧鸟（避免残留）
        if (currentBird != null)
            Destroy(currentBird.gameObject);

        uiManager.OnGameStart();
        uiManager.UpdateBestScore(_bestScore);
        SpawnBird();
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
        if (currentBird != null) currentBird.SetActive(false);
        if (score > _bestScore)
        {
            _bestScore = score;
            PlayerPrefs.SetInt("BestScore", _bestScore);
            PlayerPrefs.Save();
        }
        uiManager.ShowGameOverText(reason, score, _bestScore, highestCombo, totalCaught);
    }

    public void Restart()
    {
        StartGame();
    }
}
