using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatHitbox2D : MonoBehaviour
{
    private const int DefaultBufferCapacity = 16;
    private const int MaximumBufferCapacity = 256;

    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField, Min(1)] private int initialBufferCapacity = DefaultBufferCapacity;
    [SerializeField, Min(1)] private int maximumBufferCapacity = MaximumBufferCapacity;

    private ContactFilter2D contactFilter;
    private Collider2D[] overlapBuffer;
    private Hurtbox2D[] pulseReceivers;
    private Hurtbox2D sourceHurtbox;
    private bool playerOwned;
    private bool warnedAboutBufferGrowth;
    private bool warnedAboutBufferLimit;
    private readonly RaycastHit2D[] obstructionBuffer = new RaycastHit2D[16];

    private void Awake()
    {
        sourceHurtbox = GetComponentInParent<Hurtbox2D>();
        playerOwned = GetComponentInParent<PlayerCombat>() != null;
        initialBufferCapacity = Mathf.Max(1, initialBufferCapacity);
        maximumBufferCapacity = Mathf.Max(initialBufferCapacity, maximumBufferCapacity);
        overlapBuffer = new Collider2D[initialBufferCapacity];
        pulseReceivers = new Hurtbox2D[initialBufferCapacity];

        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true
        };
        contactFilter.SetLayerMask(targetLayers);
    }

    public int ApplyCirclePulse(Vector2 center, float radius, int damage, bool isSkillPulse, int maximumTargets = int.MaxValue)
        => ApplyCirclePulse(center, radius, damage, isSkillPulse, maximumTargets, null);

    public int ApplyCirclePulse(Vector2 center, float radius, int damage, bool isSkillPulse, int maximumTargets, Hurtbox2D requiredTarget)
    {
        using (GameplayProfilerMarkers.CombatQuery.Auto())
        {
            int count = QueryCircle(center, radius);
            return ApplyDamageToResults(count, damage, isSkillPulse, maximumTargets, requiredTarget);
        }
    }

    public int ApplyBoxPulse(BoxCollider2D box, int damage, bool isSkillPulse)
        => ApplyBoxPulse(box, damage, isSkillPulse, int.MaxValue, null);

    public int ApplyBoxPulse(BoxCollider2D box, int damage, bool isSkillPulse, int maximumTargets, Hurtbox2D requiredTarget = null)
    {
        if (box == null)
            return 0;

        using (GameplayProfilerMarkers.CombatQuery.Auto())
        {
            Vector3 scale = box.transform.lossyScale;
            Vector2 size = Vector2.Scale(box.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 center = box.transform.TransformPoint(box.offset);
            int count = QueryBox(center, size, box.transform.eulerAngles.z);
            // Skills/jump attacks keep their area pulse; basic attacks request one locked receiver.
            return ApplyDamageToResults(count, damage, isSkillPulse, maximumTargets, requiredTarget);
        }
    }

    public static Bounds GetBoxWorldBounds(BoxCollider2D box)
    {
        Vector3 center = box.transform.TransformPoint(box.offset);
        Vector3 right = box.transform.TransformVector(Vector3.right * box.size.x * 0.5f);
        Vector3 up = box.transform.TransformVector(Vector3.up * box.size.y * 0.5f);
        return new Bounds(center, new Vector3(2f * (Mathf.Abs(right.x) + Mathf.Abs(up.x)),
            2f * (Mathf.Abs(right.y) + Mathf.Abs(up.y)), 0.02f));
    }

    public int ApplyBoxSweep(BoxCollider2D box, int damage, HashSet<Hurtbox2D> castReceivers, LayerMask obstructionLayers)
    {
        if (box == null || castReceivers == null)
            return 0;
        using (GameplayProfilerMarkers.CombatQuery.Auto())
        {
            Vector3 scale = box.transform.lossyScale;
            Vector2 size = Vector2.Scale(box.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 center = box.transform.TransformPoint(box.offset);
            int count = QueryBox(center, size, box.transform.eulerAngles.z);
            return ApplyDamageToResults(count, damage, true, int.MaxValue, null, castReceivers, obstructionLayers);
        }
    }

    public Hurtbox2D FindNearestMonster(Vector2 center, float radius, Bounds meleeBounds,
        LayerMask obstructionLayers, out Collider2D targetCollider)
    {
        targetCollider = null;
        Hurtbox2D nearest = null;
        float nearestDistanceSquared = radius * radius;
        int receiverCount = 0;
        int count = QueryCircle(center, radius);
        for (int i = 0; i < count; i++)
        {
            Collider2D overlap = overlapBuffer[i];
            Hurtbox2D receiver = overlap != null ? overlap.GetComponentInParent<Hurtbox2D>() : null;
            if (receiver == null || receiver == sourceHurtbox || !receiver.IsMonster ||
                !receiver.isActiveAndEnabled || !receiver.IsAlive || WasAlreadyHit(receiver, receiverCount))
                continue;

            pulseReceivers[receiverCount++] = receiver;
            Collider2D candidate = receiver.BodyCollider;
            if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy ||
                candidate.bounds.max.y < meleeBounds.min.y || candidate.bounds.min.y > meleeBounds.max.y)
                continue;

            float distanceSquared = (candidate.ClosestPoint(center) - center).sqrMagnitude;
            if (distanceSquared > nearestDistanceSquared ||
                (nearest != null && Mathf.Approximately(distanceSquared, nearestDistanceSquared) &&
                 receiver.GetInstanceID() >= nearest.GetInstanceID()) ||
                !HasClearPath(center, candidate, obstructionLayers))
                continue;

            nearest = receiver;
            targetCollider = candidate;
            nearestDistanceSquared = distanceSquared;
        }
        return nearest;
    }

    public bool HasClearPath(Vector2 origin, Collider2D target, LayerMask obstructionLayers)
    {
        if (target == null)
            return false;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstructionLayers);
        Vector2 end = target.ClosestPoint(origin);
        int count = Physics2D.Linecast(origin, end, filter, obstructionBuffer);
        if (count == obstructionBuffer.Length)
            return false;
        for (int i = 0; i < count; i++)
        {
            Collider2D blocker = obstructionBuffer[i].collider;
            if (blocker == null || blocker == target ||
                blocker.GetComponentInParent<Hurtbox2D>() == sourceHurtbox ||
                blocker.GetComponentInParent<MonsterHealth>() != null)
                continue;
            return false;
        }
        return true;
    }

    private int QueryCircle(Vector2 center, float radius)
    {
        int count = Physics2D.OverlapCircle(center, Mathf.Max(0f, radius), contactFilter, overlapBuffer);
        while (count == overlapBuffer.Length && GrowBuffer())
            count = Physics2D.OverlapCircle(center, Mathf.Max(0f, radius), contactFilter, overlapBuffer);

        ReportBufferLimitIfNeeded(count);
        return count;
    }

    private int QueryBox(Vector2 center, Vector2 size, float angle)
    {
        int count = Physics2D.OverlapBox(center, size, angle, contactFilter, overlapBuffer);
        while (count == overlapBuffer.Length && GrowBuffer())
            count = Physics2D.OverlapBox(center, size, angle, contactFilter, overlapBuffer);

        ReportBufferLimitIfNeeded(count);
        return count;
    }

    private bool GrowBuffer()
    {
        if (overlapBuffer.Length >= maximumBufferCapacity)
            return false;

        int newCapacity = Mathf.Min(maximumBufferCapacity, overlapBuffer.Length * 2);
        Array.Resize(ref overlapBuffer, newCapacity);
        Array.Resize(ref pulseReceivers, newCapacity);

        if (!warnedAboutBufferGrowth)
        {
            warnedAboutBufferGrowth = true;
            Debug.LogWarning($"{name} combat overlap buffer grew to {newCapacity}. Increase its capacity if this is expected in gameplay.", this);
        }

        return true;
    }

    private void ReportBufferLimitIfNeeded(int count)
    {
        if (count != overlapBuffer.Length || overlapBuffer.Length < maximumBufferCapacity || warnedAboutBufferLimit)
            return;

        warnedAboutBufferLimit = true;
        Debug.LogError($"{name} combat overlap buffer reached its configured maximum of {maximumBufferCapacity}; some overlaps may not be processed.", this);
    }

    private int ApplyDamageToResults(int count, int damage, bool isSkillPulse, int maximumTargets, Hurtbox2D requiredTarget,
        HashSet<Hurtbox2D> castReceivers = null, LayerMask obstructionLayers = default)
    {
        if (damage <= 0 || maximumTargets <= 0)
            return 0;

        if (playerOwned && RunResources.Instance != null)
            damage = RunResources.Instance.ScalePlayerDamage(damage);
        int damagedTargets = 0;
        int receiverCount = 0;
        Transform damageSource = sourceHurtbox != null ? sourceHurtbox.transform : transform;
        for (int i = 0; i < count; i++)
        {
            Collider2D overlap = overlapBuffer[i];
            if (overlap == null)
                continue;

            Hurtbox2D hurtbox = overlap.GetComponentInParent<Hurtbox2D>();
            if (hurtbox == null || hurtbox == sourceHurtbox || !hurtbox.IsAlive ||
                (requiredTarget != null && hurtbox != requiredTarget) || WasAlreadyHit(hurtbox, receiverCount))
                continue;

            if (castReceivers != null && (!hurtbox.IsMonster || castReceivers.Contains(hurtbox) ||
                hurtbox.BodyCollider == null || overlap != hurtbox.BodyCollider ||
                !HasClearPath(damageSource.position, hurtbox.BodyCollider, obstructionLayers)))
                continue;
            // A monster's separate ground/weapon collider must not consume its body hit.
            pulseReceivers[receiverCount++] = hurtbox;
            if (!hurtbox.TryReceiveDamage(damage, isSkillPulse, damageSource))
                continue;

            castReceivers?.Add(hurtbox);
            damagedTargets++;
            if (damagedTargets >= maximumTargets)
                break;
        }

        return damagedTargets;
    }

    private bool WasAlreadyHit(Hurtbox2D candidate, int receiverCount)
    {
        for (int i = 0; i < receiverCount; i++)
        {
            if (pulseReceivers[i] == candidate)
                return true;
        }

        return false;
    }
}
