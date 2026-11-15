using System;
using System.Collections.Generic;

public enum RunSummaryOutcome
{
    Cleared,
    Defeated
}

/// <summary>
/// Immutable presentation snapshot captured before a finished run is reset.
/// It contains display-ready values only and never mutates the run.
/// </summary>
public sealed class RunSummarySnapshot
{
    public RunSummaryOutcome Outcome { get; }
    public string EndingReason { get; }
    public string FinalEnemyName { get; }
    public string RunId { get; }
    public bool IsTerminal { get; }
    public int RunSeed { get; }
    public int NodeCount { get; }
    public int NodesResolved { get; }
    public int CurrentHP { get; }
    public int EffectiveMaxHP { get; }
    public int CurrentMana { get; }
    public int EffectiveMaxMana { get; }
    public int TotalEmbers { get; }
    public int EchoEarned { get; }
    public int EchoBalance { get; }
    public int EchoBattleReward { get; }
    public int EchoEliteReward { get; }
    public int EchoBossReward { get; }
    public int EchoClearBonus { get; }
    public int EchoFound { get; }
    public bool EchoDefeatPenalty { get; }
    public bool EchoPreviouslyClaimed { get; }
    public bool EchoSaveFailed { get; }
    public bool HistorySaveFailed { get; }
    public int BattlesWon { get; }
    public int DamageDealt { get; }
    public int DamageTaken { get; }
    public int HealingReceived { get; }
    public int CriticalHits { get; }
    public int ConvergencesTriggered { get; }
    public int ScoreableConvergences { get; }
    public ScoreResult Score { get; }
    public int ActionsTaken { get; }
    public float ElapsedSeconds { get; }
    public string DurationText { get; }
    public string FavoriteConvergence { get; }
    public string RelicsText { get; }
    public string AnomaliesText { get; }
    public string ModifiersText { get; }
    public string RouteText { get; }
    public StandardRunNodeType[] VisitedNodeTypes { get; }

    private RunSummarySnapshot(
        RunSummaryOutcome outcome,
        string endingReason,
        string finalEnemyName,
        RunManager run,
        RunStatController stats,
        EchoFragmentAward echoAward,
        EchoFragmentReceipt echoReceipt,
        bool echoSaveFailed,
        bool historySaveFailed
    )
    {
        Outcome = outcome;
        EndingReason = endingReason ?? string.Empty;
        FinalEnemyName = finalEnemyName ?? string.Empty;
        RunId = run.RunId;
        IsTerminal = !run.IsActive &&
            ((outcome == RunSummaryOutcome.Cleared && run.IsCompleted) ||
             (outcome == RunSummaryOutcome.Defeated && !run.IsCompleted));
        RunSeed = run.RunSeed;
        NodeCount = run.NodeMap == null ? 0 : run.NodeMap.Count;
        NodesResolved = run.VisitedNodeCodes.Length;
        CurrentHP = run.CurrentHP;
        EffectiveMaxHP = stats.EffectiveMaxHP;
        CurrentMana = run.CurrentMana;
        EffectiveMaxMana = stats.EffectiveMaxMana;
        TotalEmbers = run.TotalEmbers;
        EchoEarned = echoReceipt?.Earned ?? 0;
        EchoBalance = echoReceipt?.Balance ?? 0;
        EchoBattleReward = echoAward?.NormalBattleFragments ?? 0;
        EchoEliteReward = echoAward?.EliteFragments ?? 0;
        EchoBossReward = echoAward?.BossFragments ?? 0;
        EchoClearBonus = echoAward?.ClearBonus ?? 0;
        EchoFound = echoAward?.Found ?? 0;
        EchoDefeatPenalty = outcome == RunSummaryOutcome.Defeated;
        EchoPreviouslyClaimed = echoReceipt?.WasAlreadyClaimed ?? false;
        EchoSaveFailed = echoSaveFailed;
        HistorySaveFailed = historySaveFailed;
        BattlesWon = run.BattlesWon;
        DamageDealt = run.DamageDealt;
        DamageTaken = run.DamageTaken;
        HealingReceived = run.HealingReceived;
        CriticalHits = run.CriticalHits;
        ConvergencesTriggered = run.ConvergencesTriggered;
        ScoreableConvergences = run.ScoreableConvergences;
        ActionsTaken = run.ActionsTaken;
        ElapsedSeconds = run.ElapsedSeconds;
        DurationText = FormatDuration(run.ElapsedSeconds);
        FavoriteConvergence = GetFavoriteConvergence(run);
        VisitedNodeTypes = ResolveVisitedTypes(run);
        RouteText = BuildRouteText(VisitedNodeTypes);
        Score = ScoreCalculator.Calculate(VisitedNodeTypes, ScoreableConvergences);

        BuildModifierText(
            run.Modifiers,
            out string relics,
            out string anomalies,
            out string modifiers
        );
        RelicsText = relics;
        AnomaliesText = anomalies;
        ModifiersText = modifiers;
    }

    public static RunSummarySnapshot Create(
        RunSummaryOutcome outcome,
        string endingReason,
        string finalEnemyName,
        RunManager run,
        RunStatController stats,
        EchoFragmentAward echoAward = null,
        EchoFragmentReceipt echoReceipt = null,
        bool echoSaveFailed = false,
        bool historySaveFailed = false
    )
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        if (stats == null) throw new ArgumentNullException(nameof(stats));

        return new RunSummarySnapshot(
            outcome,
            endingReason,
            finalEnemyName,
            run,
            stats,
            echoAward,
            echoReceipt,
            echoSaveFailed,
            historySaveFailed
        );
    }

    private static string FormatDuration(float seconds)
    {
        int totalSeconds = Math.Max(0, (int)Math.Floor(seconds));
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds / 60 % 60;
        int remainingSeconds = totalSeconds % 60;
        return hours > 0
            ? $"{hours:00}:{minutes:00}:{remainingSeconds:00}"
            : $"{minutes:00}:{remainingSeconds:00}";
    }

    private static string GetFavoriteConvergence(RunManager run)
    {
        int maximum = Math.Max(
            run.EventHorizonCount,
            Math.Max(run.ReversalOrbitCount, run.UnstablePulseCount)
        );

        if (maximum <= 0) return "NONE RECORDED";
        if (run.EventHorizonCount == maximum) return "EVENT HORIZON";
        if (run.ReversalOrbitCount == maximum) return "REVERSAL ORBIT";
        return "UNSTABLE PULSE";
    }

    private static StandardRunNodeType[] ResolveVisitedTypes(RunManager run)
    {
        if (run.NodeMap == null) return new StandardRunNodeType[0];

        var types = new List<StandardRunNodeType>();
        foreach (string code in run.VisitedNodeCodes)
        {
            StandardRunNode node = run.NodeMap.FindNode(code);
            if (node != null) types.Add(node.Type);
        }
        return types.ToArray();
    }

    private static string BuildRouteText(StandardRunNodeType[] types)
    {
        if (types == null || types.Length == 0) return "NO NODES RESOLVED";

        var labels = new List<string>(types.Length);
        foreach (StandardRunNodeType type in types)
        {
            labels.Add(GetNodeLabel(type));
        }
        return string.Join("  ›  ", labels);
    }

    private static string GetNodeLabel(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.NormalBattle:
                return "BATTLE";
            case StandardRunNodeType.EliteBattle:
                return "ELITE";
            case StandardRunNodeType.RandomEvent:
                return "EVENT";
            case StandardRunNodeType.Rest:
                return "REST";
            case StandardRunNodeType.Merchant:
                return "MERCHANT";
            case StandardRunNodeType.Relic:
                return "RELIC";
            case StandardRunNodeType.Boss:
                return "BOSS";
            default:
                return type.ToString().ToUpperInvariant();
        }
    }

    private static void BuildModifierText(
        RunModifierCollection collection,
        out string relicsText,
        out string anomaliesText,
        out string modifiersText
    )
    {
        var relicNames = new List<string>();
        var anomalyNames = new List<string>();

        foreach (RunModifier modifier in collection.CreateSnapshot())
        {
            if (RunRelicCatalog.IsRelic(modifier.EffectType))
            {
                relicNames.Add(FindDisplayName(
                    RunRelicCatalog.GetAllDefinitions(),
                    modifier.SourceCode
                ));
            }
            else if (RunAnomalyCatalog.IsAnomaly(modifier.EffectType))
            {
                anomalyNames.Add(FindDisplayName(
                    RunAnomalyCatalog.GetDefinitions(),
                    modifier.SourceCode
                ));
            }
        }

        var modifierLabels = new List<string>();
        float attack = collection.GetTotalPercent(RewardEffectType.AttackPercent);
        int maxHP = collection.GetTotalFlat(RewardEffectType.MaxHPFlat);
        float guard = collection.GetTotalPercent(
            RewardEffectType.GuardStrengthPercent
        );
        if (attack > 0f)
            modifierLabels.Add($"Attack +{ToWholePercent(attack)}%");
        if (maxHP > 0)
            modifierLabels.Add($"Max HP +{maxHP}");
        if (guard > 0f)
            modifierLabels.Add($"Guard +{ToWholePercent(guard)}%");

        relicsText = JoinOrNone(relicNames);
        anomaliesText = JoinOrNone(anomalyNames);
        modifiersText = JoinOrNone(modifierLabels);
    }

    private static string FindDisplayName(
        RewardDefinition[] definitions,
        string sourceCode
    )
    {
        foreach (RewardDefinition definition in definitions)
        {
            if (string.Equals(
                definition.Code,
                sourceCode,
                StringComparison.Ordinal
            ))
            {
                return definition.DisplayName;
            }
        }
        return sourceCode;
    }

    private static int ToWholePercent(float value)
    {
        return (int)Math.Round(
            value * 100f,
            MidpointRounding.AwayFromZero
        );
    }

    private static string JoinOrNone(List<string> values)
    {
        return values.Count == 0 ? "NONE" : string.Join("  •  ", values);
    }
}
