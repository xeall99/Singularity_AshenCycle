using System;

/// <summary>Versioned, deterministic Standard Run points. No time or farmable resource contributes.</summary>
public static class ScoreCalculator
{
    public const int ModelVersion = 2;

    public static ScoreResult Calculate(RunSummarySnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (!snapshot.IsTerminal)
            throw new ArgumentException("A score requires a finished run.", nameof(snapshot));
        return Calculate(snapshot.VisitedNodeTypes, snapshot.ScoreableConvergences);
    }

    public static ScoreResult Calculate(
        StandardRunNodeType[] resolvedNodes,
        int scoreableConvergences)
    {
        if (resolvedNodes == null) throw new ArgumentNullException(nameof(resolvedNodes));
        if (resolvedNodes.Length > StandardRunNodeMap.MaximumNodeCount ||
            scoreableConvergences < 0)
            throw new ArgumentOutOfRangeException(nameof(resolvedNodes));

        int normal = 0;
        int events = 0;
        int elite = 0;
        int boss = 0;
        foreach (StandardRunNodeType node in resolvedNodes)
        {
            switch (node)
            {
                case StandardRunNodeType.NormalBattle: normal++; break;
                case StandardRunNodeType.RandomEvent: events++; break;
                case StandardRunNodeType.EliteBattle: elite++; break;
                case StandardRunNodeType.Boss: boss++; break;
                case StandardRunNodeType.Rest:
                case StandardRunNodeType.Merchant:
                case StandardRunNodeType.Relic:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolvedNodes));
            }
        }
        if (boss > 1 || scoreableConvergences > 3 * (normal + elite + boss))
            throw new ArgumentException("The resolved-node score counters are inconsistent.");

        return new ScoreResult(
            normal * 100,
            events * 75,
            elite * 500,
            boss * 1500,
            scoreableConvergences * 50);
    }
}

public sealed class ScoreResult
{
    public int ModelVersion => ScoreCalculator.ModelVersion;
    public int BattlePoints { get; }
    public int EventPoints { get; }
    public int ElitePoints { get; }
    public int BossPoints { get; }
    public int ConvergencePoints { get; }
    public int Total => BattlePoints + EventPoints + ElitePoints + BossPoints + ConvergencePoints;

    internal ScoreResult(int battle, int events, int elite, int boss, int convergence)
    {
        BattlePoints = battle;
        EventPoints = events;
        ElitePoints = elite;
        BossPoints = boss;
        ConvergencePoints = convergence;
    }
}
