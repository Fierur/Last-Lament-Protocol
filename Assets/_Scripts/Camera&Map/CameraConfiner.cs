using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraConfiner : MonoBehaviour
{
    CinemachineCamera cinemachineCamera;
    CinemachineConfiner2D confiner;
    Camera mainCam;

    Collider2D currentBounds;

    void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();
        mainCam = Camera.main;
    }

    public void SetBounds(Collider2D bounds)
    {
        currentBounds = bounds;

        if (confiner != null)
        {
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateBoundingShapeCache();
        }
    }

    public void HardCutToCurrentZone()
    {
        if (cinemachineCamera != null)
        {
            // Hủy damping / previous camera state ở frame kế tiếp
            cinemachineCamera.PreviousStateIsValid = false;
        }

        ForceClampMainCamera();
    }

    public void ForceClampMainCamera()
    {
        if (mainCam == null || currentBounds == null) return;

        Bounds clampBounds = currentBounds.bounds;

        float camHalfH = mainCam.orthographicSize;
        float camHalfW = mainCam.orthographicSize * mainCam.aspect;

        Vector3 pos = mainCam.transform.position;

        float minX = clampBounds.min.x + camHalfW;
        float maxX = clampBounds.max.x - camHalfW;
        float minY = clampBounds.min.y + camHalfH;
        float maxY = clampBounds.max.y - camHalfH;

        if (minX > maxX)
            pos.x = clampBounds.center.x;
        else
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY)
            pos.y = clampBounds.center.y;
        else
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        mainCam.transform.position = new Vector3(pos.x, pos.y, mainCam.transform.position.z);
    }

    void LateUpdate()
    {
        ForceClampMainCamera();
    }
}