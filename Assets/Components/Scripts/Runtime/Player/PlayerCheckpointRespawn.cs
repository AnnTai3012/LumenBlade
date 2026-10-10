using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PlayerCheckpointRespawn : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Seconds to show the death animation before displaying the restart panel.")]
    private float deathPanelDelay = 1.25f;
    [SerializeField, Min(0f), Tooltip("Hold the completed death pose before showing Game Over.")]
    private float gameOverDelayAfterDeath = 0.25f;

    private Health health;
    private bool respawnQueued;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
            health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
        StopAllCoroutines();
        respawnQueued = false;
    }

    private void HandleDeath()
    {
        if (respawnQueued)
            return;

        respawnQueued = true;
        RunResources resources = RunResources.Instance;
        if (resources != null && resources.Settings != null && resources.TryConsume(ConsumableKind.ReviveCross))
        {
            StartCoroutine(ReviveInPlace(resources.Settings));
            return;
        }
        RunResources.Instance?.DiscardUnbankedLevelProgressOnDeath();
        GetComponent<PlayerKeyInventory>()?.TryConsumeKey();
        StartCoroutine(ShowRestartPanel());
    }

    private IEnumerator ReviveInPlace(GameResourceSettings settings)
    {
        // Wait until every death listener has stopped combat and physics before restoring them.
        yield return WaitForDeathAnimation();
        if (health != null && health.Revive(Mathf.CeilToInt(health.MaxHealth * settings.ReviveHealthFraction), settings.ReviveInvulnerability))
        {
            PlayerStamina stamina = GetComponent<PlayerStamina>();
            if (stamina != null) stamina.RestoreCurrentStamina(stamina.MaxStamina);
        }
        respawnQueued = false;
    }

    private IEnumerator ShowRestartPanel()
    {
        yield return WaitForDeathAnimation();
        // Let the completed pose render before the panel covers it and pauses the Animator.
        yield return null;
        if (gameOverDelayAfterDeath > 0f)
            yield return new WaitForSeconds(gameOverDelayAfterDeath);
        if (health == null || !health.IsDead)
        {
            respawnQueued = false;
            yield break;
        }

        GameplayPanelController panels = FindFirstObjectByType<GameplayPanelController>();
        if (panels != null)
        {
            panels.OpenRestartPanel();
            yield break;
        }

        RestartLevel();
    }

    private IEnumerator WaitForDeathAnimation()
    {
        Animator animator = GetComponent<Animator>();
        int deathState = Animator.StringToHash("Base Layer.Death");
        if (animator == null || !animator.isActiveAndEnabled || !animator.HasState(0, deathState))
        {
            if (deathPanelDelay > 0f)
                yield return new WaitForSeconds(deathPanelDelay);
            yield break;
        }

        // A different current state is not proof that Death finished: wait for its real end.
        while (animator != null && animator.isActiveAndEnabled && health != null && health.IsDead)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == deathState && state.normalizedTime >= 1f)
                yield break;
            yield return null;
        }
    }

    public static bool RestartLevel(System.Action beforeRestart = null)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.path))
        {
            Debug.LogError("Cannot respawn because the active level has no valid scene path.");
            return false;
        }

        RunResources resources = RunResources.Instance;
        string restartScenePath = activeScene.path;
        if (resources != null && resources.TryGetRestartCheckpoint(activeScene.path, out string checkpointScenePath, out _))
            restartScenePath = checkpointScenePath;

        if (!Application.CanStreamedLevelBeLoaded(restartScenePath))
        {
            Debug.LogError($"Cannot reload checkpoint scene '{restartScenePath}'. Add it to Build Settings.");
            return false;
        }

        beforeRestart?.Invoke();
        resources?.PrepareRestart(activeScene.path);
        Time.timeScale = 1f;
        GameAudioManager.NotifyRestart();
        SceneManager.LoadScene(restartScenePath, LoadSceneMode.Single);
        return true;
    }
}
