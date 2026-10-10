using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class SpiritStoneRewardOnDeath : MonoBehaviour
{
    [SerializeField, Tooltip("Unique per scene enemy; survives checkpoint reloads.")]
    private string rewardId;
    private MonsterHealth health;
    private string stableId;
    private bool rewarded;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        stableId = rewardId;
        if (string.IsNullOrEmpty(stableId))
        {
            var path = new StringBuilder();
            for (Transform node = transform; node != null; node = node.parent)
                path.Insert(0, "/" + node.name + "[" + node.GetSiblingIndex() + "]");
            stableId = path.ToString();
        }
    }

    private void OnEnable()
    {
        if (health != null) health.OnDied += Reward;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= Reward;
    }

    private void Reward()
    {
        if (rewarded || RunResources.Instance == null)
            return;
        rewarded = true;
        RunResources.Instance.TryRewardEnemy(gameObject.scene.path, stableId);
    }
}
