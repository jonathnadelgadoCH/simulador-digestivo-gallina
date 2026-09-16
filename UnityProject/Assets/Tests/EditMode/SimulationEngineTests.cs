using System;
using System.Linq;
using DigestiveSimulator.Data;
using DigestiveSimulator.Runtime;
using NUnit.Framework;

namespace DigestiveSimulator.Tests
{
    public sealed class SimulationEngineTests
    {
        [Test]
        public void AccessoryChainContributesWithoutBecomingBolusStage()
        {
            var graph = new DigestionGraph
            {
                nodes = new[]
                {
                    new DigestiveNode { id = "mouth_node", organId = "mouth", nextNodeIds = new[] { "duodenum_node" } },
                    new DigestiveNode { id = "liver_node", organId = "liver", accessory = true, nextNodeIds = new[] { "bile_node" } },
                    new DigestiveNode { id = "bile_node", organId = "bile", accessory = true, nextNodeIds = new[] { "duodenum_node" } },
                    new DigestiveNode { id = "duodenum_node", organId = "duodenum", nextNodeIds = Array.Empty<string>() }
                },
                entryNodeId = "mouth_node"
            };
            var engine = new SimulationEngine();
            var species = new SpeciesDefinition { id = "bird" };
            var food = new FoodDefinition { id = "grain" };
            var profile = new SpeciesFoodProfile { schemaVersion = "1.0", speciesId = "bird", foodId = "grain", scientificStatus = "pending", simulation = new SimulationProfile() };

            engine.Configure(species, food, profile, graph);

            Assert.That(engine.Steps.Select(x => x.OrganId), Is.EqualTo(new[] { "mouth", "duodenum" }));
            Assert.That(engine.Steps[1].ContributingAccessoryOrganIds, Is.EquivalentTo(new[] { "liver", "bile" }));
        }

        [Test]
        public void DigestiveProcessComesFromGraphData()
        {
            var graph = new DigestionGraph
            {
                entryNodeId = "gizzard_node",
                nodes = new[]
                {
                    new DigestiveNode
                    {
                        id = "gizzard_node",
                        organId = "gizzard",
                        process = "mechanical_digestion",
                        nextNodeIds = Array.Empty<string>()
                    }
                }
            };
            var engine = new SimulationEngine();
            var species = new SpeciesDefinition { id = "bird" };
            var food = new FoodDefinition { id = "grain" };
            var profile = new SpeciesFoodProfile { schemaVersion = "1.0", speciesId = "bird", foodId = "grain", scientificStatus = "pending", simulation = new SimulationProfile() };

            engine.Configure(species, food, profile, graph);

            Assert.That(engine.Steps.Single().ProcessType, Is.EqualTo("mechanical_digestion"));
        }
    }
}
