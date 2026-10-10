using UnityEngine;

// Visual culling and simulation sleep have separate boundaries so enemies wake before entering view.
[DisallowMultipleComponent]
public sealed class MonsterVisibilityCulling : MonoBehaviour
{
    [SerializeField] private bool cullOutsidePlayerView = true;
    [SerializeField] private bool suspendWhenFarAway = true;
    [SerializeField, Min(0f)] private float simulationPadding = 3f;

    private MonsterHealth health;
    private MonsterMotor2D motor;
    private MonsterController controller;
    private MonsterCombat combat;
    private BossEncounter encounter;
    private MimicInteractable mimic;
    private HealthBarUI healthBar;
    private Rigidbody2D body;
    private Renderer[] renderers;
    private bool[] previousRenderingOff;
    private Animator[] animators;
    private bool[] suspendedAnimators;
    private bool[] previousKeepAnimatorState;
    private bool suspendedBody;
    private Vector2 savedVelocity;
    private float savedAngularVelocity;
    private float keepAwakeUntil;
    private int groundLayers;

    public bool IsVisible { get; private set; } = true;
    public bool IsSleeping { get; private set; }

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        motor = GetComponent<MonsterMotor2D>();
        controller = GetComponent<MonsterController>();
        combat = GetComponent<MonsterCombat>();
        encounter = GetComponent<BossEncounter>();
        mimic = GetComponent<MimicInteractable>();
        healthBar = GetComponent<HealthBarUI>();
        body = GetComponent<Rigidbody2D>();
        groundLayers = LayerMask.GetMask("Ground");
        renderers = GetComponentsInChildren<Renderer>(true);
        previousRenderingOff = new bool[renderers.Length];
        animators = GetComponentsInChildren<Animator>(true);
        suspendedAnimators = new bool[animators.Length];
        previousKeepAnimatorState = new bool[animators.Length];
    }

    private void OnEnable()
    {
        keepAwakeUntil = Time.time + 0.5f;
        MonsterVisibilitySystem.Register(this);
    }

    private void OnDisable()
    {
        MonsterVisibilitySystem.Unregister(this);
        SetSleeping(false);
        SetVisible(true);
    }

    // Called before health events so damage/death reactions still own physics and animation.
    public void WakeForActivity()
    {
        keepAwakeUntil = Time.time + 1f;
        SetSleeping(false);
    }

    internal void RefreshVisibility(Plane[] viewPlanes, Bounds? orthographicView)
    {
        if (viewPlanes == null || !cullOutsidePlayerView)
        {
            SetSleeping(false);
            SetVisible(true);
            return;
        }

        Bounds bounds = GetVisualBounds();
        bool visible = IntersectsView(viewPlanes, orthographicView, bounds);
        SetVisible(visible);

        // A wider wake boundary and a further sleep boundary prevent edge oscillation.
        bounds.Expand(2f * (simulationPadding + (IsSleeping ? 0f : 1f)));
        bool nearView = visible || IntersectsView(viewPlanes, orthographicView, bounds);
        SetSleeping(!nearView && CanSuspend());
    }

    private static bool IntersectsView(Plane[] planes, Bounds? orthographicView, Bounds bounds)
    {
        return orthographicView.HasValue
            ? orthographicView.Value.Intersects(bounds)
            : GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    private Bounds GetVisualBounds()
    {
        Bounds result = new Bounds(transform.position, Vector3.one * 0.1f);
        bool foundRenderer = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer visual = renderers[i];
            if (visual == null || !visual.enabled || !visual.gameObject.activeInHierarchy)
                continue;

            if (!foundRenderer)
                result = visual.bounds;
            else
                result.Encapsulate(visual.bounds);
            foundRenderer = true;
        }
        return result;
    }

    private bool CanSuspend()
    {
        // Boss mechanics and corpse falls continue even when their artwork is out of view.
        if (!suspendWhenFarAway || health == null || health.IsDead || encounter != null ||
            Time.time < keepAwakeUntil || (combat != null && combat.IsAttacking) ||
            (controller != null && controller.CurrentState == MonsterAIState.Hurt) ||
            (mimic != null && mimic.IsTransforming))
            return false;

        // Let grounded enemies finish falling rather than freezing above their platforms.
        return IsSleeping || body == null || !body.simulated || (motor != null && motor.IsFlying) ||
               body.gravityScale <= 0f || body.IsSleeping() ||
               (Mathf.Abs(body.linearVelocity.y) < 0.05f && body.IsTouchingLayers(groundLayers));
    }

    private void SetVisible(bool visible)
    {
        if (IsVisible == visible)
            return;

        IsVisible = visible;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer visual = renderers[i];
            if (visual == null)
                continue;

            if (!visible)
            {
                previousRenderingOff[i] = visual.forceRenderingOff;
                visual.forceRenderingOff = true;
            }
            else
            {
                visual.forceRenderingOff = previousRenderingOff[i];
            }
        }
        // HealthBarUI creates a separate world object, outside the monster hierarchy.
        healthBar?.SetCulled(!visible);
    }

    private void SetSleeping(bool sleeping)
    {
        if (IsSleeping == sleeping)
            return;

        IsSleeping = sleeping;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
                continue;

            if (sleeping)
            {
                suspendedAnimators[i] = animator.enabled;
                if (!suspendedAnimators[i])
                    continue;
                previousKeepAnimatorState[i] = animator.keepAnimatorStateOnDisable;
                animator.keepAnimatorStateOnDisable = true;
                animator.enabled = false;
            }
            else if (suspendedAnimators[i])
            {
                animator.enabled = true;
                animator.keepAnimatorStateOnDisable = previousKeepAnimatorState[i];
                suspendedAnimators[i] = false;
            }
        }

        if (body == null)
            return;

        if (sleeping)
        {
            suspendedBody = body.simulated;
            if (!suspendedBody)
                return;
            savedVelocity = body.linearVelocity;
            savedAngularVelocity = body.angularVelocity;
            body.simulated = false;
        }
        else if (suspendedBody)
        {
            body.simulated = true;
            body.linearVelocity = savedVelocity;
            body.angularVelocity = savedAngularVelocity;
            body.WakeUp();
            suspendedBody = false;
        }
    }
}
