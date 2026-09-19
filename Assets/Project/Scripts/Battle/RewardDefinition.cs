using System;

public enum RewardEffectType
{
    AttackPercent,
    MaxHPFlat,
    GuardStrengthPercent
}

public sealed class RewardTierConfiguration
{
    public int Tier { get; }
    public int MinimumDepth { get; }
    public int Weight { get; }
    public float ValueMultiplier { get; }
    public string DesignRole { get; }

    public RewardTierConfiguration(
        int tier,
        int minimumDepth,
        int weight,
        float valueMultiplier,
        string designRole
    )
    {
        if (tier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tier));
        }

        if (minimumDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumDepth));
        }

        if (weight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(weight));
        }

        if (valueMultiplier <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(valueMultiplier));
        }

        Tier = tier;
        MinimumDepth = minimumDepth;
        Weight = weight;
        ValueMultiplier = valueMultiplier;
        DesignRole = designRole ?? string.Empty;
    }
}

public sealed class RewardDefinition
{
    public string Code { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string Glyph { get; }
    public RewardEffectType EffectType { get; }
    public int Tier { get; }
    public int MinimumDepth { get; }
    public int Weight { get; }
    public int FlatValue { get; }
    public float PercentValue { get; }
    public string ExactEffectText { get; }

    public RewardDefinition(
        string code,
        string displayName,
        string description,
        string glyph,
        RewardEffectType effectType,
        RewardTierConfiguration tier,
        int flatValue,
        float percentValue,
        string exactEffectText
    )
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Reward code wajib diisi.", nameof(code));
        }

        if (tier == null)
        {
            throw new ArgumentNullException(nameof(tier));
        }

        Code = code;
        DisplayName = displayName ?? code;
        Description = description ?? string.Empty;
        Glyph = string.IsNullOrWhiteSpace(glyph) ? "?" : glyph;
        EffectType = effectType;
        Tier = tier.Tier;
        MinimumDepth = tier.MinimumDepth;
        Weight = tier.Weight;
        FlatValue = Math.Max(0, flatValue);
        PercentValue = Math.Max(0f, percentValue);
        ExactEffectText = exactEffectText ?? string.Empty;
    }
}
