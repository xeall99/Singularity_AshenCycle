using System.Collections.Generic;

public sealed class OrbitResolver
{
    public ConvergenceResult Resolve(
        IReadOnlyList<PlayerActionType> pattern
    )
    {
        if (pattern == null || pattern.Count != 3)
        {
            return ConvergenceResult.None();
        }

        if (Matches(
            pattern,
            PlayerActionType.Attack,
            PlayerActionType.Attack,
            PlayerActionType.Skill
        ))
        {
            return new ConvergenceResult(
                ConvergenceType.EventHorizon,
                "EVENT HORIZON",
                "Skill ketiga mendapatkan tambahan 30% damage."
            );
        }

        if (Matches(
            pattern,
            PlayerActionType.Guard,
            PlayerActionType.Attack,
            PlayerActionType.Guard
        ))
        {
            return new ConvergenceResult(
                ConvergenceType.ReversalOrbit,
                "REVERSAL ORBIT",
                "Menyiapkan serangan balasan setelah menerima serangan."
            );
        }

        return new ConvergenceResult(
            ConvergenceType.UnstablePulse,
            "UNSTABLE PULSE",
            "Memulihkan 1 Mana."
        );
    }

    public ConvergenceResult Preview(
        IReadOnlyList<PlayerActionType> currentActions,
        PlayerActionType nextAction
    )
    {
        if (currentActions == null ||
            currentActions.Count != 2)
        {
            return ConvergenceResult.None();
        }

        PlayerActionType[] previewPattern =
        {
            currentActions[0],
            currentActions[1],
            nextAction
        };

        return Resolve(previewPattern);
    }

    /// <summary>
    /// Black Sun treats its missing first slot as a wildcard. A Named Pattern
    /// is preferred when one legal prefix completes the two recorded actions;
    /// otherwise the pair resolves as Unstable Pulse.
    /// </summary>
    public ConvergenceResult ResolveWithWildcardPrefix(
        IReadOnlyList<PlayerActionType> recordedActions
    )
    {
        if (recordedActions == null || recordedActions.Count != 2)
        {
            return ConvergenceResult.None();
        }

        PlayerActionType[] legalPrefixes =
        {
            PlayerActionType.Attack,
            PlayerActionType.Guard,
            PlayerActionType.Skill
        };

        for (int index = 0; index < legalPrefixes.Length; index++)
        {
            PlayerActionType[] candidate =
            {
                legalPrefixes[index],
                recordedActions[0],
                recordedActions[1]
            };
            ConvergenceResult result = Resolve(candidate);

            if (result.Type != ConvergenceType.UnstablePulse)
            {
                return result;
            }
        }

        return new ConvergenceResult(
            ConvergenceType.UnstablePulse,
            "UNSTABLE PULSE",
            "Black Sun resolves the two-action sequence and restores 1 Mana."
        );
    }

    private bool Matches(
        IReadOnlyList<PlayerActionType> pattern,
        PlayerActionType first,
        PlayerActionType second,
        PlayerActionType third
    )
    {
        return
            pattern[0] == first &&
            pattern[1] == second &&
            pattern[2] == third;
    }
}
