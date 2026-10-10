using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Skill3HitboxFrame
{
    [Range(0f, 1f)] public float normalizedTime;
    public Vector2 offset;
    public Vector2 size;
}

// The authored wave travels inside the sprite clip; its damage box follows the same frames.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class Skill3Hitbox : MonoBehaviour
{
    [SerializeField] private Skill3HitboxFrame[] frames = Array.Empty<Skill3HitboxFrame>();

    private BoxCollider2D hitbox;
    private CombatHitbox2D combatHitbox;
    private SkillDamageController damageController;
    private PlayerMotor2D motor;
    private readonly HashSet<Hurtbox2D> castReceivers = new HashSet<Hurtbox2D>();
    private int nextFrameIndex;
    private bool isCasting;
    private bool hasPreviousShape;
    private Rect previousShape;

    public bool IsConfigured => hitbox != null && frames.Length > 0;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        combatHitbox = GetComponentInParent<CombatHitbox2D>();
        damageController = GetComponentInParent<SkillDamageController>();
        motor = GetComponentInParent<PlayerMotor2D>();
        hitbox.isTrigger = true;
        Cancel();
    }

    public void Begin()
    {
        castReceivers.Clear();
        nextFrameIndex = 0;
        hasPreviousShape = false;
        isCasting = true;
        hitbox.enabled = false;
    }

    public void Tick(float normalizedTime)
    {
        if (!isCasting || !IsConfigured || combatHitbox == null || damageController == null)
            return;

        // Catch up crossed sprite frames even after a slow frame, then sweep the current segment.
        while (nextFrameIndex < frames.Length && frames[nextFrameIndex].normalizedTime <= normalizedTime)
            ApplyShape(frames[nextFrameIndex++]);

        if (nextFrameIndex == 0)
            return;

        Skill3HitboxFrame current = frames[nextFrameIndex - 1];
        if (nextFrameIndex < frames.Length)
        {
            Skill3HitboxFrame next = frames[nextFrameIndex];
            float blend = Mathf.InverseLerp(current.normalizedTime, next.normalizedTime, normalizedTime);
            current.offset = Vector2.Lerp(current.offset, next.offset, blend);
            current.size = Vector2.Lerp(current.size, next.size, blend);
        }
        ApplyShape(current);
    }

    private void ApplyShape(Skill3HitboxFrame frame)
    {
        Rect shape = new Rect(frame.offset - frame.size * 0.5f, frame.size);
        Rect sweep = hasPreviousShape ? Rect.MinMaxRect(
            Mathf.Min(previousShape.xMin, shape.xMin), Mathf.Min(previousShape.yMin, shape.yMin),
            Mathf.Max(previousShape.xMax, shape.xMax), Mathf.Max(previousShape.yMax, shape.yMax)) : shape;
        hitbox.enabled = true;
        hitbox.offset = sweep.center;
        hitbox.size = sweep.size;
        combatHitbox.ApplyBoxSweep(hitbox, damageController.GetDamage(PlayerSkill.Skill3, 0), castReceivers,
            motor != null ? motor.ObstructionLayers : 0);
        hitbox.offset = frame.offset;
        hitbox.size = frame.size;
        previousShape = shape;
        hasPreviousShape = true;
    }

    public void Cancel()
    {
        isCasting = false;
        hasPreviousShape = false;
        castReceivers.Clear();
        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void OnDisable() => Cancel();

    private void OnValidate()
    {
        if (frames == null) frames = Array.Empty<Skill3HitboxFrame>();
        float previousTime = 0f;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i].normalizedTime = Mathf.Clamp(frames[i].normalizedTime, previousTime, 1f);
            frames[i].size = new Vector2(Mathf.Max(0.01f, frames[i].size.x), Mathf.Max(0.01f, frames[i].size.y));
            previousTime = frames[i].normalizedTime;
        }
    }
}
