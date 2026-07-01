using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 树枝管理：生成、溢出、鸟定位。独立于 UIManager。
/// </summary>
public class BranchDisplay : MonoBehaviour
{
    [Header("树枝")]
    public GameObject branchPrefab;
    public Canvas mainCanvas;

    class BranchInfo
    {
        public GameObject gameObject;
        public RectTransform rect;
        public Transform birdContainer;
        public int birdCount;
    }
    List<BranchInfo> _branches = new List<BranchInfo>();
    int _currentBranchIdx;

    const float BIRD_SPACING = 42f;
    const float BRANCH_Y_BASE = 40f;
    const float BRANCH_Y_OFFSET = 22f;

    // ========== Editor 一键创建 ==========

    [ContextMenu("Create Branch")]
    public void CreateEditorBranch()
    {
        // 清理旧树枝
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c.name.StartsWith("Branch_") || c.name == "Branch")
                DestroyImmediate(c.gameObject);
        }
        _branches.Clear();
        SpawnBranchInternal();
    }

    void Start()
    {
        if (mainCanvas == null) mainCanvas = FindObjectOfType<Canvas>();
        ScanForExistingBranches();
        if (_branches.Count == 0) SpawnBranchInternal();
        ApplyDoodleToAllBranches();
    }

    void ApplyDoodleToAllBranches()
    {
        if (MaterialProvider.Instance == null) return;
        var mat = MaterialProvider.Instance.GetMaterial();
        if (mat == null) return;
        for (int i = 0; i < _branches.Count; i++)
        {
            var img = _branches[i].gameObject.GetComponent<Image>();
            if (img != null) img.material = mat;
        }
    }

    // ========== 公共接口 ==========

    /// <summary>GameStart 时调用：清空鸟，仅保留第一根树枝</summary>
    public void OnGameStart()
    {
        for (int i = _branches.Count - 1; i >= 1; i--)
        {
            Destroy(_branches[i].gameObject);
            _branches.RemoveAt(i);
        }
        if (_branches.Count > 0)
        {
            Transform c = _branches[0].birdContainer;
            if (c != null)
                for (int j = c.childCount - 1; j >= 0; j--)
                    Destroy(c.GetChild(j).gameObject);
            _branches[0].birdCount = 0;
        }
        _currentBranchIdx = 0;
    }

    /// <summary>获取当前树枝的鸟容器</summary>
    public Transform GetBranchContainer()
    {
        if (_branches.Count == 0) SpawnBranchInternal();
        return _branches[_currentBranchIdx].birdContainer;
    }

    /// <summary>获取下一只鸟在树枝上的本地 X 偏移（自动处理满枝溢出）</summary>
    public float GetNextBranchX()
    {
        if (_branches.Count == 0) SpawnBranchInternal();

        var gm = GameManager.Instance;
        int capacity = gm != null ? gm.config.branchCapacity : 10;
        int maxBranches = gm != null ? gm.config.maxBranchCount : 3;

        BranchInfo cur = _branches[_currentBranchIdx];

        if (cur.birdCount >= capacity && _branches.Count < maxBranches)
        {
            SpawnBranchInternal();
            cur = _branches[_currentBranchIdx];
        }

        float branchWidth = cur.rect.sizeDelta.x;
        float startX = -branchWidth * 0.5f + BIRD_SPACING;
        int index = cur.birdCount;
        cur.birdCount++;
        return startX + index * BIRD_SPACING;
    }

    // ========== 内部 ==========

    void ScanForExistingBranches()
    {
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        List<Transform> found = new List<Transform>();
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith("Branch_") || child.name == "Branch")
                found.Add(child);
        }
        if (found.Count == 0) return;

        found.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));

        _branches.Clear();
        for (int i = 0; i < found.Count; i++)
        {
            Transform t = found[i];
            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt == null) continue;

            Transform container = t.Find("BirdContainer");
            if (container == null)
            {
                GameObject c = new GameObject("BirdContainer", typeof(RectTransform));
                c.transform.SetParent(t, false);
                RectTransform crt = c.GetComponent<RectTransform>();
                crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.sizeDelta = Vector2.zero;
                container = c.transform;
            }

            _branches.Add(new BranchInfo
            {
                gameObject = t.gameObject,
                rect = rt,
                birdContainer = container,
                birdCount = 0,
            });
        }
        _currentBranchIdx = 0;
    }

    /// <summary>实例化一根新树枝</summary>
    BranchInfo SpawnBranchInternal()
    {
        Transform canvasT = mainCanvas != null ? mainCanvas.transform : transform;
        GameObject go;

        if (branchPrefab != null)
        {
            go = Instantiate(branchPrefab, canvasT);
            go.name = "Branch_" + _branches.Count;
        }
        else
        {
            go = new GameObject("Branch_" + _branches.Count, typeof(RectTransform));
            go.transform.SetParent(canvasT, false);
            Image img = go.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            img.sprite = GetBranchSprite();
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, BRANCH_Y_BASE + _branches.Count * BRANCH_Y_OFFSET);
        if (branchPrefab == null) rt.sizeDelta = new Vector2(640, 14);

        go.transform.SetAsLastSibling();

        // 应用 Doodle 材质（树枝描边）
        Image branchImg = go.GetComponent<Image>();
        if (MaterialProvider.Instance != null && branchImg != null)
            branchImg.material = MaterialProvider.Instance.GetMaterial();

        Transform container = go.transform.Find("BirdContainer");
        if (container == null)
        {
            GameObject c = new GameObject("BirdContainer", typeof(RectTransform));
            c.transform.SetParent(go.transform, false);
            RectTransform crt = c.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.sizeDelta = Vector2.zero;
            container = c.transform;
        }

        BranchInfo info = new BranchInfo { gameObject = go, rect = rt, birdContainer = container, birdCount = 0 };
        _branches.Add(info);
        _currentBranchIdx = _branches.Count - 1;
        return info;
    }

    static int ExtractIndex(string name)
    {
        int underscore = name.IndexOf('_');
        if (underscore < 0) return 0;
        int.TryParse(name.Substring(underscore + 1), out int idx);
        return idx;
    }

    // ========== 树枝 Sprite 生成 ==========

    static Sprite _branchSprite;
    static Sprite GetBranchSprite()
    {
        if (_branchSprite != null) return _branchSprite;
        int margin = 4;
        Texture2D tex = new Texture2D(640 + margin * 2, 14 + margin * 2, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[tex.width * tex.height];
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                bool edge = x < margin || x >= tex.width - margin || y < margin || y >= tex.height - margin;
                pixels[y * tex.width + x] = edge ? Color.clear : Color.white;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _branchSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return _branchSprite;
    }
}
