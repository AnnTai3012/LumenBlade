using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMotor2D : MonoBehaviour
{
    private const float JumpAttackReleaseFallSpeed = -0.1f;
    private const float GroundedUpwardVelocityThreshold = 0.15f;
    private const float MinimumGroundNormalY = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float groundAcceleration = 120f;
    [SerializeField] private float groundDeceleration = 110f;
    [SerializeField] private float airAcceleration = 70f;
    [SerializeField] private float airDeceleration = 55f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5.8f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.18f;
    [SerializeField] private float apexThreshold = 1.5f;
    [SerializeField] private float apexGravityMultiplier = 0.85f;
    [SerializeField] private float fallGravityMultiplier = 2.6f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.2f;
    [SerializeField] private float maxFallSpeed = 15f;
    [SerializeField, Min(0.01f)] private float fallbackTileHeight = 1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private AnimationClip dashClip;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField, Min(0f)] private float dashBufferTime = 0.18f;
    [SerializeField] private int maxAirDashes = 1;

    private Rigidbody2D body;
    private Collider2D playerBodyCollider;
    private PlayerStamina stamina;
    private Collider2D[] bodyColliders;
    private Collider2D[] solidBodyColliders;
    private readonly Collider2D[] groundCheckResults = new Collider2D[16];
    private readonly ContactPoint2D[] groundContactResults = new ContactPoint2D[16];
    private ContactFilter2D groundCheckFilter;
    private readonly List<ColliderIgnorePair> ignoredMonsterCollisionPairs = new List<ColliderIgnorePair>();
    private readonly List<Collider2D> monsterColliderResults = new List<Collider2D>(8);
    private Vector3 originalScale;
    private int facingDirection;
    private bool isGrounded;
    private float moveInput;
    private bool jumpHeld;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool canStartBufferedJump;
    private float defaultGravityScale;
    private float maxJumpHeight;
    private Collider2D currentGroundCollider;
    private Grid currentGroundGrid;
    private Grid fallbackGroundGrid;
    private RigidbodyConstraints2D originalConstraints;
    private RigidbodyConstraints2D constraintsBeforeDash;
    private bool isDashing;
    private float dashTimer;
    private float dashDuration;
    private float dashCooldownTimer;
    private float dashBufferCounter;
    private int airDashesUsed;
    private bool isJumpAttackPositionLocked;
    private float knockbackTimer;
    private bool isTrackingFall;
    private bool hasStartedJump;
    private float highestAirPositionY;
    private float nextFootstepTime;
    private Hurtbox2D attackApproachReceiver;
    private Collider2D attackApproachTarget;
    private BoxCollider2D attackApproachHitbox;
    private float attackApproachRange;
    private float attackApproachSpeed;
    private float attackApproachTimer;
    private float attackApproachDistance;
    private float attackApproachStartX;
    private readonly RaycastHit2D[] attackApproachHits = new RaycastHit2D[16];
    private const float AttackApproachInset = 0.04f;
    private const float AttackApproachSkin = 0.015f;

    private readonly struct ColliderIgnorePair
    {
        public readonly Collider2D PlayerCollider;
        public readonly Collider2D MonsterCollider;

        public ColliderIgnorePair(Collider2D playerCollider, Collider2D monsterCollider)
        {
            PlayerCollider = playerCollider;
            MonsterCollider = monsterCollider;
        }
    }

    public bool IsGrounded => isGrounded;
    public bool HasStartedJump => hasStartedJump;
    public bool IsDashing => isDashing;
    public bool IsKnockedBack => knockbackTimer > 0f;
    public bool IsAttackApproaching { get; private set; }
    public LayerMask ObstructionLayers => groundLayer;
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
    public float FacingDirection => facingDirection;
    public float MaxJumpHeight => maxJumpHeight;
    public float GroundTileHeight
    {
        get
        {
            if (currentGroundGrid == null && currentGroundCollider != null)
                currentGroundGrid = currentGroundCollider.GetComponentInParent<Grid>();

            Grid grid = currentGroundGrid != null ? currentGroundGrid : fallbackGroundGrid;
            return grid != null
                ? Mathf.Abs(grid.cellSize.y)
                : Mathf.Max(0.01f, fallbackTileHeight);
        }
    }

    public event Action<float> LandedFromFall;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerBodyCollider = GetComponent<Collider2D>();
        stamina = GetComponent<PlayerStamina>();
        bodyColliders = GetComponentsInChildren<Collider2D>(true);
        enabledCollidersBeforeDeath = new bool[bodyColliders.Length];
        int solidColliderCount = 0;
        for (int i = 0; i < bodyColliders.Length; i++)
        {
            if (bodyColliders[i] != null && !bodyColliders[i].isTrigger)
                solidColliderCount++;
        }

        solidBodyColliders = new Collider2D[solidColliderCount];
        int solidColliderIndex = 0;
        for (int i = 0; i < bodyColliders.Length; i++)
        {
            if (bodyColliders[i] != null && !bodyColliders[i].isTrigger)
                solidBodyColliders[solidColliderIndex++] = bodyColliders[i];
        }

        originalScale = transform.localScale;
        facingDirection = originalScale.x >= 0f ? 1 : -1;
        defaultGravityScale = body.gravityScale;
        maxJumpHeight = CalculateMaxJumpHeight();
        originalConstraints = body.constraints;
        dashDuration = dashClip != null ? dashClip.length : 0.2f;
        fallbackGroundGrid = FindFirstObjectByType<Grid>();
        groundCheckFilter = new ContactFilter2D();
        groundCheckFilter.SetLayerMask(groundLayer);
        groundCheckFilter.useTriggers = false;
    }

    public void UpdateInputState(PlayerInputFrame input, bool movementLocked)
    {
        if (IsKnockedBack)
        {
            moveInput = 0f;
            jumpHeld = false;
            return;
        }

        jumpHeld = input.JumpHeld;
        moveInput = movementLocked ? 0f : input.Horizontal;

        if (isDashing || IsAttackApproaching)
            return;

        if (moveInput > 0.01f)
            SetFacingDirection(1);
        else if (moveInput < -0.01f)
            SetFacingDirection(-1);
    }

    public void TickDash(PlayerInputFrame input, bool actionBusy)
    {
        dashBufferCounter = Mathf.Max(0f, dashBufferCounter - Time.deltaTime);
        if (IsKnockedBack)
        {
            dashBufferCounter = 0f;
            return;
        }

        if (input.DashPressed)
            dashBufferCounter = Mathf.Max(dashBufferTime, Time.deltaTime);

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                EndDash();
            else
                return;
        }

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        TryStartBufferedDash(actionBusy);
    }

    public void TryStartBufferedDash(bool actionBusy)
    {
        if (actionBusy || isDashing || IsKnockedBack || isJumpAttackPositionLocked ||
            body == null || !body.simulated || dashBufferCounter <= 0f)
            return;

        bool canDash = dashCooldownTimer <= 0f && (isGrounded || airDashesUsed < maxAirDashes);
        if (canDash && (stamina == null || stamina.TrySpendDash()))
        {
            dashBufferCounter = 0f;
            StartDash();
        }
    }

    public void HandleJumpInput(PlayerInputFrame input, bool canJump)
    {
        canStartBufferedJump = canJump && !isDashing && !IsAttackApproaching && !IsKnockedBack;

        if (IsKnockedBack)
            jumpBufferCounter = 0f;
        else if (input.JumpPressed)
            jumpBufferCounter = Mathf.Max(jumpBufferTime, Time.deltaTime);
        else
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);

        if (input.JumpReleased && body.linearVelocity.y > 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * 0.5f);
    }

    public void FixedTick()
    {
        if (body == null)
            return;

        TickFootsteps();

        RestoreMonsterBodyCollisions();
        CheckGrounded();
        bool landedFromFall = UpdateFallTracking(out float fallDistance);
        TryStartBufferedJump();
        if (isDashing)
        {
            NotifyFallLanded(landedFromFall, fallDistance);
            return;
        }

        using (GameplayProfilerMarkers.PlayerMovement.Auto())
        {
            if (IsAttackApproaching)
            {
                TickAttackApproach();
                ApplyBetterGravity();
            }
            else if (isJumpAttackPositionLocked)
            {
                body.linearVelocity = Vector2.zero;
            }
            else if (IsKnockedBack)
            {
                knockbackTimer = Mathf.Max(0f, knockbackTimer - Time.fixedDeltaTime);
                ApplyBetterGravity();
            }
            else
            {
                ApplyMovement();
                ApplyBetterGravity();
            }
        }

        NotifyFallLanded(landedFromFall, fallDistance);
    }

    public void ApplyKnockback(Vector2 direction, float horizontalSpeed, float verticalSpeed, float controlLockDuration)
    {
        if (body == null || !body.simulated || (horizontalSpeed <= 0f && verticalSpeed <= 0f))
            return;

        CancelAttackApproach();
        float horizontalDirection = Mathf.Abs(direction.x) > 0.001f
            ? Mathf.Sign(direction.x)
            : facingDirection;
        moveInput = 0f;
        jumpHeld = false;
        jumpBufferCounter = 0f;
        dashBufferCounter = 0f;
        knockbackTimer = Mathf.Max(knockbackTimer, Mathf.Max(0f, controlLockDuration));

        if (isJumpAttackPositionLocked)
            SetJumpAttackPositionLocked(false);

        body.linearVelocity = new Vector2(
            horizontalDirection * Mathf.Max(0f, horizontalSpeed),
            Mathf.Max(body.linearVelocity.y, Mathf.Max(0f, verticalSpeed)));
    }

    public void CancelTransientInput()
    {
        CancelAttackApproach();
        moveInput = 0f;
        jumpHeld = false;
        jumpBufferCounter = 0f;
        dashBufferCounter = 0f;
        canStartBufferedJump = false;
        coyoteTimeCounter = 0f;
        knockbackTimer = 0f;
        isTrackingFall = false;
        hasStartedJump = false;
        bool wasJumpAttackPositionLocked = isJumpAttackPositionLocked;
        isJumpAttackPositionLocked = false;

        bool wasDashing = isDashing;
        if (wasDashing) GameAudioManager.StopCue(GameAudioCue.PlayerDash);
        RestoreMonsterBodyCollisions(true);
        isDashing = false;
        dashTimer = 0f;
        if (body != null)
        {
            body.constraints = wasDashing ? constraintsBeforeDash : originalConstraints;
            body.gravityScale = defaultGravityScale;
            if (wasDashing)
                body.linearVelocity = Vector2.zero;
            else if (wasJumpAttackPositionLocked)
                body.linearVelocity = Vector2.zero;
        }
    }

    public void SetJumpAttackPositionLocked(bool isLocked)
    {
        isLocked &= !IsKnockedBack;
        if (body == null || isJumpAttackPositionLocked == isLocked)
            return;

        isJumpAttackPositionLocked = isLocked;
        body.linearVelocity = Vector2.zero;
        if (isLocked)
        {
            body.gravityScale = 0f;
            body.constraints = originalConstraints | RigidbodyConstraints2D.FreezePosition;
        }
        else
        {
            body.constraints = originalConstraints;
            body.gravityScale = defaultGravityScale;
            body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, JumpAttackReleaseFallSpeed));
        }
    }

    private bool[] enabledCollidersBeforeDeath;
    private bool deathPhysicsSuspended;
    private bool simulatedBeforeDeath;

    public void StopAndDisablePhysics()
    {
        if (deathPhysicsSuspended) return;
        deathPhysicsSuspended = true;
        CancelTransientInput();
        simulatedBeforeDeath = body.simulated;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        for (int i = 0; i < bodyColliders.Length; i++)
        {
            if (bodyColliders[i] != null)
            {
                enabledCollidersBeforeDeath[i] = bodyColliders[i].enabled;
                bodyColliders[i].enabled = false;
            }
        }
    }

    public void RestorePhysicsAfterRevive()
    {
        if (!deathPhysicsSuspended) return;
        CancelTransientInput();
        body.linearVelocity = Vector2.zero;
        for (int i = 0; i < bodyColliders.Length; i++)
            if (bodyColliders[i] != null)
                bodyColliders[i].enabled = enabledCollidersBeforeDeath[i];
        body.simulated = simulatedBeforeDeath;
        deathPhysicsSuspended = false;
    }

    private void CheckGrounded()
    {
        using (GameplayProfilerMarkers.PlayerGroundCheck.Auto())
        {
            currentGroundCollider = null;
            // Use the body's actual support contacts even when the editable probe misses the feet.
            int contactCount = body.GetContacts(groundCheckFilter, groundContactResults);
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint2D contact = groundContactResults[i];
                if (contact.normal.y < MinimumGroundNormalY)
                    continue;

                Collider2D candidate = contact.collider;
                if (candidate != null && candidate.attachedRigidbody == body)
                    candidate = contact.otherCollider;

                if (candidate == null || candidate.attachedRigidbody == body || candidate.isTrigger)
                    continue;

                currentGroundCollider = candidate;
                break;
            }

            if (currentGroundCollider == null && groundCheck != null)
            {
                int overlapCount = Physics2D.OverlapCircle(
                    groundCheck.position,
                    groundCheckRadius,
                    groundCheckFilter,
                    groundCheckResults);

                for (int i = 0; i < overlapCount; i++)
                {
                    Collider2D candidate = groundCheckResults[i];
                    if (candidate == null || candidate.attachedRigidbody == body)
                        continue;

                    if (playerBodyCollider != null)
                    {
                        ColliderDistance2D supportDistance = playerBodyCollider.Distance(candidate);
                        if (!supportDistance.isValid)
                            continue;

                        // Overlap reverses the vector between the two closest surface points.
                        float supportNormalY = supportDistance.isOverlapped
                            ? -supportDistance.normal.y
                            : supportDistance.normal.y;
                        if (supportNormalY < MinimumGroundNormalY)
                            continue;
                    }

                    currentGroundCollider = candidate;
                    break;
                }
            }

            isGrounded = currentGroundCollider != null &&
                         body.linearVelocity.y <= GroundedUpwardVelocityThreshold;
        }

        if (isGrounded && body.linearVelocity.y <= GroundedUpwardVelocityThreshold)
            hasStartedJump = false;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            airDashesUsed = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }

    private void TryStartBufferedJump()
    {
        if (!canStartBufferedJump || jumpBufferCounter <= 0f || isDashing || IsAttackApproaching || IsKnockedBack || isJumpAttackPositionLocked)
            return;

        bool hasGroundJumpSupport = isGrounded && body.linearVelocity.y <= GroundedUpwardVelocityThreshold;
        if (!hasGroundJumpSupport && coyoteTimeCounter <= 0f)
            return;

        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        hasStartedJump = true;
        GameAudioManager.PlayCue(GameAudioCue.PlayerJump);
        isGrounded = false;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private float CalculateMaxJumpHeight()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * defaultGravityScale);
        if (gravity <= Mathf.Epsilon)
            return 0f;

        float initialJumpSpeed = Mathf.Max(0f, jumpForce);
        float speedAtApexThreshold = Mathf.Min(initialJumpSpeed, Mathf.Max(0f, apexThreshold));
        float normalGravityHeight =
            (initialJumpSpeed * initialJumpSpeed - speedAtApexThreshold * speedAtApexThreshold) /
            (2f * gravity);
        float apexGravity = gravity * Mathf.Max(0.01f, apexGravityMultiplier);
        float reducedGravityHeight =
            speedAtApexThreshold * speedAtApexThreshold / (2f * apexGravity);

        return Mathf.Max(0f, normalGravityHeight + reducedGravityHeight);
    }

    private bool UpdateFallTracking(out float fallDistance)
    {
        fallDistance = 0f;

        if (isGrounded)
        {
            if (!isTrackingFall)
                return false;

            fallDistance = Mathf.Max(0f, highestAirPositionY - body.position.y);
            isTrackingFall = false;
            return true;
        }

        if (!isTrackingFall)
        {
            isTrackingFall = true;
            highestAirPositionY = body.position.y;
        }
        else
        {
            highestAirPositionY = Mathf.Max(highestAirPositionY, body.position.y);
        }

        return false;
    }

    private void NotifyFallLanded(bool landedFromFall, float fallDistance)
    {
        if (landedFromFall)
            LandedFromFall?.Invoke(fallDistance);
    }

    private void SetFacingDirection(int direction)
    {
        if (direction == facingDirection)
            return;

        facingDirection = direction;
        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x) * facingDirection,
            originalScale.y,
            originalScale.z);
    }

    public void FaceAttackTarget(Collider2D target)
    {
        if (target == null || isDashing || IsKnockedBack)
            return;
        float offset = target.bounds.center.x - transform.position.x;
        if (Mathf.Abs(offset) > 0.001f)
        {
            int previousFacing = facingDirection;
            SetFacingDirection(offset > 0f ? 1 : -1);
            // Distance/cast queries in this attack must see the mirrored collider immediately.
            if (previousFacing != facingDirection)
                Physics2D.SyncTransforms();
        }
    }

    public void BeginAttackApproach(Hurtbox2D receiver, Collider2D target, BoxCollider2D hitbox,
        float fallbackRange, float speed, float maximumDistance, float duration)
    {
        CancelAttackApproach();
        if (body == null || !body.simulated || target == null || receiver == null ||
            !isGrounded || isDashing || IsKnockedBack || isJumpAttackPositionLocked)
            return;

        FaceAttackTarget(target);
        moveInput = 0f;
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        attackApproachReceiver = receiver;
        attackApproachTarget = target;
        attackApproachHitbox = hitbox;
        attackApproachRange = fallbackRange;
        attackApproachSpeed = Mathf.Max(0f, speed);
        attackApproachTimer = Mathf.Max(0f, duration);
        attackApproachDistance = Mathf.Max(0f, maximumDistance);
        attackApproachStartX = body.position.x;
        IsAttackApproaching = attackApproachSpeed > 0f && attackApproachTimer > 0f && GetAttackApproachGap() > 0f;
    }

    public void CancelAttackApproach()
    {
        if (IsAttackApproaching && body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        IsAttackApproaching = false;
        attackApproachReceiver = null;
        attackApproachTarget = null;
        attackApproachHitbox = null;
        attackApproachTimer = 0f;
    }

    private float GetAttackApproachGap()
    {
        if (attackApproachTarget == null)
            return 0f;

        if (attackApproachHitbox != null)
        {
            ColliderDistance2D separation = attackApproachHitbox.Distance(attackApproachTarget);
            if (separation.isValid)
                return separation.isOverlapped ? 0f : Mathf.Max(0f,
                    facingDirection * (separation.pointB.x - separation.pointA.x) + AttackApproachInset);

            Bounds attackBounds = CombatHitbox2D.GetBoxWorldBounds(attackApproachHitbox);
            return Mathf.Max(0f, facingDirection > 0
                ? attackApproachTarget.bounds.min.x - attackBounds.max.x + AttackApproachInset
                : attackBounds.min.x - attackApproachTarget.bounds.max.x + AttackApproachInset);
        }

        Vector2 origin = body.position + Vector2.right * facingDirection * attackApproachRange * 0.5f;
        Vector2 nearest = attackApproachTarget.ClosestPoint(origin);
        if ((nearest - origin).sqrMagnitude <= attackApproachRange * attackApproachRange)
            return 0f;
        float dy = nearest.y - origin.y;
        float horizontalReach = Mathf.Sqrt(Mathf.Max(0f, attackApproachRange * attackApproachRange - dy * dy));
        return Mathf.Max(0f, facingDirection * (nearest.x - origin.x) - horizontalReach + AttackApproachInset);
    }

    private void TickAttackApproach()
    {
        if (attackApproachReceiver == null || !attackApproachReceiver.IsAlive ||
            !attackApproachReceiver.isActiveAndEnabled || attackApproachTarget == null ||
            !attackApproachTarget.enabled || !attackApproachTarget.gameObject.activeInHierarchy ||
            !isGrounded || IsKnockedBack || attackApproachTimer <= 0f)
        {
            CancelAttackApproach();
            return;
        }

        FaceAttackTarget(attackApproachTarget);
        float gap = GetAttackApproachGap();
        float remainingDistance = attackApproachDistance - Mathf.Abs(body.position.x - attackApproachStartX);
        float step = Mathf.Min(gap, remainingDistance, attackApproachSpeed * Time.fixedDeltaTime);
        if (step <= 0.001f)
        {
            CancelAttackApproach();
            return;
        }

        Vector2 direction = Vector2.right * facingDirection;
        for (int i = 0; i < solidBodyColliders.Length; i++)
        {
            Collider2D source = solidBodyColliders[i];
            if (source == null || !source.enabled || !source.gameObject.activeInHierarchy)
                continue;
            int count = source.Cast(direction, groundCheckFilter, attackApproachHits, step + AttackApproachSkin, true);
            if (count == attackApproachHits.Length)
                step = 0f;
            for (int hitIndex = 0; hitIndex < count; hitIndex++)
            {
                RaycastHit2D hit = attackApproachHits[hitIndex];
                if (hit.collider == null || Vector2.Dot(hit.normal, direction) >= -0.01f ||
                    hit.collider.GetComponentInParent<MonsterHealth>() != null)
                    continue;
                step = Mathf.Min(step, Mathf.Max(0f, hit.distance - AttackApproachSkin));
            }
        }
        if (step <= 0.001f)
        {
            CancelAttackApproach();
            return;
        }
        attackApproachTimer -= Time.fixedDeltaTime;
        body.linearVelocity = new Vector2(facingDirection * step / Time.fixedDeltaTime, body.linearVelocity.y);
    }

    private void StartDash()
    {
        GameAudioManager.PlayCueForDuration(GameAudioCue.PlayerDash, dashDuration);
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        IgnoreMonsterBodyCollisions();

        if (!isGrounded)
            airDashesUsed++;

        float direction = Mathf.Abs(moveInput) > 0.01f ? Mathf.Sign(moveInput) : FacingDirection;
        constraintsBeforeDash = body.constraints;
        body.constraints = constraintsBeforeDash | RigidbodyConstraints2D.FreezePositionY;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(direction * dashSpeed, 0f);
    }

    private void EndDash()
    {
        GameAudioManager.StopCue(GameAudioCue.PlayerDash);
        isDashing = false;
        RestoreMonsterBodyCollisions();
        body.constraints = constraintsBeforeDash;
        body.gravityScale = defaultGravityScale;
        body.linearVelocity = new Vector2(body.linearVelocity.x * 0.5f, 0f);
    }

    private void IgnoreMonsterBodyCollisions()
    {
        // Only solid body pairs are ignored; terrain and overlap-based combat queries stay active.
        RestoreMonsterBodyCollisions();
        if (solidBodyColliders == null || solidBodyColliders.Length == 0)
            return;

        IReadOnlyList<MonsterHealth> monsters = MonsterHealth.CollisionInstances;
        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterHealth monster = monsters[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
                continue;

            // Refresh live children without allocating arrays; spawned/reparented colliders stay correct.
            monster.GetComponentsInChildren(true, monsterColliderResults);
            for (int j = 0; j < monsterColliderResults.Count; j++)
            {
                Collider2D monsterCollider = monsterColliderResults[j];
                if (monsterCollider == null || !monsterCollider.enabled || monsterCollider.isTrigger || !monsterCollider.gameObject.activeInHierarchy)
                    continue;

                for (int k = 0; k < solidBodyColliders.Length; k++)
                {
                    Collider2D playerCollider = solidBodyColliders[k];
                    if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy || Physics2D.GetIgnoreCollision(playerCollider, monsterCollider))
                        continue;

                    Physics2D.IgnoreCollision(playerCollider, monsterCollider, true);
                    ignoredMonsterCollisionPairs.Add(new ColliderIgnorePair(playerCollider, monsterCollider));
                }
            }
        }
    }

    private void RestoreMonsterBodyCollisions(bool immediately = false)
    {
        for (int i = ignoredMonsterCollisionPairs.Count - 1; i >= 0; i--)
        {
            ColliderIgnorePair pair = ignoredMonsterCollisionPairs[i];
            if (pair.PlayerCollider == null || pair.MonsterCollider == null ||
                !pair.PlayerCollider.enabled || !pair.MonsterCollider.enabled ||
                !pair.PlayerCollider.gameObject.activeInHierarchy || !pair.MonsterCollider.gameObject.activeInHierarchy)
            {
                ignoredMonsterCollisionPairs.RemoveAt(i);
                continue;
            }

            if (!immediately && pair.PlayerCollider.Distance(pair.MonsterCollider).isOverlapped)
                continue;

            Physics2D.IgnoreCollision(pair.PlayerCollider, pair.MonsterCollider, false);
            ignoredMonsterCollisionPairs.RemoveAt(i);
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float acceleration = Mathf.Abs(targetSpeed) > 0.01f
            ? (isGrounded ? groundAcceleration : airAcceleration)
            : (isGrounded ? groundDeceleration : airDeceleration);

        float horizontalSpeed = Mathf.MoveTowards(
            body.linearVelocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(horizontalSpeed, body.linearVelocity.y);
    }

    private void TickFootsteps()
    {
        if (!isGrounded || isDashing || IsAttackApproaching || IsKnockedBack || Mathf.Abs(body.linearVelocity.x) < 0.15f)
        {
            nextFootstepTime = 0f;
            return;
        }

        if (Time.time < nextFootstepTime)
            return;

        GameAudioManager.PlayCue(GameAudioCue.PlayerFootstep);
        nextFootstepTime = Time.time + 0.38f;
    }

    private void ApplyBetterGravity()
    {
        float velocityY = body.linearVelocity.y;
        if (velocityY < 0f)
            body.gravityScale = defaultGravityScale * fallGravityMultiplier;
        else if (Mathf.Abs(velocityY) < apexThreshold)
            body.gravityScale = defaultGravityScale * apexGravityMultiplier;
        else if (velocityY > 0f && !jumpHeld)
            body.gravityScale = defaultGravityScale * lowJumpGravityMultiplier;
        else
            body.gravityScale = defaultGravityScale;

        if (body.linearVelocity.y < -maxFallSpeed)
            body.linearVelocity = new Vector2(body.linearVelocity.x, -maxFallSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
