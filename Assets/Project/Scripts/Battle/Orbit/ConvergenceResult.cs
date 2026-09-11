public enum ConvergenceType
{
    None,
    UnstablePulse,
    EventHorizon,
    ReversalOrbit
}

public sealed class ConvergenceResult
{
    public ConvergenceType Type { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public bool HasConvergence =>
        Type != ConvergenceType.None;

    public ConvergenceResult(
        ConvergenceType type,
        string displayName,
        string description
    )
    {
        Type = type;
        DisplayName = displayName;
        Description = description;
    }

    public static ConvergenceResult None()
    {
        return new ConvergenceResult(
            ConvergenceType.None,
            string.Empty,
            string.Empty
        );
    }
}