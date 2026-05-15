using UnityEngine;

public class MapDoor : MonoBehaviour
{
    [Header("Door Settings")]
    // Map index to switch to when player enters the door
    public int targetMapIndex;
    // Player spawn point in the target map
    public Transform playerSpawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Move player to spawn point
        other.transform.position = playerSpawnPoint.position;

        // Switch map
        MapManager.Instance.SwitchToMap(targetMapIndex);
    }
}
