using UnityEngine;


public class MapBounds : MonoBehaviour
{
    
    public Collider2D BoundingCollider => GetComponent<Collider2D>();
    // Shorthand để ParallaxLayer đọc bounds
    public float minX => BoundingCollider.bounds.min.x;
    public float maxX => BoundingCollider.bounds.max.x;
    public float minY => BoundingCollider.bounds.min.y;
    public float maxY => BoundingCollider.bounds.max.y;

    void OnDrawGizmosSelected()
    {
        var b = GetComponent<PolygonCollider2D>();
        if (b == null) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
