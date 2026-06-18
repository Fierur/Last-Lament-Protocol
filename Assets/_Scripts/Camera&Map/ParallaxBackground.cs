using System.Collections;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Dependencies")]
    public MapBounds mapBounds;

    ParallaxLayer[] layers;
    Camera          mainCam;

    // Cache tất cả SpriteRenderer trong children để fade
    SpriteRenderer[] allRenderers;

    void Awake()
    {
        mainCam  = Camera.main;
        layers   = GetDirectChildLayers();
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // ── Activate / Deactivate ─────────────────────────────────────────────────
    public void Activate()
    {
        gameObject.SetActive(true);
        foreach (var layer in layers)
        {
            layer.gameObject.SetActive(true);
            layer.Init(mainCam, mapBounds.BoundingCollider);
        }
        SetAlpha(1f);
    }

    public void Deactivate()
    {
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    //  Crossfade 
    // Gọi từ MapManager - fade alpha từ giá trị hiện tại → target trong duration giây
    public Coroutine FadeTo(float targetAlpha, float duration)
    {
        return StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = GetCurrentAlpha();
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            // Smoothstep để fade mượt hơn linear
            float smooth = t * t * (3f - 2f * t);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, smooth));
            yield return null;
        }

        SetAlpha(targetAlpha);

        // Tắt hẳn nếu fade về 0 - tiết kiệm draw call
        if (targetAlpha <= 0f)
            gameObject.SetActive(false);
    }

    // Alpha helpers
    void SetAlpha(float alpha)
    {
        if (allRenderers == null) return;
        foreach (var sr in allRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    
    float GetCurrentAlpha()
    {
        if (allRenderers == null || allRenderers.Length == 0) return 1f;
        foreach (var sr in allRenderers)
            if (sr != null) return sr.color.a;
        return 1f;
    }

    // Tick 
    void LateUpdate()
    {
        foreach (var layer in layers)
            layer.Tick();
    }

    public void SetAlphaImmediate(float alpha)
    {
        // Refresh cache phòng trường hợp children thay đổi
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        SetAlpha(alpha);
    }

    // Helpers
    ParallaxLayer[] GetDirectChildLayers()
    {
        var result = new System.Collections.Generic.List<ParallaxLayer>();
        foreach (Transform child in transform)
        {
            var layer = child.GetComponent<ParallaxLayer>();
            if (layer != null) result.Add(layer);
        }
        return result.ToArray();
    }
}