using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class TreasureChestInteractable : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private Animator animator;
    [Header("Loot")]
    [SerializeField, Min(0)] private int cryHealAmount = 1;
    [SerializeField, Min(0)] private int cryShieldAmount = 1;
    [SerializeField, Min(0f), Tooltip("Delay until the opening animation has finished.")]
    private float openingDuration = 0.6f;
    [Header("Interaction prompt")]
    [SerializeField] private TMP_Text interactionPrompt;
    [SerializeField, Min(0.1f)] private float promptRange = 1.35f;

    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private bool opened;
    private bool opening;
    private int remainingCryHeal;
    private int remainingCryShield;
    private float giveLootAt;
    private float findPlayerAt;
    private Transform recipient;
    private Health nearbyPlayer;
    private Collider2D interactionCollider;

    public bool IsOpened => opened;
    public int RemainingCryHeal => remainingCryHeal;
    public int RemainingCryShield => remainingCryShield;

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        interactionCollider = GetComponent<Collider2D>();
        remainingCryHeal = Mathf.Max(0, cryHealAmount);
        remainingCryShield = Mathf.Max(0, cryShieldAmount);
        if (interactionPrompt != null) interactionPrompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (opening && Time.time >= giveLootAt)
        {
            opening = false;
            if (IsLivingPlayer(recipient)) CollectRemainingLoot();
            recipient = null;
        }
        RefreshPrompt();
    }

    private void OnDisable()
    {
        if (interactionPrompt != null) interactionPrompt.gameObject.SetActive(false);
    }

    public bool CanInteract(Transform player)
    {
        return !opening && HasRemainingLoot && CanReceiveLoot && IsLivingPlayer(player) &&
               Time.timeScale > 0f && !GameplayPanelController.BlocksGameplayInput;
    }

    public void Interact(Transform player)
    {
        if (!CanInteract(player) || animator == null)
            return;

        if (opened)
        {
            CollectRemainingLoot();
            return;
        }
        opened = true;
        opening = true;
        GameAudioManager.PlayCue(GameAudioCue.ChestOpen);
        recipient = player;
        giveLootAt = Time.time + openingDuration;
        animator.ResetTrigger(OpenTrigger);
        animator.SetTrigger(OpenTrigger);
    }

    private bool HasRemainingLoot => remainingCryHeal > 0 || remainingCryShield > 0;
    private bool CanReceiveLoot => RunResources.Instance != null &&
        (remainingCryHeal > 0 && RunResources.Instance.HasRoom(ConsumableKind.CryHeal) ||
         remainingCryShield > 0 && RunResources.Instance.HasRoom(ConsumableKind.CryShield));

    private static bool IsLivingPlayer(Transform player) => player != null &&
        player.TryGetComponent(out Health health) && !health.IsDead;

    private void CollectRemainingLoot()
    {
        RunResources resources = RunResources.Instance;
        if (resources == null) return;
        while (remainingCryHeal > 0 && resources.TryAddConsumable(ConsumableKind.CryHeal)) remainingCryHeal--;
        while (remainingCryShield > 0 && resources.TryAddConsumable(ConsumableKind.CryShield)) remainingCryShield--;
    }

    private void RefreshPrompt()
    {
        if (interactionPrompt == null) return;
        if (nearbyPlayer == null && Time.unscaledTime >= findPlayerAt)
        {
            findPlayerAt = Time.unscaledTime + 0.5f;
            nearbyPlayer = FindFirstObjectByType<Health>();
        }
        bool show = nearbyPlayer != null && !nearbyPlayer.IsDead && !opening && HasRemainingLoot &&
                    Time.timeScale > 0f && !GameplayPanelController.BlocksGameplayInput;
        if (show)
        {
            Vector2 closestPoint = interactionCollider != null ? interactionCollider.ClosestPoint(nearbyPlayer.transform.position) : (Vector2)transform.position;
            show = (closestPoint - (Vector2)nearbyPlayer.transform.position).sqrMagnitude <= promptRange * promptRange;
        }
        if (interactionPrompt.gameObject.activeSelf != show) interactionPrompt.gameObject.SetActive(show);
        if (!show) return;
        string message = !CanReceiveLoot ? "Kho đá đã đầy" : opened ? "F - Nhận đá còn lại" : "F - Mở rương";
        if (interactionPrompt.text != message) interactionPrompt.text = message;
        interactionPrompt.color = CanReceiveLoot ? new Color(1f, 0.89f, 0.6f) : new Color(1f, 0.55f, 0.5f);
    }
}
