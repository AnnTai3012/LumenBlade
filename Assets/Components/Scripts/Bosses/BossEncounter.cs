using System.Collections.Generic;
using UnityEngine;

// The arena is measured in world units and stays at its spawn position while the boss moves.
[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class BossEncounter : MonoBehaviour
{
    [SerializeField] private string bossName = "BOSS";
    [SerializeField] private MonsterHealth health;
    [SerializeField] private Health player;
    [SerializeField] private Vector2 arenaSize = new Vector2(20f, 12f);
    [SerializeField] private Vector2 arenaOffset = new Vector2(0f, 2f);

    private static readonly List<BossEncounter> activeEncounters = new List<BossEncounter>();
    private Vector2 arenaCenter;
    private float targetRetryTime;
    private bool appearSoundPlayed;
    private bool ownsAppearSound;

    public static IReadOnlyList<BossEncounter> ActiveEncounters => activeEncounters;
    public string BossName => bossName;
    public MonsterHealth Health => health;
    public Health Player => player;
    public Vector2 ArenaCenter => arenaCenter;
    public bool IsPlayerInside => isActiveAndEnabled && health != null && !health.IsDead &&
        player != null && player.isActiveAndEnabled && !player.IsDead && Contains(player.transform.position);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEncounters() => activeEncounters.Clear();

    private void Awake()
    {
        health ??= GetComponent<MonsterHealth>();
        ownsAppearSound = GetComponent<GolemBossController>() == null;
        arenaCenter = (Vector2)transform.position + arenaOffset;
        ResolvePlayer();
    }

    private void OnEnable()
    {
        if (!activeEncounters.Contains(this)) activeEncounters.Add(this);
    }

    private void OnDisable() => activeEncounters.Remove(this);

    private void Update()
    {
        if (ownsAppearSound && !appearSoundPlayed && IsPlayerInside)
        {
            appearSoundPlayed = true;
            GameAudioManager.PlayCue(GameAudioCue.BossAppear);
        }
        if (player != null && player.isActiveAndEnabled) return;
        if (Time.unscaledTime < targetRetryTime) return;
        targetRetryTime = Time.unscaledTime + 0.5f;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player == null || !player.isActiveAndEnabled)
            player = FindFirstObjectByType<Health>();
    }

    public bool Contains(Vector2 position)
    {
        Vector2 offset = position - arenaCenter;
        return Mathf.Abs(offset.x) <= arenaSize.x * 0.5f && Mathf.Abs(offset.y) <= arenaSize.y * 0.5f;
    }

    private void OnValidate()
    {
        arenaSize.x = Mathf.Max(0.1f, arenaSize.x);
        arenaSize.y = Mathf.Max(0.1f, arenaSize.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.35f, 0.8f);
        Vector2 center = Application.isPlaying ? arenaCenter : (Vector2)transform.position + arenaOffset;
        Gizmos.DrawWireCube(center, new Vector3(arenaSize.x, arenaSize.y, 0f));
    }
}
