using UnityEngine;

public class MapBounds : MonoBehaviour
{
    [Header("Map Boundary (World Space)")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
