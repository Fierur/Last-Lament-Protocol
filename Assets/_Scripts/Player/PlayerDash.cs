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
    Coroutine dashCoroutine;

    void Start()
    {
        pc = GetComponent<PlayerController>();
        playerEffect = GetComponent<PlayerEffect>();
        currentDashCount = maxDashCount;
    }

    void Update()
    {
        if (!pc.dashPressed) return;
        if (!CanStartDash()) return;

        dashCoroutine = StartCoroutine(Dash());
    }

    bool CanStartDash()
    {
        if (pc.isDead) return false;
        if (pc.isGrabbing) return false;
        if (pc.isDashing) return false;
        if (pc.isCrouching) return false;

        // Chặn dash chen vào attack để tránh văng xa
        if (pc.isAttacking) return false;

        if (currentDashCount <= 0) return false;

        return true;
    }

    IEnumerator Dash()
    {
        pc.isDashing = true;
        currentDashCount--;

        float direction = pc.IsFacingRight ? 1f : -1f;

        // Ép animator biết đang dash NGAY LẬP TỨC
        pc.anim.SetBool("IsDashing", true);
        pc.anim.ResetTrigger("Dash");
        pc.anim.SetTrigger("Dash");

        if (playerEffect != null)
            playerEffect.PlayDashEffect(direction);

        pc.rb.gravityScale = 0f;
        pc.rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            // Mặc kệ tường. Nếu tường chặn thì player không đi xuyên,
            // nhưng dash state + dash animation vẫn chạy đủ thời gian.
            pc.rb.gravityScale = 0f;
            pc.rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        pc.rb.linearVelocity = Vector2.zero;
        pc.rb.gravityScale = pc.defaultGravity;

        pc.isDashing = false;
        pc.anim.SetBool("IsDashing", false);

        dashCoroutine = null;

        StartCoroutine(RestoreOneDash());
    }

    IEnumerator RestoreOneDash()
    {
        yield return new WaitForSeconds(dashCooldown);

        currentDashCount++;

        if (currentDashCount > maxDashCount)
            currentDashCount = maxDashCount;
    }
}