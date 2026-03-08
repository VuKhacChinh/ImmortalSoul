using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CreatureBrain : MonoBehaviour
{
    [Header("Control")]
    public bool isPlayerControlled = false;

    [Header("Level")]
    public int level = 1;
    public int currentXP = 0;

    [Header("Drop")]
    public GameObject meatPrefab;
    public int dropCountMin = 1;
    public int dropCountMax = 2;

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

    [Header("Food AI")]
    public float foodSearchRange = 5f;
    public float lowHPPriority = 0.6f;
    ItemPickup targetFood;

    [Header("Obstacle Avoidance")]
    LayerMask obstacleMask;
    public float obstacleCheckDistance = 1.5f;
    Vector2 avoidDirection;
    float avoidTimer;
    const float AVOID_TIME = 1f;

    [Header("Hide System")]
    public bool isHidden = false;
    int hideZoneLayer;

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

    float scanTimer;
    float confidenceTimer;

    float cachedConfidence;

    const float SCAN_INTERVAL = 0.25f;
    const float CONFIDENCE_INTERVAL = 0.5f;

    float attackAnimTimer;
    bool damageDone;

    static Collider2D[] scanBuffer = new Collider2D[32];
    static Collider2D[] foodBuffer = new Collider2D[32];

    enum AIState
    {
        Idle,
        Wander,
        Fight,
        Flee,
        SeekFood
    }

    AIState currentState = AIState.Idle;

    Vector2 wanderDirection;
    float stateTimer;
    bool initialFlipX;

    // ===== STUCK DETECTION =====

    Vector2 lastPosition;
    float stuckTimer;
    const float STUCK_TIME = 0.4f;

    void Awake()
    {
        hideZoneLayer = LayerMask.NameToLayer("HideZone");
        obstacleMask = LayerMask.GetMask("Obstacle");
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        initialFlipX = spriteRenderer.flipX;

        currentHP = maxHP;
        displayedHP = maxHP;

        lastPosition = transform.position;
    }

    void Start()
    {
        HPBarManager.Instance.CreateHPBar(this);
        ChangeToIdle();
    }

    void Update()
    {
        if (isDead) return;

        float dt = Time.deltaTime;

        attackTimer -= dt;
        scanTimer -= dt;
        confidenceTimer -= dt;

        if (revengeTimer > 0)
            revengeTimer -= dt;
        else
            lastAttacker = null;

        if (isAttacking)
        {
            attackAnimTimer -= dt;

            if (!damageDone && attackAnimTimer <= 0.3f)
            {
                DoDamage();
                damageDone = true;
            }

            if (attackAnimTimer <= 0f)
                EndAttack();
        }

        if (isPlayerControlled)
            HandlePlayerInput();
        else
            HandleAI();

        CheckStuck();

        HandleAnimatorState();
        UpdateHPVisual();
    }

    public void SetHidden(bool value)
    {
        isHidden = value;

        if (value)
        {
            currentTarget = null;
            fleeTimer = 0;
        }

        // PLAYER
        if (isPlayerControlled)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = value ? Color.red : Color.white;

            // player luôn thấy HP bar
            HPBarManager.Instance.SetHPBarVisible(this, true);
        }
        else
        {
            spriteRenderer.enabled = !value;

            // AI ẩn thì ẩn luôn HP bar
            HPBarManager.Instance.SetHPBarVisible(this, !value);
        }
    }

    void CheckStuck()
    {
        float moveDist = ((Vector2)transform.position - lastPosition).sqrMagnitude;

        if (rb.linearVelocity.sqrMagnitude > 0.1f && moveDist < 0.00001f)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer > STUCK_TIME)
            {
                ResolveStuck();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    void ResolveStuck()
    {
        Vector2 newDir = Random.insideUnitCircle.normalized;

        if (newDir == Vector2.zero)
            newDir = Vector2.right;

        rb.linearVelocity = newDir * moveSpeed;

        wanderDirection = newDir;

        FaceDirection(newDir.x);
    }

    Vector2 AvoidObstacle(Vector2 dir)
    {
        if (avoidTimer > 0f)
        {
            avoidTimer -= Time.deltaTime;
            return avoidDirection;
        }

        RaycastHit2D forward = Physics2D.Raycast(
            transform.position,
            dir,
            obstacleCheckDistance,
            obstacleMask
        );

        if (!forward)
            return dir;

        Vector2 left = new Vector2(-dir.y, dir.x);
        Vector2 right = new Vector2(dir.y, -dir.x);

        RaycastHit2D hitLeft = Physics2D.Raycast(
            transform.position,
            left,
            obstacleCheckDistance,
            obstacleMask
        );

        RaycastHit2D hitRight = Physics2D.Raycast(
            transform.position,
            right,
            obstacleCheckDistance,
            obstacleMask
        );

        if (!hitLeft)
            avoidDirection = (dir + left).normalized;
        else if (!hitRight)
            avoidDirection = (dir + right).normalized;
        else
            avoidDirection = -dir;

        avoidTimer = AVOID_TIME;

        return avoidDirection;
    }

    void UpdateHPVisual()
    {
        if (displayedHP != currentHP)
        {
            displayedHP = Mathf.MoveTowards(displayedHP, currentHP, 60f * Time.deltaTime);
            HPBarManager.Instance.UpdateHP(this, displayedHP / maxHP);
        }
    }

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

    void HandleAI()
    {
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (fleeTimer > 0)
        {
            fleeTimer -= Time.deltaTime;
            rb.linearVelocity = fleeDirection * moveSpeed;
            FaceDirection(fleeDirection.x);
            return;
        }

        if (scanTimer <= 0f)
        {
            scanTimer = SCAN_INTERVAL;

            CreatureBrain newTarget = FindBestTarget();

            if (newTarget != null)
            {
                if (currentTarget == null)
                {
                    currentTarget = newTarget;
                }
                else
                {
                    float oldScore = EvaluateThreat(currentTarget);
                    float newScore = EvaluateThreat(newTarget);

                    if (newScore > oldScore + 10f)
                        currentTarget = newTarget;
                }
            }

            if (targetFood == null || !targetFood.gameObject.activeInHierarchy)
                targetFood = FindFood();
        }

        if (targetFood != null)
        {
            float distFood = (targetFood.transform.position - transform.position).sqrMagnitude;

            if (distFood < visionRange * visionRange)
            {
                currentState = AIState.SeekFood;
                HandleSeekFood();
                return;
            }
        }

        if (currentTarget != null)
        {
            float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            if (dist > visionRange * visionRange)
            {
                currentTarget = null;
                return;
            }

            float sqrDist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            if (sqrDist <= attackRange * attackRange)
            {
                HandleFight();
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

    void HandleSeekFood()
    {
        if (targetFood == null)
        {
            currentState = AIState.Idle;
            return;
        }

        Vector2 toFood = targetFood.transform.position - transform.position;
        float dist = toFood.sqrMagnitude;

        if (dist < 0.2f)
        {
            rb.linearVelocity = Vector2.zero;
            targetFood = null;
            return;
        }

        Vector2 dir = toFood.normalized;
        dir = AvoidObstacle(dir);
        rb.linearVelocity = dir * moveSpeed;
        FaceDirection(dir.x);
    }

    CreatureBrain FindBestTarget()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            visionRange,
            scanBuffer
        );

        CreatureBrain best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = scanBuffer[i];

            CreatureBrain other = hit.attachedRigidbody?.GetComponent<CreatureBrain>();

            if (other == null || other == this || other.isDead || other.isHidden)
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
        float dist = (transform.position - enemy.transform.position).sqrMagnitude;

        float score = 0f;

        if (level > enemy.level)
            score += 20f;

        score += (visionRange * visionRange - dist) * 0.5f;

        float myPower = currentHP * attackDamage;
        float enemyPower = enemy.currentHP * enemy.attackDamage;

        float ratio = myPower / (enemyPower + 1f);

        if (ratio > 1.3f)
            score += 20f;

        if (enemy.currentHP / enemy.maxHP < lowHPThreshold)
            score += 35f;

        if (enemy == lastAttacker)
            score += 50f;

        if (enemy.currentState == AIState.Fight)
            score += 15f;

        return score;
    }

    float CalculateConfidence()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            allyAssistRange,
            scanBuffer
        );

        int allies = 0;
        int enemies = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = scanBuffer[i];

            CreatureBrain other = hit.attachedRigidbody?.GetComponent<CreatureBrain>();

            if (other == null || other == this || other.isDead)
                continue;

            if (other.currentTarget == currentTarget)
                allies++;
            else
                enemies++;
        }

        return allies - enemies;
    }

    ItemPickup FindFood()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            foodSearchRange,
            foodBuffer
        );

        ItemPickup best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            ItemPickup food = foodBuffer[i].GetComponent<ItemPickup>();

            if (food == null)
                continue;

            float dist = (food.transform.position - transform.position).sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = food;
            }
        }

        return best;
    }

    void DecideCombatState()
    {
        float hpRatio = currentHP / maxHP;

        if (hpRatio < lowHPThreshold)
        {
            currentState = AIState.Flee;
            return;
        }

        if (confidenceTimer <= 0f)
        {
            confidenceTimer = CONFIDENCE_INTERVAL;
            cachedConfidence = CalculateConfidence();
        }

        float myPower = currentHP * attackDamage;
        float enemyPower = currentTarget.currentHP * currentTarget.attackDamage;

        float ratio = myPower / (enemyPower + 1f);
        float confidence = cachedConfidence;

        float enemySpeed = currentTarget.moveSpeed;

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

        if (enemySpeed >= moveSpeed)
        {
            currentState = AIState.Fight;
            return;
        }

        currentState = AIState.Flee;
    }

    void HandleFight()
    {
        CreatureBrain closeTarget = FindBestTarget();

        if (closeTarget != null)
        {
            float dist = (closeTarget.transform.position - transform.position).sqrMagnitude;

            if (dist < attackRange * attackRange)
                currentTarget = closeTarget;
        }

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;

        float stopDist = attackRange * 0.9f;

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
        else if (sqrDist > stopDist * stopDist)
        {
            Vector2 dir = toTarget.normalized;
            dir = AvoidObstacle(dir);
            rb.linearVelocity = dir * moveSpeed;
            FaceDirection(dir.x);
        }
    }

    void HandleFlee()
    {
        if (fleeTimer <= 0f)
        {
            fleeDirection = (transform.position - currentTarget.transform.position).normalized;
            fleeTimer = fleeDuration;
        }

        Vector2 dir = AvoidObstacle(fleeDirection);
        rb.linearVelocity = dir * moveSpeed;
        FaceDirection(dir.x);
    }

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
            Vector2 dir = AvoidObstacle(wanderDirection);
            rb.linearVelocity = dir * moveSpeed;
            FaceDirection(dir.x);

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

    void StartAttack()
    {
        if (currentTarget != null)
        {
            float dir = currentTarget.transform.position.x - transform.position.x;
            FaceDirection(dir);
        }

        isAttacking = true;
        damageDone = false;
        attackAnimTimer = 0.5f;

        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetTrigger("Attack");
        }
    }

    void DoDamage()
    {
        if (currentTarget == null) return;
        if (currentTarget.isDead) return;
        if (currentTarget.isHidden) return;

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

    public void EatItem(int xp, float heal)
    {
        GainXP(xp);

        currentHP += heal;
        currentHP = Mathf.Min(currentHP, maxHP);
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void GainXP(int xp)
    {
        if (LevelSystem.Instance == null) return;

        LevelSystem.Instance.GainXP(this, xp);
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

        DropItems();

        HPBarManager.Instance.RemoveHPBar(this);

        if (GameManager.Instance != null)
            GameManager.Instance.OnCreatureDeath(this);

        Destroy(gameObject);
    }

    void DropItems()
    {
        if (meatPrefab == null) return;

        int count = Random.Range(dropCountMin, dropCountMax + 1);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;

            Instantiate(
                meatPrefab,
                transform.position + (Vector3)offset,
                Quaternion.identity
            );
        }
    }

    void FaceDirection(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.01f) return;

        if (xDir > 0)
            spriteRenderer.flipX = initialFlipX;
        else
            spriteRenderer.flipX = !initialFlipX;
    }

    void HandleAnimatorState()
    {
        if (animator == null) return;
        if (isAttacking) return;

        float speed = rb.linearVelocity.magnitude;
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