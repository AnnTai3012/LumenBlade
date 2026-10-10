using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LevelDoorDirection
{
    Forward,
    Return
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class LevelExitDoor : MonoBehaviour, IPlayerInteractable
{
    [Header("Door Direction")]
    [Tooltip("Forward: door to the next map. Return: door back to the previous map.")]
    [SerializeField] private LevelDoorDirection direction = LevelDoorDirection.Forward;
    [SerializeField] private string targetSceneName = "Level 2";
    [SerializeField] private bool requireKey = true;
    [SerializeField] private bool offerBlessingOnArrival = true;
    [SerializeField] private string openStateName = "Open";
    [SerializeField, Min(0f)] private float openAnimationDuration = 0.9f;

    [Header("Travel")]
    [SerializeField] private string doorId = "Exit";
    [SerializeField] private string targetDoorId = "Entrance";
    [SerializeField] private bool startOpen;
    [SerializeField] private bool enterOnTouch = true;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Vector2 arrivalOffset = new Vector2(-1.2f, 0f);
    [SerializeField, Tooltip("Restarting in the target scene returns to this door's arrival position.")]
    private bool returnHereOnTargetRestart;
    [SerializeField, Min(0.1f), Tooltip("Move farther away from the arrival door before it can automatically activate again.")]
    private float arrivalRearmClearance = 0.5f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D doorCollider;
    private bool isOpening;
    private bool isOpen;
    private Transform departingPlayer;
    private Collider2D arrivingPlayerCollider;
    private Collider2D touchingPlayerCollider;
    private float requiredArrivalClearance;
    private float synchronizedOpenDuration;
    private float animatorSpeedBeforeOpening;
    private bool hasSynchronizedOpeningSpeed;

    public LevelDoorDirection Direction => direction;
    public string DoorId => doorId;
    public string TargetSceneName => targetSceneName;
    public string TargetDoorId => targetDoorId;
    public Vector3 ArrivalPosition => transform.TransformPoint(arrivalOffset);
    public bool IsOpen => isOpen;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<BoxCollider2D>();
        doorCollider.isTrigger = true;
    }

    private void Start()
    {
        if (startOpen || RunResources.Instance != null && RunResources.Instance.HasUsedLevelKey(gameObject.scene.path))
            ShowOpenFrame();
    }

    private void Update()
    {
        if (arrivingPlayerCollider != null)
        {
            if (!arrivingPlayerCollider.enabled || !arrivingPlayerCollider.gameObject.activeInHierarchy)
                arrivingPlayerCollider = null;
            else
            {
                ColliderDistance2D distance = doorCollider.Distance(arrivingPlayerCollider);
                if (distance.isValid && !distance.isOverlapped && distance.distance >= requiredArrivalClearance)
                    arrivingPlayerCollider = null;
            }
        }

        if (touchingPlayerCollider == null)
            return;
        if (!IsInsideDoor(touchingPlayerCollider))
        {
            touchingPlayerCollider = null;
            return;
        }

        // Sleeping bodies stop sending trigger-stay callbacks. Recheck key/pause state
        // while the player's body remains inside, without accepting weapon hitboxes.
        TryEnterOnTouch(touchingPlayerCollider);
    }

    private bool IsInsideDoor(Collider2D playerCollider) =>
        doorCollider.enabled && playerCollider.enabled && playerCollider.gameObject.activeInHierarchy &&
        doorCollider.Distance(playerCollider).isOverlapped;

    public bool CanInteract(Transform player)
    {
        if (isOpening || player == null || Time.timeScale <= 0f || !Application.CanStreamedLevelBeLoaded(targetSceneName))
            return false;

        Health health = player.GetComponent<Health>();
        if (health == null || health.IsDead)
            return false;

        PlayerKeyInventory inventory = player.GetComponent<PlayerKeyInventory>();
        return !NeedsKey() || inventory != null && inventory.HasKey;
    }

    public void Interact(Transform player)
    {
        if (!CanInteract(player))
            return;

        departingPlayer = player;
        isOpening = true;
        synchronizedOpenDuration = Mathf.Max(openAnimationDuration, GameAudioManager.GetCueDuration(GameAudioCue.ExitGateOpen));
        if (!isOpen)
            GameAudioManager.PlayCue(GameAudioCue.ExitGateOpen);
        if (!isOpen && HasOpenAnimation())
        {
            animator.Play(openStateName, 0, 0f);
            animator.Update(0f);
            float duration = animator.GetCurrentAnimatorStateInfo(0).length;
            if (duration > 0f && synchronizedOpenDuration > 0f)
            {
                animatorSpeedBeforeOpening = animator.speed;
                hasSynchronizedOpeningSpeed = true;
                animator.speed = duration / synchronizedOpenDuration;
            }
        }

        StartCoroutine(LoadTargetAfterDoorOpens());
    }

    private void OnTriggerEnter2D(Collider2D other) => TryEnterOnTouch(other);
    private void OnTriggerStay2D(Collider2D other) => TryEnterOnTouch(other);

    private void TryEnterOnTouch(Collider2D other)
    {
        if (!isActiveAndEnabled || other == null)
            return;
        Health playerHealth = other.GetComponentInParent<Health>();
        if (playerHealth == null || other != playerHealth.GetComponent<Collider2D>() ||
            playerHealth.GetComponent<PlayerKeyInventory>() == null)
            return;

        touchingPlayerCollider = other;
        if (!enterOnTouch || isOpening ||
            arrivingPlayerCollider != null && arrivingPlayerCollider.transform == playerHealth.transform)
            return;
        Interact(playerHealth.transform);
    }

    public void PlaceArrivingPlayer(Transform player)
    {
        if (player == null)
            return;
        Vector3 position = ArrivalPosition;
        player.position = new Vector3(position.x, position.y, player.position.z);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        Physics2D.SyncTransforms();
        arrivingPlayerCollider = player.GetComponent<Collider2D>();
        if (arrivingPlayerCollider != null)
        {
            ColliderDistance2D distance = doorCollider.Distance(arrivingPlayerCollider);
            requiredArrivalClearance = Mathf.Max(0f, distance.distance) + Mathf.Max(0.1f, arrivalRearmClearance);
        }
    }

    private bool NeedsKey() => requireKey &&
        (RunResources.Instance == null || !RunResources.Instance.HasUsedLevelKey(gameObject.scene.path));

    private IEnumerator LoadTargetAfterDoorOpens()
    {
        if (!isOpen && HasOpenAnimation())
        {
            int openStateHash = Animator.StringToHash($"Base Layer.{openStateName}");
            float elapsed = 0f;
            float timeout = Mathf.Max(synchronizedOpenDuration + 1f, 1.5f);
            while (elapsed < timeout)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == openStateHash && state.normalizedTime >= 1f)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else if (!isOpen && synchronizedOpenDuration > 0f)
        {
            yield return new WaitForSeconds(synchronizedOpenDuration);
        }

        // Defer scene loading out of the physics callback, including already-open doors.
        yield return null;
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            CancelOpening();
            Debug.LogError($"Level exit '{name}' cannot load scene '{targetSceneName}'. Add it to Build Settings.", this);
            yield break;
        }

        Health departingHealth = departingPlayer != null ? departingPlayer.GetComponent<Health>() : null;
        if (departingPlayer == null || departingHealth == null || departingHealth.IsDead)
        {
            CancelOpening();
            yield break;
        }

        RunResources resources = RunResources.Instance;
        PlayerKeyInventory inventory = departingPlayer.GetComponent<PlayerKeyInventory>();
        if (NeedsKey() && (inventory == null || !inventory.HasKey))
        {
            CancelOpening();
            yield break;
        }
        if (resources != null && !resources.CompleteLevel(gameObject.scene.path))
        {
            CancelOpening();
            Debug.LogError($"Level exit '{name}' could not bank resources for scene '{gameObject.scene.path}'.", this);
            yield break;
        }

        // Commit the key only after the target, living player and resource owner are valid.
        if (NeedsKey())
        {
            inventory.TryConsumeKey();
            resources?.MarkLevelKeyUsed(gameObject.scene.path);
        }

        if (resources != null)
        {
            string targetScenePath = ResolveTargetScenePath();
            resources.SetArrivalDoor(targetScenePath, targetDoorId);
            if (returnHereOnTargetRestart)
                resources.SetRestartCheckpoint(targetScenePath, gameObject.scene.path, doorId);
            resources.PrepareArrival(departingHealth, departingPlayer.GetComponent<PlayerStamina>(), offerBlessingOnArrival);
        }
        GameAudioManager.PlayCue(GameAudioCue.LevelTransition);
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }

    private string ResolveTargetScenePath()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path == targetSceneName || Path.GetFileNameWithoutExtension(path) == targetSceneName)
                return path;
        }
        return targetSceneName;
    }

    private void CancelOpening()
    {
        RestoreOpeningSpeed();
        isOpening = false;
        departingPlayer = null;
    }

    private void RestoreOpeningSpeed()
    {
        if (!hasSynchronizedOpeningSpeed || animator == null)
            return;
        animator.speed = animatorSpeedBeforeOpening;
        hasSynchronizedOpeningSpeed = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        CancelOpening();
    }

    private void ShowOpenFrame()
    {
        isOpen = true;
        if (openSprite != null && spriteRenderer != null)
        {
            animator.enabled = false;
            spriteRenderer.sprite = openSprite;
        }
        else if (HasOpenAnimation())
        {
            animator.Play(openStateName, 0, 1f);
            animator.Update(0f);
            animator.enabled = false;
        }
    }

    private bool HasOpenAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || string.IsNullOrWhiteSpace(openStateName))
            return false;

        return animator.HasState(0, Animator.StringToHash($"Base Layer.{openStateName}"));
    }
}
