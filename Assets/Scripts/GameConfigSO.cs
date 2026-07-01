using UnityEngine;

/// <summary>
/// 所有可调参数集中配置。数据驱动，不硬编码。
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Don't Catch That Bird/GameConfig")]
public class GameConfigSO : ScriptableObject
{
    [Header("小鸟移动 — 难度曲线")]
    public DifficultyLevel[] difficultyLevels = new DifficultyLevel[]
    {
        new DifficultyLevel { scoreThreshold = 0,   minInterval = 2.0f, maxInterval = 2.5f },
        new DifficultyLevel { scoreThreshold = 50,  minInterval = 1.5f, maxInterval = 2.0f },
        new DifficultyLevel { scoreThreshold = 100, minInterval = 1.0f, maxInterval = 1.5f },
        new DifficultyLevel { scoreThreshold = 200, minInterval = 0.6f, maxInterval = 1.0f },
    };

    [Header("分数")]
    public int baseScore = 10;
    public int comboMultiplier = 5;

    [Header("Miss 机制")]
    public int maxMissCount = 5;

    [Header("DangerKey 阈值（按 Score 递增）")]
    public DangerKeyThreshold[] dangerKeyThresholds = new DangerKeyThreshold[]
    {
        new DangerKeyThreshold { score = 0,   count = 1 },
        new DangerKeyThreshold { score = 100, count = 2 },
        new DangerKeyThreshold { score = 200, count = 3 },
    };

    [Header("树枝")]
    public int branchCapacity = 10;
    public int maxBranchCount = 3;

    [Header("键盘布局")]
    public KeyCode[] validKeys = new KeyCode[]
    {
        // 26 字母
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y,
        KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H,
        KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
        // 10 数字
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        // 空格
        KeyCode.Space,
    };

}

[System.Serializable]
public class DangerKeyThreshold
{
    public int score;
    public int count;
}

[System.Serializable]
public class DifficultyLevel
{
    public int scoreThreshold;
    public float minInterval;
    public float maxInterval;
}
