using System.Collections;
using UnityEngine;

public enum MonsterAIState
{
    Patrol,
    Chase,
    Attack,
    Hurt,
    Dead
}

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterMotor2D))]
[RequireComponent(typeof(MonsterCombat))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(Animator))]
public sealed class MonsterController : MonoBehaviour
{
    [Header("AI")]
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float attackRange = 1.25f;
    [SerializeField] private bool useSecondaryAttack;
    [SerializeField, Min(0.1f)] private float secondaryAttackRange = 2.4f;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private string startupStationaryStateName;
    [Header("Mimic")]
    [SerializeField] private bool startDormant;
    [SerializeField, Min(0f)] private float initialMimicLungeDistance = 0.9f;
    [SerializeField, Min(0.01f)] private float initialMimicLungeDuration = 0.2f;
    [Header("Hit Reaction")]
    [SerializeField, Range(0f, 1f)] private float counterAttackChanceOnHit = 0.33f;
    [SerializeField, Min(0.01f)] private float hurtDuration = 0.43f;
    [SerializeField, Min(0f)] private float destroyDelay = 1.05f;
    [SerializeField, Min(0f)] private float flightVerticalRange = 1.5f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0f)] private float deathGravityScale = 1f;
    [Header("Attack pairs (DemonSlime)")]
    [SerializeField] private bool useAttackPairs;
    [UnityEngine.Serialization.FormerlySerializedAs("chaseBetweenPairsDuration")]
    [SerializeField, Min(0f), Tooltip("Minimum interval between the start of consecutive attack pairs.")]
    private float attackPairInterval = 5f;
    private int pairAttacksRemaining;
    private bool waitingForPairAttackEnd;
    private float chasePairUntil;
    private BossEncounter pairEncounter;
    public int PairAttacksRemaining => pairAttacksRemaining;
    public float PairChaseSecondsRemaining => Mathf.Max(0f, chasePairUntil - Time.time);

    private MonsterMotor2D motor;
    private MonsterCombat combat;
    private MonsterHealth health;
    private Animator animator;
    private Transform player;
    private Health playerHealth;
    private Vector3 startPosition;
    private MonsterAIState currentState = MonsterAIState.Patrol;
    private int patrolDirection = 1;
    private float actionTimer;
    private float detectionRangeSquared;
    private float attackRangeSquared;
    private bool waitingForGroundAfterDeath;
    private bool attackAnimationObserved;
    private bool deathQueuedUntilAttackCompletes;
    private bool isDormant;
    private int currentAttackVariant = 1;
    private int startupStationaryStateHash;

    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");
    private static readonly int TakeHitState = Animator.StringToHash("Base Layer.TakeHit");
    private static readonly int Attack1State = Animator.StringToHash("Base Layer.Attack1");
    private static readonly int Attack2State = Animator.StringToHash("Base Layer.Attack2");
    private static readonly int DeathCapitalState = Animator.StringToHash("Base Layer.Death");
    private static readonly int DeathLowercaseState = Animator.StringToHash("Base Layer.death");
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int TakeHitParameter = Animator.StringToHash("TakeHit");
    private static readonly int DeathParam = Animator.StringToHash("Death");
    private bool animatorHasMovingParameter;
    private bool animatorHasAttackTrigger;
    private bool animatorHasTakeHitTrigger;
    private bool animatorHasDeathParameter;

    public MonsterAIState CurrentState => currentState;

    private void Awake()
    {
        pairEncounter = GetComponent<BossEncounter>();
        motor = GetComponent<MonsterMotor2D>();
        combat = GetComponent<MonsterCombat>();
        health = GetComponent<MonsterHealth>();
        animator = GetComponent<Animator>();
        AnimatorControllerParameter[] animatorParameters = animator.parameters;
        for (int i = 0; i < animatorParameters.Length; i++)
        {
            if (animatorParameters[i].nameHash == IsMovingParameter &&
                animatorParameters[i].type == AnimatorControllerParameterType.Bool)
                animatorHasMovingParameter = true;
            else if (animatorParameters[i].nameHash == AttackParameter &&
                     animatorParameters[i].type == AnimatorControllerParameterType.Trigger)
                animatorHasAttackTrigger = true;
            else if (animatorParameters[i].nameHash == TakeHitParameter &&
                     animatorParameters[i].type == AnimatorControllerParameterType.Trigger)
                animatorHasTakeHitTrigger = true;
            else if (animatorParameters[i].nameHash == DeathParam &&
                     animatorParameters[i].type == AnimatorControllerParameterType.Bool)
                animatorHasDeathParameter = true;
        }

        startPosition = transform.position;
        isDormant = startDormant;
        detectionRangeSquared = detectionRange * detectionRange;
        attackRangeSquared = attackRange * attackRange;
        if (!string.IsNullOrWhiteSpace(startupStationaryStateName))
            startupStationaryStateHash = Animator.StringToHash($"Base Layer.{startupStationaryStateName.Trim()}");

        player = playerTarget;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += ReactToHit;
            health.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= ReactToHit;
            health.OnDied -= HandleDeath;
        }
    }

    private void Update()
    {
        if (health != null && health.IsSimulationSuspended)
            return;

        if (isDormant)
        {
            Stop();
            return;
        }

        if (useAttackPairs && waitingForPairAttackEnd && combat != null && !combat.IsAttacking)
            CompletePairAttack();

        if (combat != null && combat.IsAttacking)
        {
            currentState = MonsterAIState.Attack;
            Stop();

            int attackStateHash = GetCurrentAttackStateHash();
            bool hasAttackAnimation = animator != null && animator.enabled && attackStateHash != 0;
            combat.Tick(IsAttackAnimationComplete(), !hasAttackAnimation);
            if (useAttackPairs && waitingForPairAttackEnd && !combat.IsAttacking)
                CompletePairAttack();
            if (!combat.IsAttacking && deathQueuedUntilAttackCompletes)
                HandleDeath();
            return;
        }

        if (health == null || health.IsDead || currentState == MonsterAIState.Dead)
        {
            Stop();
            return;
        }

        if (currentState == MonsterAIState.Hurt)
        {
            Stop();
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
                EndHurt();
            return;
        }

        if (IsInStartupStationaryAnimation())
        {
            currentState = MonsterAIState.Patrol;
            Stop();
            return;
        }

        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            Patrol();
            return;
        }

        Vector2 offsetToPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distanceToPlayerSquared = offsetToPlayer.sqrMagnitude;
        if (useAttackPairs && Time.time < chasePairUntil)
        {
            if (distanceToPlayerSquared > attackRangeSquared) ChasePlayer();
            else { currentState = MonsterAIState.Chase; Stop(); }
            return;
        }
        int attackVariant = 1;
        bool inPrimaryAttackRange = distanceToPlayerSquared <= attackRangeSquared;
        bool inSecondaryAttackRange = useSecondaryAttack &&
                                      distanceToPlayerSquared <= secondaryAttackRange * secondaryAttackRange;
        if (inPrimaryAttackRange || inSecondaryAttackRange)
        {
            currentState = MonsterAIState.Attack;
            Stop();
            if (Mathf.Abs(offsetToPlayer.x) > 0.001f)
                motor.FaceDirection(offsetToPlayer.x > 0f ? 1 : -1);
            attackVariant = inPrimaryAttackRange ? 1 : 2;
            if (combat.TryStartAttack(player, playerHealth, attackVariant))
            {
                if (useAttackPairs)
                {
                    pairAttacksRemaining = 2;
                    waitingForPairAttackEnd = true;
                    chasePairUntil = Time.time + attackPairInterval;
                }
                currentAttackVariant = attackVariant;
                attackAnimationObserved = false;
                PlayAttackAnimation(attackVariant);
            }
        }
        else if (distanceToPlayerSquared <= detectionRangeSquared ||
            (useAttackPairs && pairEncounter != null && pairEncounter.IsPlayerInside))
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void CompletePairAttack()
    {
        waitingForPairAttackEnd = false;
        if (health == null || health.IsDead) return;
        pairAttacksRemaining = Mathf.Max(0, pairAttacksRemaining - 1);
        if (pairAttacksRemaining > 0 && player != null && playerHealth != null && !playerHealth.IsDead &&
            combat.TryStartCounterAttack(player, playerHealth))
        {
            // The second swing bypasses the normal cooldown but retains its own hit window.
            motor.FaceDirection(player.position.x >= transform.position.x ? 1 : -1);
            currentAttackVariant = 1;
            attackAnimationObserved = false;
            waitingForPairAttackEnd = true;
            currentState = MonsterAIState.Attack;
            Stop();
            PlayAttackAnimation(1, true);
            return;
        }
        pairAttacksRemaining = 0;
    }

    private void Patrol()
    {
        currentState = MonsterAIState.Patrol;
        float distanceFromStart = transform.position.x - startPosition.x;
        if (patrolDirection == 1 && distanceFromStart >= patrolDistance)
            patrolDirection = -1;
        else if (patrolDirection == -1 && distanceFromStart <= -patrolDistance)
            patrolDirection = 1;

        if (motor.IsFlying)
            motor.SetDesiredVelocity(new Vector2(patrolDirection * motor.PatrolSpeed, 0f));
        else
            motor.SetDesiredHorizontalSpeed(patrolDirection * motor.PatrolSpeed);

        motor.FaceDirection(patrolDirection);
        SetMovingAnimation(Mathf.Abs(motor.DesiredHorizontalSpeed) > 0.01f);
    }

    private void ChasePlayer()
    {
        currentState = MonsterAIState.Chase;
        if (!motor.IsFlying)
        {
            int direction = player.position.x > transform.position.x ? 1 : -1;
            motor.SetDesiredHorizontalSpeed(direction * motor.ChaseSpeed);
            motor.FaceDirection(direction);
            SetMovingAnimation(Mathf.Abs(motor.DesiredHorizontalSpeed) > 0.01f);
            return;
        }

        float horizontalRange = Mathf.Max(0f, patrolDistance);
        float verticalRange = Mathf.Max(0f, flightVerticalRange);
        Vector2 boundedTarget = new Vector2(
            Mathf.Clamp(player.position.x, startPosition.x - horizontalRange, startPosition.x + horizontalRange),
            Mathf.Clamp(player.position.y, startPosition.y - verticalRange, startPosition.y + verticalRange));
        Vector2 offset = boundedTarget - (Vector2)transform.position;
        Vector2 velocity = offset.sqrMagnitude > 0.0025f
            ? offset.normalized * motor.ChaseSpeed
            : Vector2.zero;

        motor.SetDesiredVelocity(velocity);
        if (Mathf.Abs(offset.x) > 0.001f)
            motor.FaceDirection(offset.x > 0f ? 1 : -1);
        SetMovingAnimation(velocity.sqrMagnitude > 0.0001f);
    }

    private void Stop()
    {
        motor.SetDesiredVelocity(Vector2.zero);
        SetMovingAnimation(false);
    }

    private bool IsInStartupStationaryAnimation()
    {
        if (startupStationaryStateHash == 0)
            return false;

        if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == startupStationaryStateHash)
            return true;

        return animator.IsInTransition(0) &&
               animator.GetNextAnimatorStateInfo(0).fullPathHash == startupStationaryStateHash;
    }

    private void SetMovingAnimation(bool isMoving)
    {
        if (animatorHasMovingParameter)
            animator.SetBool(IsMovingParameter, isMoving);
    }

    // Legacy Animation Event bridge for the attack impact frame.
    public void DealDamageToPlayer() => combat.ApplyAttackDamageFromAnimation();

    // Legacy Animation Event bridge for the final frame of Attack.
    public void EndAttack() => combat.EndAttack();

    public void ReactToHit()
    {
        if (isDormant || health == null || health.IsDead || currentState == MonsterAIState.Dead)
            return;

        if (combat != null && combat.IsAttacking)
            return;

        combat.CancelAttack();
        if (TryStartCounterAttackOnHit())
            return;

        currentState = MonsterAIState.Hurt;
        actionTimer = hurtDuration;
        Stop();
        if (animatorHasTakeHitTrigger)
        {
            animator.ResetTrigger(TakeHitParameter);
            animator.SetTrigger(TakeHitParameter);
        }
        else if (animator.HasState(0, TakeHitState))
        {
            animator.Play(TakeHitState, 0, 0f);
        }
        else if (animator.HasState(0, HurtState))
        {
            animator.CrossFadeInFixedTime(HurtState, 0.03f, 0, 0f);
        }
    }

    public void ActivateFromMimic(Transform target)
    {
        if (!isDormant || health == null || health.IsDead)
            return;

        isDormant = false;
        startPosition = transform.position;
        player = target;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        playerHealth = player != null ? player.GetComponent<Health>() : null;
        currentAttackVariant = 1;
        if (player == null || playerHealth == null || playerHealth.IsDead ||
            !combat.TryStartAttack(player, playerHealth, 1))
        {
            currentState = MonsterAIState.Patrol;
            return;
        }

        int direction = player.position.x >= transform.position.x ? 1 : -1;
        motor.FaceDirection(direction);
        motor.BeginLunge(direction, initialMimicLungeDistance, initialMimicLungeDuration);
        currentState = MonsterAIState.Attack;
        attackAnimationObserved = false;
        PlayAttackAnimation(1, true);
    }

    private bool TryStartCounterAttackOnHit()
    {
        if (useAttackPairs) return false;
        float counterAttackChance = Mathf.Clamp01(counterAttackChanceOnHit);
        if (player == null || playerHealth == null || playerHealth.IsDead || counterAttackChance <= 0f ||
            (counterAttackChance < 1f && Random.value >= counterAttackChance))
            return false;

        motor.FaceDirection(player.position.x >= transform.position.x ? 1 : -1);
        Stop();
        if (!combat.TryStartCounterAttack(player, playerHealth))
            return false;

        currentState = MonsterAIState.Attack;
        currentAttackVariant = 1;
        actionTimer = 0f;
        attackAnimationObserved = false;
        PlayAttackAnimation(1, true);
        return true;
    }

    private bool IsAttackAnimationComplete()
    {
        if (animator == null || !animator.enabled)
            return false;

        int attackStateHash = GetCurrentAttackStateHash();
        if (attackStateHash == 0)
            return false;

        if (animator.IsInTransition(0) &&
            animator.GetNextAnimatorStateInfo(0).fullPathHash == attackStateHash)
        {
            attackAnimationObserved = true;
            return false;
        }

        AnimatorStateInfo currentAnimation = animator.GetCurrentAnimatorStateInfo(0);
        if (currentAnimation.fullPathHash == attackStateHash)
        {
            attackAnimationObserved = true;
            return currentAnimation.normalizedTime >= 1f;
        }

        return attackAnimationObserved;
    }

    // Legacy Animation Event bridge for the final frame of Hurt.
    public void EndHurt()
    {
        if (currentState != MonsterAIState.Hurt)
            return;

        currentState = MonsterAIState.Patrol;
        actionTimer = 0f;
    }

    private void HandleDeath()
    {
        if (currentState == MonsterAIState.Dead)
            return;

        if (combat != null && combat.IsAttacking)
        {
            deathQueuedUntilAttackCompletes = true;
            return;
        }

        deathQueuedUntilAttackCompletes = false;

        currentState = MonsterAIState.Dead;
        combat.CancelAttack();
        bool shouldFallToGround = motor.IsFlying;
        if (shouldFallToGround)
        {
            waitingForGroundAfterDeath = true;
            motor.BeginDeathFall(deathGravityScale);
        }
        else
        {
            motor.StopAndDisablePhysics();
        }

        if (animatorHasDeathParameter)
            animator.SetBool(DeathParam, true);
        int deathStateHash = animator.HasState(0, DeathCapitalState)
            ? DeathCapitalState
            : DeathLowercaseState;
        if (animator.HasState(0, deathStateHash))
            animator.Play(deathStateHash, 0, 0f);

        if (!shouldFallToGround)
            StartCoroutine(DestroyAfterDeathAnimation(deathStateHash));
    }

    private IEnumerator DestroyAfterDeathAnimation(int stateHash)
    {
        yield return DeathAnimationPlayback.WaitForCompletion(animator, stateHash, destroyDelay);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DestroyAfterGroundContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DestroyAfterGroundContact(collision);
    }

    private void DestroyAfterGroundContact(Collision2D collision)
    {
        if (!waitingForGroundAfterDeath || collision == null)
            return;

        int collisionLayer = collision.gameObject.layer;
        if ((groundLayers.value & (1 << collisionLayer)) == 0)
            return;

        waitingForGroundAfterDeath = false;
        motor.StopAndDisablePhysics();
        int stateHash = animator.HasState(0, DeathCapitalState) ? DeathCapitalState : DeathLowercaseState;
        StartCoroutine(DestroyAfterDeathAnimation(stateHash));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (useSecondaryAttack)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, secondaryAttackRange);
        }
    }

    private int GetCurrentAttackStateHash()
    {
        int preferredState = currentAttackVariant == 2 ? Attack2State : Attack1State;
        if (animator != null && animator.enabled && animator.HasState(0, preferredState))
            return preferredState;

        return animator != null && animator.enabled && animator.HasState(0, AttackState) ? AttackState : 0;
    }

    private void PlayAttackAnimation(int attackVariant, bool interruptCurrentState = false)
    {
        currentAttackVariant = attackVariant == 2 ? 2 : 1;
        int selectedState = GetCurrentAttackStateHash();
        if (selectedState != 0)
        {
            if (animatorHasAttackTrigger)
                animator.ResetTrigger(AttackParameter);

            animator.Play(selectedState, 0, 0f);
            return;
        }

        if (animatorHasAttackTrigger)
        {
            animator.ResetTrigger(AttackParameter);
            animator.SetTrigger(AttackParameter);
        }
        else if (animator.HasState(0, AttackState))
        {
            animator.Play(AttackState, 0, 0f);
        }
    }
}
