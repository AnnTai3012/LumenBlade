using System;
using TMPro;
using UnityEngine;

public enum ConsumableKind { CryHeal = 0, CryShield = 1, ReviveCross = 2 }
public enum StoreCatalog { Supplies = 0, SpecialItems = 1 }
public enum BlessingReward
{
    MaxHealth = 0, MaxStamina = 1, StaminaRecovery = MaxStamina, CryHeal = 2, CryShield = 3, RestoreHealth = 4,
    AllDamage = 5, ConsumableBundle = 6
}
public enum PurchaseResult { Success, NotEnoughSpiritStone, InventoryFull }

[Serializable]
public sealed class BlessingOption
{
    public string title;
    [TextArea(2, 5)] public string description;
    public BlessingReward reward;
    public Sprite icon;
}

[CreateAssetMenu(menuName = "BTL/Resources and UI Settings")]
public sealed class GameResourceSettings : ScriptableObject
{
    [Header("Consumables (GDD)")]
    [SerializeField, Min(1)] private int capacityPerKind = 5;
    [SerializeField, Min(0)] private int cryHealPrice = 6;
    [SerializeField, Min(0)] private int cryShieldPrice = 5;
    [SerializeField, Range(0.01f, 1f)] private float healFraction = 0.3f;
    [SerializeField, Range(0.01f, 1f)] private float shieldFraction = 0.25f;
    [SerializeField, Min(0.1f)] private float shieldDuration = 8f;
    [SerializeField] private Sprite cryHealIcon;
    [SerializeField] private Sprite cryShieldIcon;
    [SerializeField] private Sprite spiritStoneIcon;
    [SerializeField] private Sprite keyIcon;
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private GameObject shieldEffectPrefab;

    [Header("Thánh giá hồi sinh")]
    [SerializeField, Min(1)] private int reviveCrossCapacity = 2;
    [SerializeField, Min(1)] private int reviveCrossPrice = 30;
    [SerializeField, Range(0.01f, 1f)] private float reviveHealthFraction = 0.5f;
    [SerializeField, Min(0f)] private float reviveInvulnerability = 3f;
    [SerializeField] private Sprite reviveCrossIcon;

    [Header("UI artwork")]
    [SerializeField] private Sprite itemCellFrame;
    [SerializeField] private Sprite shopCellFrame;
    [SerializeField] private Sprite shopPanelFrame;
    [SerializeField] private Sprite blessingPanelFrame;
    [SerializeField] private TMP_FontAsset font;
    [Header("Skill HUD icons")]
    [SerializeField] private Sprite skill1Icon;
    [SerializeField] private Sprite skill2Icon;
    [SerializeField] private Sprite skill3Icon;

    [Header("Damage blessing")]
    [SerializeField, Range(0.01f, 1f)] private float damageBonusPerBlessing = 0.05f;
    [SerializeField, Range(0.01f, 1f)] private float maxDamageBonus = 1f;
    [SerializeField] private Sprite damageBlessingIcon;

    [Header("Four editable blessing choices")]
    [SerializeField] private BlessingOption[] blessings =
    {
        new BlessingOption { title = "Sinh lực", description = "+5% HP gốc của Chapter.\nTối đa +100% (20 lần).", reward = BlessingReward.MaxHealth },
        new BlessingOption { title = "Thể lực", description = "+5% stamina gốc của Chapter.\nTối đa +100% (20 lần).", reward = BlessingReward.MaxStamina },
        new BlessingOption { title = "Sức mạnh", description = "+{damagePercent}% sát thương mọi đòn đánh.\nTối đa +{damageCap}%.", reward = BlessingReward.AllDamage },
        new BlessingOption { title = "Vật phẩm", description = "Nhận 1 CryHeal\nvà 1 CryShield.\nNhấn 1 / 2 để dùng.", reward = BlessingReward.ConsumableBundle }
    };

    public int CapacityPerKind => capacityPerKind;
    public float HealFraction => healFraction;
    public float ShieldFraction => shieldFraction;
    public float ShieldDuration => shieldDuration;
    public float ReviveHealthFraction => reviveHealthFraction;
    public float ReviveInvulnerability => reviveInvulnerability;
    public Sprite SpiritStoneIcon => spiritStoneIcon;
    public Sprite KeyIcon => keyIcon;
    public GameObject HealEffectPrefab => healEffectPrefab;
    public GameObject ShieldEffectPrefab => shieldEffectPrefab;
    public Sprite ItemCellFrame => itemCellFrame;
    public Sprite ShopCellFrame => shopCellFrame;
    public Sprite ShopPanelFrame => shopPanelFrame;
    public Sprite BlessingPanelFrame => blessingPanelFrame;
    public TMP_FontAsset Font => font;
    public Sprite SkillIcon(PlayerSkill skill) => skill == PlayerSkill.Skill1 ? skill1Icon :
        skill == PlayerSkill.Skill2 ? skill2Icon : skill3Icon;
    public float DamageBonusPerBlessing => damageBonusPerBlessing;
    public float MaxDamageBonus => maxDamageBonus;
    public Sprite DamageBlessingIcon => damageBlessingIcon;
    public BlessingOption[] Blessings => blessings;
    public int Capacity(ConsumableKind kind) => kind == ConsumableKind.ReviveCross ? reviveCrossCapacity : capacityPerKind;
    public int Price(ConsumableKind kind) => kind switch
    {
        ConsumableKind.CryHeal => cryHealPrice,
        ConsumableKind.CryShield => cryShieldPrice,
        ConsumableKind.ReviveCross => reviveCrossPrice,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public Sprite Icon(ConsumableKind kind) => kind switch
    {
        ConsumableKind.CryHeal => cryHealIcon,
        ConsumableKind.CryShield => cryShieldIcon,
        ConsumableKind.ReviveCross => reviveCrossIcon,
        _ => null
    };
    public static string ItemName(ConsumableKind kind) => kind == ConsumableKind.ReviveCross ? "Thánh giá" : kind.ToString();
    public static bool IsSoldAt(ConsumableKind kind, StoreCatalog catalog) => catalog == StoreCatalog.SpecialItems
        ? kind == ConsumableKind.ReviveCross
        : kind == ConsumableKind.CryHeal || kind == ConsumableKind.CryShield;
}
