using System;
using System.Collections.Generic;

public sealed class RewardManager
{
    private sealed class RewardTemplate
    {
        public string Code { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Glyph { get; }
        public RewardEffectType EffectType { get; }

        public RewardTemplate(
            string code,
            string displayName,
            string description,
            string glyph,
            RewardEffectType effectType
        )
        {
            Code = code;
            DisplayName = displayName;
            Description = description;
            Glyph = glyph;
            EffectType = effectType;
        }
    }

    private readonly int baseMaxHPRewardAmount;
    private readonly float baseAttackRewardPercent;
    private readonly float baseGuardRewardPercent;
    private readonly RewardTierConfiguration[] tierConfigurations;

    private readonly RewardTemplate[] templates =
    {
        new RewardTemplate(
            "REWARD_ATTACK_SURGE",
            "ATTACK SURGE",
            "Meningkatkan damage Basic Attack selama run aktif.",
            "A",
            RewardEffectType.AttackPercent
        ),
        new RewardTemplate(
            "REWARD_VITAL_CORE",
            "VITAL CORE",
            "Meningkatkan Max HP dan memulihkan HP sebesar nilai yang sama.",
            "V",
            RewardEffectType.MaxHPFlat
        ),
        new RewardTemplate(
            "REWARD_GUARD_RESONANCE",
            "GUARD RESONANCE",
            "Memperkuat pengurangan damage Guard selama run aktif.",
            "G",
            RewardEffectType.GuardStrengthPercent
        )
    };

    public RewardManager(
        int maxHPRewardAmount,
        float attackRewardPercent,
        float guardRewardPercent
    ) : this(
        maxHPRewardAmount,
        attackRewardPercent,
        guardRewardPercent,
        CreateDefaultTierConfigurations()
    )
    {
    }

    public RewardManager(
        int maxHPRewardAmount,
        float attackRewardPercent,
        float guardRewardPercent,
        RewardTierConfiguration[] configurations
    )
    {
        if (configurations == null || configurations.Length == 0)
        {
            throw new ArgumentException(
                "Minimal satu RewardTierConfiguration diperlukan.",
                nameof(configurations)
            );
        }

        baseMaxHPRewardAmount = Math.Max(0, maxHPRewardAmount);
        baseAttackRewardPercent = Math.Max(0f, attackRewardPercent);
        baseGuardRewardPercent = Math.Max(0f, guardRewardPercent);
        tierConfigurations = (RewardTierConfiguration[])configurations.Clone();
        Array.Sort(
            tierConfigurations,
            (left, right) => left.Tier.CompareTo(right.Tier)
        );
    }

    public RewardDefinition[] GenerateChoices(int depth, int seed)
    {
        int safeDepth = Math.Max(1, depth);
        RewardTierConfiguration[] eligibleTiers = GetEligibleTiers(safeDepth);
        var random = new Random(seed);
        var choices = new List<RewardDefinition>(templates.Length);

        for (int index = 0; index < templates.Length; index++)
        {
            RewardTierConfiguration tier = RollTier(eligibleTiers, random);
            choices.Add(CreateDefinition(templates[index], tier));
        }

        for (int index = choices.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            RewardDefinition temporary = choices[index];
            choices[index] = choices[swapIndex];
            choices[swapIndex] = temporary;
        }

        return choices.ToArray();
    }

    public RewardTierConfiguration[] GetEligibleTiers(int depth)
    {
        int safeDepth = Math.Max(1, depth);
        var eligible = new List<RewardTierConfiguration>();

        for (int index = 0; index < tierConfigurations.Length; index++)
        {
            RewardTierConfiguration tier = tierConfigurations[index];

            if (tier.MinimumDepth <= safeDepth)
            {
                eligible.Add(tier);
            }
        }

        if (eligible.Count == 0)
        {
            throw new InvalidOperationException(
                $"Tidak ada reward tier yang eligible pada depth {safeDepth}."
            );
        }

        return eligible.ToArray();
    }

    public static RewardTierConfiguration[] CreateDefaultTierConfigurations()
    {
        // GDD v0.4 menetapkan threshold, tetapi belum menetapkan angka bobot
        // atau multiplier. Keduanya dipusatkan di sini sebagai baseline balancing.
        return new[]
        {
            new RewardTierConfiguration(1, 1, 100, 1.0f, "Fondasi build"),
            new RewardTierConfiguration(2, 5, 75, 1.5f, "Spesialisasi awal"),
            new RewardTierConfiguration(3, 15, 55, 2.0f, "Modifier fokus yang lebih kuat"),
            new RewardTierConfiguration(4, 30, 40, 2.5f, "Pendukung kombinasi build"),
            new RewardTierConfiguration(5, 60, 30, 3.0f, "Rare modifier mid depth"),
            new RewardTierConfiguration(6, 120, 22, 3.5f, "Advanced Infinite modifier"),
            new RewardTierConfiguration(7, 250, 16, 4.0f, "Deep run modifier"),
            new RewardTierConfiguration(8, 500, 11, 4.5f, "Very rare deep run modifier"),
            new RewardTierConfiguration(9, 750, 7, 5.0f, "Extreme depth modifier"),
            new RewardTierConfiguration(10, 1001, 4, 6.0f, "Tier tertinggi")
        };
    }

    private RewardDefinition CreateDefinition(
        RewardTemplate template,
        RewardTierConfiguration tier
    )
    {
        int flatValue = 0;
        float percentValue = 0f;
        string exactEffectText;

        switch (template.EffectType)
        {
            case RewardEffectType.AttackPercent:
                percentValue = baseAttackRewardPercent * tier.ValueMultiplier;
                exactEffectText =
                    $"Basic Attack +{ToWholePercent(percentValue)}%";
                break;

            case RewardEffectType.MaxHPFlat:
                flatValue = Math.Max(
                    1,
                    (int)Math.Round(
                        baseMaxHPRewardAmount * tier.ValueMultiplier,
                        MidpointRounding.AwayFromZero
                    )
                );
                exactEffectText =
                    $"Max HP +{flatValue} | HP +{flatValue}";
                break;

            case RewardEffectType.GuardStrengthPercent:
                percentValue = baseGuardRewardPercent * tier.ValueMultiplier;
                exactEffectText =
                    $"Guard Strength +{ToWholePercent(percentValue)}%";
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return new RewardDefinition(
            template.Code,
            template.DisplayName,
            template.Description,
            template.Glyph,
            template.EffectType,
            tier,
            flatValue,
            percentValue,
            exactEffectText
        );
    }

    private static RewardTierConfiguration RollTier(
        RewardTierConfiguration[] eligibleTiers,
        Random random
    )
    {
        int totalWeight = 0;

        for (int index = 0; index < eligibleTiers.Length; index++)
        {
            totalWeight += eligibleTiers[index].Weight;
        }

        int roll = random.Next(totalWeight);

        for (int index = 0; index < eligibleTiers.Length; index++)
        {
            RewardTierConfiguration tier = eligibleTiers[index];

            if (roll < tier.Weight)
            {
                return tier;
            }

            roll -= tier.Weight;
        }

        return eligibleTiers[eligibleTiers.Length - 1];
    }

    private static int ToWholePercent(float value)
    {
        return (int)Math.Round(
            value * 100f,
            MidpointRounding.AwayFromZero
        );
    }
}
