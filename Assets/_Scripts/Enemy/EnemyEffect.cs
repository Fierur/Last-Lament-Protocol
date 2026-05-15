using UnityEngine;

public class EnemyEffect : MonoBehaviour
{
    [Header("Spell Effect Settings")]
    public GameObject spellEffectPrefab;
    public Transform spellSpawnPoint;   
    public float spellEffectDuration;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Invoke from Animation Event in attack cast spell clip
    public void PlaySpellEffect()
    {
        if (spellEffectPrefab == null) return;

        GameObject spellEffect = Instantiate(
            spellEffectPrefab,
            spellSpawnPoint.position,
            Quaternion.identity);

        Destroy(spellEffect, spellEffectDuration);
    }
    
}
