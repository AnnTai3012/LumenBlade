using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PlayerConsumables : MonoBehaviour
{
    [SerializeField] private GameResourceSettings settings;
    private Health health;
    private RunResources resources;
    private GameObject shieldEffect;
    private Animator shieldAnimator;
    private float shieldBreakDuration;
    private float shieldActivationDuration;
    private float shieldBreakEndsAt;
    private bool isShieldBreaking;
    private static readonly int ShieldState = Animator.StringToHash("Base Layer.Shiled");
    private static readonly int ShieldBreakState = Animator.StringToHash("Base Layer.Break");
    public event Action<string> OnUseFeedback;

    private void Awake()
    {
        health = GetComponent<Health>();
        resources = RunResources.GetOrCreate(settings);
        if (settings == null)
        {
            Debug.LogError("PlayerConsumables needs Resources and UI Settings.", this);
            enabled = false;
            return;
        }
        shieldEffect = CreateEffect(settings.ShieldEffectPrefab, "Shield Effect");
        if (shieldEffect != null)
        {
            shieldAnimator = shieldEffect.GetComponent<Animator>();
            if (shieldAnimator != null && shieldAnimator.runtimeAnimatorController != null)
                foreach (AnimationClip clip in shieldAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == "Break") shieldBreakDuration = clip.length;
                    if (clip.name == "Shiled") shieldActivationDuration = clip.length;
                }
        }
    }

    private GameObject CreateEffect(GameObject prefab, string effectName)
    {
        if (prefab == null)
            return null;
        GameObject effect = Instantiate(prefab, transform, false);
        effect.name = effectName;
        effect.SetActive(false);
        return effect;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnShieldChanged += RefreshShield;
            health.OnShieldBroken += ShowShieldBreak;
            health.OnShieldDamaged += PlayShieldHit;
            health.OnDied += HideEffects;
            RefreshShield(health.HasShield ? health.ShieldHealth : 0, health.MaxShieldHealth);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnShieldChanged -= RefreshShield;
            health.OnShieldBroken -= ShowShieldBreak;
            health.OnShieldDamaged -= PlayShieldHit;
            health.OnDied -= HideEffects;
        }
        HideEffects();
    }

    private void Update()
    {
        if (isShieldBreaking && Time.time >= shieldBreakEndsAt)
            HideEffects();
    }

    public bool TryUse(ConsumableKind kind)
    {
        if (health == null || health.IsDead || settings == null || Time.timeScale <= 0f)
            return false;
        if (kind == ConsumableKind.ReviveCross)
            return Report("Thánh giá tự dùng khi hết HP để hồi sinh tại chỗ");
        resources.SelectConsumable(kind);
        if (resources.Count(kind) == 0)
            return Report(kind == ConsumableKind.CryHeal ? "Đã hết đá hồi máu" : "Đã hết đá khiên");
        if (kind == ConsumableKind.CryHeal && health.CurrentHealth >= health.MaxHealth)
            return Report("Máu đã đầy");
        if (kind == ConsumableKind.CryShield && health.HasShield)
            return Report("Khiên đang hoạt động");
        if (!resources.TryConsume(kind))
            return false;

        if (kind == ConsumableKind.CryHeal)
        {
            GameAudioManager.PlayCue(GameAudioCue.HealItemUse);
            health.Heal(Mathf.RoundToInt(health.MaxHealth * settings.HealFraction));
            OnUseFeedback?.Invoke("Đã dùng CryHeal");
        }
        else
        {
            health.ActivateShield(Mathf.CeilToInt(health.MaxHealth * settings.ShieldFraction), settings.ShieldDuration);
            GameAudioManager.StopCue(GameAudioCue.ShieldBreak);
            GameAudioManager.PlayCueForDuration(GameAudioCue.ShieldItemUse, ShieldAnimationSeconds(shieldActivationDuration));
            OnUseFeedback?.Invoke("Đã bật khiên");
        }
        return true;
    }

    private bool Report(string message)
    {
        OnUseFeedback?.Invoke(message);
        return false;
    }

    private void RefreshShield(int amount, int maximum)
    {
        if (shieldEffect == null) return;
        if (amount <= 0)
        {
            GameAudioManager.StopCue(GameAudioCue.ShieldItemUse);
            if (!isShieldBreaking) shieldEffect.SetActive(false);
            return;
        }

        bool restart = isShieldBreaking || !shieldEffect.activeSelf;
        isShieldBreaking = false;
        shieldEffect.SetActive(true);
        if (restart && shieldAnimator != null && shieldAnimator.HasState(0, ShieldState))
        {
            shieldAnimator.Play(ShieldState, 0, 0f);
            shieldAnimator.Update(0f);
        }
    }

    private void ShowShieldBreak()
    {
        GameAudioManager.StopCue(GameAudioCue.ShieldItemUse);
        GameAudioManager.StopCue(GameAudioCue.ShieldHit);
        float breakSeconds = ShieldAnimationSeconds(shieldBreakDuration);
        GameAudioManager.PlayCueForDuration(GameAudioCue.ShieldBreak, breakSeconds);
        if (shieldEffect == null || shieldAnimator == null || shieldBreakDuration <= 0f)
            return;

        // Shield depletion hid the object; re-enable its Animator before querying its states.
        shieldEffect.SetActive(true);
        if (!shieldAnimator.HasState(0, ShieldBreakState))
        {
            HideEffects();
            return;
        }
        isShieldBreaking = true;
        shieldBreakEndsAt = Time.time + breakSeconds;
        shieldAnimator.Play(ShieldBreakState, 0, 0f);
        shieldAnimator.Update(0f);
    }

    private void HideEffects()
    {
        GameAudioManager.StopCue(GameAudioCue.ShieldItemUse);
        GameAudioManager.StopCue(GameAudioCue.ShieldHit);
        GameAudioManager.StopCue(GameAudioCue.ShieldBreak);
        isShieldBreaking = false;
        if (shieldEffect != null) shieldEffect.SetActive(false);
    }

    private float ShieldAnimationSeconds(float clipSeconds) => shieldAnimator != null
        ? clipSeconds / Mathf.Max(0.01f, Mathf.Abs(shieldAnimator.speed)) : clipSeconds;

    private void PlayShieldHit(int absorbed, int remaining)
    {
        // The final impact uses Break alone; fully absorbed hits never play player TakeHit.
        if (absorbed > 0 && remaining > 0) GameAudioManager.PlayCue(GameAudioCue.ShieldHit);
    }
}
