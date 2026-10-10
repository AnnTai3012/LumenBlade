using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMotor2D))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(CharacterAnimationDriver))]
[RequireComponent(typeof(CombatHitbox2D))]
[RequireComponent(typeof(PlayerInteraction))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private CharacterAnimationDriver animationDriver;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField] private PlayerConsumables consumables;

    [Header("Fall Damage")]
    [SerializeField, Min(0f)] private float safeFallMaxJumpHeightMultiplier = 2.5f;
    [SerializeField, Min(0)] private int baseFallDamage = 10;
    [SerializeField, Min(0)] private int additionalFallDamagePerTile = 5;

    private void Awake()
    {
        health ??= GetComponent<Health>();
        inputReader ??= GetComponent<PlayerInputReader>();
        motor ??= GetComponent<PlayerMotor2D>();
        combat ??= GetComponent<PlayerCombat>();
        animationDriver ??= GetComponent<CharacterAnimationDriver>();
        interaction ??= GetComponent<PlayerInteraction>();
        consumables ??= GetComponent<PlayerConsumables>();
    }

    private void OnEnable()
    {
        if (motor != null)
            motor.LandedFromFall += HandleLandedFromFall;

        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnDied += HandleDeath;
            health.OnRevived += HandleRevived;
        }
    }

    private void OnDisable()
    {
        if (motor != null)
            motor.LandedFromFall -= HandleLandedFromFall;

        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDied -= HandleDeath;
            health.OnRevived -= HandleRevived;
        }

        combat?.CancelActions();
        motor?.CancelTransientInput();
        animationDriver?.SetDashing(false);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || GameplayPanelController.BlocksGameplayInput)
        {
            SuspendForPanel();
            return;
        }
        if (health != null && health.IsDead)
            return;

        PlayerInputFrame input = inputReader.ReadFrame();
        interaction?.TryInteract(input.InteractPressed);
        if (Time.timeScale <= 0f)
        {
            SuspendForPanel();
            return;
        }
        interaction?.TryPickup(input.PickupPressed);
        if (input.HealItemPressed) consumables?.TryUse(ConsumableKind.CryHeal);
        if (input.ShieldItemPressed) consumables?.TryUse(ConsumableKind.CryShield);
        TickActions(input);
    }

    private void TickActions(PlayerInputFrame input)
    {
        bool movementWasLocked = combat.LocksMovement;
        motor.UpdateInputState(input, movementWasLocked);
        motor.TickDash(input, combat.IsBusy);
        combat.Tick(input, motor.IsDashing, motor.FacingDirection, motor.IsGrounded);
        // Apply action entry/exit before the next physics step, including a buffered dash handoff.
        if (combat.LocksMovement != movementWasLocked)
            motor.UpdateInputState(input, combat.LocksMovement);
        motor.SetJumpAttackPositionLocked(combat.IsJumpAttackPositionLocked);
        // A queued dash can start in the same frame that an attack/skill releases control.
        motor.TryStartBufferedDash(combat.IsBusy);
        motor.HandleJumpInput(input, combat.CanJump && !motor.IsDashing);
        animationDriver.TickMovement(motor.Velocity, motor.IsGrounded, motor.IsDashing, motor.HasStartedJump);
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
            return;

        motor.FixedTick();
    }

    public void SuspendForPanel()
    {
        combat?.CancelActions();
        motor?.CancelTransientInput();
        animationDriver?.SetDashing(false);
    }

    private void HandleDeath()
    {
        GameAudioManager.PlayCue(GameAudioCue.PlayerDeath);
        combat.CancelActions();
        motor.StopAndDisablePhysics();
        animationDriver.SetDashing(false);
        animationDriver.PlayDeath();
    }

    private void HandleRevived()
    {
        combat?.CancelActions();
        motor?.RestorePhysicsAfterRevive();
        animationDriver?.ResetAfterRevive();
        GameAudioManager.PlayCue(GameAudioCue.PlayerRevive);
    }

    private void HandleDamaged()
    {
        GameAudioManager.PlayCue(GameAudioCue.PlayerTakeHit);
        if (combat != null && combat.IsUsingRangedSkill)
            combat.CancelActions();
        if (health != null && !health.IsDead)
            animationDriver?.PlayTakeHit();
    }

    private void HandleLandedFromFall(float fallDistance)
    {
        if (health == null || motor == null || health.IsDead || fallDistance <= 0f)
            return;

        GameAudioManager.PlayCue(GameAudioCue.PlayerFallImpact);

        float tileHeight = Mathf.Max(0.01f, motor.GroundTileHeight);
        float safeFallDistance = Mathf.Max(0f, motor.MaxJumpHeight * safeFallMaxJumpHeightMultiplier);
        if (fallDistance <= safeFallDistance)
            return;

        int additionalDamagingTiles = Mathf.FloorToInt((fallDistance - safeFallDistance) / tileHeight);
        long damage = Mathf.Max(0, baseFallDamage) +
                      (long)additionalDamagingTiles * Mathf.Max(0, additionalFallDamagePerTile);
        health.TakeDamage(damage >= int.MaxValue ? int.MaxValue : (int)damage);
    }

    // These public methods remain as bridges for existing Animator Events.
    public void EnableWeaponHitbox() => combat?.ApplyBasicAttackFromAnimation();
    public void DisableWeaponHitbox() { }
    public void EnableSkill1Hitbox() => combat?.SetLegacySkillHitboxEnabled(PlayerSkill.Skill1, true);
    public void DisableSkill1Hitbox() => combat?.SetLegacySkillHitboxEnabled(PlayerSkill.Skill1, false);
    public void EnableSkill2Hitbox() => combat?.SetLegacySkillHitboxEnabled(PlayerSkill.Skill2, true);
    public void DisableSkill2Hitbox() => combat?.SetLegacySkillHitboxEnabled(PlayerSkill.Skill2, false);
    public void ApplySkillPulseFromAnimation(PlayerSkill skill, int pulseIndex) => combat?.ApplySkillPulseFromAnimation(skill, pulseIndex);
}
