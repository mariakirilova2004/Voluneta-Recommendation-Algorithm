using System;
using System.Collections.Generic;
using System.Text;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class MinCostMaxFlowAllocatorTests
    {
        [Fact]
        public void Allocate_FindsMaximumAllocation()
        {
            //Arrange
            AllocationCandidate[] candidates =
            [
                new("Maria", "A", 0.90, true),
                new("Maria", "B", 0.80, true),
                new("Ivan", "A", 0.85, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var allocator = new MinCostMaxFlowAllocator();

            //Act
            var result = allocator.Allocate(candidates, capacities);

            //Assert
            Assert.Equal(2, result.AllocatedCount);
            Assert.Equal(1.65, result.TotalScore, 6);
            Assert.Contains(result.Assignments, a => a.VolunteerId == "Maria" && a.CauseId == "B");
            Assert.Contains(result.Assignments, a => a.VolunteerId == "Ivan" && a.CauseId == "A");
        }

        [Fact]
        public void Allocate_BetterThanGreedy()
        {
            //Arrange
            AllocationCandidate[] candidates =
            [
                new("Maria", "A", 0.90, true),
                new("Maria", "B", 0.80, true),
                new("Ivan", "A", 0.85, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var greedy = new GreedyAllocator();
            var optimal = new MinCostMaxFlowAllocator();

            //Act
            var greedyResult = greedy.Allocate(candidates, capacities);
            var optimalResult = optimal.Allocate(candidates, capacities);

            //Assert
            Assert.Equal(1, greedyResult.AllocatedCount);
            Assert.Equal(2, optimalResult.AllocatedCount);
            Assert.True(optimalResult.TotalScore > greedyResult.TotalScore);
        }

        [Fact]
        public void Allocate_SelectsBestTotalScore()
        {
            //Arrange
            AllocationCandidate[] candidates =
            [
                new("Maria", "A", 0.95, true),
                new("Maria", "B", 0.90, true),
                new("Ivan", "A", 0.94, true),
                new("Ivan", "B", 0.10, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var allocator = new MinCostMaxFlowAllocator();

            //Act
            var result = allocator.Allocate(candidates, capacities, 0);

            //Assert
            Assert.Equal(2, result.AllocatedCount);
            Assert.Equal(1.84, result.TotalScore, 6);
            Assert.Contains(result.Assignments, a => a.VolunteerId == "Maria" && a.CauseId == "B");
            Assert.Contains(result.Assignments, a => a.VolunteerId == "Ivan" && a.CauseId == "A");
        }

        [Fact]
        public void Allocate_RespectsCapacity()
        {
            //Arrange
            AllocationCandidate[] candidates =
            [
                new("Maria", "A", 0.95, true),
                new("Ivan", "A", 0.90, true),
                new("Peter", "A", 0.85, true),
                new("Sara", "A", 0.80, true)
            ];

            CauseCapacity[] capacities = [new("A", 2)];
            var allocator = new MinCostMaxFlowAllocator();

            //Act
            var result = allocator.Allocate(candidates, capacities);

            //Assert
            Assert.Equal(2, result.AllocatedCount);
            Assert.Equal(2, result.Assignments.Select(a => a.VolunteerId).Distinct().Count());
            Assert.Equal(1.85, result.TotalScore, 6);
        }

        [Fact]
        public void Allocate_SkipsIneligibleCandidates()
        {
            //Arrange
            AllocationCandidate[] candidates =
            [
                new("Maria", "A", 0.99, false),
                new("Ivan", "A", 0.40, true),
                new("Peter", "B", 0.80, true)
            ];

            CauseCapacity[] capacities = [new("A", 1), new("B", 1)];
            var allocator = new MinCostMaxFlowAllocator();

            //Act
            var result = allocator.Allocate(candidates, capacities);

            //Assert
            Assert.Single(result.Assignments);
            Assert.Equal("Peter", result.Assignments[0].VolunteerId);
            Assert.Equal("B", result.Assignments[0].CauseId);
        }

        [Fact]
        public void Allocate_NoAvailablePlaces()
        {
            //Arrange
            AllocationCandidate[] candidates = [new("Maria", "A", 0.95, true)];
            CauseCapacity[] capacities = [new("A", 0)];
            var allocator = new MinCostMaxFlowAllocator();

            //Act
            var result = allocator.Allocate(candidates, capacities);

            //Assert
            Assert.Empty(result.Assignments);
            Assert.Equal(0, result.AllocatedCount);
            Assert.Equal(0, result.TotalScore);
        }

        [Fact]
        public void Allocate_RejectsInvalidScore()
        {
            //Arrange
            AllocationCandidate[] candidates = [new("Maria", "A", double.NaN, true)];
            CauseCapacity[] capacities = [new("A", 1)];
            var allocator = new MinCostMaxFlowAllocator();

            //Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Allocate(candidates, capacities));
        }
    }
}
