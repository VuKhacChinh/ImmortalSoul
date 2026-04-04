using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CombatController))]
public class CreatureBrain : MonoBehaviour
{

    [Header("Control")]
    public bool isPlayerControlled = false;

    [Header("Identity")]
    public string creatureName = "Unknown";

    [Header("Tower Guardian")]
    public bool isTowerGuardian = false;

    Vector2 guardCenter;
    float guardRadius;

    public float leashRadius = 6f;  // giới hạn max

    [Header("Boss")]
    public bool isBoss = false;
    bool wasBoss = false;
    public Sprite defeatedSprite;
    bool isDefeated = false;

    [Header("Tower")]
    public bool isTower = false;
    public CreatureBrain bossPrefab;
    CreatureBrain ownerTower;

    [Header("Tower Guardians")]
    public CreatureBrain guardianPrefab;
    public int guardianCount = 3;
    float guardRadiusInner;
    bool isReturningToCenter = false;

    [Header("Level")]
    public int level = 1;
    public int currentXP = 0;
    float displayedXP;

    [Header("Drop")]
    public GameObject meatPrefab;
    public int dropCountMin = 1;
    public int dropCountMax = 2;

    [Header("Stats")]
    public CreatureStats stats;

    [Header("HP MP")]
    public RuntimeStats runtime;
    float displayedHP;
    float displayedMP;

    [Header("AI")]
    public float lowHPThreshold = 0.25f;
    public float fleeDuration = 2f;
    public float revengeMemoryDuration = 4f;

    [Header("AI Skills")]
    public float skillUseChance = 0.35f;
    public float skillRangeMultiplier = 1.3f;

    [Header("Pack")]
    public float allyAssistRange = 3f;

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
    CombatController combat;

    bool isDead = false;
    bool isAttacking = false;

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

    Material outlineMaterial;
    Material originalMaterial;

    public CreatureBrain CurrentTarget => currentTarget;
    public CombatController Combat => combat;
    public static CreatureBrain playerHighlightTarget;

    float knockbackTimer = 0f;

    public System.Action<CreatureBrain> OnDeathCallback;

    bool hasManualTarget = false;

    const float FACE_THRESHOLD = 0.2f;

    void Awake()
    {
        hideZoneLayer = LayerMask.NameToLayer("HideZone");
        obstacleMask = LayerMask.GetMask("Obstacle");
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
        outlineMaterial = Resources.Load<Material>("Materials/OutlineRed");
        initialFlipX = spriteRenderer.flipX;

        runtime = new RuntimeStats();

        runtime.HP = stats.maxHP;
        runtime.MP = stats.maxMP;

        displayedHP = runtime.HP;
        displayedMP = runtime.MP;
        displayedXP = LevelSystem.Instance != null ? LevelSystem.Instance.GetXPPercent(this) : 0f;

        lastPosition = transform.position;
        combat = GetComponent<CombatController>();
    }

    void Start()
    {
        BarManager.Instance.CreateBar(this);

        // luôn ẩn trước
        BarManager.Instance.SetHPBarVisible(this, false);

        // nếu là player thì bật lại
        if (isPlayerControlled)
        {
            BarManager.Instance.SetHPBarVisible(this, true);
        }

        ChangeToIdle();

        // ===== BOSS SPAWN DIALOG =====
        if (isBoss && SpeechBubbleSystem.Instance != null)
        {
            SpeechBubbleSystem.Instance.Say(
                "X",
                Emotion.Angry,
                3f
            );
        }

        if (isTower)
        {
            SpawnGuardians();
        }
    }

    void Update()
    {
        if (isDead) return;

        float dt = Time.deltaTime;

            // ✅ CHẶN NGAY TỪ ĐẦU
        if (knockbackTimer > 0)
        {
            knockbackTimer -= dt;
            return;
        }

        combat.Tick(dt);
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
        {
            // ===== AUTO TARGET =====
            if (!hasManualTarget && (currentTarget == null || currentTarget.IsDead()))
            {
                CreatureBrain newTarget = FindBestTargetInRange(combat.AttackRange);
                currentTarget = newTarget;
                UpdatePlayerHighlight(newTarget);
            }

            // reset manual nếu target chết
            if (hasManualTarget && (currentTarget == null || currentTarget.IsDead()))
            {
                hasManualTarget = false;
                UpdatePlayerHighlight(null);
            }

            // ===== INPUT LUÔN CHẠY =====
            Vector2 input = Vector2.zero;

            if (UIController.Instance != null && UIController.Instance.MovePad != null)
            {
                input = UIController.Instance.MovePad.Input;
                input = Vector2.ClampMagnitude(input, 1f);
            }

            // ===== LOGIC ƯU TIÊN =====
            if (hasManualTarget && currentTarget != null && !isAttacking)
            {
                float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;
                float range = combat.AttackRange;

                if (dist > range * range)
                {
                    // 👉 chỉ auto move nếu player KHÔNG kéo joystick
                    if (input.sqrMagnitude < 0.01f)
                    {
                        MoveToTarget();
                    }
                    else
                    {
                        rb.linearVelocity = input * stats.moveSpeed;
                        FaceDirection(input.x);
                    }
                }
                else
                {
                    if (input.sqrMagnitude > 0.01f)
                    {
                        rb.linearVelocity = input * stats.moveSpeed;
                        FaceDirection(input.x);
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
            }
            else
            {
                rb.linearVelocity = input * stats.moveSpeed;

                if (input.sqrMagnitude > 0.01f)
                    FaceDirection(input.x);
            }
        }
        else {
            if (isTower)
            {
                HandleTowerLogic();
            }
            else if (isTowerGuardian)
            {
                HandleTowerGuardian();
            }
            else
            {
                HandleAI();
            }
        }

        CheckStuck();

        HandleAnimatorState();
        UpdateHPVisual();
        UpdateMPVisual();
        UpdateXPVisual();

    }

    public void InitGuardian(CreatureBrain tower)
    {
        ownerTower = tower;

        guardCenter = tower.transform.position;
        guardRadius = leashRadius;

        guardRadiusInner = guardRadius * 0.5f; // buffer 80%
    }

    void SpawnGuardians()
    {
        if (!isTower || guardianPrefab == null) return;

        for (int i = 0; i < guardianCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * guardRadius;

            CreatureBrain g = Instantiate(
                guardianPrefab,
                transform.position + (Vector3)offset,
                Quaternion.identity
            );

            g.isTowerGuardian = true;
            g.isTower = false;

            // 🔥 auto gắn owner
            g.InitGuardian(this);
        }
    }

    void HandleTowerGuardian()
    {
        if (ownerTower != null)
        {
            guardCenter = ownerTower.transform.position;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distFromCenter = (transform.position - (Vector3)guardCenter).magnitude;

        // 🚫 quá gần trụ → đẩy ra ngoài
        if (distFromCenter < guardRadius * 0.3f)
        {
            Vector2 pushOut = ((Vector2)transform.position - guardCenter).normalized;
            rb.linearVelocity = pushOut * stats.moveSpeed;
            FaceDirection(pushOut.x);
            return;
        }

        // ===== ENTER RETURN MODE =====
        if (!isReturningToCenter && distFromCenter > guardRadius)
        {
            isReturningToCenter = true;
        }

        // ===== EXIT RETURN MODE =====
        if (isReturningToCenter && distFromCenter < guardRadiusInner)
        {
            isReturningToCenter = false;
        }

        CreatureBrain player = FindPlayerInRange(combat.AttackRange);

        // ===== FORCE RETURN nhưng vẫn được attack =====
        if (isReturningToCenter)
        {
            if (player != null)
            {
                currentTarget = player;

                float dist = (player.transform.position - transform.position).sqrMagnitude;

                if (dist <= combat.AttackRange * combat.AttackRange && combat.CanAttack())
                {
                    rb.linearVelocity = Vector2.zero;
                    FaceDirection(player.transform.position.x - transform.position.x);
                    combat.StartCooldown();
                    StartAttack();
                    return;
                }
            }

            Vector2 backDir = (guardCenter - (Vector2)transform.position).normalized;
            rb.linearVelocity = backDir * stats.moveSpeed;
            FaceDirection(backDir.x);
            return;
        }

        if (player != null)
        {
            currentTarget = player; // luôn cập nhật nếu thấy player
        }
        else if (currentTarget != null)
        {
            float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            if (dist > stats.visionRange * stats.visionRange)
            {
                currentTarget = null;
            }
        }

        if (currentTarget != null)
        {
            float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;
            float range = combat.AttackRange;

            if (dist <= range * range)
            {
                FaceDirection(currentTarget.transform.position.x - transform.position.x);

                if (!isAttacking && combat.CanAttack())
                {
                    rb.linearVelocity = Vector2.zero;
                    combat.StartCooldown();
                    StartAttack();
                }

                return;
            }

            // 🏃 đuổi nhưng vẫn bị leash
            Vector2 dir = (currentTarget.transform.position - transform.position).normalized;
            rb.linearVelocity = dir * stats.moveSpeed;
            FaceDirection(dir.x);
            return;
        }

        // 🔁 tuần quanh trụ
        PatrolAroundTower();
    }

    CreatureBrain FindPlayerInRange(float range)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            scanBuffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            CreatureBrain other = scanBuffer[i].GetComponentInParent<CreatureBrain>();

            if (other == null || other.IsDead() || other.isHidden)
                continue;

            if (other.isPlayerControlled)
                return other;
        }

        return null;
    }

    void PatrolAroundTower()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            stateTimer = Random.Range(2f, 4f);

            float radius = Random.Range(guardRadiusInner, guardRadius);

            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector2 targetPos = guardCenter + randomDir * radius;

            wanderDirection = (targetPos - (Vector2)transform.position).normalized;
        }

        Vector2 dir = AvoidObstacle(wanderDirection);

        rb.linearVelocity = dir * stats.moveSpeed;
        FaceDirection(dir.x);
    }

    public void SetManualTarget(CreatureBrain target)
    {
        if (!isPlayerControlled) return;
        if (target == null || target.IsDead() || target.isHidden) return;

        currentTarget = target;
        hasManualTarget = true;
        UpdatePlayerHighlight(target);
    }

    public void ClearManualTarget()
    {
        if (!isPlayerControlled) return;

        currentTarget = null;
        hasManualTarget = false;
        UpdatePlayerHighlight(null);
    }

    void HandleTowerLogic()
    {
        rb.linearVelocity = Vector2.zero;

        // 🔥 VALIDATE TARGET TRƯỚC
        if (currentTarget != null)
        {
            if (currentTarget.IsDead() || currentTarget.isHidden)
            {
                currentTarget = null;
            }
            else
            {
                float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

                if (dist > stats.visionRange * stats.visionRange)
                {
                    currentTarget = null;
                }
            }
        }

        // 🔥 LUÔN TÌM LẠI TARGET NẾU NULL
        if (currentTarget == null)
        {
            currentTarget = FindBestTargetInRange(stats.visionRange);
        }

        if (currentTarget == null) return;

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;
        float range = combat.AttackRange;

        if (sqrDist <= range * range)
        {
            FaceDirection(toTarget.x);

            if (!isAttacking && combat.CanAttack())
            {
                combat.StartCooldown();
                StartAttack();
            }
        }
    }

    public void ApplyKnockback(float duration)
    {
        knockbackTimer = duration;
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
            SetHiddenHighlight(value);
            
            spriteRenderer.enabled = true;

            // player luôn thấy HP bar
            BarManager.Instance.SetHPBarVisible(this, true);
        }
        else
        {
            spriteRenderer.enabled = !value;

            // AI ẩn thì ẩn luôn HP bar
            BarManager.Instance.SetHPBarVisible(this, !value);
        }
    }

    void SetHiddenHighlight(bool value)
    {
        if (spriteRenderer == null) return;

        if (value)
            spriteRenderer.material = outlineMaterial;
        else
            spriteRenderer.material = originalMaterial;
    }

    void UpdatePlayerHighlight(CreatureBrain newTarget)
    {
        if (!isPlayerControlled) return;

        // clear target cũ nếu còn tồn tại
        if (playerHighlightTarget != null)
        {
            BarManager.Instance.SetHPBarVisible(playerHighlightTarget, false);
        }

        playerHighlightTarget = newTarget;

        if (playerHighlightTarget != null && !playerHighlightTarget.IsDead())
        {
            BarManager.Instance.SetHPBarVisible(playerHighlightTarget, true);
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

        rb.linearVelocity = newDir * stats.moveSpeed;

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
        if (displayedHP != runtime.HP)
        {
            displayedHP = Mathf.MoveTowards(displayedHP, runtime.HP, 60f * Time.deltaTime);
            BarManager.Instance.UpdateHP(this, displayedHP / stats.maxHP);
        }
    }

    void UpdateMPVisual()
    {
        if (displayedMP != runtime.MP)
        {
            displayedMP = Mathf.MoveTowards(displayedMP, runtime.MP, 60f * Time.deltaTime);

            BarManager.Instance.UpdateMP(this, displayedMP / stats.maxMP);
        }
    }

    void UpdateXPVisual()
    {
        if (LevelSystem.Instance == null) return;

        float targetXP = LevelSystem.Instance.GetXPPercent(this);

        if (displayedXP != targetXP)
        {
            if (targetXP < displayedXP)
                displayedXP = targetXP;
            
            displayedXP = Mathf.MoveTowards(displayedXP, targetXP, 1.5f * Time.deltaTime);

            BarManager.Instance.UpdateXP(this, displayedXP);
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

        rb.linearVelocity = input * stats.moveSpeed;

        if (input.sqrMagnitude > 0.01f)
            FaceDirection(input.x);
    }

    public void TryAttack()
    {
        if (!isPlayerControlled) return;
        if (isDead) return;
        if (isAttacking) return;
        if (!combat.CanAttack()) return;

        if (currentTarget == null) return;

        float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;
        float range = combat.AttackRange;

        // ❗ Nếu ngoài tầm → KHÔNG bắn
        if (dist > range * range)
        {
            // 👉 nếu là manual target → tiến lại gần
            if (hasManualTarget)
            {
                MoveToTarget();
            }
            else
            {
                // auto target thì bỏ luôn
                currentTarget = null;
                UpdatePlayerHighlight(null);
            }

            return;
        }

        // ✅ Trong tầm → mới được bắn
        combat.StartCooldown();
        StartAttack();
    }

    void MoveToTarget()
    {
        if (currentTarget == null) return;

        Vector2 dir = (currentTarget.transform.position - transform.position).normalized;

        dir = AvoidObstacle(dir);

        rb.linearVelocity = dir * stats.moveSpeed;

        FaceDirection(dir.x);
    }

    public static void ResetPlayerHighlight()
    {
        if (playerHighlightTarget != null)
        {
            BarManager.Instance.SetHPBarVisible(playerHighlightTarget, false);
            playerHighlightTarget = null;
        }
    }

    bool TryUseAISkill()
    {
        if (combat == null)
            return false;

        if (currentTarget == null)
            return false;

        float dist = (currentTarget.transform.position - transform.position).magnitude;

        for (int i = 0; i < 3; i++)
        {
            SkillDefinition skill = combat.GetSkill(i);

            if (skill == null)
                continue;

            if (!combat.CanUseSkill(i))
                continue;

            float range = skill.range * skillRangeMultiplier;

            if (dist > range)
                continue;

            FaceDirection(currentTarget.transform.position.x - transform.position.x);

            combat.UseSkill(i);

            return true;
        }

        return false;
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
            rb.linearVelocity = fleeDirection * stats.moveSpeed;
            FaceDirection(fleeDirection.x);
            return;
        }

        if (scanTimer <= 0f)
        {
            scanTimer = SCAN_INTERVAL;

            AutoTarget();
            CreatureBrain newTarget = FindBestTargetInRange(stats.visionRange);

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

            if (distFood < stats.visionRange * stats.visionRange)
            {
                currentState = AIState.SeekFood;
                HandleSeekFood();
                return;
            }
        }

        if (currentTarget != null)
        {
            float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            if (dist > stats.visionRange * stats.visionRange)
            {
                currentTarget = null;
                return;
            }

            float sqrDist = (currentTarget.transform.position - transform.position).sqrMagnitude;

            float range = combat.AttackRange;

            if (sqrDist <= range * range)
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
        rb.linearVelocity = dir * stats.moveSpeed;
        FaceDirection(dir.x);
    }

    public CreatureBrain FindBestTargetInRange(float range)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            scanBuffer
        );

        CreatureBrain best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            CreatureBrain other = scanBuffer[i].GetComponentInParent<CreatureBrain>();

            if (other == null || other == this || other.IsDead() || other.isHidden)
                continue;

            if (isBoss)
            {
                if (!other.isPlayerControlled)
                    continue;
            }

            else if (!isPlayerControlled)
            {
                if (other.isBoss || other.isTower || other.isTowerGuardian)
                    continue;
            }

            float score = EvaluateThreat(other);

            if (score > bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        return best;
    }

    void AutoTarget()
    {
        if (currentTarget == null || currentTarget.IsDead() || currentTarget.isHidden)
        {
            currentTarget = FindBestTargetInRange(combat.AttackRange);
            return;
        }

        float dist = (currentTarget.transform.position - transform.position).sqrMagnitude;

        if (dist > stats.visionRange * stats.visionRange)
        {
            currentTarget = FindBestTargetInRange(combat.AttackRange);
        }
    }

    float EvaluateThreat(CreatureBrain enemy)
    {
        float dist = (transform.position - enemy.transform.position).sqrMagnitude;

        float score = 0f;

        if (level > enemy.level)
            score += 20f;

        score += (stats.visionRange * stats.visionRange - dist) * 0.5f;

        float myPower = runtime.HP * stats.attackDamage;
        float enemyPower = enemy.runtime.HP * enemy.stats.attackDamage;

        float ratio = myPower / (enemyPower + 1f);

        if (ratio > 1.3f)
            score += 20f;

        if (enemy.runtime.HP / enemy.stats.maxHP < lowHPThreshold)
            score += 35f;

        if (enemy == lastAttacker)
            score += 50f;

        if (enemy.currentState == AIState.Fight)
            score += 15f;

        return score;
    }

    public void OnPossessed(bool hasPossessedBefore)
    {
        if (SpeechBubbleSystem.Instance == null)
            return;

        // ===== LẦN ĐẦU =====
        // if (!hasPossessedBefore)
        // {
        //     SpeechBubbleSystem.Instance.Say(
        //         "I've escaped! Those bastards dared to destroy my demon lord's body! I will definitely get revenge!",
        //         Emotion.Normal,
        //         3f
        //     );
        //     return;
        // }

        // ===== ƯU TIÊN BOSS (KỂ CẢ ĐÃ BỊ CHIẾM) =====
        // if (wasBoss)
        // {
        //     SpeechBubbleSystem.Instance.Say(
        //         $"Even {creatureName} would have to lose to me, haha.",
        //         Emotion.Happy,
        //         3f
        //     );
        //     return;
        // }

        // ===== NHỮNG LẦN SAU =====
        string msg = $"Thanks to the soul stone, which caused my harm, my soul is now immortal, and this body is... {creatureName}";

        SpeechBubbleSystem.Instance.Say(
            msg,
            Emotion.Happy,
            2.5f
        );
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
        float hpRatio = runtime.HP / stats.maxHP;

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

        float myPower = runtime.HP * stats.attackDamage;
        float enemyPower = currentTarget.runtime.HP * currentTarget.stats.attackDamage;

        float ratio = myPower / (enemyPower + 1f);
        float confidence = cachedConfidence;

        float enemySpeed = currentTarget.stats.moveSpeed;

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

        if (enemySpeed >= stats.moveSpeed)
        {
            currentState = AIState.Fight;
            return;
        }

        currentState = AIState.Flee;
    }

    void HandleFight()
    {
        if (currentTarget == null || currentTarget.IsDead())
        {
            currentTarget = FindBestTargetInRange(combat.AttackRange);

            if (currentTarget == null)
                return;
        }

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;

        float attackRange = combat.AttackRange;

        if (sqrDist <= attackRange * attackRange)
        {
            // luôn quay mặt
            FaceDirection(toTarget.x);

            // thử dùng skill
            if (!isAttacking && TryUseAISkill())
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // attack nếu có thể
            if (!isAttacking && combat.CanAttack())
            {
                rb.linearVelocity = Vector2.zero;

                combat.StartCooldown();
                StartAttack();
                return;
            }

            // ❗ QUAN TRỌNG: đang trong range nhưng chưa đánh được → đứng yên
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ===== DI CHUYỂN TỚI TARGET =====
        Vector2 dir = toTarget.normalized;
        dir = AvoidObstacle(dir);

        rb.linearVelocity = dir * stats.moveSpeed;

        FaceDirection(dir.x);
    }

    void HandleFlee()
    {
        if (fleeTimer <= 0f)
        {
            fleeDirection = (transform.position - currentTarget.transform.position).normalized;
            fleeTimer = fleeDuration;
        }

        Vector2 dir = AvoidObstacle(fleeDirection);
        rb.linearVelocity = dir * stats.moveSpeed;
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
            rb.linearVelocity = dir * stats.moveSpeed;
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
        combat.DoDamage();
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    public void TakeDamage(float dmg, CreatureBrain attacker)
    {
        if (isDead) return;

        if (isBoss && !attacker.isPlayerControlled)
            return;

        runtime.HP -= dmg;
        runtime.HP = Mathf.Max(runtime.HP, 0);

        lastAttacker = attacker;
        revengeTimer = revengeMemoryDuration;

        if (runtime.HP <= 0)
            Die();
    }

    public void EatItem(int xp, float heal)
    {
        GainXP(xp);

        runtime.HP += heal;
        runtime.HP = Mathf.Min(runtime.HP, stats.maxHP);
    }

    public bool IsDead()
    {
        return isDead || isDefeated;
    }

    public void GainXP(int xp)
    {
        if (LevelSystem.Instance == null) return;

        LevelSystem.Instance.GainXP(this, xp);
    }

    public void ReviveForPossession()
    {
        isDead = false;
        isDefeated = false;

        // ===== LƯU TRẠNG THÁI BOSS =====
        wasBoss = true;
        isBoss = false;

        runtime.HP = stats.maxHP;
        runtime.MP = stats.maxMP;

        Combat.enabled = true;
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        // player chết → mất 1 linh hồn
        if (isPlayerControlled && SoulManager.Instance != null)
        {
            SoulManager.Instance.ConsumeSoul();
        }

        rb.linearVelocity = Vector2.zero;

        if (isTower)
        {
            OnDeathCallback?.Invoke(this);
            SpawnBoss();
            Destroy(gameObject);
            return;
        }

        // =========================
        // BOSS LOGIC
        // =========================
        if (isBoss)
        {
            EnterDefeatedState();
            return;
        }

        // =========================
        // NORMAL CREATURE
        // =========================

        DropItems();

        BarManager.Instance.RemoveHPBar(this);

        if (GameManager.Instance != null)
            GameManager.Instance.OnCreatureDeath(this);

        Destroy(gameObject);
    }

    void SpawnBoss()
    {
        if (bossPrefab == null) return;

        CreatureBrain boss = Instantiate(
            bossPrefab,
            transform.position,
            Quaternion.identity
        );

        boss.isBoss = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterExternalCreature(boss);

            // 🔥 SPAWN EFFECT KHI BOSS XUẤT HIỆN
            GameManager.Instance.PlayBossSpawnEffect(boss);
        }
    }

    void EnterDefeatedState()
    {
        isDefeated = true;

        if (spriteRenderer != null && defeatedSprite != null)
            spriteRenderer.sprite = defeatedSprite;

        rb.linearVelocity = Vector2.zero;

        Combat.enabled = false;
        isPlayerControlled = false;

        BarManager.Instance.RefreshBar(this);
        
        if (GameManager.Instance != null)
            GameManager.Instance.OnBossDefeated(this);
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
        if (Mathf.Abs(xDir) < FACE_THRESHOLD) return;

        if (xDir > 0)
            spriteRenderer.flipX = initialFlipX;
        else
            spriteRenderer.flipX = !initialFlipX;
    }

    public Vector2 GetFacingDirection()
    {
        if (spriteRenderer.flipX == initialFlipX)
            return Vector2.right;
        else
            return Vector2.left;
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
        Gizmos.DrawWireSphere(transform.position, stats.visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combat.AttackRange);
    }

    void OnDestroy()
    {
        if (playerHighlightTarget == this)
        {
            playerHighlightTarget = null;
        }

        if (BarManager.Instance != null)
            BarManager.Instance.RemoveHPBar(this);
    }
}