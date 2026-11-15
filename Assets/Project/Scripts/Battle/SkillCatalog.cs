using System;
using System.Collections.Generic;

public enum SkillActiveEffect
{
    Damage,
    Burn,
    Weakness,
    HealAndCleanse,
    Curse,
    DamageAndParry,
    RestoreMana,
    BurnBonus,
    Siphon,
    Mark,
    SelfSacrifice,
    Execute30,
    DamageAndWard,
    EchoDamage,
    BurnVerdict,
    WeaknessBonus,
    BurnSustain,
    WardOnly,
    DamageAndCleanse,
    OrbitSever,
    AnalyzeDamage,
    BurnConsume,
    OrbitEcho,
    GuardEcho,
    NightfallVow,
    GravityThread,
    CurseBrand,
    MourningBell,
    UmbralExecution,
    BurnAndSustain,
    HealAndWard
}

public enum SkillPassiveEffect
{
    None,
    ShadowAfterAnalyzeDamage,
    AshwakeBurnMana,
    GravemuteWeaknessGuard,
    VesperLowHealthDiscount,
    PaleLedgerCursedConvergenceMana,
    BlackglassReversalWeakness,
    HollowUnstableMana,
    CinderRefrainBurnDamage,
    RiftSiphonCursedKillHeal,
    BlackstarMarkWeakness,
    VowSacrificeWard,
    GallowsFirstSkillKillMana,
    MirrorWardAttack,
    EchoScriptGuardDiscount,
    AbyssalVerdictBurnCurse,
    FractureLanceWeaknessMana,
    WraithBurnHeal,
    GloamingExtraGuardHit,
    RequiemCleanseWeakness,
    OrbitalSeverConvergenceHeal,
    AsterionAnalyzeProtection,
    CinderKeyEmber,
    SableGuardHeal,
    EchoBraidAttackFollowup,
    EmberGuardSkillMana,
    NightfallSacrificeMana,
    GravityThreadWeaknessDuration,
    PaperCrownCurseDuration,
    MourningBellConvergenceMana,
    UmbralExecutionWard
}

public sealed class SkillDefinition
{
    public string Code { get; }
    public string Name { get; }
    public int ManaCost { get; }
    public int Power { get; }
    public string Description { get; }
    public string PassiveDescription { get; }
    public SkillActiveEffect ActiveEffect { get; }
    public SkillPassiveEffect PassiveEffect { get; }
    public SkillData Data { get; }

    internal SkillDefinition(
        int id,
        string code,
        string name,
        int manaCost,
        int power,
        string description,
        string passiveDescription,
        SkillActiveEffect activeEffect,
        SkillPassiveEffect passiveEffect
    )
    {
        Code = code;
        Name = name;
        ManaCost = manaCost;
        Power = power;
        Description = description;
        PassiveDescription = passiveDescription;
        ActiveEffect = activeEffect;
        PassiveEffect = passiveEffect;
        Data = new SkillData
        {
            Id = id,
            Code = code,
            Name = name,
            ManaCost = manaCost,
            Power = power,
            TargetType = activeEffect == SkillActiveEffect.HealAndCleanse ||
                activeEffect == SkillActiveEffect.HealAndWard ||
                activeEffect == SkillActiveEffect.RestoreMana ||
                activeEffect == SkillActiveEffect.WardOnly ? "Self" : "SingleEnemy",
            EffectType = activeEffect == SkillActiveEffect.HealAndCleanse ||
                activeEffect == SkillActiveEffect.HealAndWard ||
                activeEffect == SkillActiveEffect.RestoreMana ? "Heal" : "Damage",
            Description = description,
            AssetKey = code,
            CreatedAt = "catalog"
        };
    }
}

/// <summary>Curated, stable skill roster. Runtime content tools cannot mutate this list.</summary>
public static class SkillCatalog
{
    private static readonly SkillDefinition[] definitions =
    {
        D(1, "SKL_SHADOW_STRIKE", "Shadow Strike", 2, 20, "Deal 20 damage. Passive (once per battle): after Analyze, your first Shadow Strike deals 4 extra damage.", "After Analyze: your first Shadow Strike each battle deals +4 damage", SkillActiveEffect.Damage, SkillPassiveEffect.ShadowAfterAnalyzeDamage),
        D(2, "SKL_ASHWAKE_CANTICLE", "Ashwake Canticle", 1, 6, "Deal 6 damage and apply 1 Burn for 2 enemy turns. Passive (once per battle): the first Burn tick restores 1 Mana.", "First Burn tick each battle restores 1 Mana", SkillActiveEffect.Burn, SkillPassiveEffect.AshwakeBurnMana),
        D(3, "SKL_GRAVEMUTE_SEAL", "Gravemute Seal", 2, 4, "Deal 4 damage and apply Weakness for 2 enemy turns. Passive (once per battle): the first hit from a Weakened enemy deals 3 less damage.", "First hit each battle from a Weakened enemy deals 3 less damage", SkillActiveEffect.Weakness, SkillPassiveEffect.GravemuteWeaknessGuard),
        D(4, "SKL_VESPER_STITCH", "Vesper Stitch", 3, 8, "Restore 8 HP and cleanse one Weakness or Curse. Passive (once per battle): Vesper Stitch costs 1 less Mana while your HP is at or below 40%.", "Once per battle at 40% HP or below: Vesper Stitch costs 1 less Mana", SkillActiveEffect.HealAndCleanse, SkillPassiveEffect.VesperLowHealthDiscount),
        D(5, "SKL_PALE_LEDGER", "Pale Ledger", 2, 6, "Deal 6 damage and apply Curse for 2 enemy turns. Passive (once per battle): the first Event Horizon against a Cursed enemy restores 1 Mana.", "First Event Horizon each battle against a Cursed enemy restores 1 Mana", SkillActiveEffect.Curse, SkillPassiveEffect.PaleLedgerCursedConvergenceMana),
        D(6, "SKL_BLACKGLASS_PARRY", "Blackglass Parry", 3, 5, "Deal 5 damage and prepare a 50% parry against the next direct hit. Passive (once per battle): your first Reversal Orbit applies Weakness for 1 enemy turn.", "First Reversal Orbit each battle applies Weakness for 1 enemy turn", SkillActiveEffect.DamageAndParry, SkillPassiveEffect.BlackglassReversalWeakness),
        D(7, "SKL_HOLLOW_CONDUIT", "Hollow Conduit", 2, 4, "Restore 4 Mana. Passive (once per battle): the first Unstable Pulse restores 1 additional Mana.", "First Unstable Pulse each battle restores 1 extra Mana", SkillActiveEffect.RestoreMana, SkillPassiveEffect.HollowUnstableMana),
        D(8, "SKL_CINDER_REFRAIN", "Cinder Refrain", 2, 12, "Deal 12 damage, plus 4 if the enemy is Burning. Passive (once per battle): your first hit on a Burning enemy deals 2 extra damage.", "First hit each battle on a Burning enemy deals +2 damage", SkillActiveEffect.BurnBonus, SkillPassiveEffect.CinderRefrainBurnDamage),
        D(9, "SKL_RIFT_SIPHON", "Rift Siphon", 4, 14, "Deal 14 damage and restore 4 HP if the enemy is Cursed. Passive (once per battle): your first kill of a Cursed enemy restores 4 HP.", "First Cursed enemy kill each battle restores 4 HP", SkillActiveEffect.Siphon, SkillPassiveEffect.RiftSiphonCursedKillHeal),
        D(10, "SKL_BLACKSTAR_BRAND", "Blackstar Brand", 2, 7, "Deal 7 damage and mark the enemy; your next Basic Attack deals 8 extra damage. Passive (once per battle): the first marked target that survives your Basic Attack gains Weakness for 1 enemy turn.", "First marked target each battle to survive your Basic Attack gains Weakness for 1 enemy turn", SkillActiveEffect.Mark, SkillPassiveEffect.BlackstarMarkWeakness),
        D(11, "SKL_VOW_OF_ASH", "Vow of Ash", 1, 15, "Deal 15 damage and lose 3 HP. Cannot be used at 3 HP or less. Passive (once per battle): your first Vow of Ash grants 3 Ward after its HP cost.", "First Vow of Ash each battle grants 3 Ward after its HP cost", SkillActiveEffect.SelfSacrifice, SkillPassiveEffect.VowSacrificeWard),
        D(12, "SKL_GALLOWS_BLOOM", "Gallows Bloom", 4, 11, "Deal 11 damage, plus 11 against enemies at 30% HP or less. Passive (once per battle): the first Skill kill restores 1 Mana, regardless of which Skill defeats the enemy.", "First Skill kill each battle restores 1 Mana", SkillActiveEffect.Execute30, SkillPassiveEffect.GallowsFirstSkillKillMana),
        D(13, "SKL_MIRROR_WARDEN", "Mirror Warden", 2, 8, "Deal 8 damage and ward against up to 5 damage from the next direct hit. Passive (once per battle): the first hit absorbed by your Ward empowers your next Basic Attack by 4.", "First Ward absorption each battle empowers your next Basic Attack by 4", SkillActiveEffect.DamageAndWard, SkillPassiveEffect.MirrorWardAttack),
        D(14, "SKL_ECHO_SCRIPT", "Echo Script", 4, 9, "Deal 9 damage plus half your last Basic Attack (up to 5). Passive (repeatable): Echo Script costs 1 less Mana whenever your previous action was Guard.", "After Guard: Echo Script costs 1 less Mana (repeatable)", SkillActiveEffect.EchoDamage, SkillPassiveEffect.EchoScriptGuardDiscount),
        D(15, "SKL_ABYSSAL_VERDICT", "Abyssal Verdict", 5, 12, "Deal 12 damage plus 4 per Burn stack, then consume up to 3 Burn. Passive (once per battle): the first cast that consumes at least 2 Burn and leaves the target alive applies Curse for 1 turn.", "First qualifying Abyssal Verdict each battle applies Curse if the target survives", SkillActiveEffect.BurnVerdict, SkillPassiveEffect.AbyssalVerdictBurnCurse),
        D(16, "SKL_FRACTURE_LANCE", "Fracture Lance", 2, 10, "Deal 10 damage, plus 4 against a Weakened enemy. Passive (once per battle): your first hit against a Weakened enemy restores 1 Mana.", "First hit each battle against a Weakened enemy restores 1 Mana", SkillActiveEffect.WeaknessBonus, SkillPassiveEffect.FractureLanceWeaknessMana),
        D(17, "SKL_WRAITH_CANDLE", "Wraith Candle", 1, 5, "Deal 5 damage, apply 1 Burn for 2 enemy turns, and restore 2 HP. Passive (once per battle): the first Burn tick restores 2 more HP.", "First Burn tick each battle restores 2 HP", SkillActiveEffect.BurnAndSustain, SkillPassiveEffect.WraithBurnHeal),
        D(18, "SKL_GLOAMING_BULWARK", "Gloaming Bulwark", 3, 8, "Ward against up to 8 damage from the next direct hit. Passive (once per battle): the first time Ward absorbs damage, your next Guard blocks one additional enemy hit.", "First Ward absorption each battle empowers your next Guard to block 1 extra hit", SkillActiveEffect.WardOnly, SkillPassiveEffect.GloamingExtraGuardHit),
        D(19, "SKL_REQUIEM_OF_GLASS", "Requiem of Glass", 2, 8, "Deal 8 damage and cleanse one Curse or Weakness. Passive (once per battle): your first successful cleanse applies Weakness to the enemy for 1 turn.", "First successful cleanse each battle applies Weakness to the enemy for 1 turn", SkillActiveEffect.DamageAndCleanse, SkillPassiveEffect.RequiemCleanseWeakness),
        D(20, "SKL_ORBITAL_SEVER", "Orbital Sever", 3, 9, "Deal 9 damage, plus 4 when this skill fills Orbit slot three. Passive (once per battle): your first Convergence after Orbital Sever restores 2 HP.", "First Convergence after Orbital Sever each battle restores 2 HP", SkillActiveEffect.OrbitSever, SkillPassiveEffect.OrbitalSeverConvergenceHeal),
        D(21, "SKL_ASTERION_GAZE", "Asterion Gaze", 2, 6, "Deal 6 damage, plus 4 if Analyze has revealed the enemy intent. Passive (once per battle): the first hit from that revealed intent deals 3 less damage.", "First hit each battle from the revealed enemy intent deals 3 less damage", SkillActiveEffect.AnalyzeDamage, SkillPassiveEffect.AsterionAnalyzeProtection),
        D(22, "SKL_CINDER_KEY", "Cinder Key", 3, 10, "Deal 10 damage; consume 1 Burn stack to deal 6 more. Passive (once per battle): consuming a Burn stack with Cinder Key grants 1 Ember the first time.", "First Burn stack consumed by Cinder Key each battle grants 1 Ember", SkillActiveEffect.BurnConsume, SkillPassiveEffect.CinderKeyEmber),
        D(23, "SKL_SABLE_SUTURE", "Sable Suture", 2, 7, "Restore 7 HP and gain 5 Ward. Passive (once per battle): your first Guard after casting Sable Suture restores 2 HP.", "First Guard each battle after Sable Suture restores 2 HP", SkillActiveEffect.HealAndWard, SkillPassiveEffect.SableGuardHeal),
        D(24, "SKL_ECHO_BRAID", "Echo Braid", 3, 7, "Deal 7 damage, plus 2 for each different action in the last two Orbit slots (up to 4). Passive (once per battle): your first Echo Braid empowers your next Basic Attack by 3.", "First Echo Braid each battle empowers your next Basic Attack by 3", SkillActiveEffect.OrbitEcho, SkillPassiveEffect.EchoBraidAttackFollowup),
        D(25, "SKL_EMBER_REPRISE", "Ember Reprise", 2, 8, "Deal 8 damage, plus 4 if your previous action was Guard. Passive (once per battle): your first Skill immediately after Guard restores 1 Mana.", "First Skill each battle immediately after Guard restores 1 Mana", SkillActiveEffect.GuardEcho, SkillPassiveEffect.EmberGuardSkillMana),
        D(26, "SKL_NIGHTFALL_VOW", "Nightfall Vow", 2, 18, "Deal 18 damage and lose 5 HP. Cannot be used at 5 HP or less. Passive (once per battle): after paying this cost, restore 2 Mana.", "First Nightfall Vow each battle restores 2 Mana after its HP cost", SkillActiveEffect.NightfallVow, SkillPassiveEffect.NightfallSacrificeMana),
        D(27, "SKL_GRAVITY_THREAD", "Gravity Thread", 2, 6, "Deal 6 damage and apply Weakness for 2 enemy turns (3 if already Weakened). Passive (once per battle): its first Weakness application lasts 1 additional enemy turn.", "First Gravity Thread Weakness each battle lasts 1 extra enemy turn", SkillActiveEffect.GravityThread, SkillPassiveEffect.GravityThreadWeaknessDuration),
        D(28, "SKL_PAPER_CROWN", "Paper Crown", 3, 8, "Deal 8 damage, plus 6 against a Cursed enemy; otherwise apply Curse for 2 turns. Passive (once per battle): your first hit against a Cursed enemy extends Curse by 1 turn.", "First hit each battle against a Cursed enemy extends Curse by 1 turn", SkillActiveEffect.CurseBrand, SkillPassiveEffect.PaperCrownCurseDuration),
        D(29, "SKL_MOURNING_BELL", "Mourning Bell", 3, 7, "Deal 7 damage, plus 6 if the previous two actions include Attack and Guard. Passive (once per battle): your first Convergence restores 2 Mana.", "First Convergence each battle restores 2 Mana", SkillActiveEffect.MourningBell, SkillPassiveEffect.MourningBellConvergenceMana),
        D(30, "SKL_UMBRAL_EXECUTION", "Umbral Execution", 4, 11, "Deal 11 damage, plus 9 against enemies at 35% HP or less. Passive (once per battle): your first hit against an enemy in execute range grants 4 Ward.", "First hit each battle against an enemy at 35% HP or below grants 4 Ward", SkillActiveEffect.UmbralExecution, SkillPassiveEffect.UmbralExecutionWard)
    };

    private static readonly Dictionary<string, SkillDefinition> byCode = BuildLookup();

    public static SkillDefinition[] All => (SkillDefinition[])definitions.Clone();
    public static int Count => definitions.Length;

    public static bool TryGet(string code, out SkillDefinition definition)
    {
        definition = null;
        return !string.IsNullOrWhiteSpace(code) && byCode.TryGetValue(code, out definition);
    }

    public static SkillDefinition Get(string code)
    {
        return TryGet(code, out SkillDefinition definition) ? definition : null;
    }

    private static SkillDefinition D(int id, string code, string name, int cost, int power,
        string description, string passive, SkillActiveEffect active, SkillPassiveEffect passiveEffect)
    {
        return new SkillDefinition(id, code, name, cost, power, description,
            passive, active, passiveEffect);
    }

    private static Dictionary<string, SkillDefinition> BuildLookup()
    {
        var result = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
        foreach (SkillDefinition definition in definitions)
            result.Add(definition.Code, definition);
        return result;
    }
}
