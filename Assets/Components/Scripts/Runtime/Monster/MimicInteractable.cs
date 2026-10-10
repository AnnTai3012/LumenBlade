using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(MonsterController))]
public sealed class MimicInteractable : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private Animator animator;
    [SerializeField] private MonsterController monsterController;
    [SerializeField] private Hurtbox2D hurtbox;
    [SerializeField] private HealthBarUI healthBar;
    [SerializeField, Range(0.5f, 1f)] private float transformActivationTime = 0.96f;

    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private static readonly int TransformState = Animator.StringToHash("Base Layer.Transform");
    private static readonly int Attack1State = Animator.StringToHash("Base Layer.Attack1");

    private Transform interactingPlayer;
    private bool interactionStarted;
    private bool transformed;
    public bool IsTransforming => interactionStarted && !transformed;

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        monsterController ??= GetComponent<MonsterController>();
        hurtbox ??= GetComponent<Hurtbox2D>();
        healthBar ??= GetComponent<HealthBarUI>();
        if (hurtbox != null)
            hurtbox.enabled = false;
        if (healthBar != null)
            healthBar.enabled = false;
    }

    private void Update()
    {
        if (!interactionStarted || transformed || animator == null)
            return;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        bool transformFinished = current.fullPathHash == TransformState &&
                                 current.normalizedTime >= transformActivationTime;
        bool initialAttackStarted = current.fullPathHash == Attack1State;
        if (!transformFinished && !initialAttackStarted)
            return;

        transformed = true;
        if (hurtbox != null)
            hurtbox.enabled = true;
        if (healthBar != null)
            healthBar.enabled = true;

        monsterController?.ActivateFromMimic(interactingPlayer);
    }

    public bool CanInteract(Transform player)
    {
        return !interactionStarted && player != null;
    }

    public void Interact(Transform player)
    {
        if (!CanInteract(player) || animator == null)
            return;

        GetComponent<MonsterVisibilityCulling>()?.WakeForActivity();
        interactingPlayer = player;
        interactionStarted = true;
        animator.ResetTrigger(OpenTrigger);
        animator.SetTrigger(OpenTrigger);
    }
}
