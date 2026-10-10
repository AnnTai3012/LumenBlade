using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class PlayerResourceHud : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerStamina stamina;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private Image shieldFill;
    [SerializeField] private RectTransform healthFrame;
    [SerializeField] private RectTransform healthRecess;
    [SerializeField] private TMP_Text healthValue;
    [SerializeField] private TMP_Text shieldValue;
    [SerializeField] private TMP_Text spiritStoneValue;
    [SerializeField] private TMP_Text keyValue;
    [SerializeField] private Image keyIcon;
    [SerializeField] private TMP_Text[] itemCounts;
    [SerializeField] private Button[] itemButtons;
    [SerializeField] private RectTransform[] itemSlots;
    [SerializeField] private RectTransform selectionMarker;
    [SerializeField] private TMP_Text itemFeedback;

    private bool isSubscribedToHealth;
    private bool isSubscribedToStamina;
    private float bindRetryTimer;
    private RunResources resources;
    private PlayerKeyInventory keys;
    private PlayerConsumables consumables;
    private bool subscribedToInventory;
    private float feedbackExpires;
    private int displayedShieldSeconds = -1;
    private UnityEngine.Events.UnityAction[] itemActions;
    private float initialMaxHealth;
    private float baseHealthFillWidth;
    private float baseShieldFillWidth;
    private float baseHealthFrameWidth;
    private float baseHealthRecessWidth;
    private Vector2 baseHealthValuePosition;
    private Vector2 baseShieldValuePosition;
    private PlayerSkillHud skillHud;

    private void Awake()
    {
        skillHud = GetComponent<PlayerSkillHud>();
        if (skillHud == null) skillHud = gameObject.AddComponent<PlayerSkillHud>();
        healthFrame ??= transform.Find("ResourceBars/HealthBar Rim") as RectTransform;
        healthRecess ??= transform.Find("ResourceBars/HealthBar Recess") as RectTransform;
        baseHealthFillWidth = healthFill != null ? healthFill.rectTransform.sizeDelta.x : 0f;
        baseShieldFillWidth = shieldFill != null ? shieldFill.rectTransform.sizeDelta.x : 0f;
        baseHealthFrameWidth = healthFrame != null ? healthFrame.sizeDelta.x : 0f;
        baseHealthRecessWidth = healthRecess != null ? healthRecess.sizeDelta.x : 0f;
        baseHealthValuePosition = healthValue != null ? healthValue.rectTransform.anchoredPosition : Vector2.zero;
        baseShieldValuePosition = shieldValue != null ? shieldValue.rectTransform.anchoredPosition : Vector2.zero;
        if (itemButtons == null) return;
        itemActions = new UnityEngine.Events.UnityAction[itemButtons.Length];
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int index = i;
            itemActions[i] = () => consumables?.TryUse((ConsumableKind)index);
            itemButtons[i].onClick.AddListener(itemActions[i]);
        }
    }

    private void OnEnable()
    {
        BindToPlayer();
    }

    private void Update()
    {
        if (itemFeedback != null && itemFeedback.text.Length > 0 && Time.unscaledTime >= feedbackExpires)
            itemFeedback.text = "";
        if (health != null && shieldValue != null)
        {
            int seconds = Mathf.CeilToInt(health.ShieldSecondsRemaining);
            if (seconds != displayedShieldSeconds)
            {
                displayedShieldSeconds = seconds;
                shieldValue.text = health.HasShield ? "KHIÊN " + health.ShieldHealth + " · " + seconds + "s" : "";
            }
        }
        if (health != null && stamina != null && subscribedToInventory)
            return;

        bindRetryTimer -= Time.unscaledDeltaTime;
        if (bindRetryTimer <= 0f)
        {
            bindRetryTimer = 0.5f;
            BindToPlayer();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (itemActions != null)
            for (int i = 0; i < itemActions.Length; i++)
                if (itemButtons[i] != null) itemButtons[i].onClick.RemoveListener(itemActions[i]);
    }

    private void BindToPlayer()
    {
        Health resolvedHealth = health != null ? health : FindFirstObjectByType<Health>();
        PlayerStamina resolvedStamina = stamina != null ? stamina : FindFirstObjectByType<PlayerStamina>();

        if (health != resolvedHealth)
        {
            UnsubscribeHealth();
            health = resolvedHealth;
        }

        if (stamina != resolvedStamina)
        {
            UnsubscribeStamina();
            stamina = resolvedStamina;
        }
        skillHud?.BindPlayer(health, stamina);

        if (health != null && !isSubscribedToHealth)
        {
            health.OnHealthChanged += UpdateHealth;
            health.OnShieldChanged += UpdateShield;
            isSubscribedToHealth = true;
        }

        if (stamina != null && !isSubscribedToStamina)
        {
            stamina.OnStaminaChanged += UpdateStamina;
            isSubscribedToStamina = true;
        }

        if (health != null)
            UpdateHealth(health.CurrentHealth, health.MaxHealth);
        if (stamina != null)
            UpdateStamina(stamina.CurrentStamina, stamina.MaxStamina);

        if (!subscribedToInventory && health != null && RunResources.Instance != null)
        {
            resources = RunResources.Instance;
            keys = health.GetComponent<PlayerKeyInventory>();
            consumables = health.GetComponent<PlayerConsumables>();
            resources.OnResourcesChanged += UpdateInventory;
            if (keys != null) keys.OnKeyChanged += UpdateKey;
            if (consumables != null) consumables.OnUseFeedback += ShowFeedback;
            subscribedToInventory = true;
        }
        UpdateInventory();
        UpdateKey(keys != null && keys.HasKey);
        if (health != null) UpdateShield(health.ShieldHealth, health.MaxShieldHealth);
    }

    private void Unsubscribe()
    {
        UnsubscribeHealth();
        UnsubscribeStamina();
        if (subscribedToInventory)
        {
            if (resources != null) resources.OnResourcesChanged -= UpdateInventory;
            if (keys != null) keys.OnKeyChanged -= UpdateKey;
            if (consumables != null) consumables.OnUseFeedback -= ShowFeedback;
        }
        subscribedToInventory = false;
    }

    private void UnsubscribeHealth()
    {
        if (isSubscribedToHealth && health != null)
            health.OnHealthChanged -= UpdateHealth;
        if (isSubscribedToHealth && health != null)
            health.OnShieldChanged -= UpdateShield;

        isSubscribedToHealth = false;
    }

    private void UnsubscribeStamina()
    {
        if (isSubscribedToStamina && stamina != null)
            stamina.OnStaminaChanged -= UpdateStamina;

        isSubscribedToStamina = false;
    }

    private void UpdateHealth(int current, int max)
    {
        if (initialMaxHealth <= 0f) initialMaxHealth = Mathf.Max(1, max);
        float healthScale = Mathf.Max(1, max) / initialMaxHealth;
        SetFill(healthFill, current, max);
        SetBarWidth(healthFill != null ? healthFill.rectTransform : null, baseHealthFillWidth, healthScale);
        SetBarWidth(shieldFill != null ? shieldFill.rectTransform : null, baseShieldFillWidth, healthScale);
        SetBarWidth(healthFrame, baseHealthFrameWidth, healthScale);
        SetBarWidth(healthRecess, baseHealthRecessWidth, healthScale);
        float extraWidth = healthFrame != null ? healthFrame.sizeDelta.x - baseHealthFrameWidth : 0f;
        if (healthValue != null) healthValue.rectTransform.anchoredPosition = baseHealthValuePosition + Vector2.right * extraWidth;
        if (shieldValue != null) shieldValue.rectTransform.anchoredPosition = baseShieldValuePosition + Vector2.right * extraWidth;
        if (healthValue != null) healthValue.text = current + " / " + max;
    }

    private void UpdateStamina(float current, float max)
    {
        SetFill(staminaFill, current, max);
    }

    private static void SetFill(Image image, float current, float max)
    {
        if (image == null)
            return;

        image.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private static void SetBarWidth(RectTransform rect, float baseWidth, float scale)
    {
        if (rect == null || baseWidth <= 0f) return;
        Vector2 size = rect.sizeDelta;
        size.x = baseWidth * scale;
        rect.sizeDelta = size;
    }

    private void UpdateShield(int current, int maximum)
    {
        SetFill(shieldFill, current, health != null ? health.MaxHealth : maximum);
        if (shieldFill != null) shieldFill.gameObject.SetActive(current > 0);
        displayedShieldSeconds = -1;
    }

    private void UpdateInventory()
    {
        if (resources == null) return;
        if (spiritStoneValue != null) spiritStoneValue.text = resources.SpiritStone.ToString();
        if (itemCounts != null)
            for (int i = 0; i < itemCounts.Length; i++)
                itemCounts[i].text = resources.Count((ConsumableKind)i) + "/" + resources.Settings.Capacity((ConsumableKind)i);
        if (selectionMarker != null && itemSlots != null && (int)resources.SelectedConsumable < itemSlots.Length)
        {
            selectionMarker.SetParent(itemSlots[(int)resources.SelectedConsumable], false);
            selectionMarker.anchorMin = selectionMarker.anchorMax = new Vector2(0.5f, 1f);
            selectionMarker.anchoredPosition = new Vector2(0f, 16f);
        }
    }

    private void UpdateKey(bool hasKey)
    {
        if (keyValue != null) keyValue.text = hasKey ? "1 / 1" : "0 / 1";
        if (keyIcon != null) keyIcon.color = hasKey ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.65f);
    }

    private void ShowFeedback(string message)
    {
        if (itemFeedback == null) return;
        itemFeedback.text = message;
        feedbackExpires = Time.unscaledTime + 2.5f;
    }
}
