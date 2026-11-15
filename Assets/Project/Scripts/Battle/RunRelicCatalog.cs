using System;
using System.Collections.Generic;
using System.IO;

/// <summary>Unique, active-run-only relic definitions. No rarity upgrades or stat tiers.</summary>
public static class RunRelicCatalog
{
    private static readonly RewardTierConfiguration tier = new RewardTierConfiguration(1, 1, 1, 1f, "Unique relic");
    private static readonly RewardDefinition[] definitions =
    {
        Define("RELIC_HOLLOW_CROWN", "Hollow Crown", "C", RewardEffectType.RelicGuardMana,
            "Guard restores +1 Mana. Capped at maximum Mana."),
        Define("RELIC_ASHEN_FANG", "Ashen Fang", "F", RewardEffectType.RelicAttackBurn,
            "Basic Attack: +1 Burn for 2 turns.\n1 damage/stack after enemy acts.\nMax 3 stacks; refreshes duration."),
        Define("RELIC_EMBER_LOCKET", "Ember Locket", "L", RewardEffectType.RelicEntryHeal,
            "Restore 6 HP when a battle begins. Capped at maximum HP."),
        Define("RELIC_FRACTURED_LENS", "Fractured Lens", "V", RewardEffectType.RelicFirstSkill,
            "Your first successful Skill each battle deals +6 damage.")
    };
    private static readonly RewardDefinition echoPrism = Define(
        HorizontalUnlockCatalog.EchoPrismId, "Echo Prism", "P",
        RewardEffectType.RelicFirstAttack,
        "First Basic Attack each battle deals +4 damage."
    );

    private static RewardDefinition Define(string code, string name, string glyph, RewardEffectType effect, string text) =>
        new RewardDefinition(code, name, "Unique relic. Active run only.", glyph, effect, tier, 1, 0, text);

    public static bool IsRelic(RewardEffectType effect) =>
        (effect >= RewardEffectType.RelicGuardMana &&
         effect <= RewardEffectType.RelicFirstSkill) ||
        effect == RewardEffectType.RelicFirstAttack;

    public static bool Has(RunModifierCollection modifiers, RewardEffectType effect) => modifiers.GetStackCount(effect) > 0;

    public static RewardDefinition[] GetDefinitions() => (RewardDefinition[])definitions.Clone();

    public static RewardDefinition GetUnlockableDefinition() => echoPrism;

    public static RewardDefinition[] GetAllDefinitions()
    {
        var all = new RewardDefinition[definitions.Length + 1];
        Array.Copy(definitions, all, definitions.Length);
        all[all.Length - 1] = echoPrism;
        return all;
    }

    public static RewardDefinition[] CreateChoices(int seed, RunModifierCollection owned)
    {
        bool unlocked;
        try
        {
            unlocked = EchoFragmentWallet.CreateDefault().HasUnlock(
                HorizontalUnlockCatalog.EchoPrismId);
        }
        catch (Exception error) when (error is IOException ||
                                      error is UnauthorizedAccessException ||
                                      error is InvalidOperationException)
        {
            // A damaged permanent wallet must not prevent an active run from
            // reaching its relic shrine. The menu surfaces the wallet error.
            unlocked = false;
        }
        return CreateChoicesForUnlockState(seed, owned, unlocked);
    }

    public static RewardDefinition[] CreateChoicesForUnlockState(
        int seed, RunModifierCollection owned, bool echoPrismUnlocked)
    {
        var pool = new List<RewardDefinition>();
        foreach (RewardDefinition definition in definitions)
            if (!Has(owned, definition.EffectType)) pool.Add(definition);
        if (echoPrismUnlocked && !Has(owned, echoPrism.EffectType))
            pool.Add(echoPrism);
        uint random = unchecked((uint)seed) ^ 0x7F4A7C15u;
        for (int i = pool.Count - 1; i > 0; i--)
        {
            random = unchecked(random * 1664525u + 1013904223u);
            int j = (int)(random % (uint)(i + 1));
            RewardDefinition value = pool[i]; pool[i] = pool[j]; pool[j] = value;
        }
        if (pool.Count < 3) throw new InvalidOperationException("Not enough unowned relics for a three-card choice.");
        return pool.GetRange(0, 3).ToArray();
    }

    public static void Validate(string code, RewardEffectType effect, int flat, float percent, int stacks)
    {
        if (!IsRelic(effect) && !code.StartsWith("RELIC_", StringComparison.Ordinal)) return;
        foreach (RewardDefinition definition in definitions)
            if (definition.Code == code && definition.EffectType == effect && flat == 1 && percent == 0 && stacks == 1) return;
        if (echoPrism.Code == code && echoPrism.EffectType == effect &&
            flat == 1 && percent == 0 && stacks == 1) return;
        throw new InvalidOperationException("Relic identity or unique-stack values are invalid.");
    }
}
