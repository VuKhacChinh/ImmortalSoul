using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CreatureBrain : MonoBehaviour
{
    [Header("Control")]
    public bool isPlayerControlled = false;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Vision")]
    public float visionRange = 5f;

    [Header("Combat")]
    public float maxHP = 100f;
    public float currentHP;
    private float displayedHP;
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("AI")]
    public float courageOffset = 20f;

    [Header("VFX")]
    public GameObject hitEffectPrefab;

    private float attackTimer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isDead = false;
    private bool isAttacking = false;

    private CreatureBrain currentTarget;

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
        displayedHP = maxHP;
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

        if (isPlayerControlled)
            HandlePlayerInput();
        else
            HandleAI();

        UpdateFacing();
        HandleAnimatorState();
        UpdateHPVisual();
    }

    void UpdateHPVisual()
    {
        if (displayedHP > currentHP)
        {
            displayedHP = Mathf.MoveTowards(
                displayedHP,
                currentHP,
                60f * Time.deltaTime
            );

            HPBarManager.Instance.UpdateHP(this, displayedHP / maxHP);
        }
    }

    // =========================================================
    // ===================== PLAYER =============================
    // =========================================================

    void HandlePlayerInput()
    {
        if (UIController.Instance == null ||
            UIController.Instance.MovePad == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 input = UIController.Instance.MovePad.Input;
        input = Vector2.ClampMagnitude(input, 1f);

        rb.linearVelocity = input * moveSpeed;

        if (input.sqrMagnitude > 0.01f)
            FaceDirection(input.x);
    }

    public void TryAttack()
    {
        if (!isPlayerControlled) return;
        if (isDead) return;
        if (isAttacking) return;
        if (attackTimer > 0f) return;

        currentTarget = FindTargetInVision();

        if (currentTarget == null || currentTarget.isDead)
            return;

        attackTimer = attackCooldown;
        StartAttack();
    }

    public void SpawnHitEffectAt(Vector3 position)
    {
        if (hitEffectPrefab == null) return;

        Instantiate(hitEffectPrefab, position, Quaternion.identity);
    }

    void HandleAutoAttack()
    {
        if (currentTarget == null || currentTarget.isDead)
            currentTarget = FindTargetInVision();

        if (currentTarget == null) return;

        float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

        if (dist <= attackRange * attackRange)
        {
            rb.linearVelocity = Vector2.zero;

            if (attackTimer <= 0f && !isAttacking)
            {
                attackTimer = attackCooldown;
                StartAttack();
            }
        }
    }

    // =========================================================
    // ===================== AI CORE ===========================
    // =========================================================

    void HandleAI()
    {
        // 🔥 Nếu target ra khỏi vision thì bỏ target
        if (currentTarget != null)
        {
            float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            if (dist > visionRange * visionRange)
                currentTarget = null;
        }

        // tìm target mới
        if (currentTarget == null || currentTarget.isDead)
            currentTarget = FindTargetInVision();

        if (currentTarget != null)
        {
            DecideCombatState();

            if (currentState == AIState.Fight)
                HandleFight();
            else
                HandleFlee();

            return;
        }

        HandleWanderIdle();
    }

    void DecideCombatState()
    {
        if (currentTarget == null)
        {
            currentState = AIState.Idle;
            return;
        }

        float myPower = currentHP * attackDamage;
        float enemyPower = currentTarget.currentHP * currentTarget.attackDamage;

        float p = enemyPower;
        float n = courageOffset;

        bool closePower =
            myPower > (p - n) &&
            myPower < (p + n);

        bool strongerAndFaster =
            myPower > (p + n) &&
            moveSpeed > currentTarget.moveSpeed;

        if (closePower || strongerAndFaster)
            currentState = AIState.Fight;
        else
            currentState = AIState.Flee;
    }

    // =========================================================
    // ===================== FIGHT =============================
    // =========================================================

    void HandleFight()
    {
        if (currentTarget == null) return;

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;

        if (sqrDist <= attackRange * attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            FaceDirection(toTarget.x);

            if (attackTimer <= 0f && !isAttacking)
            {
                attackTimer = attackCooldown;
                StartAttack();
            }
        }
        else
        {
            Vector2 dir = toTarget.normalized;
            rb.linearVelocity = dir * moveSpeed;
            FaceDirection(dir.x);
        }
    }

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

            if (stateTimer <= 0f)
                ChangeToWander();
        }
        else // Wander
        {
            rb.linearVelocity = wanderDirection * moveSpeed;
            FaceDirection(wanderDirection.x);

            if (stateTimer <= 0f)
                ChangeToIdle();
        }
    }

    void ChangeToIdle()
    {
        currentState = AIState.Idle;
        stateTimer = Random.Range(2f, 4f);
    }

    void ChangeToWander()
    {
        currentState = AIState.Wander;
        stateTimer = Random.Range(2f, 4f);

        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-0.3f, 0.3f);

        wanderDirection = new Vector2(randomX, randomY).normalized;
    }

    // =========================================================
    // ===================== ATTACK ============================
    // =========================================================

    void StartAttack()
    {
        if (currentTarget != null)
        {
            float dir = currentTarget.transform.position.x - transform.position.x;
            FaceDirection(dir);
        }

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetTrigger("Attack");
        }

        Invoke(nameof(DoDamage), 0.2f);
        Invoke(nameof(EndAttack), 0.4f);
    }

    void DoDamage()
    {
        if (currentTarget == null) return;
        if (currentTarget.isDead) return;

        float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

        if (dist <= attackRange * attackRange)
        {
            currentTarget.TakeDamage(attackDamage);
            SpawnHitEffectAt(currentTarget.transform.position);
        }
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    // =========================================================
    // ===================== FACING ============================
    // =========================================================

    void FaceDirection(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.01f) return;

        Vector3 scale = transform.localScale;
        float absX = Mathf.Abs(scale.x);

        scale.x = xDir > 0 ? absX : -absX;
        transform.localScale = scale;
    }

    void UpdateFacing()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
            FaceDirection(rb.linearVelocity.x);
    }

    // =========================================================
    // ===================== TARGET ============================
    // =========================================================

    CreatureBrain FindTargetInVision()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);

        CreatureBrain closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            CreatureBrain other = hit.GetComponentInParent<CreatureBrain>();

            if (other == null || other == this || other.isDead)
                continue;

            float dist = (other.transform.position - transform.position).sqrMagnitude;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = other;
            }
        }

        return closest;
    }

    // =========================================================
    // ===================== DAMAGE ============================
    // =========================================================

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);

        if (currentHP <= 0)
            Die();
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("HitEffectPrefab chưa gán!");
            return;
        }

        Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        HPBarManager.Instance.RemoveHPBar(this);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCreatureDeath(this);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    void HandleAnimatorState()
    {
        if (animator == null) return;

        if (isAttacking) return; // 🔥 QUAN TRỌNG

        float speed = rb.linearVelocity.sqrMagnitude;
        animator.SetFloat("Speed", speed);
    }

}