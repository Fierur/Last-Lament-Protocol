using System.Collections;
using UnityEngine;
 
 // Initialize this script before any other (-1 means it runs before default 0)
[DefaultExecutionOrder(-1)]
public class PlayerController : MonoBehaviour
{
    #region References
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public BoxCollider2D col;
    #endregion
 
    #region Ground & Ceiling Check
    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [HideInInspector] public bool isGrounded;
 
    [Header("Ceiling Check Settings")]
    public Transform ceilingCheck;
    public Vector2 ceilingCheckSize;
    [HideInInspector] public bool isCeilingBlocked;
    #endregion
 
    #region Shared State — read/write by component scripts
    [HideInInspector] public float horizontalInput;
    [HideInInspector] public bool jumpPressed;
    [HideInInspector] public bool dashPressed;
    [HideInInspector] public bool attackPressed;
    [HideInInspector] public bool crouchHeld;
 
    [HideInInspector] public bool isCrouching;
    [HideInInspector] public bool isDashing;
    [HideInInspector] public bool isAttacking;
    [HideInInspector] public bool isDead;
    #endregion
 
    #region Gravity
    [Header("Fall Settings")]
    [Range(1, 5)]
    public float fallGravityMultiplier;
    [HideInInspector] public float defaultGravity;
    #endregion
 
    void Start()
    {
        rb  = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col  = GetComponent<BoxCollider2D>();
        defaultGravity = rb.gravityScale;
    }
 
    // Update - Use this for input and non-physics updates. Called every frame.
    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
 
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching && !isDashing)
            jumpPressed = true;

        // Cancel jumpPressed if player isn't grounded before run HandleJump
        if(!isGrounded && jumpPressed)
        {
            jumpPressed = false;
        }
 
        attackPressed = Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
 
        dashPressed = Input.GetButtonDown("Dash");
 
        crouchHeld = Input.GetButton("Crouch");
 
        HandleFlip();
        UpdateAnimator();
    }
 
    // FixedUpdate - Use this for physics updates. Called at a fixed interval.
    void FixedUpdate()
    {
        CheckGround();
        CheckCeiling();
    }
 
    #region Ground & Ceiling
    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }
 
    private void CheckCeiling()
    {
        isCeilingBlocked = Physics2D.OverlapBox(ceilingCheck.position, ceilingCheckSize, 0f, groundLayer);
    }
    #endregion
 
    #region Flip
    private void HandleFlip()
    {
        if (horizontalInput > 0)
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        else if (horizontalInput < 0)
            transform.localRotation = Quaternion.Euler(0, 180, 0);
    }
    #endregion
 
    #region Animator
    private void UpdateAnimator()
    {
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocityY);
    }
    #endregion
 
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        if (ceilingCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(ceilingCheck.position, ceilingCheckSize);
        }
    }
}
 