using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>Permanent currency earned only from a finished Standard Run.</summary>
public sealed class EchoFragmentAward
{
    public const int EliteBattleReward = 20;
    public const int BossBattleReward = 50;
    public const int ClearBonusReward = 20; // Provisional until owner balance review.
    public int NormalBattleFragments { get; }
    public int EliteFragments { get; }
    public int BossFragments { get; }
    public int ClearBonus { get; }
    public int Found => NormalBattleFragments + EliteFragments + BossFragments;
    public bool IsClear { get; }
    public int Total => IsClear
        ? Found + ClearBonus
        : Found * 60 / 100; // Defeat retains 60%, rounded down.

    private EchoFragmentAward(int normal, int elite, int boss,
        int clearBonus, bool isClear)
    {
        NormalBattleFragments = normal;
        EliteFragments = elite;
        BossFragments = boss;
        ClearBonus = clearBonus;
        IsClear = isClear;
    }

    public static EchoFragmentAward FromFinishedRun(RunManager run)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        if (run.IsActive || run.NodeMap == null)
            throw new InvalidOperationException("Echo Fragment hanya diberikan setelah run berakhir.");

        int normal = 0;
        int elite = 0;
        int boss = 0;
        foreach (string code in run.VisitedNodeCodes)
        {
            StandardRunNode node = run.NodeMap.FindNode(code);
            if (node == null) throw new InvalidOperationException("Riwayat node tidak valid.");
            switch (node.Type)
            {
                case StandardRunNodeType.NormalBattle:
                    normal += RollNormalBattle(run.RunSeed, code);
                    break;
                case StandardRunNodeType.EliteBattle:
                    elite += EliteBattleReward;
                    break;
                case StandardRunNodeType.Boss:
                    if (run.IsCompleted) boss += BossBattleReward;
                    break;
            }
        }
        return new EchoFragmentAward(normal, elite, boss,
            run.IsCompleted ? ClearBonusReward : 0, run.IsCompleted);
    }

    private static int RollNormalBattle(int runSeed, string nodeCode)
    {
        // Stable across save/continue; unrelated to runtime random state.
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)runSeed) * 16777619u;
            foreach (char letter in nodeCode)
                hash = (hash ^ letter) * 16777619u;
            return 8 + (int)(hash % 5u);
        }
    }
}

public sealed class EchoFragmentReceipt
{
    public int Earned { get; }
    public int Balance { get; }
    public bool WasAlreadyClaimed { get; }

    public EchoFragmentReceipt(int earned, int balance, bool wasAlreadyClaimed)
    {
        Earned = earned;
        Balance = balance;
        WasAlreadyClaimed = wasAlreadyClaimed;
    }
}

public sealed class EchoUnlockReceipt
{
    public bool Purchased { get; }
    public bool AlreadyOwned { get; }
    public int Balance { get; }

    public EchoUnlockReceipt(bool purchased, bool alreadyOwned, int balance)
    {
        Purchased = purchased;
        AlreadyOwned = alreadyOwned;
        Balance = balance;
    }
}

/// <summary>
/// Separate from the Active Run checkpoint. A run identity can be claimed once,
/// even when the same terminal callback or recovered checkpoint is replayed.
/// </summary>
public sealed class EchoFragmentWallet
{
    public const string DefaultFileName = "syngrava-echo-fragments-v1.json";
    private readonly string directoryPath;
    public string WalletPath { get; }
    public string BackupPath => WalletPath + ".bak";

    [Serializable]
    private sealed class Claim
    {
        public string RunId;
        public int Amount;
    }

    [Serializable]
    private sealed class Purchase
    {
        public string UnlockId;
        public int Cost;
    }

    [Serializable]
    private sealed class WalletData
    {
        public int Version = 2;
        public int Balance;
        public Claim[] Claims = new Claim[0];
        public Purchase[] Purchases = new Purchase[0];
    }

    [Serializable]
    private sealed class Envelope
    {
        public string Payload;
        public string Sha256;
    }

    public EchoFragmentWallet(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Wallet directory wajib diisi.", nameof(directoryPath));
        this.directoryPath = Path.GetFullPath(directoryPath);
        WalletPath = Path.Combine(this.directoryPath, DefaultFileName);
    }

    public static EchoFragmentWallet CreateDefault()
    {
        return new EchoFragmentWallet(Path.Combine(Application.persistentDataPath, "Saves"));
    }

    public int ReadBalance()
    {
        return ReadWallet(out _).Balance;
    }

    public bool HasUnlock(string unlockId)
    {
        HorizontalUnlockCatalog.GetCost(unlockId);
        foreach (Purchase purchase in ReadWallet(out _).Purchases)
            if (purchase.UnlockId == unlockId) return true;
        return false;
    }

    public EchoUnlockReceipt TryPurchase(string unlockId)
    {
        int cost = HorizontalUnlockCatalog.GetCost(unlockId);
        WalletData wallet = ReadWallet(out bool primaryValid);
        foreach (Purchase purchase in wallet.Purchases)
            if (purchase.UnlockId == unlockId)
                return new EchoUnlockReceipt(false, true, wallet.Balance);
        if (wallet.Balance < cost)
            return new EchoUnlockReceipt(false, false, wallet.Balance);

        var purchases = new List<Purchase>(wallet.Purchases)
        {
            new Purchase { UnlockId = unlockId, Cost = cost }
        };
        wallet.Balance -= cost;
        wallet.Purchases = purchases.ToArray();
        SaveWallet(wallet, primaryValid);
        return new EchoUnlockReceipt(true, false, wallet.Balance);
    }

    public EchoFragmentReceipt ClaimFinishedRun(RunManager run)
    {
        if (run == null) throw new ArgumentNullException(nameof(run));
        EchoFragmentAward award = EchoFragmentAward.FromFinishedRun(run);
        return ClaimAward(run.RunId, award.Total);
    }

    private EchoFragmentReceipt ClaimAward(string runId, int amount)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run identity wajib diisi.", nameof(runId));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

        WalletData wallet = ReadWallet(out bool primaryValid);
        foreach (Claim prior in wallet.Claims)
        {
            if (!string.Equals(prior.RunId, runId, StringComparison.Ordinal)) continue;
            // A recovered boundary may lead through a different branch. The
            // first committed award remains authoritative for that run.
            return new EchoFragmentReceipt(prior.Amount, wallet.Balance, true);
        }

        if (wallet.Balance > int.MaxValue - amount)
            throw new InvalidOperationException("Saldo Echo Fragment melebihi kapasitas.");

        var claims = new List<Claim>(wallet.Claims)
        {
            new Claim { RunId = runId, Amount = amount }
        };
        wallet.Balance += amount;
        wallet.Claims = claims.ToArray();
        SaveWallet(wallet, primaryValid);
        return new EchoFragmentReceipt(amount, wallet.Balance, false);
    }

    private WalletData ReadWallet(out bool primaryValid)
    {
        primaryValid = false;
        if (File.Exists(WalletPath))
        {
            try
            {
                WalletData primary = Load(WalletPath);
                primaryValid = true;
                return primary;
            }
            catch (Exception error) when (error is IOException ||
                                          error is InvalidOperationException ||
                                          error is ArgumentException)
            {
                if (!File.Exists(BackupPath))
                    throw new InvalidOperationException("Echo wallet rusak dan tidak ada backup.", error);
            }
        }
        if (File.Exists(BackupPath)) return Load(BackupPath);
        return new WalletData();
    }

    private static WalletData Load(string path)
    {
        Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
        if (envelope == null || string.IsNullOrEmpty(envelope.Payload) ||
            !string.Equals(Hash(envelope.Payload), envelope.Sha256,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Integritas Echo wallet gagal.");
        WalletData wallet = JsonUtility.FromJson<WalletData>(envelope.Payload);
        if (wallet == null || (wallet.Version != 1 && wallet.Version != 2) ||
            wallet.Balance < 0 ||
            wallet.Claims == null)
            throw new InvalidOperationException("Format Echo wallet tidak valid.");
        if (wallet.Version == 1)
            wallet.Purchases = new Purchase[0];
        if (wallet.Purchases == null)
            throw new InvalidOperationException("Ledger unlock Echo tidak valid.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        long total = 0;
        foreach (Claim claim in wallet.Claims)
        {
            if (claim == null || string.IsNullOrWhiteSpace(claim.RunId) ||
                claim.Amount < 0 || !seen.Add(claim.RunId))
                throw new InvalidOperationException("Ledger Echo wallet tidak valid.");
            total += claim.Amount;
        }
        seen.Clear();
        foreach (Purchase purchase in wallet.Purchases)
        {
            if (purchase == null || string.IsNullOrWhiteSpace(purchase.UnlockId) ||
                !seen.Add(purchase.UnlockId))
                throw new InvalidOperationException("Ledger unlock Echo tidak valid.");
            int expectedCost;
            try { expectedCost = HorizontalUnlockCatalog.GetCost(purchase.UnlockId); }
            catch (ArgumentException error)
            {
                throw new InvalidOperationException("Unknown unlock in Echo wallet.", error);
            }
            if (purchase.Cost != expectedCost)
                throw new InvalidOperationException("Unlock cost does not match catalog.");
            total -= purchase.Cost;
        }
        if (total != wallet.Balance)
            throw new InvalidOperationException("Saldo Echo wallet tidak sesuai ledger.");
        wallet.Version = 2;
        return wallet;
    }

    private void SaveWallet(WalletData wallet, bool primaryValid)
    {
        Directory.CreateDirectory(directoryPath);
        string payload = JsonUtility.ToJson(wallet);
        string content = JsonUtility.ToJson(new Envelope
        {
            Payload = payload,
            Sha256 = Hash(payload)
        }, true);
        string temporary = WalletPath + ".tmp";
        byte[] bytes = new UTF8Encoding(false).GetBytes(content);
        using (var stream = new FileStream(temporary, FileMode.Create,
            FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        if (primaryValid)
        {
            File.Replace(temporary, WalletPath, BackupPath);
        }
        else
        {
            // The backup remains untouched if primary had failed validation.
            if (File.Exists(WalletPath)) File.Delete(WalletPath);
            File.Move(temporary, WalletPath);
        }
    }

    private static string Hash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
