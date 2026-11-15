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
    public const int CurrentSchemaVersion = 4;
    public const string CurrentRulesetVersion = "0.5";

    public int SchemaVersion;
    public string RulesetVersion;
    public int RunSeed;
    public string RunId;
    public int RouteVersion;
    public int Depth;
    public int NodeCount;
    public int CurrentHP;
    public int CurrentMana;
    public int TotalEmbers;
    public bool IsActive;
    public bool IsCompleted;
    public SeededRunModifierState[] Modifiers;
    public string[] VisitedNodeCodes = new string[0];
    public string SelectedNodeCode;
    public bool NodeEntered;
    public float ElapsedSeconds;
    public int BattlesWon;
    public int DamageDealt;
    public int DamageTaken;
    public int HealingReceived;
    public int CriticalHits;
    public int ConvergencesTriggered;
    // Absent in legacy schema-3 saves; zero means bonuses cannot be reconstructed.
    public int ScoreModelVersion;
    public int ScoreableConvergences;
    public int CurrentBattleConvergences;
    public int ActionsTaken;
    public int EventHorizonCount;
    public int ReversalOrbitCount;
    public int UnstablePulseCount;
    public string[] EquippedSkillCodes = { "SKL_SHADOW_STRIKE" };

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
            RunId = RunId,
            RouteVersion = RouteVersion,
            Depth = Depth,
            NodeCount = NodeCount,
            CurrentHP = CurrentHP,
            CurrentMana = CurrentMana,
            TotalEmbers = TotalEmbers,
            IsActive = IsActive,
            IsCompleted = IsCompleted,
            VisitedNodeCodes = VisitedNodeCodes == null ? null : (string[])VisitedNodeCodes.Clone(),
            SelectedNodeCode = SelectedNodeCode,
            NodeEntered = NodeEntered,
            ElapsedSeconds = ElapsedSeconds,
            BattlesWon = BattlesWon,
            DamageDealt = DamageDealt,
            DamageTaken = DamageTaken,
            HealingReceived = HealingReceived,
            CriticalHits = CriticalHits,
            ConvergencesTriggered = ConvergencesTriggered,
            ScoreModelVersion = ScoreModelVersion,
            ScoreableConvergences = ScoreableConvergences,
            CurrentBattleConvergences = CurrentBattleConvergences,
            ActionsTaken = ActionsTaken,
            EventHorizonCount = EventHorizonCount,
            ReversalOrbitCount = ReversalOrbitCount,
            UnstablePulseCount = UnstablePulseCount,
            EquippedSkillCodes = EquippedSkillCodes == null
                ? null : (string[])EquippedSkillCodes.Clone(),
            Modifiers = modifierCopy
        };
    }

    internal void ValidateRoute(StandardRunNodeMap map)
    {
        bool bossResolved = IsCompleted && Depth == NodeCount &&
            VisitedNodeCodes != null && VisitedNodeCodes.Length == Depth;
        if (VisitedNodeCodes == null || VisitedNodeCodes.Length != Depth - (bossResolved ? 0 : 1))
            throw new InvalidOperationException("Checkpoint branch history does not match depth.");
        string previous = null;
        foreach (string code in VisitedNodeCodes)
        {
            bool reachable = false;
            foreach (StandardRunNode choice in map.GetChoices(previous))
                if (choice.Code == code) reachable = true;
            if (!reachable) throw new InvalidOperationException("Checkpoint contains an unreachable branch.");
            previous = code;
        }
        if (NodeEntered && string.IsNullOrEmpty(SelectedNodeCode))
            throw new InvalidOperationException("Entered checkpoint node has no identity.");
        if (string.IsNullOrEmpty(SelectedNodeCode)) return;
        if (bossResolved && SelectedNodeCode == previous && !NodeEntered) return;
        foreach (StandardRunNode choice in map.GetChoices(previous))
            if (choice.Code == SelectedNodeCode && choice.Depth == Depth && NodeEntered) return;
        throw new InvalidOperationException("Checkpoint selection is outside the reachable branches.");
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

        // Older schema-3 checkpoints have no RunId. Restore gives them a
        // stable legacy identity before a permanent reward can be claimed.
        if (!string.IsNullOrEmpty(RunId) &&
            !Guid.TryParse(RunId, out _) &&
            !string.Equals(RunId, $"legacy-{RunSeed}-{RouteVersion}",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("RunId checkpoint tidak valid.");
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

        if (float.IsNaN(ElapsedSeconds) ||
            float.IsInfinity(ElapsedSeconds) ||
            ElapsedSeconds < 0f)
        {
            throw new InvalidOperationException(
                "Elapsed time pada SeededRunState tidak valid."
            );
        }

        if (ScoreModelVersion < 0 || ScoreModelVersion > ScoreCalculator.ModelVersion)
            throw new InvalidOperationException("Unsupported saved score model.");

        if (BattlesWon < 0 || DamageDealt < 0 || DamageTaken < 0 ||
            HealingReceived < 0 || CriticalHits < 0 ||
            ConvergencesTriggered < 0 || ActionsTaken < 0 ||
            ScoreableConvergences < 0 || CurrentBattleConvergences < 0 ||
            EventHorizonCount < 0 || ReversalOrbitCount < 0 ||
            UnstablePulseCount < 0)
        {
            throw new InvalidOperationException(
                "Statistik Run Summary tidak boleh negatif."
            );
        }

        long convergenceTotal = (long)EventHorizonCount +
            ReversalOrbitCount + UnstablePulseCount;
        long scoreEligibleTotal = ScoreModelVersion == ScoreCalculator.ModelVersion
            ? (long)EventHorizonCount + ReversalOrbitCount : convergenceTotal;
        if (convergenceTotal != ConvergencesTriggered ||
            CriticalHits > ActionsTaken || BattlesWon > NodeCount ||
            CurrentBattleConvergences > 3 ||
            (long)ScoreableConvergences + CurrentBattleConvergences >
                scoreEligibleTotal ||
            ScoreableConvergences > 3 * NodeCount)
        {
            throw new InvalidOperationException(
                "Statistik Run Summary tidak konsisten."
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

        if (EquippedSkillCodes == null ||
            EquippedSkillCodes.Length != SkillProgressStore.LoadoutSlotCount)
            throw new InvalidOperationException("Checkpoint skill loadout must contain three slots.");
        var equipped = new HashSet<string>(StringComparer.Ordinal);
        foreach (string skillCode in EquippedSkillCodes)
        {
            if (string.IsNullOrWhiteSpace(skillCode)) continue;
            if (!SkillCatalog.TryGet(skillCode, out _) || !equipped.Add(skillCode))
                throw new InvalidOperationException("Checkpoint has an unknown or duplicate equipped skill.");
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

        RunRelicCatalog.Validate(SourceCode, EffectType, FlatValue, PercentValue, StackCount);
        RunAnomalyCatalog.Validate(SourceCode, EffectType, FlatValue, PercentValue, StackCount);

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
