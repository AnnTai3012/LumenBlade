using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerKeyInventory : MonoBehaviour
{
    public bool HasKey { get; private set; }

    // A HUD can subscribe later without owning the key state itself.
    public event Action<bool> OnKeyChanged;

    private void Start()
    {
        HasKey = RunResources.Instance != null && RunResources.Instance.HasHeldLevelKey(gameObject.scene.path);
        OnKeyChanged?.Invoke(HasKey);
    }

    public bool TryGiveKey()
    {
        if (HasKey || RunResources.Instance != null && RunResources.Instance.HasUsedLevelKey(gameObject.scene.path))
            return false;

        HasKey = true;
        RunResources.Instance?.SetHeldLevelKey(gameObject.scene.path, true);
        OnKeyChanged?.Invoke(true);
        return true;
    }

    public bool TryConsumeKey()
    {
        if (!HasKey)
            return false;

        HasKey = false;
        RunResources.Instance?.SetHeldLevelKey(gameObject.scene.path, false);
        OnKeyChanged?.Invoke(false);
        return true;
    }
}
