using System;
using UnityEngine;

// Authoritative player health, healing, invulnerability, and change events.
public sealed class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float invulnerabilityTime = 0.2f;

    private PlayerMotor2D motor;
    private bool skill2Invulnerable;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public int ShieldHealth { get; private set; }
    public int MaxShieldHealth { get; private set; }
    public bool HasShield => ShieldHealth > 0 && Time.time < shieldExpiresAt;
    public float ShieldSecondsRemaining => HasShield ? Mathf.Max(0f, shieldExpiresAt - Time.time) : 0f;
    public bool IsInvulnerable =>
        skill2Invulnerable ||
        Time.time < invulnerableUntil ||
        (motor != null && motor.IsDashing);

    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDied;
    public event Action<int> OnHealed;
    public event Action OnRevived;
    public event Action<int, int> OnShieldChanged;
    public event Action OnShieldBroken;
    public event Action<int, int> OnShieldDamaged;

    private float invulnerableUntil;
    private float shieldExpiresAt;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor2D>();
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        if (ShieldHealth > 0 && Time.time >= shieldExpiresAt)
            ClearShield();
    }

    public void TakeDamage(int amount)
    {
        using (GameplayProfilerMarkers.Damage.Auto())
        {
            if (IsDead || IsInvulnerable || amount <= 0)
                return;

            if (HasShield)
            {
                int absorbed = Mathf.Min(ShieldHealth, amount);
                ShieldHealth -= absorbed;
                amount -= absorbed;
                OnShieldChanged?.Invoke(ShieldHealth, MaxShieldHealth);
                OnShieldDamaged?.Invoke(absorbed, ShieldHealth);
                if (ShieldHealth == 0)
                    OnShieldBroken?.Invoke();
            }
            invulnerableUntil = Time.time + invulnerabilityTime;
            if (amount == 0)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                Die();
                return;
            }

            invulnerableUntil = Time.time + invulnerabilityTime;
            OnDamaged?.Invoke();
        }
    }

    public void SetSkill2Invulnerable(bool isInvulnerable)
    {
        skill2Invulnerable = isInvulnerable && !IsDead;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int restored = Mathf.Min(amount, maxHealth - CurrentHealth);
        if (restored <= 0) return;
        CurrentHealth += restored;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnHealed?.Invoke(restored);
    }

    public bool Revive(int healthAmount, float protectionSeconds)
    {
        if (!IsDead || healthAmount <= 0) return false;
        IsDead = false;
        skill2Invulnerable = false;
        CurrentHealth = Mathf.Clamp(healthAmount, 1, maxHealth);
        invulnerableUntil = Time.time + Mathf.Max(0f, protectionSeconds);
        OnRevived?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        return true;
    }

    public void ActivateShield(int amount, float duration)
    {
        if (IsDead || amount <= 0 || duration <= 0f)
            return;
        ShieldHealth = MaxShieldHealth = amount;
        shieldExpiresAt = Time.time + duration;
        OnShieldChanged?.Invoke(ShieldHealth, MaxShieldHealth);
    }

    public void SetMaximumHealth(int amount, bool healIncrease)
    {
        int previousMaximum = maxHealth;
        maxHealth = Mathf.Max(1, amount);
        CurrentHealth = Mathf.Clamp(CurrentHealth + (healIncrease ? Mathf.Max(0, maxHealth - previousMaximum) : 0), 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void RestoreCurrentHealth(int amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Clamp(amount, 1, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void ClearShield()
    {
        ShieldHealth = 0;
        MaxShieldHealth = 0;
        shieldExpiresAt = 0f;
        OnShieldChanged?.Invoke(0, 0);
    }

    private void Die()
    {
        skill2Invulnerable = false;
        IsDead = true;
        ClearShield();
        OnDied?.Invoke();
    }
}
