using System;
using System.Globalization;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;

// Content Manager edits the persistent copy of the content database. The seed
// remains untouched, and shipped combat skills cannot be changed here.
public sealed class SkillContentRepository
{
    public const string CustomCodePrefix = "SKL_CUSTOM_";
    private readonly string databasePath;
    private readonly Func<bool> hasActiveRun;

    public SkillContentRepository(string databasePath, Func<bool> hasActiveRun = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        this.databasePath = Path.GetFullPath(databasePath);
        this.hasActiveRun = hasActiveRun ?? (() => false);
    }

    public static SkillContentRepository CreateDefault()
    {
        string path = Path.Combine(Application.persistentDataPath,
            "Database", "game_data.db");
        string seed = Path.Combine(Application.streamingAssetsPath,
            "Database", "game_seed.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (!File.Exists(path))
        {
            if (!File.Exists(seed))
                throw new FileNotFoundException("Content database seed is missing.", seed);
            File.Copy(seed, path);
        }
        return new SkillContentRepository(path, () =>
            ActiveRunCheckpointStore.CreateDefault().Inspect(100, 10).HasUsableState);
    }

    public SkillData[] ReadAll()
    {
        using (SQLiteConnection connection = Open())
            return connection.Table<SkillData>().ToList()
                .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(skill => skill.Id).ToArray();
    }

    public SkillData Create(string name, int manaCost, int power)
    {
        EnsureWritesAllowed();
        name = Validate(name, manaCost, power);
        SkillData created = null;
        using (SQLiteConnection connection = Open())
        {
            connection.RunInTransaction(() =>
            {
                EnsureUniqueName(connection, name, 0);
                created = new SkillData
                {
                    Id = connection.ExecuteScalar<int>(
                        "SELECT COALESCE(MAX(id), 0) + 1 FROM skills"),
                    Code = CustomCodePrefix + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                    Name = name,
                    ManaCost = manaCost,
                    Power = power,
                    TargetType = "SingleEnemy",
                    EffectType = "Damage",
                    Description = "Content Manager demonstration skill.",
                    AssetKey = string.Empty,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture)
                };
                connection.Insert(created);
            });
        }
        return created;
    }

    public SkillData Update(int id, string name, int manaCost, int power)
    {
        EnsureWritesAllowed();
        name = Validate(name, manaCost, power);
        SkillData updated = null;
        using (SQLiteConnection connection = Open())
        {
            connection.RunInTransaction(() =>
            {
                updated = RequireCustom(connection, id);
                EnsureUniqueName(connection, name, id);
                updated.Name = name;
                updated.ManaCost = manaCost;
                updated.Power = power;
                if (connection.Update(updated) != 1)
                    throw new InvalidOperationException("Skill update did not affect one row.");
            });
        }
        return updated;
    }

    public void Delete(int id)
    {
        EnsureWritesAllowed();
        using (SQLiteConnection connection = Open())
        {
            connection.RunInTransaction(() =>
            {
                SkillData skill = RequireCustom(connection, id);
                if (connection.Delete(skill) != 1)
                    throw new InvalidOperationException("Skill delete did not affect one row.");
            });
        }
    }

    public static bool IsCustom(SkillData skill) => skill != null &&
        skill.Code != null &&
        skill.Code.StartsWith(CustomCodePrefix, StringComparison.Ordinal);

    private SQLiteConnection Open()
    {
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Content database is missing.", databasePath);
        SQLiteConnection connection = new SQLiteConnection(databasePath);
        try
        {
            connection.Execute("PRAGMA foreign_keys = ON");
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private void EnsureWritesAllowed()
    {
        if (hasActiveRun())
            throw new InvalidOperationException(
                "Finish or abandon the active expedition before editing content.");
    }

    private static string Validate(string name, int manaCost, int power)
    {
        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40 ||
            name.Any(char.IsControl))
            throw new ArgumentException("Skill name must be 1–40 printable characters.");
        if (manaCost < 0 || manaCost > 10)
            throw new ArgumentOutOfRangeException(nameof(manaCost),
                "Mana cost must be between 0 and 10.");
        if (power < 0 || power > 999)
            throw new ArgumentOutOfRangeException(nameof(power),
                "Power must be between 0 and 999.");
        return name;
    }

    private static void EnsureUniqueName(SQLiteConnection connection, string name, int exceptId)
    {
        int matches = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM skills WHERE lower(name) = lower(?) AND id <> ?",
            name, exceptId);
        if (matches != 0)
            throw new InvalidOperationException("A skill with this name already exists.");
    }

    private static SkillData RequireCustom(SQLiteConnection connection, int id)
    {
        SkillData skill = connection.Table<SkillData>()
            .Where(row => row.Id == id).FirstOrDefault();
        if (skill == null)
            throw new InvalidOperationException("Skill no longer exists. Refresh the list.");
        if (!IsCustom(skill))
            throw new InvalidOperationException("Core combat skills are read-only.");
        return skill;
    }
}
