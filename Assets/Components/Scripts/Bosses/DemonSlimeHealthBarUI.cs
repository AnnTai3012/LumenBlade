using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class DemonSlimeHealthBarUI : MonoBehaviour
{
    [Header("Health source")]
    [SerializeField] private MonsterHealth health;

    [Header("Sprites")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;
    [SerializeField, Min(0.01f)] private float sizeMultiplier = 1f;

    [Header("Placement")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private int sortingOrder = 20;

    private Transform viewRoot;
    private SpriteRenderer fillRenderer;
    private float fillStartX;
    private float fillStartY;
    private bool isSubscribed;
    private bool hasPosition;
    private Vector3 lastPosition;

    private void Awake()
    {
        health ??= GetComponent<MonsterHealth>();
        targetRenderer ??= GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (health == null || targetRenderer == null || backgroundSprite == null || fillSprite == null)
        {
            Debug.LogError($"{name} needs MonsterHealth, a SpriteRenderer, and both health bar sprites.", this);
            enabled = false;
            return;
        }

        CreateViewIfNeeded();
        viewRoot.gameObject.SetActive(true);
        hasPosition = false;
        health.OnHealthChanged += UpdateBar;
        isSubscribed = true;
    }

    private void Start()
    {
        RefreshBar();
    }

    private void OnDisable()
    {
        if (isSubscribed && health != null)
            health.OnHealthChanged -= UpdateBar;

        isSubscribed = false;
        if (viewRoot != null)
            viewRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (isSubscribed && health != null)
            health.OnHealthChanged -= UpdateBar;

        if (viewRoot != null)
            Destroy(viewRoot.gameObject);
    }

    private void LateUpdate()
    {
        if (viewRoot == null || targetRenderer == null)
            return;

        Bounds bounds = targetRenderer.bounds;
        Vector3 position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + offset;
        if (hasPosition && position == lastPosition)
            return;

        viewRoot.position = position;
        lastPosition = position;
        hasPosition = true;
    }

    private void RefreshBar()
    {
        if (health != null)
            UpdateBar(health.CurrentHealth, health.MaxHealth);
    }

    private void UpdateBar(int current, int max)
    {
        if (fillRenderer == null)
            return;

        float fillPercent = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fillRenderer.transform.localScale = new Vector3(sizeMultiplier * fillPercent, sizeMultiplier, 1f);
        fillRenderer.transform.localPosition = new Vector3(fillStartX, fillStartY, -0.01f);
    }

    private void CreateViewIfNeeded()
    {
        if (viewRoot != null)
            return;

        GameObject root = new GameObject($"{name}_HealthBar");
        root.hideFlags = HideFlags.DontSave;
        viewRoot = root.transform;

        SpriteRenderer background = CreateBarSprite("Background", backgroundSprite, sortingOrder);
        fillRenderer = CreateBarSprite("Fill", fillSprite, sortingOrder + 1);

        Vector2 backgroundSize = backgroundSprite.bounds.size * sizeMultiplier;
        Vector2 fillSize = fillSprite.bounds.size * sizeMultiplier;
        background.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, 1f);
        background.transform.localPosition = new Vector3(-backgroundSize.x * 0.5f, 0f, 0f);
        fillRenderer.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, 1f);
        fillStartX = -backgroundSize.x * 0.5f + Mathf.Max(0f, (backgroundSize.x - fillSize.x) * 0.5f);
        fillStartY = Mathf.Max(0f, (backgroundSize.y - fillSize.y) * 0.5f);
        fillRenderer.transform.localPosition = new Vector3(fillStartX, fillStartY, -0.01f);
    }

    private SpriteRenderer CreateBarSprite(string spriteName, Sprite sprite, int order)
    {
        GameObject spriteObject = new GameObject(spriteName, typeof(SpriteRenderer));
        spriteObject.transform.SetParent(viewRoot, false);

        SpriteRenderer renderer = spriteObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = targetRenderer.sortingLayerID;
        renderer.sortingOrder = targetRenderer.sortingOrder + order;
        return renderer;
    }
}
