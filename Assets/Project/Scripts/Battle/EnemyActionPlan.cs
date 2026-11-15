using System;

/// <summary>
/// Immutable values copied from enemy data at the start of a battle.
/// Normal Attack repeats this same plan for the current single-enemy node.
/// Future action types need their own preparation and execution rules.
/// </summary>
public sealed class EnemyActionPlan
{
    public const string NormalAttackCode = "NORMAL_ATTACK";

    public string ActionCode { get; }
    public string DisplayName { get; }
    public int SourceEnemyId { get; }
    public string Target { get; }
    public int HitCount { get; }
    public int DamagePerHit { get; }
    public int TotalEstimatedDamage { get; }
    public float Multiplier { get; }
    public string EffectDescription { get; }
    public BattleStatusEffectType? AppliedStatus { get; }
    public int StatusDuration { get; }

    private EnemyActionPlan(int sourceEnemyId, int attackDamage)
    {
        if (attackDamage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attackDamage));
        }

        ActionCode = NormalAttackCode;
        DisplayName = "Normal Attack";
        SourceEnemyId = sourceEnemyId;
        Target = "Warden";
        HitCount = 1;
        Multiplier = 1f;
        DamagePerHit = attackDamage;
        TotalEstimatedDamage = DamagePerHit * HitCount;
        EffectDescription = "None";
        AppliedStatus = null;
        StatusDuration = 0;
    }

    public static EnemyActionPlan CreateNormalAttack(int sourceEnemyId, int attackDamage)
    {
        return new EnemyActionPlan(sourceEnemyId, attackDamage);
    }

    private EnemyActionPlan(int sourceEnemyId, int damage, string code, string name, string effect,
        float multiplier = 1f, bool charging = false,
        BattleStatusEffectType? appliedStatus = null, int statusDuration = 0)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        SourceEnemyId = sourceEnemyId;
        ActionCode = code;
        DisplayName = name;
        Target = charging ? "Self" : "Warden";
        HitCount = charging ? 0 : 1;
        DamagePerHit = TotalEstimatedDamage = damage;
        Multiplier = multiplier;
        EffectDescription = effect;
        AppliedStatus = appliedStatus;
        StatusDuration = appliedStatus.HasValue ? Math.Max(1, statusDuration) : 0;
    }

    public static EnemyActionPlan CreateEncounterAction(StandardRunNodeType type, int sourceEnemyId,
        int baseDamage, int turn, bool awakened)
    {
        if (baseDamage < 0 || turn < 0) throw new ArgumentOutOfRangeException();
        if (type == StandardRunNodeType.EliteBattle)
            return turn % 2 == 0 ? CreateNormalAttack(sourceEnemyId, baseDamage) :
                new EnemyActionPlan(sourceEnemyId, ScaleDamage(baseDamage, 135),
                    "ELITE_CRUSH", "Ashen Crush",
                    "Heavy strike. Applies Weakness for 2 turns.", 1.35f,
                    false, BattleStatusEffectType.Weakness, 2);
        if (type != StandardRunNodeType.Boss) return CreateNormalAttack(sourceEnemyId, baseDamage);
        int step = turn % 3;
        if (step == 1)
            return new EnemyActionPlan(sourceEnemyId, 0, "BOSS_CHARGE", "Gathering the Abyss", "No damage. A heavy strike follows next turn.", 0f, true);
        int percent = step == 2 ? (awakened ? 175 : 150) : (awakened ? 125 : 100);
        return new EnemyActionPlan(sourceEnemyId, ScaleDamage(baseDamage, percent),
            step == 2 ? "BOSS_RUPTURE" : "BOSS_STRIKE",
            step == 2 ? "Abyssal Rupture" : "Void Strike",
            step == 2
                ? "Heavy strike. Applies Curse for 2 turns."
                : "Guard reduces this strike. No resource drain.",
            percent / 100f, false,
            step == 2 ? BattleStatusEffectType.Curse : null,
            step == 2 ? 2 : 0);
    }

    private static int ScaleDamage(int damage, int percent) =>
        (int)Math.Min(int.MaxValue, ((long)damage * percent + 99) / 100);
}
