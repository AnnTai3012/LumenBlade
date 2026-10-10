using System;
using UnityEngine;

public enum MonsterArchetype
{
    GreenSlime,
    Bat,
    BlueSlime,
    RedSlime,
    Rat,
    Mimic,
    SuperMonster
}

// Baseline with no attack blessings; keep the rounding identical to the design table.
public static class ChapterBalance
{
    public const float PlayerReferenceDps = 22.5f;
    public const int PlayerReferenceBasicDamage = 12;

    public static int PlayerBaseHealth(int chapter) => 100 + 10 * (ClampChapter(chapter) - 1);
    public static int PlayerDesignHealth(int chapter) => Round(PlayerBaseHealth(chapter) * 1.3m);
    public static float PlayerStamina(int chapter) => 100f + 15f * (ClampChapter(chapter) - 1);

    public static int MonsterHealth(MonsterArchetype kind, int chapter)
    {
        // Starter enemies take 2 / 3 / 4 / 6 basic hits from an unblessed C1 player.
        int starterHits = kind switch
        {
            MonsterArchetype.GreenSlime => 2,
            MonsterArchetype.Bat => 3,
            MonsterArchetype.Rat => 4,
            MonsterArchetype.Mimic => 6,
            _ => 0
        };
        if (starterHits > 0)
            return Round(PlayerReferenceBasicDamage * starterHits * ChapterScale(chapter));

        decimal ttk;
        switch (kind)
        {
            case MonsterArchetype.BlueSlime: ttk = 5m; break;
            case MonsterArchetype.RedSlime: ttk = 6m; break;
            case MonsterArchetype.SuperMonster: ttk = 9m; break;
            default: ttk = 3m; break;
        }
        return Round(22.5m * ttk * ChapterScale(chapter));
    }

    public static int MonsterDamage(MonsterArchetype kind, int chapter)
    {
        // New players have no HP blessings: balance the four starters against base HP.
        decimal starterFraction = kind switch
        {
            MonsterArchetype.GreenSlime => 0.04m,
            MonsterArchetype.Bat => 0.05m,
            MonsterArchetype.Rat => 0.07m,
            MonsterArchetype.Mimic => 0.10m,
            _ => 0m
        };
        if (starterFraction > 0m)
            return Round(PlayerBaseHealth(chapter) * starterFraction);

        int hits;
        switch (kind)
        {
            case MonsterArchetype.BlueSlime:
            case MonsterArchetype.SuperMonster: hits = 8; break;
            case MonsterArchetype.RedSlime: hits = 7; break;
            default: hits = 12; break;
        }
        return Round((decimal)PlayerDesignHealth(chapter) / hits);
    }

    public static int BossHealthBudget(int chapter) =>
        Round(22.5m * (90m + 10m * (ClampChapter(chapter) - 1)) * ChapterScale(chapter));

    public static int BossDamage(int chapter, bool heavy = false) =>
        Round(PlayerDesignHealth(chapter) * (heavy ? 0.15m : 0.10m));

    // Golem restores 5% twice. Use base HP + those heals, rather than adding 10% to the budget.
    public static int GolemBaseHealth(int chapter) =>
        (int)decimal.Ceiling(BossHealthBudget(chapter) / 1.10m);

    private static int ClampChapter(int chapter) => Mathf.Clamp(chapter, 1, 5);
    private static decimal ChapterScale(int chapter) => 1m + 0.05m * (ClampChapter(chapter) - 1);
    private static int Round(decimal value) => (int)Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
