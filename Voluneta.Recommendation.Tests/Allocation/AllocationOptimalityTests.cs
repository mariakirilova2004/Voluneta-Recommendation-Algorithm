using System;
using System.Collections.Generic;
using System.Text;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class AllocationOptimalityTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(42)]
        [InlineData(123)]
        [InlineData(2026)]
        public void Allocate_MatchesBruteForce(int seed)
        {
            //Arrange
            var random = new Random(seed);
            var allocator = new MinCostMaxFlowAllocator();

            for (int test = 0; test < 30; test++)
            {
                CauseCapacity[] capacities = [new("A", random.Next(0, 3)),
                                          new("B", random.Next(0, 3)),
                                          new("C", random.Next(0, 3))];

                var candidates = new List<AllocationCandidate>();

                for (int i = 0; i < 4; i++)
                {
                    foreach (var cause in capacities)
                    {
                        if (random.NextDouble() < 0.8)
                            candidates.Add(new($"V{i}", cause.CauseId,
                                Math.Round(random.NextDouble(), 2), random.NextDouble() < 0.85));
                    }
                }

                //Act
                var expected = BruteForce(candidates, capacities);
                var actual = allocator.Allocate(candidates, capacities);

                //Assert
                Assert.Equal(expected.Count, actual.AllocatedCount);
                Assert.Equal(expected.Score, actual.TotalScore, 6);
            }
        }

        private static (int Count, double Score) BruteForce(
            List<AllocationCandidate> candidates, CauseCapacity[] capacities)
        {
            var volunteers = candidates.Select(c => c.VolunteerId).Distinct().ToArray();
            var places = capacities.ToDictionary(c => c.CauseId, c => c.AvailablePlaces);

            int bestCount = 0;
            double bestScore = 0;

            void Search(int index, int count, double score)
            {
                if (index == volunteers.Length)
                {
                    if (count > bestCount || (count == bestCount && score > bestScore))
                    {
                        bestCount = count;
                        bestScore = score;
                    }
                    return;
                }

                // Leave this volunteer without an assignment
                Search(index + 1, count, score);

                foreach (var candidate in candidates.Where(c =>
                    c.VolunteerId == volunteers[index] && c.IsEligible && c.Score >= 0.50))
                {
                    if (places[candidate.CauseId] == 0) continue;

                    places[candidate.CauseId]--;
                    Search(index + 1, count + 1, score + candidate.Score);
                    places[candidate.CauseId]++;
                }
            }

            Search(0, 0, 0);
            return (bestCount, bestScore);
        }
    }
}
