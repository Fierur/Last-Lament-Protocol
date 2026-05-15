using UnityEngine;
 
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float jumpForce;
 
    [Header("Crouch Settings")]
    [Range(0, 1)]
    public float crouchSpeedMultiplier;
 
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
    }
 
    #region Movement
    private void HandleMovement()
    {
        if (pc.isCrouching)
        {
            pc.rb.linearVelocity = new Vector2(
                pc.horizontalInput * moveSpeed * crouchSpeedMultiplier,
                pc.rb.linearVelocityY);
            return;
        }
        if (pc.isDashing) return;
        if (pc.isAttacking) return;
 
        pc.rb.linearVelocity = new Vector2(
            pc.horizontalInput * moveSpeed,
            pc.rb.linearVelocityY);
    }
    #endregion
 
    #region Jump
    private void HandleJump()
    {
        if (pc.isDashing || !pc.isGrounded || pc.isCrouching) return;
 
        if (pc.jumpPressed && pc.isGrounded)
        {
            pc.rb.linearVelocity = new Vector2(pc.rb.linearVelocityX, jumpForce);
            pc.jumpPressed = false;
        }
    }
    #endregion
 
    #region Fall Gravity
    private void HandleFallGravity()
    {
        if (pc.isDashing) return;
 
        if (!pc.isGrounded && pc.rb.linearVelocityY < 0)
            pc.rb.gravityScale = pc.defaultGravity * pc.fallGravityMultiplier;
        else
            pc.rb.gravityScale = pc.defaultGravity;
    }
    #endregion
}