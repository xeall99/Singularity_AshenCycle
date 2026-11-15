using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public enum ActiveRunCheckpointStatus
{
    None,
    ValidPrimary,
    RecoverableBackup,
    Invalid
}

/// <summary>
/// Read-only result used by startup UI. Inspecting never mutates the live run.
/// </summary>
public sealed class ActiveRunCheckpointInspection
{
    private readonly SeededRunState state;

    public ActiveRunCheckpointStatus Status { get; }
    public string Message { get; }
    public bool CanContinue => Status == ActiveRunCheckpointStatus.ValidPrimary;
    public bool CanRecover => Status == ActiveRunCheckpointStatus.RecoverableBackup;
    public bool HasUsableState => CanContinue || CanRecover;

    public ActiveRunCheckpointInspection(
        ActiveRunCheckpointStatus status,
        SeededRunState state,
        string message
    )
    {
        Status = status;
        this.state = state?.Clone();
        Message = message ?? string.Empty;
    }

    public SeededRunState CreateStateCopy()
    {
        return state?.Clone();
    }
}

/// <summary>
/// Persists one active Standard Run at a committed map boundary. The previous
/// committed checkpoint is retained as a recovery copy whenever primary save
/// replacement succeeds.
/// </summary>
public sealed class ActiveRunCheckpointStore
{
    public const int CurrentFileVersion = 1;
    public const string DefaultFileName = "syngrava-active-run-v0.4.json";

    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private readonly string directoryPath;

    public string CheckpointPath { get; }
    public string BackupPath => CheckpointPath + ".bak";
    public bool HasCheckpoint => File.Exists(CheckpointPath);
    public bool HasBackup => File.Exists(BackupPath);

    public ActiveRunCheckpointStore(string directoryPath)
        : this(directoryPath, DefaultFileName)
    {
    }

    public ActiveRunCheckpointStore(string directoryPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException(
                "Directory checkpoint wajib diisi.",
                nameof(directoryPath)
            );
        }

        if (string.IsNullOrWhiteSpace(fileName) ||
            Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException(
                "Nama file checkpoint tidak valid.",
                nameof(fileName)
            );
        }

        this.directoryPath = Path.GetFullPath(directoryPath);
        CheckpointPath = Path.Combine(this.directoryPath, fileName);
    }

    public static ActiveRunCheckpointStore CreateDefault()
    {
        return new ActiveRunCheckpointStore(
            Path.Combine(Application.persistentDataPath, "Saves")
        );
    }

    public void SaveActiveRun(RunManager run)
    {
        SaveRun(run, false);
    }

    private void SaveRun(RunManager run, bool preserveBackup)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        if (!run.IsActive || run.IsCompleted)
        {
            throw new InvalidOperationException(
                "Checkpoint hanya boleh menyimpan run aktif."
            );
        }
        if (run.NodeEntered)
        {
            throw new InvalidOperationException(
                "Checkpoint tidak boleh dibuat ketika node sedang diproses."
            );
        }

        SeededRunState state = run.CaptureState();
        string stateJson = JsonUtility.ToJson(state);
        var envelope = new ActiveRunCheckpointEnvelope
        {
            FileVersion = CurrentFileVersion,
            SavedAtUtc = DateTime.UtcNow.ToString("O"),
            StateJson = stateJson,
            StateSha256 = ComputeSha256(stateJson)
        };
        Directory.CreateDirectory(directoryPath);
        WriteAtomically(JsonUtility.ToJson(envelope, true), preserveBackup);
    }

    public SeededRunState LoadActiveRun()
    {
        return LoadStateFromPath(CheckpointPath, "Active Run");
    }

    public SeededRunState LoadBackupRun()
    {
        return LoadStateFromPath(BackupPath, "backup Active Run");
    }

    /// <summary>
    /// Verifies envelope, hash, schema, ruleset, modifiers and branch graph in
    /// a disposable RunManager. Supplied values are base resource caps.
    /// </summary>
    public ActiveRunCheckpointInspection Inspect(
        int baseMaxHP,
        int baseMaxMana
    )
    {
        if (!HasCheckpoint && !HasBackup)
        {
            return new ActiveRunCheckpointInspection(
                ActiveRunCheckpointStatus.None,
                null,
                "Belum ada perjalanan yang dapat dilanjutkan."
            );
        }

        Exception primaryFailure = null;
        if (HasCheckpoint)
        {
            try
            {
                SeededRunState primary = ValidateState(
                    LoadActiveRun(), baseMaxHP, baseMaxMana
                );
                return new ActiveRunCheckpointInspection(
                    ActiveRunCheckpointStatus.ValidPrimary,
                    primary,
                    "Save utama siap dilanjutkan."
                );
            }
            catch (Exception exception)
            {
                primaryFailure = exception;
            }
        }

        if (HasBackup)
        {
            try
            {
                SeededRunState backup = ValidateState(
                    LoadBackupRun(), baseMaxHP, baseMaxMana
                );
                return new ActiveRunCheckpointInspection(
                    ActiveRunCheckpointStatus.RecoverableBackup,
                    backup,
                    "Save utama rusak, tetapi backup terakhir masih aman."
                );
            }
            catch (Exception backupFailure)
            {
                string message = primaryFailure == null
                    ? backupFailure.Message
                    : primaryFailure.Message + " | " + backupFailure.Message;
                return new ActiveRunCheckpointInspection(
                    ActiveRunCheckpointStatus.Invalid,
                    null,
                    message
                );
            }
        }

        return new ActiveRunCheckpointInspection(
            ActiveRunCheckpointStatus.Invalid,
            null,
            primaryFailure?.Message ?? "Save tidak dapat diverifikasi."
        );
    }

    public void RestoreActiveRun(
        RunManager target,
        int effectiveMaxHP,
        int effectiveMaxMana
    )
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        target.RestoreState(LoadActiveRun(), effectiveMaxHP, effectiveMaxMana);
    }

    public void RecoverBackup(
        RunManager target,
        int baseMaxHP,
        int baseMaxMana
    )
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        SeededRunState recovered = ValidateState(
            LoadBackupRun(), baseMaxHP, baseMaxMana
        );
        GetEffectiveCaps(recovered, baseMaxHP, baseMaxMana,
            out int effectiveMaxHP, out int effectiveMaxMana);
        var staged = new RunManager(new RunModifierCollection());
        staged.RestoreState(recovered, effectiveMaxHP, effectiveMaxMana);
        // Promote durably before changing live state. The verified backup
        // remains available even if the primary replacement fails.
        SaveRun(staged, true);
        target.RestoreState(recovered, effectiveMaxHP, effectiveMaxMana);
    }

    public bool DeleteCheckpoint()
    {
        bool deleted = false;
        deleted |= DeleteIfExists(CheckpointPath);
        deleted |= DeleteIfExists(BackupPath);
        DeleteIfExists(GetTemporaryPath());
        DeleteIfExists(GetPreviousPath());
        return deleted;
    }

    private SeededRunState LoadStateFromPath(string path, string label)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Checkpoint {label} tidak ditemukan.", path
            );
        }

        string checkpointJson = File.ReadAllText(path, Encoding.UTF8);
        ActiveRunCheckpointEnvelope envelope;
        try
        {
            envelope = JsonUtility.FromJson<ActiveRunCheckpointEnvelope>(
                checkpointJson
            );
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"Checkpoint {label} bukan JSON yang valid.", exception
            );
        }

        if (envelope == null ||
            envelope.FileVersion != CurrentFileVersion ||
            string.IsNullOrWhiteSpace(envelope.StateJson) ||
            string.IsNullOrWhiteSpace(envelope.StateSha256))
        {
            throw new InvalidDataException(
                $"Envelope checkpoint {label} tidak valid."
            );
        }

        string expectedHash = ComputeSha256(envelope.StateJson);
        if (!string.Equals(expectedHash, envelope.StateSha256,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Integritas checkpoint {label} gagal diverifikasi."
            );
        }

        SeededRunState state;
        try
        {
            state = JsonUtility.FromJson<SeededRunState>(envelope.StateJson);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"State checkpoint {label} tidak dapat dibaca.", exception
            );
        }

        if (state == null || !state.IsActive || state.IsCompleted ||
            state.NodeEntered)
        {
            throw new InvalidDataException(
                $"Checkpoint {label} tidak berada pada boundary run yang aman."
            );
        }

        // Schema 3 checkpoints predate skill loadouts. Keep the verified route,
        // stats, and modifiers while assigning the original starter skill.
        if (state.SchemaVersion == 3)
        {
            state.SchemaVersion = SeededRunState.CurrentSchemaVersion;
            state.RulesetVersion = SeededRunState.CurrentRulesetVersion;
            state.EquippedSkillCodes = new[] { "SKL_SHADOW_STRIKE", null, null };
        }
        return state;
    }

    private static SeededRunState ValidateState(
        SeededRunState state,
        int baseMaxHP,
        int baseMaxMana
    )
    {
        GetEffectiveCaps(state, baseMaxHP, baseMaxMana,
            out int effectiveMaxHP, out int effectiveMaxMana);
        var probe = new RunManager(new RunModifierCollection());
        probe.RestoreState(state.Clone(), effectiveMaxHP, effectiveMaxMana);
        return probe.CaptureState();
    }

    public static void GetEffectiveCaps(
        SeededRunState state,
        int baseMaxHP,
        int baseMaxMana,
        out int effectiveMaxHP,
        out int effectiveMaxMana
    )
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        var modifiers = new RunModifierCollection();
        modifiers.RestoreStateSnapshot(state.Modifiers);
        var stats = new RunStatController(
            baseMaxHP, baseMaxMana, 1, 0f, modifiers
        );
        effectiveMaxHP = stats.EffectiveMaxHP;
        effectiveMaxMana = stats.EffectiveMaxMana;
    }

    private void WriteAtomically(string checkpointJson, bool preserveBackup)
    {
        string temporaryPath = GetTemporaryPath();
        try
        {
            using (var stream = new FileStream(
                temporaryPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.WriteThrough
            ))
            using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                writer.Write(checkpointJson);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(CheckpointPath))
            {
                ReplaceExistingCheckpoint(temporaryPath, preserveBackup);
            }
            else
            {
                File.Move(temporaryPath, CheckpointPath);
            }
        }
        finally
        {
            DeleteIfExists(temporaryPath);
        }
    }

    private void ReplaceExistingCheckpoint(string temporaryPath, bool preserveBackup)
    {
        try
        {
            File.Replace(temporaryPath, CheckpointPath, preserveBackup ? null : BackupPath, true);
        }
        catch (PlatformNotSupportedException)
        {
            ReplaceWithRollback(temporaryPath, preserveBackup);
        }
    }

    private void ReplaceWithRollback(string temporaryPath, bool preserveBackup)
    {
        string previousPath = GetPreviousPath();
        DeleteIfExists(previousPath);
        File.Copy(CheckpointPath, previousPath, true);
        if (!preserveBackup) File.Copy(CheckpointPath, BackupPath, true);
        File.Delete(CheckpointPath);
        try
        {
            File.Move(temporaryPath, CheckpointPath);
            File.Delete(previousPath);
        }
        catch
        {
            if (!File.Exists(CheckpointPath) && File.Exists(previousPath))
            {
                File.Move(previousPath, CheckpointPath);
            }
            throw;
        }
    }

    private void DeletePrimaryAndTemporaryFiles()
    {
        DeleteIfExists(CheckpointPath);
        DeleteIfExists(GetTemporaryPath());
        DeleteIfExists(GetPreviousPath());
    }

    private static bool DeleteIfExists(string path)
    {
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private string GetTemporaryPath() => CheckpointPath + ".tmp";
    private string GetPreviousPath() => CheckpointPath + ".previous";

    private static string ComputeSha256(string value)
    {
        using (SHA256 algorithm = SHA256.Create())
        {
            byte[] hash = algorithm.ComputeHash(Utf8NoBom.GetBytes(value));
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}

[Serializable]
internal sealed class ActiveRunCheckpointEnvelope
{
    public int FileVersion;
    public string SavedAtUtc;
    public string StateJson;
    public string StateSha256;
}
