using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class JumpAttackHitbox : MonoBehaviour
{
    private BoxCollider2D hitboxCollider;
    private CombatHitbox2D combatHitbox;

    private void Awake()
    {
        CacheReferences();
        SetEnabled(false);

        if (combatHitbox == null)
            Debug.LogError($"{nameof(JumpAttackHitbox)} requires a parent {nameof(CombatHitbox2D)}.", this);
    }

    public void SetEnabled(bool isEnabled)
    {
        CacheReferences();
        if (hitboxCollider != null)
            hitboxCollider.enabled = isEnabled;
    }

    public void ApplyDamagePulse(int damage)
    {
        CacheReferences();
        if (damage > 0 && hitboxCollider != null && combatHitbox != null)
            combatHitbox.ApplyBoxPulse(hitboxCollider, damage, false);
    }

    private void CacheReferences()
    {
        hitboxCollider ??= GetComponent<BoxCollider2D>();
        combatHitbox ??= GetComponentInParent<CombatHitbox2D>();
    }
}
