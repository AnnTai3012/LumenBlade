using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class KeyDropOnDeath : MonoBehaviour
{
    [Header("Key Spawn")]
    [SerializeField] private bool dropKeyOnDeath;
    [SerializeField] private KeyPickup keyPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.45f);

    private MonsterHealth health;
    private bool hasDroppedKey;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<MonsterHealth>();

        if (health != null)
            health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (!dropKeyOnDeath || hasDroppedKey || RunResources.Instance != null &&
            (RunResources.Instance.HasUsedLevelKey(gameObject.scene.path) || RunResources.Instance.HasHeldLevelKey(gameObject.scene.path)))
            return;

        if (keyPrefab == null)
        {
            Debug.LogError($"{name} is configured to drop a key but has no Key Pickup prefab assigned.", this);
            return;
        }

        hasDroppedKey = true;
        Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;
        Instantiate(keyPrefab, spawnPosition, Quaternion.identity);
    }
}
