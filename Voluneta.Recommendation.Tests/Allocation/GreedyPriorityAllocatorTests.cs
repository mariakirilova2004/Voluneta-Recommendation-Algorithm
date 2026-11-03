using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class GreedyPriorityAllocatorTests
    {
        [Fact]
        public void RespectsCapacityAndUniqueVolunteers()
        {
            //Arrange
            var candidates = new[]
            {
                new RecommendationCandidate("Maria", "A", 0.95, true),
                new RecommendationCandidate("Ivan", "A", 0.90, true),
                new RecommendationCandidate("Ivan", "B", 0.85, true),
                new RecommendationCandidate("Peter", "B", 0.80, true),
                new RecommendationCandidate("Sara", "B", 0.70, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1),
                new CauseCapacity("B", 2)
            };
            var allocator = new GreedyPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Equal(3, result.PrioritizedCount);
            Assert.Equal(1, result.Priorities.Count(p => p.CauseId == "A"));
            Assert.Equal(2, result.Priorities.Count(p => p.CauseId == "B"));
            Assert.Equal(3, result.Priorities.Select(p => p.VolunteerId).Distinct().Count());
        }

        [Fact]
        public void GreedyCanMissMaximumFlow()
        {
            //Arrange
            var candidates = new[]
            {
                new RecommendationCandidate("Maria", "A", 0.90, true),
                new RecommendationCandidate("Maria", "B", 0.80, true),
                new RecommendationCandidate("Ivan", "A", 0.85, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1),
                new CauseCapacity("B", 1)
            };
            var allocator = new GreedyPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Single(result.Priorities);
            Assert.Equal("Maria", result.Priorities[0].VolunteerId);
            Assert.Equal("A", result.Priorities[0].CauseId);
            Assert.Equal(0.90, result.TotalScore, 6);
        }

        [Fact]
        public void ExcludesIneligibleAndLowScores()
        {
            //Arrange
            var candidates = new[]
            {
                new RecommendationCandidate("Maria", "A", 0.99, false),
                new RecommendationCandidate("Maria", "B", 0.49, true),
                new RecommendationCandidate("Ivan", "A", 0.75, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1),
                new CauseCapacity("B", 1)
            };
            var allocator = new GreedyPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities, minimumScore: 0.50);

            //Assert
            Assert.Single(result.Priorities);
            Assert.Equal("Ivan", result.Priorities[0].VolunteerId);
            Assert.Equal("A", result.Priorities[0].CauseId);
        }

        [Fact]
        public void RejectsInvalidScore()
        {
            //Arrange
            var candidates = new[]
            {
                new RecommendationCandidate("Maria", "A", double.NaN, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1)
            };
            var allocator = new GreedyPriorityAllocator();

            //Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.CalculatePriorities(candidates, capacities));
        }
    }
}
