using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SkillHudState { Ready, Executing, Locked, LowStamina, Busy, Paused, Dead, Unavailable }

[DisallowMultipleComponent]
public sealed class PlayerSkillHud : MonoBehaviour
{
    [SerializeField] private Vector2 cornerInset = new Vector2(36f, 28f);
    [SerializeField, HideInInspector] private UiSpriteFrame[] frames = new UiSpriteFrame[3];
    [SerializeField, HideInInspector] private Image[] shades = new Image[3];
    [SerializeField, HideInInspector] private TMP_Text[] labels = new TMP_Text[3];
    [SerializeField, HideInInspector] private TMP_Text[] costs = new TMP_Text[3];
    [SerializeField, HideInInspector] private TMP_Text[] keys = new TMP_Text[3];
    [SerializeField, HideInInspector] private Image[] icons = new Image[3];
    [SerializeField, HideInInspector] private TMP_Text[] titles = new TMP_Text[3];
    private readonly SkillHudState[] shownStates = new SkillHudState[3];
    private readonly float[] shownCosts = { -1f, -1f, -1f };
    private Health health;
    private PlayerStamina stamina;
    private PlayerCombat combat;
    private PlayerMotor2D motor;
    private PlayerInputReader input;
    [SerializeField, HideInInspector] private RectTransform strip;
    private static readonly Color Gold = new Color(1f, 0.8f, 0.36f);

    private void Awake() => Build();
    private void OnEnable()
    {
        if (strip == null) Build();
        Refresh(true);
    }

    public void BindPlayer(Health playerHealth, PlayerStamina playerStamina)
    {
        if (health != playerHealth)
        {
            health = playerHealth;
            combat = health != null ? health.GetComponent<PlayerCombat>() : null;
            motor = health != null ? health.GetComponent<PlayerMotor2D>() : null;
            input = health != null ? health.GetComponent<PlayerInputReader>() : null;
            for (int i = 0; i < 3; i++)
                if (keys[i] != null) keys[i].text = input != null ? input.GetSkillKey((PlayerSkill)i).ToString() : "";
        }
        stamina = playerStamina;
        GameResourceSettings settings = RunResources.Instance != null ? RunResources.Instance.Settings : null;
        if (settings != null)
            for (int i = 0; i < 3; i++)
            {
                icons[i].sprite = settings.SkillIcon((PlayerSkill)i);
                icons[i].enabled = icons[i].sprite != null;
                titles[i].gameObject.SetActive(icons[i].sprite == null);
            }
        Refresh(true);
    }

    public SkillHudState GetState(PlayerSkill skill)
    {
        if (health == null || stamina == null || combat == null) return SkillHudState.Unavailable;
        if (health.IsDead) return SkillHudState.Dead;
        if (Time.timeScale <= 0f || GameplayPanelController.BlocksGameplayInput) return SkillHudState.Paused;
        if (combat.IsExecutingSkill(skill)) return SkillHudState.Executing;
        if (!stamina.IsSkillUnlocked(skill)) return SkillHudState.Locked;
        if (!combat.IsSkillConfigured(skill)) return SkillHudState.Unavailable;
        if (!stamina.CanAffordSkill(skill)) return SkillHudState.LowStamina;
        if (combat.IsBusy || (motor != null && (motor.IsDashing || motor.IsKnockedBack))) return SkillHudState.Busy;
        return SkillHudState.Ready;
    }

    private void LateUpdate() => Refresh(false);

    private void Refresh(bool force)
    {
        if (strip == null) return;
        Vector2 position = new Vector2(-cornerInset.x, cornerInset.y);
        if (strip.anchoredPosition != position) strip.anchoredPosition = position;
        for (int i = 0; i < 3; i++)
        {
            PlayerSkill skill = (PlayerSkill)i;
            SkillHudState state = GetState(skill);
            if (force || shownStates[i] != state)
            {
                shownStates[i] = state;
                bool active = state == SkillHudState.Executing;
                bool ready = state == SkillHudState.Ready;
                frames[i].color = active ? new Color(0.5f, 1f, 1f) : ready ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                shades[i].color = active ? new Color(0.05f, 0.7f, 0.85f, 0.3f) : new Color(0f, 0f, 0f, ready ? 0f : 0.45f);
                icons[i].color = active || ready ? Color.white : new Color(0.65f, 0.65f, 0.65f);
                labels[i].color = active ? Color.cyan : ready ? Gold : new Color(0.65f, 0.65f, 0.65f);
                labels[i].text = state switch
                {
                    SkillHudState.Executing => "ĐANG DÙNG", SkillHudState.Ready => "SẴN SÀNG",
                    SkillHudState.Locked => "CHƯA MỞ", SkillHudState.LowStamina => "THIẾU SP",
                    SkillHudState.Busy => "CHỜ", SkillHudState.Paused => "TẠM DỪNG",
                    SkillHudState.Dead => "ĐÃ CHẾT", _ => "CHƯA CÓ"
                };
            }
            float cost = stamina != null ? stamina.GetSkillCost(skill) : 0f;
            if (force || !Mathf.Approximately(shownCosts[i], cost))
            {
                shownCosts[i] = cost;
                costs[i].text = "SP " + cost.ToString("0.#");
            }
        }
    }

    private void Build()
    {
        if (strip != null) return;
        UiSpriteFrame template = transform.Find("ResourceBars/CryHeal Cell")?.GetComponent<UiSpriteFrame>();
        TMP_Text textTemplate = transform.Find("ResourceBars/CryHeal Cell/Hotkey")?.GetComponent<TMP_Text>();
        strip = Rect("Skill HUD", transform, new Vector2(282f, 112f));
        strip.anchorMin = strip.anchorMax = new Vector2(1f, 0f);
        strip.pivot = new Vector2(1f, 0f);
        strip.anchoredPosition = new Vector2(-cornerInset.x, cornerInset.y);
        for (int i = 0; i < 3; i++)
        {
            RectTransform cell = Rect("Skill " + (i + 1) + " Cell", strip, new Vector2(88f, 88f));
            cell.anchoredPosition = new Vector2(i * 98f, 24f);
            frames[i] = cell.gameObject.AddComponent<UiSpriteFrame>();
            frames[i].CopyAppearanceFrom(template);
            frames[i].raycastTarget = false;
            var icon = Rect("Skill Icon", cell, new Vector2(64f, 56f));
            icon.anchoredPosition = new Vector2(12f, 24f);
            icons[i] = icon.gameObject.AddComponent<Image>();
            icons[i].preserveAspect = true; icons[i].raycastTarget = false; icons[i].enabled = false;
            var shade = Rect("State Tint", cell, new Vector2(64f, 64f));
            shade.anchoredPosition = new Vector2(12f, 12f);
            shades[i] = shade.gameObject.AddComponent<Image>(); shades[i].raycastTarget = false;
            titles[i] = Text("Skill Name", cell, "SKILL\n" + (i + 1), new Vector2(44f, 48f), new Vector2(66f, 46f), 20f, Color.white, textTemplate);
            var badge = Rect("Hotkey Background", cell, new Vector2(22f, 22f));
            badge.anchoredPosition = new Vector2(4f, 63f);
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.color = new Color(0.02f, 0.03f, 0.05f, 0.85f); badgeImage.raycastTarget = false;
            keys[i] = Text("Hotkey", cell, "", new Vector2(15f, 74f), new Vector2(24f, 22f), 17f, Gold, textTemplate);
            costs[i] = Text("Stamina Cost", cell, "", new Vector2(44f, 17f), new Vector2(70f, 20f), 14f, Gold, textTemplate);
            labels[i] = Text("State", cell, "", new Vector2(44f, -13f), new Vector2(96f, 22f), 13f, Gold, textTemplate);
        }
        Refresh(true);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.sizeDelta = size; return rect;
    }

    private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size,
        float fontSize, Color color, TMP_Text template)
    {
        var rect = Rect(name, parent, size); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (template != null) text.font = template.font;
        text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }
}
