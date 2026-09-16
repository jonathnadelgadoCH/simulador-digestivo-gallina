using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DigestiveSimulator.Data;
using DigestiveSimulator.Runtime;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var contentRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : throw new ArgumentException("Content root is required.");
            var speciesRoot = Path.Combine(contentRoot, "species", "chicken");
            var species = Read<SpeciesDefinition>(Path.Combine(speciesRoot, "species.json"));
            var graph = Read<DigestionGraph>(Path.Combine(speciesRoot, species.digestiveSystem.graphFile));
            var organs = species.digestiveSystem.organs.Select(id => Read<OrganDefinition>(Path.Combine(speciesRoot, "organs", id, "organ.json"))).ToArray();
            var speciesDatabase = new RuntimeSpeciesDatabase();
            speciesDatabase.RegisterSpecies(species, organs, graph);

            var food = Read<FoodDefinition>(Path.Combine(contentRoot, "foods", "corn", "food.json"));
            var foodDatabase = new RuntimeFoodDatabase();
            foodDatabase.RegisterFood(food);
            var profile = Read<SpeciesFoodProfile>(Path.Combine(contentRoot, "profiles", "chicken", "corn.json"));
            Require(species.absorptionRoutes?.Length == 4, "Expected four documented absorption routes for chicken.");
            var lipidRoute = species.absorptionRoutes.Single(route => route.id == "lipids_portal");
            Require(lipidRoute.targetOrganId == "liver" && lipidRoute.transport.Contains("porta", StringComparison.OrdinalIgnoreCase), "Avian lipid portal route was not loaded correctly.");

            var engine = new SimulationEngine();
            engine.Configure(species, food, profile, graph);
            Require(engine.Steps.Count == 13, $"Expected 13 bolus stages, found {engine.Steps.Count}.");
            Require(engine.Steps.All(step => !graph.nodes.Single(node => node.id == step.NodeId).accessory), "Accessory node became a bolus stage.");
            Require(engine.Steps.All(step => !string.IsNullOrWhiteSpace(step.ProcessType)), "A bolus stage has no digestive process.");
            Require(engine.Steps.Single(step => step.OrganId == "gizzard").ProcessType == "mechanical_digestion", "Gizzard process was not loaded from graph data.");
            RequireContributors(engine, "oral_cavity", "tongue", "salivary_glands");
            RequireContributors(engine, "duodenum", "pancreas", "liver", "biliary_tract");

            engine.Play();
            Require(engine.State == SimulationState.Playing && engine.CurrentStepIndex == 0, "Simulation did not enter the first stage.");
            while (engine.Next()) { }
            Require(engine.State == SimulationState.Completed, "Simulation did not complete.");

            Console.WriteLine($"Runtime check passed: {engine.Steps.Count} stages, digestive processes and accessory chains resolved, state machine completed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static T Read<T>(string path) => JsonDataReader.Read<T>(File.ReadAllText(path));

    private static void RequireContributors(SimulationEngine engine, string organId, params string[] expected)
    {
        var actual = engine.Steps.Single(step => step.OrganId == organId).ContributingAccessoryOrganIds;
        Require(new HashSet<string>(actual, StringComparer.OrdinalIgnoreCase).SetEquals(expected), $"Unexpected contributors for {organId}: {string.Join(", ", actual)}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
