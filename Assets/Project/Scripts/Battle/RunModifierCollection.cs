using System;
using System.Collections.Generic;

public enum RunModifierScope
{
    ActiveRun
}

public sealed class RunModifier
{
    public string SourceCode { get; }
    public RewardEffectType EffectType { get; }
    public int FlatValue { get; }
    public float PercentValue { get; }
    public int StackCount { get; }
    public RunModifierScope Scope { get; }

    public RunModifier(
        string sourceCode,
        RewardEffectType effectType,
        int flatValue,
        float percentValue,
        int stackCount,
        RunModifierScope scope
    )
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            throw new ArgumentException(
                "Run modifier source wajib diisi.",
                nameof(sourceCode)
            );
        }

        if (stackCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(stackCount));
        }

        SourceCode = sourceCode;
        EffectType = effectType;
        FlatValue = Math.Max(0, flatValue);
        PercentValue = Math.Max(0f, percentValue);
        StackCount = stackCount;
        Scope = scope;
    }

    internal RunModifier AddStack(int flatValue, float percentValue)
    {
        return new RunModifier(
            SourceCode,
            EffectType,
            checked(FlatValue + Math.Max(0, flatValue)),
            PercentValue + Math.Max(0f, percentValue),
            checked(StackCount + 1),
            Scope
        );
    }
}

public sealed class RunModifierCollection
{
    private readonly Dictionary<string, RunModifier> modifiers =
        new Dictionary<string, RunModifier>(StringComparer.Ordinal);

    public int Count => modifiers.Count;

    public void AddOrStack(RewardDefinition reward)
    {
        if (reward == null)
        {
            throw new ArgumentNullException(nameof(reward));
        }

        if (modifiers.TryGetValue(
            reward.Code,
            out RunModifier existingModifier
        ))
        {
            if (existingModifier.EffectType != reward.EffectType)
            {
                throw new InvalidOperationException(
                    $"Reward {reward.Code} berubah effect type dalam run yang sama."
                );
            }

            modifiers[reward.Code] = existingModifier.AddStack(
                reward.FlatValue,
                reward.PercentValue
            );
            return;
        }

        modifiers.Add(
            reward.Code,
            new RunModifier(
                reward.Code,
                reward.EffectType,
                reward.FlatValue,
                reward.PercentValue,
                1,
                RunModifierScope.ActiveRun
            )
        );
    }

    public int GetTotalFlat(RewardEffectType effectType)
    {
        int total = 0;

        foreach (RunModifier modifier in modifiers.Values)
        {
            if (modifier.EffectType == effectType)
            {
                total = checked(total + modifier.FlatValue);
            }
        }

        return total;
    }

    public float GetTotalPercent(RewardEffectType effectType)
    {
        float total = 0f;

        foreach (RunModifier modifier in modifiers.Values)
        {
            if (modifier.EffectType == effectType)
            {
                total += modifier.PercentValue;
            }
        }

        return total;
    }

    public int GetStackCount(RewardEffectType effectType)
    {
        int total = 0;

        foreach (RunModifier modifier in modifiers.Values)
        {
            if (modifier.EffectType == effectType)
            {
                total = checked(total + modifier.StackCount);
            }
        }

        return total;
    }

    public RunModifier[] CreateSnapshot()
    {
        var snapshot = new List<RunModifier>(modifiers.Values);
        snapshot.Sort(
            (left, right) => string.CompareOrdinal(
                left.SourceCode,
                right.SourceCode
            )
        );
        return snapshot.ToArray();
    }

    public SeededRunModifierState[] CreateStateSnapshot()
    {
        RunModifier[] runtimeSnapshot = CreateSnapshot();
        var stateSnapshot = new SeededRunModifierState[runtimeSnapshot.Length];

        for (int index = 0; index < runtimeSnapshot.Length; index++)
        {
            stateSnapshot[index] = SeededRunModifierState.FromRuntime(
                runtimeSnapshot[index]
            );
        }

        return stateSnapshot;
    }

    public void RestoreStateSnapshot(
        SeededRunModifierState[] stateSnapshot
    )
    {
        if (stateSnapshot == null)
        {
            throw new ArgumentNullException(nameof(stateSnapshot));
        }

        // Build and validate the replacement collection first. The current
        // modifiers remain untouched if any checkpoint entry is invalid.
        var restoredModifiers = new Dictionary<string, RunModifier>(
            StringComparer.Ordinal
        );

        foreach (SeededRunModifierState state in stateSnapshot)
        {
            if (state == null)
            {
                throw new InvalidOperationException(
                    "Snapshot modifier tidak boleh null."
                );
            }

            RunModifier modifier = state.ToRuntimeModifier();

            if (restoredModifiers.ContainsKey(modifier.SourceCode))
            {
                throw new InvalidOperationException(
                    $"Modifier {modifier.SourceCode} tercatat lebih dari sekali."
                );
            }

            restoredModifiers.Add(modifier.SourceCode, modifier);
        }

        modifiers.Clear();

        foreach (KeyValuePair<string, RunModifier> pair in restoredModifiers)
        {
            modifiers.Add(pair.Key, pair.Value);
        }
    }

    public void Clear()
    {
        modifiers.Clear();
    }
}
