using System;
using System.Collections.Generic;

/// <summary>
/// Active-run-only Anomalies. Every definition carries a benefit and a real
/// drawback. Identity and values are validated again when a run is restored.
/// </summary>
public static class RunAnomalyCatalog
{
    private static readonly RewardTierConfiguration tier =
        new RewardTierConfiguration(1, 1, 1, 1f, "Run-defining Anomaly");

    private static readonly RewardDefinition[] definitions =
    {
        new RewardDefinition(
            "ANOMALY_COLLAPSED_STAR",
            "Collapsed Star",
            "Anomaly • Active run only.",
            "C",
            RewardEffectType.AnomalyCollapsedStar,
            tier,
            1,
            0.30f,
            "<color=#AEEBFF>BENEFIT</color>  Skill damage +30%\n" +
            "<color=#FF879D>CONSEQUENCE</color>  All Skills cost +1 Mana"
        ),
        new RewardDefinition(
            "ANOMALY_BLACK_SUN",
            "Black Sun",
            "Anomaly • Active run only.",
            "B",
            RewardEffectType.AnomalyBlackSun,
            tier,
            15,
            0f,
            "<color=#AEEBFF>BENEFIT</color>  First Convergence uses two actions\n" +
            "<color=#FF879D>CONSEQUENCE</color>  Maximum HP -15"
        ),
        new RewardDefinition(
            "ANOMALY_FROZEN_ORBIT",
            "Frozen Orbit",
            "Anomaly • Active run only.",
            "F",
            RewardEffectType.AnomalyFrozenOrbit,
            tier,
            2,
            0f,
            "<color=#AEEBFF>BENEFIT</color>  Guard blocks two attacks\n" +
            "<color=#FF879D>CONSEQUENCE</color>  Player Speed -2"
        ),
        new RewardDefinition(
            "ANOMALY_INVERTED_PULSE",
            "Inverted Pulse",
            "Anomaly • Active run only.",
            "I",
            RewardEffectType.AnomalyInvertedPulse,
            tier,
            2,
            0f,
            "<color=#AEEBFF>BENEFIT</color>  Basic Attack restores 2 HP\n" +
            "<color=#FF879D>CONSEQUENCE</color>  Basic Attack cannot critical"
        )
    };

    public static bool IsAnomaly(RewardEffectType effect)
    {
        return effect == RewardEffectType.AnomalyCollapsedStar ||
            effect == RewardEffectType.AnomalyBlackSun ||
            effect == RewardEffectType.AnomalyFrozenOrbit ||
            effect == RewardEffectType.AnomalyInvertedPulse;
    }

    public static bool Has(
        RunModifierCollection modifiers,
        RewardEffectType effect
    )
    {
        if (modifiers == null)
        {
            throw new ArgumentNullException(nameof(modifiers));
        }

        return modifiers.GetStackCount(effect) > 0;
    }

    public static RewardDefinition[] GetDefinitions()
    {
        return (RewardDefinition[])definitions.Clone();
    }

    public static RewardDefinition[] CreateChoices(
        int seed,
        RunModifierCollection owned
    )
    {
        if (owned == null)
        {
            throw new ArgumentNullException(nameof(owned));
        }

        var pool = new List<RewardDefinition>();
        foreach (RewardDefinition definition in definitions)
        {
            if (!Has(owned, definition.EffectType))
            {
                pool.Add(definition);
            }
        }

        uint random = unchecked((uint)seed) ^ 0xB1A5C5E1u;
        for (int index = pool.Count - 1; index > 0; index--)
        {
            random = unchecked(random * 1664525u + 1013904223u);
            int swapIndex = (int)(random % (uint)(index + 1));
            RewardDefinition value = pool[index];
            pool[index] = pool[swapIndex];
            pool[swapIndex] = value;
        }

        return pool.GetRange(0, Math.Min(3, pool.Count)).ToArray();
    }

    public static void Validate(
        string code,
        RewardEffectType effect,
        int flat,
        float percent,
        int stacks
    )
    {
        bool anomalyIdentity = IsAnomaly(effect) ||
            code.StartsWith("ANOMALY_", StringComparison.Ordinal);

        if (!anomalyIdentity)
        {
            return;
        }

        foreach (RewardDefinition definition in definitions)
        {
            if (definition.Code == code &&
                definition.EffectType == effect &&
                definition.FlatValue == flat &&
                Math.Abs(definition.PercentValue - percent) < 0.0001f &&
                stacks == 1)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "Anomaly identity or unique-stack values are invalid."
        );
    }
}
