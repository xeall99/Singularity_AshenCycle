using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;

[Table("player_skill_unlocks")]
public sealed class PlayerSkillUnlockRow
{
    [PrimaryKey, Column("skill_code")] public string SkillCode { get; set; }
    [Column("unlocked_utc")] public string UnlockedUtc { get; set; }
    [Column("source_run_id")] public string SourceRunId { get; set; }
}

[Table("player_skill_loadout")]
public sealed class PlayerSkillLoadoutRow
{
    [PrimaryKey, Column("slot_index")] public int SlotIndex { get; set; }
    [Column("skill_code")] public string SkillCode { get; set; }
}

[Table("skill_reward_claims")]
public sealed class SkillRewardClaimRow
{
    [PrimaryKey, Column("claim_id")] public string ClaimId { get; set; }
    [Column("run_id")] public string RunId { get; set; }
    [Column("milestone_code")] public string MilestoneCode { get; set; }
    [Column("offer_codes")] public string OfferCodes { get; set; }
    [Column("chosen_skill_code")] public string ChosenSkillCode { get; set; }
    [Column("status")] public string Status { get; set; }
    [Column("created_utc")] public string CreatedUtc { get; set; }
    [Column("claimed_utc")] public string ClaimedUtc { get; set; }

    [Ignore]
    public string[] OfferedSkills => string.IsNullOrEmpty(OfferCodes)
        ? new string[0]
        : OfferCodes.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// Local, single-profile collection/loadout/reward persistence. Reward offers
/// are immutable and idempotent by run + milestone.
/// </summary>
public sealed class SkillProgressStore : IDisposable
{
    public const int LoadoutSlotCount = 3;
    public const string StandardRunMilestone = "STANDARD_RUN_TERMINAL";

    private readonly SQLiteConnection connection;

    public SkillProgressStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        string folder = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        connection = new SQLiteConnection(databasePath);
        InitializeSchema();
    }

    public static SkillProgressStore CreateDefault()
    {
        string folder = Path.Combine(Application.persistentDataPath, "Database");
        return new SkillProgressStore(Path.Combine(folder, "player_progress.db"));
    }

    public PlayerSkillUnlockRow[] GetCollection()
    {
        EnsureStarterSkill();
        return connection.Table<PlayerSkillUnlockRow>()
            .OrderBy(row => row.SkillCode).ToArray();
    }

    public bool Owns(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode)) return false;
        EnsureStarterSkill();
        return connection.Find<PlayerSkillUnlockRow>(skillCode) != null;
    }

    public string[] GetLoadout()
    {
        EnsureStarterSkill();
        string[] result = new string[LoadoutSlotCount];
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (PlayerSkillLoadoutRow row in connection.Table<PlayerSkillLoadoutRow>())
            if (row.SlotIndex >= 0 && row.SlotIndex < result.Length &&
                SkillCatalog.TryGet(row.SkillCode, out _) &&
                connection.Find<PlayerSkillUnlockRow>(row.SkillCode) != null &&
                unique.Add(row.SkillCode))
                result[row.SlotIndex] = row.SkillCode;
        return result;
    }

    public void SaveLoadout(string[] skillCodes)
    {
        if (skillCodes == null || skillCodes.Length != LoadoutSlotCount)
            throw new ArgumentException("Loadout must contain exactly three slots.", nameof(skillCodes));

        var unique = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < skillCodes.Length; i++)
        {
            string code = skillCodes[i];
            if (string.IsNullOrWhiteSpace(code)) continue;
            if (!SkillCatalog.TryGet(code, out _) || !Owns(code))
                throw new InvalidOperationException("Only owned official skills can be equipped.");
            if (!unique.Add(code))
                throw new InvalidOperationException("A skill can only be equipped once.");
        }

        connection.RunInTransaction(() =>
        {
            connection.DeleteAll<PlayerSkillLoadoutRow>();
            for (int i = 0; i < skillCodes.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(skillCodes[i])) continue;
                connection.Insert(new PlayerSkillLoadoutRow
                {
                    SlotIndex = i,
                    SkillCode = skillCodes[i]
                });
            }
        });
    }

    public SkillRewardClaimRow EnsureStandardRunOffer(
        string runId,
        bool hasResolvedNode,
        int runSeed
    )
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run identity is required.", nameof(runId));
        if (!hasResolvedNode) return null;

        string claimId = runId + ":" + StandardRunMilestone;
        SkillRewardClaimRow existing = connection.Find<SkillRewardClaimRow>(claimId);
        if (existing != null) return existing;

        EnsureStarterSkill();
        HashSet<string> owned = new HashSet<string>(
            connection.Table<PlayerSkillUnlockRow>().Select(row => row.SkillCode),
            StringComparer.Ordinal
        );
        SkillDefinition[] offer = SkillCatalog.All
            .Where(definition => !owned.Contains(definition.Code))
            .OrderBy(definition => StableOrder(runId, runSeed, definition.Code))
            .ThenBy(definition => definition.Code, StringComparer.Ordinal)
            .Take(3)
            .ToArray();
        if (offer.Length == 0) return null;

        var row = new SkillRewardClaimRow
        {
            ClaimId = claimId,
            RunId = runId,
            MilestoneCode = StandardRunMilestone,
            OfferCodes = string.Join("\n", offer.Select(item => item.Code)),
            Status = "Pending",
            CreatedUtc = DateTime.UtcNow.ToString("O"),
            ChosenSkillCode = string.Empty,
            ClaimedUtc = string.Empty
        };

        connection.RunInTransaction(() =>
        {
            if (connection.Find<SkillRewardClaimRow>(claimId) == null)
                connection.Insert(row);
        });
        return connection.Find<SkillRewardClaimRow>(claimId) ?? row;
    }

    public SkillRewardClaimRow GetPendingOffer()
    {
        return connection.Table<SkillRewardClaimRow>()
            .Where(row => row.Status == "Pending")
            .OrderBy(row => row.CreatedUtc)
            .FirstOrDefault();
    }

    public SkillRewardClaimRow GetOffer(string claimId)
    {
        return string.IsNullOrWhiteSpace(claimId)
            ? null : connection.Find<SkillRewardClaimRow>(claimId);
    }

    public bool TryClaim(string claimId, string skillCode)
    {
        if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(skillCode))
            return false;
        SkillDefinition skill = SkillCatalog.Get(skillCode);
        if (skill == null) return false;

        bool claimed = false;
        connection.RunInTransaction(() =>
        {
            SkillRewardClaimRow row = connection.Find<SkillRewardClaimRow>(claimId);
            if (row == null) return;
            if (row.Status == "Claimed")
            {
                claimed = string.Equals(row.ChosenSkillCode, skillCode, StringComparison.Ordinal);
                return;
            }
            if (row.Status != "Pending" || !row.OfferedSkills.Contains(skillCode) || Owns(skillCode))
                return;

            connection.Insert(new PlayerSkillUnlockRow
            {
                SkillCode = skillCode,
                UnlockedUtc = DateTime.UtcNow.ToString("O"),
                SourceRunId = row.RunId
            });
            row.ChosenSkillCode = skillCode;
            row.Status = "Claimed";
            row.ClaimedUtc = DateTime.UtcNow.ToString("O");
            connection.Update(row);
            claimed = true;
        });
        return claimed;
    }

    private void InitializeSchema()
    {
        connection.CreateTable<PlayerSkillUnlockRow>();
        connection.CreateTable<PlayerSkillLoadoutRow>();
        connection.CreateTable<SkillRewardClaimRow>();
        connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_skill_claim_run_milestone ON skill_reward_claims(run_id, milestone_code)");
        EnsureStarterSkill();
    }

    private void EnsureStarterSkill()
    {
        if (connection.Find<PlayerSkillUnlockRow>("SKL_SHADOW_STRIKE") == null)
        {
            connection.Insert(new PlayerSkillUnlockRow
            {
                SkillCode = "SKL_SHADOW_STRIKE",
                UnlockedUtc = DateTime.UtcNow.ToString("O"),
                SourceRunId = "STARTER"
            });
        }
        if (!connection.Table<PlayerSkillLoadoutRow>().Any())
        {
            connection.Insert(new PlayerSkillLoadoutRow
            {
                SlotIndex = 0,
                SkillCode = "SKL_SHADOW_STRIKE"
            });
        }
    }

    private static long StableOrder(string runId, int seed, string code)
    {
        unchecked
        {
            ulong hash = 14695981039346656037UL;
            string value = runId + "|" + seed + "|" + code + "|SKILL-CATALOG-1";
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
            return (long)(hash & 0x7fffffffffffffffUL);
        }
    }

    public void Dispose()
    {
        connection?.Close();
        connection?.Dispose();
    }
}
