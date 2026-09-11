using System;
using System.IO;
using SQLite;
using UnityEngine;

public class GameDatabase : MonoBehaviour
{
    public static GameDatabase Instance { get; private set; }

    public bool IsReady { get; private set; }

    private SQLiteConnection connection;
    private string runtimeDatabasePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            string seedDatabasePath = Path.Combine(
                Application.streamingAssetsPath,
                "Database",
                "game_seed.db"
            );

            string runtimeDatabaseFolder = Path.Combine(
                Application.persistentDataPath,
                "Database"
            );

            runtimeDatabasePath = Path.Combine(
                runtimeDatabaseFolder,
                "game_data.db"
            );

            if (!File.Exists(seedDatabasePath))
            {
                Debug.LogError(
                    $"Database seed tidak ditemukan: " +
                    $"{seedDatabasePath}"
                );

                return;
            }

            Directory.CreateDirectory(runtimeDatabaseFolder);

#if UNITY_EDITOR
            File.Copy(
                seedDatabasePath,
                runtimeDatabasePath,
                true
            );

            Debug.Log(
                "Database runtime diperbarui dari seed."
            );
#else
            if (!File.Exists(runtimeDatabasePath))
            {
                File.Copy(
                    seedDatabasePath,
                    runtimeDatabasePath
                );

                Debug.Log(
                    "Database seed berhasil disalin."
                );
            }
#endif

            connection = new SQLiteConnection(
                runtimeDatabasePath
            );

            IsReady = true;

            TestReadData();
        }
        catch (Exception exception)
        {
            IsReady = false;

            Debug.LogError(
                $"Gagal membuka database: " +
                $"{exception.Message}"
            );
        }
    }

    public SkillData GetSkillByCode(string skillCode)
    {
        if (!IsReady || connection == null)
        {
            Debug.LogError("Database belum siap.");
            return null;
        }

        return connection
            .Table<SkillData>()
            .Where(skill => skill.Code == skillCode)
            .FirstOrDefault();
    }

    public EnemyData GetEnemyByBattleIndex(int battleIndex)
    {
        if (!IsReady || connection == null)
        {
            Debug.LogError("Database belum siap.");
            return null;
        }

        return connection
            .Table<EnemyData>()
            .Where(enemy =>
                enemy.BattleIndex == battleIndex)
            .FirstOrDefault();
    }

    private void TestReadData()
    {
        SkillData skill = GetSkillByCode(
            "SKL_SHADOW_STRIKE"
        );

        if (skill == null)
        {
            Debug.LogError(
                "Skill SKL_SHADOW_STRIKE tidak ditemukan."
            );
        }
        else
        {
            Debug.Log(
                $"SQLite berhasil membaca skill! " +
                $"Nama: {skill.Name} | " +
                $"Mana: {skill.ManaCost} | " +
                $"Power: {skill.Power}"
            );
        }

        EnemyData enemy = GetEnemyByBattleIndex(1);

        if (enemy == null)
        {
            Debug.LogError(
                "Enemy untuk Battle 1 tidak ditemukan."
            );
        }
        else
        {
            Debug.Log(
                $"SQLite berhasil membaca enemy! " +
                $"Nama: {enemy.Name} | " +
                $"HP: {enemy.MaxHP} | " +
                $"Damage: {enemy.AttackDamage}"
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        connection?.Close();
        connection?.Dispose();

        connection = null;
        Instance = null;
    }
}