using UnityEngine;

// Initialize this script before any other (-1 means it runs before default 0)
[DefaultExecutionOrder(-1)]
public class PlayerController : MonoBehaviour
{
    #region References
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public BoxCollider2D col;
    [HideInInspector] public SpriteRenderer sr;
    #endregion

    #region Ground & Ceiling Check
    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Tooltip("Nên để X rộng vừa phải, Y mỏng. Ví dụ X = 0.45, Y = 0.08")]
    public Vector2 groundCheckSize = new Vector2(0.45f, 0.08f);

    [HideInInspector] public bool isGrounded;

    [Header("Ceiling Check Settings")]
    public Transform ceilingCheck;
    public Vector2 ceilingCheckSize;
    [HideInInspector] public bool isCeilingBlocked;
    #endregion

    #region Jump Assist
    [Header("Jump Assist Settings")]
    [Tooltip("Cho phép nhảy trong một khoảng ngắn sau khi vừa rời khỏi ground.")]
    public float coyoteTime = 0.12f;

    [Tooltip("Cho phép bấm jump hơi sớm trước khi chạm đất.")]
    public float jumpBufferTime = 0.12f;

    [HideInInspector] public float coyoteTimeCounter;
    [HideInInspector] public float jumpBufferCounter;
    #endregion

    #region Shared State — read/write by component scripts
    [HideInInspector] public float horizontalInput;

    // Giữ lại để không phá script cũ, nhưng jump mới dùng jumpBufferCounter + coyoteTimeCounter
    [HideInInspector] public bool jumpPressed;

    [HideInInspector] public bool dashPressed;
    [HideInInspector] public bool attackPressed;
    [HideInInspector] public bool crouchHeld;
    [HideInInspector] public bool grabPressed;

    [HideInInspector] public bool isCrouching;
    [HideInInspector] public bool isDashing;
    [HideInInspector] public bool isAttacking;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool isGrabbing;
    
    #endregion

    #region Gravity
    [Header("Fall Settings")]
    [Range(1, 5)]
    public float fallGravityMultiplier;

    [HideInInspector] public float defaultGravity;
    #endregion

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        grabPressed = Input.GetKeyDown(KeyCode.E);

        if (isGrabbing)
        {
            jumpPressed = false;
            dashPressed = false;
            attackPressed = false;
            crouchHeld = false;

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;

            HandleFlip();
            UpdateAnimator();
            return;
        }

        // Jump Buffer
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter < 0f)
            jumpBufferCounter = 0f;

        attackPressed = Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
        dashPressed = Input.GetButtonDown("Dash");
        crouchHeld = Input.GetButton("Crouch");

        HandleFlip();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        CheckGround();
        CheckCeiling();
        UpdateCoyoteTime();
    }

    #region Ground & Ceiling
    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        );
    }

    private void CheckCeiling()
    {
        isCeilingBlocked = Physics2D.OverlapBox(
            ceilingCheck.position,
            ceilingCheckSize,
            0f,
            groundLayer
        );
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.fixedDeltaTime;

        if (coyoteTimeCounter < 0f)
            coyoteTimeCounter = 0f;
    }
    #endregion

    #region Flip
    private void HandleFlip()
    {
        if (isGrabbing) return;

        if (horizontalInput > 0)
            SetFacing(true);
        else if (horizontalInput < 0)
            SetFacing(false);
    }

    // Gọi từ PlayerGrab để set facing về hướng ledge
    public void SetFacing(bool facingRight)
    {
        if (sr != null)
            sr.flipX = !facingRight;
    }

    public bool IsFacingRight => sr == null || !sr.flipX;

    #endregion

    #region Animator
    private void UpdateAnimator()
    {
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocityY);
        anim.SetBool("IsDashing", isDashing);
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