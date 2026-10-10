using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CombatHitbox2D))]
public sealed class PlayerCombat : MonoBehaviour
{
    private const float AnimationCompletionFallbackBuffer = 0.1f;

    [Header("Attack")]
    [SerializeField] private float attack1Duration = 0.2f;
    [SerializeField] private float attack2Duration = 0.6f;
    [SerializeField] private AnimationClip attack2Clip;
    [SerializeField, Min(0.01f)] private float attackInputBufferTime = 0.18f;
    [SerializeField] private bool lockMovementWhileAttacking = true;
    [SerializeField, Min(1)] private int attackDamage = 12;
    [SerializeField, Min(0.01f)] private float attackRange = 0.85f;
    [SerializeField, Range(0f, 1f)] private float attackHitNormalizedTime = 0.5f;

    [Header("Basic Attack Targeting")]
    [SerializeField] private bool autoTargetBasicAttacks = true;
    [SerializeField] private BoxCollider2D basicAttackHitbox;
    [SerializeField, Min(0f)] private float basicAttackTargetRange = 3.5f;
    [SerializeField, Min(0f)] private float basicAttackApproachSpeed = 28f;
    [SerializeField, Min(0f)] private float basicAttackApproachDuration = 0.15f;

    [Header("Air Jump Attack")]
    [SerializeField] private AnimationClip jumpAttackClip;
    [SerializeField] private bool lockMovementWhileJumpAttacking = true;
    [SerializeField, Min(1)] private int jumpAttackDamage = 18;
    [SerializeField, Range(0f, 1f)] private float jumpAttackHitboxStartNormalizedTime = 0.3f;
    [SerializeField, Range(0f, 1f)] private float jumpAttackHitboxEndNormalizedTime = 0.72f;
    [SerializeField, Range(0f, 1f)] private float jumpAttackHitNormalizedTime = 0.52f;
    [SerializeField, Min(0f)] private float jumpAttackHoverDuration = 0.25f;
    [SerializeField] private JumpAttackHitbox jumpAttackHitbox;

    [Header("Skill 1")]
    [SerializeField] private AnimationClip skill1Clip;
    [SerializeField] private bool lockMovementWhileUsingSkill1 = true;
    [SerializeField, Range(0f, 1f)] private float skill1HitboxStartNormalizedTime = 0.35f;
    [SerializeField, Range(0f, 1f)] private float skill1HitboxEndNormalizedTime = 0.65f;
    [SerializeField] private Skill1Hitbox skill1Hitbox;

    [Header("Skill 2")]
    [SerializeField] private AnimationClip skill2Clip;
    [SerializeField] private bool lockMovementWhileUsingSkill2 = true;
    [SerializeField, Range(0f, 1f)] private float skill2HitboxStartNormalizedTime = 0.3f;
    [SerializeField, Range(0f, 1f)] private float skill2HitboxEndNormalizedTime = 0.68f;
    [SerializeField] private Skill1Hitbox skill2Hitbox;

    [Header("Skill 3 - Ranged (U)")]
    [SerializeField] private AnimationClip skill3Clip;
    [SerializeField] private bool lockMovementWhileUsingSkill3 = true;
    [SerializeField] private Skill3Hitbox skill3Hitbox;

    [Header("Skill Input")]
    [SerializeField, Min(0.01f)] private float skillInputBufferTime = 0.15f;

    private CombatHitbox2D combatHitbox;
    private PlayerStamina stamina;
    private Health health;
    private SkillExecution skill1Execution;
    private SkillExecution skill2Execution;
    private CharacterAnimationDriver animationDriver;
    private PlayerMotor2D motor;
    private Hurtbox2D basicAttackTarget;
    private Collider2D basicAttackTargetCollider;
    private bool hadBasicAttackTarget;
    private bool attackQueuedAfterDash;
    private bool isAttacking;
    private bool isJumpAttacking;
    private bool isGrounded;
    private float attackTimer;
    private float attackImpactTimer;
    private bool attackDamageApplied;
    private int comboStep;
    private bool comboQueued;
    private bool isJumpAttackHovering;
    private float jumpAttackHoverTimer;
    private float attackInputBufferTimer;
    private bool isUsingSkill;
    private PlayerSkill activeSkill;
    private float skillTimer;
    private float skill1Duration;
    private float skill2Duration;
    private float skill3Duration;
    private float jumpAttackDuration;
    private bool isSkillHitboxActive;
    private float skill1InputBufferTimer;
    private float skill2InputBufferTimer;
    private float skill3InputBufferTimer;
    private bool skill1QueuedAfterDash;
    private bool skill2QueuedAfterDash;
    private bool skill3QueuedAfterDash;
    private float currentFacingDirection = 1f;

    public bool IsAttacking => isAttacking;
    public bool IsUsingSkill => isUsingSkill;
    public bool IsExecutingSkill(PlayerSkill skill) => isUsingSkill && activeSkill == skill;
    public bool IsSkillConfigured(PlayerSkill skill) => skill != PlayerSkill.Skill3 ||
        (skill3Clip != null && skill3Hitbox != null && skill3Hitbox.IsConfigured);
    public bool IsUsingRangedSkill => isUsingSkill && activeSkill == PlayerSkill.Skill3;
    public Hurtbox2D BasicAttackTarget => basicAttackTarget;
    public bool IsJumpAttackPositionLocked => isJumpAttacking;
    public bool IsBusy => isAttacking || isUsingSkill;
    public bool LocksMovement =>
        (motor != null && motor.IsAttackApproaching) ||
        (isJumpAttacking && lockMovementWhileJumpAttacking) ||
        (isAttacking && !isJumpAttacking && lockMovementWhileAttacking) ||
        (isUsingSkill && (activeSkill == PlayerSkill.Skill1 ? lockMovementWhileUsingSkill1 :
            activeSkill == PlayerSkill.Skill2 ? lockMovementWhileUsingSkill2 : lockMovementWhileUsingSkill3));
    public bool CanJump => !isUsingSkill && !isJumpAttacking && (motor == null || !motor.IsAttackApproaching) && (!isAttacking || !lockMovementWhileAttacking);

    private void Awake()
    {
        combatHitbox = GetComponent<CombatHitbox2D>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<Health>();
        animationDriver = GetComponent<CharacterAnimationDriver>();
        motor = GetComponent<PlayerMotor2D>();
        if (basicAttackHitbox == null)
        {
            Transform authoredHitbox = transform.Find("AttackHitbox");
            if (authoredHitbox != null)
                basicAttackHitbox = authoredHitbox.GetComponent<BoxCollider2D>();
        }
        skill1Hitbox ??= FindSkillHitbox(PlayerSkill.Skill1);
        skill2Hitbox ??= FindSkillHitbox(PlayerSkill.Skill2);
        skill3Hitbox ??= GetComponentInChildren<Skill3Hitbox>(true);
        jumpAttackHitbox ??= GetComponentInChildren<JumpAttackHitbox>(true);
        attack2Duration = attack2Clip != null ? attack2Clip.length : attack2Duration;
        skill1Duration = skill1Clip != null ? skill1Clip.length : 1f;
        skill2Duration = skill2Clip != null ? skill2Clip.length : 1.5f;
        skill3Duration = skill3Clip != null ? skill3Clip.length : 0.75f;
        jumpAttackDuration = jumpAttackClip != null ? jumpAttackClip.length : 0.75f;
        skill1Execution = new SkillExecution(skill1Hitbox, skill1HitboxStartNormalizedTime, skill1HitboxEndNormalizedTime);
        skill2Execution = new SkillExecution(skill2Hitbox, skill2HitboxStartNormalizedTime, skill2HitboxEndNormalizedTime);
        skill1Hitbox?.SetEnabled(false);
        skill2Hitbox?.SetEnabled(false);
        skill3Hitbox?.Cancel();
        jumpAttackHitbox?.SetEnabled(false);
    }

    private void OnValidate()
    {
        jumpAttackDamage = Mathf.Max(1, jumpAttackDamage);
        skill1HitboxEndNormalizedTime = Mathf.Max(skill1HitboxStartNormalizedTime, skill1HitboxEndNormalizedTime);
        skill2HitboxEndNormalizedTime = Mathf.Max(skill2HitboxStartNormalizedTime, skill2HitboxEndNormalizedTime);
        jumpAttackHitboxEndNormalizedTime = Mathf.Max(jumpAttackHitboxStartNormalizedTime, jumpAttackHitboxEndNormalizedTime);
        jumpAttackHitNormalizedTime = Mathf.Clamp(jumpAttackHitNormalizedTime, jumpAttackHitboxStartNormalizedTime, jumpAttackHitboxEndNormalizedTime);
    }

    public void Tick(PlayerInputFrame input, bool isDashing, float facingDirection, bool grounded)
    {
        currentFacingDirection = facingDirection;
        isGrounded = grounded;
        BufferAttackInput(input.AttackPressed, isDashing);
        BufferSkillInput(input.Skill1Pressed, input.Skill2Pressed, input.Skill3Pressed, isDashing);

        if (isDashing)
            return;

        HandleSkillInput();
        if (!isUsingSkill)
            HandleAttackInput();
    }

    public void CancelActions()
    {
        GameAudioManager.StopCue(GameAudioCue.PlayerAttack);
        GameAudioManager.StopCue(GameAudioCue.PlayerJumpAttack);
        GameAudioManager.StopCue(GameAudioCue.PlayerSkill1);
        GameAudioManager.StopCue(GameAudioCue.PlayerSkill2);
        GameAudioManager.StopCue(GameAudioCue.PlayerSkill3);
        motor?.CancelAttackApproach();
        basicAttackTarget = null;
        basicAttackTargetCollider = null;
        hadBasicAttackTarget = false;
        attackQueuedAfterDash = false;
        health?.SetSkill2Invulnerable(false);
        isAttacking = false;
        isJumpAttacking = false;
        isJumpAttackHovering = false;
        animationDriver?.SetJumpAttackPoseHeld(false);
        attackTimer = 0f;
        jumpAttackHoverTimer = 0f;
        attackImpactTimer = 0f;
        attackDamageApplied = false;
        comboStep = 0;
        comboQueued = false;
        attackInputBufferTimer = 0f;
        skill1InputBufferTimer = 0f;
        skill2InputBufferTimer = 0f;
        skill3InputBufferTimer = 0f;
        skill1QueuedAfterDash = false;
        skill2QueuedAfterDash = false;
        skill3QueuedAfterDash = false;
        skill1Execution?.Cancel();
        skill2Execution?.Cancel();
        skill3Hitbox?.Cancel();
        if (IsUsingRangedSkill)
            stamina?.SetSkill3RecoveryBlocked(false);
        if (isUsingSkill)
            animationDriver?.FinishSkill();
        isUsingSkill = false;
        isSkillHitboxActive = false;
        skill1Hitbox?.SetEnabled(false);
        skill2Hitbox?.SetEnabled(false);
        jumpAttackHitbox?.SetEnabled(false);
        animationDriver?.SetAttacking(false);
    }

    public void ApplyBasicAttackFromAnimation()
    {
        DealDamageToTarget();
    }

    public void SetLegacySkillHitboxEnabled(PlayerSkill skill, bool isEnabled)
    {
        if (skill == PlayerSkill.Skill3)
            return;
        Skill1Hitbox hitbox = skill == PlayerSkill.Skill1 ? skill1Hitbox : skill2Hitbox;
        hitbox?.SetEnabled(isEnabled);
    }

    public void ApplySkillPulseFromAnimation(PlayerSkill skill, int pulseIndex)
    {
        if (!isUsingSkill || skill != activeSkill || skill == PlayerSkill.Skill3)
            return;

        SkillExecution execution = skill == PlayerSkill.Skill1 ? skill1Execution : skill2Execution;
        Skill1Hitbox hitbox = skill == PlayerSkill.Skill1 ? skill1Hitbox : skill2Hitbox;
        execution?.TryApplyAnimationPulse(pulseIndex, hitbox);
    }

    private void BufferAttackInput(bool attackPressed, bool isDashing)
    {
        if (isDashing)
        {
            attackQueuedAfterDash |= attackPressed;
            return;
        }
        if (attackQueuedAfterDash)
        {
            attackInputBufferTimer = attackInputBufferTime;
            attackQueuedAfterDash = false;
            return;
        }
        if (attackPressed)
            attackInputBufferTimer = attackInputBufferTime;
        else if (attackInputBufferTimer > 0f)
            attackInputBufferTimer = Mathf.Max(0f, attackInputBufferTimer - Time.deltaTime);
    }

    private void HandleAttackInput()
    {
        if (isAttacking)
        {
            if (isJumpAttacking)
            {
                TickJumpAttack();
                return;
            }

            UpdateBasicAttackFacing();

            if (comboStep == 1 && !comboQueued && attackInputBufferTimer > 0f)
            {
                comboQueued = true;
                attackInputBufferTimer = 0f;
            }

            if (comboStep == 2 && !comboQueued && attackInputBufferTimer > 0f)
            {
                comboQueued = true;
                attackInputBufferTimer = 0f;
            }

            attackTimer -= Time.deltaTime;
            attackImpactTimer -= Time.deltaTime;
            if (motor != null && motor.IsAttackApproaching)
                return;
            if (!attackDamageApplied && attackImpactTimer <= 0f)
                DealDamageToTarget();

            if (comboStep == 2)
            {
                bool hasAttack2State = animationDriver != null && animationDriver.HasAttack2AnimationState;
                bool attack2Finished = hasAttack2State && animationDriver.IsAttack2AnimationFinished();
                if (!attack2Finished && (!hasAttack2State || !animationDriver.HasObservedAttack2Animation))
                    attack2Finished = attackTimer <= -AnimationCompletionFallbackBuffer;

                if (attack2Finished)
                    FinishAttack2();

                return;
            }

            if (attackTimer <= 0f)
            {
                if (comboStep == 1 && comboQueued)
                {
                    comboStep = 2;
                    comboQueued = false;
                    ClearBasicAttackTarget();
                    StartAttackWindow(attack2Duration);
                    animationDriver?.TriggerAttack2();
                }
                else
                {
                    isAttacking = false;
                    attackDamageApplied = false;
                    comboStep = 0;
                    comboQueued = false;
                    ClearBasicAttackTarget();
                    animationDriver?.SetAttacking(false);
                }
            }

            return;
        }

        if (attackInputBufferTimer <= 0f)
            return;

        attackInputBufferTimer = 0f;
        isAttacking = true;

        if (!isGrounded)
        {
            StartJumpAttack();
            return;
        }

        comboStep = 1;
        comboQueued = false;
        StartAttackWindow(attack1Duration);
        animationDriver?.SetAttacking(true);
        animationDriver?.TriggerAttack1();
    }

    private void StartJumpAttack()
    {
        GameAudioManager.PlayCueForDuration(GameAudioCue.PlayerJumpAttack, jumpAttackDuration);
        isJumpAttacking = true;
        comboStep = 0;
        comboQueued = false;
        attackTimer = jumpAttackDuration;
        attackDamageApplied = false;
        jumpAttackHitbox?.SetEnabled(false);
        animationDriver?.SetAttacking(true);
        animationDriver?.TriggerJumpAttack();
    }

    private void TickJumpAttack()
    {
        if (!comboQueued && attackInputBufferTimer > 0f)
        {
            comboQueued = true;
            attackInputBufferTimer = 0f;
        }

        if (isJumpAttackHovering)
        {
            jumpAttackHoverTimer -= Time.deltaTime;
            if (jumpAttackHoverTimer <= 0f)
                FinishJumpAttack();

            return;
        }

        attackTimer -= Time.deltaTime;
        float normalizedTime = 1f - Mathf.Clamp01(attackTimer / Mathf.Max(0.01f, jumpAttackDuration));
        bool hitboxActive = normalizedTime >= jumpAttackHitboxStartNormalizedTime && normalizedTime <= jumpAttackHitboxEndNormalizedTime;
        jumpAttackHitbox?.SetEnabled(hitboxActive);

        if (!attackDamageApplied && normalizedTime >= jumpAttackHitNormalizedTime)
        {
            attackDamageApplied = true;
            jumpAttackHitbox?.ApplyDamagePulse(jumpAttackDamage);
        }

        bool hasJumpAttackState = animationDriver != null && animationDriver.HasJumpAttackAnimationState;
        bool jumpAttackFinished = hasJumpAttackState && animationDriver.IsJumpAttackAnimationFinished();
        if (!jumpAttackFinished && (!hasJumpAttackState || !animationDriver.HasObservedJumpAttackAnimation))
            jumpAttackFinished = attackTimer <= -AnimationCompletionFallbackBuffer;
        if (!jumpAttackFinished)
            return;

        jumpAttackHitbox?.SetEnabled(false);

        if (!isGrounded && jumpAttackHoverDuration > 0f)
        {
            isJumpAttackHovering = true;
            jumpAttackHoverTimer = jumpAttackHoverDuration;
            animationDriver?.SetJumpAttackPoseHeld(true);
            return;
        }

        FinishJumpAttack();
    }

    private void FinishJumpAttack()
    {
        jumpAttackHitbox?.SetEnabled(false);
        isJumpAttackHovering = false;
        jumpAttackHoverTimer = 0f;
        animationDriver?.SetJumpAttackPoseHeld(false);
        isJumpAttacking = false;
        isAttacking = false;
        attackTimer = 0f;
        attackImpactTimer = 0f;
        attackDamageApplied = false;
        attackInputBufferTimer = 0f;
        comboStep = 0;
        comboQueued = false;
        ClearBasicAttackTarget();
        animationDriver?.SetAttacking(false);
    }

    private void FinishAttack2()
    {
        if (comboQueued)
        {
            comboStep = 1;
            comboQueued = false;
            StartAttackWindow(attack1Duration);
            animationDriver?.TriggerAttack1();
            return;
        }

        isAttacking = false;
        attackDamageApplied = false;
        comboStep = 0;
        comboQueued = false;
        ClearBasicAttackTarget();
        animationDriver?.SetAttacking(false);
    }

    private void StartAttackWindow(float duration)
    {
        AcquireBasicAttackTarget(duration);
        GameAudioManager.StopCue(GameAudioCue.PlayerAttack);
        GameAudioManager.PlayCueForDuration(GameAudioCue.PlayerAttack, duration);
        attackTimer = duration;
        attackImpactTimer = duration * attackHitNormalizedTime;
        attackDamageApplied = false;
    }

    private void DealDamageToTarget()
    {
        if (!isAttacking || isJumpAttacking || attackDamageApplied || (motor != null && motor.IsAttackApproaching))
            return;

        attackDamageApplied = true;
        UpdateBasicAttackFacing();
        if (autoTargetBasicAttacks)
        {
            if (!hadBasicAttackTarget)
                FindBasicAttackTarget();
            if (basicAttackTarget == null || !basicAttackTarget.IsAlive || !basicAttackTarget.isActiveAndEnabled ||
                basicAttackTargetCollider == null || !basicAttackTargetCollider.enabled ||
                !combatHitbox.HasClearPath(transform.position, basicAttackTargetCollider,
                    motor != null ? motor.ObstructionLayers : 0))
                return;
        }
        if (basicAttackHitbox != null)
        {
            combatHitbox?.ApplyBoxPulse(basicAttackHitbox, attackDamage, false, 1,
                autoTargetBasicAttacks ? basicAttackTarget : null);
            return;
        }
        Vector2 attackOrigin = (Vector2)transform.position + Vector2.right * currentFacingDirection * attackRange * 0.5f;
        combatHitbox?.ApplyCirclePulse(attackOrigin, attackRange, attackDamage, false, 1,
            autoTargetBasicAttacks ? basicAttackTarget : null);
    }

    private Bounds GetBasicAttackBounds()
    {
        if (basicAttackHitbox != null)
            return CombatHitbox2D.GetBoxWorldBounds(basicAttackHitbox);
        Vector3 center = transform.position + Vector3.right * currentFacingDirection * attackRange * 0.5f;
        return new Bounds(center, Vector3.one * attackRange * 2f);
    }

    private void FindBasicAttackTarget()
    {
        basicAttackTarget = combatHitbox.FindNearestMonster(transform.position, basicAttackTargetRange,
            GetBasicAttackBounds(), motor != null ? motor.ObstructionLayers : 0, out basicAttackTargetCollider);
        hadBasicAttackTarget = basicAttackTarget != null;
    }

    private void AcquireBasicAttackTarget(float duration)
    {
        ClearBasicAttackTarget();
        if (!autoTargetBasicAttacks || combatHitbox == null)
            return;
        FindBasicAttackTarget();
        if (basicAttackTarget == null)
            return;
        UpdateBasicAttackFacing();
        motor?.BeginAttackApproach(basicAttackTarget, basicAttackTargetCollider, basicAttackHitbox,
            attackRange, basicAttackApproachSpeed, basicAttackTargetRange,
            Mathf.Min(basicAttackApproachDuration, Mathf.Max(0.02f, duration * attackHitNormalizedTime)));
    }

    private void UpdateBasicAttackFacing()
    {
        if (basicAttackTarget == null || !basicAttackTarget.IsAlive || basicAttackTargetCollider == null)
            return;
        motor?.FaceAttackTarget(basicAttackTargetCollider);
        if (motor != null)
            currentFacingDirection = motor.FacingDirection;
    }

    private void ClearBasicAttackTarget()
    {
        motor?.CancelAttackApproach();
        basicAttackTarget = null;
        basicAttackTargetCollider = null;
        hadBasicAttackTarget = false;
    }

    private void BufferSkillInput(bool skill1Pressed, bool skill2Pressed, bool skill3Pressed, bool isDashing)
    {
        if (isDashing)
        {
            skill1QueuedAfterDash |= skill1Pressed;
            skill2QueuedAfterDash |= skill2Pressed;
            skill3QueuedAfterDash |= skill3Pressed;
            return;
        }

        skill1InputBufferTimer = skill1Pressed || skill1QueuedAfterDash
            ? skillInputBufferTime
            : Mathf.Max(0f, skill1InputBufferTimer - Time.deltaTime);
        skill2InputBufferTimer = skill2Pressed || skill2QueuedAfterDash
            ? skillInputBufferTime
            : Mathf.Max(0f, skill2InputBufferTimer - Time.deltaTime);
        skill3InputBufferTimer = skill3Pressed || skill3QueuedAfterDash
            ? skillInputBufferTime
            : Mathf.Max(0f, skill3InputBufferTimer - Time.deltaTime);
        skill1QueuedAfterDash = false;
        skill2QueuedAfterDash = false;
        skill3QueuedAfterDash = false;
    }

    private void HandleSkillInput()
    {
        if (isUsingSkill)
        {
            skillTimer -= Time.deltaTime;
            UpdateActiveSkillHitboxWindow();
            bool finished = skillTimer <= 0f;
            if (activeSkill == PlayerSkill.Skill3 && animationDriver != null && animationDriver.HasSkillAnimationState(activeSkill))
            {
                finished = animationDriver.IsSkillAnimationFinished(activeSkill);
                if (!animationDriver.HasObservedSkillAnimation)
                    finished = skillTimer <= -AnimationCompletionFallbackBuffer;
            }
            if (!finished)
                return;
            EndSkill();
        }

        if (isAttacking)
            return;

        if (skill1InputBufferTimer > 0f)
        {
            StartSkill(PlayerSkill.Skill1);
            return;
        }

        if (skill2InputBufferTimer > 0f)
        {
            StartSkill(PlayerSkill.Skill2);
            return;
        }
        if (skill3InputBufferTimer > 0f)
            StartSkill(PlayerSkill.Skill3);
    }

    private void StartSkill(PlayerSkill skill)
    {
        if (skill == PlayerSkill.Skill1)
            skill1InputBufferTimer = 0f;
        else if (skill == PlayerSkill.Skill2)
            skill2InputBufferTimer = 0f;
        else
            skill3InputBufferTimer = 0f;

        if (skill == PlayerSkill.Skill3 && (skill3Clip == null || skill3Hitbox == null || !skill3Hitbox.IsConfigured))
            return;

        if (stamina != null && !stamina.TrySpendSkill(skill))
            return;

        activeSkill = skill;
        isUsingSkill = true;
        skillTimer = skill == PlayerSkill.Skill1 ? skill1Duration : skill == PlayerSkill.Skill2 ? skill2Duration : skill3Duration;
        GameAudioManager.PlayCueForDuration(skill == PlayerSkill.Skill1 ? GameAudioCue.PlayerSkill1 :
            skill == PlayerSkill.Skill2 ? GameAudioCue.PlayerSkill2 : GameAudioCue.PlayerSkill3,
            skillTimer / Mathf.Max(0.01f, animationDriver != null ? animationDriver.PlaybackSpeed : 1f));
        if (skill == PlayerSkill.Skill1)
        {
            skill1Execution.Begin();
        }
        else if (skill == PlayerSkill.Skill2)
        {
            skill2Execution.Begin();
            health?.SetSkill2Invulnerable(true);
        }
        else
        {
            skill3Hitbox.Begin();
            stamina?.SetSkill3RecoveryBlocked(true);
        }

        SetActiveSkillHitboxEnabled(false);
        animationDriver?.TriggerSkill(skill);
    }

    private void EndSkill()
    {
        if (activeSkill == PlayerSkill.Skill1)
            skill1Execution.Cancel();
        else if (activeSkill == PlayerSkill.Skill2)
        {
            skill2Execution.Cancel();
            health?.SetSkill2Invulnerable(false);
        }
        else
        {
            skill3Hitbox?.Cancel();
            stamina?.SetSkill3RecoveryBlocked(false);
        }

        isUsingSkill = false;
        SetActiveSkillHitboxEnabled(false);
        animationDriver?.FinishSkill();
    }

    private void UpdateActiveSkillHitboxWindow()
    {
        if (activeSkill == PlayerSkill.Skill3)
        {
            float progress = 1f - Mathf.Clamp01(skillTimer / Mathf.Max(0.01f, skill3Duration));
            if (animationDriver != null && animationDriver.TryGetSkillNormalizedTime(activeSkill, out float animationProgress))
                progress = animationProgress;
            skill3Hitbox?.Tick(progress);
            return;
        }
        float duration = activeSkill == PlayerSkill.Skill1 ? skill1Duration : skill2Duration;
        if (duration <= 0f)
            return;

        float startTime = activeSkill == PlayerSkill.Skill1 ? skill1HitboxStartNormalizedTime : skill2HitboxStartNormalizedTime;
        float endTime = activeSkill == PlayerSkill.Skill1 ? skill1HitboxEndNormalizedTime : skill2HitboxEndNormalizedTime;
        float normalizedTime = 1f - Mathf.Clamp01(skillTimer / duration);
        bool shouldBeActive = normalizedTime >= startTime && normalizedTime <= endTime;

        if (shouldBeActive != isSkillHitboxActive)
            SetActiveSkillHitboxEnabled(shouldBeActive);

        SkillExecution execution = activeSkill == PlayerSkill.Skill1 ? skill1Execution : skill2Execution;
        Skill1Hitbox hitbox = activeSkill == PlayerSkill.Skill1 ? skill1Hitbox : skill2Hitbox;
        execution?.Tick(Mathf.Min(normalizedTime, endTime), hitbox);
    }

    private void SetActiveSkillHitboxEnabled(bool isEnabled)
    {
        isSkillHitboxActive = isEnabled;
        if (activeSkill == PlayerSkill.Skill3)
            return;
        Skill1Hitbox hitbox = activeSkill == PlayerSkill.Skill1 ? skill1Hitbox : skill2Hitbox;
        hitbox?.SetEnabled(isEnabled);
    }

    private Skill1Hitbox FindSkillHitbox(PlayerSkill skill)
    {
        Skill1Hitbox[] hitboxes = GetComponentsInChildren<Skill1Hitbox>(true);
        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (hitboxes[i].Skill == skill)
                return hitboxes[i];
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float facingDirection = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 attackOrigin = transform.position + Vector3.right * facingDirection * attackRange * 0.5f;
        Gizmos.DrawWireSphere(attackOrigin, attackRange);
    }

    private void OnDisable() => CancelActions();
}
