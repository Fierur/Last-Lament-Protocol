using UnityEngine;

public class MapZone : MonoBehaviour
{
    [Header("Zone Identity")]
    public string zoneName = "Zone";

    [Header("References")]
    public MapBounds       mapBounds;
    public ParallaxBackground parallaxBackground;

    [Header("Transition")]
    public float fadeDuration = 1f;

    [Header("Zone Bounds")]
    [Tooltip("Collider xác định vùng zone — không cần IsTrigger")]
    public Collider2D zoneBounds;

    // Kiểm tra center point của player có nằm trong zone này không
    public bool ContainsPoint(Vector2 point)
    {
        if (zoneBounds == null) return false;
        return zoneBounds.OverlapPoint(point);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        UnityEditor.Handles.Label(transform.position, $"[Zone] {zoneName}");
    }
#endif
}