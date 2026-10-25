using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class GreedyAllocatorTests
    {
        [Fact]
        public void Allocate_RespectsCapacityAndUniqueVolunteers()
        {
            // Arrange
            var candidates = new[]
            {
                new AllocationCandidate("Maria", "A", 0.95, true),
                new AllocationCandidate("Ivan", "A", 0.90, true),
                new AllocationCandidate("Ivan", "B", 0.85, true),
                new AllocationCandidate("Peter", "B", 0.80, true),
                new AllocationCandidate("Sara", "B", 0.70, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1),
                new CauseCapacity("B", 2)
            };
            var allocator = new GreedyAllocator();

            // Act
            var result = allocator.Allocate(candidates, capacities);

            // Assert
            Assert.Equal(3, result.AllocatedCount);
            Assert.Equal(1, result.Assignments.Count(a => a.CauseId == "A"));
            Assert.Equal(2, result.Assignments.Count(a => a.CauseId == "B"));
            Assert.Equal(3, result.Assignments.Select(a => a.VolunteerId).Distinct().Count());
        }

        [Fact]
        public void Allocate_GreedyCanMissMaximumFlow()
        {
            // Arrange
            var candidates = new[]
            {
                new AllocationCandidate("Maria", "A", 0.90, true),
                new AllocationCandidate("Maria", "B", 0.80, true),
                new AllocationCandidate("Ivan", "A", 0.85, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1),
                new CauseCapacity("B", 1)
            };
            var allocator = new GreedyAllocator();

            // Act
            var result = allocator.Allocate(candidates, capacities);

            // Assert
            Assert.Single(result.Assignments);
            Assert.Equal("Maria", result.Assignments[0].VolunteerId);
            Assert.Equal("A", result.Assignments[0].CauseId);
            Assert.Equal(0.90, result.TotalScore, 6);
        }

        [Fact]
        public void Allocate_ExcludesIneligibleAndLowScores()
        {
            // Arrange
            var candidates = new[]
            {
                new AllocationCandidate("Maria", "A", 0.99, false),
                new AllocationCandidate("Maria", "B", 0.49, true),
                new AllocationCandidate("Ivan", "A", 0.75, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1),
                new CauseCapacity("B", 1)
            };
            var allocator = new GreedyAllocator();

            // Act
            var result = allocator.Allocate(candidates, capacities, minimumScore: 0.50);

            // Assert
            Assert.Single(result.Assignments);
            Assert.Equal("Ivan", result.Assignments[0].VolunteerId);
            Assert.Equal("A", result.Assignments[0].CauseId);
        }

        [Fact]
        public void Allocate_RejectsInvalidScore()
        {
            // Arrange
            var candidates = new[]
            {
                new AllocationCandidate("Maria", "A", double.NaN, true)
            };
            var capacities = new[]
            {
                new CauseCapacity("A", 1)
            };
            var allocator = new GreedyAllocator();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Allocate(candidates, capacities));
        }
    }
}
