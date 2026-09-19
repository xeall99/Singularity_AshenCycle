using System;
using System.Collections.Generic;

/// <summary>
/// Serializable, deterministic representation of an active or completed run.
/// Values derived from Base Stats are intentionally omitted and recalculated by
/// RunStatController when the state is restored.
/// </summary>
[Serializable]
public sealed class SeededRunState
{
    public const int CurrentSchemaVersion = 2;
    public const string CurrentRulesetVersion = "0.4";

    public int SchemaVersion;
    public string RulesetVersion;
    public int RunSeed;
    public int Depth;
    public int NodeCount;
    public int CurrentHP;
    public int CurrentMana;
    public int TotalEmbers;
    public bool IsActive;
    public bool IsCompleted;
    public SeededRunModifierState[] Modifiers;

    // Unity JsonUtility needs a parameterless constructor when a checkpoint is
    // deserialized. Defaults are still validated by RunManager.RestoreState.
    public SeededRunState()
    {
        SchemaVersion = CurrentSchemaVersion;
        RulesetVersion = CurrentRulesetVersion;
        Depth = RunManager.FirstDepth;
        NodeCount = StandardRunNodeMap.DefaultNodeCount;
        Modifiers = new SeededRunModifierState[0];
    }

    internal static SeededRunState FromRuntime(
        int runSeed,
        int depth,
        int nodeCount,
        int currentHP,
        int currentMana,
        int totalEmbers,
        bool isActive,
        bool isCompleted,
        SeededRunModifierState[] modifiers
    )
    {
        return new SeededRunState
        {
            SchemaVersion = CurrentSchemaVersion,
            RulesetVersion = CurrentRulesetVersion,
            RunSeed = runSeed,
            Depth = depth,
            NodeCount = nodeCount,
            CurrentHP = currentHP,
            CurrentMana = currentMana,
            TotalEmbers = totalEmbers,
            IsActive = isActive,
            IsCompleted = isCompleted,
            Modifiers = modifiers ?? new SeededRunModifierState[0]
        };
    }

    public SeededRunState Clone()
    {
        SeededRunModifierState[] modifierCopy = null;

        if (Modifiers != null)
        {
            modifierCopy = new SeededRunModifierState[Modifiers.Length];

            for (int index = 0; index < Modifiers.Length; index++)
            {
                modifierCopy[index] = Modifiers[index]?.Clone();
            }
        }

        return new SeededRunState
        {
            SchemaVersion = SchemaVersion,
            RulesetVersion = RulesetVersion,
            RunSeed = RunSeed,
            Depth = Depth,
            NodeCount = NodeCount,
            CurrentHP = CurrentHP,
            CurrentMana = CurrentMana,
            TotalEmbers = TotalEmbers,
            IsActive = IsActive,
            IsCompleted = IsCompleted,
            Modifiers = modifierCopy
        };
    }

    internal void Validate(int effectiveMaxHP, int effectiveMaxMana)
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"SeededRunState schema {SchemaVersion} tidak didukung."
            );
        }

        if (!string.Equals(
            RulesetVersion,
            CurrentRulesetVersion,
            StringComparison.Ordinal
        ))
        {
            throw new InvalidOperationException(
                "SeededRunState memakai ruleset version yang berbeda."
            );
        }

        if (RunSeed == 0)
        {
            throw new InvalidOperationException(
                "SeededRunState wajib memiliki seed yang tidak nol."
            );
        }

        if (Depth < RunManager.FirstDepth)
        {
            throw new InvalidOperationException(
                "SeededRunState memiliki depth yang tidak valid."
            );
        }

        if (NodeCount < StandardRunNodeMap.MinimumNodeCount ||
            NodeCount > StandardRunNodeMap.MaximumNodeCount)
        {
            throw new InvalidOperationException(
                "SeededRunState memiliki jumlah node Standard Run " +
                "yang tidak valid."
            );
        }

        if (Depth > NodeCount)
        {
            throw new InvalidOperationException(
                "Depth SeededRunState berada di luar node map."
            );
        }

        if (effectiveMaxHP < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveMaxHP),
                "Effective Max HP harus lebih besar dari nol."
            );
        }

        if (effectiveMaxMana < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveMaxMana),
                "Effective Max Mana tidak boleh negatif."
            );
        }

        if (CurrentHP < 0 || CurrentHP > effectiveMaxHP)
        {
            throw new InvalidOperationException(
                "Current HP pada SeededRunState melewati batas efektif."
            );
        }

        if (CurrentMana < 0 || CurrentMana > effectiveMaxMana)
        {
            throw new InvalidOperationException(
                "Current Mana pada SeededRunState melewati batas efektif."
            );
        }

        if (TotalEmbers < 0)
        {
            throw new InvalidOperationException(
                "Total Embers pada SeededRunState tidak boleh negatif."
            );
        }

        if (IsActive && IsCompleted)
        {
            throw new InvalidOperationException(
                "SeededRunState tidak boleh Active dan Completed bersamaan."
            );
        }

        if (Modifiers == null)
        {
            throw new InvalidOperationException(
                "SeededRunState tidak memiliki daftar modifier yang valid."
            );
        }

        var sourceCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (SeededRunModifierState modifier in Modifiers)
        {
            if (modifier == null)
            {
                throw new InvalidOperationException(
                    "SeededRunState berisi modifier null."
                );
            }

            modifier.Validate();

            if (!sourceCodes.Add(modifier.SourceCode))
            {
                throw new InvalidOperationException(
                    $"Modifier {modifier.SourceCode} tercatat lebih dari sekali."
                );
            }
        }
    }
}

/// <summary>
/// Serializable value object for one active-run modifier. Runtime modifiers
/// remain immutable and are reconstructed by RunModifierCollection.
/// </summary>
[Serializable]
public sealed class SeededRunModifierState
{
    public string SourceCode;
    public RewardEffectType EffectType;
    public int FlatValue;
    public float PercentValue;
    public int StackCount;
    public RunModifierScope Scope;

    public SeededRunModifierState()
    {
    }

    internal static SeededRunModifierState FromRuntime(RunModifier modifier)
    {
        if (modifier == null)
        {
            throw new ArgumentNullException(nameof(modifier));
        }

        return new SeededRunModifierState
        {
            SourceCode = modifier.SourceCode,
            EffectType = modifier.EffectType,
            FlatValue = modifier.FlatValue,
            PercentValue = modifier.PercentValue,
            StackCount = modifier.StackCount,
            Scope = modifier.Scope
        };
    }

    internal RunModifier ToRuntimeModifier()
    {
        Validate();

        return new RunModifier(
            SourceCode,
            EffectType,
            FlatValue,
            PercentValue,
            StackCount,
            Scope
        );
    }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(SourceCode))
        {
            throw new InvalidOperationException(
                "SeededRunState modifier wajib memiliki SourceCode."
            );
        }

        if (!Enum.IsDefined(typeof(RewardEffectType), EffectType))
        {
            throw new InvalidOperationException(
                $"EffectType modifier {SourceCode} tidak valid."
            );
        }

        if (FlatValue < 0 || StackCount < 1)
        {
            throw new InvalidOperationException(
                $"Nilai modifier {SourceCode} tidak valid."
            );
        }

        if (float.IsNaN(PercentValue) ||
            float.IsInfinity(PercentValue) ||
            PercentValue < 0f)
        {
            throw new InvalidOperationException(
                $"PercentValue modifier {SourceCode} tidak valid."
            );
        }

        if (!Enum.IsDefined(typeof(RunModifierScope), Scope))
        {
            throw new InvalidOperationException(
                $"Scope modifier {SourceCode} tidak valid."
            );
        }
    }

    internal SeededRunModifierState Clone()
    {
        return new SeededRunModifierState
        {
            SourceCode = SourceCode,
            EffectType = EffectType,
            FlatValue = FlatValue,
            PercentValue = PercentValue,
            StackCount = StackCount,
            Scope = Scope
        };
    }
}
