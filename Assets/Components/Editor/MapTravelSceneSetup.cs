using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Author collision shapes and chapter links through Unity, keeping scene GUIDs intact.
public static class MapTravelSceneSetup
{
    private const string ExitPrefabPath = "Assets/Objects/Items/EntranceForward.prefab";
    private const string ReturnPrefabPath = "Assets/Objects/Items/EntranceReturn.prefab";
    private const string OpenClipPath = "Assets/Objects/Environment/AnimEntrance/Open.anim";
    private static readonly string[] EnemyPrefabs =
    {
        "Assets/Objects/Enemies/Slime.prefab", "Assets/Objects/Enemies/Bat.prefab", "Assets/Objects/Enemies/Rat.prefab", "Assets/Objects/Enemies/Minic.prefab",
        "Assets/Objects/Bosses/DemonSlime/DemonSlime (1).prefab", "Assets/Objects/Bosses/Golem/Golem 1.prefab", "Assets/Objects/Bosses/BringerOfDeath/Bringer of Death.prefab"
    };

    private sealed class MapDefinition
    {
        public string Path;
        public int Chapter;
        public string Target;
        public string Previous;
    }

    [MenuItem("Tools/BTL/Install Chapter Return Doors and Triggers")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install map travel outside Play Mode.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save the current scenes before configuring chapter travel.");

        try
        {
            Sprite openSprite = GetOpenSprite();
            foreach (string path in EnemyPrefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigureEnemyColliders(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            ConfigureDoorPrefabs(openSprite);
            List<MapDefinition> maps = ReadMaps();
            ResolveChapterLinks(maps);
            GameObject returnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReturnPrefabPath);
            foreach (MapDefinition map in maps)
            {
                Scene scene = EditorSceneManager.OpenScene(map.Path, OpenSceneMode.Single);
                foreach (MonsterHealth enemy in ComponentsInScene<MonsterHealth>(scene))
                    ConfigureEnemyColliders(enemy.gameObject);

                foreach (Tilemap terrain in ComponentsInScene<Tilemap>(scene))
                {
                    if (terrain.GetComponents<Collider2D>().Any(c => !c.isTrigger))
                        terrain.gameObject.layer = LayerMask.NameToLayer("Ground");
                }
                SplitSpikeTilemaps(scene);
                Physics2D.SyncTransforms();
                Health player = ComponentsInScene<Health>(scene).First();
                foreach (LevelExitDoor exit in ComponentsInScene<LevelExitDoor>(scene).Where(d => d.DoorId != "Entrance"))
                {
                    ConfigureDoor(exit, "Exit", "Entrance", openSprite);
                    if (!string.IsNullOrEmpty(map.Target))
                        SetString(exit, "targetSceneName", System.IO.Path.GetFileNameWithoutExtension(map.Target));
                    float direction = Mathf.Sign(player.transform.position.x - exit.transform.position.x);
                    Vector3 spawn = FindPlayerSpawn(exit.transform.position, direction == 0f ? -1f : direction, player);
                    SetVector(exit, "arrivalOffset", exit.transform.InverseTransformPoint(spawn));
                }

                if (!string.IsNullOrEmpty(map.Previous))
                {
                    LevelExitDoor entrance = ComponentsInScene<LevelExitDoor>(scene).FirstOrDefault(d => d.DoorId == "Entrance");
                    if (entrance == null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(returnPrefab, scene);
                        instance.name = "Entrance Return";
                        entrance = instance.GetComponent<LevelExitDoor>();
                        PositionReturnDoor(entrance, player);
                    }
                    ConfigureDoor(entrance, "Entrance", "Exit", openSprite);
                    SetString(entrance, "targetSceneName", System.IO.Path.GetFileNameWithoutExtension(map.Previous));
                    SetBool(entrance, "requireKey", false);
                    SetBool(entrance, "offerBlessingOnArrival", false);
                    SetBool(entrance, "startOpen", true);
                    entrance.GetComponent<Animator>().enabled = false;
                    entrance.GetComponent<SpriteRenderer>().sprite = openSprite;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(entrance.GetComponent<Animator>());
                    PrefabUtility.RecordPrefabInstancePropertyModifications(entrance.GetComponent<SpriteRenderer>());
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"Could not save '{map.Path}'.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Configured trigger hurtboxes, spike triggers and return travel for {maps.Count} gameplay scenes.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static List<MapDefinition> ReadMaps()
    {
        var maps = new List<MapDefinition>();
        foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Objects/World/Scenes" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            LevelResourceFlow flow = ComponentsInScene<LevelResourceFlow>(scene).FirstOrDefault();
            if (flow == null || !ComponentsInScene<Health>(scene).Any())
                continue;
            LevelExitDoor exit = ComponentsInScene<LevelExitDoor>(scene).FirstOrDefault(d => d.DoorId != "Entrance");
            maps.Add(new MapDefinition { Path = path, Chapter = flow.ChapterIndex, Target = exit != null ? exit.TargetSceneName : null });
        }
        return maps;
    }

    private static void ResolveChapterLinks(List<MapDefinition> maps)
    {
        foreach (MapDefinition source in maps.Where(m => !string.IsNullOrEmpty(m.Target)))
        {
            MapDefinition target = maps.FirstOrDefault(m => m.Path == source.Target || System.IO.Path.GetFileNameWithoutExtension(m.Path) == source.Target);
            // Support the project's Purchasing Levels -> Purchasing LevelsC1 scene rename.
            if (target == null)
                target = maps.FirstOrDefault(m => m.Chapter == source.Chapter && System.IO.Path.GetFileNameWithoutExtension(m.Path) == source.Target + "C" + source.Chapter);
            if (target == null)
                throw new InvalidOperationException($"Door in '{source.Path}' has an unresolved target '{source.Target}'.");
            source.Target = target.Path;
            if (target.Chapter != source.Chapter)
                continue;
            if (target.Previous != null && target.Previous != source.Path)
                throw new InvalidOperationException($"'{target.Path}' has multiple previous maps. Configure its return door explicitly.");
            target.Previous = source.Path;
        }
    }

    private static Sprite GetOpenSprite()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OpenClipPath);
        EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite");
        return (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, binding).Last().value;
    }

    private static void ConfigureDoorPrefabs(Sprite openSprite)
    {
        GameObject exit = PrefabUtility.LoadPrefabContents(ExitPrefabPath);
        try
        {
            exit.name = "Entrance Forward";
            ConfigureDoor(exit.GetComponent<LevelExitDoor>(), "Exit", "Entrance", openSprite);
            PrefabUtility.SaveAsPrefabAsset(exit, ExitPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(exit); }

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ReturnPrefabPath);
        GameObject root = existing != null ? PrefabUtility.LoadPrefabContents(ReturnPrefabPath) : new GameObject("Entrance Return");
        try
        {
            root.name = "Entrance Return";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
            if (root.GetComponent<SpriteRenderer>() == null)
                EditorUtility.CopySerialized(source.GetComponent<SpriteRenderer>(), root.AddComponent<SpriteRenderer>());
            Animator animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = source.GetComponent<Animator>().runtimeAnimatorController;
            animator.enabled = false;
            BoxCollider2D box = root.GetComponent<BoxCollider2D>();
            if (box == null) box = root.AddComponent<BoxCollider2D>();
            box.size = source.GetComponent<BoxCollider2D>().size;
            box.offset = source.GetComponent<BoxCollider2D>().offset;
            LevelExitDoor door = root.GetComponent<LevelExitDoor>();
            if (door == null) door = root.AddComponent<LevelExitDoor>();
            ConfigureDoor(door, "Entrance", "Exit", openSprite);
            SetBool(door, "requireKey", false);
            SetBool(door, "offerBlessingOnArrival", false);
            SetBool(door, "startOpen", true);
            SetFloat(door, "openAnimationDuration", 0f);
            SetString(door, "targetSceneName", "Level 1");
            root.GetComponent<SpriteRenderer>().sprite = openSprite;
            PrefabUtility.SaveAsPrefabAsset(root, ReturnPrefabPath);
        }
        finally
        {
            if (existing != null) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigureDoor(LevelExitDoor door, string id, string targetId, Sprite openSprite)
    {
        BoxCollider2D collider = door.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        EditorUtility.SetDirty(collider);
        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        SetString(door, "doorId", id);
        SetString(door, "targetDoorId", targetId);
        bool returning = id == "Entrance";
        SetBool(door, "enterOnTouch", !returning);
        var role = new SerializedObject(door);
        role.FindProperty("direction").enumValueIndex = returning ? (int)LevelDoorDirection.Return : (int)LevelDoorDirection.Forward;
        role.ApplyModifiedPropertiesWithoutUndo();
        var serialized = new SerializedObject(door);
        serialized.FindProperty("openSprite").objectReferenceValue = openSprite;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(door);
        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
    }

    private static void ConfigureEnemyColliders(GameObject root)
    {
        Collider2D hurtbox = root.GetComponent<Collider2D>();
        if (hurtbox == null)
            throw new InvalidOperationException($"Enemy '{root.name}' needs a body collider.");
        hurtbox.isTrigger = true;
        EditorUtility.SetDirty(hurtbox);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hurtbox);
        Transform child = root.transform.Find("Ground Collider");
        if (child == null)
        {
            child = new GameObject("Ground Collider").transform;
            child.SetParent(root.transform, false);
        }
        child.gameObject.layer = root.layer;
        Collider2D floor = child.GetComponent<Collider2D>();
        if (floor == null) floor = (Collider2D)child.gameObject.AddComponent(hurtbox.GetType());
        floor.offset = hurtbox.offset;
        floor.sharedMaterial = hurtbox.sharedMaterial;
        if (root.GetComponent<Rigidbody2D>().useAutoMass)
            floor.density = hurtbox.density;
        if (hurtbox is BoxCollider2D box && floor is BoxCollider2D floorBox)
        {
            floorBox.size = box.size;
            floorBox.edgeRadius = box.edgeRadius;
        }
        else if (hurtbox is CircleCollider2D circle && floor is CircleCollider2D floorCircle)
            floorCircle.radius = circle.radius;
        else if (hurtbox is CapsuleCollider2D capsule && floor is CapsuleCollider2D floorCapsule)
        {
            floorCapsule.size = capsule.size;
            floorCapsule.direction = capsule.direction;
        }
        else
            throw new InvalidOperationException($"Unsupported enemy collider on '{root.name}'.");
        int ground = LayerMask.GetMask("Ground");
        if (ground == 0)
            throw new InvalidOperationException("The Ground layer is required for enemy floor collision.");
        floor.isTrigger = false;
        floor.enabled = true;
        floor.includeLayers = ground;
        floor.excludeLayers = ~ground;
        floor.callbackLayers = ground;
        floor.layerOverridePriority = 1;
        EditorUtility.SetDirty(floor);
        PrefabUtility.RecordPrefabInstancePropertyModifications(floor);
    }

    private static void SplitSpikeTilemaps(Scene scene)
    {
        foreach (Tilemap source in ComponentsInScene<Tilemap>(scene).ToArray())
        {
            if (source.name.EndsWith(" Spike Triggers", StringComparison.Ordinal))
                continue;
            var cells = new List<Vector3Int>();
            foreach (Vector3Int cell in source.cellBounds.allPositionsWithin)
            {
                TileBase tile = source.GetTile(cell);
                if (tile != null && string.Equals(tile.name, "trap", StringComparison.OrdinalIgnoreCase)) cells.Add(cell);
            }
            if (cells.Count == 0)
                continue;
            Transform sibling = source.transform.parent.Find(source.name + " Spike Triggers");
            Tilemap spikes;
            if (sibling == null)
            {
                var root = new GameObject(source.name + " Spike Triggers", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
                root.transform.SetParent(source.transform.parent, false);
                root.transform.localPosition = source.transform.localPosition;
                root.transform.localRotation = source.transform.localRotation;
                root.transform.localScale = source.transform.localScale;
                spikes = root.GetComponent<Tilemap>();
                spikes.tileAnchor = source.tileAnchor;
                spikes.orientation = source.orientation;
                spikes.orientationMatrix = source.orientationMatrix;
                spikes.color = source.color;
                EditorUtility.CopySerialized(source.GetComponent<TilemapRenderer>(), root.GetComponent<TilemapRenderer>());
                root.GetComponent<TilemapCollider2D>().isTrigger = true;
            }
            else spikes = sibling.GetComponent<Tilemap>();
            Transform floors = spikes.transform.Find("Spike Floors");
            if (floors == null)
            {
                floors = new GameObject("Spike Floors").transform;
                floors.SetParent(spikes.transform, false);
            }
            foreach (Vector3Int cell in cells)
            {
                TileBase tile = source.GetTile(cell);
                Matrix4x4 matrix = source.GetTransformMatrix(cell);
                Color color = source.GetColor(cell);
                TileFlags flags = source.GetTileFlags(cell);
                spikes.SetTile(cell, tile);
                spikes.SetTileFlags(cell, TileFlags.None);
                spikes.SetTransformMatrix(cell, matrix);
                spikes.SetColor(cell, color);
                spikes.SetTileFlags(cell, flags);

                // Keep a thin, invisible supporting floor below the damage trigger.
                // This retains standing damage without treating the spike tips as solid ground.
                string floorName = $"Spike Floor {cell.x} {cell.y}";
                if (floors.Find(floorName) == null)
                {
                    Sprite sprite = source.GetSprite(cell);
                    Vector3 center = source.GetCellCenterLocal(cell) + matrix.MultiplyPoint3x4(sprite.bounds.center);
                    Vector3 extents = matrix.MultiplyVector(sprite.bounds.extents);
                    float height = Mathf.Max(0.01f, Mathf.Abs(extents.y) * 0.2f);
                    var floor = new GameObject(floorName, typeof(BoxCollider2D));
                    floor.layer = LayerMask.NameToLayer("Ground");
                    floor.transform.SetParent(floors, false);
                    floor.transform.localPosition = new Vector3(center.x, center.y - Mathf.Abs(extents.y) + height * 0.5f, center.z);
                    floor.GetComponent<BoxCollider2D>().size = new Vector2(Mathf.Max(0.01f, Mathf.Abs(extents.x) * 2f), height);
                }
                source.SetTile(cell, null);
            }
            spikes.CompressBounds();
            spikes.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            TilemapCollider2D sourceCollider = source.GetComponent<TilemapCollider2D>();
            if (sourceCollider != null) sourceCollider.ProcessTilemapChanges();
        }
    }

    private static void PositionReturnDoor(LevelExitDoor door, Health player)
    {
        Vector3 spawn = FindPlayerSpawn(player.transform.position, 0f, player);
        Sprite sprite = door.GetComponent<SpriteRenderer>().sprite;
        float bottomOffset = player.GetComponent<Collider2D>().bounds.min.y - player.transform.position.y;
        float floorY = spawn.y + bottomOffset;
        foreach (float offset in new[] { -1.35f, 1.35f, -1.7f, 1.7f })
        {
            float x = spawn.x + offset;
            if (!TryFindFloor(new Vector2(x, spawn.y + 1f), out RaycastHit2D hit) || Mathf.Abs(hit.point.y - floorY) > 0.4f)
                continue;
            Vector2 boxSize = door.GetComponent<BoxCollider2D>().size;
            Vector2 center = new Vector2(x, hit.point.y + boxSize.y * 0.5f + 0.02f);
            if (HasSolidOverlap(center, boxSize * 0.95f))
                continue;
            door.transform.position = new Vector3(x, hit.point.y - sprite.bounds.min.y + 0.02f, spawn.z);
            SetVector(door, "arrivalOffset", door.transform.InverseTransformPoint(spawn));
            return;
        }
        throw new InvalidOperationException($"No safe return-door position near the start of '{player.gameObject.scene.path}'.");
    }

    private static Vector3 FindPlayerSpawn(Vector3 origin, float preferredDirection, Health player)
    {
        Collider2D collider = player.GetComponent<Collider2D>();
        Vector3 centerOffset = collider.bounds.center - player.transform.position;
        float bottomOffset = collider.bounds.min.y - player.transform.position.y;
        float[] offsets = preferredDirection == 0f ? new[] { 0f, -0.5f, 0.5f, -1f, 1f } :
            new[] { preferredDirection * 1.35f, -preferredDirection * 1.35f, preferredDirection * 1.8f, -preferredDirection * 1.8f, preferredDirection * 2.3f, -preferredDirection * 2.3f };
        foreach (float offset in offsets)
        {
            if (!TryFindFloor(new Vector2(origin.x + offset, origin.y + 1f), out RaycastHit2D hit))
                continue;
            Vector3 spawn = new Vector3(origin.x + offset, hit.point.y - bottomOffset + 0.025f, origin.z);
            if (!HasSolidOverlap((Vector2)(spawn + centerOffset), (Vector2)collider.bounds.size * 0.95f))
                return spawn;
        }
        throw new InvalidOperationException($"No safe arrival spawn near {origin} in '{player.gameObject.scene.path}'.");
    }

    private static bool TryFindFloor(Vector2 origin, out RaycastHit2D hit)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(LayerMask.GetMask("Ground"));
        var hits = new RaycastHit2D[8];
        int count = Physics2D.Raycast(origin, Vector2.down, filter, hits, 8f);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].normal.y <= 0.5f)
                continue;
            hit = hits[i];
            return true;
        }
        hit = default;
        return false;
    }

    private static bool HasSolidOverlap(Vector2 center, Vector2 size)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(LayerMask.GetMask("Ground"));
        return Physics2D.OverlapBox(center, size, 0f, filter, new Collider2D[8]) > 0;
    }

    private static IEnumerable<T> ComponentsInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

    private static void SetString(UnityEngine.Object target, string field, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void SetBool(UnityEngine.Object target, string field, bool value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void SetFloat(UnityEngine.Object target, string field, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void SetVector(UnityEngine.Object target, string field, Vector2 value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
