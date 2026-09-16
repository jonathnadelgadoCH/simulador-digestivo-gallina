using System;
using System.Collections.Generic;
using System.Linq;
using DigestiveSimulator.Data;

namespace DigestiveSimulator.Runtime
{
    public enum SimulationState { Idle, Ready, Playing, Paused, Completed }

    public sealed class SimulationStep
    {
        public string NodeId { get; }
        public string OrganId { get; }
        public string ProcessType { get; }
        public string[] ContributingAccessoryOrganIds { get; }
        public float DurationSeconds { get; }
        public string Message { get; }
        public string AudioFile { get; }

        public SimulationStep(string nodeId, string organId, string processType, string[] accessoryIds, float durationSeconds, string message, string audioFile)
        {
            NodeId = nodeId;
            OrganId = organId;
            ProcessType = processType;
            ContributingAccessoryOrganIds = accessoryIds;
            DurationSeconds = durationSeconds;
            Message = message;
            AudioFile = audioFile;
        }
    }

    public sealed class SimulationEngine
    {
        private readonly List<SimulationStep> steps = new List<SimulationStep>();
        private int currentIndex = -1;

        public SpeciesDefinition Species { get; private set; }
        public FoodDefinition Food { get; private set; }
        public SpeciesFoodProfile Profile { get; private set; }
        public SimulationState State { get; private set; } = SimulationState.Idle;
        public IReadOnlyList<SimulationStep> Steps => steps;
        public int CurrentStepIndex => currentIndex;
        public SimulationStep CurrentStep => currentIndex >= 0 && currentIndex < steps.Count ? steps[currentIndex] : null;

        public event Action<SimulationState> StateChanged;
        public event Action<SimulationStep, int> StepChanged;

        public void Configure(SpeciesDefinition species, FoodDefinition food, SpeciesFoodProfile profile, DigestionGraph graph)
        {
            ScientificDataValidator.ValidateProfile(profile, species, food);
            Species = species;
            Food = food;
            Profile = profile;
            steps.Clear();
            steps.AddRange(BuildPlan(graph, profile));
            currentIndex = -1;
            SetState(SimulationState.Ready);
        }

        public void Play()
        {
            if (State == SimulationState.Completed) Restart();
            if (State == SimulationState.Ready && steps.Count > 0) MoveTo(0);
            if (State == SimulationState.Ready || State == SimulationState.Paused) SetState(SimulationState.Playing);
        }

        public void Pause() { if (State == SimulationState.Playing) SetState(SimulationState.Paused); }
        public void Restart() { currentIndex = -1; SetState(SimulationState.Ready); }

        public bool Next()
        {
            if (steps.Count == 0) return false;
            var next = currentIndex + 1;
            if (next >= steps.Count) { SetState(SimulationState.Completed); return false; }
            MoveTo(next);
            return true;
        }

        private void MoveTo(int index) { currentIndex = index; StepChanged?.Invoke(steps[index], index); }
        private void SetState(SimulationState state) { State = state; StateChanged?.Invoke(state); }

        private static IEnumerable<SimulationStep> BuildPlan(DigestionGraph graph, SpeciesFoodProfile profile)
        {
            var byId = graph.nodes.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            var accessory = graph.nodes.Where(x => x.accessory).ToArray();
            var overrides = (profile.simulation?.stageOverrides ?? Array.Empty<SimulationStageOverride>())
                .GroupBy(x => x.organId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(graph.entryNodeId);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!visited.Add(id)) continue;
                var node = byId[id];
                if (!node.accessory)
                {
                    overrides.TryGetValue(node.organId, out var stageOverride);
                    var contributors = accessory
                        .Where(x => AccessoryReaches(x, id, byId, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                        .Select(x => x.organId).ToArray();
                    yield return new SimulationStep(node.id, node.organId, string.IsNullOrWhiteSpace(node.process) ? "transit" : node.process, contributors, stageOverride?.durationSeconds > 0 ? stageOverride.durationSeconds : 3f, stageOverride?.message, stageOverride?.audioFile);
                }
                foreach (var next in node.nextNodeIds ?? Array.Empty<string>())
                    if (byId.TryGetValue(next, out var nextNode) && !nextNode.accessory) queue.Enqueue(next);
            }
        }

        private static bool AccessoryReaches(DigestiveNode node, string targetNodeId, IReadOnlyDictionary<string, DigestiveNode> nodes, ISet<string> visited)
        {
            if (!visited.Add(node.id)) return false;
            foreach (var nextId in node.nextNodeIds ?? Array.Empty<string>())
            {
                if (string.Equals(nextId, targetNodeId, StringComparison.OrdinalIgnoreCase)) return true;
                if (nodes.TryGetValue(nextId, out var next) && next.accessory && AccessoryReaches(next, targetNodeId, nodes, visited)) return true;
            }
            return false;
        }
    }
}
