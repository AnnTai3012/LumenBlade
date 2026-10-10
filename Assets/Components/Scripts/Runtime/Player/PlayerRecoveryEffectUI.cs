using System;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

// Plays the authored sprite frames over the player's body on the overlay canvas.
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class PlayerRecoveryEffectUI : MonoBehaviour
{
    [Serializable]
    private sealed class EffectFrames
    {
        public RectTransform root;
        public UnityEngine.UI.Image image;
        public TMP_Text label;
        public CanvasGroup visibility;
        public Sprite[] frames;
        public float[] frameTimes;
        public float duration;
        [NonSerialized] public float startedAt;
        [NonSerialized] public int frame;
        [NonSerialized] public bool playing;
        [NonSerialized] public Vector2 maximumSpriteSize;
        [NonSerialized] public Vector2 targetSize;
        [NonSerialized] public bool finalFrameShown;
    }

    [SerializeField] private Health health;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private EffectFrames heal;
    [SerializeField] private EffectFrames revive;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private Vector2 imageSize = new Vector2(117f, 117f);
    [SerializeField, Min(1f)] private float healBodyScale = 4f;
    [SerializeField, Min(1f)] private float reviveBodyScale = 2.2f;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Health subscribedHealth;
    private Collider2D playerBody;
    private SpriteRenderer playerRenderer;
    private Bounds neutralSpriteBounds;
    private float nextBindAt;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = transform as RectTransform;
        Initialize(heal, false);
        Initialize(revive, true);
    }

    private static void Initialize(EffectFrames effect, bool anchorAtFeet)
    {
        if (effect == null || effect.root == null) return;
        effect.playing = false;
        effect.frame = 0;
        effect.finalFrameShown = false;
        effect.maximumSpriteSize = Vector2.zero;
        effect.root.pivot = new Vector2(0.5f, 0.5f);
        if (effect.image != null)
        {
            RectTransform imageRect = effect.image.rectTransform;
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, anchorAtFeet ? 0f : 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
        }
        effect.root.gameObject.SetActive(false);
        if (effect.frames == null) return;
        foreach (Sprite sprite in effect.frames)
            if (sprite != null)
                effect.maximumSpriteSize = Vector2.Max(effect.maximumSpriteSize, sprite.rect.size);
    }

    private void OnEnable()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = transform as RectTransform;
        Initialize(heal, false);
        Initialize(revive, true);
        Unsubscribe();
        Bind();
    }

    private void Bind()
    {
        if (health == null) health = FindFirstObjectByType<Health>();
        ResolveWorldCamera();
        if (health == subscribedHealth) return;
        Unsubscribe();
        subscribedHealth = health;
        playerBody = health != null ? health.GetComponent<Collider2D>() : null;
        playerRenderer = health != null ? health.GetComponent<SpriteRenderer>() : null;
        if (playerRenderer != null && playerRenderer.sprite != null)
            neutralSpriteBounds = playerRenderer.sprite.bounds;
        if (subscribedHealth == null) return;
        subscribedHealth.OnHealed += ShowHeal;
        subscribedHealth.OnRevived += ShowRevive;
        subscribedHealth.OnDied += Hide;
    }

    private void ResolveWorldCamera()
    {
        Camera selected = null;
        bool followsPlayer = false;
        foreach (Camera candidate in Camera.allCameras)
        {
            if (!candidate.isActiveAndEnabled || candidate.targetTexture != null ||
                (health != null && candidate.gameObject.scene != health.gameObject.scene) ||
                (health != null && (candidate.cullingMask & (1 << health.gameObject.layer)) == 0)) continue;
            CinemachineBrain brain = candidate.GetComponent<CinemachineBrain>();
            bool following = brain != null && brain.isActiveAndEnabled && brain.ActiveVirtualCamera != null;
            if (selected == null || (following && !followsPlayer) ||
                (following == followsPlayer && candidate.depth >= selected.depth))
            {
                selected = candidate;
                followsPlayer = following;
            }
        }
        if (selected != null) worldCamera = selected;
        else if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Unsubscribe()
    {
        if (subscribedHealth != null)
        {
            subscribedHealth.OnHealed -= ShowHeal;
            subscribedHealth.OnRevived -= ShowRevive;
            subscribedHealth.OnDied -= Hide;
        }
        subscribedHealth = null;
    }

    private void OnDisable()
    {
        Unsubscribe();
        Hide();
    }

    private void ShowHeal(int amount) => Play(heal, "+" + amount + " HP");
    private void ShowRevive() => Play(revive, "HỒI SINH");

    private void Play(EffectFrames effect, string text)
    {
        if (effect == null || effect.root == null || effect.image == null || effect.frames == null ||
            effect.frames.Length == 0 || effect.frameTimes == null || effect.frames.Length != effect.frameTimes.Length)
            return;
        effect.startedAt = Time.time;
        effect.frame = 0;
        effect.finalFrameShown = false;
        effect.playing = true;
        effect.targetSize = imageSize;
        // The image child may be disabled in the authored HUD; the frame alone cannot reveal it.
        effect.image.gameObject.SetActive(true);
        if (effect.label != null) effect.label.text = text;
        ApplyFrame(effect);
        effect.root.gameObject.SetActive(true);
    }

    private void ApplyFrame(EffectFrames effect)
    {
        Sprite sprite = effect.frames[effect.frame];
        effect.image.sprite = sprite;
        effect.image.enabled = sprite != null;
        if (sprite == null) return;
        Vector2 targetSize = effect.targetSize;
        float scale = Mathf.Min(targetSize.x / Mathf.Max(1f, effect.maximumSpriteSize.x), targetSize.y / Mathf.Max(1f, effect.maximumSpriteSize.y));
        effect.image.rectTransform.sizeDelta = sprite.rect.size * scale;
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextBindAt)
        {
            nextBindAt = Time.unscaledTime + 0.5f;
            if (health == null || worldCamera == null || subscribedHealth == null) Bind();
            else ResolveWorldCamera();
        }
        if (health == null || worldCamera == null) return;
        bool healPlaying = heal != null && heal.playing;
        bool revivePlaying = revive != null && revive.playing;
        if (!healPlaying && !revivePlaying) return;
        Vector3 bodyPosition = playerBody != null && playerBody.enabled
            ? playerBody.bounds.center : health.transform.position;
        if ((playerBody == null || !playerBody.enabled) && playerRenderer != null && neutralSpriteBounds.size.y > 0f)
            bodyPosition = playerRenderer.transform.TransformPoint(neutralSpriteBounds.center);
        Vector3 feetPosition = playerBody != null && playerBody.enabled
            ? new Vector3(playerBody.bounds.center.x, playerBody.bounds.min.y, bodyPosition.z)
            : playerRenderer != null ? playerRenderer.bounds.center - Vector3.up * playerRenderer.bounds.extents.y : health.transform.position;
        Vector3 healScreen = worldCamera.WorldToScreenPoint(bodyPosition + worldOffset);
        Vector3 reviveScreen = worldCamera.WorldToScreenPoint(feetPosition + worldOffset);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        float bodySize = GetBodySize(uiCamera);
        Resize(heal, bodySize, healBodyScale);
        Resize(revive, bodySize, reviveBodyScale);
        Tick(heal, healScreen, uiCamera);
        Tick(revive, reviveScreen, uiCamera);
    }

    private float GetBodySize(Camera uiCamera)
    {
        if (playerRenderer == null || neutralSpriteBounds.size.y <= 0f)
            return 0f;

        // Cache the neutral body bounds, so wide attack/skill sprites do not enlarge the VFX.
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 local = neutralSpriteBounds.center + new Vector3(
                (corner & 1) == 0 ? -neutralSpriteBounds.extents.x : neutralSpriteBounds.extents.x,
                (corner & 2) == 0 ? -neutralSpriteBounds.extents.y : neutralSpriteBounds.extents.y, 0f);
            Vector3 screen = worldCamera.WorldToScreenPoint(playerRenderer.transform.TransformPoint(local));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 point);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        Vector2 size = max - min;
        return Mathf.Max(size.x, size.y);
    }

    private void Resize(EffectFrames effect, float bodySize, float bodyScale)
    {
        if (effect == null || !effect.playing) return;
        effect.targetSize = bodySize > 0f ? Vector2.one * (bodySize * bodyScale) : imageSize;
        ApplyFrame(effect);
    }

    private void Tick(EffectFrames effect, Vector3 screenPosition, Camera uiCamera)
    {
        if (effect == null || !effect.playing) return;
        float elapsed = Time.time - effect.startedAt;
        if (elapsed >= effect.duration)
        {
            if (effect.finalFrameShown)
            {
                Stop(effect);
                return;
            }
            // Even a skipped/slow frame must render the authored final sprite once.
            effect.frame = effect.frames.Length - 1;
            effect.finalFrameShown = true;
            ApplyFrame(effect);
        }
        int frame = effect.frame;
        while (frame + 1 < effect.frameTimes.Length && elapsed >= effect.frameTimes[frame + 1]) frame++;
        if (frame != effect.frame)
        {
            effect.frame = frame;
            ApplyFrame(effect);
        }
        RectTransform parent = effect.root.parent as RectTransform;
        if (parent != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPosition, uiCamera, out Vector3 position))
            effect.root.position = position;
        if (effect.visibility != null) effect.visibility.alpha = screenPosition.z > 0f ? 1f : 0f;
    }

    private static void Stop(EffectFrames effect)
    {
        if (effect == null) return;
        effect.playing = false;
        if (effect.root != null) effect.root.gameObject.SetActive(false);
    }

    private void Hide()
    {
        Stop(heal);
        Stop(revive);
    }
}
