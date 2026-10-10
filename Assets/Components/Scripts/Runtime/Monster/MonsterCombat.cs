using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Collider2D attackCollider;
    [SerializeField] private float attackHitboxRadius = 0.65f;
    [SerializeField, Min(0f)] private float attackPointForwardOffset = 0.5f;
    [SerializeField, Min(0.01f)] private float attackDuration = 0.85f;
    [SerializeField, Range(0f, 1f)] private float attackImpactNormalizedTime = 0.5f;

    [Header("Secondary Attack")]
    [SerializeField] private Transform secondaryAttackPoint;
    [SerializeField] private Collider2D secondaryAttackCollider;
    [SerializeField] private int secondaryAttackDamage = 12;
    [SerializeField] private float secondaryAttackHitboxRadius = 0.85f;
    [SerializeField, Min(0f)] private float secondaryAttackPointForwardOffset = 1f;
    [SerializeField, Min(0.01f)] private float secondaryAttackDuration = 0.95f;
    [SerializeField, Range(0f, 1f)] private float secondaryAttackImpactNormalizedTime = 0.6f;

    [Header("Player Knockback")]
    [SerializeField, Min(0f)] private float playerKnockbackHorizontalSpeed;
    [SerializeField, Min(0f)] private float playerKnockbackVerticalSpeed;
    [SerializeField, Min(0f)] private float playerKnockbackControlLockDuration;
    [SerializeField] private bool ignoreTargetBodyCollisionDuringAttack;

    private readonly List<AttackCollisionPair> ignoredBodyCollisionPairs = new List<AttackCollisionPair>();

    private float lastAttackTime;
    private float actionTimer;
    private float attackImpactTimer;
    private Transform playerTarget;
    private Health playerHealth;
    private PlayerMotor2D playerMotor;
    private Collider2D playerCollider;
    private bool isAttacking;
    private bool attackDamageApplied;
    private int attackVariant = 1;
    private GameAudioCue primaryAudioCue;
    private GameAudioCue secondaryAudioCue;

    private readonly struct AttackCollisionPair
    {
        public readonly Collider2D MonsterCollider;
        public readonly Collider2D PlayerCollider;

        public AttackCollisionPair(Collider2D monsterCollider, Collider2D playerCollider)
        {
            MonsterCollider = monsterCollider;
            PlayerCollider = playerCollider;
        }
    }

    public bool IsAttacking => isAttacking;
    public int CurrentAttackVariant => attackVariant;

    public void ConfigureDamage(int primaryDamage, int secondaryDamage)
    {
        attackDamage = Mathf.Max(1, primaryDamage);
        secondaryAttackDamage = Mathf.Max(1, secondaryDamage);
    }

    private void Awake()
    {
        MonsterChapterStats stats = GetComponent<MonsterChapterStats>();
        bool slime = stats != null && (stats.Archetype == MonsterArchetype.GreenSlime ||
            stats.Archetype == MonsterArchetype.BlueSlime || stats.Archetype == MonsterArchetype.RedSlime);
        bool demonSlime = GetComponent<BossEncounter>() != null && GetComponent<GolemBossController>() == null &&
            GetComponent<BringerOfDeathController>() == null;
        primaryAudioCue = demonSlime ? GameAudioCue.DemonSlimeAttack : slime ? GameAudioCue.SlimeAttack : GameAudioCue.MonsterAttack1;
        secondaryAudioCue = demonSlime ? GameAudioCue.DemonSlimeAttack : stats != null && stats.Archetype == MonsterArchetype.Mimic ? GameAudioCue.MimicAttack2 : GameAudioCue.MonsterAttack2;
        lastAttackTime = -attackCooldown;

        if (attackCollider != null)
        {
            attackCollider.isTrigger = true;
            attackCollider.enabled = false;
        }
        else if (attackPoint != null)
        {
            // The legacy marker must not act as a second solid body collider and block the chase.
            Collider2D attackPointCollider = attackPoint.GetComponent<Collider2D>();
            if (attackPointCollider != null)
                attackPointCollider.isTrigger = true;
        }

        if (secondaryAttackCollider != null)
        {
            secondaryAttackCollider.isTrigger = true;
            secondaryAttackCollider.enabled = false;
        }
    }

    public bool TryStartAttack(Transform target, Health targetHealth, int variant = 1)
    {
        return TryBeginAttack(target, targetHealth, false, variant);
    }

    public bool TryStartCounterAttack(Transform target, Health targetHealth)
    {
        return TryBeginAttack(target, targetHealth, true, 1);
    }

    private bool TryBeginAttack(Transform target, Health targetHealth, bool bypassCooldown, int variant)
    {
        if (isAttacking || (!bypassCooldown && Time.time < lastAttackTime + attackCooldown))
            return false;

        attackVariant = variant == 2 && secondaryAttackCollider != null ? 2 : 1;
        playerTarget = target;
        playerHealth = targetHealth;
        playerMotor = target != null ? target.GetComponent<PlayerMotor2D>() : null;
        playerCollider = target != null ? target.GetComponent<Collider2D>() : null;
        lastAttackTime = Time.time;
        float duration = GetAttackDuration();
        actionTimer = duration;
        attackImpactTimer = duration * GetAttackImpactNormalizedTime();
        attackDamageApplied = false;
        isAttacking = true;
        GameAudioManager.PlayCueAt(attackVariant == 2 ? secondaryAudioCue : primaryAudioCue, transform.position);
        IgnoreTargetBodyCollisions();
        SetAttackColliderEnabled(true);
        return true;
    }

    public void Tick(bool attackAnimationComplete, bool useDurationFallback)
    {
        if (!isAttacking)
            return;

        actionTimer -= Time.deltaTime;
        attackImpactTimer -= Time.deltaTime;
        if (!attackDamageApplied && attackImpactTimer <= 0f)
            ApplyAttackDamage();

        if (attackAnimationComplete || (useDurationFallback && actionTimer <= 0f))
        {
            if (!attackDamageApplied)
                ApplyAttackDamage();
            EndAttack();
        }
    }

    // Legacy Animation Event bridge; timer-driven and event-driven hits share this one-shot path.
    public void ApplyAttackDamageFromAnimation()
    {
        ApplyAttackDamage();
    }

    public void EndAttack()
    {
        if (!isAttacking)
            return;

        if (!attackDamageApplied)
            ApplyAttackDamage();

        isAttacking = false;
        attackDamageApplied = false;
        actionTimer = 0f;
        SetAttackColliderEnabled(false);
        RestoreTargetBodyCollisions();
    }

    public void CancelAttack()
    {
        isAttacking = false;
        attackDamageApplied = false;
        actionTimer = 0f;
        attackImpactTimer = 0f;
        playerTarget = null;
        playerHealth = null;
        playerMotor = null;
        SetAttackColliderEnabled(false);
        RestoreTargetBodyCollisions();
    }

    private void OnDisable()
    {
        RestoreTargetBodyCollisions();
    }

    private void IgnoreTargetBodyCollisions()
    {
        if (!ignoreTargetBodyCollisionDuringAttack || playerTarget == null)
            return;

        Collider2D[] monsterColliders = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] targetColliders = playerTarget.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < monsterColliders.Length; i++)
        {
            Collider2D monsterCollider = monsterColliders[i];
            if (monsterCollider == null || !monsterCollider.enabled || monsterCollider.isTrigger ||
                !monsterCollider.gameObject.activeInHierarchy)
                continue;

            for (int j = 0; j < targetColliders.Length; j++)
            {
                Collider2D targetCollider = targetColliders[j];
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger ||
                    !targetCollider.gameObject.activeInHierarchy ||
                    Physics2D.GetIgnoreCollision(monsterCollider, targetCollider))
                    continue;

                Physics2D.IgnoreCollision(monsterCollider, targetCollider, true);
                ignoredBodyCollisionPairs.Add(new AttackCollisionPair(monsterCollider, targetCollider));
            }
        }
    }

    private void RestoreTargetBodyCollisions()
    {
        for (int i = 0; i < ignoredBodyCollisionPairs.Count; i++)
        {
            AttackCollisionPair pair = ignoredBodyCollisionPairs[i];
            if (pair.MonsterCollider == null || pair.PlayerCollider == null ||
                !pair.MonsterCollider.enabled || !pair.PlayerCollider.enabled ||
                !pair.MonsterCollider.gameObject.activeInHierarchy || !pair.PlayerCollider.gameObject.activeInHierarchy)
                continue;

            Physics2D.IgnoreCollision(pair.MonsterCollider, pair.PlayerCollider, false);
        }

        ignoredBodyCollisionPairs.Clear();
    }

    private void ApplyAttackDamage()
    {
        if (!isAttacking || attackDamageApplied || playerTarget == null)
            return;

        attackDamageApplied = true;
        Collider2D activeAttackCollider = GetAttackCollider();
        bool hitTarget;
        if (activeAttackCollider != null && playerCollider != null)
        {
            hitTarget = activeAttackCollider.enabled && activeAttackCollider.Distance(playerCollider).isOverlapped;
        }
        else
        {
            Vector2 attackOrigin = GetAttackOrigin(attackVariant);
            Vector2 targetPoint = playerCollider != null
                ? playerCollider.ClosestPoint(attackOrigin)
                : (Vector2)playerTarget.position;
            Vector2 targetOffset = targetPoint - attackOrigin;
            float hitRadius = Mathf.Max(0f, GetAttackHitboxRadius());
            hitTarget = targetOffset.sqrMagnitude <= hitRadius * hitRadius;
        }

        if (!hitTarget || playerHealth == null)
            return;

        int previousHealth = playerHealth.CurrentHealth;
        bool wasDead = playerHealth.IsDead;
        playerHealth.TakeDamage(GetAttackDamage());
        if (!wasDead && (playerHealth.IsDead || playerHealth.CurrentHealth < previousHealth))
            ApplyPlayerKnockback();
    }

    private void ApplyPlayerKnockback()
    {
        if (playerMotor == null ||
            (playerKnockbackHorizontalSpeed <= 0f && playerKnockbackVerticalSpeed <= 0f))
            return;

        float awayDirection = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(awayDirection) <= 0.001f)
            awayDirection = transform.lossyScale.x >= 0f ? 1f : -1f;

        playerMotor.ApplyKnockback(
            Vector2.right * Mathf.Sign(awayDirection),
            playerKnockbackHorizontalSpeed,
            playerKnockbackVerticalSpeed,
            playerKnockbackControlLockDuration);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(GetAttackOrigin(1), attackHitboxRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetAttackOrigin(2), secondaryAttackHitboxRadius);
    }

    private Vector2 GetAttackOrigin(int variant)
    {
        Transform point = variant == 2 && secondaryAttackPoint != null ? secondaryAttackPoint : attackPoint;
        Vector2 origin = point != null ? (Vector2)point.position : (Vector2)transform.position;
        float facingDirection = transform.lossyScale.x < 0f ? -1f : 1f;
        float forwardOffset = variant == 2 ? secondaryAttackPointForwardOffset : attackPointForwardOffset;
        return origin + Vector2.right * (facingDirection * forwardOffset);
    }

    private Collider2D GetAttackCollider()
    {
        return attackVariant == 2 ? secondaryAttackCollider : attackCollider;
    }

    private int GetAttackDamage()
    {
        return attackVariant == 2 ? secondaryAttackDamage : attackDamage;
    }

    private float GetAttackDuration()
    {
        return attackVariant == 2 ? secondaryAttackDuration : attackDuration;
    }

    private float GetAttackImpactNormalizedTime()
    {
        return attackVariant == 2 ? secondaryAttackImpactNormalizedTime : attackImpactNormalizedTime;
    }

    private float GetAttackHitboxRadius()
    {
        return attackVariant == 2 ? secondaryAttackHitboxRadius : attackHitboxRadius;
    }

    private void SetAttackColliderEnabled(bool isEnabled)
    {
        if (attackCollider != null)
            attackCollider.enabled = isEnabled && attackVariant == 1;
        if (secondaryAttackCollider != null)
            secondaryAttackCollider.enabled = isEnabled && attackVariant == 2;
    }
}
