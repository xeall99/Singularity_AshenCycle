using System;
using System.IO;
using SQLite;
using UnityEngine;

// A separate SQLite file keeps preferences independent of the seeded content
// database, which the Editor refreshes during Play Mode initialization.
public sealed class UserSettingsStore
{
    private readonly string databasePath;

    public UserSettingsStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Settings database path is required.", nameof(databasePath));
        this.databasePath = Path.GetFullPath(databasePath);
    }

    public static UserSettingsStore CreateDefault()
    {
        return new UserSettingsStore(Path.Combine(
            Application.persistentDataPath, "Database", "user_settings.db"));
    }

    public string Read(string key)
    {
        ValidateKey(key);
        using (SQLiteConnection connection = Open())
        {
            UserSettingEntry entry = connection.Table<UserSettingEntry>()
                .Where(setting => setting.Key == key).FirstOrDefault();
            return entry?.Value;
        }
    }

    public void Write(string key, string value)
    {
        ValidateKey(key);
        if (value == null) throw new ArgumentNullException(nameof(value));
        using (SQLiteConnection connection = Open())
            connection.InsertOrReplace(new UserSettingEntry { Key = key, Value = value });
    }

    private SQLiteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
        SQLiteConnection connection = new SQLiteConnection(databasePath);
        try
        {
            connection.CreateTable<UserSettingEntry>();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Settings key is required.", nameof(key));
    }

    [Table("user_settings")]
    public sealed class UserSettingEntry
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
