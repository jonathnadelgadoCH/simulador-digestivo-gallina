using System;
using System.Collections.Generic;
using System.Linq;
using DigestiveSimulator.Data;

namespace DigestiveSimulator.Runtime
{
    public static class DigestionGraphValidator
    {
        public static void Validate(DigestionGraph graph, ISet<string> organIds)
        {
            ScientificDataValidator.ValidateVersion(graph.schemaVersion, $"graph '{graph.speciesId}'");
            var nodes = graph.nodes ?? Array.Empty<DigestiveNode>();
            var nodeIds = new HashSet<string>(nodes.Select(x => x.id), StringComparer.OrdinalIgnoreCase);
            if (!nodeIds.Contains(graph.entryNodeId)) throw new InvalidOperationException("Graph entry node does not exist.");
            if (nodeIds.Count != nodes.Length) throw new InvalidOperationException("Graph node IDs must be unique.");

            foreach (var node in nodes)
            {
                if (!organIds.Contains(node.organId)) throw new InvalidOperationException($"Graph node '{node.id}' references unknown organ '{node.organId}'.");
                foreach (var next in node.nextNodeIds ?? Array.Empty<string>())
                    if (!nodeIds.Contains(next)) throw new InvalidOperationException($"Graph node '{node.id}' points to unknown node '{next}'.");
            }
        }
    }
}

