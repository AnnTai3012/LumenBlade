using UnityEngine;

[CreateAssetMenu(menuName = "Game/Performance Settings")]
public sealed class GamePerformanceSettings : ScriptableObject
{
    [SerializeField, Range(30, 240)] private int targetFrameRate = 60;
    [SerializeField, Range(30, 120)] private int physicsFrequency = 60;
    [SerializeField, Range(0.05f, 0.2f)] private float maximumPhysicsCatchUp = 0.1f;
    [SerializeField] private bool useVSyncWhenCompatible = true;

    public int TargetFrameRate => Mathf.Clamp(targetFrameRate, 30, 240);
    public int PhysicsFrequency => Mathf.Clamp(physicsFrequency, 30, 120);
    public float MaximumPhysicsCatchUp => Mathf.Max(1f / PhysicsFrequency, Mathf.Clamp(maximumPhysicsCatchUp, 0.05f, 0.2f));
    public bool UseVSyncWhenCompatible => useVSyncWhenCompatible;
}
