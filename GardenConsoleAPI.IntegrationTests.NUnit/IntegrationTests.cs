using GardenConsoleAPI.Business;
using GardenConsoleAPI.Business.Contracts;
using GardenConsoleAPI.Data.Models;
using GardenConsoleAPI.DataAccess;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace GardenConsoleAPI.IntegrationTests.NUnit
{
    public class IntegrationTests
    {
        private TestPlantsDbContext dbContext;
        private IPlantsManager plantsManager;

        [SetUp]
        public void SetUp()
        {
            this.dbContext = new TestPlantsDbContext();
            this.plantsManager = new PlantsManager(new PlantsRepository(this.dbContext));
        }


        [TearDown]
        public void TearDown()
        {
            this.dbContext.Database.EnsureDeleted();
            this.dbContext.Dispose();
        }

        [Test]
        public async Task AddPlantAsync_ShouldAddNewPlant()
        {
            // Arrange
            var newPlant = new Plant()
            {
                CatalogNumber = "1234ABCD9876",
                Name = "Cucumber",
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true                
            };

            // Act
            await plantsManager.AddAsync(newPlant);
            var plantInDb = await dbContext.Plants.FirstOrDefaultAsync( x => x.CatalogNumber == newPlant.CatalogNumber);

            // Assert
            Assert.NotNull(plantInDb);
            Assert.That(plantInDb.Id, Is.EqualTo(newPlant.Id));
            Assert.That(plantInDb.CatalogNumber, Is.EqualTo(newPlant.CatalogNumber));
            Assert.That(plantInDb.Name, Is.EqualTo(newPlant.Name));
            Assert.That(plantInDb.PlantType, Is.EqualTo(newPlant.PlantType));
            Assert.That(plantInDb.FoodType, Is.EqualTo(newPlant.FoodType));
            Assert.That(plantInDb.Quantity, Is.EqualTo(newPlant.Quantity));
            Assert.That(plantInDb.IsEdible, Is.EqualTo(newPlant.IsEdible));
        }

        [Test]
        public async Task AddPlantAsync_TryToAddPlantWithInvalidCredentials_ShouldThrowException()
        {
            // Arrange
            var newPlant = new Plant()
            {
                CatalogNumber = "ABCD9876",
                Name = "Cucumber",
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true
            };

            string expectedMessage = "Invalid plant!";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>( () => plantsManager.AddAsync(newPlant));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));

        }

        [Test]
        public async Task DeletePlantAsync_WithValidCatalogNumber_ShouldRemovePlantFromDb()
        {
            // Arrange
            var newPlant = new Plant()
            {
                CatalogNumber = "1234ABCD9876",
                Name = "Cucumber",
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true
            };

            await plantsManager.AddAsync(newPlant);

            // Act
            await plantsManager.DeleteAsync(newPlant.CatalogNumber);

            var plantInDb = await dbContext.Plants.FirstOrDefaultAsync( x => x.CatalogNumber == newPlant.CatalogNumber);           

            // Assert
            Assert.Null(plantInDb);            
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task DeletePlantAsync_TryToDeleteWithNullOrWhiteSpaceCatalogNumber_ShouldThrowException(string catalogNumber)
        {
            // Arrange
            string expectedMessage = "Catalog number cannot be empty.";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>( () => plantsManager.DeleteAsync(catalogNumber));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task GetAllAsync_WhenPlantsExist_ShouldReturnAllPlants()
        {
            // Arrange
            var firstPlant = new Plant()
            {
                CatalogNumber = "1234ABCD9876",
                Name = "Cucumber",
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true
            };

            var secondPlant = new Plant()
            {
                CatalogNumber = "9876ZXCV6789",
                Name = "Tomato",
                PlantType = "Some red plant",
                FoodType = "Vegetable",
                Quantity = 200,
                IsEdible = true
            };

            await plantsManager.AddAsync(firstPlant);
            await plantsManager.AddAsync(secondPlant);

            // Act
            var result = await plantsManager.GetAllAsync();

            var plantsInDb = await dbContext.Plants.ToListAsync();
            var firstPlantInResult = result.FirstOrDefault( x => x.CatalogNumber == firstPlant.CatalogNumber);
            var secondPlantInResult = result.FirstOrDefault(x => x.CatalogNumber == secondPlant.CatalogNumber);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(plantsInDb.Count(), Is.EqualTo(result.Count()));
            Assert.That(firstPlantInResult.CatalogNumber, Is.Not.EqualTo(secondPlantInResult.CatalogNumber));
            Assert.That(firstPlantInResult.Id, Is.Not.EqualTo(secondPlantInResult.Id));

            Assert.That(firstPlantInResult.Id, Is.EqualTo(firstPlant.Id));
            Assert.That(firstPlantInResult.Name, Is.EqualTo(firstPlant.Name));
            Assert.That(firstPlantInResult.PlantType, Is.EqualTo(firstPlant.PlantType));
            Assert.That(firstPlantInResult.FoodType, Is.EqualTo(firstPlant.FoodType));
            Assert.That(firstPlantInResult.Quantity, Is.EqualTo(firstPlant.Quantity));
            Assert.That(firstPlantInResult.IsEdible, Is.EqualTo(firstPlant.IsEdible));
            Assert.That(firstPlantInResult.CatalogNumber, Is.EqualTo(firstPlant.CatalogNumber));

            Assert.That(secondPlantInResult.Id, Is.EqualTo(secondPlant.Id));
            Assert.That(secondPlantInResult.Name, Is.EqualTo(secondPlant.Name));
            Assert.That(secondPlantInResult.PlantType, Is.EqualTo(secondPlant.PlantType));
            Assert.That(secondPlantInResult.FoodType, Is.EqualTo(secondPlant.FoodType));
            Assert.That(secondPlantInResult.Quantity, Is.EqualTo(secondPlant.Quantity));
            Assert.That(secondPlantInResult.IsEdible, Is.EqualTo(secondPlant.IsEdible));
            Assert.That(secondPlantInResult.CatalogNumber, Is.EqualTo(secondPlant.CatalogNumber));
        }

        [Test]
        public async Task GetAllAsync_WhenNoPlantsExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string expectedMessage = "No plant found.";

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>( () => plantsManager.GetAllAsync());
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task SearchByFoodTypeAsync_WithExistingFoodType_ShouldReturnMatchingPlants()
        {
            // Arrange
            var firstPlant = new Plant()
            {
                CatalogNumber = "1234ABCD9876",
                Name = "Cucumber",
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true
            };

            var secondPlant = new Plant()
            {
                CatalogNumber = "9876ZXCV6789",
                Name = "Tomato",
                PlantType = "Some red plant",
                FoodType = "Vegetable",
                Quantity = 200,
                IsEdible = false
            };

            await plantsManager.AddAsync(firstPlant);
            await plantsManager.AddAsync(secondPlant);

            // Act
            var result = await plantsManager.SearchByFoodTypeAsync(firstPlant.FoodType);

            var firstPlantInResult = result.FirstOrDefault( x => x.CatalogNumber == firstPlant.CatalogNumber);
            var secondPlantInResult = result.FirstOrDefault( x => x.CatalogNumber == secondPlant.CatalogNumber);
   
            // Assert
            Assert.NotNull(result);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(firstPlantInResult.FoodType, Is.EqualTo(secondPlantInResult.FoodType));

            Assert.That(firstPlantInResult.CatalogNumber, Is.Not.EqualTo(secondPlantInResult.CatalogNumber));
            Assert.That(firstPlantInResult.Name, Is.Not.EqualTo(secondPlantInResult.Name));
            Assert.That(firstPlantInResult.PlantType, Is.Not.EqualTo(secondPlantInResult.PlantType));
            Assert.That(firstPlantInResult.Quantity, Is.Not.EqualTo(secondPlantInResult.Quantity));
            Assert.That(firstPlantInResult.IsEdible, Is.Not.EqualTo(secondPlantInResult.IsEdible));
            Assert.That(firstPlantInResult.Id, Is.Not.EqualTo(secondPlantInResult.Id));            
        }

        [Test]
        public async Task SearchByFoodTypeAsync_WithNonExistingFoodType_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string expectedMessage = "No plant found with the given food type.";

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(() => plantsManager.SearchByFoodTypeAsync("NonExistingType"));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }
        
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task SearchByFoodTypeAsync_WithNullOrWhiteSpaceFoodType_ShouldThrowArgumentException(string foodType)
        {
            // Arrange
            string expectedMessage = "Food type cannot be empty.";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => plantsManager.SearchByFoodTypeAsync(foodType));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task GetSpecificAsync_WithValidCatalogNumber_ShouldReturnPlant()
        {
            // Arrange
            var newPlant = new Plant()
            {
                CatalogNumber = "1234ABCD9876",
                Name = "Cucumber",
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true
            };

            await plantsManager.AddAsync(newPlant);

            // Act
            var result = await plantsManager.GetSpecificAsync(newPlant.CatalogNumber);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Id, Is.EqualTo(newPlant.Id));
            Assert.That(result.CatalogNumber, Is.EqualTo(newPlant.CatalogNumber));
            Assert.That(result.Name, Is.EqualTo(newPlant.Name));
            Assert.That(result.PlantType, Is.EqualTo(newPlant.PlantType));
            Assert.That(result.FoodType, Is.EqualTo(newPlant.FoodType));
            Assert.That(result.Quantity, Is.EqualTo(newPlant.Quantity));
            Assert.That(result.IsEdible, Is.EqualTo(newPlant.IsEdible));
            
        }

        [Test]
        public async Task GetSpecificAsync_WithInvalidCatalogNumber_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string catalogNumber = "randomNumber";
            string expectedMessage = $"No plant found with catalog number: {catalogNumber}";

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>( () => plantsManager.GetSpecificAsync(catalogNumber));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task GetSpecificAsync_WithNullOrWhiteSpaceCatalogNumber_ShouldThrowArgumentException(string catalogNumber)
        {
            // Arrange
            string expectedMessage = "Catalog number cannot be empty.";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => plantsManager.GetSpecificAsync(catalogNumber));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task UpdateAsync_WithValidPlant_ShouldUpdatePlant()
        {
            // Arrange
            string oldName = "Cucumber";

            var newPlant = new Plant()
            {
                CatalogNumber = "1234ABCD9876",
                Name = oldName,
                PlantType = "Some green plant",
                FoodType = "Vegetable",
                Quantity = 100,
                IsEdible = true
            };

            await plantsManager.AddAsync(newPlant);

            string newName = "Potato";
            newPlant.Name = newName;

            // Act
            await plantsManager.UpdateAsync(newPlant);
            var plantInDb = await dbContext.Plants.FirstAsync();

            // Assert
            Assert.NotNull(plantInDb);
            Assert.That(plantInDb.Name, Is.EqualTo(newName));
            Assert.That(plantInDb.Name, Is.Not.EqualTo(oldName));
            Assert.That(plantInDb.Name, Is.EqualTo(newPlant.Name));
            Assert.That(plantInDb.Id, Is.EqualTo(newPlant.Id));
            Assert.That(plantInDb.CatalogNumber, Is.EqualTo(newPlant.CatalogNumber));
            Assert.That(plantInDb.PlantType, Is.EqualTo(newPlant.PlantType));
            Assert.That(plantInDb.FoodType, Is.EqualTo(newPlant.FoodType));
            Assert.That(plantInDb.Quantity, Is.EqualTo(newPlant.Quantity));
            Assert.That(plantInDb.IsEdible, Is.EqualTo(newPlant.IsEdible));
        }

        [Test]
        public async Task UpdateAsync_WithInvalidPlant_ShouldThrowValidationException()
        {
            // Arrange 
            string expectedMessage = "Invalid plant!";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>( () => plantsManager.UpdateAsync(new Plant()));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task UpdateAsync_WithNullPlant_ShouldThrowValidationException()
        {
            // Arrange  
            string expectedMessage = "Invalid plant!";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>( () => plantsManager.UpdateAsync(null));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }
    }
}
