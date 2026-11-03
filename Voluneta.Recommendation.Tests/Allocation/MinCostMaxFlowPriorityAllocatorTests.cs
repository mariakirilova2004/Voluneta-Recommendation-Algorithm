using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class MinCostMaxFlowPriorityAllocatorTests
    {
        [Fact]
        public void FindsMaximumPriorities()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.90, true),
                new("Maria", "B", 0.80, true),
                new("Ivan", "A", 0.85, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var allocator = new MinCostMaxFlowPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Equal(2, result.PrioritizedCount);
            Assert.Equal(1.65, result.TotalScore, 6);
            Assert.Contains(result.Priorities, p => p.VolunteerId == "Maria" && p.CauseId == "B");
            Assert.Contains(result.Priorities, p => p.VolunteerId == "Ivan" && p.CauseId == "A");
        }

        [Fact]
        public void BetterThanGreedy()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.90, true),
                new("Maria", "B", 0.80, true),
                new("Ivan", "A", 0.85, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var greedy = new GreedyPriorityAllocator();
            var optimal = new MinCostMaxFlowPriorityAllocator();

            //Act
            var greedyResult = greedy.CalculatePriorities(candidates, capacities);
            var optimalResult = optimal.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Equal(1, greedyResult.PrioritizedCount);
            Assert.Equal(2, optimalResult.PrioritizedCount);
            Assert.True(optimalResult.TotalScore > greedyResult.TotalScore);
        }

        [Fact]
        public void SelectsBestTotalScore()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.95, true),
                new("Maria", "B", 0.90, true),
                new("Ivan", "A", 0.94, true),
                new("Ivan", "B", 0.10, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var allocator = new MinCostMaxFlowPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities, 0);

            //Assert
            Assert.Equal(2, result.PrioritizedCount);
            Assert.Equal(1.84, result.TotalScore, 6);
            Assert.Contains(result.Priorities, p => p.VolunteerId == "Maria" && p.CauseId == "B");
            Assert.Contains(result.Priorities, p => p.VolunteerId == "Ivan" && p.CauseId == "A");
        }

        [Fact]
        public void RespectsCapacity()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.95, true),
                new("Ivan", "A", 0.90, true),
                new("Peter", "A", 0.85, true),
                new("Sara", "A", 0.80, true)
            ];

            CauseCapacity[] capacities = [new("A", 2)];
            var allocator = new MinCostMaxFlowPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Equal(2, result.PrioritizedCount);
            Assert.Equal(2, result.Priorities.Select(p => p.VolunteerId).Distinct().Count());
            Assert.Equal(1.85, result.TotalScore, 6);
        }

        [Fact]
        public void SkipsIneligibleCandidates()
        {
            //Arrange
            RecommendationCandidate[] candidates =
            [
                new("Maria", "A", 0.99, false),
                new("Ivan", "A", 0.40, true),
                new("Peter", "B", 0.80, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var allocator = new MinCostMaxFlowPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Single(result.Priorities);
            Assert.Equal("Peter", result.Priorities[0].VolunteerId);
            Assert.Equal("B", result.Priorities[0].CauseId);
        }

        [Fact]
        public void NoRemainingPlaces()
        {
            //Arrange
            RecommendationCandidate[] candidates = [new("Maria", "A", 0.95, true)];
            CauseCapacity[] capacities = [new("A", 0)];
            var allocator = new MinCostMaxFlowPriorityAllocator();

            //Act
            var result = allocator.CalculatePriorities(candidates, capacities);

            //Assert
            Assert.Empty(result.Priorities);
            Assert.Equal(0, result.PrioritizedCount);
            Assert.Equal(0, result.TotalScore);
        }

        [Fact]
        public void RejectsInvalidScore()
        {
            //Arrange
            RecommendationCandidate[] candidates = [new("Maria", "A", double.NaN, true)];
            CauseCapacity[] capacities = [new("A", 1)];
            var allocator = new MinCostMaxFlowPriorityAllocator();

            //Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.CalculatePriorities(candidates, capacities));
        }
    }
}
