using UnityEngine;

public interface IPlayerInteractable
{
    bool CanInteract(Transform player);
    void Interact(Transform player);
}

public interface IPlayerPickup
{
    bool CanPickup(Transform player);
    void PickUp(Transform player);
}

[DisallowMultipleComponent]
public sealed class PlayerInteraction : MonoBehaviour
{
    private const int OverlapBufferCapacity = 32;

    [SerializeField, Min(0.1f)] private float interactionRange = 1.35f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    private readonly Collider2D[] overlapBuffer = new Collider2D[OverlapBufferCapacity];
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true
        };
        contactFilter.SetLayerMask(interactionLayers);
    }

    public bool TryInteract(bool interactPressed)
    {
        if (!interactPressed)
            return false;

        int count = Physics2D.OverlapCircle(transform.position, interactionRange, contactFilter, overlapBuffer);
        IPlayerInteractable nearest = null;
        float nearestDistanceSquared = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider2D candidateCollider = overlapBuffer[i];
            if (candidateCollider == null)
                continue;

            MonoBehaviour[] components = candidateCollider.GetComponentsInParent<MonoBehaviour>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                if (components[componentIndex] is not IPlayerInteractable interactable ||
                    !interactable.CanInteract(transform))
                    continue;

                Vector2 closestPoint = candidateCollider.ClosestPoint(transform.position);
                float distanceSquared = ((Vector2)transform.position - closestPoint).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearest = interactable;
            }
        }

        if (nearest == null)
            return false;

        nearest.Interact(transform);
        return true;
    }

    public bool TryPickup(bool pickupPressed)
    {
        if (!pickupPressed)
            return false;

        int count = Physics2D.OverlapCircle(transform.position, interactionRange, contactFilter, overlapBuffer);
        IPlayerPickup nearest = null;
        float nearestDistanceSquared = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider2D candidateCollider = overlapBuffer[i];
            if (candidateCollider == null)
                continue;

            MonoBehaviour[] components = candidateCollider.GetComponentsInParent<MonoBehaviour>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                if (components[componentIndex] is not IPlayerPickup pickup ||
                    !pickup.CanPickup(transform))
                    continue;

                Vector2 closestPoint = candidateCollider.ClosestPoint(transform.position);
                float distanceSquared = ((Vector2)transform.position - closestPoint).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearest = pickup;
            }
        }

        if (nearest == null)
            return false;

        nearest.PickUp(transform);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
