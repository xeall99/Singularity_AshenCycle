using UnityEngine;

/// <summary>
/// Authored visual set used by the runtime-built Standard Run Map.
/// Keeping the sprites in one Resources catalog avoids adding scene or
/// Inspector dependencies to the Battle prototype.
/// </summary>
public sealed class StandardRunMapVisualAssets : ScriptableObject
{
    [SerializeField] private Sprite backdrop;
    [SerializeField] private Sprite outerFrame;
    [SerializeField] private Sprite detailFrame;
    [SerializeField] private Sprite nodePlaque;
    [SerializeField] private Sprite battleIcon;
    [SerializeField] private Sprite eliteIcon;
    [SerializeField] private Sprite eventIcon;
    [SerializeField] private Sprite restIcon;
    [SerializeField] private Sprite merchantIcon;
    [SerializeField] private Sprite bossIcon;
    [SerializeField] private Sprite currentMarker;
    [SerializeField] private Sprite stateRing;
    [SerializeField] private Sprite connectorStroke;
    [SerializeField] private Sprite ornaments;

    public Sprite Backdrop => backdrop;
    public Sprite OuterFrame => outerFrame;
    public Sprite DetailFrame => detailFrame;
    public Sprite NodePlaque => nodePlaque;
    public Sprite CurrentMarker => currentMarker;
    public Sprite StateRing => stateRing;
    public Sprite ConnectorStroke => connectorStroke;
    public Sprite Ornaments => ornaments;

    public bool HasCompleteCoreSet =>
        backdrop != null &&
        outerFrame != null &&
        detailFrame != null &&
        nodePlaque != null &&
        battleIcon != null &&
        eliteIcon != null &&
        eventIcon != null &&
        restIcon != null &&
        merchantIcon != null &&
        bossIcon != null &&
        currentMarker != null &&
        stateRing != null &&
        connectorStroke != null;

    public Sprite GetNodeIcon(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.NormalBattle:
                return battleIcon;
            case StandardRunNodeType.EliteBattle:
                return eliteIcon;
            case StandardRunNodeType.RandomEvent:
                return eventIcon;
            case StandardRunNodeType.Rest:
                return restIcon;
            case StandardRunNodeType.Merchant:
                return merchantIcon;
            case StandardRunNodeType.Boss:
                return bossIcon;
            case StandardRunNodeType.Relic:
                return currentMarker;
            default:
                return null;
        }
    }
}
