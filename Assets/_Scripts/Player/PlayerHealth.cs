using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Heath Settings")]
    public int maxHealth;
    int currentHealth;
    public float invincibleDuration;
    bool isInvincible;
    PlayerController pc;
    Animator anim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pc = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region Take Damage
    public void TakeDamage(int damage)
    {
        if(isInvincible || currentHealth <= 0) return;

        currentHealth -= damage;
        if(currentHealth <=0)
        {
            currentHealth = 0;
            currentHealth = 0;
            Die();
        }
        else
        {
            anim.SetTrigger("Hit");
            StartCoroutine(InvincibilityCoroutine());
        }
    }
    #endregion



    #region Die
    void Die()
    {
        anim.SetTrigger("Death");
        pc.enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;
        GetComponent<PlayerDash>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<Collider2D>().enabled = false;

        // Continue add component into this func in future
        // add effects, loading screen, etc...
    }
    #endregion


    #region Invincible Time
    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }
    #endregion
}
