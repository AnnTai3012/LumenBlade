using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TrapTileDamage2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(1)] private int repeatDamage = 5;
    [SerializeField, Min(0f)] private float minimumFallSpeed = 0.35f;
    [SerializeField, Min(0.1f)] private float damageInterval = 0.4f;
    [SerializeField] private string trapTileName = "trap";

    private readonly HashSet<Collider2D> touchingTraps = new HashSet<Collider2D>();
    private Health health;
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private float verticalSpeedBeforePhysics;
    private float nextDamageTime;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        verticalSpeedBeforePhysics = body.linearVelocity.y;
        if (health.IsDead || touchingTraps.Count == 0)
            return;

        bool touchingActiveTrap = false;
        foreach (Collider2D trap in touchingTraps)
        {
            if (trap != null && trap.enabled && trap.gameObject.activeInHierarchy)
            {
                touchingActiveTrap = true;
                break;
            }
        }
        if (!touchingActiveTrap)
        {
            touchingTraps.Clear();
            return;
        }
        if (Time.time >= nextDamageTime)
        {
            health.TakeDamage(repeatDamage);
            nextDamageTime = Time.time + Mathf.Max(0.1f, damageInterval);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => BeginTrapContact(other);
    private void OnTriggerStay2D(Collider2D other) => BeginTrapContact(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        // A weapon/skill child can leave the trap while the player's body still overlaps it.
        if (other != null && playerCollider != null && playerCollider.Distance(other).isOverlapped)
            return;
        touchingTraps.Remove(other);
    }
    private void OnDisable() => touchingTraps.Clear();

    private void BeginTrapContact(Collider2D other)
    {
        if (other == null || touchingTraps.Contains(other) || playerCollider == null ||
            !playerCollider.Distance(other).isOverlapped || !IsTrap(other))
            return;
        bool firstContact = touchingTraps.Count == 0;
        touchingTraps.Add(other);
        if (!firstContact)
            return;

        if (verticalSpeedBeforePhysics <= -minimumFallSpeed)
            health.TakeDamage(damage);
        nextDamageTime = Time.time + Mathf.Max(0.1f, damageInterval);
    }

    private bool IsTrap(Collider2D other)
    {
        Tilemap tilemap = other.GetComponent<Tilemap>();
        if (tilemap == null)
            return string.Equals(other.gameObject.name, trapTileName, StringComparison.OrdinalIgnoreCase);
        if (playerCollider == null)
            return false;

        Bounds bounds = playerCollider.bounds;
        Vector3Int minimum = tilemap.WorldToCell(bounds.min);
        Vector3Int maximum = tilemap.WorldToCell(bounds.max);
        for (int y = minimum.y; y <= maximum.y; y++)
        {
            for (int x = minimum.x; x <= maximum.x; x++)
            {
                TileBase tile = tilemap.GetTile(new Vector3Int(x, y, minimum.z));
                if (tile != null && string.Equals(tile.name, trapTileName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}
