using System.Collections.Generic;
using UnityEngine;

// One camera-frustum calculation and one allocation-free pass for all registered monsters.
[DefaultExecutionOrder(10000)]
public sealed class MonsterVisibilitySystem : MonoBehaviour
{
    private static readonly List<MonsterVisibilityCulling> monsters = new List<MonsterVisibilityCulling>();
    private static MonsterVisibilitySystem instance;
    private readonly Plane[] viewPlanes = new Plane[6];
    private Camera playerCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        monsters.Clear();
        instance = null;
    }

    internal static void Register(MonsterVisibilityCulling monster)
    {
        if (!Application.isPlaying)
            return;
        if (!monsters.Contains(monster))
            monsters.Add(monster);
        if (instance != null)
            return;

        GameObject host = new GameObject("Monster Visibility", typeof(MonsterVisibilitySystem));
        instance = host.GetComponent<MonsterVisibilitySystem>();
        DontDestroyOnLoad(host);
    }

    internal static void Unregister(MonsterVisibilityCulling monster) => monsters.Remove(monster);

    private void LateUpdate()
    {
        if (monsters.Count == 0)
            return;

        using (GameplayProfilerMarkers.MonsterVisibility.Auto())
        {
            if (playerCamera == null || !playerCamera.isActiveAndEnabled)
                playerCamera = Camera.main;

            bool hasCamera = playerCamera != null && playerCamera.isActiveAndEnabled;
            Bounds? orthographicView = null;
            if (hasCamera)
            {
                // The project's unrotated 2D camera needs only inexpensive AABB intersections.
                if (playerCamera.orthographic && playerCamera.transform.rotation == Quaternion.identity)
                {
                    Vector3 minimum = playerCamera.ViewportToWorldPoint(new Vector3(0f, 0f, playerCamera.nearClipPlane));
                    Vector3 maximum = playerCamera.ViewportToWorldPoint(new Vector3(1f, 1f, playerCamera.farClipPlane));
                    Bounds view = new Bounds();
                    view.SetMinMax(Vector3.Min(minimum, maximum), Vector3.Max(minimum, maximum));
                    orthographicView = view;
                }
                else
                    GeometryUtility.CalculateFrustumPlanes(playerCamera, viewPlanes);
            }

            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                MonsterVisibilityCulling monster = monsters[i];
                if (monster == null)
                    monsters.RemoveAt(i);
                else
                    monster.RefreshVisibility(hasCamera ? viewPlanes : null, orthographicView);
            }
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
