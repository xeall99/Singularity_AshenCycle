using System;

public sealed class RunManager
{
    public const int FirstDepth = 1;

    public RunModifierCollection Modifiers { get; }
    public int RunSeed { get; private set; }
    public int Depth { get; private set; } = FirstDepth;
    public int CurrentHP { get; private set; }
    public int CurrentMana { get; private set; }
    public int TotalEmbers { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public StandardRunNodeMap NodeMap { get; private set; }

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

            return NodeMap.GetNodeAtDepth(Depth);
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
        RunSeed = runSeed;
        NodeMap = generatedMap;
        Depth = FirstDepth;
        TotalEmbers = 0;
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
        return previousHP - CurrentHP;
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
        return CurrentHP - previousHP;
    }

    public void AddEmbers(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        TotalEmbers = SaturatingAdd(TotalEmbers, amount);
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

        if (currentNode.IsFinal)
        {
            CompleteRun();
            return true;
        }

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

        return SeededRunState.FromRuntime(
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
        StandardRunNodeMap restoredMap = StandardRunNodeMap.Generate(
            state.RunSeed,
            state.NodeCount
        );

        if (state.Depth > restoredMap.Count)
        {
            throw new InvalidOperationException(
                "Depth checkpoint berada di luar Standard Run Node Map."
            );
        }

        // RestoreStateSnapshot validates and constructs all modifier records
        // before replacing the current collection, keeping this operation
        // safe when a checkpoint is corrupt or from another ruleset.
        Modifiers.RestoreStateSnapshot(state.Modifiers);

        RunSeed = state.RunSeed;
        NodeMap = restoredMap;
        Depth = state.Depth;
        CurrentHP = state.CurrentHP;
        CurrentMana = state.CurrentMana;
        TotalEmbers = state.TotalEmbers;
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
