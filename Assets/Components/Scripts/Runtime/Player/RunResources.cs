using System;
using System.Collections.Generic;
using UnityEngine;

// One inventory for the current run. Scene players and all UI observe this owner.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class RunResources : MonoBehaviour
{
    public const int MaxStatBlessings = 20;
    public const float StatBonusPerBlessing = 0.05f;
    [SerializeField] private GameResourceSettings settings;

    [Header("Tài nguyên - chỉnh trực tiếp để chơi thử")]
    [SerializeField, Min(0), Tooltip("Số SpiritStone khi bắt đầu từ scene này. Có thể sửa khi Play để thử cửa hàng.")]
    private long spiritStone;
    [SerializeField, Min(0), Tooltip("Số đá hồi máu. Giới hạn theo Capacity Per Kind trong Settings.")]
    private int cryHealCount;
    [SerializeField, Min(0), Tooltip("Số đá khiên. Giới hạn theo Capacity Per Kind trong Settings.")]
    private int cryShieldCount;
    [SerializeField, Min(0), Tooltip("Thánh giá tự dùng khi hết HP. Giới hạn riêng trong Settings.")]
    private int reviveCrossCount;

    public static RunResources Instance { get; private set; }
    public static void EndCurrentRun()
    {
        if (Instance == null) return;
        GameObject owner = Instance.gameObject;
        Instance = null;
        Destroy(owner);
    }
    public GameResourceSettings Settings { get => settings; private set => settings = value; }
    public long SpiritStone => SaturatingAdd(spiritStone, pendingSpiritStone);
    public long BankedSpiritStone => spiritStone;
    public long PendingSpiritStone => pendingSpiritStone;
    public int CryHealCount { get => cryHealCount; private set => cryHealCount = value; }
    public int CryShieldCount { get => cryShieldCount; private set => cryShieldCount = value; }
    public int ReviveCrossCount => reviveCrossCount;
    public ConsumableKind SelectedConsumable { get; private set; }
    public int HealthBlessings { get; private set; }
    public int StaminaBlessings { get; private set; }
    // Keep the old read API and enum value for existing scene/UI integrations.
    public int RecoveryBlessings => StaminaBlessings;
    public int DamageBlessings { get; private set; }
    public float HealthBonus => Mathf.Min(HealthBlessings, MaxStatBlessings) * StatBonusPerBlessing;
    public float StaminaBonus => Mathf.Min(StaminaBlessings, MaxStatBlessings) * StatBonusPerBlessing;
    public float RecoveryBonus => StaminaBonus;
    public float DamageBonus => Settings != null ? Mathf.Min(DamageBlessings * Settings.DamageBonusPerBlessing, Settings.MaxDamageBonus) : 0f;
    public event Action OnResourcesChanged;
    public event Action<ConsumableKind, int> OnConsumableReceived;

    private readonly HashSet<string> rewardedEnemies = new HashSet<string>();
    private readonly HashSet<string> usedLevelKeys = new HashSet<string>();
    private readonly HashSet<string> heldLevelKeys = new HashSet<string>();
    private readonly HashSet<string> visitedLevels = new HashSet<string>();
    private readonly Dictionary<string, string> arrivalDoors = new Dictionary<string, string>();
    private readonly Dictionary<string, RestartCheckpoint> restartCheckpoints = new Dictionary<string, RestartCheckpoint>();
    private string levelPath;
    private bool firstLevelVisit;
    private int chapterIndex;
    private bool awaitingArrival;
    private bool offerBlessingOnArrival;
    private int carriedHealth;
    private float carriedStamina;
    private int carriedShield;
    private float carriedShieldSeconds;
    private bool inspectorResourcesChanged;
    private long pendingSpiritStone;

    private readonly struct RestartCheckpoint
    {
        public readonly string ScenePath;
        public readonly string DoorId;

        public RestartCheckpoint(string scenePath, string doorId)
        {
            ScenePath = scenePath;
            DoorId = doorId;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    public static RunResources GetOrCreate(GameResourceSettings settings)
    {
        if (Instance == null)
        {
            var owner = new GameObject("Run Resources");
            Instance = owner.AddComponent<RunResources>();
        }
        if (settings != null)
        {
            Instance.Settings = settings;
            Instance.ClampResourceValues();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ClampResourceValues();
        DontDestroyOnLoad(gameObject);
    }

    private void OnValidate()
    {
        ClampResourceValues();
        // OnValidate may run while assets load; refresh Unity UI on the next Update.
        inspectorResourcesChanged = true;
    }

    private void ClampResourceValues()
    {
        spiritStone = Math.Max(0L, spiritStone);
        int capacity = settings != null ? settings.CapacityPerKind : int.MaxValue;
        cryHealCount = Mathf.Clamp(cryHealCount, 0, capacity);
        cryShieldCount = Mathf.Clamp(cryShieldCount, 0, capacity);
        reviveCrossCount = Mathf.Clamp(reviveCrossCount, 0, settings != null ? settings.Capacity(ConsumableKind.ReviveCross) : int.MaxValue);
    }

    private void Update()
    {
        if (!inspectorResourcesChanged || Instance != this)
            return;
        inspectorResourcesChanged = false;
        OnResourcesChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int Count(ConsumableKind kind) => kind switch
    {
        ConsumableKind.CryHeal => CryHealCount,
        ConsumableKind.CryShield => CryShieldCount,
        ConsumableKind.ReviveCross => ReviveCrossCount,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public bool HasRoom(ConsumableKind kind) => Settings != null && Count(kind) < Settings.Capacity(kind);

    public void SelectConsumable(ConsumableKind kind)
    {
        SelectedConsumable = kind;
        OnResourcesChanged?.Invoke();
    }

    public bool TryAddConsumable(ConsumableKind kind)
    {
        if (!HasRoom(kind))
            return false;
        ChangeCount(kind, 1);
        OnResourcesChanged?.Invoke();
        return true;
    }

    public bool TryConsume(ConsumableKind kind)
    {
        if (Count(kind) <= 0)
            return false;
        ChangeCount(kind, -1);
        OnResourcesChanged?.Invoke();
        return true;
    }

    public PurchaseResult TryPurchase(ConsumableKind kind)
    {
        if (!HasRoom(kind))
            return PurchaseResult.InventoryFull;
        int price = Settings.Price(kind);
        if (BankedSpiritStone < price)
            return PurchaseResult.NotEnoughSpiritStone;
        spiritStone -= price;
        ChangeCount(kind, 1);
        OnResourcesChanged?.Invoke();
        return PurchaseResult.Success;
    }

    private void ChangeCount(ConsumableKind kind, int delta)
    {
        switch (kind)
        {
            case ConsumableKind.CryHeal: CryHealCount += delta; break;
            case ConsumableKind.CryShield: CryShieldCount += delta; break;
            case ConsumableKind.ReviveCross: reviveCrossCount += delta; break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        if (delta > 0)
            OnConsumableReceived?.Invoke(kind, delta);
    }

    public void EnterLevel(string scenePath, int chapter)
    {
        firstLevelVisit = visitedLevels.Add(scenePath);
        if (levelPath != scenePath)
        {
            levelPath = scenePath;
            pendingSpiritStone = 0;
            rewardedEnemies.Clear();
        }
        if (chapterIndex != chapter)
        {
            chapterIndex = chapter;
            HealthBlessings = 0;
            StaminaBlessings = 0;
            DamageBlessings = 0;
        }
    }

    public bool HasUsedLevelKey(string scenePath) => usedLevelKeys.Contains(scenePath);
    public bool HasHeldLevelKey(string scenePath) => !HasUsedLevelKey(scenePath) && heldLevelKeys.Contains(scenePath);

    public void SetHeldLevelKey(string scenePath, bool held)
    {
        if (string.IsNullOrEmpty(scenePath))
            return;
        if (held && !HasUsedLevelKey(scenePath))
            heldLevelKeys.Add(scenePath);
        else
            heldLevelKeys.Remove(scenePath);
    }

    public void MarkLevelKeyUsed(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
            return;
        usedLevelKeys.Add(scenePath);
        heldLevelKeys.Remove(scenePath);
    }

    public void SetArrivalDoor(string destinationScenePath, string doorId)
    {
        if (!string.IsNullOrEmpty(destinationScenePath) && !string.IsNullOrEmpty(doorId))
            arrivalDoors[destinationScenePath] = doorId;
    }

    public bool TryGetArrivalDoor(string scenePath, out string doorId) => arrivalDoors.TryGetValue(scenePath, out doorId);

    public void SetRestartCheckpoint(string scenePath, string restartScenePath, string restartDoorId)
    {
        if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(restartScenePath) || string.IsNullOrEmpty(restartDoorId))
            return;

        restartCheckpoints[scenePath] = new RestartCheckpoint(restartScenePath, restartDoorId);
    }

    public bool TryGetRestartCheckpoint(string scenePath, out string restartScenePath, out string restartDoorId)
    {
        if (!string.IsNullOrEmpty(scenePath) && restartCheckpoints.TryGetValue(scenePath, out RestartCheckpoint checkpoint))
        {
            restartScenePath = checkpoint.ScenePath;
            restartDoorId = checkpoint.DoorId;
            return true;
        }

        restartScenePath = null;
        restartDoorId = null;
        return false;
    }

    public void PrepareRestart(string scenePath)
    {
        // A restart creates a healthy player rather than restoring the defeated attempt.
        awaitingArrival = false;
        offerBlessingOnArrival = false;
        if (TryGetRestartCheckpoint(scenePath, out string restartScenePath, out string restartDoorId))
            SetArrivalDoor(restartScenePath, restartDoorId);
    }

    public bool TryRewardEnemy(string scenePath, string enemyId)
    {
        if (scenePath != levelPath || string.IsNullOrEmpty(enemyId) ||
            SpiritStone >= long.MaxValue || !rewardedEnemies.Add(enemyId))
            return false;
        pendingSpiritStone++;
        OnResourcesChanged?.Invoke();
        return true;
    }

    public bool CompleteLevel(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || scenePath != levelPath)
            return false;

        spiritStone = SaturatingAdd(spiritStone, pendingSpiritStone);
        pendingSpiritStone = 0;
        rewardedEnemies.Clear();
        OnResourcesChanged?.Invoke();
        return true;
    }

    public void DiscardUnbankedLevelProgressOnDeath()
    {
        if (pendingSpiritStone == 0 && rewardedEnemies.Count == 0)
            return;

        pendingSpiritStone = 0;
        rewardedEnemies.Clear();
        OnResourcesChanged?.Invoke();
    }

    public void PrepareArrival(Health health, PlayerStamina stamina, bool offerBlessing)
    {
        carriedHealth = health != null ? health.CurrentHealth : 0;
        carriedStamina = stamina != null ? stamina.CurrentStamina : 0f;
        carriedShield = health != null && health.HasShield ? health.ShieldHealth : 0;
        carriedShieldSeconds = health != null ? health.ShieldSecondsRemaining : 0f;
        offerBlessingOnArrival = offerBlessing;
        awaitingArrival = true;
    }

    public bool RestoreArrival(Health health, PlayerStamina stamina, bool purchasingLevel)
    {
        if (health != null)
            health.SetMaximumHealth(Mathf.RoundToInt(ChapterBalance.PlayerBaseHealth(chapterIndex) * (1f + HealthBonus)), false);
        stamina?.ApplyChapterProfile(chapterIndex);
        stamina?.SetMaximumBonus(StaminaBonus);
        // Revisiting a map can reward stones again, but does not generate another blessing.
        bool showBlessing = awaitingArrival && offerBlessingOnArrival && firstLevelVisit && !purchasingLevel;
        if (awaitingArrival)
        {
            if (health != null)
                health.RestoreCurrentHealth(purchasingLevel ? health.MaxHealth : carriedHealth);
            stamina?.RestoreCurrentStamina(carriedStamina);
            if (carriedShield > 0 && health != null)
                health.ActivateShield(carriedShield, carriedShieldSeconds);
        }
        else if (health != null)
        {
            health.Heal(health.MaxHealth);
        }
        awaitingArrival = false;
        offerBlessingOnArrival = false;
        return showBlessing;
    }

    public BlessingReward ResolveBlessing(BlessingReward reward)
    {
        if (reward == BlessingReward.MaxHealth && HealthBlessings < MaxStatBlessings ||
            reward == BlessingReward.MaxStamina && StaminaBlessings < MaxStatBlessings ||
            reward == BlessingReward.AllDamage && DamageBlessings < MaxStatBlessings && Settings != null && DamageBonus < Settings.MaxDamageBonus ||
            reward == BlessingReward.RestoreHealth)
            return reward;
        if (reward == BlessingReward.CryHeal && HasRoom(ConsumableKind.CryHeal))
            return reward;
        if (reward == BlessingReward.CryShield && HasRoom(ConsumableKind.CryShield))
            return reward;
        return HasRoom(ConsumableKind.CryHeal) || HasRoom(ConsumableKind.CryShield)
            ? BlessingReward.ConsumableBundle : BlessingReward.RestoreHealth;
    }

    public int ScalePlayerDamage(int baseDamage)
    {
        if (baseDamage <= 0 || DamageBonus <= 0f)
            return baseDamage;
        // Integer damage rounds up; decimal avoids a float turning an exact 42 into 43.
        decimal scaled = Math.Ceiling(baseDamage * (1m + (decimal)DamageBonus));
        return (int)Math.Min(int.MaxValue, scaled);
    }

    public void ApplyBlessing(BlessingReward reward, Health health, PlayerStamina stamina)
    {
        reward = ResolveBlessing(reward);
        switch (reward)
        {
            case BlessingReward.MaxHealth:
                HealthBlessings++;
                health?.SetMaximumHealth(Mathf.RoundToInt(ChapterBalance.PlayerBaseHealth(chapterIndex) * (1f + HealthBonus)), true);
                break;
            case BlessingReward.MaxStamina:
                StaminaBlessings++;
                stamina?.SetMaximumBonus(StaminaBonus);
                break;
            case BlessingReward.AllDamage:
                DamageBlessings++;
                break;
            case BlessingReward.ConsumableBundle:
                if (HasRoom(ConsumableKind.CryHeal)) ChangeCount(ConsumableKind.CryHeal, 1);
                if (HasRoom(ConsumableKind.CryShield)) ChangeCount(ConsumableKind.CryShield, 1);
                break;
            case BlessingReward.CryHeal:
                TryAddConsumable(ConsumableKind.CryHeal);
                break;
            case BlessingReward.CryShield:
                TryAddConsumable(ConsumableKind.CryShield);
                break;
            case BlessingReward.RestoreHealth:
                health?.Heal(Mathf.RoundToInt(health.MaxHealth * Settings.HealFraction));
                break;
        }
        OnResourcesChanged?.Invoke();
    }

    private static long SaturatingAdd(long left, long right)
    {
        if (right <= 0)
            return left;

        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }
}
