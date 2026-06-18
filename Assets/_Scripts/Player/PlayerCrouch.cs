using UnityEngine;
 
public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch Collider Settings")]
    public Vector2 crouchingColliderOffset;
    public Vector2 crouchingColliderSize;
    
    [Header("Crouch Movement Settings")]
    // crouchSpeedMultiplier lives in PlayerMovement — reuse from there
 
    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
 
    PlayerController pc;
 
    void Start()
    {
        pc = GetComponent<PlayerController>();
 
        // Cache original standing collider values
        standingColliderSize   = pc.col.size;
        standingColliderOffset = pc.col.offset;
    }
 
    void Update()
    {
        if (pc.isGrabbing) return;
        HandleCrouch();
    }
 
    #region Crouch Logic
    void HandleCrouch()
    {
        if (pc.isGrabbing) return;
        
        if (pc.crouchHeld && pc.isGrounded)
            pc.isCrouching = true;
        else if (!pc.isCeilingBlocked)
            pc.isCrouching = false;
        // If ceiling is blocked -> keep isCrouching = true even after releasing button
 
        // Resize collider
        if (pc.isCrouching)
        {
            pc.col.size   = crouchingColliderSize;
            pc.col.offset = crouchingColliderOffset;
        }
        else
        {
            pc.col.size   = standingColliderSize;
            pc.col.offset = standingColliderOffset;
        }
 
        pc.anim.SetBool("Crouch", pc.isCrouching);
        pc.anim.SetBool("CrouchWalk", pc.isCrouching && Mathf.Abs(pc.horizontalInput) > 0.1f);
    }
    #endregion
}