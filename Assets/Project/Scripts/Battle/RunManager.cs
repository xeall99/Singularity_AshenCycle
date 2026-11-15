using System;
using System.Collections.Generic;

public sealed class RunManager
{
    public const int FirstDepth = 1;

    public RunModifierCollection Modifiers { get; }
    public int RunSeed { get; private set; }
    public string RunId { get; private set; }
    public int Depth { get; private set; } = FirstDepth;
    public int CurrentHP { get; private set; }
    public int CurrentMana { get; private set; }
    public int TotalEmbers { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int BattlesWon { get; private set; }
    public int DamageDealt { get; private set; }
    public int DamageTaken { get; private set; }
    public int HealingReceived { get; private set; }
    public int CriticalHits { get; private set; }
    public int ConvergencesTriggered { get; private set; }
    public int ScoreableConvergences { get; private set; }
    public int CurrentBattleConvergences { get; private set; }
    public int ActionsTaken { get; private set; }
    public int EventHorizonCount { get; private set; }
    public int ReversalOrbitCount { get; private set; }
    public int UnstablePulseCount { get; private set; }
    public string[] EquippedSkillCodes { get; private set; } = new[] { "SKL_SHADOW_STRIKE", null, null };
    public StandardRunNodeMap NodeMap { get; private set; }
    private readonly List<string> visitedNodeCodes = new List<string>();
    private string selectedNodeCode;
    public bool NodeEntered { get; private set; }
    public string[] VisitedNodeCodes => visitedNodeCodes.ToArray();
    public string LastVisitedNodeCode => visitedNodeCodes.Count == 0
        ? null : visitedNodeCodes[visitedNodeCodes.Count - 1];
    public StandardRunNode[] ReachableNodes => NodeMap == null || !IsActive
        ? new StandardRunNode[0] : NodeMap.GetChoices(LastVisitedNodeCode);

    public bool HasVisited(string code) => visitedNodeCodes.Contains(code);

    public bool CanEnterNode(string code)
    {
        if (!IsActive || IsCompleted || NodeEntered) return false;
        foreach (StandardRunNode choice in ReachableNodes)
            if (choice.Code == code && choice.Depth == Depth) return true;
        return false;
    }

    public bool TryEnterNode(string code)
    {
        if (!CanEnterNode(code)) return false;
        selectedNodeCode = code;
        NodeEntered = true;
        return true;
    }

    public StandardRunNode CurrentNode
    {
        get
        {
            if (NodeMap == null ||
                Depth < FirstDepth ||
                Depth > NodeMap.Count)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(selectedNodeCode)) return NodeMap.FindNode(selectedNodeCode);
            // Compatibility default for depth-based callers; the map still
            // requires an explicit choice before dispatching an encounter.
            StandardRunNode[] choices = NodeMap.GetChoices(LastVisitedNodeCode);
            return choices.Length > 0 ? choices[0] : NodeMap.GetNodeAtDepth(Depth);
        }
    }

    public bool HasNextNode
    {
        get
        {
            return NodeMap != null && Depth < NodeMap.Count;
        }
    }

    public RunManager(RunModifierCollection modifiers)
    {
        Modifiers = modifiers ??
            throw new ArgumentNullException(nameof(modifiers));
    }

    public void StartNewRun(
        int runSeed,
        int startingHP,
        int startingMana,
        int effectiveMaxHP,
        int effectiveMaxMana
    )
    {
        StartNewRunInternal(
            runSeed,
            StandardRunNodeMap.DefaultNodeCount,
            startingHP,
            startingMana,
            effectiveMaxHP,
            effectiveMaxMana
        );
    }

    public void StartNewRunWithNodeCount(
        int runSeed,
        int nodeCount,
        int startingHP,
        int startingMana,
        int effectiveMaxHP,
        int effectiveMaxMana
    )
    {
        StartNewRunInternal(
            runSeed,
            nodeCount,
            startingHP,
            startingMana,
            effectiveMaxHP,
            effectiveMaxMana
        );
    }

    private void StartNewRunInternal(
        int runSeed,
        int nodeCount,
        int startingHP,
        int startingMana,
        int effectiveMaxHP,
        int effectiveMaxMana
    )
    {
        if (runSeed == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSeed),
                "Run seed tidak boleh nol."
            );
        }

        // Generate and validate the complete route before mutating live run
        // state. Invalid node rules therefore cannot partially reset a run.
        StandardRunNodeMap generatedMap = StandardRunNodeMap.Generate(
            runSeed,
            nodeCount
        );

        Modifiers.Clear();
        RunId = Guid.NewGuid().ToString("N");
        RunSeed = runSeed;
        NodeMap = generatedMap;
        visitedNodeCodes.Clear();
        selectedNodeCode = null;
        NodeEntered = false;
        Depth = FirstDepth;
        TotalEmbers = 0;
        ElapsedSeconds = 0f;
        BattlesWon = 0;
        DamageDealt = 0;
        DamageTaken = 0;
        HealingReceived = 0;
        CriticalHits = 0;
        ConvergencesTriggered = 0;
        ScoreableConvergences = 0;
        CurrentBattleConvergences = 0;
        ActionsTaken = 0;
        EventHorizonCount = 0;
        ReversalOrbitCount = 0;
        UnstablePulseCount = 0;
        EquippedSkillCodes = new[] { "SKL_SHADOW_STRIKE", null, null };
        IsActive = true;
        IsCompleted = false;
        SetResources(
            startingHP,
            startingMana,
            effectiveMaxHP,
            effectiveMaxMana
        );
    }

    public void SetResources(
        int currentHP,
        int currentMana,
        int effectiveMaxHP,
        int effectiveMaxMana
    )
    {
        CurrentHP = ClampResource(currentHP, effectiveMaxHP);
        CurrentMana = ClampResource(currentMana, effectiveMaxMana);
    }

    public void ClampResources(int effectiveMaxHP, int effectiveMaxMana)
    {
        SetResources(
            CurrentHP,
            CurrentMana,
            effectiveMaxHP,
            effectiveMaxMana
        );
    }

    public bool TrySpendMana(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (CurrentMana < amount)
        {
            return false;
        }

        CurrentMana -= amount;
        return true;
    }

    public int RestoreMana(int amount, int effectiveMaxMana)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        int previousMana = CurrentMana;
        CurrentMana = ClampResource(
            SaturatingAdd(CurrentMana, amount),
            effectiveMaxMana
        );
        return CurrentMana - previousMana;
    }

    public int ApplyDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        int previousHP = CurrentHP;
        CurrentHP = Math.Max(0, CurrentHP - amount);
        int applied = previousHP - CurrentHP;
        DamageTaken = SaturatingAdd(DamageTaken, applied);
        return applied;
    }

    public int RestoreHP(int amount, int effectiveMaxHP)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        int previousHP = CurrentHP;
        CurrentHP = ClampResource(
            SaturatingAdd(CurrentHP, amount),
            effectiveMaxHP
        );
        int restored = CurrentHP - previousHP;
        HealingReceived = SaturatingAdd(HealingReceived, restored);
        return restored;
    }

    public void AdvanceElapsedTime(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!IsActive) return;
        ElapsedSeconds = Math.Min(float.MaxValue, ElapsedSeconds + seconds);
    }

    public void RecordPlayerAction(int damageDealt, bool critical)
    {
        if (damageDealt < 0)
            throw new ArgumentOutOfRangeException(nameof(damageDealt));
        ActionsTaken = SaturatingAdd(ActionsTaken, 1);
        DamageDealt = SaturatingAdd(DamageDealt, damageDealt);
        if (critical) CriticalHits = SaturatingAdd(CriticalHits, 1);
    }

    public void RecordConvergence(ConvergenceType type)
    {
        if (type == ConvergenceType.None) return;
        ConvergencesTriggered = SaturatingAdd(ConvergencesTriggered, 1);
        bool namedPattern = type == ConvergenceType.EventHorizon ||
            type == ConvergenceType.ReversalOrbit;
        if (namedPattern && IsActive && CurrentNode != null && CurrentNode.IsBattle &&
            CurrentBattleConvergences < 3)
            CurrentBattleConvergences++;
        switch (type)
        {
            case ConvergenceType.EventHorizon:
                EventHorizonCount = SaturatingAdd(EventHorizonCount, 1);
                break;
            case ConvergenceType.ReversalOrbit:
                ReversalOrbitCount = SaturatingAdd(ReversalOrbitCount, 1);
                break;
            case ConvergenceType.UnstablePulse:
                UnstablePulseCount = SaturatingAdd(UnstablePulseCount, 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public void RecordBattleWon()
    {
        BattlesWon = SaturatingAdd(BattlesWon, 1);
    }

    public void AddEmbers(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        TotalEmbers = SaturatingAdd(TotalEmbers, amount);
    }

    public bool TrySpendEmbers(int amount)
    {
        if (amount < 0 || amount > TotalEmbers) return false;
        TotalEmbers -= amount;
        return true;
    }

    public bool TryAdvanceDepth()
    {
        StandardRunNode currentNode = CurrentNode;

        if (currentNode == null || currentNode.IsFinal)
        {
            return false;
        }

        return TryResolveCurrentNode(
            currentNode.Type,
            currentNode.Code
        );
    }

    /// <summary>
    /// Commits exactly one resolved node. The expected type and stable code
    /// prevent a stale callback, wrong node handler, or repeated click from
    /// advancing the run twice.
    /// </summary>
    public bool TryResolveCurrentNode(
        StandardRunNodeType expectedType,
        string expectedNodeCode
    )
    {
        if (!IsActive ||
            IsCompleted ||
            NodeMap == null ||
            string.IsNullOrWhiteSpace(expectedNodeCode))
        {
            return false;
        }

        NodeMap.Validate();

        StandardRunNode currentNode = CurrentNode;

        if (currentNode == null ||
            currentNode.Type != expectedType ||
            !string.Equals(
                currentNode.Code,
                expectedNodeCode,
                StringComparison.Ordinal
            ))
        {
            return false;
        }

        // Award only resolved combat. Failed battles cannot retain a farmed
        // Convergence bonus; each combat node has its own three-trigger cap.
        if (currentNode.IsBattle)
            ScoreableConvergences = SaturatingAdd(
                ScoreableConvergences, CurrentBattleConvergences);
        CurrentBattleConvergences = 0;

        if (currentNode.IsFinal)
        {
            visitedNodeCodes.Add(currentNode.Code);
            selectedNodeCode = currentNode.Code;
            NodeEntered = false;
            CompleteRun();
            return true;
        }

        visitedNodeCodes.Add(currentNode.Code);
        selectedNodeCode = null;
        NodeEntered = false;
        Depth++;
        return true;
    }

    public void MarkDefeated()
    {
        IsActive = false;
        IsCompleted = false;
    }

    public void CompleteRun()
    {
        IsActive = false;
        IsCompleted = true;
    }

    public void ClearModifiers()
    {
        Modifiers.Clear();
    }

    public void SetEquippedSkillCodes(string[] skillCodes)
    {
        if (skillCodes == null || skillCodes.Length != SkillProgressStore.LoadoutSlotCount)
            throw new ArgumentException("Run loadout must contain three slots.", nameof(skillCodes));
        var unique = new HashSet<string>(StringComparer.Ordinal);
        var copy = (string[])skillCodes.Clone();
        foreach (string code in copy)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            if (!SkillCatalog.TryGet(code, out _) || !unique.Add(code))
                throw new InvalidOperationException("Run loadout has an unknown or duplicate skill.");
        }
        EquippedSkillCodes = copy;
    }

    public SeededRunState CaptureState()
    {
        if (RunSeed == 0 || NodeMap == null)
        {
            throw new InvalidOperationException(
                "RunManager belum memiliki run yang dapat disnapshot."
            );
        }

        NodeMap.Validate();

        if (NodeMap.RunSeed != RunSeed)
        {
            throw new InvalidOperationException(
                "Seed RunManager dan StandardRunNodeMap tidak sama."
            );
        }

        SeededRunState snapshot = SeededRunState.FromRuntime(
            RunSeed,
            Depth,
            NodeMap.Count,
            CurrentHP,
            CurrentMana,
            TotalEmbers,
            IsActive,
            IsCompleted,
            Modifiers.CreateStateSnapshot()
        );
        snapshot.VisitedNodeCodes = visitedNodeCodes.ToArray();
        snapshot.SelectedNodeCode = selectedNodeCode;
        snapshot.NodeEntered = NodeEntered;
        snapshot.RouteVersion = NodeMap.RouteVersion;
        snapshot.RunId = RunId;
        snapshot.ElapsedSeconds = ElapsedSeconds;
        snapshot.BattlesWon = BattlesWon;
        snapshot.DamageDealt = DamageDealt;
        snapshot.DamageTaken = DamageTaken;
        snapshot.HealingReceived = HealingReceived;
        snapshot.CriticalHits = CriticalHits;
        snapshot.ConvergencesTriggered = ConvergencesTriggered;
        snapshot.ScoreModelVersion = ScoreCalculator.ModelVersion;
        snapshot.ScoreableConvergences = ScoreableConvergences;
        snapshot.CurrentBattleConvergences = CurrentBattleConvergences;
        snapshot.ActionsTaken = ActionsTaken;
        snapshot.EventHorizonCount = EventHorizonCount;
        snapshot.ReversalOrbitCount = ReversalOrbitCount;
        snapshot.UnstablePulseCount = UnstablePulseCount;
        snapshot.EquippedSkillCodes = (string[])EquippedSkillCodes.Clone();
        return snapshot;
    }

    public void RestoreState(
        SeededRunState state,
        int effectiveMaxHP,
        int effectiveMaxMana
    )
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        state.Validate(effectiveMaxHP, effectiveMaxMana);

        // The route is derived from the checkpoint identity. Rebuild it before
        // replacing any mutable runtime state so a corrupt depth or node count
        // cannot partially restore modifiers or resources.
        if (state.RouteVersion < 0 || state.RouteVersion > 1)
            throw new InvalidOperationException("Unsupported saved route version.");
        StandardRunNodeMap restoredMap = state.RouteVersion == 0
            ? StandardRunNodeMap.GenerateLegacy(state.RunSeed, state.NodeCount)
            : StandardRunNodeMap.Generate(state.RunSeed, state.NodeCount);

        if (state.Depth > restoredMap.Count)
        {
            throw new InvalidOperationException(
                "Depth checkpoint berada di luar Standard Run Node Map."
            );
        }
        state.ValidateRoute(restoredMap);

        // RestoreStateSnapshot validates and constructs all modifier records
        // before replacing the current collection, keeping this operation
        // safe when a checkpoint is corrupt or from another ruleset.
        Modifiers.RestoreStateSnapshot(state.Modifiers);

        RunSeed = state.RunSeed;
        RunId = string.IsNullOrEmpty(state.RunId)
            ? $"legacy-{state.RunSeed}-{state.RouteVersion}"
            : state.RunId;
        NodeMap = restoredMap;
        visitedNodeCodes.Clear();
        visitedNodeCodes.AddRange(state.VisitedNodeCodes);
        selectedNodeCode = state.SelectedNodeCode;
        NodeEntered = state.NodeEntered;
        Depth = state.Depth;
        CurrentHP = state.CurrentHP;
        CurrentMana = state.CurrentMana;
        TotalEmbers = state.TotalEmbers;
        ElapsedSeconds = state.ElapsedSeconds;
        BattlesWon = state.BattlesWon;
        DamageDealt = state.DamageDealt;
        DamageTaken = state.DamageTaken;
        HealingReceived = state.HealingReceived;
        CriticalHits = state.CriticalHits;
        ConvergencesTriggered = state.ConvergencesTriggered;
        // Older counters included Unstable Pulse and cannot be split back into
        // named patterns per battle. Keep the run playable without inventing
        // retroactive bonuses; new named patterns use the current score model.
        bool currentScoreModel = state.ScoreModelVersion == ScoreCalculator.ModelVersion;
        ScoreableConvergences = currentScoreModel ? state.ScoreableConvergences : 0;
        CurrentBattleConvergences = currentScoreModel ? state.CurrentBattleConvergences : 0;
        ActionsTaken = state.ActionsTaken;
        EventHorizonCount = state.EventHorizonCount;
        ReversalOrbitCount = state.ReversalOrbitCount;
        UnstablePulseCount = state.UnstablePulseCount;
        EquippedSkillCodes = (string[])state.EquippedSkillCodes.Clone();
        IsActive = state.IsActive;
        IsCompleted = state.IsCompleted;
    }

    private static int ClampResource(int value, int maximum)
    {
        return Math.Min(Math.Max(0, value), Math.Max(0, maximum));
    }

    private static int SaturatingAdd(int current, int amount)
    {
        long total = (long)current + amount;
        return total >= int.MaxValue ? int.MaxValue : (int)total;
    }
}
