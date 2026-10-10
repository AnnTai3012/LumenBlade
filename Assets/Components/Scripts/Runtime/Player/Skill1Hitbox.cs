using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Skill1Hitbox : MonoBehaviour
{
    // Component này dùng được cho từng vùng đánh của Skill 1 hoặc Skill 2.
    [SerializeField] private PlayerSkill skill = PlayerSkill.Skill1;
    // Giá trị dự phòng cho các hitbox chưa có SkillDamageController ở GameObject cha.
    [SerializeField, Min(1)] private int damage = 20;

    private Collider2D hitboxCollider;
    private SkillDamageController damageController;
    private CombatHitbox2D combatHitbox;
    private PlayerController playerController;
    private bool dependenciesCached;

    public PlayerSkill Skill => skill;

    private void Awake()
    {
        CacheDependencies();
        SetEnabled(false);
    }

    private void CacheDependencies()
    {
        if (dependenciesCached)
            return;

        hitboxCollider = GetComponent<Collider2D>();
        damageController = GetComponentInParent<SkillDamageController>();
        combatHitbox = GetComponentInParent<CombatHitbox2D>();
        playerController = GetComponentInParent<PlayerController>();
        dependenciesCached = true;
    }

    public void SetEnabled(bool isEnabled)
    {
        CacheDependencies();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = isEnabled;
        }
    }

    public int DamagePulseCount
    {
        get
        {
            CacheDependencies();
            return damageController != null ? damageController.GetDamagePulseCount(skill) : 1;
        }
    }

    public int GetPulseDamage(int pulseIndex)
    {
        CacheDependencies();
        return damageController != null ? damageController.GetDamage(skill, pulseIndex) : damage;
    }

    public float GetPulseNormalizedTime(int pulseIndex, float startNormalizedTime, float endNormalizedTime)
    {
        CacheDependencies();
        return damageController != null
            ? damageController.GetPulseTime(skill, pulseIndex, startNormalizedTime, endNormalizedTime)
            : (DamagePulseCount <= 1
                ? startNormalizedTime
                : Mathf.Lerp(startNormalizedTime, endNormalizedTime, (float)pulseIndex / (DamagePulseCount - 1)));
    }

    // Compatibility bridge for legacy Animation Events; SkillExecution owns the one-shot guard.
    public void ApplyDamagePulse(int pulseIndex)
    {
        CacheDependencies();
        playerController?.ApplySkillPulseFromAnimation(skill, pulseIndex);
    }

    internal void ApplyPulseDamage(int finalDamage)
    {
        CacheDependencies();
        if (finalDamage > 0 && combatHitbox != null && hitboxCollider is BoxCollider2D boxCollider)
            combatHitbox.ApplyBoxPulse(boxCollider, finalDamage, true);
    }
}
