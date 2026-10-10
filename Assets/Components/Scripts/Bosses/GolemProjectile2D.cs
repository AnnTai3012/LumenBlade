using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class GolemProjectile2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 20;
    [SerializeField, Min(0.1f)] private float speed = 7f;
    [SerializeField, Min(0.1f)] private float lifetime = 8f;

    private Rigidbody2D body;
    private Collider2D hitbox;
    private Transform ownerRoot;
    private bool hasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction, Transform owner)
    {
        ownerRoot = owner;
        hasHit = false;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        direction.Normalize();
        transform.right = direction;
        hitbox.enabled = true;
        body.linearVelocity = direction * speed;
    }

    public void PrepareForLaunch(Transform owner)
    {
        ownerRoot = owner;
        body.linearVelocity = Vector2.zero;
        hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null || IsOwnerCollider(other))
            return;

        Health targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
            hasHit = true;
            targetHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        return ownerRoot != null &&
               (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot));
    }
}
