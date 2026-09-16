using System;

namespace DigestiveSimulator.Data
{
    [Serializable]
    public sealed class DigestionGraph
    {
        public string schemaVersion;
        public string contentVersion;
        public string speciesId;
        public string entryNodeId;
        public DigestiveNode[] nodes;
    }

    [Serializable]
    public sealed class DigestiveNode
    {
        public string id;
        public string organId;
        public string type;
        public string process;
        public string[] nextNodeIds;
        public bool optional;
        public bool accessory;
    }
}
