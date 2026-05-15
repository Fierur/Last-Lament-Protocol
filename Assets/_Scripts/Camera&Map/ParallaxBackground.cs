using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Dependencies")]
    // Drag the MapBounds component of the current map here
    public MapBounds mapBounds;     

    ParallaxLayer[] layers;
    Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        layers  = GetComponentsInChildren<ParallaxLayer>();
    }

    // Invoke from MapManager when the map is activated
    public void Activate()
    {
        foreach (var layer in layers)
            layer.Init(mainCam, mapBounds);

        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        foreach (var layer in layers)
            layer.Tick();
    }
}
