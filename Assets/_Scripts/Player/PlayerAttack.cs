using System.Collections;
using UnityEngine;
 
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public int maxCombo;
    [Tooltip("Wait time to reset combo if no new attack is made (in seconds)")]
    public float comboResetTime;
    [Tooltip("Threshold for re-combo at atk4. I will set it to 3")]
    public int atk4ComboThreshold;
    [Tooltip("Force applied to the player during attack to create a sense of impact. Set it to 3f")]
    public float attackStepForce;
    public Transform[] attackPoints;
    public float[] attackRadiusi; //i is the index for different attack's hitbox radius
    public LayerMask enemyLayer;
    public int damagePerHit = 1;
 
    private int currentCombo = 0;
    private Coroutine comboResetCoroutine;
    private bool inputBuffered;
    private int atk4ClickCount;
 
    PlayerController pc;
 
    void Start()
    {
        pc = GetComponent<PlayerController>();
    }
 
    void Update()
    {
        if (pc.attackPressed)
            HandleAttack();
    }
 
    #region Attack Logic
    private void HandleAttack()
    {
        if (!pc.isGrounded || pc.isCrouching || pc.jumpPressed || Mathf.Abs(pc.horizontalInput) > 0.1f) return;
 
        if (pc.isAttacking)
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
        pc.isAttacking = true;
        inputBuffered = false;
        atk4ClickCount = 0; // Reset click count each time a new attack is initiated
 
        pc.anim.SetInteger("Combo", currentCombo);
        pc.anim.SetTrigger("Attack");
        Debug.Log("Current Combo " + currentCombo);
        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);
        comboResetCoroutine = StartCoroutine(ResetComboAfterDelay());
    }
 
    // Invoked by Animation Event at the end of each attack animation
    public void OnAttackEnd()
    {
        pc.isAttacking = false;
 
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
        pc.anim.SetInteger("Combo", 0);
 
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
        pc.isAttacking = false;
    }
    #endregion
 
    #region Attack Hit & Push
    // Invoked by Animation Event at the moment of hitbox activation in each attack animation
    public void AttackHit()
    {
         // currentCombo starts from 1, array index starts from 0
        int index = currentCombo - 1;
        if (index < 0 || index >= attackPoints.Length) return;
        if (attackPoints[index] == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoints[index].position, attackRadiusi[index], enemyLayer);

        foreach (Collider2D enemyObject in hitEnemies)
            enemyObject.GetComponent<Enemy>().TakeDamage(damagePerHit);
    }
 
    // Invoked by Animation Event to push player forward on each attack
    public void PushForwardAttack()
    {
        // Assuming facing right is 0 degrees and left is 180 degrees
        float pushDirection = transform.localRotation.eulerAngles.y == 0 ? 1 : -1;
        pc.rb.linearVelocity = new Vector2(pushDirection * attackStepForce, pc.rb.linearVelocityY);
    }
    #endregion
 
    void OnDrawGizmosSelected()
    {
        if (attackPoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < attackPoints.Length; i++)
    {
        if (attackPoints[i] == null) continue;
        float radius = (attackRadiusi != null && i < attackRadiusi.Length) ? attackRadiusi[i] : 0.3f;
        Gizmos.DrawWireSphere(attackPoints[i].position, radius);
    }
    }
}
 
