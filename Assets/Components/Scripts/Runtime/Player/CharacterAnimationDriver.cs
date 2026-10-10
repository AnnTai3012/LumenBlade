using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterAnimationDriver : MonoBehaviour
{
    private const float MinJumpAnimationSpeed = 0.15f;
    private const float MinFallAnimationSpeed = 0.15f;

    [SerializeField] private Animator animator;
    [SerializeField] private float animSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float fallAnimationStartDelay = 0.08f;

    private bool isAttacking;
    private bool isDashing;
    private bool isJumping;
    private bool isFalling;
    private bool hasCachedAirborneParameters;
    private bool animatorGrounded;
    private bool hasObservedAttack2State;
    private bool hasObservedJumpAttackState;
    private bool hasObservedSkillState;
    private bool isJumpAttackPoseHeld;
    private float animatorSpeedBeforeJumpAttackHold = 1f;
    private float fallingTime;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int IsFallParam = Animator.StringToHash("isFall");
    private static readonly int Attack1Param = Animator.StringToHash("Attack1");
    private static readonly int Attack2Param = Animator.StringToHash("Attack2");
    private static readonly int JumpAttackParam = Animator.StringToHash("JumpAttack");
    private static readonly int Attack2StateHash = Animator.StringToHash("Base Layer.Atk2");
    private static readonly int Attack1StateHash = Animator.StringToHash("Base Layer.Atk1");
    private static readonly int DashStateHash = Animator.StringToHash("Base Layer.Dash");
    private static readonly int JumpAttackStateHash = Animator.StringToHash("Base Layer.JumpAttack");
    private static readonly int Skill1StateHash = Animator.StringToHash("Base Layer.Skill_1");
    private static readonly int Skill2StateHash = Animator.StringToHash("Base Layer.Skill_2");
    private static readonly int Skill3StateHash = Animator.StringToHash("Base Layer.Skill_3");
    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.idle");
    private static readonly int RunStateHash = Animator.StringToHash("Base Layer.Run");
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.Jump");
    private static readonly int FallStateHash = Animator.StringToHash("Base Layer.Fall");
    private static readonly int IsAttackingParam = Animator.StringToHash("IsAttacking");
    private static readonly int Skill1Param = Animator.StringToHash("Skill1");
    private static readonly int Skill2Param = Animator.StringToHash("Skill2");
    private static readonly int Skill3Param = Animator.StringToHash("Skill3");
    private static readonly int IsUsingSkillParam = Animator.StringToHash("IsUsingSkill");
    private static readonly int IsDashingParam = Animator.StringToHash("IsDashing");
    private static readonly int DeathParam = Animator.StringToHash("Death");
    private static readonly int TakeHitParam = Animator.StringToHash("TakeHit");
    private static readonly int DeathStateHash = Animator.StringToHash("Base Layer.Death");
    private static readonly int TakeHitStateHash = Animator.StringToHash("Base Layer.TakeHit");

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // Animator state may reset while this component is disabled; resync once on re-enable.
        hasCachedAirborneParameters = false;
    }

    public void TickMovement(Vector2 velocity, bool isGrounded, bool dashing, bool jumpStarted)
    {
        if (animator == null)
            return;

        animator.SetFloat(SpeedParam, Mathf.Abs(velocity.x), animSmoothTime, Time.deltaTime);
        bool shouldJump = jumpStarted && !dashing && velocity.y > MinJumpAnimationSpeed;
        bool shouldBeGrounded = isGrounded && !shouldJump;
        bool isDescending = !isGrounded && !dashing && velocity.y < -MinFallAnimationSpeed;
        fallingTime = isDescending ? fallingTime + Time.deltaTime : 0f;
        bool shouldFall = isDescending && fallingTime >= fallAnimationStartDelay;

        if (!hasCachedAirborneParameters || animatorGrounded != shouldBeGrounded)
        {
            animatorGrounded = shouldBeGrounded;
            animator.SetBool(IsGroundedParam, animatorGrounded);
        }

        if (!hasCachedAirborneParameters || isJumping != shouldJump)
        {
            isJumping = shouldJump;
            animator.SetBool(IsJumpingParam, isJumping);
        }

        if (!hasCachedAirborneParameters || isFalling != shouldFall)
        {
            isFalling = shouldFall;
            animator.SetBool(IsFallParam, isFalling);
        }

        hasCachedAirborneParameters = true;
        SetDashing(dashing);
    }

    public void SetAttacking(bool value)
    {
        if (animator == null || isAttacking == value)
            return;

        isAttacking = value;
        animator.SetBool(IsAttackingParam, value);
    }

    public void TriggerAttack1()
    {
        if (animator == null)
            return;
        bool leavingDash = isDashing || animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DashStateHash;
        SetDashing(false);
        if (leavingDash && animator.HasState(0, Attack1StateHash))
        {
            animator.ResetTrigger(Attack1Param);
            animator.Play(Attack1StateHash, 0, 0f);
        }
        else
            animator.SetTrigger(Attack1Param);
    }

    public void TriggerAttack2()
    {
        hasObservedAttack2State = false;
        animator?.SetTrigger(Attack2Param);
    }

    public void TriggerJumpAttack()
    {
        hasObservedJumpAttackState = false;
        animator?.SetTrigger(JumpAttackParam);
    }

    public bool HasAttack2AnimationState => animator != null && animator.HasState(0, Attack2StateHash);
    public float PlaybackSpeed => animator != null ? Mathf.Abs(animator.speed) : 1f;
    public bool HasJumpAttackAnimationState => animator != null && animator.HasState(0, JumpAttackStateHash);
    public bool HasObservedAttack2Animation => hasObservedAttack2State;
    public bool HasObservedJumpAttackAnimation => hasObservedJumpAttackState;
    public bool IsAttack2AnimationFinished() => IsAnimationFinished(Attack2StateHash, ref hasObservedAttack2State);
    public bool IsJumpAttackAnimationFinished() => IsAnimationFinished(JumpAttackStateHash, ref hasObservedJumpAttackState);
    public bool HasObservedSkillAnimation => hasObservedSkillState;
    public bool HasSkillAnimationState(PlayerSkill skill) => animator != null && animator.HasState(0, SkillStateHash(skill));
    public bool IsSkillAnimationFinished(PlayerSkill skill) => IsAnimationFinished(SkillStateHash(skill), ref hasObservedSkillState);
    private static int SkillStateHash(PlayerSkill skill) => skill == PlayerSkill.Skill1 ? Skill1StateHash :
        skill == PlayerSkill.Skill2 ? Skill2StateHash : Skill3StateHash;

    public bool TryGetSkillNormalizedTime(PlayerSkill skill, out float progress)
    {
        progress = 0f;
        if (animator == null) return false;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != SkillStateHash(skill)) return false;
        hasObservedSkillState = true;
        progress = Mathf.Clamp01(state.normalizedTime);
        return true;
    }
    public void TriggerSkill(PlayerSkill skill)
    {
        if (animator == null)
            return;

        SetDashing(false);
        animator.ResetTrigger(Skill1Param);
        animator.ResetTrigger(Skill2Param);
        animator.ResetTrigger(Skill3Param);
        animator.SetBool(IsUsingSkillParam, true);
        hasObservedSkillState = false;
        int stateHash = SkillStateHash(skill);
        // Sprite clips need immediate entry so visuals and the damage clock start together.
        if (animator.HasState(0, stateHash))
            animator.Play(stateHash, 0, 0f);
        else
            animator.SetTrigger(skill == PlayerSkill.Skill1 ? Skill1Param : skill == PlayerSkill.Skill2 ? Skill2Param : Skill3Param);
    }

    public void FinishSkill()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(Skill1Param);
        animator.ResetTrigger(Skill2Param);
        animator.ResetTrigger(Skill3Param);
        animator.SetBool(IsUsingSkillParam, false);
        int current = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        int next = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0).fullPathHash : 0;
        if (animator.GetBool(DeathParam) || current == TakeHitStateHash || next == TakeHitStateHash)
            return;
        if (current != Skill1StateHash && current != Skill2StateHash && current != Skill3StateHash &&
            next != Skill1StateHash && next != Skill2StateHash && next != Skill3StateHash)
            return;

        int state = animatorGrounded
            ? (animator.GetFloat(SpeedParam) > 0.1f ? RunStateHash : IdleStateHash)
            : (isJumping ? JumpStateHash : FallStateHash);
        if (animator.HasState(0, state))
            animator.Play(state, 0, 0f);
    }

    public void SetJumpAttackPoseHeld(bool isHeld)
    {
        if (animator == null || isJumpAttackPoseHeld == isHeld)
            return;

        isJumpAttackPoseHeld = isHeld;
        if (isHeld)
        {
            animatorSpeedBeforeJumpAttackHold = animator.speed;
            animator.speed = 0f;
        }
        else
        {
            animator.speed = animatorSpeedBeforeJumpAttackHold;
        }
    }

    public void SetDashing(bool value)
    {
        if (animator == null || isDashing == value)
            return;

        isDashing = value;
        animator.SetBool(IsDashingParam, value);
    }

    public void PlayDeath()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(Attack1Param);
        animator.ResetTrigger(Attack2Param);
        animator.ResetTrigger(JumpAttackParam);
        animator.ResetTrigger(Skill1Param);
        animator.ResetTrigger(Skill2Param);
        animator.ResetTrigger(Skill3Param);
        animator.SetBool(IsUsingSkillParam, false);
        animator.ResetTrigger(TakeHitParam);
        animator.SetBool(DeathParam, true);
        animator.Play(DeathStateHash, 0, 0f);
    }

    public void ResetAfterRevive()
    {
        if (animator == null) return;
        SetJumpAttackPoseHeld(false);
        isAttacking = isDashing = isJumping = isFalling = false;
        hasCachedAirborneParameters = false;
        hasObservedAttack2State = hasObservedJumpAttackState = false;
        hasObservedSkillState = false;
        fallingTime = 0f;
        animator.Rebind();
        animator.Update(0f);
    }

    public void PlayTakeHit()
    {
        if (animator == null || animator.GetBool(DeathParam))
            return;

        animator.ResetTrigger(TakeHitParam);
        animator.SetTrigger(TakeHitParam);
    }

    private bool IsAnimationFinished(int stateHash, ref bool hasObservedState)
    {
        if (animator == null)
            return false;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash == stateHash)
        {
            hasObservedState = true;
            return currentState.normalizedTime >= 1f;
        }

        if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash == stateHash)
        {
            hasObservedState = true;
            return false;
        }

        return hasObservedState;
    }

}
