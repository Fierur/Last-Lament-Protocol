using System.Collections;
using UnityEngine;
 
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
    public int maxDashCount = 2;
 
    [HideInInspector] public int currentDashCount;
 
    PlayerController pc;
    PlayerEffect playerEffect;
 
    void Start()
    {
        pc           = GetComponent<PlayerController>();
        playerEffect = GetComponent<PlayerEffect>();
        currentDashCount = maxDashCount;
    }
 
    void Update()
    {
        if (pc.dashPressed && currentDashCount > 0 && !pc.isDashing && !pc.isCrouching)
            StartCoroutine(Dash());
    }
 
    #region Dash Coroutine
    IEnumerator Dash()
    {
        pc.isDashing = true;
        currentDashCount--;
 
        pc.anim.SetTrigger("Dash");
        playerEffect.PlayDashEffect(transform.right.x);
 
        pc.rb.gravityScale = 0f;
 
        float direction = transform.right.x;
        pc.rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);
 
        yield return new WaitForSeconds(dashDuration);
 
        pc.rb.gravityScale = pc.defaultGravity;
        pc.isDashing = false;
 
        StartCoroutine(RestoreOneDash());
    }
 
    IEnumerator RestoreOneDash()
    {
        yield return new WaitForSeconds(dashCooldown);
 
        currentDashCount++;
 
        if (currentDashCount > maxDashCount)
            currentDashCount = maxDashCount;
    }
    #endregion
}
 