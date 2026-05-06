using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerEffect : MonoBehaviour
{

    [Header("Dash Effect Settings")]
    public GameObject dashEffectPrefab;
    public Transform dashEffectSpawnPoint;
    public float dashEffectDuration;
    public void PlayDashEffect(float directionX)
    {
        if(dashEffectPrefab == null) return;

        GameObject dashEffect = Instantiate(dashEffectPrefab, dashEffectSpawnPoint.position, Quaternion.identity);

        dashEffect.transform.rotation = directionX > 0 ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

        Destroy(dashEffect, dashEffectDuration);
    }
}
