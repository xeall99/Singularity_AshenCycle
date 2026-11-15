using System;

/// <summary>
/// Deterministic combat rules shared by initiative and damage execution.
/// The encounter currently contains one enemy; its ruleset Speed is 9 until
/// enemy Speed is stored in the content database.
/// </summary>
public static class BattleTurnRules
{
    public const int EnemyBaseSpeed = 9;

    public static bool PlayerActsFirst(int playerSpeed, int enemySpeed = EnemyBaseSpeed)
    {
        if (playerSpeed < 1 || enemySpeed < 1)
            throw new ArgumentOutOfRangeException();
        return playerSpeed >= enemySpeed;
    }

    public static bool IsCritical(int runSeed, int depth, int strikeIndex, float chance)
    {
        if (depth < 1 || strikeIndex < 0 || chance < 0f || chance > 1f)
            throw new ArgumentOutOfRangeException();
        if (chance <= 0f) return false;
        if (chance >= 1f) return true;

        // This roll does not consume route/reward RNG state. Replaying the
        // same seed and actions produces the same result, including on Retry.
        uint value = unchecked((uint)runSeed);
        value ^= unchecked((uint)depth * 0x9E3779B9u);
        value ^= unchecked((uint)(strikeIndex + 1) * 0x85EBCA6Bu);
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return value % 10000u < (uint)Math.Round(chance * 10000f);
    }

    public static int ApplyCritical(int finalDamage)
    {
        if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
        return (int)Math.Min(int.MaxValue, ((long)finalDamage * 3 + 1) / 2);
    }
}
