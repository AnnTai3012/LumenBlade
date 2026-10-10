using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Wiring is authored through Unity so scene instances inherit the shared Player prefab.
public static class PlayerSkill3Setup
{
    private const string PlayerPath = "Assets/Objects/Player/Player.prefab";
    private const string ControllerPath = "Assets/Objects/Player/Sprite/Player.controller";
    private const string ClipPath = "Assets/Objects/Player/Sprite/Anim/Skill_3.anim";

    [MenuItem("Tools/BTL/Configure Ranged Skill 3 and Blessing Caps")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure Skill 3 outside Play Mode.");
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Objects/Player/Sprite/skill3.png")
            .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        if (sprites.Length != 9)
            throw new InvalidOperationException("Skill 3 needs the nine authored sprite frames.");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Skill_3" };
            AssetDatabase.CreateAsset(clip, ClipPath);
        }
        clip.frameRate = 12f;
        var keys = sprites.Select((sprite, i) => new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprite }).ToList();
        float duration = sprites.Length / clip.frameRate;
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.startTime = 0f;
        clipSettings.stopTime = duration;
        clipSettings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!controller.parameters.Any(p => p.name == "Skill3")) controller.AddParameter("Skill3", AnimatorControllerParameterType.Trigger);
        if (!controller.parameters.Any(p => p.name == "IsUsingSkill")) controller.AddParameter("IsUsingSkill", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Skill_3");
        if (state == null) state = machine.AddState("Skill_3", new Vector3(580f, 250f, 0f));
        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;
        if (!machine.anyStateTransitions.Any(t => t.destinationState == state))
        {
            var entry = machine.AddAnyStateTransition(state);
            entry.hasExitTime = false;
            entry.duration = 0f;
            entry.canTransitionToSelf = false;
            entry.AddCondition(AnimatorConditionMode.If, 0f, "Skill3");
            entry.AddCondition(AnimatorConditionMode.IfNot, 0f, "Death");
        }
        if (state.transitions.Length == 0)
        {
            AddExit(state, machine, "Fall", "IsGrounded", AnimatorConditionMode.IfNot);
            AddExit(state, machine, "Run", "Speed", AnimatorConditionMode.Greater);
            AddExit(state, machine, "idle", null, AnimatorConditionMode.If);
        }
        foreach (var transition in machine.anyStateTransitions)
            if (transition.destinationState != null && transition.destinationState.name == "Jump" &&
                !transition.conditions.Any(c => c.parameter == "IsUsingSkill"))
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsUsingSkill");
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);

        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            Transform child = player.transform.Find("Skill3Hitbox");
            if (child == null)
            {
                child = new GameObject("Skill3Hitbox").transform;
                child.SetParent(player.transform, false);
                child.gameObject.layer = player.layer;
            }
            BoxCollider2D box = child.GetComponent<BoxCollider2D>();
            if (box == null) box = child.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.enabled = false;
            Skill3Hitbox hitbox = child.GetComponent<Skill3Hitbox>();
            if (hitbox == null) hitbox = child.gameObject.AddComponent<Skill3Hitbox>();
            var hitboxData = new SerializedObject(hitbox);
            var frames = hitboxData.FindProperty("frames");
            // White wave bounds, in pixels within each of the seven damaging sprite rectangles.
            int[,] bounds = { { 1, 5, 108, 70 }, { 9, 6, 158, 71 }, { 110, 4, 226, 69 },
                { 238, 8, 321, 73 }, { 280, 7, 351, 71 }, { 304, 10, 350, 68 }, { 338, 26, 351, 58 } };
            frames.arraySize = 7;
            for (int i = 0; i < frames.arraySize; i++)
            {
                Sprite sprite = sprites[i + 2];
                float ppu = sprite.pixelsPerUnit;
                float xMin = Mathf.Max(0.3f, (bounds[i, 0] - sprite.pivot.x) / ppu);
                float xMax = (bounds[i, 2] - sprite.pivot.x) / ppu;
                float yMin = (bounds[i, 1] - sprite.pivot.y) / ppu;
                float yMax = (bounds[i, 3] - sprite.pivot.y) / ppu;
                var frame = frames.GetArrayElementAtIndex(i);
                frame.FindPropertyRelative("normalizedTime").floatValue = (i + 2f) / (clip.frameRate * clip.length);
                frame.FindPropertyRelative("offset").vector2Value = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
                frame.FindPropertyRelative("size").vector2Value = new Vector2(xMax - xMin, yMax - yMin);
                if (i == 0)
                {
                    box.offset = frame.FindPropertyRelative("offset").vector2Value;
                    box.size = frame.FindPropertyRelative("size").vector2Value;
                }
            }
            hitboxData.ApplyModifiedPropertiesWithoutUndo();
            var combat = new SerializedObject(player.GetComponent<PlayerCombat>());
            combat.FindProperty("skill3Clip").objectReferenceValue = clip;
            combat.FindProperty("skill3Hitbox").objectReferenceValue = hitbox;
            combat.FindProperty("lockMovementWhileUsingSkill3").boolValue = true;
            combat.ApplyModifiedPropertiesWithoutUndo();
            var input = new SerializedObject(player.GetComponent<PlayerInputReader>());
            input.FindProperty("skill3Key").intValue = (int)KeyCode.U;
            input.ApplyModifiedPropertiesWithoutUndo();
            var stamina = new SerializedObject(player.GetComponent<PlayerStamina>());
            stamina.FindProperty("maxStamina").floatValue = ChapterBalance.PlayerStamina(1);
            stamina.FindProperty("skill3CastsPerBaseBar").intValue = 6;
            stamina.ApplyModifiedPropertiesWithoutUndo();
            var damage = new SerializedObject(player.GetComponent<SkillDamageController>());
            var damageList = damage.FindProperty("skill3DamagePerHit");
            damageList.arraySize = 1;
            damageList.GetArrayElementAtIndex(0).intValue = 24;
            damage.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }

        var settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>("Assets/Components/UI/Gameplay/Resources and UI Settings.asset");
        var config = new SerializedObject(settings);
        config.FindProperty("damageBonusPerBlessing").floatValue = RunResources.StatBonusPerBlessing;
        config.FindProperty("maxDamageBonus").floatValue = 1f;
        config.ApplyModifiedPropertiesWithoutUndo();
        foreach (var blessing in settings.Blessings)
        {
            if (blessing.reward == BlessingReward.MaxHealth)
                blessing.description = "+5% HP gốc của Chapter.\nTối đa +100% (20 lần).";
            else if (blessing.reward == BlessingReward.MaxStamina)
            {
                blessing.title = "Thể lực";
                blessing.description = "+5% stamina gốc của Chapter.\nTối đa +100% (20 lần).";
            }
        }
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);
    }

    private static void AddExit(AnimatorState source, AnimatorStateMachine machine, string destination,
        string parameter, AnimatorConditionMode mode)
    {
        var target = machine.states.Select(s => s.state).First(s => s.name == destination);
        var exit = source.AddTransition(target);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.duration = 0f;
        exit.canTransitionToSelf = false;
        if (parameter != null) exit.AddCondition(mode, parameter == "Speed" ? 0.1f : 0f, parameter);
        if (destination == "Run") exit.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
    }
}
