using System.Collections.Generic;

public sealed class OrbitState
{
    private const int MaximumActions = 3;

    private readonly List<PlayerActionType> actions =
        new List<PlayerActionType>(MaximumActions);

    public int Count => actions.Count;

    public bool IsFull =>
        actions.Count >= MaximumActions;

    public IReadOnlyList<PlayerActionType> Actions =>
        actions;

    public void AddAction(PlayerActionType action)
    {
        if (IsFull)
        {
            return;
        }

        actions.Add(action);
    }

    public bool TryGetAction(
        int index,
        out PlayerActionType action
    )
    {
        if (index >= 0 && index < actions.Count)
        {
            action = actions[index];
            return true;
        }

        action = default;
        return false;
    }

    public void Clear()
    {
        actions.Clear();
    }
}