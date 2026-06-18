using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public enum ParallaxMode { Clamp, Loop }

    // ── Mode ──────────────────────────────────────────────────────────────────
    [Header("── Mode ──")]
    public ParallaxMode mode = ParallaxMode.Clamp;

    // ── Parallax Speed ────────────────────────────────────────────────────────
    [Header("── Parallax Speed ──")]
    [Tooltip("Clamp: nhân với layerRange để điều chỉnh mức phản ứng (1 = full range, 0.5 = nửa range)\n" +
             "Loop: tốc độ trượt theo camera delta")]
    [Range(0f, 1f)] public float parallaxSpeedX = 0.5f;
    [Range(0f, 1f)] public float parallaxSpeedY = 0f;

    // ── Visibility Threshold ──────────────────────────────────────────────────
    [Header("── Visibility Threshold (Player Y) ──")]
    public bool  useVisibilityThreshold = false;
    public float hideAbovePlayerY       = 20f;
    public float fadeRange              = 3f;

    // ── Clamp Mode ────────────────────────────────────────────────────────────
    [Header("── [Clamp] Settings ──")]
    [Tooltip("Nửa chiều rộng sprite — xác định khoảng di chuyển ngang của layer")]
    public float spriteHalfWidth  = 10f;
    [Tooltip("Nửa chiều cao sprite — xác định khoảng di chuyển dọc của layer")]
    public float spriteHalfHeight = 5f;
    [Tooltip("Cho phép layer vượt quá bounds một đoạn trước khi dừng (che màu nền Unity)")]
    public float boundsOvershoot  = 2f;
    [Tooltip("Dịch chuyển layer theo Y — không ảnh hưởng logic parallax")]
    public float offsetY = 0f;

    

    // ── Loop Mode ─────────────────────────────────────────────────────────────
    [Header("── [Loop] Setup ──")]
    [Tooltip("Số copies — tối thiểu 3")]
    public int cols = 3;
    [Tooltip("Chiều rộng mỗi copy (world units) — đo từ SpriteRenderer.bounds.size.x")]
    public float copyWidth = 25f;
    [Tooltip("Nửa chiều cao sprite — dùng clamp Y trong Loop mode")]
    public float copyHalfHeight = 7f;
    [Tooltip("Cho phép vượt quá bounds Y (che màu nền Unity) trong Loop mode")]
    public float loopBoundsOvershootY = 2f;
    [Tooltip("Dịch chuyển layer theo Y - không ảnh hưởng logic parallax")]
    public float loopOffsetY = 0f;
    [Tooltip("Kéo copies vào đây theo thứ tự trái → phải")]
    
    public Transform[] loopCopies;

    // ── Private ───────────────────────────────────────────────────────────────
    Camera           mainCam;
    Collider2D       boundsCollider;
    Vector3          startPos;      // chỉ dùng cho Loop mode
    Vector3          startCamPos;   // chỉ dùng cho Loop mode
    float            loopStartY;
    float[]          copyStartX;
    SpriteRenderer[] spriteRenderers;
    Transform        playerTransform;

    float minX => boundsCollider.bounds.min.x;
    float maxX => boundsCollider.bounds.max.x;
    float minY => boundsCollider.bounds.min.y;
    float maxY => boundsCollider.bounds.max.y;

    // ── Init ──────────────────────────────────────────────────────────────────
    public void Init(Camera cam, Collider2D mapBoundsCollider)
    {
        mainCam        = cam;
        boundsCollider = mapBoundsCollider;
        startPos       = transform.position;
        startCamPos    = cam.transform.position;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        if (mode == ParallaxMode.Loop && loopCopies != null && loopCopies.Length >= 3)
        {
            copyStartX = new float[loopCopies.Length];
            for (int i = 0; i < loopCopies.Length; i++)
                if (loopCopies[i] != null)
                    copyStartX[i] = loopCopies[i].position.x;

            loopStartY = loopCopies[0].position.y;
        }
    }

    // ── Tick ──────────────────────────────────────────────────────────────────
    public void Tick()
    {
        if (mainCam == null || boundsCollider == null) return;

        if (mode == ParallaxMode.Clamp)
            TickClamp();
        else
            TickLoop();

        if (useVisibilityThreshold)
            UpdateVisibility();
    }

    // ── Clamp Mode ────────────────────────────────────────────────────────────
    // Vị trí layer được map từ tỉ lệ camera trong map sang tỉ lệ tương ứng
    // trong khoảng di chuyển của layer — layer nhỏ hơn map luôn ở đúng
    // tương đối với player (giữa map = giữa layer, về biên = về biên layer)
    void TickClamp()
    {
        float mapW = maxX - minX;
        float mapH = maxY - minY;

        float ratioX = mapW > 0f
            ? Mathf.Clamp01((mainCam.transform.position.x - minX) / mapW)
            : 0.5f;
        float ratioY = mapH > 0f
            ? Mathf.Clamp01((mainCam.transform.position.y - minY) / mapH)
            : 0.5f;

        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        // Dùng mapW/mapH * speed thay vì (mapSize - spriteSize) * speed
        // → Y luôn di chuyển dù sprite lớn hơn map theo chiều đó
        float targetX = centerX + (ratioX - 0.5f) * mapW * parallaxSpeedX;
        float targetY = centerY + (ratioY - 0.5f) * mapH * parallaxSpeedY + offsetY;

        float clampedX = Mathf.Clamp(targetX,
            minX - boundsOvershoot + spriteHalfWidth,
            maxX + boundsOvershoot - spriteHalfWidth);
        float clampedY = Mathf.Clamp(targetY,
            minY - boundsOvershoot + spriteHalfHeight,
            maxY + boundsOvershoot - spriteHalfHeight);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    // ── Loop Mode ─────────────────────────────────────────────────────────────
    void TickLoop()
    {
        if (loopCopies == null || loopCopies.Length < 3 || copyStartX == null) return;

        Vector3 camDelta = mainCam.transform.position - startCamPos;
        float   offsetX  = camDelta.x * parallaxSpeedX;
        float   camX     = mainCam.transform.position.x;
        float   totalWidth = copyWidth * loopCopies.Length;

        // Y: map theo tỉ lệ giống Clamp mode — layer xa di chuyển chậm hơn theo Y
        float mapH    = maxY - minY;
        float ratioY  = mapH > 0f
            ? Mathf.Clamp01((mainCam.transform.position.y - minY) / mapH)
            : 0.5f;
        float layerRangeY = (mapH - copyHalfHeight * 2f) * parallaxSpeedY;
        float centerY     = (minY + maxY) * 0.5f;
        float targetY = centerY + (ratioY - 0.5f) * mapH * parallaxSpeedY + loopOffsetY;
        float clampedY = Mathf.Clamp(targetY,
            minY - loopBoundsOvershootY + copyHalfHeight,
            maxY + loopBoundsOvershootY - copyHalfHeight);

        // X: loop tile — mỗi copy tự snap về slot gần camera nhất
        for (int i = 0; i < loopCopies.Length; i++)
        {
            if (loopCopies[i] == null) continue;

            float idealX   = copyStartX[i] + offsetX;
            float delta    = camX - idealX;
            float snappedX = idealX + Mathf.Round(delta / totalWidth) * totalWidth;

            loopCopies[i].position = new Vector3(snappedX, clampedY, loopCopies[i].position.z);
        }
    }

    // ── Visibility ────────────────────────────────────────────────────────────
    void UpdateVisibility()
    {
        if (playerTransform == null || spriteRenderers == null) return;

        float playerY = playerTransform.position.y;
        float alpha   = fadeRange <= 0f
            ? (playerY <= hideAbovePlayerY ? 1f : 0f)
            : Mathf.InverseLerp(hideAbovePlayerY, hideAbovePlayerY - fadeRange, playerY);

        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color; c.a = alpha; sr.color = c;
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (useVisibilityThreshold)
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
            Gizmos.DrawLine(new Vector3(-1000f, hideAbovePlayerY, 0f),
                            new Vector3( 1000f, hideAbovePlayerY, 0f));
            if (fadeRange > 0f)
            {
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
                Gizmos.DrawLine(new Vector3(-1000f, hideAbovePlayerY - fadeRange, 0f),
                                new Vector3( 1000f, hideAbovePlayerY - fadeRange, 0f));
            }
        }

        if (mode == ParallaxMode.Loop && loopCopies != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
            foreach (var c in loopCopies)
            {
                if (c == null) continue;
                Gizmos.DrawWireCube(c.position,
                    new Vector3(copyWidth, copyHalfHeight * 2f, 0f));
            }
        }
    }
}