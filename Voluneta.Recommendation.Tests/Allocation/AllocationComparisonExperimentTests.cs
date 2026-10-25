using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;
using Xunit.Abstractions;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class AllocationComparisonExperimentTests
    {
        private readonly ITestOutputHelper _output;

        public AllocationComparisonExperimentTests(ITestOutputHelper output) => _output = output;

        [Theory]
        [InlineData(20, 5)]
        [InlineData(50, 10)]
        [InlineData(100, 20)]
        public void Compare_Algorithms(int volunteerCount, int causeCount)
        {
            //Arrange
            var random = new Random(42 + volunteerCount);
            var capacities = Enumerable.Range(0, causeCount)
                .Select(i => new CauseCapacity($"C{i}", random.Next(1, 5))).ToArray();

            var candidates = new List<AllocationCandidate>();

            for (int i = 0; i < volunteerCount; i++)
            {
                for (int j = 0; j < causeCount; j++)
                {
                    if (random.NextDouble() < 0.6)
                        candidates.Add(new($"V{i}", $"C{j}", Math.Round(random.NextDouble(), 2),
                            random.NextDouble() < 0.9));
                }
            }

            var greedy = new GreedyAllocator();
            var optimal = new MinCostMaxFlowAllocator();

            //Act
            var greedyResult = greedy.Allocate(candidates, capacities);
            var optimalResult = optimal.Allocate(candidates, capacities);

            double greedyMs = Measure(() => greedy.Allocate(candidates, capacities));
            double optimalMs = Measure(() => optimal.Allocate(candidates, capacities));

            //Assert
            Assert.True(optimalResult.AllocatedCount >= greedyResult.AllocatedCount);

            if (optimalResult.AllocatedCount == greedyResult.AllocatedCount)
                Assert.True(optimalResult.TotalScore >= greedyResult.TotalScore - 0.000001);

            Assert.Equal(optimalResult.AllocatedCount,
                optimalResult.Assignments.Select(a => a.VolunteerId).Distinct().Count());

            foreach (var cause in capacities)
                Assert.True(optimalResult.Assignments.Count(a => a.CauseId == cause.CauseId) <= cause.AvailablePlaces);

            // Results
            _output.WriteLine($"Volunteers: {volunteerCount}, Causes: {causeCount}, Pairs: {candidates.Count}");
            _output.WriteLine($"Greedy: {greedyResult.AllocatedCount} assigned, score {greedyResult.TotalScore:F3}, time {greedyMs:F3} ms");
            _output.WriteLine($"Min-Cost Max-Flow: {optimalResult.AllocatedCount} assigned, score {optimalResult.TotalScore:F3}, time {optimalMs:F3} ms");
        }

        private static double Measure(Action action)
        {
            action(); // Warm up
            var times = new double[5];

            for (int i = 0; i < times.Length; i++)
            {
                var watch = Stopwatch.StartNew();
                action();
                watch.Stop();
                times[i] = watch.Elapsed.TotalMilliseconds;
            }

            Array.Sort(times);
            return times[2];
        }
    }
}
