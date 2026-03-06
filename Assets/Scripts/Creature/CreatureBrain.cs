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
    float displayedHP;

    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("AI")]
    public float lowHPThreshold = 0.25f;
    public float fleeDuration = 2f;
    public float revengeMemoryDuration = 4f;

    [Header("Pack")]
    public float allyAssistRange = 3f;

    [Header("VFX")]
    public GameObject hitEffectPrefab;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;

    bool isDead = false;
    bool isAttacking = false;

    float attackTimer;

    CreatureBrain currentTarget;

    CreatureBrain lastAttacker;
    float revengeTimer;

    Vector2 fleeDirection;
    float fleeTimer;

    enum AIState
    {
        Idle,
        Wander,
        Fight,
        Flee
    }

    AIState currentState = AIState.Idle;

    Vector2 wanderDirection;
    float stateTimer;

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

        if (revengeTimer > 0)
            revengeTimer -= Time.deltaTime;
        else
            lastAttacker = null;

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
            displayedHP = Mathf.MoveTowards(displayedHP, currentHP, 60f * Time.deltaTime);
            HPBarManager.Instance.UpdateHP(this, displayedHP / maxHP);
        }
    }

    // PLAYER CONTROL

    void HandlePlayerInput()
    {
        if (UIController.Instance == null || UIController.Instance.MovePad == null)
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

        currentTarget = FindBestTarget();

        if (currentTarget == null || currentTarget.isDead)
            return;

        attackTimer = attackCooldown;
        StartAttack();
    }

    // AI

    void HandleAI()
    {
        if (fleeTimer > 0)
        {
            fleeTimer -= Time.deltaTime;
            rb.linearVelocity = fleeDirection * moveSpeed;
            FaceDirection(fleeDirection.x);
            return;
        }

        if (currentTarget == null || currentTarget.isDead)
            currentTarget = FindBestTarget();

        if (currentTarget != null)
        {
            float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            if (dist > visionRange * visionRange)
            {
                currentTarget = null;
                return;
            }

            DecideCombatState();

            if (currentState == AIState.Fight)
                HandleFight();
            else
                HandleFlee();

            return;
        }

        HandleWanderIdle();
    }

    // TARGET SELECTION (SMART)

    CreatureBrain FindBestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);

        CreatureBrain best = null;
        float bestScore = float.MinValue;

        foreach (var hit in hits)
        {
            CreatureBrain other = hit.GetComponentInParent<CreatureBrain>();

            if (other == null || other == this || other.isDead)
                continue;

            float score = EvaluateThreat(other);

            if (score > bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        return best;
    }

    float EvaluateThreat(CreatureBrain enemy)
    {
        float dist = Vector2.Distance(transform.position, enemy.transform.position);

        float score = 0f;

        score += (visionRange - dist) * 2f;

        float myPower = EvaluatePower();
        float enemyPower = enemy.EvaluatePower();

        float ratio = myPower / (enemyPower + 1f);

        if (ratio > 1.3f)
            score += 20f;

        if (enemy.currentHP / enemy.maxHP < lowHPThreshold)
            score += 35f;

        if (enemy == lastAttacker)
            score += 50f;

        if (IsEnemyFighting(enemy))
            score += 15f;

        return score;
    }

    bool IsEnemyFighting(CreatureBrain enemy)
    {
        return enemy.currentState == AIState.Fight;
    }

    // POWER

    float EvaluatePower()
    {
        return currentHP * attackDamage;
    }

    float CalculateConfidence()
    {
        int allies = 0;
        int enemies = 0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, allyAssistRange);

        foreach (var hit in hits)
        {
            CreatureBrain other = hit.GetComponentInParent<CreatureBrain>();

            if (other == null || other == this || other.isDead)
                continue;

            if (other.currentTarget == currentTarget)
                allies++;
            else
                enemies++;
        }

        return allies - enemies;
    }

    void DecideCombatState()
    {
        float myPower = EvaluatePower();
        float enemyPower = currentTarget.EvaluatePower();

        float ratio = myPower / (enemyPower + 1f);

        float confidence = CalculateConfidence();

        if (ratio >= 1.2f)
        {
            currentState = AIState.Fight;
            return;
        }

        if (confidence > 1)
        {
            currentState = AIState.Fight;
            return;
        }

        if (ratio > 0.8f)
        {
            currentState = AIState.Fight;
            return;
        }

        currentState = AIState.Flee;
    }

    // FIGHT

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

    // FLEE

    void HandleFlee()
    {
        if (currentTarget == null) return;

        fleeDirection = (transform.position - currentTarget.transform.position).normalized;
        fleeTimer = fleeDuration;

        rb.linearVelocity = fleeDirection * moveSpeed;
        FaceDirection(fleeDirection.x);
    }

    // WANDER

    void HandleWanderIdle()
    {
        stateTimer -= Time.deltaTime;

        if (currentState == AIState.Idle)
        {
            rb.linearVelocity = Vector2.zero;

            if (stateTimer <= 0f)
                ChangeToWander();
        }
        else
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

    // ATTACK

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
            currentTarget.TakeDamage(attackDamage, this);
            SpawnHitEffectAt(currentTarget.transform.position);
        }
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    // DAMAGE

    public void TakeDamage(float dmg, CreatureBrain attacker)
    {
        if (isDead) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);

        lastAttacker = attacker;
        revengeTimer = revengeMemoryDuration;

        if (currentHP <= 0)
            Die();
    }

    void SpawnHitEffectAt(Vector3 pos)
    {
        if (hitEffectPrefab == null) return;
        Instantiate(hitEffectPrefab, pos, Quaternion.identity);
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        HPBarManager.Instance.RemoveHPBar(this);

        if (GameManager.Instance != null)
            GameManager.Instance.OnCreatureDeath(this);

        Destroy(gameObject);
    }

    // FACING

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

    void HandleAnimatorState()
    {
        if (animator == null) return;
        if (isAttacking) return;

        float speed = rb.linearVelocity.sqrMagnitude;
        animator.SetFloat("Speed", speed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}