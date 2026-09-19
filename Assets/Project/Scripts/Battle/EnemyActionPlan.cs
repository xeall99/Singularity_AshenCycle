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
    }

    public static EnemyActionPlan CreateNormalAttack(int sourceEnemyId, int attackDamage)
    {
        return new EnemyActionPlan(sourceEnemyId, attackDamage);
    }
}
