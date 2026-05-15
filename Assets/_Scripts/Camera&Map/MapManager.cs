using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Setup")]
    public CameraConfiner cameraConfiner;
    // Drag each map's ParallaxBackground and Mapbounds here in the same order
    public ParallaxBackground[] allParallaxBackgrounds; // Kéo từng map vào theo thứ tự
    public MapBounds[]           allMapBounds;           // Kéo MapBounds từng map theo cùng thứ tự

    int currentMapIndex = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Load map at the beginning, default is map 0
        SwitchToMap(0);
    }

    public void SwitchToMap(int index)
    {
        if (index == currentMapIndex) return;
        if (index < 0 || index >= allMapBounds.Length) return;

        // Turn off old parallax
        if (currentMapIndex >= 0)
            allParallaxBackgrounds[currentMapIndex].Deactivate();

        currentMapIndex = index;

        // Turn on new parallax
        allParallaxBackgrounds[index].Activate();

        // Update camera bounds
        cameraConfiner.SetBounds(allMapBounds[index]);
    }
}
