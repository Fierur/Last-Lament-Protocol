using System.Collections;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("References")]
    public CameraConfiner cameraConfiner;

    [Header("Map Setup")]
    public MapEntry[] maps;

    [Header("Zone Setup")]
    public MapZone[] allZones;
    public MapZone startZone;

    [Header("Zone Detection")]
    [Tooltip("Bao nhiêu frame check 1 lần — 1 = mỗi frame, 3 = cứ 3 frame check 1 lần")]
    public int zoneCheckInterval = 2;

    // State
    int currentMapIndex = -1;
    MapZone currentZone;
    bool isTransitioning;

    // Player ref
    Transform playerTransform;
    Collider2D playerCollider;
    int frameCounter;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (var zone in allZones)
        {
            if (zone != null && zone.parallaxBackground != null)
                zone.parallaxBackground.Deactivate();
        }
    }

    void Start()
    {
        CachePlayer();

        if (startZone != null)
            ActivateZoneImmediate(startZone);

        if (maps != null && maps.Length > 0)
            SwitchToMap(0);
    }

    void Update()
    {
        frameCounter++;
        if (frameCounter < zoneCheckInterval) return;
        frameCounter = 0;

        CheckPlayerZone();
    }

    void CachePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCollider = player.GetComponent<Collider2D>();
        }
    }

    // ── Zone Detection ────────────────────────────────────────────────────────
    void CheckPlayerZone()
    {
        if (playerTransform == null || isTransitioning) return;

        Vector2 playerCenter = playerCollider != null
            ? playerCollider.bounds.center
            : (Vector2)playerTransform.position;

        MapZone detectedZone = null;

        foreach (var zone in allZones)
        {
            if (zone == null) continue;

            if (zone.ContainsPoint(playerCenter))
            {
                detectedZone = zone;
                break;
            }
        }

        if (detectedZone == null || detectedZone == currentZone) return;

        SwitchZone(detectedZone);
    }

    // ── Zone Transition trong cùng map ────────────────────────────────────────
    public void SwitchZone(MapZone newZone)
    {
        if (newZone == currentZone || isTransitioning) return;

        StartCoroutine(ZoneTransitionRoutine(newZone));
    }

    IEnumerator ZoneTransitionRoutine(MapZone newZone)
    {
        isTransitioning = true;

        MapZone oldZone = currentZone;
        currentZone = newZone;

        // Bật parallax zone mới ngay
        if (newZone.parallaxBackground != null)
        {
            newZone.parallaxBackground.Activate();
            newZone.parallaxBackground.SetAlphaImmediate(1f);
        }

        // Đổi bounds ngay, không blend, đồng thời hủy damping của Cinemachine
        if (newZone.mapBounds != null && cameraConfiner != null)
        {
            cameraConfiner.SetBounds(newZone.mapBounds.BoundingCollider);
            cameraConfiner.HardCutToCurrentZone();
        }

        // Tắt parallax zone cũ sau cùng
        if (oldZone != null &&
            oldZone != newZone &&
            oldZone.parallaxBackground != null)
        {
            oldZone.parallaxBackground.Deactivate();
        }

        isTransitioning = false;

        yield break;
    }

    void ActivateZoneImmediate(MapZone zone)
    {
        currentZone = zone;

        if (zone.parallaxBackground != null)
        {
            zone.parallaxBackground.Activate();
            zone.parallaxBackground.SetAlphaImmediate(1f);
        }

        if (zone.mapBounds != null && cameraConfiner != null)
        {
            cameraConfiner.SetBounds(zone.mapBounds.BoundingCollider);
            cameraConfiner.ForceClampMainCamera();
        }
    }

    // ── Map API: dùng khi start hoặc gọi cũ ──────────────────────────────────
    public void SwitchToMap(int index)
    {
        if (maps == null || index < 0 || index >= maps.Length) return;
        if (index == currentMapIndex) return;

        if (currentMapIndex >= 0 && maps[currentMapIndex].rootObject != null)
            maps[currentMapIndex].rootObject.SetActive(false);

        currentMapIndex = index;

        if (maps[index].rootObject != null)
            maps[index].rootObject.SetActive(true);

        if (maps[index].startZone != null)
            ActivateZoneImmediate(maps[index].startZone);
    }

    // ── Map API: dùng cho MapDoor teleport sang map khác ─────────────────────
    public void SwitchToMap(int index, Transform spawnPoint)
    {
        if (maps == null || index < 0 || index >= maps.Length) return;
        if (spawnPoint == null) return;

        if (playerTransform == null)
            CachePlayer();

        if (playerTransform == null) return;

        isTransitioning = true;

        Vector3 oldPlayerPos = playerTransform.position;

        // Tắt map cũ
        if (currentMapIndex >= 0 && maps[currentMapIndex].rootObject != null)
            maps[currentMapIndex].rootObject.SetActive(false);

        currentMapIndex = index;

        // Bật map mới
        if (maps[index].rootObject != null)
            maps[index].rootObject.SetActive(true);

        // Bật zone/parallax mới + set camera bounds mới trước
        if (maps[index].startZone != null)
            ActivateZoneImmediate(maps[index].startZone);

        // Teleport player sau khi map mới đã active
        playerTransform.position = spawnPoint.position;

        Vector3 delta = playerTransform.position - oldPlayerPos;

        // Báo Cinemachine target vừa warp để damping không kéo camera từ vị trí cũ qua mới
        if (cameraConfiner != null)
        {
            cameraConfiner.ForceClampMainCamera();
        }

        isTransitioning = false;
    }
}

[System.Serializable]
public class MapEntry
{
    public GameObject rootObject;
    public MapZone startZone;
}