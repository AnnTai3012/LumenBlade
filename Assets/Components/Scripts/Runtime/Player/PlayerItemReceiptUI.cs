using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerItemReceiptUI : MonoBehaviour
{
    [Serializable]
    private sealed class ReceiptRow
    {
        public RectTransform root;
        public UnityEngine.UI.Image icon;
        public TMP_Text label;
        public CanvasGroup visibility;
    }

    [SerializeField] private GameResourceSettings settings;
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private ReceiptRow[] rows;
    [Header("Floating notification")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField, Min(0.2f)] private float duration = 2.2f;
    [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.55f;
    [SerializeField, Min(0f)] private float riseDistance = 36f;
    [SerializeField, Min(1f)] private float rowSpacing = 48f;

    private RectTransform canvasRect;
    private Canvas canvas;
    private RunResources resources;
    private float nextBindAt;
    private int[] amounts;
    private float[] receivedAt;
    private bool[] visible;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = transform as RectTransform;
        if (canvas == null || rows == null || rows.Length < 2)
        {
            Debug.LogError("PlayerItemReceiptUI needs a Canvas and two receipt rows.", this);
            enabled = false;
            return;
        }
        amounts = new int[rows.Length];
        receivedAt = new float[rows.Length];
        visible = new bool[rows.Length];
        for (int i = 0; i < rows.Length; i++) rows[i].root.gameObject.SetActive(false);
    }

    private void OnEnable() => Bind();

    private void Bind()
    {
        if (player == null)
        {
            Health health = FindFirstObjectByType<Health>();
            if (health != null) player = health.transform;
        }
        if (worldCamera == null) worldCamera = Camera.main;
        if (resources == RunResources.Instance) return;
        if (resources != null) resources.OnConsumableReceived -= ShowReceipt;
        resources = RunResources.Instance;
        if (resources != null)
        {
            if (settings == null) settings = resources.Settings;
            resources.OnConsumableReceived += ShowReceipt;
        }
    }

    private void OnDisable()
    {
        if (resources != null) resources.OnConsumableReceived -= ShowReceipt;
        resources = null;
        if (visible == null) return;
        for (int i = 0; i < rows.Length; i++)
        {
            visible[i] = false;
            rows[i].root.gameObject.SetActive(false);
        }
    }

    private void ShowReceipt(ConsumableKind kind, int amount)
    {
        int index = (int)kind;
        if (amount <= 0 || visible == null || index < 0 || index >= rows.Length) return;
        GameAudioManager.PlayCue(GameAudioCue.ItemReceived);
        amounts[index] = visible[index] && Time.unscaledTime - receivedAt[index] < duration ? amounts[index] + amount : amount;
        receivedAt[index] = Time.unscaledTime;
        visible[index] = true;
        rows[index].icon.sprite = settings != null ? settings.Icon(kind) : null;
        rows[index].label.text = "Nhận " + GameResourceSettings.ItemName(kind) + " +" + amounts[index];
        rows[index].visibility.alpha = 0f;
        rows[index].root.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if ((player == null || worldCamera == null || resources == null) && Time.unscaledTime >= nextBindAt)
        {
            nextBindAt = Time.unscaledTime + 0.5f;
            Bind();
        }
        if (visible == null) return;
        bool anyVisible = false;
        for (int i = 0; i < rows.Length; i++)
        {
            if (!visible[i]) continue;
            if (Time.unscaledTime - receivedAt[i] >= duration)
            {
                visible[i] = false;
                rows[i].root.gameObject.SetActive(false);
            }
            else anyVisible = true;
        }
        if (!anyVisible || player == null || worldCamera == null) return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(player.position + worldOffset);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 position);
        for (int i = 0; i < rows.Length; i++)
        {
            if (!visible[i]) continue;
            float elapsed = Time.unscaledTime - receivedAt[i];
            float progress = Mathf.Clamp01(elapsed / duration);
            float fadeIn = Mathf.Clamp01(elapsed / 0.12f);
            float fadeOut = Mathf.Clamp01((duration - elapsed) / Mathf.Min(fadeOutDuration, duration));
            rows[i].visibility.alpha = screenPosition.z > 0f ? fadeIn * fadeOut : 0f;
            Vector2 rowPosition = position + Vector2.up * (i * rowSpacing + progress * riseDistance);
            float halfWidth = rows[i].root.rect.width * 0.5f;
            rowPosition.x = Mathf.Clamp(rowPosition.x, canvasRect.rect.xMin + halfWidth + 8f, canvasRect.rect.xMax - halfWidth - 8f);
            rows[i].root.anchoredPosition = rowPosition;
        }
    }
}
