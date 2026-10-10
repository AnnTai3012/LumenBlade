using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelResourceFlow : MonoBehaviour
{
    [SerializeField] private GameResourceSettings settings;
    [SerializeField] private GameplayPanelController panels;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private bool purchasingLevel;
    [SerializeField, Min(1)] private int chapterIndex = 1;

    [Header("Restart Checkpoint")]
    [SerializeField, Tooltip("Optional fallback checkpoint when starting this scene directly in the Editor.")]
    private string restartScenePath;
    [SerializeField] private string restartDoorId = "Exit";

    private RunResources resources;
    public bool IsPurchasingLevel => purchasingLevel;
    public int ChapterIndex => chapterIndex;

    private void Awake()
    {
        resources = RunResources.GetOrCreate(settings);
        resources.EnterLevel(gameObject.scene.path, chapterIndex);
        if (!string.IsNullOrEmpty(restartScenePath) &&
            !resources.TryGetRestartCheckpoint(gameObject.scene.path, out _, out _))
            resources.SetRestartCheckpoint(gameObject.scene.path, restartScenePath, restartDoorId);
    }

    private void Start()
    {
        if (playerHealth != null && resources.TryGetArrivalDoor(gameObject.scene.path, out string doorId))
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                bool placed = false;
                foreach (LevelExitDoor door in root.GetComponentsInChildren<LevelExitDoor>(true))
                {
                    if (door.DoorId != doorId)
                        continue;
                    door.PlaceArrivingPlayer(playerHealth.transform);
                    placed = true;
                    break;
                }
                if (placed)
                    break;
            }
        }
        bool offerBlessing = resources.RestoreArrival(playerHealth, playerStamina, purchasingLevel);
        panels.BindPlayer(playerHealth, playerStamina, resources);
        if (offerBlessing)
            panels.OpenBlessings();
    }
}
