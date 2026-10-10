using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class ConsumablePickup : MonoBehaviour, IPlayerPickup
{
    [SerializeField] private ConsumableKind kind;
    [SerializeField, Min(0f)] private float bobHeight = 0.08f;
    private Vector3 origin;

    private void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
        origin = transform.position;
    }

    private void Update()
    {
        transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
    }

    public bool CanPickup(Transform player)
    {
        return player != null && player.GetComponent<PlayerConsumables>() != null &&
               RunResources.Instance != null && RunResources.Instance.HasRoom(kind);
    }

    public void PickUp(Transform player)
    {
        if (CanPickup(player) && RunResources.Instance.TryAddConsumable(kind))
            Destroy(gameObject);
    }
}
