using UnityEngine;

// Thanh máu world-space dùng chung cho Player (Health) và Monster (MonsterHealth).
[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("Health source")]
    [SerializeField] private Health health;
    [SerializeField] private MonsterHealth monsterHealth;

    [Header("Visual")]
    [SerializeField] private Sprite healthBarSprite;
    [SerializeField] private Vector2 size = new Vector2(64f, 16f);
    [SerializeField, Min(0.001f)] private float worldScale = 0.01f;
    [SerializeField] private int sortingOrder = 20;

    [Header("Placement")]
    [SerializeField] private Transform target;
    [SerializeField, Tooltip("World-space adjustment from the top center of the target sprite. Increase Y to add more space above the head.")]
    private Vector3 offset = new Vector3(0f, 0.2f, 0f);
    private Transform viewRoot;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer targetRenderer;
    private PlayerCombat playerCombat;
    private Vector3 playerHeadAnchor;
    private bool isSubscribed;
    private bool hasPosition;
    private Vector3 lastPosition;
    private bool isCulled;

    private void Awake()
    {
        if (!ResolveHealthSource())
        {
            enabled = false;
            return;
        }

        target ??= transform;
        targetRenderer = target.GetComponent<SpriteRenderer>();
        playerCombat = health != null ? health.GetComponent<PlayerCombat>() : null;
        if (targetRenderer != null && targetRenderer.sprite != null)
            playerHeadAnchor = new Vector3(0f, targetRenderer.sprite.bounds.max.y, 0f);
    }

    private bool ResolveHealthSource()
    {
        for (Transform owner = transform; owner != null; owner = owner.parent)
        {
            Health ownerHealth = owner.GetComponent<Health>();
            MonsterHealth ownerMonsterHealth = owner.GetComponent<MonsterHealth>();
            if (ownerHealth == null && ownerMonsterHealth == null)
                continue;

            if (ownerHealth != null && ownerMonsterHealth != null)
            {
                Debug.LogError($"{name} has both Health and MonsterHealth on its owner; a health bar needs one source.", this);
                return false;
            }

            if (health != ownerHealth || monsterHealth != ownerMonsterHealth)
                Debug.LogWarning($"{name} HealthBarUI references did not match its owner. Binding to the owner's health source.", this);

            health = ownerHealth;
            monsterHealth = ownerMonsterHealth;
            return true;
        }

        if (health != null && monsterHealth != null)
        {
            Debug.LogError($"{name} has both Health and MonsterHealth references; a health bar needs one source.", this);
            return false;
        }

        return true;
    }

    private void OnEnable()
    {
        if (!HasHealthSource() || healthBarSprite == null)
        {
            enabled = false;
            return;
        }

        CreateViewIfNeeded();
        viewRoot.gameObject.SetActive(!isCulled);
        hasPosition = false;
        Subscribe();
    }

    private void Start()
    {
        // Start chạy sau Awake của Health/MonsterHealth, nên giá trị ban đầu luôn đúng.
        RefreshBar();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (viewRoot != null)
            viewRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (viewRoot != null)
            Destroy(viewRoot.gameObject);
    }

    private void LateUpdate()
    {
        if (!isCulled)
            UpdateViewPosition();
    }

    private void UpdateViewPosition()
    {
        if (viewRoot == null || target == null)
            return;

        Vector3 position = GetTargetTopCenter() + offset;
        if (hasPosition && position == lastPosition)
            return;

        viewRoot.position = position;
        lastPosition = position;
        hasPosition = true;
    }

    public void SetCulled(bool culled)
    {
        isCulled = culled;
        if (viewRoot != null)
            viewRoot.gameObject.SetActive(!culled && isActiveAndEnabled);
        if (!culled)
        {
            hasPosition = false;
            UpdateViewPosition();
        }
    }

    private Vector3 GetTargetTopCenter()
    {
        // Skill 3 includes a traveling wave in the root sprite; its bounds extend far past the body.
        if (playerCombat != null && playerCombat.IsUsingRangedSkill)
            return target.TransformPoint(playerHeadAnchor);
        if (targetRenderer == null)
            return target.position;

        Bounds spriteBounds = targetRenderer.bounds;
        return new Vector3(spriteBounds.center.x, spriteBounds.max.y, spriteBounds.center.z);
    }

    private bool HasHealthSource()
    {
        return health != null || monsterHealth != null;
    }

    private void Subscribe()
    {
        if (isSubscribed)
            return;

        if (health != null)
            health.OnHealthChanged += UpdateBar;
        else
            monsterHealth.OnHealthChanged += UpdateBar;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
            return;

        if (health != null)
            health.OnHealthChanged -= UpdateBar;
        else if (monsterHealth != null)
            monsterHealth.OnHealthChanged -= UpdateBar;

        isSubscribed = false;
    }

    private void RefreshBar()
    {
        if (health != null)
            UpdateBar(health.CurrentHealth, health.MaxHealth);
        else if (monsterHealth != null)
            UpdateBar(monsterHealth.CurrentHealth, monsterHealth.MaxHealth);
    }

    private void UpdateBar(int current, int max)
    {
        if (fillRenderer == null)
            return;

        float fillPercent = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        Vector2 spriteSize = healthBarSprite.bounds.size;
        Vector2 worldSize = GetWorldSize();
        float fillWidth = worldSize.x * fillPercent;
        fillRenderer.transform.localScale = new Vector3(
            fillWidth / spriteSize.x,
            worldSize.y / spriteSize.y,
            1f);
        fillRenderer.transform.localPosition = new Vector3((fillPercent - 1f) * worldSize.x * 0.5f, 0f, 0f);
    }

    private void CreateViewIfNeeded()
    {
        if (viewRoot != null)
            return;

        GameObject root = new GameObject($"{name}_HealthBar");
        root.hideFlags = HideFlags.DontSave;
        viewRoot = root.transform;

        CreateBarSprite("Background", new Color(0.35f, 0.35f, 0.35f, 0.85f), sortingOrder);
        fillRenderer = CreateBarSprite("Fill", Color.white, sortingOrder + 1);
    }

    private SpriteRenderer CreateBarSprite(string spriteName, Color color, int order)
    {
        GameObject spriteObject = new GameObject(spriteName, typeof(SpriteRenderer));
        spriteObject.transform.SetParent(viewRoot, false);

        SpriteRenderer renderer = spriteObject.GetComponent<SpriteRenderer>();
        renderer.sprite = healthBarSprite;
        renderer.color = color;
        renderer.sortingLayerID = targetRenderer != null ? targetRenderer.sortingLayerID : 0;
        renderer.sortingOrder = (targetRenderer != null ? targetRenderer.sortingOrder : 0) + order;

        Vector2 spriteSize = healthBarSprite.bounds.size;
        Vector2 worldSize = GetWorldSize();
        spriteObject.transform.localScale = new Vector3(worldSize.x / spriteSize.x, worldSize.y / spriteSize.y, 1f);
        return renderer;
    }

    private Vector2 GetWorldSize()
    {
        return size * worldScale;
    }
}
