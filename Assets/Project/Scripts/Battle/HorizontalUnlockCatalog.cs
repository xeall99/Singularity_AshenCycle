using System;

/// <summary>Permanent choices. Unlocks expand the pool, never Base Stats.</summary>
public static class HorizontalUnlockCatalog
{
    public const string EchoPrismId = "RELIC_ECHO_PRISM";
    public const int EchoPrismCost = 120;

    public static int GetCost(string unlockId)
    {
        if (string.Equals(unlockId, EchoPrismId, StringComparison.Ordinal))
            return EchoPrismCost;
        throw new ArgumentException("Unknown horizontal unlock.", nameof(unlockId));
    }
}
