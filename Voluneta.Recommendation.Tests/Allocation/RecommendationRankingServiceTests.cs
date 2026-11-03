using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Ranking;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class RecommendationRankingServiceTests
    {
        [Fact]
        public void RankForVolunteer_PutsPriorityFirst()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.95, true),
                new("Maria", "B", 0.90, true),
                new("Maria", "C", 0.75, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 2), new("C", 3)];
            var priorities = new RecommendationPriorityResult([new PriorityRecommendation("Maria", "B", 0.90)]);
            var service = new RecommendationRankingService();

            //Act
            var result = service.RankForVolunteer("Maria", candidates, capacities, priorities);

            //Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { "B", "A", "C" }, result.Select(r => r.CauseId));
            Assert.True(result[0].IsPriority);
            Assert.Equal(1, result[0].Rank);
        }

        [Fact]
        public void RankForVolunteer_SortsByScoreWithoutPriority()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.60, true),
                new("Maria", "B", 0.95, true),
                new("Maria", "C", 0.80, true)
            ];

            CauseCapacity[] capacities = [new("A", 2), new("B", 2), new("C", 2)];
            var priorities = new RecommendationPriorityResult([]);
            var service = new RecommendationRankingService();

            //Act
            var result = service.RankForVolunteer("Maria", candidates, capacities, priorities);

            //Assert
            Assert.Equal(new[] { "B", "C", "A" }, result.Select(r => r.CauseId));
            Assert.All(result, r => Assert.False(r.IsPriority));
        }

        [Fact]
        public void RankForVolunteer_SkipsFullCauses()
        {
            //Arrange
            RecommendationCandidate[] candidates = [new("Maria", "A", 0.95, true), new("Maria", "B", 0.80, true)];
            CauseCapacity[] capacities = [new("A", 0), new("B", 2)];
            var priorities = new RecommendationPriorityResult([new PriorityRecommendation("Maria", "A", 0.95)]);
            var service = new RecommendationRankingService();

            //Act
            var result = service.RankForVolunteer("Maria", candidates, capacities, priorities);

            //Assert
            Assert.Single(result);
            Assert.Equal("B", result[0].CauseId);
            Assert.False(result[0].IsPriority);
            Assert.Equal(0, capacities[0].RemainingPlaces);
        }

        [Fact]
        public void RankForVolunteer_SkipsIneligibleCauses()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.99, false),
                new("Maria", "B", 0.40, true),
                new("Ivan", "C", 0.95, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1), new("C", 1)];
            var priorities = new RecommendationPriorityResult([]);
            var service = new RecommendationRankingService();

            //Act
            var result = service.RankForVolunteer("Maria", candidates, capacities, priorities);

            //Assert
            Assert.Single(result);
            Assert.Equal("B", result[0].CauseId);
            Assert.Equal(0.40, result[0].CompatibilityScore, 6);
        }
    }
}
