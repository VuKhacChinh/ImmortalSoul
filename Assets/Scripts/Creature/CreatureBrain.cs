using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CreatureBrain : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Vision")]
    public float visionRange = 5f;

    [Header("Combat")]
    public float maxHP = 100f;
    public float currentHP;
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("AI")]
    public float courageOffset = 20f;

    private float attackTimer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isDead = false;
    private bool isAttacking = false;

    private CreatureBrain currentTarget;

    private bool originalFlipX;   // 🔥 LƯU FLIP BAN ĐẦU

    private enum AIState
    {
        Idle,
        Wander,
        Fight,
        Flee
    }

    private AIState currentState = AIState.Idle;

    private Vector2 wanderDirection;
    private float stateTimer;

    // =========================================================

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHP = maxHP;

        // 🔥 Lưu flip gốc
        originalFlipX = spriteRenderer.flipX;
    }

    void Start()
    {
        HPBarManager.Instance.CreateHPBar(this);
        ChangeToIdle();
    }

    void Update()
    {
        if (isDead) return;

        attackTimer -= Time.deltaTime;

        HandleAI();
        UpdateFacing();
    }

    // =========================================================
    // ===================== AI CORE ===========================
    // =========================================================

    void HandleAI()
    {
        if (currentTarget == null || currentTarget.isDead)
            currentTarget = FindTargetInVision();

        if (currentTarget != null)
        {
            // 🔥 Khi có target → bỏ hoàn toàn Idle/Wander
            DecideCombatState();

            if (currentState == AIState.Fight)
                HandleFight();
            else
                HandleFlee();

            return; // QUAN TRỌNG
        }

        // Không có target mới được Idle/Wander
        HandleWanderIdle();
    }

    void DecideCombatState()
    {
        float myPower = GetPowerScore();
        float enemyPower = currentTarget.GetPowerScore();

        if (myPower >= enemyPower - courageOffset)
            currentState = AIState.Fight;
        else
            currentState = AIState.Flee;
    }

    float GetPowerScore()
    {
        return currentHP * attackDamage;
    }

    // =========================================================
    // ===================== FIGHT =============================
    // =========================================================

    void HandleFight()
    {
        if (currentTarget == null) return;

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;
        float rangeSqr = attackRange * attackRange;

        if (sqrDist <= rangeSqr)
        {
            rb.linearVelocity = Vector2.zero;

            // 🔥 quay mặt theo target khi đứng đánh
            FaceDirection(toTarget.x);

            if (attackTimer <= 0f && !isAttacking)
            {
                attackTimer = attackCooldown;
                StartAttack();
                currentTarget.TakeDamage(attackDamage);
            }
        }
        else
        {
            Vector2 dir = toTarget.normalized;
            rb.linearVelocity = dir * moveSpeed;

            FaceDirection(dir.x);
        }
    }

    // =========================================================
    // ===================== FLEE ==============================
    // =========================================================

    void HandleFlee()
    {
        if (currentTarget == null) return;

        Vector2 dir = (transform.position - currentTarget.transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        FaceDirection(dir.x);
    }

    // =========================================================
    // ===================== WANDER ============================
    // =========================================================

    void HandleWanderIdle()
    {
        stateTimer -= Time.deltaTime;

        if (currentState == AIState.Idle)
        {
            rb.linearVelocity = Vector2.zero;

            if (stateTimer <= 0)
                ChangeToWander();
        }
        else if (currentState == AIState.Wander)
        {
            rb.linearVelocity = wanderDirection * moveSpeed;

            FaceDirection(wanderDirection.x);

            if (stateTimer <= 0)
            {
                if (Random.value > 0.5f)
                    ChangeToIdle();
                else
                    ChangeToWander();
            }
        }
    }

    void ChangeToIdle()
    {
        currentState = AIState.Idle;
        stateTimer = Random.Range(1f, 2f);
    }

    void ChangeToWander()
    {
        currentState = AIState.Wander;
        stateTimer = Random.Range(1.5f, 3f);

        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-0.3f, 0.3f);

        wanderDirection = new Vector2(randomX, randomY).normalized;
    }

    // =========================================================
    // ===================== ATTACK ============================
    // =========================================================

    void StartAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetTrigger("Attack");

        Invoke(nameof(EndAttack), 0.4f);
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    // =========================================================
    // ===================== FACING FIX ========================
    // =========================================================

    void FaceDirection(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.01f)
            return;

        if (xDir > 0)
        {
            // 🔥 đi sang phải → giữ flip gốc
            spriteRenderer.flipX = originalFlipX;
        }
        else
        {
            // 🔥 đi sang trái → đảo flip gốc
            spriteRenderer.flipX = !originalFlipX;
        }
    }

    void UpdateFacing()
    {
        // fallback nếu đang trượt vật lý
        if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
        {
            FaceDirection(rb.linearVelocity.x);
        }
    }

    // =========================================================
    // ===================== TARGET SEARCH =====================
    // =========================================================

    CreatureBrain FindTargetInVision()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);

        CreatureBrain closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            CreatureBrain other = hit.GetComponentInParent<CreatureBrain>();

            if (other == null || other == this || other.isDead)
                continue;

            float distance = (other.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = other;
            }
        }

        return closestTarget;
    }

    // =========================================================
    // ===================== DAMAGE ============================
    // =========================================================

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        HPBarManager.Instance.RemoveHPBar(this);

        Destroy(gameObject, 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}