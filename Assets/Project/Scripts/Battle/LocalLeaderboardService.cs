using System;
using System.Collections.Generic;

/// <summary>A read-only Standard Run ranking derived from committed Run History.</summary>
public sealed class LocalLeaderboardService
{
    private readonly RunHistoryStore history;

    public LocalLeaderboardService(RunHistoryStore history)
    {
        this.history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public LocalLeaderboardEntry[] ReadTopTen() =>
        RankStandardRuns(history.ReadRecent());

    public static LocalLeaderboardEntry[] RankStandardRuns(
        IEnumerable<RunHistoryRecord> records,
        int maximum = 10)
    {
        if (records == null) throw new ArgumentNullException(nameof(records));
        if (maximum < 1 || maximum > 10)
            throw new ArgumentOutOfRangeException(nameof(maximum));

        var candidates = new List<LocalLeaderboardEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (RunHistoryRecord record in records)
        {
            if (record == null || record.ScoreModelVersion != ScoreCalculator.ModelVersion ||
                (!string.IsNullOrEmpty(record.Mode) &&
                 !string.Equals(record.Mode, "STANDARD", StringComparison.Ordinal)) ||
                string.IsNullOrEmpty(record.RunId) || !seen.Add(record.RunId))
                continue;
            candidates.Add(new LocalLeaderboardEntry(record));
        }
        candidates.Sort(Compare);
        int count = Math.Min(maximum, candidates.Count);
        var result = new LocalLeaderboardEntry[count];
        for (int index = 0; index < count; index++)
        {
            candidates[index].Rank = index + 1;
            result[index] = candidates[index];
        }
        return result;
    }

    private static int Compare(LocalLeaderboardEntry left, LocalLeaderboardEntry right)
    {
        int order = right.Score.CompareTo(left.Score);
        if (order == 0) order = right.NodesResolved.CompareTo(left.NodesResolved);
        if (order == 0) order = right.BossCount.CompareTo(left.BossCount);
        if (order == 0) order = left.ElapsedSeconds.CompareTo(right.ElapsedSeconds);
        if (order == 0) order = left.FinishedUtcTicks.CompareTo(right.FinishedUtcTicks);
        return order == 0
            ? string.CompareOrdinal(left.RunId, right.RunId) : order;
    }
}

public sealed class LocalLeaderboardEntry
{
    public int Rank { get; internal set; }
    public string RunId { get; }
    public RunSummaryOutcome Outcome { get; }
    public int Score { get; }
    public int NodesResolved { get; }
    public int NodeCount { get; }
    public int BossCount { get; }
    public int Cycle { get; }
    public float ElapsedSeconds { get; }
    public int RunSeed { get; }
    public long FinishedUtcTicks { get; }
    public string RulesetVersion { get; }
    public int BattlePoints { get; }
    public int EventPoints { get; }
    public int ElitePoints { get; }
    public int BossPoints { get; }
    public int ConvergencePoints { get; }

    public DateTime FinishedUtc => new DateTime(FinishedUtcTicks, DateTimeKind.Utc);

    internal LocalLeaderboardEntry(RunHistoryRecord record)
    {
        RunId = record.RunId;
        Outcome = record.Outcome;
        Score = record.Score;
        NodesResolved = record.NodesResolved;
        NodeCount = record.NodeCount;
        BossCount = record.BossPoints / 1500;
        Cycle = record.Cycle == 0 ? 1 : record.Cycle;
        ElapsedSeconds = record.ElapsedSeconds;
        RunSeed = record.RunSeed;
        FinishedUtcTicks = record.FinishedUtcTicks;
        RulesetVersion = record.RulesetVersion;
        BattlePoints = record.BattlePoints;
        EventPoints = record.EventPoints;
        ElitePoints = record.ElitePoints;
        BossPoints = record.BossPoints;
        ConvergencePoints = record.ConvergencePoints;
    }
}
