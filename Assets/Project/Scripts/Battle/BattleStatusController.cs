using System;
using System.Collections.Generic;

public enum BattleStatusEffectType
{
    Burn,
    Weakness,
    Curse
}

public enum BattleStatusTarget
{
    Player,
    Enemy
}

public sealed class BattleStatusState
{
    public BattleStatusEffectType EffectType { get; }
    public BattleStatusTarget Target { get; }
    public int StackCount { get; }
    public int RemainingTurns { get; }

    public BattleStatusState(
        BattleStatusEffectType effectType,
        BattleStatusTarget target,
        int stackCount,
        int remainingTurns
    )
    {
        EffectType = effectType;
        Target = target;
        StackCount = stackCount;
        RemainingTurns = remainingTurns;
    }
}

public sealed class BattleStatusTickResult
{
    public int BurnDamage { get; }
    public BattleStatusState[] ExpiredStatuses { get; }

    public BattleStatusTickResult(
        int burnDamage,
        BattleStatusState[] expiredStatuses
    )
    {
        BurnDamage = Math.Max(0, burnDamage);
        ExpiredStatuses = expiredStatuses ?? new BattleStatusState[0];
    }
}

/// <summary>
/// Owns temporary, battle-scoped status state. Status duration advances only
/// when the affected unit finishes its turn, keeping tick timing deterministic.
/// </summary>
public sealed class BattleStatusController
{
    public const int DefaultDuration = 2;
    public const int BurnMaximumStacks = 3;
    public const float WeaknessDamageMultiplier = 0.75f;
    public const float CurseDamageMultiplier = 1.20f;

    private readonly Dictionary<string, BattleStatusState> statuses =
        new Dictionary<string, BattleStatusState>(StringComparer.Ordinal);

    public int Count => statuses.Count;

    public BattleStatusState Apply(
        BattleStatusEffectType effectType,
        BattleStatusTarget target,
        int stackAmount = 1,
        int duration = DefaultDuration
    )
    {
        if (stackAmount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(stackAmount));
        }

        if (duration < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        string key = CreateKey(effectType, target);
        int maximumStacks = effectType == BattleStatusEffectType.Burn
            ? BurnMaximumStacks
            : 1;
        int currentStacks = statuses.TryGetValue(
            key,
            out BattleStatusState existing
        )
            ? existing.StackCount
            : 0;
        int nextStacks = Math.Min(maximumStacks, currentStacks + stackAmount);
        var next = new BattleStatusState(
            effectType,
            target,
            nextStacks,
            duration
        );
        statuses[key] = next;
        return next;
    }

    public bool TryGet(
        BattleStatusEffectType effectType,
        BattleStatusTarget target,
        out BattleStatusState state
    )
    {
        return statuses.TryGetValue(CreateKey(effectType, target), out state);
    }

    public int GetStacks(
        BattleStatusEffectType effectType,
        BattleStatusTarget target
    )
    {
        return TryGet(effectType, target, out BattleStatusState state)
            ? state.StackCount
            : 0;
    }

    public int GetRemainingTurns(
        BattleStatusEffectType effectType,
        BattleStatusTarget target
    )
    {
        return TryGet(effectType, target, out BattleStatusState state)
            ? state.RemainingTurns
            : 0;
    }

    public bool RemoveOne(
        BattleStatusEffectType effectType,
        BattleStatusTarget target
    )
    {
        string key = CreateKey(effectType, target);
        if (!statuses.TryGetValue(key, out BattleStatusState existing)) return false;
        if (existing.StackCount <= 1)
        {
            statuses.Remove(key);
            return true;
        }
        statuses[key] = new BattleStatusState(
            existing.EffectType, existing.Target,
            existing.StackCount - 1, existing.RemainingTurns
        );
        return true;
    }

    public bool ExtendDuration(
        BattleStatusEffectType effectType,
        BattleStatusTarget target,
        int extraTurns
    )
    {
        if (extraTurns < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(extraTurns));
        }

        string key = CreateKey(effectType, target);
        if (!statuses.TryGetValue(key, out BattleStatusState existing)) return false;
        statuses[key] = new BattleStatusState(
            existing.EffectType,
            existing.Target,
            existing.StackCount,
            checked(existing.RemainingTurns + extraTurns)
        );
        return true;
    }

    public int ConsumeStacks(
        BattleStatusEffectType effectType,
        BattleStatusTarget target,
        int maximum
    )
    {
        if (maximum < 1) return 0;
        int current = GetStacks(effectType, target);
        int consumed = Math.Min(maximum, current);
        if (consumed <= 0) return 0;
        string key = CreateKey(effectType, target);
        if (consumed == current)
        {
            statuses.Remove(key);
            return consumed;
        }
        BattleStatusState existing = statuses[key];
        statuses[key] = new BattleStatusState(
            effectType, target, current - consumed, existing.RemainingTurns
        );
        return consumed;
    }

    public int ResolveOutgoingDamage(
        BattleStatusTarget attacker,
        int baseDamage
    )
    {
        ValidateDamage(baseDamage);
        if (!TryGet(
            BattleStatusEffectType.Weakness,
            attacker,
            out BattleStatusState _
        ))
        {
            return baseDamage;
        }

        return RoundDamage(baseDamage * WeaknessDamageMultiplier);
    }

    public int ResolveIncomingDamage(
        BattleStatusTarget target,
        int baseDamage
    )
    {
        ValidateDamage(baseDamage);
        if (!TryGet(
            BattleStatusEffectType.Curse,
            target,
            out BattleStatusState _
        ))
        {
            return baseDamage;
        }

        return RoundDamage(baseDamage * CurseDamageMultiplier);
    }

    public BattleStatusTickResult ResolveEndTurn(BattleStatusTarget target)
    {
        int burnDamage = GetStacks(BattleStatusEffectType.Burn, target);
        var keys = new List<string>();

        foreach (KeyValuePair<string, BattleStatusState> pair in statuses)
        {
            if (pair.Value.Target == target)
            {
                keys.Add(pair.Key);
            }
        }

        var expired = new List<BattleStatusState>();
        foreach (string key in keys)
        {
            BattleStatusState current = statuses[key];
            int remainingTurns = current.RemainingTurns - 1;

            if (remainingTurns <= 0)
            {
                statuses.Remove(key);
                expired.Add(current);
                continue;
            }

            statuses[key] = new BattleStatusState(
                current.EffectType,
                current.Target,
                current.StackCount,
                remainingTurns
            );
        }

        return new BattleStatusTickResult(burnDamage, expired.ToArray());
    }

    public BattleStatusState[] CreateSnapshot(BattleStatusTarget target)
    {
        var snapshot = new List<BattleStatusState>();
        foreach (BattleStatusState state in statuses.Values)
        {
            if (state.Target == target)
            {
                snapshot.Add(state);
            }
        }

        snapshot.Sort((left, right) => left.EffectType.CompareTo(right.EffectType));
        return snapshot.ToArray();
    }

    public void Clear()
    {
        statuses.Clear();
    }

    private static string CreateKey(
        BattleStatusEffectType effectType,
        BattleStatusTarget target
    )
    {
        return $"{(int)target}:{(int)effectType}";
    }

    private static void ValidateDamage(int damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }
    }

    private static int RoundDamage(float damage)
    {
        if (damage <= 0f)
        {
            return 0;
        }

        return Math.Max(
            1,
            (int)Math.Round(damage, MidpointRounding.AwayFromZero)
        );
    }
}
