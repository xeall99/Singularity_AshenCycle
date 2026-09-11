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