using System.Collections.Generic;
using UnityEngine;

// Optional local override on top of the scene profile; supports multiple player colliders.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class AudioEnvironmentZone : MonoBehaviour
{
    [SerializeField] private GameAudioEnvironment environment = GameAudioEnvironment.Cave;
    [SerializeField] private int priority;
    private readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
    public GameAudioEnvironment Environment => environment;
    public int Priority => priority;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Health>() == null) return;
        contacts.Add(other);
        GameAudioManager.Instance?.EnterEnvironment(this);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        contacts.Remove(other);
        if (contacts.Count == 0) GameAudioManager.Instance?.ExitEnvironment(this);
    }
    private void OnDisable()
    {
        contacts.Clear();
        GameAudioManager.Instance?.ExitEnvironment(this);
    }
    private void OnValidate() => GetComponent<Collider2D>().isTrigger = true;
}
