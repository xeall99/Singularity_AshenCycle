using System;
using System.Collections.Generic;

/// <summary>
/// Node types that can appear in the finite Standard Run route.
/// </summary>
public enum StandardRunNodeType
{
    NormalBattle,
    EliteBattle,
    RandomEvent,
    Rest,
    Merchant,
    Boss
}

/// <summary>
/// Serializable description of one Standard Run node. Depth is one-based so
/// it can be used directly with RunManager.Depth.
/// </summary>
[Serializable]
public sealed class StandardRunNode
{
    public int Depth;
    public StandardRunNodeType Type;
    public string Code;
    public bool IsFinal;

    public StandardRunNode()
    {
    }

    internal StandardRunNode(
        int depth,
        StandardRunNodeType type,
        string code,
        bool isFinal
    )
    {
        Depth = depth;
        Type = type;
        Code = code;
        IsFinal = isFinal;
    }

    public bool IsBattle
    {
        get
        {
            return Type == StandardRunNodeType.NormalBattle ||
                Type == StandardRunNodeType.EliteBattle ||
                Type == StandardRunNodeType.Boss;
        }
    }

    public override string ToString()
    {
        return $"{Depth}:{Type}:{Code}";
    }
}

/// <summary>
/// Deterministic route for one finite Standard Run.
///
/// The map is intentionally a plain serializable model. It can be stored in a
/// checkpoint or displayed by a future node-map UI without adding a scene
/// object or an Inspector dependency to the current battle prototype.
/// </summary>
[Serializable]
public sealed class StandardRunNodeMap
{
    public const int MinimumNodeCount = 8;
    public const int MaximumNodeCount = 10;
    public const int DefaultNodeCount = 10;

    public int RunSeed;
    public StandardRunNode[] Nodes;

    public int Count
    {
        get { return Nodes == null ? 0 : Nodes.Length; }
    }

    public StandardRunNodeMap()
    {
        Nodes = new StandardRunNode[0];
    }

    /// <summary>
    /// Creates a route from a non-zero seed. The same seed and node count
    /// always produce the same serialized node sequence.
    /// </summary>
    public static StandardRunNodeMap Generate(
        int runSeed,
        int nodeCount = DefaultNodeCount
    )
    {
        return StandardRunNodeGenerator.Generate(runSeed, nodeCount);
    }

    public StandardRunNode GetNodeAtDepth(int depth)
    {
        if (Nodes == null)
        {
            throw new InvalidOperationException(
                "StandardRunNodeMap belum memiliki node."
            );
        }

        if (depth < 1 || depth > Nodes.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(depth),
                "Depth node berada di luar map Standard Run."
            );
        }

        StandardRunNode node = Nodes[depth - 1];

        if (node == null)
        {
            throw new InvalidOperationException(
                $"Node depth {depth} pada StandardRunNodeMap kosong."
            );
        }

        return node;
    }

    public bool TryGetNodeAtDepth(
        int depth,
        out StandardRunNode node
    )
    {
        node = null;

        if (Nodes == null || depth < 1 || depth > Nodes.Length)
        {
            return false;
        }

        node = Nodes[depth - 1];
        return node != null;
    }

    /// <summary>
    /// Validates the structural rules that every Standard Run route must obey.
    /// </summary>
    public void Validate()
    {
        if (RunSeed == 0)
        {
            throw new InvalidOperationException(
                "StandardRunNodeMap wajib memiliki seed yang tidak nol."
            );
        }

        if (Nodes == null ||
            Nodes.Length < MinimumNodeCount ||
            Nodes.Length > MaximumNodeCount)
        {
            throw new InvalidOperationException(
                $"Standard Run harus memiliki {MinimumNodeCount} sampai " +
                $"{MaximumNodeCount} node."
            );
        }

        int normalBattleCount = 0;
        int eliteCount = 0;
        int eventCount = 0;
        int restCount = 0;
        int merchantCount = 0;
        int bossCount = 0;
        var nodeCodes = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < Nodes.Length; index++)
        {
            StandardRunNode node = Nodes[index];

            if (node == null)
            {
                throw new InvalidOperationException(
                    $"Standard Run node index {index} tidak boleh null."
                );
            }

            int expectedDepth = index + 1;

            if (node.Depth != expectedDepth)
            {
                throw new InvalidOperationException(
                    $"Node index {index} memiliki depth {node.Depth}, " +
                    $"seharusnya {expectedDepth}."
                );
            }

            if (string.IsNullOrWhiteSpace(node.Code))
            {
                throw new InvalidOperationException(
                    $"Node depth {node.Depth} wajib memiliki code."
                );
            }

            if (!nodeCodes.Add(node.Code))
            {
                throw new InvalidOperationException(
                    $"Node code {node.Code} tercatat lebih dari sekali."
                );
            }

            bool expectedFinal = index == Nodes.Length - 1;

            if (node.IsFinal != expectedFinal)
            {
                throw new InvalidOperationException(
                    $"Flag IsFinal node depth {node.Depth} tidak sesuai map."
                );
            }

            switch (node.Type)
            {
                case StandardRunNodeType.NormalBattle:
                    normalBattleCount++;
                    break;
                case StandardRunNodeType.EliteBattle:
                    eliteCount++;
                    break;
                case StandardRunNodeType.RandomEvent:
                    eventCount++;
                    break;
                case StandardRunNodeType.Rest:
                    restCount++;
                    break;
                case StandardRunNodeType.Merchant:
                    merchantCount++;
                    break;
                case StandardRunNodeType.Boss:
                    bossCount++;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Node depth {node.Depth} memiliki tipe tidak dikenal."
                    );
            }

            if (index > 0 &&
                node.Type == StandardRunNodeType.EliteBattle &&
                Nodes[index - 1].Type == StandardRunNodeType.EliteBattle)
            {
                throw new InvalidOperationException(
                    "Dua Elite Battle tidak boleh berurutan."
                );
            }
        }

        if (Nodes[0].Type != StandardRunNodeType.NormalBattle)
        {
            throw new InvalidOperationException(
                "Node pertama Standard Run harus Normal Battle."
            );
        }

        if (Nodes[Nodes.Length - 1].Type != StandardRunNodeType.Boss)
        {
            throw new InvalidOperationException(
                "Node terakhir Standard Run harus Boss."
            );
        }

        if (normalBattleCount < 3 || normalBattleCount > 5)
        {
            throw new InvalidOperationException(
                "Standard Run harus memiliki 3 sampai 5 Normal Battle."
            );
        }

        if (eventCount < 2 || eventCount > 3)
        {
            throw new InvalidOperationException(
                "Standard Run harus memiliki 2 sampai 3 Random Event."
            );
        }

        if (eliteCount != 1)
        {
            throw new InvalidOperationException(
                "Standard Run harus memiliki tepat satu Elite Battle."
            );
        }

        if (restCount < 1)
        {
            throw new InvalidOperationException(
                "Standard Run harus memiliki minimal satu Rest."
            );
        }

        if (merchantCount > 1 || bossCount != 1)
        {
            throw new InvalidOperationException(
                "Standard Run hanya boleh memiliki maksimal satu Merchant dan " +
                "tepat satu Boss."
            );
        }

        bool hasRestBeforeBoss = false;

        for (int index = 0; index < Nodes.Length - 1; index++)
        {
            if (Nodes[index].Type == StandardRunNodeType.Rest)
            {
                hasRestBeforeBoss = true;
                break;
            }
        }

        if (!hasRestBeforeBoss)
        {
            throw new InvalidOperationException(
                "Rest harus tersedia sebelum Boss."
            );
        }
    }
}

/// <summary>
/// Small deterministic generator for the first Standard Run contract.
/// Encounter IDs, event payloads, and map UI presentation remain separate
/// responsibilities for later milestones.
/// </summary>
public static class StandardRunNodeGenerator
{
    public static StandardRunNodeMap Generate(
        int runSeed,
        int nodeCount = StandardRunNodeMap.DefaultNodeCount
    )
    {
        if (runSeed == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSeed),
                "Run seed tidak boleh nol."
            );
        }

        if (nodeCount < StandardRunNodeMap.MinimumNodeCount ||
            nodeCount > StandardRunNodeMap.MaximumNodeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeCount),
                $"Node count harus berada di antara " +
                $"{StandardRunNodeMap.MinimumNodeCount} dan " +
                $"{StandardRunNodeMap.MaximumNodeCount}."
            );
        }

        var sequence = new DeterministicNodeSequence(runSeed);
        var map = new StandardRunNodeMap
        {
            RunSeed = runSeed,
            Nodes = new StandardRunNode[nodeCount]
        };

        int eliteDepth = nodeCount - 3;
        int restDepth = nodeCount - 2;

        map.Nodes[eliteDepth - 1] = CreateNode(
            eliteDepth,
            StandardRunNodeType.EliteBattle,
            false
        );
        map.Nodes[restDepth - 1] = CreateNode(
            restDepth,
            StandardRunNodeType.Rest,
            false
        );
        map.Nodes[nodeCount - 1] = CreateNode(
            nodeCount,
            StandardRunNodeType.Boss,
            true
        );

        var variableDepths = new List<int>();

        for (int depth = 1; depth <= nodeCount; depth++)
        {
            if (depth != eliteDepth &&
                depth != restDepth &&
                depth != nodeCount)
            {
                variableDepths.Add(depth);
            }
        }

        // The opening node is always a readable, low-risk battle.
        map.Nodes[0] = CreateNode(
            1,
            StandardRunNodeType.NormalBattle,
            false
        );

        int variableCount = variableDepths.Count;
        bool includeMerchant = nodeCount == StandardRunNodeMap.MaximumNodeCount &&
            sequence.NextInt(4) == 0;
        int normalBattleTarget;

        if (nodeCount == 8)
        {
            normalBattleTarget = 3;
        }
        else if (nodeCount == 9)
        {
            normalBattleTarget = 4;
        }
        else
        {
            normalBattleTarget = includeMerchant
                ? 4
                : 4 + sequence.NextInt(2);
        }

        int remainingNormalBattles = normalBattleTarget - 1;
        int remainingMerchant = includeMerchant ? 1 : 0;
        int remainingEvents = variableCount -
            1 -
            remainingNormalBattles -
            remainingMerchant;

        if (remainingNormalBattles < 0 || remainingEvents < 2)
        {
            throw new InvalidOperationException(
                "Generator Standard Run gagal membentuk kuota node yang valid."
            );
        }

        var remainingTypes = new List<StandardRunNodeType>();

        for (int index = 0; index < remainingNormalBattles; index++)
        {
            remainingTypes.Add(StandardRunNodeType.NormalBattle);
        }

        for (int index = 0; index < remainingEvents; index++)
        {
            remainingTypes.Add(StandardRunNodeType.RandomEvent);
        }

        if (remainingMerchant == 1)
        {
            remainingTypes.Add(StandardRunNodeType.Merchant);
        }

        var assignableDepths = new List<int>();

        for (int index = 1; index < variableDepths.Count; index++)
        {
            assignableDepths.Add(variableDepths[index]);
        }

        sequence.Shuffle(assignableDepths);
        sequence.Shuffle(remainingTypes);

        for (int index = 0; index < assignableDepths.Count; index++)
        {
            int depth = assignableDepths[index];
            map.Nodes[depth - 1] = CreateNode(
                depth,
                remainingTypes[index],
                false
            );
        }

        map.Validate();
        return map;
    }

    private static StandardRunNode CreateNode(
        int depth,
        StandardRunNodeType type,
        bool isFinal
    )
    {
        return new StandardRunNode(
            depth,
            type,
            $"{GetCodePrefix(type)}_{depth:00}",
            isFinal
        );
    }

    private static string GetCodePrefix(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.NormalBattle:
                return "BATTLE";
            case StandardRunNodeType.EliteBattle:
                return "ELITE";
            case StandardRunNodeType.RandomEvent:
                return "EVENT";
            case StandardRunNodeType.Rest:
                return "REST";
            case StandardRunNodeType.Merchant:
                return "MERCHANT";
            case StandardRunNodeType.Boss:
                return "BOSS";
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private sealed class DeterministicNodeSequence
    {
        private uint state;

        public DeterministicNodeSequence(int seed)
        {
            state = (uint)seed;

            if (state == 0)
            {
                state = 0x9E3779B9u;
            }
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMaximum)
                );
            }

            return (int)(NextUInt() % (uint)exclusiveMaximum);
        }

        public void Shuffle<T>(IList<T> values)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = NextInt(index + 1);
                T value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }
    }
}
