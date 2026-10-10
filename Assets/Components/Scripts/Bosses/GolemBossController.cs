using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterMotor2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public sealed class GolemBossController : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
    private static readonly int AppearState = Animator.StringToHash("Base Layer.Appear");
    private static readonly int ThrowStoneState = Animator.StringToHash("Base Layer.ThrowStone");
    private static readonly int CastMagicState = Animator.StringToHash("Base Layer.CastMagic");
    private static readonly int GlowState = Animator.StringToHash("Base Layer.Glow");
    private static readonly int LaserCastState = Animator.StringToHash("Base Layer.LaserCast");
    private static readonly int ImmuneState = Animator.StringToHash("Base Layer.Immune");
    private static readonly int ShieldState = Animator.StringToHash("Base Layer.Shield");
    private static readonly int MeleeState = Animator.StringToHash("Base Layer.Melee");
    private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");
    private static readonly float[] ImmuneThresholds = { 0.5f, 0.1f };

    [Header("Targeting")]
    [SerializeField] private Transform playerTarget;
    [SerializeField, Min(0.1f)] private float detectionRange = 12f;
    [SerializeField, Min(0.1f)] private float attackRange = 9f;
    [SerializeField, Min(0f)] private float patrolDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float retargetInterval = 0.5f;

    [Header("Appear")]
    [SerializeField, Min(0.01f)] private float appearDuration = 1f;
    [SerializeField] private LayerMask groundLayers = 1 << 6;
    [SerializeField, Min(0f)] private float groundSnapRayStartOffset = 1f;
    [SerializeField, Min(0.1f)] private float groundSnapDistance = 32f;

    [Header("Shoot")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GolemProjectile2D shootProjectilePrefab;
    [SerializeField, Min(1)] private int shootProjectileCount = 3;
    [SerializeField, Min(0f)] private float shootSpreadAngle = 15f;
    [SerializeField, Range(0f, 1f)] private float shootHoldNormalizedTime = 0.7778f;
    [SerializeField, Min(0.01f)] private float bulletChargeDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float shootRecoveryDuration = 0.2f;

    [Header("Other ranged attacks")]
    [SerializeField] private GolemProjectile2D stoneProjectilePrefab;
    [SerializeField] private GolemProjectile2D magicProjectilePrefab;
    [SerializeField] private GolemLaserBeam2D laserBeamPrefab;
    [SerializeField, Min(0.1f), Tooltip("Minimum time between attack starts. Current attacks always finish before another can begin.")]
    private float attackCooldown = 0.9f;
    [SerializeField, Min(0.1f)] private float attackAnimationDuration = 0.9f;
    [SerializeField, Min(0f)] private float attackWindUp = 0.675f;

    [Header("Attack selection")]
    [SerializeField, Min(1), Tooltip("Consecutive melee attacks before using a ranged ability, even when the player stays close.")]
    private int meleeAttacksBeforeSpecial = 1;
    [SerializeField, Min(1), Tooltip("Offensive ranged attacks between uses of Shield.")]
    private int offensiveAttacksBeforeShield = 6;

    [Header("Laser")]
    [SerializeField] private Transform laserSpawnPoint;
    [SerializeField, Min(1), Tooltip("Damage applied by the laser beam. Adjust this on the GolemBossController component.")]
    private int laserDamage = 35;
    [SerializeField, Min(0.1f), Tooltip("Maximum laser reach in world units.")]
    private float laserMaxLength = 20f;
    [SerializeField, Min(0f)] private float laserForwardOffset = 0.9f;
    [SerializeField, Min(0f), Tooltip("Extra reach so the laser artwork and hitbox extend into the player's collider.")]
    private float laserTargetPadding = 0.25f;
    [SerializeField, Min(0.01f)] private float glowDuration = 0.8889f;
    [SerializeField, Min(0.01f)] private float laserCastDuration = 0.6f;

    [Header("Melee")]
    [SerializeField, Min(0.1f)] private float meleeRange = 2.25f;
    [SerializeField, Min(0f)] private float meleeForwardOffset = 0.8f;
    [SerializeField, Min(0.1f)] private float meleeWindUp = 0.3f;
    [SerializeField, Min(0.1f)] private float meleeAnimationDuration = 0.9f;
    [SerializeField, Min(1), Tooltip("Damage dealt by the Golem's melee attack.")]
    private int meleeDamage = 30;
    [SerializeField, Min(0f)] private float meleeKnockbackHorizontalSpeed = 4f;
    [SerializeField, Min(0f)] private float meleeKnockbackVerticalSpeed = 1.25f;
    [SerializeField, Min(0f)] private float meleeKnockbackControlLockDuration = 0.25f;

    [Header("Shield")]
    [SerializeField, Min(0.01f)] private float shieldDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float shieldDamageMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float shieldKnockbackHorizontalSpeed = 6f;
    [SerializeField, Min(0f)] private float shieldKnockbackVerticalSpeed = 1.5f;
    [SerializeField, Min(0f)] private float shieldKnockbackControlLockDuration = 0.3f;

    [Header("Threshold immunity")]
    [SerializeField, Min(0.01f)] private float immuneDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float postImmuneHealPercent = 0.05f;
    [SerializeField, Min(0.01f)] private float postImmuneHealDuration = 5f;

    [Header("Death")]
    [SerializeField, Min(0f), Tooltip("Seconds to keep the corpse after it lands.")]
    private float deathDestroyDelay = 2f;
    [SerializeField, Min(0.1f)] private float deathGravityScale = 3f;
    [SerializeField, Min(0.1f), Tooltip("Cleanup time if the corpse falls out of the level without finding ground.")]
    private float deathFallTimeout = 10f;

    private MonsterMotor2D motor;
    private MonsterHealth health;
    private Rigidbody2D body;
    private Animator animator;
    private SpriteRenderer bodyRenderer;
    private BoxCollider2D bodyCollider;
    private Health playerHealth;
    private PlayerMotor2D playerMotor;
    private Collider2D playerCollider;
    private Vector3 startPosition;
    private float targetRefreshTimer;
    private float initialGravityScale;
    private float nextAttackTime;
    private float phaseTimer;
    private float healElapsed;
    private float appearTimer;
    private float immuneTimer;
    private float deathFallTimer;
    private float deathGroundHeight;
    private int healTotal;
    private int healGranted;
    private int pendingPostImmuneHeal;
    private int patrolDirection = 1;
    private RigidbodyConstraints2D initialConstraints;
    private int nextAttackIndex;
    private int meleeAttacksSinceSpecial;
    private int offensiveAttacksSinceShield;
    private int nextImmuneThresholdIndex;
    private int pendingImmuneActivations;
    private int lastObservedHealth;
    private AttackType currentAttackType;
    private AttackPhase attackPhase;
    private GolemProjectile2D activeShootProjectilePrefab;
    private GolemProjectile2D chargedProjectile;
    private GolemLaserBeam2D currentLaserBeam;
    private Vector2 shootAimDirection;
    private bool hasAppeared;
    private bool isAppearing;
    private bool isAttacking;
    private bool isImmune;
    private bool isHealing;
    private bool isShieldActive;
    private bool isDead;
    private bool hasLandedAfterDeath;

    private enum AttackType
    {
        Shoot,
        Magic,
        Laser,
        Shield,
        Melee
    }

    private enum AttackPhase
    {
        None,
        ShootWindUp,
        ShootCharge,
        ShootRecovery,
        MagicWindUp,
        MagicRecovery,
        LaserGlow,
        LaserCast,
        Shield,
        MeleeWindUp,
        MeleeRecovery
    }

    private void Awake()
    {
        motor = GetComponent<MonsterMotor2D>();
        health = GetComponent<MonsterHealth>();
        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            initialGravityScale = body.gravityScale;
            initialConstraints = body.constraints;
        }
        lastObservedHealth = health.MaxHealth;
        animator = GetComponent<Animator>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<BoxCollider2D>();
        startPosition = transform.position;
        shootProjectilePrefab = shootProjectilePrefab != null
            ? shootProjectilePrefab
            : (magicProjectilePrefab != null ? magicProjectilePrefab : stoneProjectilePrefab);
        ResolvePlayerTarget();

        health.SetDamageImmune(true);
        if (bodyCollider != null)
            bodyCollider.enabled = false;
        if (bodyRenderer != null)
            bodyRenderer.enabled = false;
        SetDormantPhysics(true);

        if (!HasAnyRangedAttack())
            Debug.LogError("GolemBossController needs at least one projectile or laser prefab.", this);
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnDamaged += ReactToHit;
        health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnDamaged -= ReactToHit;
        health.OnDied -= HandleDeath;
    }

    private void Update()
    {
        if (isDead || health == null || health.IsDead)
        {
            if (!isDead && health != null && health.IsDead)
                HandleDeath();
            if (isDead && !hasLandedAfterDeath)
            {
                deathFallTimer += Time.deltaTime;
                if (deathFallTimer >= Mathf.Max(0.1f, deathFallTimeout))
                    Destroy(gameObject);
            }
            return;
        }

        RefreshPlayerTarget();

        if (isAppearing)
        {
            TickAppear();
            return;
        }

        if (!hasAppeared)
        {
            CheckForFirstActivation();
            return;
        }

        if (isImmune)
        {
            TickImmune();
            return;
        }

        if (isHealing)
            TickPostImmuneHealing();

        if (TryStartThresholdImmune())
            return;

        if (isAttacking)
        {
            TickAttack();
            return;
        }

        if (playerHealth == null || playerHealth.IsDead)
        {
            Patrol();
            return;
        }

        Vector2 offsetToPlayer = (Vector2)playerHealth.transform.position - (Vector2)transform.position;
        float distanceSquared = offsetToPlayer.sqrMagnitude;
        float distanceToBody = GetDistanceToPlayerBody(offsetToPlayer);
        if (Mathf.Abs(offsetToPlayer.x) > 0.001f)
            motor.FaceDirection(offsetToPlayer.x > 0f ? 1 : -1);

        if (distanceToBody <= meleeRange)
        {
            Stop();
            if (Time.time >= nextAttackTime)
            {
                bool useSpecial = HasAnyRangedAttack() &&
                                  meleeAttacksSinceSpecial >= Mathf.Max(1, meleeAttacksBeforeSpecial);
                BeginAttack(useSpecial ? SelectNextAttack() : AttackType.Melee);
            }
            return;
        }

        if (distanceSquared <= attackRange * attackRange)
        {
            Stop();
            if (Time.time >= nextAttackTime && HasAnyRangedAttack())
                BeginAttack(SelectNextAttack());
            return;
        }

        if (distanceSquared <= detectionRange * detectionRange)
        {
            ChasePlayer();
            return;
        }

        Patrol();
    }

    private void LateUpdate()
    {
        if (!isDead || !hasLandedAfterDeath || bodyRenderer == null || bodyRenderer.sprite == null)
            return;

        // Death frames have different pivots; keep the visible rubble resting on the floor.
        Vector3 position = transform.position;
        position.y += deathGroundHeight - bodyRenderer.bounds.min.y;
        transform.position = position;
    }

    private float GetDistanceToPlayerBody(Vector2 centerOffset)
    {
        if (bodyCollider != null && bodyCollider.enabled && playerCollider != null && playerCollider.enabled)
            return Mathf.Max(0f, bodyCollider.Distance(playerCollider).distance);

        return centerOffset.magnitude;
    }

    public bool TryDeflectShieldHit(Transform damageSource)
    {
        if (!isShieldActive || damageSource == null)
            return false;

        Health attackerHealth = damageSource.GetComponentInParent<Health>();
        PlayerMotor2D attackerMotor = damageSource.GetComponentInParent<PlayerMotor2D>();
        if (attackerHealth == null || attackerMotor == null || attackerHealth.IsDead)
            return false;

        float awayDirection = damageSource.position.x - transform.position.x;
        if (Mathf.Abs(awayDirection) <= 0.001f)
            awayDirection = motor.FacingDirection;

        attackerMotor.ApplyKnockback(
            Vector2.right * Mathf.Sign(awayDirection),
            shieldKnockbackHorizontalSpeed,
            shieldKnockbackVerticalSpeed,
            shieldKnockbackControlLockDuration);
        return true;
    }

    private void RefreshPlayerTarget()
    {
        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer > 0f)
            return;

        ResolvePlayerTarget();
        targetRefreshTimer = Mathf.Max(0.1f, retargetInterval);
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponentInParent<Health>();
            if (playerHealth != null)
                playerTarget = playerHealth.transform;
        }

        if (playerHealth == null || !playerHealth.isActiveAndEnabled)
            playerHealth = FindFirstObjectByType<Health>();

        if (playerHealth == null)
        {
            playerCollider = null;
            playerMotor = null;
            return;
        }

        playerTarget = playerHealth.transform;
        playerMotor = playerTarget.GetComponent<PlayerMotor2D>();
        playerCollider = playerTarget.GetComponent<Collider2D>();
        if (playerCollider == null)
            playerCollider = playerTarget.GetComponentInChildren<Collider2D>();
    }

    private void CheckForFirstActivation()
    {
        Stop();
        if (playerHealth == null || playerHealth.IsDead)
            return;

        Vector2 offset = (Vector2)playerHealth.transform.position - (Vector2)transform.position;
        if (offset.sqrMagnitude <= detectionRange * detectionRange)
            BeginAppear();
    }

    private void BeginAppear()
    {
        if (hasAppeared || isAppearing)
            return;

        SnapToGround();
        isAppearing = true;
        appearTimer = Mathf.Max(0.01f, appearDuration);
        health.SetDamageImmune(true);
        GameAudioManager.PlayCue(GameAudioCue.BossAppear);
        if (bodyRenderer != null)
            bodyRenderer.enabled = true;

        Stop();
        PlayState(AppearState);
    }

    private void TickAppear()
    {
        Stop();
        appearTimer -= Time.deltaTime;
        if (appearTimer > 0f)
            return;

        isAppearing = false;
        hasAppeared = true;
        health.SetDamageImmune(false);
        if (bodyCollider != null)
            bodyCollider.enabled = true;
        SetDormantPhysics(false);
        PlayState(IdleState);
    }

    private void SnapToGround()
    {
        if (bodyCollider == null)
            return;

        Vector2 rayOrigin = (Vector2)transform.position + Vector2.up * groundSnapRayStartOffset;
        RaycastHit2D groundHit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            Mathf.Max(0.1f, groundSnapDistance),
            groundLayers);
        if (groundHit.collider == null)
            return;

        float localColliderBottom = bodyCollider.offset.y - bodyCollider.size.y * 0.5f;
        float worldColliderBottomOffset = localColliderBottom * transform.lossyScale.y;
        float rootY = groundHit.point.y - worldColliderBottomOffset;
        if (body != null)
        {
            body.position = new Vector2(body.position.x, rootY);
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = new Vector3(transform.position.x, rootY, transform.position.z);
        }
    }

    private void SetDormantPhysics(bool isDormant)
    {
        if (body == null)
            return;

        body.linearVelocity = Vector2.zero;
        if (isDormant)
        {
            body.gravityScale = 0f;
            body.constraints = initialConstraints |
                              RigidbodyConstraints2D.FreezePositionX |
                              RigidbodyConstraints2D.FreezePositionY;
            return;
        }

        body.constraints = initialConstraints;
        body.gravityScale = initialGravityScale;
    }

    private bool HasAnyRangedAttack()
    {
        return shootProjectilePrefab != null || magicProjectilePrefab != null ||
               stoneProjectilePrefab != null || laserBeamPrefab != null;
    }

    private AttackType SelectNextAttack()
    {
        if (offensiveAttacksSinceShield >= Mathf.Max(1, offensiveAttacksBeforeShield))
            return AttackType.Shield;

        for (int offset = 0; offset < 3; offset++)
        {
            int candidate = (nextAttackIndex + offset) % 3;
            bool available = candidate switch
            {
                (int)AttackType.Shoot => shootProjectilePrefab != null,
                (int)AttackType.Magic => magicProjectilePrefab != null,
                (int)AttackType.Laser => laserBeamPrefab != null,
                _ => false
            };

            if (!available)
                continue;

            nextAttackIndex = (candidate + 1) % 3;
            return (AttackType)candidate;
        }

        return AttackType.Shoot;
    }

    private void BeginAttack(AttackType attackType)
    {
        if (attackType == AttackType.Melee)
            meleeAttacksSinceSpecial++;
        else
        {
            meleeAttacksSinceSpecial = 0;
            if (attackType == AttackType.Shield)
                offensiveAttacksSinceShield = 0;
            else
                offensiveAttacksSinceShield++;
        }

        GameAudioManager.PlayCue(attackType switch
        {
            AttackType.Shoot => GameAudioCue.BossShootAttack,
            AttackType.Magic => GameAudioCue.BossMagicAttack,
            AttackType.Laser => GameAudioCue.BossLaserAttack,
            AttackType.Shield => GameAudioCue.BossShield,
            _ => GameAudioCue.BossMeleeAttack
        });
        currentAttackType = attackType;
        isAttacking = true;
        attackPhase = AttackPhase.None;
        phaseTimer = 0f;
        nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);
        Stop();

        switch (currentAttackType)
        {
            case AttackType.Shoot:
                activeShootProjectilePrefab = shootProjectilePrefab;
                PlayState(ThrowStoneState);
                attackPhase = AttackPhase.ShootWindUp;
                phaseTimer = Mathf.Max(0.01f, attackAnimationDuration * shootHoldNormalizedTime);
                break;
            case AttackType.Magic:
                PlayState(CastMagicState);
                attackPhase = AttackPhase.MagicWindUp;
                phaseTimer = Mathf.Clamp(attackWindUp, 0f, attackAnimationDuration);
                break;
            case AttackType.Laser:
                PlayState(GlowState);
                attackPhase = AttackPhase.LaserGlow;
                phaseTimer = Mathf.Max(0.01f, glowDuration);
                break;
            case AttackType.Shield:
                isShieldActive = true;
                health.SetDamageTakenMultiplier(shieldDamageMultiplier);
                PlayState(ShieldState);
                attackPhase = AttackPhase.Shield;
                phaseTimer = Mathf.Max(0.01f, shieldDuration);
                break;
            case AttackType.Melee:
                PlayState(MeleeState);
                attackPhase = AttackPhase.MeleeWindUp;
                phaseTimer = Mathf.Clamp(meleeWindUp, 0f, meleeAnimationDuration);
                break;
        }
    }

    private void TickAttack()
    {
        Stop();
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
            return;

        switch (attackPhase)
        {
            case AttackPhase.ShootWindUp:
                BeginShootCharge();
                break;
            case AttackPhase.ShootCharge:
                ReleaseShootVolley();
                break;
            case AttackPhase.ShootRecovery:
            case AttackPhase.MagicRecovery:
            case AttackPhase.Shield:
                FinishAttack();
                break;
            case AttackPhase.MagicWindUp:
                LaunchSingleProjectile(magicProjectilePrefab != null ? magicProjectilePrefab : stoneProjectilePrefab);
                attackPhase = AttackPhase.MagicRecovery;
                phaseTimer = Mathf.Max(0.01f, attackAnimationDuration - attackWindUp);
                break;
            case AttackPhase.LaserGlow:
                BeginLaserCast();
                break;
            case AttackPhase.LaserCast:
            case AttackPhase.MeleeRecovery:
                FinishAttack();
                break;
            case AttackPhase.MeleeWindUp:
                ApplyMeleeDamage();
                attackPhase = AttackPhase.MeleeRecovery;
                phaseTimer = Mathf.Max(0.01f, meleeAnimationDuration - meleeWindUp);
                break;
            default:
                FinishAttack();
                break;
        }
    }

    private void BeginShootCharge()
    {
        PlayState(ThrowStoneState, shootHoldNormalizedTime);
        if (animator != null)
            animator.speed = 0f;

        Transform spawnPoint = projectileSpawnPoint;
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        shootAimDirection = GetAimDirection(origin);
        if (activeShootProjectilePrefab == null)
            activeShootProjectilePrefab = shootProjectilePrefab;

        if (activeShootProjectilePrefab != null)
        {
            chargedProjectile = Instantiate(activeShootProjectilePrefab, origin, Quaternion.identity);
            chargedProjectile.PrepareForLaunch(transform);
        }

        attackPhase = AttackPhase.ShootCharge;
        phaseTimer = Mathf.Max(0.01f, bulletChargeDuration);
    }

    private void ReleaseShootVolley()
    {
        if (animator != null)
            animator.speed = 1f;

        GolemProjectile2D projectilePrefab = activeShootProjectilePrefab;
        if (projectilePrefab != null)
        {
            Transform spawnPoint = projectileSpawnPoint;
            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            if (playerHealth != null && !playerHealth.IsDead)
                shootAimDirection = GetAimDirection(origin);
            if (shootAimDirection.sqrMagnitude <= 0.0001f)
                shootAimDirection = Vector2.right * motor.FacingDirection;
            shootAimDirection.Normalize();

            int bulletCount = Mathf.Max(1, shootProjectileCount);
            float startAngle = -shootSpreadAngle * (bulletCount - 1) * 0.5f;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + shootSpreadAngle * i;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * shootAimDirection;
                GolemProjectile2D projectile = i == 0 && chargedProjectile != null
                    ? chargedProjectile
                    : Instantiate(projectilePrefab, origin, Quaternion.identity);
                if (projectile != null)
                    projectile.Launch(direction, transform);
            }
        }
        else if (chargedProjectile != null)
        {
            chargedProjectile.Launch(shootAimDirection, transform);
        }

        chargedProjectile = null;
        activeShootProjectilePrefab = null;
        attackPhase = AttackPhase.ShootRecovery;
        phaseTimer = Mathf.Max(0.01f, shootRecoveryDuration);
    }

    private void BeginLaserCast()
    {
        PlayState(LaserCastState);
        currentLaserBeam = CreateLaserBeam(laserCastDuration);
        attackPhase = AttackPhase.LaserCast;
        phaseTimer = Mathf.Max(0.01f, laserCastDuration);
    }

    private GolemLaserBeam2D CreateLaserBeam(float chargeDuration)
    {
        if (laserBeamPrefab == null || playerHealth == null || playerHealth.IsDead)
            return null;

        Vector3 origin = GetLaserOrigin();
        Vector2 aimOffset = GetAimOffset(origin);
        Vector2 direction = aimOffset.sqrMagnitude > 0.0001f
            ? aimOffset.normalized
            : Vector2.right * motor.FacingDirection;
        float beamLength = Mathf.Min(
            Mathf.Max(0.1f, aimOffset.magnitude + laserTargetPadding),
            Mathf.Max(0.1f, laserMaxLength));
        GolemLaserBeam2D laser = Instantiate(laserBeamPrefab, origin, Quaternion.identity);
        laser.Fire(
            direction,
            transform,
            beamLength,
            laserDamage,
            chargeDuration,
            playerCollider,
            laserMaxLength,
            laserTargetPadding);
        return laser;
    }

    private Vector3 GetLaserOrigin()
    {
        Vector3 origin = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
        float bodyHalfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0f;
        origin.x = transform.position.x + motor.FacingDirection * (bodyHalfWidth + laserForwardOffset);
        return origin;
    }

    private Vector2 GetAimDirection(Vector3 origin)
    {
        Vector2 direction = GetAimOffset(origin);
        return direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right * motor.FacingDirection;
    }

    private Vector2 GetAimOffset(Vector3 origin)
    {
        if (playerHealth == null || playerHealth.IsDead)
            return Vector2.right * motor.FacingDirection;

        Vector2 targetPoint = playerCollider != null
            ? playerCollider.ClosestPoint(origin)
            : (Vector2)playerHealth.transform.position;
        return targetPoint - (Vector2)origin;
    }

    private void LaunchSingleProjectile(GolemProjectile2D projectilePrefab)
    {
        if (projectilePrefab == null || playerHealth == null || playerHealth.IsDead)
            return;

        Vector3 origin = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GolemProjectile2D projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(GetAimDirection(origin), transform);
    }

    private void ApplyMeleeDamage()
    {
        if (playerHealth == null || playerHealth.IsDead)
            return;

        Vector2 origin = (Vector2)transform.position +
                         Vector2.right * (motor.FacingDirection * meleeForwardOffset);
        Vector2 targetPoint = playerCollider != null
            ? playerCollider.ClosestPoint(origin)
            : (Vector2)playerHealth.transform.position;
        Vector2 offset = targetPoint - origin;
        if (offset.sqrMagnitude > meleeRange * meleeRange ||
            (Mathf.Abs(offset.x) > 0.01f && Mathf.Sign(offset.x) != motor.FacingDirection))
            return;

        int previousHealth = playerHealth.CurrentHealth;
        bool wasDead = playerHealth.IsDead;
        playerHealth.TakeDamage(meleeDamage);
        if (!wasDead && (playerHealth.IsDead || playerHealth.CurrentHealth < previousHealth) && playerMotor != null)
        {
            float awayDirection = playerHealth.transform.position.x - transform.position.x;
            if (Mathf.Abs(awayDirection) <= 0.001f)
                awayDirection = motor.FacingDirection;

            playerMotor.ApplyKnockback(
                Vector2.right * Mathf.Sign(awayDirection),
                meleeKnockbackHorizontalSpeed,
                meleeKnockbackVerticalSpeed,
                meleeKnockbackControlLockDuration);
        }
    }

    private void FinishAttack()
    {
        isAttacking = false;
        attackPhase = AttackPhase.None;
        phaseTimer = 0f;
        activeShootProjectilePrefab = null;
        if (isShieldActive)
        {
            isShieldActive = false;
            health.SetDamageTakenMultiplier(1f);
        }

        PlayState(IdleState);
    }

    private bool TryStartThresholdImmune()
    {
        if (!hasAppeared || isImmune || pendingImmuneActivations <= 0)
            return false;

        // Do not let a health threshold interrupt an already-started volley.
        // The immunity starts on the next update after its bullets are launched.
        if (isAttacking && currentAttackType == AttackType.Shoot &&
            (attackPhase == AttackPhase.ShootWindUp || attackPhase == AttackPhase.ShootCharge))
            return false;

        pendingImmuneActivations--;
        CancelCurrentAttack();
        Stop();
        if (isHealing)
            pendingPostImmuneHeal += Mathf.Max(0, healTotal - healGranted);
        isHealing = false;
        isImmune = true;
        immuneTimer = Mathf.Max(0.01f, immuneDuration);
        health.SetDamageImmune(true);
        PlayState(ImmuneState);
        return true;
    }

    private void TickImmune()
    {
        Stop();
        immuneTimer -= Time.deltaTime;
        if (immuneTimer > 0f)
            return;

        isImmune = false;
        health.SetDamageImmune(false);
        StartPostImmuneHealing();
        PlayState(IdleState);
    }

    private void StartPostImmuneHealing()
    {
        healTotal = Mathf.RoundToInt(health.MaxHealth * postImmuneHealPercent) + pendingPostImmuneHeal;
        pendingPostImmuneHeal = 0;
        healGranted = 0;
        healElapsed = 0f;
        isHealing = healTotal > 0 && health.CurrentHealth < health.MaxHealth;
        if (!isHealing)
            return;

        healTotal = Mathf.Min(healTotal, health.MaxHealth - health.CurrentHealth);
    }

    private void TickPostImmuneHealing()
    {
        float duration = Mathf.Max(0.01f, postImmuneHealDuration);
        healElapsed = Mathf.Min(duration, healElapsed + Time.deltaTime);
        int desiredHeal = Mathf.FloorToInt(healTotal * (healElapsed / duration));
        int healThisFrame = desiredHeal - healGranted;
        if (healThisFrame > 0)
        {
            health.Heal(healThisFrame);
            healGranted = desiredHeal;
        }

        if (healElapsed < duration)
            return;

        int remainingHeal = healTotal - healGranted;
        if (remainingHeal > 0)
            health.Heal(remainingHeal);

        isHealing = false;
    }

    private void CancelCurrentAttack()
    {
        isAttacking = false;
        attackPhase = AttackPhase.None;
        phaseTimer = 0f;
        if (animator != null)
            animator.speed = 1f;
        if (chargedProjectile != null)
            Destroy(chargedProjectile.gameObject);
        chargedProjectile = null;
        activeShootProjectilePrefab = null;
        if (isShieldActive)
        {
            isShieldActive = false;
            health.SetDamageTakenMultiplier(1f);
        }
    }

    private void Patrol()
    {
        float distanceFromStart = transform.position.x - startPosition.x;
        if (patrolDirection > 0 && distanceFromStart >= patrolDistance)
            patrolDirection = -1;
        else if (patrolDirection < 0 && distanceFromStart <= -patrolDistance)
            patrolDirection = 1;

        motor.SetDesiredHorizontalSpeed(patrolDirection * motor.PatrolSpeed);
        motor.FaceDirection(patrolDirection);
        SetMoving(Mathf.Abs(motor.DesiredHorizontalSpeed) > 0.01f);
    }

    private void ChasePlayer()
    {
        int direction = playerHealth.transform.position.x > transform.position.x ? 1 : -1;
        motor.SetDesiredHorizontalSpeed(direction * motor.ChaseSpeed);
        motor.FaceDirection(direction);
        SetMoving(Mathf.Abs(motor.DesiredHorizontalSpeed) > 0.01f);
    }

    private void Stop()
    {
        if (motor != null)
            motor.SetDesiredVelocity(Vector2.zero);
        SetMoving(false);
    }

    private void SetMoving(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(IsMovingParameter, isMoving);
    }

    private void PlayState(int stateHash, float normalizedTime = 0f)
    {
        if (animator == null || !animator.HasState(0, stateHash))
            return;

        animator.speed = 1f;
        animator.Play(stateHash, 0, normalizedTime);
        animator.Update(0f);
    }

    private void ReactToHit()
    {
        if (isDead || isAppearing || health == null || health.IsDead)
            return;

        if (!isImmune)
        {
            int currentHealth = health.CurrentHealth;
            while (nextImmuneThresholdIndex < ImmuneThresholds.Length)
            {
                int thresholdHealth = Mathf.CeilToInt(health.MaxHealth * ImmuneThresholds[nextImmuneThresholdIndex]);
                if (lastObservedHealth <= thresholdHealth || currentHealth > thresholdHealth)
                    break;

                pendingImmuneActivations++;
                nextImmuneThresholdIndex++;
            }

            lastObservedHealth = currentHealth;
        }

        if (TryStartThresholdImmune())
            return;

        if (isImmune || isAttacking || animator == null)
            return;

        PlayState(HurtState);
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        CancelCurrentAttack();
        isImmune = false;
        isHealing = false;
        health.SetDamageImmune(true);
        health.SetDamageTakenMultiplier(1f);
        Stop();
        deathFallTimer = 0f;
        hasLandedAfterDeath = false;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.constraints = (initialConstraints & ~RigidbodyConstraints2D.FreezePositionY) |
                               RigidbodyConstraints2D.FreezeRotation;
        }
        if (bodyCollider != null)
        {
            // The child Ground Collider supports the fall; the hurtbox stays passable.
            bodyCollider.isTrigger = true;
            bodyCollider.enabled = true;
            // A defeated boss falls through the player and settles on the ground.
            if (playerCollider != null && playerCollider.enabled && playerCollider.gameObject.activeInHierarchy)
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
        }
        motor.BeginDeathFall(deathGravityScale);
        if (currentLaserBeam != null)
            Destroy(currentLaserBeam.gameObject);

        if (bodyRenderer != null)
            bodyRenderer.enabled = true;
        PlayState(DeathState);
    }

    private void OnCollisionEnter2D(Collision2D collision) => FinishDeathFall(collision);

    private void OnCollisionStay2D(Collision2D collision) => FinishDeathFall(collision);

    private void FinishDeathFall(Collision2D collision)
    {
        if (!isDead || hasLandedAfterDeath || (groundLayers.value & (1 << collision.gameObject.layer)) == 0)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y <= 0.5f)
                continue;

            hasLandedAfterDeath = true;
            deathGroundHeight = collision.GetContact(i).point.y + 0.01f;
            motor.StopAndDisablePhysics();
            if (bodyCollider != null)
                bodyCollider.enabled = false;
            StartCoroutine(DestroyAfterDeathAnimation());
            return;
        }
    }

    private IEnumerator DestroyAfterDeathAnimation()
    {
        yield return DeathAnimationPlayback.WaitForCompletion(animator, DeathState, deathDestroyDelay);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
