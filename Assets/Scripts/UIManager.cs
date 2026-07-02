using UnityEngine;
using TMPro;

/// <summary>
/// UI 管理。右键 → "Create UI" 自动生成文本。
/// 树枝管理委托给 BranchDisplay 组件。
/// </summary>
[ExecuteInEditMode]
public class UIManager : MonoBehaviour
{
    [Header("UI 文本（Create UI 自动填充）")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI dangerText;
    public TextMeshProUGUI missText;
    public TextMeshProUGUI jumpTimerText;
    public TextMeshProUGUI bestScoreText;

    [Header("树枝管理")]
    public BranchDisplay branchDisplay;

    // ========== Editor 一键生成 ==========

    [ContextMenu("Create UI")]
    void CreateUI()
    {
        scoreText = MakeTMP("ScoreText", "得分: 0", 28,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -30), new Vector2(260, 40));
        comboText = MakeTMP("ComboText", "连击: 0", 28,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -75), new Vector2(260, 40));
        dangerText = MakeTMP("DangerText", "危险: -", 28,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -30), new Vector2(420, 40));
        dangerText.color = Color.red;
        dangerText.alignment = TextAlignmentOptions.TopRight;
        missText = MakeTMP("MissText", "失误: 0/5", 24,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -75), new Vector2(220, 36));
        missText.alignment = TextAlignmentOptions.TopRight;
        bestScoreText = MakeTMP("BestScoreText", "最高: 0", 24,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(260, 36));
        bestScoreText.alignment = TextAlignmentOptions.Center;
        jumpTimerText = MakeTMP("JumpTimerText", "下次: --", 22,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(220, 36));

        // 创建初始树枝
        if (branchDisplay == null)
            branchDisplay = FindObjectOfType<BranchDisplay>();
        if (branchDisplay == null)
            Debug.LogWarning("UIManager: 未找到 BranchDisplay，请手动创建 BranchDisplay 并将树枝拖入其 Branches 数组。");

        Debug.Log("UIManager: UI 文本已创建。");
    }

    TextMeshProUGUI MakeTMP(string name, string label, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize; tmp.color = Color.black; tmp.raycastTarget = false;

        var wobble = go.AddComponent<TextWobble>();
        wobble.intensity = 1.5f;
        wobble.speed = 6f;

        return tmp;
    }

    // ========== Runtime ==========

    void Start()
    {
        if (branchDisplay == null)
            branchDisplay = FindObjectOfType<BranchDisplay>();
    }

    void Update()
    {
        if (jumpTimerText == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.state != GameManager.GameState.Playing || gm.currentBird == null) return;
        jumpTimerText.text = $"下次: {gm.currentBird.TimeUntilJump:F1}s";
    }

    // ========== Ready ==========

    public void OnReady()
    {
        SetText(scoreText, "按下小鸟所在的按键抓住它！");
        SetText(comboText, "避开危险键 — 按到立即结束！");
        SetText(missText, "");
        SetText(dangerText, "");
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        SetText(bestScoreText, $"最高: {bestScore}");

        // 用 jumpTimerText 显示开始提示
        if (jumpTimerText != null)
        {
            jumpTimerText.text = "按 [空格键] 开始";
            jumpTimerText.fontSize = 26;
            jumpTimerText.color = Color.black;
        }
    }

    // ========== GameStart ==========

    public void OnGameStart()
    {
        SetText(scoreText, "得分: 0");
        SetText(comboText, "连击: 0");
        SetText(missText, "失误: 0/5");
        SetColor(missText, Color.black);
        SetText(dangerText, "危险: -");
        if (jumpTimerText != null)
        {
            jumpTimerText.text = "下次: --";
            jumpTimerText.fontSize = 22;
        }
        ResetComboColor();

        if (branchDisplay != null) branchDisplay.OnGameStart();
    }

    // ========== 数据更新 ==========

    public void UpdateScore(int val) => SetText(scoreText, $"得分: {val}");
    public void UpdateCombo(int val)
    {
        if (comboText == null) return;
        if (val >= 15)
        {
            comboText.text = $"连击: {val}!!!";
            comboText.fontSize = 38;
            comboText.color = new Color(1f, 0.247f, 0f); // #FF3F00 红
        }
        else if (val >= 10)
        {
            comboText.text = $"连击: {val}!!";
            comboText.fontSize = 36;
            comboText.color = new Color(1f, 0.5f, 0f); // 橙色
        }
        else if (val >= 5)
        {
            comboText.text = $"连击: {val}!";
            comboText.fontSize = 34;
            comboText.color = new Color(0f, 0.48f, 1f); // #007AFF 蓝
        }
        else
        {
            comboText.text = $"连击: {val}";
            comboText.fontSize = 28;
            comboText.color = Color.black;
        }
    }
    public void UpdateMiss(int val)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        SetText(missText, $"失误: {val}/{gm.config.maxMissCount}");
        SetColor(missText, val >= 4 ? Color.red : Color.black);
    }
    public void UpdateDangerKeys(KeyCode[] keys)
    {
        if (dangerText == null) return;
        if (keys == null || keys.Length == 0) { dangerText.text = "危险: -"; return; }
        string[] names = new string[keys.Length];
        for (int i = 0; i < keys.Length; i++) names[i] = KeyDisplay(keys[i]);
        dangerText.text = "危险: " + string.Join(", ", names);
        // 变更时闪黄
        StopCoroutine("FlashDangerText");
        StartCoroutine(FlashDangerText());
    }

    System.Collections.IEnumerator FlashDangerText()
    {
        if (dangerText == null) yield break;
        Color original = dangerText.color;
        dangerText.color = Color.yellow;
        yield return new WaitForSeconds(0.2f);
        if (dangerText != null) dangerText.color = Color.red;
    }

    /// <summary>委托给 BranchDisplay</summary>
    public Transform GetBranchContainer()
    {
        if (branchDisplay == null)
        {
            branchDisplay = FindObjectOfType<BranchDisplay>();
            if (branchDisplay == null)
            {
                Debug.LogError("UIManager: BranchDisplay 未找到！请确保场景中存在 BranchDisplay 组件。");
                return null;
            }
        }
        return branchDisplay.GetBranchContainer();
    }

    /// <summary>委托给 BranchDisplay</summary>
    public float GetNextBranchX()
    {
        if (branchDisplay == null)
        {
            branchDisplay = FindObjectOfType<BranchDisplay>();
            if (branchDisplay == null)
            {
                Debug.LogError("UIManager: BranchDisplay 未找到！");
                return 0f;
            }
        }
        return branchDisplay.GetNextBranchX();
    }

    // ========== GameOver ==========

    public void ShowGameOverText(GameManager.GameOverReason reason, int score, int bestScore, int highestCombo, int totalCaught)
    {
        string reasonText = GameManager.ReasonToText(reason);
        string newBest = score > bestScore ? " [新纪录！]" : "";
        SetText(scoreText, $"得分: {score} | 最高连击: {highestCombo} | 抓到: {totalCaught} 只");
        SetText(comboText, $"{reasonText} — 按 [空格] 重开{newBest}");
        SetColor(comboText, score > bestScore ? Color.green : Color.red);
        SetText(bestScoreText, $"最高: {bestScore}");
        SetText(dangerText, ""); SetText(missText, ""); SetText(jumpTimerText, "");
    }

    public void UpdateBestScore(int val) => SetText(bestScoreText, $"最高: {val}");
    public void ResetComboColor()
    {
        if (comboText == null) return;
        comboText.text = "连击: 0";
        comboText.fontSize = 28;
        comboText.color = Color.black;
    }

    void SetText(TextMeshProUGUI ui, string val) { if (ui != null) ui.text = val; }
    void SetColor(TextMeshProUGUI ui, Color c) { if (ui != null) ui.color = c; }
    string KeyDisplay(KeyCode key) => KeyCodeUtility.ToDisplayString(key);
}
