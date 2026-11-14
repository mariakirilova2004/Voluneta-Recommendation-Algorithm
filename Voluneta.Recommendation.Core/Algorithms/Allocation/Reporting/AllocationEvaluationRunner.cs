using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Reporting
{
    // One reproducible test scenario for the Algorithm 2 (priority allocation) evaluation:
    // a fixed candidate pool plus cause capacities. Scores are either an explicitly
    // constructed educational example or deterministic synthetic values generated from a
    // fixed random seed - never real neural network predictions.
    public sealed record AllocationScenario(
        string Name,
        string ScoreSource,
        int? Seed,
        IReadOnlyList<RecommendationCandidate> Candidates,
        IReadOnlyList<CauseCapacity> Capacities)
    {
        public int VolunteerCount => Candidates.Select(c => c.VolunteerId).Distinct(StringComparer.Ordinal).Count();
        public int CauseCount => Capacities.Count;
        public int TotalCapacity => Capacities.Sum(c => c.RemainingPlaces);
    }

    // Per-algorithm metrics for one scenario/threshold combination.
    public sealed record AllocationEvaluationRow(
        string ScenarioName,
        string ScoreSource,
        int? Seed,
        double Threshold,
        int VolunteerCount,
        int CauseCount,
        int TotalCapacity,
        string AlgorithmName,
        int PrioritizedVolunteerCount,
        double TotalCompatibilityScore,
        double AverageSelectedCompatibilityScore,
        double CapacityUtilization);

    // Builds a small, fixed set of reproducible scenarios (the existing educational Greedy vs
    // Optimal example plus deterministic synthetic cases with varied volunteer/cause counts and
    // capacities) and runs both existing Core allocators - MinCostMaxFlowPriorityAllocator and
    // GreedyPriorityAllocator - on identical candidates/capacities/thresholds for each. Never
    // changes the allocators themselves.
    public static class AllocationEvaluationRunner
    {
        public static readonly double[] Thresholds = [0.30, 0.50, 0.70];

        private static readonly MinCostMaxFlowPriorityAllocator MinCostMaxFlowAllocator = new();
        private static readonly GreedyPriorityAllocator GreedyAllocator = new();

        public static IReadOnlyList<AllocationScenario> BuildScenarios()
        {
            return
            [
                BuildEducationalScenario(),
                BuildSyntheticScenario("Synthetic Small (5 volunteers x 3 causes)", seed: 1001, volunteerCount: 5, causeCount: 3, minCapacity: 1, maxCapacity: 2),
                BuildSyntheticScenario("Synthetic Medium (8 volunteers x 4 causes)", seed: 2002, volunteerCount: 8, causeCount: 4, minCapacity: 1, maxCapacity: 3),
                BuildSyntheticScenario("Synthetic Large (12 volunteers x 5 causes)", seed: 3003, volunteerCount: 12, causeCount: 5, minCapacity: 1, maxCapacity: 4)
            ];
        }

        // The same 4 volunteer / 3 cause educational case used by the Optimization demo
        // (DemoEducationalScenario): explicitly constructed scores, NOT neural network
        // predictions, designed to make the Greedy vs Min-Cost Max-Flow trade-off visible.
        private static AllocationScenario BuildEducationalScenario()
        {
            var scores = new (string VolunteerId, string CauseId, double Score)[]
            {
                ("V1", "C1", 0.95), ("V1", "C2", 0.92), ("V1", "C3", 0.10),
                ("V2", "C1", 0.94), ("V2", "C2", 0.10), ("V2", "C3", 0.10),
                ("V3", "C1", 0.10), ("V3", "C2", 0.10), ("V3", "C3", 0.90),
                ("V4", "C1", 0.10), ("V4", "C2", 0.10), ("V4", "C3", 0.88)
            };

            var candidates = scores
                .Select(s => new RecommendationCandidate(s.VolunteerId, s.CauseId, s.Score, isEligible: true))
                .ToList();

            var capacities = new List<CauseCapacity>
            {
                new("C1", 1),
                new("C2", 1),
                new("C3", 2)
            };

            return new AllocationScenario("Educational Greedy vs Optimal", "Constructed educational scores (not neural network predictions)", Seed: null, candidates, capacities);
        }

        // Deterministic synthetic scenario: candidate scores and cause capacities are generated
        // from a fixed seed using System.Random, so the same seed always reproduces the same
        // scenario. These scores are synthetic test fixtures, not neural network predictions.
        private static AllocationScenario BuildSyntheticScenario(string name, int seed, int volunteerCount, int causeCount, int minCapacity, int maxCapacity)
        {
            var random = new Random(seed);

            var volunteerIds = Enumerable.Range(1, volunteerCount).Select(i => $"SV{i}").ToList();
            var causeIds = Enumerable.Range(1, causeCount).Select(i => $"SC{i}").ToList();

            var candidates = new List<RecommendationCandidate>();
            foreach (var volunteerId in volunteerIds)
            {
                foreach (var causeId in causeIds)
                {
                    double score = Math.Round(random.NextDouble(), 2);
                    candidates.Add(new RecommendationCandidate(volunteerId, causeId, score, isEligible: true));
                }
            }

            var capacities = causeIds
                .Select(causeId => new CauseCapacity(causeId, random.Next(minCapacity, maxCapacity + 1)))
                .ToList();

            return new AllocationScenario(name, "Deterministic synthetic scores (fixed random seed, not neural network predictions)", seed, candidates, capacities);
        }

        public static IReadOnlyList<AllocationEvaluationRow> Run(IReadOnlyList<AllocationScenario> scenarios)
        {
            var rows = new List<AllocationEvaluationRow>();

            foreach (var scenario in scenarios)
            {
                foreach (double threshold in Thresholds)
                {
                    var minCostMaxFlowResult = MinCostMaxFlowAllocator.CalculatePriorities(scenario.Candidates, scenario.Capacities, threshold);
                    var greedyResult = GreedyAllocator.CalculatePriorities(scenario.Candidates, scenario.Capacities, threshold);

                    rows.Add(BuildRow(scenario, threshold, "Min-Cost Max-Flow", minCostMaxFlowResult));
                    rows.Add(BuildRow(scenario, threshold, "Greedy", greedyResult));
                }
            }

            return rows;
        }

        private static AllocationEvaluationRow BuildRow(AllocationScenario scenario, double threshold, string algorithmName, RecommendationPriorityResult result)
        {
            int prioritizedCount = result.PrioritizedCount;
            double totalScore = result.TotalScore;
            double averageScore = prioritizedCount > 0 ? totalScore / prioritizedCount : 0.0;
            double capacityUtilization = scenario.TotalCapacity > 0 ? (double)prioritizedCount / scenario.TotalCapacity : 0.0;

            return new AllocationEvaluationRow(
                scenario.Name,
                scenario.ScoreSource,
                scenario.Seed,
                threshold,
                scenario.VolunteerCount,
                scenario.CauseCount,
                scenario.TotalCapacity,
                algorithmName,
                prioritizedCount,
                totalScore,
                averageScore,
                capacityUtilization);
        }
    }
}
