using System.Collections;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    #region Variables
    Rigidbody2D rb;
    Animator anim;
    
    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    bool isGrounded;

    [Header("Movement Settings")]
    public float moveSpeed;
    float horizontalInput;
    public float jumpForce;
    bool jumpPressed;

    [Header("Crouch Settings")]
    BoxCollider2D col;
    Vector2 standingColliderSize;
    Vector2 standingColliderOffset;
    public Vector2 crouchingColliderOffset;
    public Vector2 crouchingColliderSize;

    public Transform ceilingCheck;
    public Vector2 ceilingCheckSize;
    // we can just reuse groundLayer
    
    // Multiplier to reduce speed while crouching
    [Range(0,1)]
    public float crouchSpeedMultiplier;
    bool isCeilingBlocked;
    bool isCrouching;

    [Header("Fall Settings")]
    [Range(1, 5)]
    public float fallGravityMultiplier;
    private float defaultGravity;

    [Header("Attack Settings")]
    public int maxCombo;
    [Tooltip("Wait time to reset combo if no new attack is made (in seconds)")]
    public float comboResetTime; // Wait time to reset combo if no new attack is made (in seconds)
    private int currentCombo = 0;
    private Coroutine comboResetCoroutine; 
    private bool isAttacking;   
    private bool inputBuffered;     // buffer input while attacking at atk1,2,3 
    private int atk4ClickCount;  // Count number of clicks for atk4
    [Tooltip("Threshold for re-combo at atk4. I will set it to 3")]       
    public int atk4ComboThreshold; 
    [Tooltip("Force applied to the player during attack to create a sense of impact. Set it to 3f")]
    public float attackStepForce;

    [Header("Dash Settings")]
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
    bool isDashing;
    public int maxDashCount = 2;
    int currentDashCount;

    [Header("Dash Visual Effect Settings")]
    PlayerEffect playerEffect;
#endregion
    // Before code, I need to separate which functions are for Start, Update, and FixedUpdate
    // Start is called before the first frame update. It is used for initialization.
    // Update is called once per frame. It is used for handling INPUT and non-physics related updates.
    // FixedUpdate is called at a fixed interval and is used for PHYSICS updates.

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();
        playerEffect = GetComponent<PlayerEffect>();
        

        // Store the original collider size and offset
        standingColliderSize = col.size;
        standingColliderOffset = col.offset;

        currentDashCount = maxDashCount;
        defaultGravity = rb.gravityScale;
    }

    // Update is called once per frame
    void Update()
    {
        // Read input
        horizontalInput = Input.GetAxis("Horizontal");
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching && !isDashing)
            jumpPressed = true;
        

        // Read input attack button and set trigger 
        if (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0)) 
            HandleAttack();
        

        if (Input.GetButtonDown("Dash") && currentDashCount > 0 && !isDashing && !isCrouching)
            StartCoroutine(Dash());
        

        HandleCrouch();

        
        HandleFlip();
        UpdateAnimator();


    }

    // FixedUpdate is called at a fixed interval and is used for physics updates
    void FixedUpdate()
    {
        CheckGround();
        CheckCeiling();
        HandleMovement();
        HandleJump();
        HandleFallGravity();
    }

#region Dash 
    IEnumerator Dash()
    {
        isDashing = true;
        currentDashCount--;

        anim.SetTrigger("Dash");
        
        playerEffect.PlayDashEffect(transform.right.x);

        //float originalGravity = defaultGravity;
        rb.gravityScale = 0f;

        float direction = transform.right.x;

        rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = defaultGravity;
        isDashing = false;

        StartCoroutine(RestoreOneDash());
    }

    IEnumerator RestoreOneDash()
    {
        yield return new WaitForSeconds(dashCooldown);

        currentDashCount++;

        // clamp to be no more than maxDashCount
        if (currentDashCount > maxDashCount)
            currentDashCount = maxDashCount;
    }
#endregion





#region OverlapBox|Move|Flip
    private void CheckGround()
    {
        // Check if the player is grounded by checking for collisions with the ground layer
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private void CheckCeiling()
    {
        // Check if the player can stand up when nothing is above while crouching
        isCeilingBlocked = Physics2D.OverlapBox(ceilingCheck.position, ceilingCheckSize, 0f, groundLayer);
    }

    private void HandleMovement()
    {
        if (isCrouching)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed * crouchSpeedMultiplier, rb.linearVelocityY);
            return;
        }
        if (isDashing) return;
        // Prevent movement while attacking
        if (isAttacking) return; 
        // Apply movement
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocityY);
    }

    private void HandleFlip()
    {
        // Flip the player sprite based on movement direction
        if(horizontalInput > 0)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if(horizontalInput < 0)
        {
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }
#endregion 





#region Jump
    private void HandleJump()
    {
        // Prevent jumping when not grounded or while dashing
        if (isDashing || !isGrounded || isCrouching) return;

        // Check for jump input and apply jump force
        if(jumpPressed && isGrounded )
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
            jumpPressed = false; // Reset to prevent multiple jumps
        }
    }

    private void HandleFallGravity()
    {
        if (isDashing) return;

        if (!isGrounded && rb.linearVelocityY < 0)
            rb.gravityScale = defaultGravity * fallGravityMultiplier;
        else
            rb.gravityScale = defaultGravity;
    }
#endregion





#region Attack
    public void PushForwardAttack()
    {
        // Get the direction the player is facing
        // Assuming facing right is 0 degrees and left is 180 degrees
        float pushDirection = transform.localRotation.eulerAngles.y == 0 ? 1 : -1; 
        rb.linearVelocity = new Vector2(pushDirection * attackStepForce, rb.linearVelocityY);
    }

    private void HandleAttack()
    {
        // Only allow attacking if grounded and not moving (or very slow movement)
        if (!isGrounded || Mathf.Abs(horizontalInput) > 0.1f) return;

        if (isAttacking)
        {   
            if (currentCombo >= maxCombo)
            {
                // While at atk4, if player keeps clicking -> count to determine re-combo or not
                atk4ClickCount++;
                if (atk4ClickCount >= atk4ComboThreshold)
                {
                    // Player intentionally wants to combo again -> reset and attack atk1 immediately when OnAttackEnd
                    inputBuffered = true;
                }
                // Below threshold -> skip, will return to idle after OnAttackEnd
            }
            else
            {
                // While at atk1/2/3 -> buffer to continue combo
                inputBuffered = true;
            }
            return;
        }

        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        if (currentCombo >= maxCombo) return;

        currentCombo++;
        isAttacking = true;
        inputBuffered = false;
        atk4ClickCount = 0; // Reset click count each time a new attack is initiated

        anim.SetInteger("Combo", currentCombo);
        anim.SetTrigger("Attack");

        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);
        comboResetCoroutine = StartCoroutine(ResetComboAfterDelay());
    }

    // invoked by Animation Event at the end of each attack animation
    public void OnAttackEnd()
    {
        isAttacking = false;

        if (inputBuffered)
        {
            if (currentCombo >= maxCombo)
            {
                // This is the case of atk4 + sufficient click threshold -> restart combo from atk1
                ResetCombo();
            }
            ExecuteAttack();
        }
        else if (currentCombo >= maxCombo)
        {
            // reset if threshold not met and return to idle after atk4
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        currentCombo = 0;
        atk4ClickCount = 0;
        inputBuffered = false;
        anim.SetInteger("Combo", 0);

        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
    }

    private IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo();
        isAttacking = false;
    }
#endregion
    




#region Crouch
   void HandleCrouch()
    {
         if (Input.GetButton("Crouch") && isGrounded)
            isCrouching = true;
        else if (!isCeilingBlocked)
            isCrouching = false;
        
        if (isCrouching)
        {
            col.size   = crouchingColliderSize;
            col.offset = crouchingColliderOffset;
        }
        else
        {
            col.size   = standingColliderSize;
            col.offset = standingColliderOffset;
        }

        anim.SetBool("Crouch", isCrouching);
        anim.SetBool("CrouchWalk", isCrouching && Mathf.Abs(horizontalInput) > 0.1f);
            

    }
#endregion
    private void UpdateAnimator()
    {
        // Update animator parameters
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("Grounded", isGrounded);
    }

    
    

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;
        // Draw the ground check area in the editor for visualization
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        if (ceilingCheck == null)
            return;
        // Draw the ceiling check area in the editor for visualization
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(ceilingCheck.position, ceilingCheckSize);
    }
}
