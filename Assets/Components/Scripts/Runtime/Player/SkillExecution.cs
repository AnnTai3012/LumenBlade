using System;
using UnityEngine;

public readonly struct SkillPulse
{
    public float NormalizedTime { get; }
    public int Damage { get; }

    public SkillPulse(float normalizedTime, int damage)
    {
        NormalizedTime = normalizedTime;
        Damage = damage;
    }
}

public sealed class SkillExecution
{
    private readonly SkillPulse[] pulses;
    private readonly bool[] applied;
    private int nextPulseIndex;

    public bool IsActive { get; private set; }

    public SkillExecution(Skill1Hitbox hitbox, float startNormalizedTime, float endNormalizedTime)
    {
        int pulseCount = hitbox != null ? hitbox.DamagePulseCount : 0;
        pulses = new SkillPulse[pulseCount];
        applied = new bool[pulseCount];

        float previousTime = startNormalizedTime;
        for (int i = 0; i < pulseCount; i++)
        {
            float pulseTime = Mathf.Clamp(hitbox.GetPulseNormalizedTime(i, startNormalizedTime, endNormalizedTime), startNormalizedTime, endNormalizedTime);
            pulseTime = Mathf.Max(previousTime, pulseTime);
            pulses[i] = new SkillPulse(pulseTime, hitbox.GetPulseDamage(i));
            previousTime = pulseTime;
        }
    }

    public void Begin()
    {
        Array.Clear(applied, 0, applied.Length);
        nextPulseIndex = 0;
        IsActive = true;
    }

    public void Cancel()
    {
        IsActive = false;
        nextPulseIndex = 0;
    }

    public void Tick(float normalizedTime, Skill1Hitbox hitbox)
    {
        using (GameplayProfilerMarkers.SkillPulse.Auto())
        {
            if (!IsActive || hitbox == null)
                return;

            // A delayed frame catches up every due pulse in order; the applied flags prevent replay.
            while (nextPulseIndex < pulses.Length)
            {
                if (applied[nextPulseIndex])
                {
                    nextPulseIndex++;
                    continue;
                }

                if (normalizedTime < pulses[nextPulseIndex].NormalizedTime)
                    break;

                ApplyPulse(nextPulseIndex, hitbox);
                nextPulseIndex++;
            }
        }
    }

    public bool TryApplyAnimationPulse(int pulseIndex, Skill1Hitbox hitbox)
    {
        if (!IsActive || hitbox == null || (uint)pulseIndex >= (uint)pulses.Length || applied[pulseIndex])
            return false;

        ApplyPulse(pulseIndex, hitbox);
        return true;
    }

    private void ApplyPulse(int pulseIndex, Skill1Hitbox hitbox)
    {
        applied[pulseIndex] = true;
        hitbox.ApplyPulseDamage(pulses[pulseIndex].Damage);
    }
}
