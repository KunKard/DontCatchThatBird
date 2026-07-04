using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 粒子与全屏特效。GameManager 通过 Instance 调用。
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("引用")]
    public Canvas mainCanvas;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (mainCanvas == null) mainCanvas = FindObjectOfType<Canvas>();
    }

    // ========== 抓取粒子 ==========

    public void SpawnCatchParticles(Vector3 position)
    {
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        for (int i = 0; i < 8; i++)
        {
            GameObject p = new GameObject("CatchParticle", typeof(RectTransform));
            p.transform.SetParent(parent, false);
            p.transform.position = position;

            Image img = p.AddComponent<Image>();
            img.color = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);
            img.raycastTarget = false;

            RectTransform rt = p.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(12, 12);

            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(200f, 400f);
            StartCoroutine(ParticleFade(p, dir, speed));
        }
    }

    IEnumerator ParticleFade(GameObject p, Vector2 dir, float speed)
    {
        Image img = p.GetComponent<Image>();
        float t = 0f;
        float duration = Random.Range(0.4f, 0.7f);
        Color startColor = img.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float pct = t / duration;
            p.transform.position += (Vector3)dir * speed * Time.deltaTime;
            p.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, pct);
            img.color = new Color(startColor.r, startColor.g, startColor.b, 1f - pct);
            yield return null;
        }

        Destroy(p);
    }

    // ========== 分数飘出 ==========

    public void SpawnScorePopup(Vector3 position, int scoreGained, int comboLevel)
    {
        Color popColor;
        if (comboLevel >= 15)       popColor = new Color(1f, 0.247f, 0f);  // 红
        else if (comboLevel >= 10)  popColor = new Color(1f, 0.5f, 0f);    // 橙
        else if (comboLevel >= 5)   popColor = new Color(0f, 0.48f, 1f);   // 蓝
        else                        popColor = Color.black;

        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;

        GameObject go = new GameObject("ScorePopup", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.position = position + Vector3.up * 40f + Vector3.right * 30f;

        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = $"+{scoreGained}";
        if (scoreGained >= 75)      tmp.fontSize = 36;
        else if (scoreGained >= 50) tmp.fontSize = 32;
        else if (scoreGained >= 25) tmp.fontSize = 28;
        else                        tmp.fontSize = 24;
        tmp.color = popColor;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.fontStyle = TMPro.FontStyles.Bold;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 40);

        StartCoroutine(ScorePopupFade(go, tmp));
    }

    System.Collections.IEnumerator ScorePopupFade(GameObject go, TMPro.TextMeshProUGUI tmp)
    {
        Vector3 startPos = go.transform.position;
        float t = 0f;
        float duration = 0.75f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float pct = t / duration;
            go.transform.position = startPos + Vector3.up * (40f * pct);
            tmp.alpha = 1f - pct;
            yield return null;
        }

        Destroy(go);
    }

    // ========== 金鸟粒子 ==========

    public void SpawnGoldenParticles(Vector3 position)
    {
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        for (int i = 0; i < 20; i++)
        {
            GameObject p = new GameObject("GoldenParticle", typeof(RectTransform));
            p.transform.SetParent(parent, false);
            p.transform.position = position;

            Image img = p.AddComponent<Image>();
            img.color = Random.value < 0.5f
                ? new Color(1f, 0.84f, 0f)           // 金色
                : new Color(1f, 1f, 0.6f);            // 浅黄
            img.raycastTarget = false;

            RectTransform rt = p.GetComponent<RectTransform>();
            float size = Random.Range(10f, 22f);
            rt.sizeDelta = new Vector2(size, size);

            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(300f, 600f);
            StartCoroutine(ParticleFade(p, dir, speed));
        }
    }

    // ========== 道具拾取粒子 ==========

    public void SpawnPowerUpParticles(Vector3 position, Color color)
    {
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        for (int i = 0; i < 12; i++)
        {
            GameObject p = new GameObject("PowerUpParticle", typeof(RectTransform));
            p.transform.SetParent(parent, false);
            p.transform.position = position;

            Image img = p.AddComponent<Image>();
            float variation = Random.Range(0f, 0.2f);
            img.color = new Color(
                Mathf.Clamp01(color.r + variation),
                Mathf.Clamp01(color.g + variation),
                Mathf.Clamp01(color.b + variation));
            img.raycastTarget = false;

            RectTransform rt = p.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10, 10);

            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(150f, 350f);
            StartCoroutine(ParticleFade(p, dir, speed));
        }
    }

    // ========== Danger 全屏闪烁 ==========

    public void FlashDangerScreen()
    {
        Transform parent = mainCanvas != null ? mainCanvas.transform : transform;
        GameObject go = new GameObject("DangerFlash", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0.4f);
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        StartCoroutine(DangerFlashFade(go));
    }

    IEnumerator DangerFlashFade(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            img.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.4f, 0f, t * 2f));
            yield return null;
        }
        Destroy(go);
    }
}
