using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GameplayPanelController : MonoBehaviour
{
    [SerializeField] private GameResourceSettings settings;
    [SerializeField] private GameObject storePanel;
    [SerializeField] private GameObject blessingPanel;
    [SerializeField] private UnityEngine.UI.Button closeStoreButton;
    [SerializeField] private UnityEngine.UI.Button[] purchaseButtons;
    [SerializeField] private TMP_Text[] storeCounts;
    [SerializeField] private TMP_Text[] storePrices;
    [SerializeField] private TMP_Text[] storeEffects;
    [SerializeField] private TMP_Text storeWallet;
    [SerializeField] private TMP_Text storeFeedback;
    [SerializeField] private GameObject[] storeCells;
    [SerializeField] private TMP_Text storeTitle;
    [SerializeField] private TMP_Text storeSubtitle;
    [SerializeField] private UnityEngine.UI.Button[] blessingButtons;
    [SerializeField] private TMP_Text[] blessingTitles;
    [SerializeField] private TMP_Text[] blessingDescriptions;
    [SerializeField] private UnityEngine.UI.Image[] blessingIcons;
    [SerializeField] private UnityEngine.UI.Image[] blessingSecondaryIcons;
    [Header("Pause and restart panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject restartPanel;
    [SerializeField] private UnityEngine.UI.Button settingsButton;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button settingsQuitButton;
    [SerializeField] private UnityEngine.UI.Button restartButton;
    [SerializeField] private UnityEngine.UI.Button restartQuitButton;
    [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValue;
    // Order follows SfxCategory: Ambience, Action, Combat.
    [SerializeField] private UnityEngine.UI.Slider[] sfxCategorySliders = Array.Empty<UnityEngine.UI.Slider>();
    [SerializeField] private TMP_Text[] sfxCategoryValues = Array.Empty<TMP_Text>();
    [SerializeField] private UnityEvent<int> onBlessingSelected = new UnityEvent<int>();

    private enum Panel { None, Store, Blessings, Settings, Restart }
    private Panel active;
    private static GameplayPanelController activePanel;
    private static int blockInputThroughFrame = -1;
    private Health health;
    private PlayerStamina stamina;
    private RunResources resources;
    private float previousTimeScale;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private bool subscribed;
    private UnityAction[] purchaseActions;
    private UnityAction[] blessingActions;
    private AudioSettingsBinding audioSettings;
    private BlessingReward[] offeredRewards;
    private BlessingArtwork[] authoredBlessingArtwork;
    private StoreCatalog storeCatalog;

    private struct BlessingArtwork
    {
        public Sprite primarySprite;
        public Sprite secondarySprite;
        public Vector2 position;
        public Vector2 size;
    }

    public bool IsOpen => active != Panel.None;
    public bool IsStoreOpen => active == Panel.Store;
    public bool IsBlessingOpen => active == Panel.Blessings;
    public static bool BlocksGameplayInput => activePanel != null && activePanel.IsOpen || Time.frameCount <= blockInputThroughFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activePanel = null;
        blockInputThroughFrame = -1;
    }

    private void Awake()
    {
        storePanel.SetActive(false);
        blessingPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (restartPanel != null) restartPanel.SetActive(false);
        purchaseActions = new UnityAction[purchaseButtons.Length];
        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            int index = i;
            purchaseActions[i] = () => Buy((ConsumableKind)index);
            purchaseButtons[i].onClick.AddListener(purchaseActions[i]);
        }
        blessingActions = new UnityAction[blessingButtons.Length];
        offeredRewards = new BlessingReward[blessingButtons.Length];
        authoredBlessingArtwork = new BlessingArtwork[blessingButtons.Length];
        for (int i = 0; i < blessingButtons.Length; i++)
        {
            // Keep artwork edited on the scene/prefab Image when displaying its configured reward.
            UnityEngine.UI.Image icon = blessingIcons[i];
            authoredBlessingArtwork[i] = new BlessingArtwork
            {
                primarySprite = icon.sprite,
                secondarySprite = blessingSecondaryIcons != null && i < blessingSecondaryIcons.Length && blessingSecondaryIcons[i] != null
                    ? blessingSecondaryIcons[i].sprite : null,
                position = icon.rectTransform.anchoredPosition,
                size = icon.rectTransform.sizeDelta
            };
            int index = i;
            blessingActions[i] = () => ChooseBlessing(index);
            blessingButtons[i].onClick.AddListener(blessingActions[i]);
        }
        closeStoreButton.onClick.AddListener(CloseStore);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (settingsQuitButton != null) settingsQuitButton.onClick.AddListener(ReturnToMenu);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (restartQuitButton != null) restartQuitButton.onClick.AddListener(QuitGame);
        audioSettings = new AudioSettingsBinding(musicVolumeSlider, musicVolumeValue, sfxCategorySliders, sfxCategoryValues);
    }

    public void BindPlayer(Health playerHealth, PlayerStamina playerStamina, RunResources runResources)
    {
        Unsubscribe();
        health = playerHealth;
        stamina = playerStamina;
        resources = runResources;
        Subscribe();
        RefreshStore();
    }

    private void OnEnable() => Subscribe();
    private void Subscribe()
    {
        if (resources == null || subscribed) return;
        resources.OnResourcesChanged += RefreshStore;
        subscribed = true;
    }
    private void Unsubscribe()
    {
        if (resources != null && subscribed) resources.OnResourcesChanged -= RefreshStore;
        subscribed = false;
    }

    private void OnDisable()
    {
        CloseActivePanel();
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (closeStoreButton != null) closeStoreButton.onClick.RemoveListener(CloseStore);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
        if (settingsQuitButton != null) settingsQuitButton.onClick.RemoveListener(ReturnToMenu);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartLevel);
        if (restartQuitButton != null) restartQuitButton.onClick.RemoveListener(QuitGame);
        audioSettings?.Dispose();
        if (purchaseActions != null)
            for (int i = 0; i < purchaseActions.Length; i++)
                if (purchaseButtons[i] != null) purchaseButtons[i].onClick.RemoveListener(purchaseActions[i]);
        if (blessingActions != null)
            for (int i = 0; i < blessingActions.Length; i++)
                if (blessingButtons[i] != null) blessingButtons[i].onClick.RemoveListener(blessingActions[i]);
    }

    private void Update()
    {
        if (EscapePressed())
        {
            if (active == Panel.Store) CloseStore();
            else if (active == Panel.Settings) ResumeGame();
            else if (active == Panel.None && health != null && !health.IsDead) OpenSettings();
        }

        if (active == Panel.Blessings)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ChooseBlessing(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ChooseBlessing(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ChooseBlessing(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) ChooseBlessing(3);
        }
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public void OpenStore() => OpenStore(StoreCatalog.Supplies);

    public void OpenStore(StoreCatalog catalog)
    {
        if (resources == null || IsOpen || health == null || health.IsDead) return;
        storeCatalog = catalog;
        if (storeTitle != null) storeTitle.text = catalog == StoreCatalog.SpecialItems ? "VẬT PHẨM ĐẶC BIỆT" : "TƯỢNG TIẾP TẾ";
        if (storeSubtitle != null) storeSubtitle.text = catalog == StoreCatalog.SpecialItems
            ? "Thánh giá tự hồi sinh tại chỗ khi hết HP"
            : "Đổi linh thạch đã cất giữ lấy CryHeal / CryShield";
        if (storeCells != null)
            for (int i = 0; i < storeCells.Length; i++)
                if (storeCells[i] != null) storeCells[i].SetActive(GameResourceSettings.IsSoldAt((ConsumableKind)i, catalog));
        Open(Panel.Store, storePanel);
        storeFeedback.text = catalog == StoreCatalog.SpecialItems
            ? "Hết thánh giá sẽ trở về Game Over. Quay lại màn cũ để farm linh thạch."
            : "Mua thêm vật phẩm trước khi đi tiếp";
        RefreshStore();
        FocusFirstAvailable(purchaseButtons);
    }

    public void OpenBlessings()
    {
        if (resources == null || IsOpen || health == null || health.IsDead) return;
        for (int i = 0; i < blessingButtons.Length; i++)
        {
            BlessingOption option = settings.Blessings[i];
            BlessingReward reward = resources.ResolveBlessing(option.reward);
            offeredRewards[i] = reward;
            blessingTitles[i].text = reward == option.reward ? option.title : RewardTitle(reward);
            string description = reward == option.reward ? option.description : RewardDescription(reward);
            if (reward == BlessingReward.ConsumableBundle && (!resources.HasRoom(ConsumableKind.CryHeal) || !resources.HasRoom(ConsumableKind.CryShield)))
                description = resources.HasRoom(ConsumableKind.CryHeal)
                    ? "Nhận 1 CryHeal.\nKho CryShield đã đầy." : "Nhận 1 CryShield.\nKho CryHeal đã đầy.";
            blessingDescriptions[i].text = FormatBlessingDescription(description);
            BlessingArtwork artwork = authoredBlessingArtwork[i];
            bool configuredReward = reward == option.reward;
            blessingIcons[i].sprite = configuredReward && artwork.primarySprite != null ? artwork.primarySprite :
                configuredReward && option.icon != null ? option.icon : RewardIcon(reward);
            bool bundle = reward == BlessingReward.ConsumableBundle;
            blessingIcons[i].rectTransform.anchoredPosition = configuredReward ? artwork.position : new Vector2(bundle ? 94f : 132f, 269f);
            blessingIcons[i].rectTransform.sizeDelta = configuredReward ? artwork.size :
                bundle ? new Vector2(48f, 88f) : new Vector2(76f, 96f);
            if (blessingSecondaryIcons != null && i < blessingSecondaryIcons.Length && blessingSecondaryIcons[i] != null)
            {
                blessingSecondaryIcons[i].sprite = configuredReward && artwork.secondarySprite != null ? artwork.secondarySprite : settings.Icon(ConsumableKind.CryShield);
                blessingSecondaryIcons[i].gameObject.SetActive(bundle);
            }
        }
        Open(Panel.Blessings, blessingPanel);
        FocusFirstAvailable(blessingButtons);
    }

    public void OpenSettings()
    {
        if (health == null || health.IsDead || active != Panel.None || settingsPanel == null) return;
        RefreshVolumeControls();
        Open(Panel.Settings, settingsPanel);
        FocusFirstAvailable(new[] { resumeButton, settingsQuitButton });
    }

    public void OpenRestartPanel()
    {
        if (restartPanel == null || active == Panel.Restart) return;
        if (IsOpen) CloseActivePanel();
        Open(Panel.Restart, restartPanel);
        FocusFirstAvailable(new[] { restartButton, restartQuitButton });
    }

    public void ResumeGame()
    {
        if (active != Panel.Settings) return;
        PlayerPrefs.Save();
        CloseActivePanel();
    }

    private void RefreshVolumeControls() => audioSettings?.Refresh();

    public void RestartLevel()
    {
        PlayerCheckpointRespawn.RestartLevel(CloseActivePanel);
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
        CloseActivePanel();
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ReturnToMenu()
    {
        GameSceneNavigation.Load(GameSceneNavigation.MenuScenePath, () =>
        {
            PlayerPrefs.Save();
            CloseActivePanel();
        });
    }

    private void Open(Panel panel, GameObject view)
    {
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        active = panel;
        activePanel = this;
        health?.GetComponent<PlayerController>()?.SuspendForPanel();
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        view.SetActive(true);
        GameAudioManager.PlayCue(panel switch
        {
            Panel.Store => GameAudioCue.StoreOpen,
            Panel.Blessings => GameAudioCue.BlessingPanelOpen,
            Panel.Settings => GameAudioCue.SettingsOpen,
            Panel.Restart => GameAudioCue.RestartPanelOpen,
            _ => GameAudioCue.UiButtonClick
        });
    }

    public void CloseStore()
    {
        if (active == Panel.Store) CloseActivePanel();
    }

    private void CloseActivePanel()
    {
        if (!IsOpen) return;
        Panel closingPanel = active;
        active = Panel.None;
        if (storePanel != null) storePanel.SetActive(false);
        if (blessingPanel != null) blessingPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (restartPanel != null) restartPanel.SetActive(false);
        Time.timeScale = previousTimeScale;
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLock;
        if (activePanel == this) activePanel = null;
        // A number key selecting a blessing must not also consume an item that frame.
        blockInputThroughFrame = Time.frameCount;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (closingPanel == Panel.Store) GameAudioManager.PlayCue(GameAudioCue.StoreClose);
        else if (closingPanel == Panel.Settings) GameAudioManager.PlayCue(GameAudioCue.SettingsClose);
    }

    public void Buy(ConsumableKind kind)
    {
        if (active != Panel.Store || resources == null || !GameResourceSettings.IsSoldAt(kind, storeCatalog)) return;
        PurchaseResult result = resources.TryPurchase(kind);
        storeFeedback.text = result == PurchaseResult.Success ? "Đã mua " + GameResourceSettings.ItemName(kind) :
            result == PurchaseResult.InventoryFull ? "Kho đã đầy: tối đa " + settings.Capacity(kind) + " vật phẩm loại này" : "Không đủ linh thạch đã cất giữ";
        RefreshStore();
    }

    public void ChooseBlessing(int index)
    {
        if (active != Panel.Blessings || index < 0 || index >= offeredRewards.Length) return;
        resources.ApplyBlessing(offeredRewards[index], health, stamina);
        GameAudioManager.PlayCue(GameAudioCue.BlessingReceived);
        CloseActivePanel();
        onBlessingSelected.Invoke(index);
    }

    private void RefreshStore()
    {
        if (resources == null || settings == null) return;
        storeWallet.text = resources.BankedSpiritStone.ToString();
        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            ConsumableKind kind = (ConsumableKind)i;
            int price = settings.Price(kind);
            bool hasRoom = resources.HasRoom(kind);
            storeCounts[i].text = "Đang có " + resources.Count(kind) + " / " + settings.Capacity(kind);
            storePrices[i].text = price + " linh thạch";
            if (storeEffects != null && i < storeEffects.Length && storeEffects[i] != null)
                storeEffects[i].text = kind switch
                {
                    ConsumableKind.CryHeal => "Hồi " + Mathf.RoundToInt(settings.HealFraction * 100f) + "% HP tối đa",
                    ConsumableKind.CryShield => "Khiên " + Mathf.RoundToInt(settings.ShieldFraction * 100f) + "% HP · " + settings.ShieldDuration.ToString("0.#") + " giây",
                    ConsumableKind.ReviveCross => "Hồi sinh " + Mathf.RoundToInt(settings.ReviveHealthFraction * 100f) + "% HP\nBất tử " + settings.ReviveInvulnerability.ToString("0.#") + " giây",
                    _ => ""
                };
            purchaseButtons[i].interactable = GameResourceSettings.IsSoldAt(kind, storeCatalog) && hasRoom && resources.BankedSpiritStone >= price;
            TMP_Text label = purchaseButtons[i].GetComponentInChildren<TMP_Text>(true);
            label.text = !hasRoom ? "KHO ĐÃ ĐẦY" : resources.BankedSpiritStone < price ? "CHƯA ĐỦ ĐÁ" : "MUA";
        }
    }

    private static void FocusFirstAvailable(UnityEngine.UI.Button[] buttons)
    {
        if (EventSystem.current == null) return;
        foreach (var button in buttons)
            if (button != null && button.gameObject.activeInHierarchy && button.interactable) { EventSystem.current.SetSelectedGameObject(button.gameObject); break; }
    }

    private string FormatBlessingDescription(string description) => (description ?? "")
        .Replace("{damagePercent}", (Mathf.Min(settings.DamageBonusPerBlessing, settings.MaxDamageBonus - resources.DamageBonus) * 100f).ToString("0.#"))
        .Replace("{damageCap}", (settings.MaxDamageBonus * 100f).ToString("0.#"));

    private Sprite RewardIcon(BlessingReward reward) => reward == BlessingReward.AllDamage ? settings.DamageBlessingIcon :
        reward == BlessingReward.StaminaRecovery ? settings.SpiritStoneIcon :
        settings.Icon(reward == BlessingReward.CryShield ? ConsumableKind.CryShield : ConsumableKind.CryHeal);
    private static string RewardTitle(BlessingReward reward) => reward == BlessingReward.ConsumableBundle ? "Vật phẩm" :
        reward == BlessingReward.AllDamage ? "Sức mạnh" : reward == BlessingReward.CryShield ? "Đá khiên" : reward == BlessingReward.RestoreHealth ? "Hồi máu" : "Đá hồi máu";
    private static string RewardDescription(BlessingReward reward) => reward == BlessingReward.CryShield ? "Nhận 1 CryShield.\nNhấn 2 để bật khiên." :
        reward == BlessingReward.ConsumableBundle ? "Nhận 1 CryHeal\nvà 1 CryShield.\nNhấn 1 / 2 để dùng." :
        reward == BlessingReward.AllDamage ? "+{damagePercent}% sát thương mọi đòn đánh.\nTối đa +{damageCap}%." :
        reward == BlessingReward.RestoreHealth ? "Kho vật phẩm đã đầy.\nHồi ngay 30% HP tối đa." : "Nhận 1 CryHeal.\nNhấn 1 để hồi máu.";
}
