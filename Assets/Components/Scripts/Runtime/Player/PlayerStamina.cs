using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField, Min(0f)] private float recoveryPerSecond = 12f;
    [SerializeField, Min(0f)] private float recoveryDelay = 0.5f;

    [Header("Action Costs")]
    [SerializeField, Min(0f)] private float dashCost = 20f;
    [SerializeField, Min(0f)] private float skill1Cost = 40f;
    [SerializeField, Min(0f)] private float skill2Cost = 67f;
    [SerializeField, Min(1), Tooltip("Số lần đánh xa từ một thanh stamina gốc, trước khi hồi phục.")]
    private int skill3CastsPerBaseBar = 6;

    private float currentStamina;
    private float recoveryTimer;
    private float recoveryBonus;
    private float baseStamina;
    private float maximumBonus;
    private bool skill3BlocksRecovery;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float BaseStamina => baseStamina;
    public float Skill3Cost => baseStamina / Mathf.Max(1, skill3CastsPerBaseBar);
    public float RecoveryPerSecond => recoveryPerSecond * (1f + recoveryBonus);
    public int ChapterIndex { get; private set; } = 1;
    public bool IsSkill2Unlocked => ChapterIndex >= 1;
    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        maxStamina = Mathf.Max(1f, maxStamina);
        baseStamina = maxStamina;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (skill3BlocksRecovery || currentStamina >= maxStamina || recoveryPerSecond <= 0f)
            return;

        if (recoveryTimer > 0f)
        {
            recoveryTimer = Mathf.Max(0f, recoveryTimer - Time.deltaTime);
            if (recoveryTimer > 0f)
                return;
        }

        SetCurrentStamina(currentStamina + RecoveryPerSecond * Time.deltaTime);
    }

    public void SetRecoveryBonus(float bonus) => recoveryBonus = Mathf.Clamp(bonus, 0f, 0.3f);
    public void RestoreCurrentStamina(float value) => SetCurrentStamina(value);

    public void ApplyChapterProfile(int chapter)
    {
        ChapterIndex = Mathf.Clamp(chapter, 1, 5);
        baseStamina = ChapterBalance.PlayerStamina(ChapterIndex);
        RefreshMaximum();
    }

    public void SetMaximumBonus(float bonus)
    {
        maximumBonus = Mathf.Clamp01(bonus);
        RefreshMaximum();
    }

    private void RefreshMaximum()
    {
        float previousMaximum = maxStamina;
        maxStamina = baseStamina * (1f + maximumBonus);
        currentStamina = Mathf.Clamp(currentStamina + Mathf.Max(0f, maxStamina - previousMaximum), 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void SetSkill3RecoveryBlocked(bool blocked)
    {
        skill3BlocksRecovery = blocked;
        if (!blocked)
            recoveryTimer = recoveryDelay;
    }

    public bool TrySpendDash() => TrySpend(dashCost);

    public bool TrySpendSkill(PlayerSkill skill)
    {
        if (!IsSkillUnlocked(skill))
            return false;
        return TrySpend(GetSkillCost(skill));
    }

    public bool IsSkillUnlocked(PlayerSkill skill) => skill != PlayerSkill.Skill2 || IsSkill2Unlocked;
    public bool CanAffordSkill(PlayerSkill skill) => currentStamina + 0.001f >= GetSkillCost(skill);
    public float GetSkillCost(PlayerSkill skill) => skill switch
        {
            PlayerSkill.Skill1 => Mathf.Max(0f, skill1Cost),
            PlayerSkill.Skill2 => Mathf.Max(0f, skill2Cost),
            PlayerSkill.Skill3 => Mathf.Max(0f, Skill3Cost),
            _ => float.PositiveInfinity
        };

    private bool TrySpend(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (currentStamina + 0.001f < amount)
            return false;

        if (amount <= 0f)
            return true;

        recoveryTimer = recoveryDelay;
        SetCurrentStamina(currentStamina - amount);
        return true;
    }

    private void SetCurrentStamina(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxStamina);
        if (Mathf.Approximately(clamped, currentStamina))
            return;

        currentStamina = clamped;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void OnValidate()
    {
        maxStamina = Mathf.Max(1f, maxStamina);
        recoveryPerSecond = Mathf.Max(0f, recoveryPerSecond);
        recoveryDelay = Mathf.Max(0f, recoveryDelay);
        dashCost = Mathf.Max(0f, dashCost);
        skill1Cost = Mathf.Max(0f, skill1Cost);
        skill2Cost = Mathf.Max(0f, skill2Cost);
        skill3CastsPerBaseBar = Mathf.Max(1, skill3CastsPerBaseBar);
    }
}
