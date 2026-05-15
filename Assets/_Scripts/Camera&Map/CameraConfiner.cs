using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraConfiner : MonoBehaviour
{
    CinemachineCamera vcam;
    MapBounds currentBounds;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
    }

    // Gọi từ MapManager khi chuyển map
    public void SetBounds(MapBounds bounds)
    {
        currentBounds = bounds;
    }

    void LateUpdate()
    {
        if (currentBounds == null) return;

        // Get half size of ortho camera size
        Camera cam = Camera.main;
        float camHalfH = cam.orthographicSize;
        float camHalfW = cam.orthographicSize * cam.aspect;

        // Clamp the follow target position, avoid the map out of bounds
        Vector3 pos = vcam.transform.position;

        pos.x = Mathf.Clamp(pos.x,
            currentBounds.minX + camHalfW,
            currentBounds.maxX - camHalfW);

        pos.y = Mathf.Clamp(pos.y,
            currentBounds.minY + camHalfH,
            currentBounds.maxY - camHalfH);
            
        // Replace the camera position after Cinemachine calculates the new position
        cam.transform.position = new Vector3(pos.x, pos.y, cam.transform.position.z);
    }
}
