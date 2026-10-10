using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class StoreStatueInteractable : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private GameplayPanelController panels;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private StoreCatalog catalog = StoreCatalog.Supplies;
    private BoxCollider2D interactionCollider;
    public StoreCatalog Catalog => catalog;

    private void Awake()
    {
        interactionCollider = GetComponent<BoxCollider2D>();
        interactionCollider.isTrigger = true;
        if (panels == null)
            panels = FindFirstObjectByType<GameplayPanelController>();
        if (player == null)
            player = FindFirstObjectByType<Health>()?.transform;
        if (interactionPrompt != null)
        {
            // Authored statues may face backwards or use different scales; keep their UI readable.
            Transform prompt = interactionPrompt.transform;
            Vector3 parentScale = prompt.parent != null ? prompt.parent.lossyScale : Vector3.one;
            prompt.rotation = Quaternion.identity;
            prompt.localScale = new Vector3(0.007f / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                0.007f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
                0.007f / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
        }
    }

    private void Update()
    {
        if (interactionPrompt == null || player == null) return;
        bool show = panels != null && !panels.IsOpen &&
                    (interactionCollider.ClosestPoint(player.position) - (Vector2)player.position).sqrMagnitude <= 1.35f * 1.35f;
        if (interactionPrompt.activeSelf != show) interactionPrompt.SetActive(show);
    }

    public bool CanInteract(Transform player)
    {
        return player != null && panels != null && !panels.IsOpen &&
               player.TryGetComponent(out Health health) && !health.IsDead;
    }

    public void Interact(Transform player)
    {
        if (CanInteract(player)) panels.OpenStore(catalog);
    }
}
