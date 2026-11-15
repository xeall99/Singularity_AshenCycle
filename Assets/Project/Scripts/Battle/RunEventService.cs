using System;

/// <summary>Seeded encounters with explicit costs and outcomes. No Unity UI or file I/O.</summary>
public static class RunEventService
{
    public sealed class Choice
    {
        public string Label { get; }
        public string Description { get; }
        public int HealthCost { get; }
        public int ManaCost { get; }
        public int HealthPercent { get; }
        public int ManaGain { get; }
        public int EmbersGain { get; }

        internal Choice(string label, string description, int healthCost = 0,
            int manaCost = 0, int healthPercent = 0, int manaGain = 0, int embersGain = 0)
        {
            Label = label;
            Description = description;
            HealthCost = healthCost;
            ManaCost = manaCost;
            HealthPercent = healthPercent;
            ManaGain = manaGain;
            EmbersGain = embersGain;
        }

        public bool CanAfford(int health, int mana) => health > HealthCost && mana >= ManaCost;
    }

    public sealed class Encounter
    {
        public string Title { get; }
        public string Story { get; }
        public Choice First { get; }
        public Choice Second { get; }

        internal Encounter(string title, string story, Choice first, Choice second)
        {
            Title = title;
            Story = story;
            First = first;
            Second = second;
        }

        public Choice GetChoice(int index) => index == 0 ? First : index == 1 ? Second : null;
    }

    // Provisional content values are kept together for balance review.
    private static readonly Encounter[] encounters =
    {
        new Encounter("THE EMBER ALTAR", "A warm offering stirs beneath the ashes.",
            new Choice("OFFER BLOOD", "Pay 10 HP. Gain 15 Embers. Must leave at least 1 HP.", healthCost: 10, embersGain: 15),
            new Choice("GATHER ASH", "Gain 3 Embers without paying HP.", embersGain: 3)),
        new Encounter("THE SILENT WELL", "The well can mend flesh or replenish your focus.",
            new Choice("MEND WOUNDS", "Pay 3 Mana. Restore 25% of maximum HP.", manaCost: 3, healthPercent: 25),
            new Choice("RECOVER FOCUS", "Restore 2 Mana.", manaGain: 2)),
        new Encounter("THE ASHEN CACHE", "Two remnants remain. Only one can be carried onward.",
            new Choice("TAKE EMBERS", "Gain 10 Embers.", embersGain: 10),
            new Choice("TAKE THE BALM", "Restore 15% of maximum HP.", healthPercent: 15))
    };

    public static Encounter GetEncounter(int runSeed, string nodeCode)
    {
        if (string.IsNullOrWhiteSpace(nodeCode)) throw new ArgumentException("A node code is required.", nameof(nodeCode));
        // Stable FNV-1a, unlike string.GetHashCode across runtime versions.
        uint hash = unchecked((uint)runSeed ^ 2166136261u);
        foreach (char character in nodeCode) hash = unchecked((hash ^ character) * 16777619u);
        return encounters[hash % (uint)encounters.Length];
    }

    public static bool CanChoose(RunManager run, string nodeCode, int choiceIndex)
    {
        if (run == null || !run.CanEnterNode(nodeCode)) return false;
        StandardRunNode node = run.NodeMap.FindNode(nodeCode);
        if (node == null || node.Type != StandardRunNodeType.RandomEvent) return false;
        Choice choice = GetEncounter(run.RunSeed, nodeCode).GetChoice(choiceIndex);
        return choice != null && choice.CanAfford(run.CurrentHP, run.CurrentMana);
    }

    public static bool TryResolve(RunManager run, string nodeCode, int choiceIndex,
        int effectiveMaxHP, int effectiveMaxMana)
    {
        if (effectiveMaxHP < 1 || effectiveMaxMana < 0 || !CanChoose(run, nodeCode, choiceIndex)) return false;
        Choice choice = GetEncounter(run.RunSeed, nodeCode).GetChoice(choiceIndex);
        // Calculate before committing. A choice can never kill the player or exceed caps.
        long heal = ((long)effectiveMaxHP * choice.HealthPercent + 99) / 100;
        int health = (int)Math.Min(effectiveMaxHP, run.CurrentHP - choice.HealthCost + heal);
        int mana = (int)Math.Min(effectiveMaxMana, (long)run.CurrentMana - choice.ManaCost + choice.ManaGain);
        if (!run.TryEnterNode(nodeCode)) return false;
        if (!run.TryResolveCurrentNode(StandardRunNodeType.RandomEvent, nodeCode))
            throw new InvalidOperationException("An entered event could not be resolved.");
        run.SetResources(health, mana, effectiveMaxHP, effectiveMaxMana);
        run.AddEmbers(choice.EmbersGain);
        return true;
    }
}
