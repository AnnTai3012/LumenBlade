using UnityEngine;

[DisallowMultipleComponent]
public sealed class Hurtbox2D : MonoBehaviour
{
    private Health playerHealth;
    private MonsterHealth monsterHealth;
    private GolemBossController golemBoss;
    private Collider2D bodyCollider;

    public Collider2D BodyCollider => bodyCollider;
    public bool IsMonster => monsterHealth != null;

    public bool IsAlive => playerHealth != null
        ? !playerHealth.IsDead
        : monsterHealth != null && !monsterHealth.IsDead;

    private void Awake()
    {
        playerHealth = GetComponentInParent<Health>();
        monsterHealth = GetComponentInParent<MonsterHealth>();
        golemBoss = GetComponentInParent<GolemBossController>();
        bodyCollider = GetComponent<Collider2D>();

        if (playerHealth != null && monsterHealth != null)
            Debug.LogError($"{name} has both Health and MonsterHealth; a Hurtbox2D must have one damage receiver.", this);
        else if (playerHealth == null && monsterHealth == null)
            Debug.LogError($"{name} has no Health or MonsterHealth receiver.", this);
    }

    public bool TryReceiveDamage(int damage, bool isSkillPulse)
    {
        return TryReceiveDamage(damage, isSkillPulse, null);
    }

    public bool TryReceiveDamage(int damage, bool isSkillPulse, Transform damageSource)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || damage <= 0 || !IsAlive)
            return false;

        if (monsterHealth != null)
        {
            if (golemBoss != null && golemBoss.TryDeflectShieldHit(damageSource))
                return true;

            if (isSkillPulse)
                monsterHealth.TakeSkillDamage(damage);
            else
                monsterHealth.TakeDamage(damage);

            return true;
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            return true;
        }

        return false;
    }
}
