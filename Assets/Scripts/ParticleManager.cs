using UnityEngine;
using UnityEngine.UI;
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
