using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 树枝管理。拖入多根树枝，小鸟被抓时随机飞到随机树枝的随机空位。
/// </summary>
public class BranchDisplay : MonoBehaviour
{
    [Header("树枝列表（拖入场景中的树枝 GameObject）")]
    public Transform[] branches;

    int _lastBranchIdx;
    // 每根树枝已占用的槽位（索引 → branchIdx, HashSet<slot>）
    List<HashSet<int>> _occupiedSlots;

    const float BIRD_SPACING = 70f;
    const float BIRD_HEIGHT = 20f;
    const int SLOTS_PER_BRANCH = 7;

    /// <summary>所有树枝已满时 GetNextBranchX 返回此值</summary>
    public const float NO_SLOT = float.MinValue;

    void Start()
    {
        EnsureBirdContainers();
        ResetSlots();
    }

    void EnsureBirdContainers()
    {
        if (branches == null) return;
        for (int i = 0; i < branches.Length; i++)
        {
            if (branches[i] == null) continue;
            Transform existing = branches[i].Find("BirdContainer");
            if (existing != null) continue;

            GameObject c = new GameObject("BirdContainer", typeof(RectTransform));
            c.transform.SetParent(branches[i], false);
            RectTransform crt = c.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.sizeDelta = Vector2.zero;
            crt.anchoredPosition = Vector2.zero;
        }
    }

    void ResetSlots()
    {
        _occupiedSlots = new List<HashSet<int>>();
        if (branches == null) return;
        for (int i = 0; i < branches.Length; i++)
            _occupiedSlots.Add(new HashSet<int>());
    }

    /// <summary>GameStart 时清空所有树枝上的鸟和槽位</summary>
    public void OnGameStart()
    {
        if (branches == null) return;
        for (int i = 0; i < branches.Length; i++)
        {
            if (branches[i] == null) continue;
            Transform c = branches[i].Find("BirdContainer");
            if (c != null)
            {
                for (int j = c.childCount - 1; j >= 0; j--)
                    Destroy(c.GetChild(j).gameObject);
            }
        }
        ResetSlots();
        _lastBranchIdx = 0;
    }

    /// <summary>获取上一只鸟选中的树枝容器</summary>
    public Transform GetBranchContainer()
    {
        if (branches == null || branches.Length == 0) return null;
        if (_lastBranchIdx < 0 || _lastBranchIdx >= branches.Length)
            _lastBranchIdx = Random.Range(0, branches.Length);
        Transform branch = branches[_lastBranchIdx];
        Transform container = branch.Find("BirdContainer");
        if (container == null)
        {
            GameObject c = new GameObject("BirdContainer", typeof(RectTransform));
            c.transform.SetParent(branch, false);
            container = c.transform;
        }
        return container;
    }

    /// <summary>
    /// 获取下一只鸟的位置（随机树枝 + 随机空位）。
    /// 全满时返回 NO_SLOT。
    /// </summary>
    public float GetNextBranchX()
    {
        if (branches == null || branches.Length == 0) return NO_SLOT;

        // 随机顺序遍历树枝
        int[] order = new int[branches.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        for (int i = 0; i < order.Length; i++)
        {
            int r = Random.Range(i, order.Length);
            (order[i], order[r]) = (order[r], order[i]);
        }

        for (int t = 0; t < order.Length; t++)
        {
            int bi = order[t];
            HashSet<int> taken = _occupiedSlots[bi];
            if (taken.Count >= SLOTS_PER_BRANCH) continue;

            // 从该树枝中随机找一个空位
            int slot = -1;
            int[] empty = new int[SLOTS_PER_BRANCH];
            int emptyCount = 0;
            for (int s = 0; s < SLOTS_PER_BRANCH; s++)
                if (!taken.Contains(s)) empty[emptyCount++] = s;
            if (emptyCount > 0) slot = empty[Random.Range(0, emptyCount)];

            if (slot >= 0)
            {
                _lastBranchIdx = bi;
                taken.Add(slot);
                Transform branch = branches[bi];
                RectTransform rt = branch.GetComponent<RectTransform>();
                float localWidth = rt != null ? rt.sizeDelta.x : 640f;
                float scaleX = branch.lossyScale.x;
                float startX = -localWidth * 0.5f + (BIRD_SPACING - 25f) / scaleX;
                return startX + slot * BIRD_SPACING / scaleX;
            }
        }

        return NO_SLOT;
    }
}
