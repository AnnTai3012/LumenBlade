using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SfxCategory { Ambience, Action, Combat }

// Owns playback and mixing; gameplay systems retain authority over health and progression.
[DisallowMultipleComponent]
public sealed class GameAudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "BTL.Audio.MusicVolume";
    private const int VoiceLimit = 20;
    private static readonly string[] CategoryVolumeKeys = { "BTL.Audio.AmbienceVolume", "BTL.Audio.ActionVolume", "BTL.Audio.CombatVolume" };
    public static GameAudioManager Instance { get; private set; }
    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
    public GameAudioState CurrentState { get; private set; }
    public GameAudioEnvironment CurrentEnvironment { get; private set; }
    public string CurrentSceneName { get; private set; }
    public int ActiveVoiceCount { get { int n = 0; foreach (Voice v in voices) if (v.active) n++; return n; } }
    [SerializeField] private GameAudioCatalog catalog;
    private sealed class Voice
    {
        public AudioSource source;
        public AudioLowPassFilter lowPass;
        public AudioReverbFilter reverb;
        public GameAudioCue cue;
        public float gain, endTime;
        public float startedAt, fadeIn, fadeOut;
        public int priority;
        public bool active, paused;
    }
    private readonly List<Voice> voices = new List<Voice>(VoiceLimit);
    private readonly Dictionary<GameAudioCue, float> lastPlayed = new Dictionary<GameAudioCue, float>();
    private readonly List<AudioEnvironmentZone> zones = new List<AudioEnvironmentZone>();
    private AudioSource musicSource, nextMusicSource, uiSfxSource, ambienceSource, heartbeatSource;
    private AudioLowPassFilter musicLowPass, nextMusicLowPass;
    private Health player;
    private BossEncounter trackedBoss;
    private AudioSceneProfile sceneProfile;
    private float playerRetryAt, crossfade = 1f, musicGain = 1f, gameplayGain = 1f;
    private float cutoff = 22000f, reverbAmount, musicPitch = 1f;
    private float ambienceGain, heartbeatGain, outgoingGain = 1f;
    private bool victoryPlayed;
    private bool victoryCuePending;
    private float backgroundMusicResumeAt = -1f;
    private bool restartOnArrival;
    private AudioClip lastUiClip;
    private float lastUiTime = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstScene()
    {
        if (Instance == null) new GameObject("Game Audio Manager").AddComponent<GameAudioManager>();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        catalog ??= Resources.Load<GameAudioCatalog>("GameAudioCatalog");
        musicSource = CreateSource("Music A", true);
        nextMusicSource = CreateSource("Music B", true);
        musicLowPass = musicSource.gameObject.AddComponent<AudioLowPassFilter>();
        nextMusicLowPass = nextMusicSource.gameObject.AddComponent<AudioLowPassFilter>();
        uiSfxSource = CreateSource("UI SFX", false);
        uiSfxSource.ignoreListenerPause = true;
        ambienceSource = CreateSource("Environment", true);
        heartbeatSource = CreateSource("Low health heartbeat", true);
        for (int i = 0; i < VoiceLimit; i++)
        {
            AudioSource source = CreateSource("Gameplay voice " + (i + 1), false);
            var voice = new Voice { source = source, lowPass = source.gameObject.AddComponent<AudioLowPassFilter>(),
                reverb = source.gameObject.AddComponent<AudioReverbFilter>() };
            voice.reverb.reverbPreset = AudioReverbPreset.User;
            voice.reverb.room = -1200f;
            voice.reverb.decayTime = 0.85f;
            voice.reverb.reverbDelay = 0.025f;
            voice.reverb.reverbLevel = -10000f;
            voice.reverb.enabled = false;
            voices.Add(voice);
        }
        if (catalog != null)
        {
            ambienceSource.clip = catalog.caveAmbience;
            heartbeatSource.clip = catalog.lowHealthHeartbeat;
            if (ambienceSource.clip != null) ambienceSource.Play();
            if (heartbeatSource.clip != null) heartbeatSource.Play();
        }
        ApplyVolumes();
    }
    private AudioSource CreateSource(string sourceName, bool loop)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = 0f;
        return source;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (musicSource != null) ConfigureScene(SceneManager.GetActiveScene());
    }
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnDestroy() { BindPlayer(null); if (Instance == this) Instance = null; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single || scene == SceneManager.GetActiveScene()) ConfigureScene(scene);
    }
    private void ConfigureScene(Scene scene)
    {
        CurrentSceneName = scene.name;
        sceneProfile = catalog != null ? catalog.ResolveScene(scene.name) : null;
        BindPlayer(null);
        trackedBoss = null;
        playerRetryAt = 0f;
        victoryPlayed = false;
        victoryCuePending = false;
        backgroundMusicResumeAt = -1f;
        zones.Clear();
        lastPlayed.Clear();
        foreach (Voice voice in voices) Release(voice);
        CurrentEnvironment = sceneProfile != null ? sceneProfile.environment : GameAudioEnvironment.Cave;
        CurrentState = GameAudioState.Exploration;
        SelectMusic(false);
        if (restartOnArrival)
        {
            restartOnArrival = false;
            Play(GameAudioCue.PlayerRestart, 1f, false, Vector3.zero);
        }
    }
    private void Update()
    {
        if (catalog == null) return;
        float dt = Time.unscaledDeltaTime;
        if ((player == null || !player.isActiveAndEnabled) && Time.unscaledTime >= playerRetryAt)
        {
            playerRetryAt = Time.unscaledTime + 0.5f;
            BindPlayer(FindFirstObjectByType<Health>());
        }
        bool dead = player != null && player.IsDead;
        bool paused = Time.timeScale <= 0f;
        bool lowHealth = player != null && !dead && player.CurrentHealth <= player.MaxHealth * catalog.lowHealthThreshold;
        BossEncounter inside = null;
        foreach (BossEncounter encounter in BossEncounter.ActiveEncounters)
            if (encounter != null && encounter.IsPlayerInside) { inside = encounter; break; }
        if (trackedBoss != null && trackedBoss.Health != null && trackedBoss.Health.IsDead && !victoryPlayed)
        {
            victoryPlayed = true;
            backgroundMusicResumeAt = Time.time + Mathf.Max(0f, catalog.bossBackgroundDelay);
            StopMusicForVictory();
            victoryCuePending = true;
        }
        if (victoryCuePending && !paused && !dead)
        {
            Play(GameAudioCue.BossVictory, 1f, false, Vector3.zero);
            victoryCuePending = false;
        }
        if (inside != null && inside != trackedBoss)
        {
            trackedBoss = inside; victoryPlayed = false;
            victoryCuePending = false;
            backgroundMusicResumeAt = -1f;
        }
        bool bossFight = inside != null;
        bool pressure = bossFight && inside.Health.CurrentHealth <= inside.Health.MaxHealth * catalog.bossPressureThreshold;
        CurrentState = dead ? GameAudioState.Dead : paused ? GameAudioState.Paused : lowHealth ? GameAudioState.LowHealth :
            pressure ? GameAudioState.BossPressure : bossFight ? GameAudioState.BossCombat : GameAudioState.Exploration;
        CurrentEnvironment = bossFight ? GameAudioEnvironment.Arena : sceneProfile != null ? sceneProfile.environment : GameAudioEnvironment.Cave;
        int priority = int.MinValue;
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            AudioEnvironmentZone zone = zones[i];
            if (zone == null || !zone.isActiveAndEnabled) { zones.RemoveAt(i); continue; }
            if (zone.Priority >= priority) { priority = zone.Priority; CurrentEnvironment = zone.Environment; }
        }
        // Keep victory/death SFX audible without starting exploration music during the delay.
        bool waitingAfterVictory = backgroundMusicResumeAt >= 0f && Time.time < backgroundMusicResumeAt;
        if (!waitingAfterVictory)
        {
            backgroundMusicResumeAt = -1f;
            SelectMusic(bossFight && !dead);
        }
        float blend = 1f - Mathf.Exp(-dt * 5f);
        float targetCutoff = dead ? 900f : paused ? 1900f : lowHealth ? 4200f : 22000f;
        float environmentReverb = CurrentEnvironment == GameAudioEnvironment.Arena ? 0.32f :
            CurrentEnvironment == GameAudioEnvironment.Cave ? (sceneProfile != null ? sceneProfile.reverb : 0.2f) : 0.03f;
        cutoff = Mathf.Lerp(cutoff, targetCutoff, blend);
        reverbAmount = Mathf.Lerp(reverbAmount, environmentReverb, blend);
        musicGain = Mathf.Lerp(musicGain, dead ? 0.2f : paused ? 0.35f : lowHealth ? 0.55f : 0.78f, blend);
        gameplayGain = Mathf.Lerp(gameplayGain, dead ? 0.8f : 1f, blend);
        musicPitch = Mathf.Lerp(musicPitch, pressure && !paused && !dead ? 1.08f : 1f, blend);
        crossfade = Mathf.MoveTowards(crossfade, 1f, dt / Mathf.Max(0.05f, catalog.transitionSeconds));
        musicSource.volume = MusicVolume * musicGain * outgoingGain * (1f - crossfade);
        nextMusicSource.volume = MusicVolume * musicGain * crossfade;
        musicSource.pitch = nextMusicSource.pitch = musicPitch;
        musicLowPass.cutoffFrequency = nextMusicLowPass.cutoffFrequency = cutoff;
        if (crossfade >= 1f && musicSource.isPlaying) musicSource.Stop();
        float ambience = CurrentEnvironment == GameAudioEnvironment.Cave ? (sceneProfile != null ? sceneProfile.ambienceVolume : 0.12f) :
            CurrentEnvironment == GameAudioEnvironment.Arena ? 0.07f : 0f;
        ambienceGain = Mathf.Lerp(ambienceGain, paused || dead ? 0f : ambience, blend);
        heartbeatGain = Mathf.Lerp(heartbeatGain, lowHealth && !paused ? 0.42f : 0f, blend);
        ambienceSource.volume = GetCategoryVolume(SfxCategory.Ambience) * ambienceGain;
        heartbeatSource.volume = GetCategoryVolume(SfxCategory.Combat) * heartbeatGain;
        uiSfxSource.volume = GetCategoryVolume(SfxCategory.Ambience);
        foreach (Voice voice in voices)
        {
            if (!voice.active) continue;
            if (paused)
            {
                if (!voice.paused) { voice.source.Pause(); voice.paused = true; }
                voice.endTime += dt;
                voice.startedAt += dt;
            }
            else if (voice.paused) { voice.source.UnPause(); voice.paused = false; }
            if (!paused && Time.unscaledTime >= voice.endTime) { Release(voice); continue; }
            ApplyVoiceMix(voice);
        }
    }
    private void SelectMusic(bool bossFight)
    {
        AudioClip desired = catalog == null ? null : CurrentSceneName == GameSceneNavigation.MenuSceneName
            ? (catalog.menuMusic != null ? catalog.menuMusic : catalog.backgroundMusic) : bossFight && catalog.bossMusic != null ? catalog.bossMusic :
            victoryPlayed ? catalog.backgroundMusic :
            sceneProfile != null && sceneProfile.music != null ? sceneProfile.music : catalog.backgroundMusic;
        if (desired == null || nextMusicSource.clip == desired) return;
        // If interrupted, retain the louder track as the outgoing source.
        if (nextMusicSource.volume >= musicSource.volume)
        {
            AudioSource source = musicSource; musicSource = nextMusicSource; nextMusicSource = source;
            AudioLowPassFilter filter = musicLowPass; musicLowPass = nextMusicLowPass; nextMusicLowPass = filter;
        }
        nextMusicSource.Stop();
        outgoingGain = MusicVolume * musicGain > 0.001f ? Mathf.Clamp01(musicSource.volume / (MusicVolume * musicGain)) : 1f;
        nextMusicSource.clip = desired;
        nextMusicSource.volume = 0f;
        nextMusicSource.Play();
        crossfade = 0f;
    }
    private void StopMusicForVictory()
    {
        musicSource.Stop();
        nextMusicSource.Stop();
        musicSource.clip = nextMusicSource.clip = null;
        musicSource.volume = nextMusicSource.volume = 0f;
        crossfade = 1f;
    }
    public static void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
        Instance?.ApplyVolumes();
        PlayerPrefs.Save();
    }
    public static float GetCategoryVolume(SfxCategory category) =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(CategoryVolumeKeys[(int)category], 1f));

    public static void SetCategoryVolume(SfxCategory category, float volume)
    {
        PlayerPrefs.SetFloat(CategoryVolumeKeys[(int)category], Mathf.Clamp01(volume));
        Instance?.ApplyVolumes();
        PlayerPrefs.Save();
    }

    public static SfxCategory GetCueCategory(GameAudioCue cue)
    {
        if (IsUiCue(cue) || cue == GameAudioCue.KeyPickup || cue == GameAudioCue.BossVictory) return SfxCategory.Ambience;
        return cue switch
        {
            GameAudioCue.PlayerFootstep or GameAudioCue.PlayerJump or GameAudioCue.PlayerDash or
            GameAudioCue.PlayerFallImpact or GameAudioCue.ChestOpen or GameAudioCue.ExitGateOpen => SfxCategory.Action,
            _ => SfxCategory.Combat
        };
    }
    public static void PlayCue(GameAudioCue cue) => Instance?.Play(cue, 1f, false, Vector3.zero);
    public static void PlayCueAt(GameAudioCue cue, Vector3 position) => Instance?.Play(cue, 1f, false, position, true);
    public static void NotifyRestart() { if (Instance != null) Instance.restartOnArrival = true; }
    private void BindPlayer(Health next)
    {
        player = next;
    }
    // Presentation owners stop their own cues when an action is interrupted.
    public static void StopCue(GameAudioCue cue)
    {
        if (Instance == null) return;
        foreach (Voice voice in Instance.voices)
            if (voice.active && voice.cue == cue) Release(voice);
    }
    public static float GetCueDuration(GameAudioCue cue)
    {
        AudioClip clip = Instance != null && Instance.catalog != null ? Instance.catalog.Resolve(cue) : null;
        return clip != null ? clip.length : 0f;
    }
    public static void PlayCueForDuration(GameAudioCue cue, float duration)
    {
        if (Instance != null && Instance.catalog != null)
            Instance.Play(cue, Instance.catalog.ResolveSkillPitch(cue, duration), true, Vector3.zero, false, duration);
    }
    private static bool IsUiCue(GameAudioCue cue) => cue <= GameAudioCue.RestartPanelOpen ||
        cue == GameAudioCue.BlessingReceived || cue == GameAudioCue.ItemReceived ||
        cue == GameAudioCue.LevelTransition || cue == GameAudioCue.PlayerRestart;
    private void Play(GameAudioCue cue, float pitch, bool timed, Vector3 position, bool positional = false, float duration = 0f)
    {
        AudioClip clip = catalog != null ? catalog.Resolve(cue) : null;
        if (clip == null) return;
        AudioCueSettings settings = catalog.ResolveSettings(cue);
        float cooldown = settings != null ? settings.cooldown : 0f;
        if (lastPlayed.TryGetValue(cue, out float last) && Time.unscaledTime - last < cooldown) return;
        float gain = settings != null ? settings.volume : 0.7f;
        if (!timed && settings != null) pitch = Random.Range(settings.minPitch, Mathf.Max(settings.minPitch, settings.maxPitch));
        if (IsUiCue(cue))
        {
            // A button and its panel can request the same click in the same frame.
            if (lastUiClip == clip && Time.unscaledTime - lastUiTime < 0.05f) return;
            lastUiClip = clip;
            lastUiTime = Time.unscaledTime;
            lastPlayed[cue] = Time.unscaledTime;
            uiSfxSource.PlayOneShot(clip, gain);
            return;
        }
        if (Time.timeScale <= 0f && cue != GameAudioCue.PlayerDeath && cue != GameAudioCue.PlayerRevive) return;
        float pan = 0f;
        if (positional && player != null)
        {
            float distance = Vector2.Distance(position, player.transform.position);
            if (distance >= 24f) return;
            gain *= Mathf.Clamp01(1f - Mathf.Max(0f, distance - 4f) / 20f);
            pan = Mathf.Clamp((position.x - player.transform.position.x) / 20f, -0.35f, 0.35f);
        }
        int cueCount = 0;
        Voice free = null, weakest = null;
        foreach (Voice voice in voices)
        {
            if (!voice.active || (!voice.paused && !voice.source.isPlaying)) { free ??= voice; continue; }
            if (voice.cue == cue) cueCount++;
            if (weakest == null || voice.priority > weakest.priority || (voice.priority == weakest.priority && voice.endTime < weakest.endTime)) weakest = voice;
        }
        if (cueCount >= (settings != null ? settings.maxVoices : 3)) return;
        int voicePriority = settings != null ? settings.priority : 128;
        if (free == null)
        {
            if (weakest == null || weakest.priority < voicePriority) return;
            free = weakest;
            Release(free);
        }
        lastPlayed[cue] = Time.unscaledTime;
        free.active = true;
        free.paused = false;
        free.cue = cue;
        free.gain = gain;
        free.priority = voicePriority;
        free.source.priority = voicePriority;
        free.source.clip = clip;
        free.source.pitch = Mathf.Clamp(pitch, 0.01f, 3f);
        free.source.panStereo = pan;
        float playbackDuration = catalog.GetSegmentDuration(cue) / free.source.pitch;
        if (timed && duration > 0f && catalog.MatchesAnimation(cue))
            playbackDuration = Mathf.Min(playbackDuration, duration);
        free.startedAt = Time.unscaledTime;
        free.endTime = free.startedAt + playbackDuration;
        free.fadeIn = Mathf.Min(settings != null ? settings.fadeInSeconds : 0f, playbackDuration * 0.5f);
        free.fadeOut = Mathf.Min(settings != null ? settings.fadeOutSeconds : 0f, playbackDuration * 0.5f);
        free.source.timeSamples = Mathf.Clamp(Mathf.RoundToInt(catalog.GetSegmentStart(cue) * clip.frequency), 0, clip.samples - 1);
        ApplyVoiceMix(free);
        free.source.Play();
    }
    private void ApplyVoiceMix(Voice voice)
    {
        float envelope = voice.fadeIn > 0f ? Mathf.Clamp01((Time.unscaledTime - voice.startedAt) / voice.fadeIn) : 1f;
        if (voice.fadeOut > 0f) envelope *= Mathf.Clamp01((voice.endTime - Time.unscaledTime) / voice.fadeOut);
        voice.source.volume = GetCategoryVolume(GetCueCategory(voice.cue)) * voice.gain * gameplayGain * envelope;
        voice.lowPass.cutoffFrequency = voice.cue == GameAudioCue.PlayerDeath || voice.cue == GameAudioCue.PlayerRevive ? 22000f : cutoff;
        voice.reverb.enabled = reverbAmount > 0.01f;
        voice.reverb.reverbLevel = Mathf.Lerp(-2800f, 0f, Mathf.Clamp01(reverbAmount * 2f));
    }
    private static void Release(Voice voice)
    {
        voice.source.Stop();
        voice.source.clip = null;
        voice.source.volume = 0f;
        voice.active = voice.paused = false;
    }
    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = MusicVolume * musicGain * outgoingGain * (1f - crossfade);
        if (nextMusicSource != null) nextMusicSource.volume = MusicVolume * musicGain * crossfade;
        if (uiSfxSource != null) uiSfxSource.volume = GetCategoryVolume(SfxCategory.Ambience);
        if (ambienceSource != null) ambienceSource.volume = GetCategoryVolume(SfxCategory.Ambience) * ambienceGain;
        if (heartbeatSource != null) heartbeatSource.volume = GetCategoryVolume(SfxCategory.Combat) * heartbeatGain;
        foreach (Voice voice in voices) { if (voice.active) ApplyVoiceMix(voice); else voice.source.volume = 0f; }
    }
    public void EnterEnvironment(AudioEnvironmentZone zone) { if (zone != null && !zones.Contains(zone)) zones.Add(zone); }
    public void ExitEnvironment(AudioEnvironmentZone zone) => zones.Remove(zone);
    private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
    private void OnApplicationQuit() => PlayerPrefs.Save();
}
