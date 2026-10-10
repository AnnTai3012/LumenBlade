using UnityEngine;

public enum GameAudioCue
{
    UiButtonClick,
    SettingsOpen,
    SettingsClose,
    StoreOpen,
    StoreClose,
    BlessingPanelOpen,
    RestartPanelOpen,
    PlayerAttack,
    PlayerJumpAttack,
    PlayerSkill1,
    PlayerSkill2,
    PlayerTakeHit,
    PlayerDeath,
    PlayerFallImpact,
    PlayerJump,
    PlayerDash,
    PlayerFootstep,
    HealItemUse,
    ShieldItemUse,
    MonsterAttack1,
    MonsterAttack2,
    MonsterTakeHit,
    MonsterDeath,
    BossShootAttack,
    BossMagicAttack,
    BossLaserAttack,
    BossMeleeAttack,
    BossShield,
    BossTakeHit,
    BossDeath,
    BossAppear,
    ItemReceived,
    KeyPickup,
    BlessingReceived,
    LevelTransition,
    ChestOpen,
    PlayerSkill3,
    PlayerRevive,
    ExitGateOpen,
    SlimeAttack,
    SlimeTakeHit,
    DemonSlimeAttack,
    MimicAttack2,
    DemonSlimeTakeHit,
    DemonSlimeDeath,
    BossVictory,
    ShieldBreak,
    PlayerRestart,
    ShieldHit = 51
}

[System.Serializable]
public sealed class SkillSoundTiming
{
    [Tooltip("Automatically match sound duration to the skill animation.")]
    public bool matchAnimation = true;
    [Range(0.1f, 3f), Tooltip("1 keeps automatic alignment. Disable Match Animation to use this as manual playback speed.")]
    public float speedMultiplier = 1f;
}

[CreateAssetMenu(fileName = "Game Audio Catalog", menuName = "BTL/Audio/Game Audio Catalog")]
public sealed class GameAudioCatalog : ScriptableObject
{
    [Header("Music")]
    public AudioClip backgroundMusic;
    public AudioClip menuMusic;
    public AudioClip bossMusic;
    public AudioClip shopMusic;
    public AudioClip caveAmbience;
    public AudioClip lowHealthHeartbeat;
    [Header("Dynamic mix")]
    [Range(0.05f, 5f)] public float transitionSeconds = 1.25f;
    [Min(0f), Tooltip("Seconds of gameplay after boss defeat before background music returns. Pausing freezes this delay.")]
    public float bossBackgroundDelay = 10f;
    [Range(0.05f, 0.5f)] public float lowHealthThreshold = 0.25f;
    [Range(0.05f, 0.75f)] public float bossPressureThreshold = 0.35f;
    public AudioSceneProfile[] sceneProfiles = System.Array.Empty<AudioSceneProfile>();
    public AudioCueSettings[] cueSettings = System.Array.Empty<AudioCueSettings>();
    [Header("UI")]
    public AudioClip uiButtonClick;
    public AudioClip settingsOpen;
    public AudioClip settingsClose;
    public AudioClip storeOpen;
    public AudioClip storeClose;
    public AudioClip blessingPanelOpen;
    public AudioClip restartPanelOpen;

    [Header("Player combat and movement")]
    public AudioClip playerAttack;
    public AudioClip playerJumpAttack;
    public AudioClip playerSkill1;
    public AudioClip playerSkill2;
    public AudioClip playerSkill3;
    public AudioClip playerRevive;
    [Header("Skill sound speed")]
    public SkillSoundTiming skill1Timing = new SkillSoundTiming();
    public SkillSoundTiming skill2Timing = new SkillSoundTiming();
    public SkillSoundTiming skill3Timing = new SkillSoundTiming();
    [Header("Player damage and movement")]
    public AudioClip playerTakeHit;
    public AudioClip playerDeath;
    public AudioClip playerFallImpact;
    public AudioClip playerJump;
    public AudioClip playerDash;
    public AudioClip playerFootstep;
    public AudioClip healItemUse;
    public AudioClip shieldItemUse;

    [Header("Enemies")]
    public AudioClip monsterAttack1;
    public AudioClip monsterAttack2;
    public AudioClip monsterTakeHit;
    public AudioClip monsterDeath;
    public AudioClip slimeAttack;
    public AudioClip slimeTakeHit;
    public AudioClip demonSlimeAttack;
    public AudioClip mimicAttack2;
    public AudioClip demonSlimeTakeHit;
    public AudioClip demonSlimeDeath;
    public AudioClip bossVictory;
    public AudioClip shieldBreak;
    public AudioClip shieldHit;
    public AudioClip playerRestart;

    [Header("Boss")]
    public AudioClip bossShootAttack;
    public AudioClip bossMagicAttack;
    public AudioClip bossLaserAttack;
    public AudioClip bossMeleeAttack;
    public AudioClip bossShield;
    public AudioClip bossTakeHit;
    public AudioClip bossDeath;
    public AudioClip bossAppear;

    [Header("Items and level flow")]
    public AudioClip itemReceived;
    public AudioClip keyPickup;
    public AudioClip blessingReceived;
    public AudioClip levelTransition;
    public AudioClip chestOpen;
    public AudioClip exitGateOpen;

    public float ResolveSkillPitch(GameAudioCue cue, float animationDuration)
    {
        SkillSoundTiming timing = cue == GameAudioCue.PlayerSkill1 ? skill1Timing :
            cue == GameAudioCue.PlayerSkill2 ? skill2Timing : cue == GameAudioCue.PlayerSkill3 ? skill3Timing : null;
        AudioClip clip = Resolve(cue);
        AudioCueSettings settings = ResolveSettings(cue);
        bool match = timing != null ? timing.matchAnimation : settings == null || settings.matchAnimation;
        float pitch = clip != null && animationDuration > 0f && match
            ? GetSegmentDuration(cue) / animationDuration : 1f;
        return Mathf.Clamp(pitch * (timing != null ? timing.speedMultiplier : settings != null ? settings.speedMultiplier : 1f), 0.01f, 3f);
    }

    public float GetSegmentStart(GameAudioCue cue)
    {
        AudioClip clip = Resolve(cue);
        AudioCueSettings settings = ResolveSettings(cue);
        return clip != null ? Mathf.Clamp(settings != null ? settings.startSeconds : 0f, 0f, Mathf.Max(0f, clip.length - 0.001f)) : 0f;
    }

    public float GetSegmentDuration(GameAudioCue cue)
    {
        AudioClip clip = Resolve(cue);
        if (clip == null) return 0f;
        float remaining = clip.length - GetSegmentStart(cue);
        AudioCueSettings settings = ResolveSettings(cue);
        return settings != null && settings.segmentSeconds > 0f ? Mathf.Min(settings.segmentSeconds, remaining) : remaining;
    }

    public bool MatchesAnimation(GameAudioCue cue)
    {
        SkillSoundTiming timing = cue == GameAudioCue.PlayerSkill1 ? skill1Timing :
            cue == GameAudioCue.PlayerSkill2 ? skill2Timing : cue == GameAudioCue.PlayerSkill3 ? skill3Timing : null;
        return timing != null ? timing.matchAnimation : ResolveSettings(cue)?.matchAnimation != false;
    }

    public AudioClip Resolve(GameAudioCue cue)
    {
        return cue switch
        {
            GameAudioCue.UiButtonClick => uiButtonClick,
            GameAudioCue.SettingsOpen => settingsOpen,
            GameAudioCue.SettingsClose => settingsClose,
            GameAudioCue.StoreOpen => storeOpen,
            GameAudioCue.StoreClose => storeClose,
            GameAudioCue.BlessingPanelOpen => blessingPanelOpen,
            GameAudioCue.RestartPanelOpen => restartPanelOpen,
            GameAudioCue.PlayerAttack => playerAttack,
            GameAudioCue.PlayerJumpAttack => playerJumpAttack,
            GameAudioCue.PlayerSkill1 => playerSkill1,
            GameAudioCue.PlayerSkill2 => playerSkill2,
            GameAudioCue.PlayerSkill3 => playerSkill3,
            GameAudioCue.PlayerRevive => playerRevive,
            GameAudioCue.PlayerTakeHit => playerTakeHit,
            GameAudioCue.PlayerDeath => playerDeath,
            GameAudioCue.PlayerFallImpact => playerFallImpact,
            GameAudioCue.PlayerJump => playerJump,
            GameAudioCue.PlayerDash => playerDash,
            GameAudioCue.PlayerFootstep => playerFootstep,
            GameAudioCue.HealItemUse => healItemUse,
            GameAudioCue.ShieldItemUse => shieldItemUse,
            GameAudioCue.MonsterAttack1 => monsterAttack1,
            GameAudioCue.MonsterAttack2 => monsterAttack2,
            GameAudioCue.MonsterTakeHit => monsterTakeHit,
            GameAudioCue.MonsterDeath => monsterDeath,
            GameAudioCue.SlimeAttack => slimeAttack,
            GameAudioCue.SlimeTakeHit => slimeTakeHit,
            GameAudioCue.DemonSlimeAttack => demonSlimeAttack,
            GameAudioCue.MimicAttack2 => mimicAttack2,
            GameAudioCue.DemonSlimeTakeHit => demonSlimeTakeHit,
            GameAudioCue.DemonSlimeDeath => demonSlimeDeath,
            GameAudioCue.BossVictory => bossVictory,
            GameAudioCue.ShieldBreak => shieldBreak,
            GameAudioCue.ShieldHit => shieldHit,
            GameAudioCue.PlayerRestart => playerRestart,
            GameAudioCue.BossShootAttack => bossShootAttack,
            GameAudioCue.BossMagicAttack => bossMagicAttack,
            GameAudioCue.BossLaserAttack => bossLaserAttack,
            GameAudioCue.BossMeleeAttack => bossMeleeAttack,
            GameAudioCue.BossShield => bossShield,
            GameAudioCue.BossTakeHit => bossTakeHit,
            GameAudioCue.BossDeath => bossDeath,
            GameAudioCue.BossAppear => bossAppear,
            GameAudioCue.ItemReceived => itemReceived,
            GameAudioCue.KeyPickup => keyPickup,
            GameAudioCue.BlessingReceived => blessingReceived,
            GameAudioCue.LevelTransition => levelTransition,
            GameAudioCue.ChestOpen => chestOpen,
            GameAudioCue.ExitGateOpen => exitGateOpen,
            _ => null
        };
    }

    public AudioSceneProfile ResolveScene(string sceneName)
    {
        foreach (AudioSceneProfile profile in sceneProfiles)
            if (profile != null && profile.sceneName == sceneName) return profile;
        return null;
    }

    public AudioCueSettings ResolveSettings(GameAudioCue cue)
    {
        foreach (AudioCueSettings settings in cueSettings)
            if (settings != null && settings.cue == cue) return settings;
        return null;
    }
}

public enum GameAudioEnvironment { Cave, Sanctuary, Arena, Outdoors }
public enum GameAudioState { Exploration, BossCombat, BossPressure, LowHealth, Paused, Dead }

[System.Serializable]
public sealed class AudioSceneProfile
{
    public string sceneName;
    public GameAudioEnvironment environment;
    public AudioClip music;
    [Range(0f, 1f)] public float ambienceVolume = 0.12f;
    [Range(0f, 1f)] public float reverb = 0.2f;
}

[System.Serializable]
public sealed class AudioCueSettings
{
    public GameAudioCue cue;
    [Range(0f, 1f)] public float volume = 0.7f;
    [Range(0.5f, 2f)] public float minPitch = 1f;
    [Range(0.5f, 2f)] public float maxPitch = 1f;
    [Min(0f)] public float cooldown;
    [Range(1, 8)] public int maxVoices = 3;
    [Range(0, 256)] public int priority = 128;
    [Header("Animation timing (gameplay cues)")]
    public bool matchAnimation = true;
    [Min(0f), Tooltip("Start position inside the source clip, in seconds.")]
    public float startSeconds;
    [Min(0f), Tooltip("Source segment length; 0 uses the rest of the clip.")]
    public float segmentSeconds;
    [Range(0.1f, 3f)] public float speedMultiplier = 1f;
    [Min(0f)] public float fadeInSeconds;
    [Min(0f)] public float fadeOutSeconds;
}
