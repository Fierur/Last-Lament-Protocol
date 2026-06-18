using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float jumpForce;

    [Header("Crouch Settings")]
    [Range(0, 1)]
    public float crouchSpeedMultiplier;

    [Header("Ground Stick Settings")]
    [Tooltip("Bật để giảm tình trạng bị trôi nhẹ khi đứng sát mép.")]
    public bool enableGroundStick = true;

    [Tooltip("Chỉ chặn velocity Y nếu player đang rơi rất nhẹ.")]
    public float groundStickMaxFallSpeed = 0.5f;

    PlayerController pc;

    void Start()
    {
        pc = GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        HandleFallGravity();
        HandleGroundStick();
    }

    #region Movement
    private void HandleMovement()
    {
        if (pc.isGrabbing) return;

        if (pc.isCrouching)
        {
            pc.rb.linearVelocity = new Vector2(
                pc.horizontalInput * moveSpeed * crouchSpeedMultiplier,
                pc.rb.linearVelocityY
            );
            return;
        }

        if (pc.isDashing) return;
        if (pc.isAttacking) return;

        pc.rb.linearVelocity = new Vector2(
            pc.horizontalInput * moveSpeed,
            pc.rb.linearVelocityY
        );
    }
    #endregion

    #region Jump
    private void HandleJump()
    {
        if (pc.isAttacking) return;
        if (pc.isGrabbing) return;
        if (pc.isDashing) return;
        if (pc.isCrouching) return;

        bool hasBufferedJump = pc.jumpBufferCounter > 0f;
        bool canUseCoyote = pc.coyoteTimeCounter > 0f;

        if (hasBufferedJump && canUseCoyote)
        {
            pc.rb.linearVelocity = new Vector2(
                pc.rb.linearVelocityX,
                jumpForce
            );

            // Consume jump
            pc.jumpBufferCounter = 0f;
            pc.coyoteTimeCounter = 0f;
            pc.jumpPressed = false;
        }
    }
    #endregion

    #region Fall Gravity
    private void HandleFallGravity()
    {
        if (pc.isGrabbing) return;
        if (pc.isDashing) return;

        if (!pc.isGrounded && pc.rb.linearVelocityY < 0)
        {
            pc.rb.gravityScale = pc.defaultGravity * pc.fallGravityMultiplier;
        }
        else
        {
            pc.rb.gravityScale = pc.defaultGravity;
        }
    }
    #endregion

    #region Ground Stick
    private void HandleGroundStick()
    {
        if (!enableGroundStick) return;

        if (pc.isGrabbing) return;
        if (pc.isDashing) return;
        if (pc.isAttacking) return;
        if (!pc.isGrounded) return;

        bool noHorizontalInput = Mathf.Abs(pc.horizontalInput) < 0.01f;
        bool slightlyFalling =
            pc.rb.linearVelocityY < 0f &&
            pc.rb.linearVelocityY > -groundStickMaxFallSpeed;

        if (noHorizontalInput && slightlyFalling)
        {
            pc.rb.linearVelocity = new Vector2(
                pc.rb.linearVelocityX,
                0f
            );
        }
    }
    #endregion
}