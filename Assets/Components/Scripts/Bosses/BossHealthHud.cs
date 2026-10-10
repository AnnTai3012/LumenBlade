using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossHealthHud : MonoBehaviour
{
    [SerializeField] private GameObject view;
    [SerializeField] private UnityEngine.UI.Image healthFill;
    [SerializeField] private TMP_Text bossName;

    private BossEncounter currentBoss;
    private MonsterHealth observedHealth;

    public BossEncounter CurrentBoss => currentBoss;
    public bool IsVisible => view != null && view.activeSelf;

    private void Awake()
    {
        if (view == null || healthFill == null || bossName == null)
        {
            Debug.LogError("BossHealthHud needs its view, fill and boss name references.", this);
            enabled = false;
        }
        Hide();
    }

    private void LateUpdate()
    {
        BossEncounter candidate = currentBoss != null && currentBoss.IsPlayerInside ? currentBoss : null;
        if (candidate == null)
        {
            float nearestDistance = float.MaxValue;
            var encounters = BossEncounter.ActiveEncounters;
            for (int i = 0; i < encounters.Count; i++)
            {
                BossEncounter encounter = encounters[i];
                if (encounter == null || !encounter.IsPlayerInside) continue;
                float distance = ((Vector2)encounter.transform.position - (Vector2)encounter.Player.transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                candidate = encounter;
                nearestDistance = distance;
            }
        }

        if (candidate != currentBoss) Bind(candidate);
        if (view != null && view.activeSelf != (candidate != null)) view.SetActive(candidate != null);
    }

    private void Bind(BossEncounter encounter)
    {
        Unsubscribe();
        currentBoss = encounter;
        if (encounter == null) return;
        observedHealth = encounter.Health;
        bossName.text = encounter.BossName;
        observedHealth.OnHealthChanged += UpdateBar;
        observedHealth.OnDied += Hide;
        UpdateBar(observedHealth.CurrentHealth, observedHealth.MaxHealth);
    }

    private void UpdateBar(int current, int maximum)
    {
        healthFill.fillAmount = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
    }

    private void Hide()
    {
        if (view != null) view.SetActive(false);
    }

    private void OnDisable()
    {
        Unsubscribe();
        currentBoss = null;
        Hide();
    }

    private void Unsubscribe()
    {
        if (observedHealth != null)
        {
            observedHealth.OnHealthChanged -= UpdateBar;
            observedHealth.OnDied -= Hide;
        }
        observedHealth = null;
    }
}
