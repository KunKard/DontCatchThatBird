using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 物理键盘 QWERTY 布局网格。供 DangerKey 附近键位选择使用。
/// </summary>
public static class KeyboardGridHelper
{
    static readonly KeyCode[][] KeyboardGrid = new KeyCode[][]
    {
        new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
                KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 },
        new[] { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
                KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P },
        new[] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
                KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L },
        new[] { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B,
                KeyCode.N, KeyCode.M },
    };

    static int[] _gridRow, _gridCol;

    static void EnsureCache()
    {
        if (_gridRow != null) return;
        _gridRow = new int[256];
        _gridCol = new int[256];
        for (int i = 0; i < _gridRow.Length; i++) { _gridRow[i] = -1; _gridCol[i] = -1; }
        for (int r = 0; r < KeyboardGrid.Length; r++)
            for (int c = 0; c < KeyboardGrid[r].Length; c++)
            {
                int idx = (int)KeyboardGrid[r][c];
                if (idx >= 0 && idx < _gridRow.Length) { _gridRow[idx] = r; _gridCol[idx] = c; }
            }
    }

    /// <summary>获取 birdKey 周围曼哈顿距离 ≤2 的所有键</summary>
    public static List<KeyCode> GetNearbyKeys(KeyCode birdKey)
    {
        List<KeyCode> result = new List<KeyCode>();
        EnsureCache();
        int idx = (int)birdKey;
        if (idx < 0 || idx >= _gridRow.Length || _gridRow[idx] < 0) return result;

        int row = _gridRow[idx];
        int col = _gridCol[idx];

        for (int r = 0; r < KeyboardGrid.Length; r++)
        {
            for (int c = 0; c < KeyboardGrid[r].Length; c++)
            {
                int dist = Mathf.Abs(r - row) + Mathf.Abs(c - col);
                if (dist > 0 && dist <= 2)
                    result.Add(KeyboardGrid[r][c]);
            }
        }
        return result;
    }
}
