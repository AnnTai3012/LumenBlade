using Unity.Profiling;

internal static class GameplayProfilerMarkers
{
    internal static readonly ProfilerMarker PlayerMovement = new ProfilerMarker("Gameplay.Player.Movement");
    internal static readonly ProfilerMarker PlayerGroundCheck = new ProfilerMarker("Gameplay.Player.GroundCheck");
    internal static readonly ProfilerMarker CombatQuery = new ProfilerMarker("Gameplay.Combat.Query");
    internal static readonly ProfilerMarker SkillPulse = new ProfilerMarker("Gameplay.Skill.Pulse");
    internal static readonly ProfilerMarker Damage = new ProfilerMarker("Gameplay.Damage.Apply");
    internal static readonly ProfilerMarker MonsterVisibility = new ProfilerMarker("Gameplay.Monster.Visibility");
}
