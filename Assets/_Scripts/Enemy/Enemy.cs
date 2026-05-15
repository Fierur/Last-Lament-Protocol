using UnityEngine;
 
public class Enemy : MonoBehaviour
{
    // Start is called before the first frame update. It is used for initialization.
    // Update is called once per frame. It is used for handling state decisions and animator.
    // FixedUpdate is called at a fixed interval and is used for PHYSICS updates.
 
    #region Variables
    Rigidbody2D erb;
    Animator anim;
 
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Attack Settings")]
    public int enemyDamage;
    public float attackPointRadius;
    public Transform eattackPoint;
    bool isDead;

    [Header("Spell Attack Settings")]
    public float spellAttackPointRadius;
    public Transform espellAttackPoint;
 
    [Header("Ground Check Settings")]
    public Transform egroundCheck;
    public float egroundCheckDistance;
    public LayerMask groundLayer;
 
    [Header("Movement Settings")]
    public float ewalkSpeed;
    public float chaseSpeed;
    bool facingLeft;
    Vector2 desiredVelocity; // Set in Update, applied in FixedUpdate
 
    [Header("Patrol Zone Settings")]
    [Tooltip("Radius around the starting point where the enemy can roam")]
    public float roamDistance = 5f;
    float leftLimit;
    float rightLimit;
    bool isInitialized = false;
    Vector2 startPosition;
 
    [Header("Random Move Settings")]
    public float minIdleTime = 1f;
    public float maxIdleTime = 3f;
    public float minWalkTime = 2f;
    public float maxWalkTime = 5f;
    float stateTimer;
    bool isWalking;
 
    [Header("Detect & Attack Settings")]
    public float detectRangeWidth;
    public float detectRangeHeight;
    public float attackRangeWidth = 2f;
    public float attackRangeHeight = 2f;
    public LayerMask playerLayer;

    [Header("Attack Cooldown")]
    public float attackCooldown = 1.5f;  
    float attackCoolDownTimer;
    EnemyEffect enemyEffect;
    #endregion
 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        erb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        enemyEffect = GetComponent<EnemyEffect>();
        facingLeft = true;
        currentHealth = maxHealth;
    }
 
    // Update is called once per frame - handles detection, state decisions, and animator
    void Update()
    {
        if(isDead) return;

        if(attackCoolDownTimer > 0)
            attackCoolDownTimer -= Time.deltaTime;

        Collider2D collInfo = Physics2D.OverlapBox(
            transform.position,
            new Vector2(detectRangeWidth, detectRangeHeight),
            0, playerLayer);
 
        if (collInfo)
            HandleChaseAndAttack(collInfo);
        else
            HandlePatrol();
    }
 
    // FixedUpdate - applies physics movement calculated in Update
    void FixedUpdate()
    {
        if(isDead) return;

        erb.linearVelocity = new Vector2(desiredVelocity.x, erb.linearVelocity.y);
    }
 
    #region Attack & Spell
    public void EAttackHit()
    {
        if (eattackPoint == null) return;
        Collider2D hitPlayer = Physics2D.OverlapCircle(eattackPoint.position, attackPointRadius, playerLayer);
        if (hitPlayer != null)
        {
            
            hitPlayer.GetComponent<PlayerHealth>()?.TakeDamage(enemyDamage);
        }
    }

    // Call from Animation Event at the frame spell that lands
    public void CastSpell()
    {
        enemyEffect?.PlaySpellEffect();

        // Detect player tại thời điểm spell chạm đất
        if (espellAttackPoint  == null) return;
        Collider2D hitPlayer = Physics2D.OverlapCircle(
            espellAttackPoint .position, attackPointRadius, playerLayer);

        hitPlayer?.GetComponent<PlayerHealth>()?.TakeDamage(enemyDamage);
    }
    #endregion

    #region Health
    public void TakeDamage(int damage)
    {
        if(isDead) return;
            currentHealth -= damage;

        if(currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;

            desiredVelocity = Vector2.zero;
            erb.linearVelocity = Vector2.zero;

            anim.SetBool("Walk", false);
            anim.SetBool("Attack", false);
            anim.SetBool("Cast", false);

            anim.SetTrigger("Die");
        }
        else
        {
            anim.SetTrigger("Hurt");
        }
    }

    public void OnDieAnimationEnd()
    {
        Destroy(gameObject);
    }
    #endregion
 
    #region Chase & Attack
    void HandleChaseAndAttack(Collider2D playerCollider)
    {
        Vector2 playerPos = playerCollider.transform.position;
        Vector2 offset = playerPos - (Vector2)transform.position;
        bool inAttackRange = Mathf.Abs(offset.x) <= attackRangeWidth * 0.5f && Mathf.Abs(offset.y) <= attackRangeHeight * 0.5f;
 
        if (!inAttackRange)
        {
            // Chase player
            float dir = playerPos.x < transform.position.x ? -1 : 1;
            if (!IsGroundAhead())
            {
                // Stop if chasing toward an edge
                desiredVelocity = Vector2.zero;
                anim.SetBool("Walk", false);
                anim.SetBool("Attack", false);
                return;
            }
 
            desiredVelocity = new Vector2(dir * chaseSpeed, 0);
            anim.SetBool("Walk", true);
            anim.SetBool("Attack", false);
 
            // Flip to face player
            if ((dir < 0 && !facingLeft) || (dir > 0 && facingLeft))
                HandleFlip();
        }
        else
        {
            if(attackCoolDownTimer <= 0f)
            {
                // Attack player — flip to face player before attacking
                if ((playerPos.x < transform.position.x && !facingLeft) || (playerPos.x > transform.position.x && facingLeft))
                HandleFlip();
                
                desiredVelocity = Vector2.zero;
                anim.SetBool("Walk", false);

                bool useCast =  Random.value > 0.5f;
                anim.SetBool("Attack", !useCast);
                anim.SetBool("Cast", useCast);
                anim.SetBool("Attack", true);

                attackCoolDownTimer = attackCooldown;
            }
            else
            {
                desiredVelocity = Vector2.zero;
                anim.SetBool("Walk", false);
                anim.SetBool("Attack", false);
            }
            
        }
    }
    #endregion
 
    #region Patrol
    void HandlePatrol()
    {
        // Wait until enemy touches ground before initializing patrol zone
        if (!isInitialized)
        {
            RaycastHit2D groundHit = Physics2D.Raycast(egroundCheck.position, Vector2.down, egroundCheckDistance, groundLayer);
 
            if (groundHit.collider != null)
            {
                startPosition = transform.position;
                leftLimit  = startPosition.x - roamDistance;
                rightLimit = startPosition.x + roamDistance;
                isInitialized = true;
                PickNewState();
            }
            return;
        }
        HandleRandomMovement();

        // Flip if no ground ahead while walking
        RaycastHit2D hitInfo = Physics2D.Raycast(egroundCheck.position, Vector2.down, egroundCheckDistance, groundLayer);

        if (hitInfo.collider == null && isWalking)
        {
            HandleFlip();
            PickNewState();
        }
    }
    void HandleRandomMovement()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
            PickNewState();
 
        if (isWalking)
        {
            bool hitLeft  = transform.position.x <= leftLimit  && facingLeft;
            bool hitRight = transform.position.x >= rightLimit && !facingLeft;
            bool edgeAhead = !IsGroundAhead();
 
            if (hitLeft || hitRight || edgeAhead)
            {
                if (edgeAhead)
                    HandleFlip();
 
                // Hit boundary or edge -> stop and idle
                isWalking = false;
                stateTimer = Random.Range(minIdleTime, maxIdleTime);
                desiredVelocity = Vector2.zero;
                anim.SetBool("Walk", false);
                return;
            }
 
            float moveDir = facingLeft ? -1 : 1;
            desiredVelocity = new Vector2(moveDir * ewalkSpeed, 0);
            anim.SetBool("Walk", true);
        }
        else
        {
            desiredVelocity = Vector2.zero;
            anim.SetBool("Walk", false);
        }
    }
 
    bool IsGroundAhead()
    {
        if (egroundCheck == null)
            return true;
 
        return Physics2D.Raycast(egroundCheck.position, Vector2.down, egroundCheckDistance, groundLayer).collider != null;
    }
 
    void PickNewState()
    {
        isWalking = Random.value > 0.5f;
 
        if (isWalking)
        {
            stateTimer = Random.Range(minWalkTime, maxWalkTime);
 
            // Force flip away from boundary if too close
            if (isInitialized && transform.position.x <= leftLimit && facingLeft)
                HandleFlip();
            else if (isInitialized && transform.position.x >= rightLimit && !facingLeft)
                HandleFlip();
            else if (Random.value > 0.5f)
                HandleFlip();
        }
        else
        {
            stateTimer = Random.Range(minIdleTime, maxIdleTime);
        }
    }
    #endregion
 
    #region Flip
    void HandleFlip()
    {
        if (facingLeft)
        {
            transform.eulerAngles = new Vector3(0, 180, 0);
            facingLeft = false;
        }
        else
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            facingLeft = true;
        }
    }
    #endregion
 
    void OnDrawGizmosSelected()
    {
        // Ground raycast
        if (egroundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(egroundCheck.position,
                egroundCheck.position + Vector3.down * egroundCheckDistance);
        }
 
        // Patrol zone
        if (isInitialized)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 center = new Vector3((leftLimit + rightLimit) / 2f, transform.position.y, 0f);
            Vector3 size   = new Vector3(rightLimit - leftLimit, 2f, 0f);
            Gizmos.DrawCube(center, size);
 
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(leftLimit,  transform.position.y + 1f, 0f),
                            new Vector3(leftLimit,  transform.position.y - 1f, 0f));
            Gizmos.DrawLine(new Vector3(rightLimit, transform.position.y + 1f, 0f),
                            new Vector3(rightLimit, transform.position.y - 1f, 0f));
        }
 
        // Detect range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector2(detectRangeWidth, detectRangeHeight));
 
        // Attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector2(attackRangeWidth, attackRangeHeight));

        // Attack point range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(eattackPoint.position, attackPointRadius);

        if (espellAttackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(espellAttackPoint.position, spellAttackPointRadius);
        }
    }
}