using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>A compact, immutable-on-disk record of one finished Standard Run.</summary>
[Serializable]
public sealed class RunHistoryRecord
{
    public string RunId;
    public long FinishedUtcTicks;
    public RunSummaryOutcome Outcome;
    public string FinalEnemyName;
    public int RunSeed;
    public int NodeCount;
    public int NodesResolved;
    public int CurrentHP;
    public int EffectiveMaxHP;
    public int CurrentMana;
    public int EffectiveMaxMana;
    public int TotalEmbers;
    public int EchoEarned;
    public int BattlesWon;
    public int DamageDealt;
    public int DamageTaken;
    public int ScoreModelVersion;
    public string RulesetVersion;
    public string Mode;
    public int Cycle;
    public int Score;
    public int BattlePoints;
    public int EventPoints;
    public int ElitePoints;
    public int BossPoints;
    public int ConvergencePoints;
    public float ElapsedSeconds;
    public string RouteText;
    public string RelicsText;
    public string AnomaliesText;
    public string ModifiersText;

    public DateTime FinishedUtc => new DateTime(FinishedUtcTicks, DateTimeKind.Utc);

    internal static RunHistoryRecord FromSnapshot(RunSummarySnapshot snapshot)
    {
        ScoreResult score = snapshot.Score;
        return new RunHistoryRecord
        {
            RunId = snapshot.RunId,
            FinishedUtcTicks = DateTime.UtcNow.Ticks,
            Outcome = snapshot.Outcome,
            FinalEnemyName = snapshot.FinalEnemyName,
            RunSeed = snapshot.RunSeed,
            NodeCount = snapshot.NodeCount,
            NodesResolved = snapshot.NodesResolved,
            CurrentHP = snapshot.CurrentHP,
            EffectiveMaxHP = snapshot.EffectiveMaxHP,
            CurrentMana = snapshot.CurrentMana,
            EffectiveMaxMana = snapshot.EffectiveMaxMana,
            TotalEmbers = snapshot.TotalEmbers,
            EchoEarned = snapshot.EchoEarned,
            BattlesWon = snapshot.BattlesWon,
            DamageDealt = snapshot.DamageDealt,
            DamageTaken = snapshot.DamageTaken,
            ScoreModelVersion = score.ModelVersion,
            RulesetVersion = SeededRunState.CurrentRulesetVersion,
            Mode = "STANDARD",
            Cycle = 1,
            Score = score.Total,
            BattlePoints = score.BattlePoints,
            EventPoints = score.EventPoints,
            ElitePoints = score.ElitePoints,
            BossPoints = score.BossPoints,
            ConvergencePoints = score.ConvergencePoints,
            ElapsedSeconds = snapshot.ElapsedSeconds,
            RouteText = snapshot.RouteText,
            RelicsText = snapshot.RelicsText,
            AnomaliesText = snapshot.AnomaliesText,
            ModifiersText = snapshot.ModifiersText
        };
    }
}

/// <summary>
/// Local finished-run archive. Each RunId is committed at most once, independently
/// of the active-run checkpoint and Echo Fragment wallet.
/// </summary>
public sealed class RunHistoryStore
{
    private const int FormatVersion = 1;
    private const int MaximumRecords = 10000;
    private const string FileName = "syngrava-run-history-v1.json";
    private readonly string directoryPath;

    [Serializable]
    private sealed class HistoryData
    {
        public int Version = FormatVersion;
        public RunHistoryRecord[] Records = new RunHistoryRecord[0];
    }

    [Serializable]
    private sealed class Envelope
    {
        public string Payload;
        public string Sha256;
    }

    public string HistoryPath { get; }
    public string BackupPath => HistoryPath + ".bak";

    public RunHistoryStore(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("History directory is required.", nameof(directoryPath));
        this.directoryPath = Path.GetFullPath(directoryPath);
        HistoryPath = Path.Combine(this.directoryPath, FileName);
    }

    public static RunHistoryStore CreateDefault() =>
        new RunHistoryStore(Path.Combine(Application.persistentDataPath, "Saves"));

    /// <returns>True only for a new record; a replay of the same RunId is harmless.</returns>
    public bool RecordFinishedRun(RunSummarySnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (!snapshot.IsTerminal || snapshot.EchoSaveFailed)
            throw new InvalidOperationException("History requires a finished run and a saved Echo award.");

        HistoryData data = ReadData(out bool primaryValid);
        foreach (RunHistoryRecord previous in data.Records)
            if (string.Equals(previous.RunId, snapshot.RunId, StringComparison.Ordinal))
                return false;
        if (data.Records.Length >= MaximumRecords)
            throw new InvalidOperationException("Run History is full.");

        RunHistoryRecord record = RunHistoryRecord.FromSnapshot(snapshot);
        ValidateRecord(record);
        var records = new RunHistoryRecord[data.Records.Length + 1];
        Array.Copy(data.Records, records, data.Records.Length);
        records[records.Length - 1] = record;
        data.Records = records;
        Save(data, primaryValid);
        return true;
    }

    /// <summary>Newest first. Returned records are detached from the store.</summary>
    public RunHistoryRecord[] ReadRecent(int maximum = 10000)
    {
        if (maximum < 0 || maximum > MaximumRecords)
            throw new ArgumentOutOfRangeException(nameof(maximum));
        RunHistoryRecord[] source = ReadData(out _).Records;
        int count = Math.Min(maximum, source.Length);
        var recent = new RunHistoryRecord[count];
        for (int index = 0; index < count; index++)
            recent[index] = JsonUtility.FromJson<RunHistoryRecord>(
                JsonUtility.ToJson(source[source.Length - 1 - index]));
        return recent;
    }

    private HistoryData ReadData(out bool primaryValid)
    {
        primaryValid = false;
        if (File.Exists(HistoryPath))
        {
            try
            {
                HistoryData primary = Load(HistoryPath);
                primaryValid = true;
                return primary;
            }
            catch (Exception error) when (error is IOException ||
                                          error is InvalidOperationException ||
                                          error is ArgumentException)
            {
                if (!File.Exists(BackupPath))
                    throw new InvalidOperationException("Run History is damaged and has no backup.", error);
            }
        }
        return File.Exists(BackupPath) ? Load(BackupPath) : new HistoryData();
    }

    private static HistoryData Load(string path)
    {
        Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
        if (envelope == null || string.IsNullOrEmpty(envelope.Payload) ||
            !string.Equals(Hash(envelope.Payload), envelope.Sha256,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Run History integrity check failed.");
        HistoryData data = JsonUtility.FromJson<HistoryData>(envelope.Payload);
        if (data == null || data.Version != FormatVersion || data.Records == null ||
            data.Records.Length > MaximumRecords)
            throw new InvalidOperationException("Run History format is invalid.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (RunHistoryRecord record in data.Records)
        {
            ValidateRecord(record);
            if (!ids.Add(record.RunId))
                throw new InvalidOperationException("Run History has duplicate RunIds.");
        }
        return data;
    }

    private static void ValidateRecord(RunHistoryRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.RunId) ||
            record.RunId.Length > 100 ||
            (!Guid.TryParse(record.RunId, out _) &&
             !record.RunId.StartsWith("legacy-", StringComparison.Ordinal)) ||
            record.FinishedUtcTicks < DateTime.UnixEpoch.Ticks ||
            record.FinishedUtcTicks > DateTime.MaxValue.Ticks ||
            !Enum.IsDefined(typeof(RunSummaryOutcome), record.Outcome) ||
            record.RunSeed == 0 || record.NodeCount < 1 ||
            record.NodesResolved < 0 || record.NodesResolved > record.NodeCount ||
            record.EffectiveMaxHP < 1 || record.CurrentHP < 0 ||
            record.CurrentHP > record.EffectiveMaxHP ||
            record.EffectiveMaxMana < 1 || record.CurrentMana < 0 ||
            record.CurrentMana > record.EffectiveMaxMana ||
            record.TotalEmbers < 0 || record.EchoEarned < 0 ||
            record.BattlesWon < 0 || record.DamageDealt < 0 ||
            record.DamageTaken < 0 || float.IsNaN(record.ElapsedSeconds) ||
            float.IsInfinity(record.ElapsedSeconds) || record.ElapsedSeconds < 0f ||
            record.ScoreModelVersion < 0 ||
            record.ScoreModelVersion > ScoreCalculator.ModelVersion ||
            TooLongOptional(record.RulesetVersion, 30) ||
            // Legacy archives were Standard-only. JsonUtility writes a missing
            // string back as "", so both null and empty are valid legacy modes.
            (!string.IsNullOrEmpty(record.Mode) &&
             !string.Equals(record.Mode, "STANDARD", StringComparison.Ordinal)) ||
            record.Cycle < 0 || record.Cycle > 1 ||
            record.Score < 0 || record.BattlePoints < 0 ||
            record.EventPoints < 0 || record.ElitePoints < 0 ||
            record.BossPoints < 0 || record.ConvergencePoints < 0 ||
            (record.ScoreModelVersion == 0 &&
             (record.Score != 0 || record.BattlePoints != 0 ||
              record.EventPoints != 0 || record.ElitePoints != 0 ||
              record.BossPoints != 0 || record.ConvergencePoints != 0)) ||
            (record.ScoreModelVersion > 0 &&
             (long)record.BattlePoints + record.EventPoints +
                 record.ElitePoints + record.BossPoints +
                 record.ConvergencePoints != record.Score) ||
            TooLong(record.FinalEnemyName, 100) || TooLong(record.RouteText, 2000) ||
            TooLong(record.RelicsText, 1000) || TooLong(record.AnomaliesText, 1000) ||
            TooLong(record.ModifiersText, 1000))
            throw new InvalidOperationException("Run History record is invalid.");
    }

    private static bool TooLong(string value, int maximum) =>
        value == null || value.Length > maximum;

    private static bool TooLongOptional(string value, int maximum) =>
        value != null && value.Length > maximum;

    private void Save(HistoryData data, bool primaryValid)
    {
        Directory.CreateDirectory(directoryPath);
        string payload = JsonUtility.ToJson(data);
        string content = JsonUtility.ToJson(new Envelope
        {
            Payload = payload,
            Sha256 = Hash(payload)
        }, true);
        string temporary = HistoryPath + ".tmp";
        byte[] bytes = new UTF8Encoding(false).GetBytes(content);
        using (var stream = new FileStream(temporary, FileMode.Create,
            FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (primaryValid)
            File.Replace(temporary, HistoryPath, BackupPath);
        else
        {
            // A valid backup must survive repair of a corrupt primary.
            if (File.Exists(HistoryPath)) File.Delete(HistoryPath);
            File.Move(temporary, HistoryPath);
        }
    }

    private static string Hash(string value)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                .Replace("-", "").ToLowerInvariant();
    }
}
