using System;
using System.Collections.Generic;
using UnityEngine;

// Monster health is data and damage rules only; AI owns reactions and death presentation.
public sealed class MonsterHealth : MonoBehaviour
{
    private static readonly List<MonsterHealth> collisionInstances = new List<MonsterHealth>();
    internal static IReadOnlyList<MonsterHealth> CollisionInstances => collisionInstances;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCollisionInstances() => collisionInstances.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SeedCollisionInstances()
    {
        // Awake may be skipped when entering Play Mode with Scene Reload disabled.
        foreach (MonsterHealth monster in UnityEngine.Object.FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None))
            if (!collisionInstances.Contains(monster))
                collisionInstances.Add(monster);
    }

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 30;
    [SerializeField, Min(0f)] private float invincibilityTime = 0.3f;

    private int currentHealth;
    private float invincibleUntil;
    private bool isDead;
    private bool damageImmune;
    private float damageTakenMultiplier = 1f;
    private bool isBoss;
    private bool isSlime;
    private bool isDemonSlime;
    private MonsterVisibilityCulling visibility;

    public bool IsDead => isDead;
    public bool IsInvincible => Time.time < invincibleUntil;
    public bool IsDamageImmune => damageImmune;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsSimulationSuspended => visibility != null && visibility.IsSleeping;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDied;

    private void Awake()
    {
        if (!collisionInstances.Contains(this))
            collisionInstances.Add(this);
        currentHealth = maxHealth;
        isBoss = GetComponent<BossEncounter>() != null || GetComponent<GolemBossController>() != null;
        isDemonSlime = GetComponent<BossEncounter>() != null && GetComponent<GolemBossController>() == null && GetComponent<BringerOfDeathController>() == null;
        MonsterChapterStats stats = GetComponent<MonsterChapterStats>();
        isSlime = stats != null && (stats.Archetype == MonsterArchetype.GreenSlime ||
            stats.Archetype == MonsterArchetype.BlueSlime || stats.Archetype == MonsterArchetype.RedSlime);
        visibility = GetComponent<MonsterVisibilityCulling>();
        if (visibility == null)
            visibility = gameObject.AddComponent<MonsterVisibilityCulling>();
    }

    private void OnDestroy() => collisionInstances.Remove(this);

    // Chapter stats call this before AI/health Awake, once when an enemy is spawned.
    public void ConfigureMaximumHealth(int amount)
    {
        maxHealth = Mathf.Max(1, amount);
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        ApplyDamage(damage, true);
    }

    // Skill pulses intentionally bypass the normal hit invulnerability window.
    public void TakeSkillDamage(int damage)
    {
        ApplyDamage(damage, false);
    }

    public void SetDamageImmune(bool isImmune)
    {
        damageImmune = isImmune && !isDead;
    }

    public void SetDamageTakenMultiplier(float multiplier)
    {
        damageTakenMultiplier = Mathf.Clamp01(multiplier);
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (currentHealth != previousHealth)
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void ApplyDamage(int damage, bool respectsInvincibility)
    {
        using (GameplayProfilerMarkers.Damage.Auto())
        {
            if (isDead || damageImmune || damage <= 0 || (respectsInvincibility && IsInvincible))
                return;

            visibility?.WakeForActivity();
            int adjustedDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageTakenMultiplier));
            currentHealth = Mathf.Max(0, currentHealth - adjustedDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            if (respectsInvincibility)
                invincibleUntil = Time.time + invincibilityTime;

            GameAudioManager.PlayCueAt(isDemonSlime ? GameAudioCue.DemonSlimeTakeHit : isBoss ? GameAudioCue.BossTakeHit : isSlime ? GameAudioCue.SlimeTakeHit : GameAudioCue.MonsterTakeHit, transform.position);
            OnDamaged?.Invoke();
        }
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0;
        GameAudioManager.PlayCueAt(isDemonSlime ? GameAudioCue.DemonSlimeDeath : isBoss ? GameAudioCue.BossDeath : GameAudioCue.MonsterDeath, transform.position);
        OnDied?.Invoke();
    }
}
