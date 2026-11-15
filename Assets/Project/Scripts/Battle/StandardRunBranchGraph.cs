using System;
using System.Collections.Generic;

// Nodes remains the canonical lane for old depth-based callers. Alternatives
// and directed edges add real choices without changing the 8-10-stage contract.
public sealed partial class StandardRunNodeMap
{
    public StandardRunNode[] Alternatives = new StandardRunNode[0];

    public StandardRunNode[] AllNodes
    {
        get
        {
            var result = new List<StandardRunNode>(Nodes ?? new StandardRunNode[0]);
            result.AddRange(Alternatives ?? new StandardRunNode[0]);
            result.Sort((a, b) => a.Depth != b.Depth
                ? a.Depth.CompareTo(b.Depth) : a.Lane.CompareTo(b.Lane));
            return result.ToArray();
        }
    }

    public StandardRunNode FindNode(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        foreach (StandardRunNode node in AllNodes)
            if (string.Equals(node.Code, code, StringComparison.Ordinal)) return node;
        return null;
    }

    public StandardRunNode[] GetChoices(string previousCode)
    {
        if (string.IsNullOrEmpty(previousCode)) return new[] { Nodes[0] };
        StandardRunNode previous = FindNode(previousCode);
        if (previous == null) return new StandardRunNode[0];
        var result = new List<StandardRunNode>();
        foreach (string code in previous.NextNodeCodes)
        {
            StandardRunNode next = FindNode(code);
            if (next != null) result.Add(next);
        }
        return result.ToArray();
    }

    internal void BuildBranches()
    {
        var alternatives = new List<StandardRunNode>();
        uint random = unchecked((uint)RunSeed) ^ 0xA341316Cu;
        if (random == 0) random = 1;
        for (int lane = 0; lane < 3; lane += 2)
        {
            var types = new List<StandardRunNodeType>();
            for (int depth = 2; depth <= Count - 4; depth++)
                types.Add(GetNodeAtDepth(depth).Type);
            for (int i = types.Count - 1; i > 0; i--)
            {
                random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                int j = (int)(random % (uint)(i + 1));
                StandardRunNodeType temp = types[i]; types[i] = types[j]; types[j] = temp;
            }
            for (int depth = 2; depth < Count; depth++)
            {
                if (IsSharedDepth(depth)) continue;
                StandardRunNodeType type = depth <= Count - 4
                    ? types[depth - 2] : GetNodeAtDepth(depth).Type;
                alternatives.Add(new StandardRunNode(depth, type,
                    $"{type}_{depth:00}_L{lane}", false) { Lane = lane });
            }
        }
        Alternatives = alternatives.ToArray();
        StandardRunNode[] all = AllNodes;
        foreach (StandardRunNode from in all)
        {
            var edges = new List<string>();
            foreach (StandardRunNode to in all)
            {
                if (to.Depth != from.Depth + 1) continue;
                // A cross-lane edge is allowed only when both prefixes used the
                // same quotas; any chosen suffix then preserves GDD composition.
                if (IsSharedDepth(from.Depth) || IsSharedDepth(to.Depth) ||
                    from.Lane == to.Lane ||
                    PrefixCounts(from.Lane, from.Depth) == PrefixCounts(to.Lane, from.Depth))
                    edges.Add(to.Code);
            }
            from.NextNodeCodes = edges.ToArray();
        }
        // One optional fourth lane substitutes for an existing early encounter.
        // Copy that node's connections so no mandatory Elite/Rest/Boss is skipped.
        StandardRunNode replaced = alternatives.Find(n => n.Depth == 3 && n.Lane == 0);
        var relic = new StandardRunNode(3, StandardRunNodeType.Relic, "RELIC_03", false)
        {
            Lane = 3, ReplacesNodeCode = replaced.Code,
            NextNodeCodes = (string[])replaced.NextNodeCodes.Clone()
        };
        foreach (StandardRunNode node in all)
        {
            var links = new List<string>(node.NextNodeCodes);
            if (links.Contains(replaced.Code)) links.Add(relic.Code);
            node.NextNodeCodes = links.ToArray();
        }
        alternatives.Add(relic);
        Alternatives = alternatives.ToArray();
    }

    private bool IsSharedDepth(int depth) =>
        depth == 1 || depth == Count - 3 || depth == Count - 2 || depth == Count;

    private int PrefixCounts(int lane, int depth)
    {
        int counts = 0;
        for (int d = 1; d <= depth; d++)
        {
            StandardRunNode node = GetNodeAtDepth(d);
            foreach (StandardRunNode alternative in Alternatives)
                if (alternative.Depth == d && alternative.Lane == lane) node = alternative;
            counts += 1 << ((int)node.Type * 4);
        }
        return counts;
    }

    private void ValidateBranches()
    {
        if (Alternatives == null) throw new InvalidOperationException("Missing branch nodes.");
        foreach (StandardRunNode alternative in Alternatives)
            if (alternative == null) throw new InvalidOperationException("Null branch node.");
        var byCode = new Dictionary<string, StandardRunNode>(StringComparer.Ordinal);
        foreach (StandardRunNode node in AllNodes)
        {
            if (node == null || node.Depth < 1 || node.Depth > Count ||
                node.Lane < 0 || node.Lane > 3 || string.IsNullOrWhiteSpace(node.Code) ||
                !Enum.IsDefined(typeof(StandardRunNodeType), node.Type) ||
                node.NextNodeCodes == null || byCode.ContainsKey(node.Code))
                throw new InvalidOperationException("Invalid branch node identity.");
            if ((node.Type == StandardRunNodeType.Boss) != (node.Depth == Count) ||
                node.IsFinal != (node.Depth == Count))
                throw new InvalidOperationException("Boss must be the final shared node.");
            byCode.Add(node.Code, node);
        }
        for (int depth = 1; depth <= Count; depth++)
        {
            var lanes = new HashSet<int>();
            foreach (StandardRunNode node in byCode.Values)
                if (node.Depth == depth && !lanes.Add(node.Lane))
                    throw new InvalidOperationException("Duplicate lane at depth.");
            if (lanes.Count != (IsSharedDepth(depth) ? 1 : depth == 3 && RouteVersion == 1 ? 4 : 3))
                throw new InvalidOperationException("Invalid branch row width.");
        }
        var paths = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        paths[Nodes[0].Code] = new HashSet<int> { 0 };
        foreach (StandardRunNode node in AllNodes)
        {
            if (!paths.TryGetValue(node.Code, out HashSet<int> incoming))
                throw new InvalidOperationException("Unreachable branch node.");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if ((node.NextNodeCodes.Length == 0) != node.IsFinal)
                throw new InvalidOperationException("Branch dead end or Boss outgoing edge.");
            foreach (string code in node.NextNodeCodes)
            {
                if (code == null || !unique.Add(code) || !byCode.TryGetValue(code, out StandardRunNode to) ||
                    to.Depth != node.Depth + 1 ||
                    (node.Type == StandardRunNodeType.EliteBattle && to.Type == node.Type))
                    throw new InvalidOperationException("Invalid forward branch connection.");
                if (!paths.TryGetValue(code, out HashSet<int> next))
                    paths[code] = next = new HashSet<int>();
                StandardRunNodeType quotaType = node.Type;
                if (node.Type == StandardRunNodeType.Relic)
                {
                    if (RouteVersion != 1 || node.Code != "RELIC_03" || node.Depth != 3 || node.Lane != 3 ||
                        !byCode.TryGetValue(node.ReplacesNodeCode ?? "", out StandardRunNode replaced) ||
                        replaced.Depth != 3 || replaced.Lane != 0 || replaced.IsFinal ||
                        node.NextNodeCodes.Length != replaced.NextNodeCodes.Length)
                        throw new InvalidOperationException("Invalid Relic branch substitution.");
                    for (int i = 0; i < node.NextNodeCodes.Length; i++)
                        if (node.NextNodeCodes[i] != replaced.NextNodeCodes[i])
                            throw new InvalidOperationException("Relic branch changed its onward route.");
                    quotaType = replaced.Type;
                }
                foreach (int prefix in incoming) next.Add(prefix + (1 << ((int)quotaType * 4)));
            }
            if (!node.IsFinal) continue;
            foreach (int counts in incoming)
            {
                int battles = counts & 15, elites = (counts >> 4) & 15;
                int events = (counts >> 8) & 15, rests = (counts >> 12) & 15;
                int merchants = (counts >> 16) & 15;
                if (battles < 3 || battles > 5 || elites != 1 || events < 2 || events > 3 ||
                    rests < 1 || merchants > 1)
                    throw new InvalidOperationException("A branch violates Standard Run quotas.");
            }
        }
    }
}
