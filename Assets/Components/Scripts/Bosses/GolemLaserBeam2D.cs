using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class GolemLaserBeam2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float width = 0.75f;
    [SerializeField, Min(0.1f)] private float activeDuration = 0.65f;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private SpriteRenderer visualRenderer;

    private Rigidbody2D body;
    private Transform ownerRoot;
    private Collider2D aimTarget;
    private PlayerMotor2D aimTargetMotor;
    private int activeDamage;
    private float maximumLength;
    private float targetPadding;
    private float chargeTimer;
    private float activeTimer;
    private bool isFiring;
    private bool isActive;
    private bool aimLockedByDash;
    private bool hasDamagedTarget;

    private void LateUpdate()
    {
        // The animation swaps between sprites with different pixel bounds.
        // Refit after the Animator updates so every frame still matches the hitbox.
        FitVisualToHitbox();
    }

    private void Update()
    {
        if (!isFiring)
            return;

        if (!isActive)
        {
            UpdateAimAtTarget();
            chargeTimer -= Time.deltaTime;
            if (chargeTimer > 0f)
                return;

            isActive = true;
            hitbox.enabled = true;
        }

        activeTimer -= Time.deltaTime;
        if (activeTimer <= 0f)
            Destroy(gameObject);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (hitbox == null)
            hitbox = GetComponent<BoxCollider2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        hitbox.isTrigger = true;
        hitbox.enabled = false;
    }

    public void Fire(
        Vector2 direction,
        Transform owner,
        float requestedLength,
        int damage,
        float chargeDuration = 0f,
        Collider2D target = null,
        float maxLength = 20f,
        float extraReach = 0.25f)
    {
        ownerRoot = owner;
        aimTarget = target;
        aimTargetMotor = target != null ? target.GetComponentInParent<PlayerMotor2D>() : null;
        aimLockedByDash = false;
        activeDamage = Mathf.Max(1, damage);
        maximumLength = Mathf.Max(0.1f, maxLength);
        targetPadding = Mathf.Max(0f, extraReach);
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        body.linearVelocity = Vector2.zero;
        SetBeamGeometry(direction, requestedLength);

        chargeTimer = Mathf.Max(0f, chargeDuration);
        activeTimer = Mathf.Max(0.01f, activeDuration);
        isFiring = true;
        isActive = chargeTimer <= 0f;
        hitbox.enabled = isActive;
    }

    private void UpdateAimAtTarget()
    {
        if (aimLockedByDash)
            return;

        if (aimTargetMotor != null && aimTargetMotor.IsDashing)
        {
            // Keep the last pre-dash aim point; reacquiring during the same cast
            // would make a successful dash unable to dodge the laser.
            aimLockedByDash = true;
            return;
        }

        if (aimTarget == null || !aimTarget.enabled || !aimTarget.gameObject.activeInHierarchy)
            return;

        Vector2 targetPoint = aimTarget.ClosestPoint(transform.position);
        Vector2 aimOffset = targetPoint - (Vector2)transform.position;
        if (aimOffset.sqrMagnitude <= 0.0001f)
            return;

        SetBeamGeometry(aimOffset, aimOffset.magnitude + targetPadding);
    }

    private void SetBeamGeometry(Vector2 direction, float requestedLength)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        direction.Normalize();
        float beamLength = Mathf.Clamp(requestedLength, 0.1f, maximumLength);
        transform.right = direction;
        hitbox.size = new Vector2(beamLength, width);
        hitbox.offset = new Vector2(beamLength * 0.5f, 0f);
        FitVisualToHitbox();
    }

    private void FitVisualToHitbox()
    {
        if (hitbox == null || visualTransform == null || visualRenderer == null || visualRenderer.sprite == null)
            return;

        Vector3 spriteSize = visualRenderer.sprite.bounds.size;
        Vector3 spriteBoundsMin = visualRenderer.sprite.bounds.min;
        Vector2 hitboxSize = hitbox.size;
        Vector2 hitboxOffset = hitbox.offset;
        float scaleX = hitboxSize.x / Mathf.Max(0.01f, spriteSize.x);
        float scaleY = hitboxSize.y / Mathf.Max(0.01f, spriteSize.y);

        // Sprite frames use a centered pivot. Position the scaled sprite bounds,
        // rather than the pivot, on the collider bounds so art and damage area align.
        visualTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        visualTransform.localPosition = new Vector3(
            hitboxOffset.x - hitboxSize.x * 0.5f - spriteBoundsMin.x * scaleX,
            hitboxOffset.y - hitboxSize.y * 0.5f - spriteBoundsMin.y * scaleY,
            0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDamagedTarget || other == null || IsOwnerCollider(other))
            return;

        Health targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth == null || targetHealth.IsDead)
            return;

        hasDamagedTarget = true;
        targetHealth.TakeDamage(activeDamage);
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        return ownerRoot != null &&
               (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot));
    }
}
