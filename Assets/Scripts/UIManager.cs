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
        scoreText = MakeTMP("ScoreText", "Score: 0", 28,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -30), new Vector2(260, 40));
        comboText = MakeTMP("ComboText", "Combo: 0", 28,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -75), new Vector2(260, 40));
        dangerText = MakeTMP("DangerText", "Danger: -", 28,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -30), new Vector2(420, 40));
        dangerText.color = Color.red;
        dangerText.alignment = TextAlignmentOptions.TopRight;
        missText = MakeTMP("MissText", "Miss: 0/5", 24,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -75), new Vector2(220, 36));
        missText.alignment = TextAlignmentOptions.TopRight;
        bestScoreText = MakeTMP("BestScoreText", "Best: 0", 24,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(260, 36));
        bestScoreText.alignment = TextAlignmentOptions.Center;
        jumpTimerText = MakeTMP("JumpTimerText", "Next: --", 22,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(220, 36));
        jumpTimerText.alignment = TextAlignmentOptions.Center;

        // 创建初始树枝
        if (branchDisplay == null)
            branchDisplay = FindObjectOfType<BranchDisplay>();
        if (branchDisplay == null)
            Debug.LogWarning("UIManager: 未找到 BranchDisplay，请手动创建 BranchDisplay GameObject 并执行 Create Branch。");
        else
            branchDisplay.CreateEditorBranch();

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
        {
            branchDisplay = FindObjectOfType<BranchDisplay>();
            if (branchDisplay == null)
            {
                GameObject bd = new GameObject("BranchDisplay", typeof(BranchDisplay));
                bd.transform.SetParent(transform, false);
                branchDisplay = bd.GetComponent<BranchDisplay>();
                branchDisplay.mainCanvas = FindObjectOfType<Canvas>();
            }
        }
    }

    void Update()
    {
        if (jumpTimerText == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.state != GameManager.GameState.Playing || gm.currentBird == null)
        { jumpTimerText.text = "Next: --"; return; }
        jumpTimerText.text = $"Next: {gm.currentBird.TimeUntilJump:F1}s";
    }

    // ========== GameStart ==========

    public void OnGameStart()
    {
        SetText(scoreText, "Score: 0");
        SetText(comboText, "Combo: 0");
        SetText(missText, "Miss: 0/5");
        SetColor(missText, Color.black);
        SetText(dangerText, "Danger: -");
        SetText(jumpTimerText, "Next: --");
        ResetComboColor();

        if (branchDisplay != null) branchDisplay.OnGameStart();
    }

    // ========== 数据更新 ==========

    public void UpdateScore(int val) => SetText(scoreText, $"Score: {val}");
    public void UpdateCombo(int val)
    {
        SetText(comboText, $"Combo: {val}");
        if (comboText == null) return;
        if (val >= 5)
        {
            comboText.fontSize = 34;
            comboText.color = new Color(1f, 0.5f, 0f); // 橙色
        }
        else
        {
            comboText.fontSize = 28;
            comboText.color = Color.black;
        }
    }
    public void UpdateMiss(int val)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        SetText(missText, $"Miss: {val}/{gm.config.maxMissCount}");
        SetColor(missText, val >= 4 ? Color.red : Color.black);
    }
    public void UpdateDangerKeys(KeyCode[] keys)
    {
        if (dangerText == null) return;
        if (keys == null || keys.Length == 0) { dangerText.text = "Danger: -"; return; }
        string[] names = new string[keys.Length];
        for (int i = 0; i < keys.Length; i++) names[i] = KeyDisplay(keys[i]);
        dangerText.text = "Danger: " + string.Join(", ", names);
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
        string newBest = score > bestScore ? " [NEW BEST!]" : "";
        SetText(scoreText, $"Score: {score} | Combo: {highestCombo} | Caught: {totalCaught}");
        SetText(comboText, $"{reasonText} — Press [SPACE] to restart{newBest}");
        SetColor(comboText, score > bestScore ? Color.green : Color.red);
        SetText(bestScoreText, $"Best: {bestScore}");
        SetText(dangerText, ""); SetText(missText, ""); SetText(jumpTimerText, "");
    }

    public void UpdateBestScore(int val) => SetText(bestScoreText, $"Best: {val}");
    public void ResetComboColor()
    {
        SetColor(comboText, Color.black);
        if (comboText != null) comboText.fontSize = 28;
    }

    void SetText(TextMeshProUGUI ui, string val) { if (ui != null) ui.text = val; }
    void SetColor(TextMeshProUGUI ui, Color c) { if (ui != null) ui.color = c; }
    string KeyDisplay(KeyCode key) => KeyCodeUtility.ToDisplayString(key);
}
