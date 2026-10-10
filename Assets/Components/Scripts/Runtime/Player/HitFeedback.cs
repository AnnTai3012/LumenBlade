using UnityEngine;

[DisallowMultipleComponent]
public sealed class HitFeedback : MonoBehaviour
{
    [Header("Health source")]
    [SerializeField] private Health health;
    [SerializeField] private MonsterHealth monsterHealth;

    [Header("Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

    private Color originalColor = Color.white;
    private float flashTimer;
    private bool isFlashing;

    private void Awake()
    {
        health ??= GetComponent<Health>();
        monsterHealth ??= GetComponent<MonsterHealth>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += Flash;
        else if (monsterHealth != null)
            monsterHealth.OnDamaged += Flash;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= Flash;
        else if (monsterHealth != null)
            monsterHealth.OnDamaged -= Flash;

        isFlashing = false;
        flashTimer = 0f;
        RestoreColor();
    }

    private void Update()
    {
        if (!isFlashing || spriteRenderer == null)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            isFlashing = false;
            RestoreColor();
        }
    }

    private void Flash()
    {
        if (spriteRenderer == null || hitFlashDuration <= 0f)
            return;

        flashTimer = hitFlashDuration;
        isFlashing = true;
        spriteRenderer.color = hitFlashColor;
    }

    private void RestoreColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}
