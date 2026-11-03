using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Integration;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class RecommendationPipelineServiceTests
    {
        [Fact]
        public void Generate_UsesNeuralNetworkScores()
        {
            //Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var service = new RecommendationPipelineService(network);

            var featuresA = new RecommendationFeatureInput { VolunteerInterests = "Animals", CategoryName = "Animals", CategoryId = 1, DifficultyLevel = 0 };
            var featuresB = new RecommendationFeatureInput { VolunteerInterests = "Animals", CategoryName = "Environment", CategoryId = 2, DifficultyLevel = 1 };

            VolunteerCauseInput[] inputs =
            [
                new("Maria", "A", featuresA, true),
                new("Maria", "B", featuresB, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];

            double scoreA = network.Forward(RecommendationFeatureCalculator.Calculate(featuresA).ToArray());
            double scoreB = network.Forward(RecommendationFeatureCalculator.Calculate(featuresB).ToArray());

            //Act
            var result = service.Generate(inputs, capacities, 0);

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result.Count(r => r.IsPriority));
            Assert.True(result[0].IsPriority);
            Assert.Equal(1, result[0].Rank);
            Assert.Equal(Math.Max(scoreA, scoreB), result[0].CompatibilityScore, 8);
            Assert.Equal(scoreA, result.Single(r => r.CauseId == "A").CompatibilityScore, 8);
            Assert.Equal(scoreB, result.Single(r => r.CauseId == "B").CompatibilityScore, 8);
        }

        [Fact]
        public void Generate_CalculatesGlobalPriorities()
        {
            //Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var service = new RecommendationPipelineService(network);
            var features = new RecommendationFeatureInput();

            VolunteerCauseInput[] inputs =
            [
                new("Maria", "A", features, true),
                new("Maria", "B", features, true),
                new("Ivan", "A", features, true),
                new("Ivan", "B", features, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];

            //Act
            var result = service.Generate(inputs, capacities, 0);

            //Assert
            Assert.Equal(4, result.Count);
            Assert.Equal(2, result.Count(r => r.IsPriority));
            Assert.Equal(2, result.Where(r => r.IsPriority).Select(r => r.CauseId).Distinct().Count());
            Assert.All(result.Where(r => r.IsPriority), r => Assert.Equal(1, r.Rank));
            Assert.Equal(1, capacities[0].RemainingPlaces);
            Assert.Equal(1, capacities[1].RemainingPlaces);
        }

        [Fact]
        public void Generate_SkipsIneligibleAndFullCauses()
        {
            //Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var service = new RecommendationPipelineService(network);
            var features = new RecommendationFeatureInput();

            VolunteerCauseInput[] inputs =
            [
                new("Maria", "A", features, true),
                new("Maria", "B", features, false),
                new("Maria", "C", features, true)
            ];

            CauseCapacity[] capacities = [new("A", 0), new("B", 1), new("C", 1)];

            //Act
            var result = service.Generate(inputs, capacities, 0);

            //Assert
            Assert.Single(result);
            Assert.Equal("C", result[0].CauseId);
            Assert.True(result[0].IsPriority);
            Assert.Equal(0, capacities[0].RemainingPlaces);
        }

        [Fact]
        public void Generate_KeepsRecommendationsBelowPriorityThreshold()
        {
            //Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var service = new RecommendationPipelineService(network);
            var features = new RecommendationFeatureInput();

            VolunteerCauseInput[] inputs = [new("Maria", "A", features, true)];
            CauseCapacity[] capacities = [new("A", 1)];

            //Act
            var result = service.Generate(inputs, capacities, 1);

            //Assert
            Assert.Single(result);
            Assert.False(result[0].IsPriority);
            Assert.Equal(1, result[0].Rank);
        }
    }
}
