using UnityEngine;

public enum PlayerSkill
{
    Skill1,
    Skill2,
    Skill3
}

// Gắn lên Player để điều chỉnh từng đợt sát thương của skill tại cùng một nơi.
[DisallowMultipleComponent]
public class SkillDamageController : MonoBehaviour
{
    [Header("Skill 1 Damage Per Hit")]
    [SerializeField, Min(1)] private int[] skill1DamagePerHit = { 8, 8, 8, 8, 8 };

    [Header("Skill 2 Damage Per Hit")]
    [SerializeField, Min(1)] private int[] skill2DamagePerHit = { 6, 6, 6, 6, 6 };

    [Header("Skill 3 Ranged Damage (once per target per cast)")]
    [SerializeField, Min(1)] private int[] skill3DamagePerHit = { 24 };

    [Header("Pulse Time In Animation (normalized)")]
    [SerializeField, Range(0f, 1f)] private float[] skill1PulseTimesNormalized = { 0.28f, 0.39f, 0.5f, 0.61f, 0.72f };
    // Timings align the four skill-2 pulses with distinct sprite changes in Skill_2.anim.
    [SerializeField, Range(0f, 1f)] private float[] skill2PulseTimesNormalized = { 0.33f, 0.443f, 0.547f, 0.66f };

    public int GetDamagePulseCount(PlayerSkill skill)
    {
        return GetDamageList(skill).Length;
    }

    public int GetDamage(PlayerSkill skill, int pulseIndex)
    {
        int[] damagePerHit = GetDamageList(skill);
        if (damagePerHit.Length == 0)
            return 0;

        return damagePerHit[Mathf.Clamp(pulseIndex, 0, damagePerHit.Length - 1)];
    }

    public float GetPulseTime(PlayerSkill skill, int pulseIndex, float startNormalizedTime, float endNormalizedTime)
    {
        if (skill == PlayerSkill.Skill3)
            return startNormalizedTime;
        float[] pulseTimes = skill == PlayerSkill.Skill1 ? skill1PulseTimesNormalized : skill2PulseTimesNormalized;
        if ((uint)pulseIndex < (uint)pulseTimes.Length)
            return pulseTimes[pulseIndex];

        int pulseCount = GetDamagePulseCount(skill);
        if (pulseCount <= 1)
            return startNormalizedTime;

        return Mathf.Lerp(startNormalizedTime, endNormalizedTime, (float)pulseIndex / (pulseCount - 1));
    }

    private int[] GetDamageList(PlayerSkill skill)
    {
        return skill switch
        {
            PlayerSkill.Skill1 => skill1DamagePerHit,
            PlayerSkill.Skill2 => skill2DamagePerHit,
            PlayerSkill.Skill3 => skill3DamagePerHit,
            _ => System.Array.Empty<int>()
        };
    }
}
