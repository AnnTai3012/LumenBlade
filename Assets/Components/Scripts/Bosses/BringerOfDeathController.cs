using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BringerOfDeathState { Idle, Chase, Attack, Cast, Hurt, Dead }

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth), typeof(MonsterMotor2D), typeof(MonsterCombat))]
[RequireComponent(typeof(BossEncounter), typeof(Animator))]
public sealed class BringerOfDeathController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField, Min(0.1f)] private float meleeRange = 3.7f;
    [SerializeField, Min(0.1f)] private float castRange = 10f;
    [SerializeField] private LayerMask groundLayers = 1 << 6;

    [Header("Cast and Spell")]
    [SerializeField] private BringerSpell2D spellPrefab;
    [SerializeField, Min(1)] private int spellDamage = 35;
    [SerializeField, Min(0.1f)] private float castCooldown = 4.5f;
    [SerializeField, Min(0.1f)] private float castDuration = 0.9f;
    [SerializeField, Range(0f, 1f)] private float spellReleaseNormalizedTime = 0.5556f;
    [SerializeField, Min(0f)] private float firstCastDelay = 1.5f;

    [Header("Animation timing")]
    [SerializeField, Min(0.1f)] private float attackDuration = 1f;
    [SerializeField, Min(0.1f)] private float hurtDuration = 0.3f;
    [SerializeField, Min(0f)] private float deathDestroyDelay = 1.2f;

    private static readonly int MovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int CastParameter = Animator.StringToHash("Cast");
    private static readonly int HurtParameter = Animator.StringToHash("Hurt");
    private static readonly int DeadParameter = Animator.StringToHash("Dead");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int CastState = Animator.StringToHash("Base Layer.Cast");
    private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

    private readonly List<BringerSpell2D> spells = new List<BringerSpell2D>();
    private MonsterHealth health;
    private MonsterMotor2D motor;
    private MonsterCombat combat;
    private BossEncounter encounter;
    private Animator animator;
    private float actionElapsed;
    private float nextCastTime;
    private Vector3 spellPosition;
    private bool engaged;
    private bool releasedSpell;
    private bool observedActionAnimation;

    public BringerOfDeathState CurrentState { get; private set; }

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        motor = GetComponent<MonsterMotor2D>();
        combat = GetComponent<MonsterCombat>();
        encounter = GetComponent<BossEncounter>();
        animator = GetComponent<Animator>();
        if (spellPrefab == null)
        {
            Debug.LogError("Bringer of Death needs its Spell prefab.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.OnDamaged += ReactToHit;
        health.OnDied += Die;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= ReactToHit;
            health.OnDied -= Die;
        }
        combat?.CancelAttack();
        motor?.SetDesiredVelocity(Vector2.zero);
        CancelSpells();
    }

    private void Update()
    {
        if (health.IsDead || CurrentState == BringerOfDeathState.Dead) return;
        if (!encounter.IsPlayerInside)
        {
            if (CurrentState != BringerOfDeathState.Idle) CancelAction();
            Stop();
            return;
        }
        if (!engaged)
        {
            engaged = true;
            nextCastTime = Time.time + firstCastDelay;
        }

        if (CurrentState == BringerOfDeathState.Attack || CurrentState == BringerOfDeathState.Cast || CurrentState == BringerOfDeathState.Hurt)
        {
            TickAction();
            return;
        }

        Health player = encounter.Player;
        Vector2 offset = player.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > 0.01f) motor.FaceDirection(offset.x > 0f ? 1 : -1);
        float distanceSquared = offset.sqrMagnitude;
        if (Time.time >= nextCastTime && distanceSquared <= castRange * castRange)
        {
            BeginCast(player);
        }
        else if (distanceSquared <= meleeRange * meleeRange)
        {
            Stop();
            if (combat.TryStartAttack(player.transform, player)) BeginAction(BringerOfDeathState.Attack, AttackParameter);
            else CurrentState = BringerOfDeathState.Idle;
        }
        else
        {
            var groundAhead = Physics2D.Raycast((Vector2)transform.position + new Vector2(motor.FacingDirection * 0.8f, 0.4f), Vector2.down, 1.2f, groundLayers);
            bool canMove = groundAhead.collider != null;
            motor.SetDesiredHorizontalSpeed(canMove ? motor.FacingDirection * motor.ChaseSpeed : 0f);
            animator.SetBool(MovingParameter, canMove);
            CurrentState = canMove ? BringerOfDeathState.Chase : BringerOfDeathState.Idle;
        }
    }

    private void BeginCast(Health player)
    {
        // Lock the ground location at the start of the cast so the player can dodge the spell.
        var collider = player.GetComponent<Collider2D>();
        float floor = collider != null ? collider.bounds.min.y : player.transform.position.y;
        var hit = Physics2D.Raycast(new Vector2(player.transform.position.x, floor + 0.5f), Vector2.down, 32f, groundLayers);
        spellPosition = new Vector3(player.transform.position.x, hit.collider != null ? hit.point.y : floor, transform.position.z);
        nextCastTime = Time.time + castCooldown;
        releasedSpell = false;
        BeginAction(BringerOfDeathState.Cast, CastParameter);
        GameAudioManager.PlayCue(GameAudioCue.BossMagicAttack);
    }

    private void BeginAction(BringerOfDeathState state, int parameter)
    {
        Stop();
        CurrentState = state;
        actionElapsed = 0f;
        observedActionAnimation = false;
        ResetActionTriggers();
        animator.SetTrigger(parameter);
    }

    private void TickAction()
    {
        Stop();
        actionElapsed += Time.deltaTime;
        float duration = CurrentState == BringerOfDeathState.Cast ? castDuration : CurrentState == BringerOfDeathState.Hurt ? hurtDuration : attackDuration;
        int stateHash = CurrentState == BringerOfDeathState.Cast ? CastState : CurrentState == BringerOfDeathState.Hurt ? HurtState : AttackState;
        AnimatorStateInfo animation = animator.GetCurrentAnimatorStateInfo(0);
        bool inAction = animator.enabled && animation.fullPathHash == stateHash;
        if (inAction) observedActionAnimation = true;
        float progress = inAction ? animation.normalizedTime : actionElapsed / duration;
        bool completed = progress >= 1f || (observedActionAnimation && !inAction);

        if (CurrentState == BringerOfDeathState.Cast && !releasedSpell && progress >= spellReleaseNormalizedTime)
        {
            releasedSpell = true;
            for (int i = spells.Count - 1; i >= 0; i--) if (spells[i] == null) spells.RemoveAt(i);
            BringerSpell2D spell = Instantiate(spellPrefab, spellPosition, Quaternion.identity);
            spell.Initialize(health, spellDamage);
            spells.Add(spell);
        }
        if (CurrentState == BringerOfDeathState.Attack) combat.Tick(completed, !animator.enabled);
        if (!completed) return;
        if (CurrentState == BringerOfDeathState.Attack) combat.CancelAttack();
        CurrentState = BringerOfDeathState.Idle;
    }

    private void ReactToHit()
    {
        // Attack and cast recovery cannot be stun-locked; HitFeedback still flashes every hit.
        if (health.IsDead || CurrentState == BringerOfDeathState.Attack || CurrentState == BringerOfDeathState.Cast) return;
        BeginAction(BringerOfDeathState.Hurt, HurtParameter);
    }

    private void Die()
    {
        if (CurrentState == BringerOfDeathState.Dead) return;
        CurrentState = BringerOfDeathState.Dead;
        combat.CancelAttack();
        CancelSpells();
        Stop();
        ResetActionTriggers();
        animator.SetBool(DeadParameter, true);
        if (animator.HasState(0, DeathState)) animator.Play(DeathState, 0, 0f);
        motor.StopAndDisablePhysics();
        StartCoroutine(DestroyAfterDeathAnimation());
    }

    private IEnumerator DestroyAfterDeathAnimation()
    {
        yield return DeathAnimationPlayback.WaitForCompletion(animator, DeathState, deathDestroyDelay);
        Destroy(gameObject);
    }

    private void Stop()
    {
        motor.SetDesiredVelocity(Vector2.zero);
        animator.SetBool(MovingParameter, false);
    }

    private void CancelAction()
    {
        combat.CancelAttack();
        ResetActionTriggers();
        if (animator.enabled && animator.HasState(0, IdleState)) animator.Play(IdleState, 0, 0f);
        CurrentState = BringerOfDeathState.Idle;
    }

    private void ResetActionTriggers()
    {
        animator.ResetTrigger(AttackParameter);
        animator.ResetTrigger(CastParameter);
        animator.ResetTrigger(HurtParameter);
    }

    private void CancelSpells()
    {
        for (int i = 0; i < spells.Count; i++) if (spells[i] != null) spells[i].Cancel();
        spells.Clear();
    }
}
