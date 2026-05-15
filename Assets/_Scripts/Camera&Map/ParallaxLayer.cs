using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    // The speed of parallax bg , 0 = fixed, 1 = moves with the camera
    public float parallaxSpeedX = 0.5f; 
    [Range(0f, 1f)]
    public float parallaxSpeedY = 0.2f;

    [Header("Sprite Size (World Space)")]
    // Width and height of the sprite in world units, used for clamping to map bounds
    public float spriteHalfWidth  = 10f;
    public float spriteHalfHeight = 5f;

    Camera mainCam;
    Vector3 startPos;       
    Vector3 startCamPos;   
    // MapBounds.cs
    MapBounds bounds;

    public void Init(Camera cam, MapBounds mapBounds)
    {
        mainCam    = cam;
        bounds     = mapBounds;
        startPos   = transform.position;
        startCamPos = cam.transform.position;
    }

    public void Tick()
    {
        if (mainCam == null || bounds == null) return;

        // Delta camera from the start position
        Vector3 camDelta = mainCam.transform.position - startCamPos;

        // Desired position of the layer
        float targetX = startPos.x + camDelta.x * parallaxSpeedX;
        float targetY = startPos.y + camDelta.y * parallaxSpeedY;

        // Clamp: center of the sprite should not be allowed to show outside the map bounds
        float clampedX = Mathf.Clamp(targetX,
            bounds.minX + spriteHalfWidth,
            bounds.maxX - spriteHalfWidth);

        float clampedY = Mathf.Clamp(targetY,
            bounds.minY + spriteHalfHeight,
            bounds.maxY - spriteHalfHeight);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
