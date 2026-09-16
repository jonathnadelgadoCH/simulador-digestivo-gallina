using System;
using DigestiveSimulator.Data;
using DigestiveSimulator.Runtime;
using NUnit.Framework;

namespace DigestiveSimulator.Tests
{
    public sealed class RuntimeDatabaseTests
    {
        [Test]
        public void ApprovedFoodWithoutReferenceIsRejected()
        {
            var database = new RuntimeFoodDatabase();
            var food = new FoodDefinition { schemaVersion = "1.0", id = "test_food", scientificStatus = "approved", compositionReferenceIds = Array.Empty<string>() };
            Assert.Throws<InvalidOperationException>(() => database.RegisterFood(food));
        }

        [Test]
        public void PendingFoodWithoutReferenceCanBeRegistered()
        {
            var database = new RuntimeFoodDatabase();
            var food = new FoodDefinition { schemaVersion = "1.0", id = "test_food", scientificStatus = "pending", compositionReferenceIds = Array.Empty<string>() };
            database.RegisterFood(food);
            Assert.That(database.Contains("TEST_FOOD"), Is.True);
        }

        [Test]
        public void ProfileCannotBeUsedWithAnotherFood()
        {
            var species = new SpeciesDefinition { id = "chicken" };
            var food = new FoodDefinition { id = "wheat" };
            var profile = new SpeciesFoodProfile { schemaVersion = "1.0", speciesId = "chicken", foodId = "corn", scientificStatus = "pending" };
            Assert.Throws<InvalidOperationException>(() => ScientificDataValidator.ValidateProfile(profile, species, food));
        }
    }
}

