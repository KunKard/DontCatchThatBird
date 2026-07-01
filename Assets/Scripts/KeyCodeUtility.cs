using UnityEngine;

/// <summary>
/// KeyCode 工具方法。供 KeyboardDisplay / UIManager 共用。
/// </summary>
public static class KeyCodeUtility
{
    public static string ToDisplayString(KeyCode key)
    {
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            return ((int)(key - KeyCode.Alpha0)).ToString();
        if (key == KeyCode.Space) return "SPACE";
        return key.ToString();
    }
}
