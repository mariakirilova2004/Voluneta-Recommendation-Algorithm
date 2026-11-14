using System.Diagnostics;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Reporting
{
    // One deterministic synthetic performance scenario: a fixed-size candidate pool plus cause
    // capacities, generated from a fixed random seed. Reused unchanged (same candidates,
    // capacities, and threshold) for both allocators so timings are directly comparable.
    public sealed record AllocationPerformanceScenario(
        string Name,
        int Seed,
        int VolunteerCount,
        int CauseCount,
        IReadOnlyList<RecommendationCandidate> Candidates,
        IReadOnlyList<CauseCapacity> Capacities)
    {
        public int CandidateEdgeCount => Candidates.Count;
    }

    // Timing results (in milliseconds) for one algorithm on one scenario, over several measured
    // repetitions, plus context (candidate edge count, prioritized volunteer count).
    public sealed record AllocationPerformanceRow(
        string ScenarioName,
        int Seed,
        int VolunteerCount,
        int CauseCount,
        int CandidateEdgeCount,
        double Threshold,
        string AlgorithmName,
        int Repetitions,
        double MedianMilliseconds,
        double MeanMilliseconds,
        double MinMilliseconds,
        int PrioritizedVolunteerCount);

    // Builds deterministic synthetic candidate pools of increasing size and benchmarks the
    // existing MinCostMaxFlowPriorityAllocator and GreedyPriorityAllocator using Stopwatch, with
    // a warm-up run and several measured repetitions per scenario/algorithm. Never changes either
    // allocator. Input generation always happens outside the timed section.
    public static class AllocationPerformanceBenchmarkRunner
    {
        public const double Threshold = 0.50;
        public const int MeasuredRepetitions = 5;

        private static readonly MinCostMaxFlowPriorityAllocator MinCostMaxFlowAllocator = new();
        private static readonly GreedyPriorityAllocator GreedyAllocator = new();

        public static IReadOnlyList<AllocationPerformanceScenario> BuildScenarios()
        {
            return
            [
                BuildScenario("Small (10 volunteers x 5 causes)", seed: 11, volunteerCount: 10, causeCount: 5, edgeProbability: 0.6, minCapacity: 1, maxCapacity: 3),
                BuildScenario("Medium (25 volunteers x 10 causes)", seed: 22, volunteerCount: 25, causeCount: 10, edgeProbability: 0.5, minCapacity: 1, maxCapacity: 4),
                BuildScenario("Large (50 volunteers x 15 causes)", seed: 33, volunteerCount: 50, causeCount: 15, edgeProbability: 0.4, minCapacity: 1, maxCapacity: 5),
                BuildScenario("Extra Large (100 volunteers x 20 causes)", seed: 44, volunteerCount: 100, causeCount: 20, edgeProbability: 0.35, minCapacity: 1, maxCapacity: 6)
            ];
        }

        // Generates a deterministic synthetic candidate pool with bounded density (not a full
        // dense volunteer x cause matrix) from a fixed seed, so re-running the benchmark always
        // produces the same inputs. These are synthetic test fixtures, not real volunteer data
        // or neural network predictions.
        private static AllocationPerformanceScenario BuildScenario(string name, int seed, int volunteerCount, int causeCount, double edgeProbability, int minCapacity, int maxCapacity)
        {
            var random = new Random(seed);

            var volunteerIds = Enumerable.Range(1, volunteerCount).Select(i => $"PV{i}").ToList();
            var causeIds = Enumerable.Range(1, causeCount).Select(i => $"PC{i}").ToList();

            var candidates = new List<RecommendationCandidate>();
            foreach (var volunteerId in volunteerIds)
            {
                bool hasAtLeastOneCandidate = false;

                foreach (var causeId in causeIds)
                {
                    if (random.NextDouble() <= edgeProbability)
                    {
                        double score = Math.Round(random.NextDouble(), 2);
                        candidates.Add(new RecommendationCandidate(volunteerId, causeId, score, isEligible: true));
                        hasAtLeastOneCandidate = true;
                    }
                }

                // Ensure every volunteer has at least one candidate pair, keeping density bounded
                // but realistic rather than leaving isolated volunteers.
                if (!hasAtLeastOneCandidate)
                {
                    string fallbackCauseId = causeIds[random.Next(causeIds.Count)];
                    double score = Math.Round(random.NextDouble(), 2);
                    candidates.Add(new RecommendationCandidate(volunteerId, fallbackCauseId, score, isEligible: true));
                }
            }

            var capacities = causeIds
                .Select(causeId => new CauseCapacity(causeId, random.Next(minCapacity, maxCapacity + 1)))
                .ToList();

            return new AllocationPerformanceScenario(name, seed, volunteerCount, causeCount, candidates, capacities);
        }

        public static IReadOnlyList<AllocationPerformanceRow> Run(IReadOnlyList<AllocationPerformanceScenario> scenarios)
        {
            var rows = new List<AllocationPerformanceRow>();

            foreach (var scenario in scenarios)
            {
                rows.Add(BenchmarkAlgorithm(scenario, "Min-Cost Max-Flow", (candidates, capacities) => MinCostMaxFlowAllocator.CalculatePriorities(candidates, capacities, Threshold)));
                rows.Add(BenchmarkAlgorithm(scenario, "Greedy", (candidates, capacities) => GreedyAllocator.CalculatePriorities(candidates, capacities, Threshold)));
            }

            return rows;
        }

        private static AllocationPerformanceRow BenchmarkAlgorithm(
            AllocationPerformanceScenario scenario,
            string algorithmName,
            Func<IReadOnlyList<RecommendationCandidate>, IReadOnlyList<CauseCapacity>, RecommendationPriorityResult> invoke)
        {
            // Warm-up run (JIT, caching) on a fresh capacity copy, outside measured timings.
            RecommendationPriorityResult warmupResult = invoke(scenario.Candidates, CloneCapacities(scenario.Capacities));

            var measuredMilliseconds = new List<double>(MeasuredRepetitions);
            RecommendationPriorityResult? lastResult = null;

            for (int i = 0; i < MeasuredRepetitions; i++)
            {
                // Input (candidates + fresh capacities) is prepared outside the timed section.
                var capacitiesForRun = CloneCapacities(scenario.Capacities);

                var stopwatch = Stopwatch.StartNew();
                lastResult = invoke(scenario.Candidates, capacitiesForRun);
                stopwatch.Stop();

                measuredMilliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            measuredMilliseconds.Sort();
            double median = measuredMilliseconds.Count % 2 == 1
                ? measuredMilliseconds[measuredMilliseconds.Count / 2]
                : (measuredMilliseconds[measuredMilliseconds.Count / 2 - 1] + measuredMilliseconds[measuredMilliseconds.Count / 2]) / 2.0;
            double mean = measuredMilliseconds.Average();
            double min = measuredMilliseconds[0];

            return new AllocationPerformanceRow(
                scenario.Name,
                scenario.Seed,
                scenario.VolunteerCount,
                scenario.CauseCount,
                scenario.CandidateEdgeCount,
                Threshold,
                algorithmName,
                MeasuredRepetitions,
                median,
                mean,
                min,
                lastResult!.PrioritizedCount);
        }

        private static List<CauseCapacity> CloneCapacities(IReadOnlyList<CauseCapacity> capacities)
        {
            return capacities.Select(c => new CauseCapacity(c.CauseId, c.RemainingPlaces)).ToList();
        }
    }
}
