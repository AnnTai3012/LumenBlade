using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(BoxCollider2D))]
public sealed class BringerSpell2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float duration = 1.6f;
    [SerializeField, Range(0f, 1f)] private float damageStartsAt = 0.3125f;
    [SerializeField, Range(0f, 1f)] private float damageEndsAt = 0.8125f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private readonly HashSet<Health> damagedPlayers = new HashSet<Health>();
    private readonly Collider2D[] overlaps = new Collider2D[16];
    private ContactFilter2D filter;
    private BoxCollider2D hitbox;
    private MonsterHealth owner;
    private int damage;
    private float elapsed;
    private bool initialized;

    public bool IsDamageActive => hitbox != null && hitbox.enabled;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(targetLayers);
    }

    public void Initialize(MonsterHealth caster, int amount)
    {
        owner = caster;
        damage = Mathf.Max(1, amount);
        elapsed = 0f;
        initialized = true;
        damagedPlayers.Clear();
    }

    private void Update()
    {
        if (!initialized) return;
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled)
        {
            Cancel();
            return;
        }
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;
        hitbox.enabled = progress >= damageStartsAt && progress < damageEndsAt;
        if (elapsed >= duration) Cancel();
    }

    private void FixedUpdate()
    {
        if (!initialized || !IsDamageActive || owner == null || owner.IsDead) return;
        Vector3 scale = hitbox.transform.lossyScale;
        Vector2 size = Vector2.Scale(hitbox.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        int count = Physics2D.OverlapBox(hitbox.transform.TransformPoint(hitbox.offset), size,
            hitbox.transform.eulerAngles.z, filter, overlaps);
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i] == null || overlaps[i].isTrigger) continue;
            Health target = overlaps[i] != null ? overlaps[i].GetComponentInParent<Health>() : null;
            if (target == null || target.IsDead || target.IsInvulnerable || !target.isActiveAndEnabled || !damagedPlayers.Add(target)) continue;
            target.TakeDamage(damage);
        }
    }

    public void Cancel()
    {
        initialized = false;
        if (hitbox != null) hitbox.enabled = false;
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (hitbox != null) hitbox.enabled = false;
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.1f, duration);
        damageEndsAt = Mathf.Max(damageStartsAt, damageEndsAt);
    }
}
