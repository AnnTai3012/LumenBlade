using UnityEngine;

// Compatibility bridge for legacy attack Animation Events.
public class WeaponHitbox : MonoBehaviour
{
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    // The combat controller guards this against its timer-driven impact path.
    public void EnableHitbox()
    {
        playerController?.EnableWeaponHitbox();
    }

    public void DisableHitbox()
    {
        playerController?.DisableWeaponHitbox();
    }
}
