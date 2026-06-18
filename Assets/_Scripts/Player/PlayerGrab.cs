using System.Collections;
using UnityEngine;

public class PlayerGrab : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Detection")]
    [Tooltip("Layer dùng riêng cho ledge trigger/collider")]
    public LayerMask ledgeLayer;

    [Tooltip("Khoảng cách ngang từ tâm player tới detection box")]
    public float detectReachX = 0.6f;

    [Tooltip("Offset Y từ tâm player lên ngang đầu/tay bám")]
    public float detectOffsetY = 0.8f;

    [Tooltip("Kích thước OverlapBox phát hiện ledge")]
    public Vector2 detectBoxSize = new Vector2(0.3f, 0.2f);

    [Header("Climb Positions")]
    [Tooltip("Vị trí khi nhân vật vừa kéo thân lên ledge, thường là pose ngồi/quỳ.")]
    public Vector2 climbSitOffset = new Vector2(0.35f, 0.65f);

    [Tooltip("Vị trí cuối khi nhân vật đã đứng thẳng trên ledge.")]
    public Vector2 climbStandOffset = new Vector2(0.55f, 1.0f);

    [Header("Climb Movement Duration")]
    [Tooltip("Thời gian di chuyển từ vị trí bám tới vị trí ngồi/quỳ trên ledge.")]
    public float climbToSitDuration = 0.35f;

    [Tooltip("Thời gian di chuyển từ vị trí ngồi/quỳ tới vị trí đứng hoàn chỉnh.")]
    public float climbToStandDuration = 0.25f;

    [Header("Grab Input Lock")]
    [Tooltip("Sau khi vừa bám ledge, khóa E trong thời gian ngắn để tránh spam E làm rơi ngay. Không khóa hướng leo.")]
    public float grabInputLockDuration = 0.2f;

    [Header("Release Cooldown")]
    [Tooltip("Sau khi nhấn E thoát grab, delay này ngăn grab lại ngay lập tức.")]
    public float releaseCooldown = 0.3f;

    [Header("Collider — Grab Idle")]
    [Tooltip("Collider khi bám ledge đứng yên")]
    public Vector2 grabIdleSize = new Vector2(0.2f, 1.343933f);
    public Vector2 grabIdleOffset = new Vector2(-0.3f, -0.0723505f);

    [Header("Collider — Grab Climb")]
    [Tooltip("Collider khi đang leo — sprite nằm ngang/ngồi nên dẹt lại")]
    public Vector2 grabClimbSize = new Vector2(0.7114372f, 0.7125282f);
    public Vector2 grabClimbOffset = new Vector2(0.08334541f, 0.1739664f);

    // ── Private ───────────────────────────────────────────────────────────────
    private PlayerController pc;

    private Vector2 originalSize;
    private Vector2 originalOffset;

    private Vector2 ledgeSurfacePos;
    private int grabDir; // +1 = ledge bên phải, -1 = ledge bên trái

    private enum GrabState
    {
        None,
        Idle,
        Climbing
    }

    private GrabState state = GrabState.None;

    private float releaseCooldownTimer;
    private float grabInputLockTimer;

    private Vector3 climbStartPos;
    private Vector3 climbSitPos;
    private Vector3 climbStandPos;

    private Coroutine climbMoveCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        pc = GetComponent<PlayerController>();

        originalSize = pc.col.size;
        originalOffset = pc.col.offset;
    }

    void Update()
    {
        releaseCooldownTimer -= Time.deltaTime;
        grabInputLockTimer -= Time.deltaTime;

        switch (state)
        {
            case GrabState.None:
                UpdateNone();
                break;

            case GrabState.Idle:
                UpdateIdle();
                break;

            case GrabState.Climbing:
                break;
        }
    }

    void FixedUpdate()
    {
        if (pc.isGrabbing)
        {
            pc.rb.linearVelocity = Vector2.zero;
            pc.rb.angularVelocity = 0f;
        }
    }

    // ── None: chờ input E để grab ─────────────────────────────────────────────
    void UpdateNone()
    {
        if (pc.isDashing) return;
        if (pc.isAttacking) return;
        if (pc.isCrouching) return;
        if (releaseCooldownTimer > 0f) return;
        if (!pc.grabPressed) return;

        TryGrab();
    }

    void TryGrab()
    {
        // Chỉ check phía trước mặt, không check sau lưng
        int dir = pc.IsFacingRight ? 1 : -1;

        Vector2 checkPos = (Vector2)transform.position
                         + new Vector2(dir * detectReachX, detectOffsetY);

        Collider2D hit = Physics2D.OverlapBox(
            checkPos,
            detectBoxSize,
            0f,
            ledgeLayer
        );

        if (hit == null) return;

        // Vì Ledge là collider nhỏ riêng, dùng bounds sẽ ổn hơn TilemapCollider lớn
        ledgeSurfacePos = new Vector2(
            hit.bounds.center.x,
            hit.bounds.max.y
        );

        grabDir = dir;
        EnterGrab();
    }

    // ── Enter Grab ────────────────────────────────────────────────────────────
    void EnterGrab()
    {
        state = GrabState.Idle;
        pc.isGrabbing = true;

        // Chỉ khóa E sau khi vừa grab, không khóa hướng leo
        grabInputLockTimer = grabInputLockDuration;

        pc.rb.linearVelocity = Vector2.zero;
        pc.rb.angularVelocity = 0f;
        pc.rb.gravityScale = 0f;

        // Snap vào vị trí bám
        float snapX = ledgeSurfacePos.x - grabDir * (originalSize.x * 0.5f + 0.05f);
        float snapY = ledgeSurfacePos.y - detectOffsetY;

        transform.position = new Vector3(
            snapX,
            snapY,
            transform.position.z
        );

        // Quay mặt vào ledge
        pc.SetFacing(grabDir > 0);

        // Collider khi đang bám
        pc.col.size = grabIdleSize;
        pc.col.offset = grabIdleOffset;

        // Animator
        pc.anim.ResetTrigger("GrabClimb");
        pc.anim.SetBool("IsGrabbing", true);
        pc.anim.SetTrigger("GrabContact");
    }

    // ── Idle: bám ledge, chờ input ────────────────────────────────────────────
    void UpdateIdle()
    {
        // Nhấn hướng vào ledge → climb
        // Không bị chặn bởi grabInputLockTimer
        bool climbInput =
            (grabDir > 0 && pc.horizontalInput > 0.1f) ||
            (grabDir < 0 && pc.horizontalInput < -0.1f);

        if (climbInput)
        {
            EnterClimb();
            return;
        }

        // E chỉ được dùng để release sau khi hết input lock
        if (grabInputLockTimer > 0f)
            return;

        if (pc.grabPressed && releaseCooldownTimer <= 0f)
        {
            ExitGrab(triggerCooldown: true);
            return;
        }
    }

    // ── Climbing ──────────────────────────────────────────────────────────────
    void EnterClimb()
    {
        state = GrabState.Climbing;

        pc.rb.linearVelocity = Vector2.zero;
        pc.rb.angularVelocity = 0f;
        pc.rb.gravityScale = 0f;

        climbStartPos = transform.position;

        climbSitPos = new Vector3(
            ledgeSurfacePos.x + grabDir * climbSitOffset.x,
            ledgeSurfacePos.y + climbSitOffset.y,
            transform.position.z
        );

        climbStandPos = new Vector3(
            ledgeSurfacePos.x + grabDir * climbStandOffset.x,
            ledgeSurfacePos.y + climbStandOffset.y,
            transform.position.z
        );

        pc.anim.ResetTrigger("GrabContact");
        pc.anim.SetTrigger("GrabClimb");
    }

    // ── Animation Events for grab_climb ───────────────────────────────────────

    // Animation Event:
    // Đặt ở frame bắt đầu kéo người lên ledge
    public void StartMoveToSit()
    {
        if (state != GrabState.Climbing) return;

        if (climbMoveCoroutine != null)
            StopCoroutine(climbMoveCoroutine);

        climbMoveCoroutine = StartCoroutine(
            MoveToPosition(climbSitPos, climbToSitDuration)
        );
    }

    // Animation Event:
    // Đặt ở frame sprite thấp/ngồi/nằm ngang để tránh collider bị cấn
    public void SetGrabClimbCollider()
    {
        if (state != GrabState.Climbing) return;

        pc.col.size = grabClimbSize;
        pc.col.offset = grabClimbOffset;
    }

    // Animation Event:
    // Đặt ở frame bắt đầu đứng dậy
    public void StartMoveToStand()
    {
        if (state != GrabState.Climbing) return;

        if (climbMoveCoroutine != null)
            StopCoroutine(climbMoveCoroutine);

        climbMoveCoroutine = StartCoroutine(
            MoveToPosition(climbStandPos, climbToStandDuration)
        );
    }

    // Animation Event:
    // Đặt ở frame cuối khi sprite đã đứng bình thường
    public void RestoreColliderAndFinishClimb()
    {
        if (state != GrabState.Climbing) return;

        if (climbMoveCoroutine != null)
        {
            StopCoroutine(climbMoveCoroutine);
            climbMoveCoroutine = null;
        }

        transform.position = climbStandPos;

        pc.col.size = originalSize;
        pc.col.offset = originalOffset;

        ExitGrab(triggerCooldown: false);
    }

    IEnumerator MoveToPosition(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            transform.position = targetPos;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            // SmoothStep
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            pc.rb.linearVelocity = Vector2.zero;
            pc.rb.angularVelocity = 0f;

            yield return null;
        }

        transform.position = targetPos;
    }

    // ── Exit Grab ─────────────────────────────────────────────────────────────
    void ExitGrab(bool triggerCooldown = true)
    {
        state = GrabState.None;
        pc.isGrabbing = false;

        if (climbMoveCoroutine != null)
        {
            StopCoroutine(climbMoveCoroutine);
            climbMoveCoroutine = null;
        }

        pc.col.size = originalSize;
        pc.col.offset = originalOffset;

        pc.rb.gravityScale = pc.defaultGravity;
        pc.rb.linearVelocity = Vector2.zero;
        pc.rb.angularVelocity = 0f;

        pc.anim.SetBool("IsGrabbing", false);

        if (triggerCooldown)
            releaseCooldownTimer = releaseCooldown;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);

        int dir = 1;

        if (Application.isPlaying && pc != null)
            dir = pc.IsFacingRight ? 1 : -1;
        else
            dir = transform.localRotation.eulerAngles.y < 90f ? 1 : -1;

        Gizmos.DrawWireCube(
            (Vector2)transform.position + new Vector2(dir * detectReachX, detectOffsetY),
            detectBoxSize
        );

        if (Application.isPlaying && state != GrabState.None)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(climbSitPos, 0.12f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(climbStandPos, 0.12f);
        }
    }
}