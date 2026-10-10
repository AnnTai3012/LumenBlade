using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MonsterMotor2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private bool flying;
    [SerializeField] private bool spriteFacesLeftWhenUnflipped;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 3f;

    private Rigidbody2D body;
    private MonsterHealth health;
    private Vector3 originalScale;
    private Vector2 desiredVelocity;
    private int facingDirection;
    private float lungeTimer;
    private float lungeHorizontalSpeed;

    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public int FacingDirection => facingDirection;
    public bool IsFlying => flying;
    public float DesiredHorizontalSpeed => desiredVelocity.x;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<MonsterHealth>();
        originalScale = transform.localScale;
        int localScaleDirection = originalScale.x >= 0f ? 1 : -1;
        facingDirection = spriteFacesLeftWhenUnflipped ? -localScaleDirection : localScaleDirection;
        if (flying)
            body.gravityScale = 0f;
    }

    public void SetDesiredHorizontalSpeed(float speed)
    {
        desiredVelocity = new Vector2(speed, 0f);
    }

    public void SetDesiredVelocity(Vector2 velocity)
    {
        desiredVelocity = velocity;
    }

    public void FaceDirection(int direction)
    {
        if (direction == 0)
            return;

        int requestedDirection = direction > 0 ? 1 : -1;
        if (requestedDirection == facingDirection)
            return;

        facingDirection = requestedDirection;
        int localScaleDirection = spriteFacesLeftWhenUnflipped ? -requestedDirection : requestedDirection;
        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x) * localScaleDirection,
            originalScale.y,
            originalScale.z);
    }

    public void ApplyKnockback(Vector2 direction)
    {
        if (body != null && body.simulated)
            body.linearVelocity = new Vector2(direction.x * knockbackForce, body.linearVelocity.y);
    }

    public void StopAndDisablePhysics()
    {
        desiredVelocity = Vector2.zero;
        lungeTimer = 0f;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
    }

    public void BeginLunge(int direction, float distance, float duration)
    {
        if (body == null || !body.simulated || direction == 0 || distance <= 0f || duration <= 0f)
            return;

        int normalizedDirection = direction > 0 ? 1 : -1;
        FaceDirection(normalizedDirection);
        lungeHorizontalSpeed = normalizedDirection * (distance / duration);
        lungeTimer = duration;
    }

    public void BeginDeathFall(float gravityScale)
    {
        if (body == null)
            return;

        desiredVelocity = Vector2.zero;
        flying = false;
        body.gravityScale = Mathf.Max(0f, gravityScale);
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        body.WakeUp();
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsSimulationSuspended)
            return;

        if (body == null || !body.simulated)
            return;

        if (lungeTimer > 0f)
        {
            body.linearVelocity = new Vector2(lungeHorizontalSpeed, body.linearVelocity.y);
            lungeTimer = Mathf.Max(0f, lungeTimer - Time.fixedDeltaTime);
            return;
        }

        if (flying)
            body.linearVelocity = desiredVelocity;
        else
            body.linearVelocity = new Vector2(desiredVelocity.x, body.linearVelocity.y);
    }
}
