using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth), typeof(MonsterCombat))]
[DefaultExecutionOrder(-500)]
public sealed class MonsterChapterStats : MonoBehaviour
{
    [SerializeField] private MonsterArchetype archetype;
    [SerializeField, Range(1, 5), Tooltip("Used in scenes without LevelResourceFlow.")]
    private int fallbackChapter = 1;
    [SerializeField] private bool useSceneChapter = true;

    public MonsterArchetype Archetype => archetype;
    public int AppliedChapter { get; private set; }

    private void Awake()
    {
        AppliedChapter = fallbackChapter;
        if (useSceneChapter)
        {
            // Resolve once on spawn, within this enemy's scene; no cross-scene or per-frame search.
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                LevelResourceFlow flow = root.GetComponentInChildren<LevelResourceFlow>(true);
                if (flow == null) continue;
                AppliedChapter = flow.ChapterIndex;
                break;
            }
        }
        int damage = ChapterBalance.MonsterDamage(archetype, AppliedChapter);
        GetComponent<MonsterHealth>().ConfigureMaximumHealth(ChapterBalance.MonsterHealth(archetype, AppliedChapter));
        GetComponent<MonsterCombat>().ConfigureDamage(damage, damage);
    }
}
