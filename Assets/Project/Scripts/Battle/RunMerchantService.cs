using System;

/// <summary>One supply per merchant stop, or leave freely. Prices are provisional.</summary>
public static class RunMerchantService
{
    public const int HealthPrice = 12;
    public const int ManaPrice = 8;
    public const int LeaveChoice = 2;

    public static bool CanChoose(RunManager run, string code, int choice, int maxHP, int maxMana)
    {
        if (run == null || maxHP < 1 || maxMana < 0 || !run.CanEnterNode(code)) return false;
        if (run.NodeMap.FindNode(code)?.Type != StandardRunNodeType.Merchant) return false;
        switch (choice)
        {
            case 0: return run.TotalEmbers >= HealthPrice && run.CurrentHP < maxHP;
            case 1: return run.TotalEmbers >= ManaPrice && run.CurrentMana < maxMana;
            case LeaveChoice: return true;
            default: return false;
        }
    }

    public static bool TryResolve(RunManager run, string code, int choice, int maxHP, int maxMana)
    {
        if (!CanChoose(run, code, choice, maxHP, maxMana)) return false;
        int price = choice == 0 ? HealthPrice : choice == 1 ? ManaPrice : 0;
        if (!run.TryEnterNode(code)) return false;
        if (!run.TryResolveCurrentNode(StandardRunNodeType.Merchant, code))
            throw new InvalidOperationException("An entered merchant could not be resolved.");
        if (!run.TrySpendEmbers(price))
            throw new InvalidOperationException("A validated merchant purchase could not be paid.");
        // Long arithmetic avoids overflow; Restore methods clamp to the effective cap.
        if (choice == 0) run.RestoreHP((int)(((long)maxHP + 1) / 2), maxHP);
        if (choice == 1) run.RestoreMana((int)(((long)maxMana + 1) / 2), maxMana);
        return true;
    }
}
