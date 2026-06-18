using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MapDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public int targetMapIndex;
    public Transform playerSpawnPoint;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        MapManager.Instance.SwitchToMap(targetMapIndex, playerSpawnPoint);
    }
}