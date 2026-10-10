using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class KeyPickup : MonoBehaviour, IPlayerPickup
{
    [Header("Floating")]
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0.1f)] private float bobFrequency = 2f;

    private CircleCollider2D pickupCollider;
    private Vector3 floatOrigin;
    private float floatPhase;

    private void Awake()
    {
        pickupCollider = GetComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        floatOrigin = transform.position;
        floatPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        if (RunResources.Instance != null &&
            (RunResources.Instance.HasUsedLevelKey(gameObject.scene.path) || RunResources.Instance.HasHeldLevelKey(gameObject.scene.path)))
        {
            pickupCollider.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Vector3 position = floatOrigin;
        position.y += Mathf.Sin(Time.time * bobFrequency + floatPhase) * bobHeight;
        transform.position = position;
    }

    public bool CanPickup(Transform player)
    {
        if (player == null || RunResources.Instance != null && RunResources.Instance.HasUsedLevelKey(gameObject.scene.path))
            return false;

        PlayerKeyInventory inventory = player.GetComponent<PlayerKeyInventory>();
        return inventory != null && !inventory.HasKey;
    }

    public void PickUp(Transform player)
    {
        if (player == null)
            return;

        PlayerKeyInventory inventory = player.GetComponent<PlayerKeyInventory>();
        if (inventory != null && inventory.TryGiveKey())
        {
            GameAudioManager.PlayCue(GameAudioCue.KeyPickup);
            Destroy(gameObject);
        }
    }
}
