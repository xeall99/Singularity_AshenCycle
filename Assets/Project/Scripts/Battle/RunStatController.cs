using System;
using UnityEngine;

public sealed class RunStatController
{
    public const float MaximumGuardDamageReduction = 0.90f;

    private readonly RunModifierCollection modifiers;

    public int BaseMaxHP { get; }
    public int BaseMaxMana { get; }
    public int BaseBasicAttackDamage { get; }
    public float BaseGuardDamageReduction { get; }

    public int MaxHPBonus => modifiers.GetTotalFlat(
        RewardEffectType.MaxHPFlat
    );

    public float BasicAttackBonusPercent => modifiers.GetTotalPercent(
        RewardEffectType.AttackPercent
    );

    public float GuardStrengthBonusPercent => modifiers.GetTotalPercent(
        RewardEffectType.GuardStrengthPercent
    );

    public int EffectiveMaxHP => checked(BaseMaxHP + MaxHPBonus);

    // No Max Mana reward exists in the v0.4 reward pool yet. Keeping the
    // effective value here lets Analyze and resource caps use one source now.
    public int EffectiveMaxMana => BaseMaxMana;

    public int EffectiveBasicAttackDamage => Mathf.Max(
        1,
        Mathf.RoundToInt(
            BaseBasicAttackDamage * (1f + BasicAttackBonusPercent)
        )
    );

    public float EffectiveGuardDamageReduction => Mathf.Clamp(
        BaseGuardDamageReduction * (1f + GuardStrengthBonusPercent),
        0f,
        MaximumGuardDamageReduction
    );

    public int AnalyzeManaCost => CalculateAnalyzeManaCost(
        EffectiveMaxMana
    );

    public RunStatController(
        int baseMaxHP,
        int baseMaxMana,
        int baseBasicAttackDamage,
        float baseGuardDamageReduction,
        RunModifierCollection modifiers
    )
    {
        this.modifiers = modifiers ??
            throw new ArgumentNullException(nameof(modifiers));

        BaseMaxHP = Math.Max(1, baseMaxHP);
        BaseMaxMana = Math.Max(0, baseMaxMana);
        BaseBasicAttackDamage = Math.Max(1, baseBasicAttackDamage);
        BaseGuardDamageReduction = Mathf.Clamp(
            baseGuardDamageReduction,
            0f,
            MaximumGuardDamageReduction
        );
    }

    public static int CalculateAnalyzeManaCost(int effectiveMaxMana)
    {
        int safeMaxMana = Math.Max(0, effectiveMaxMana);

        // For integer M >= 0: ceil(0.75 * M) == M - floor(M / 4).
        // M = 10: ceil(7.5) = 8, without floating-point rounding loss.
        return safeMaxMana - safeMaxMana / 4;
    }
}
