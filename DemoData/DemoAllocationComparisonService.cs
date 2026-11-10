using Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;

namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Demonstrates Algorithm 2 (priority allocation) in isolation, reusing the real
    // Algorithm 1 compatibility scores produced by DemoRecommendationService. Runs the
    // existing MinCostMaxFlowPriorityAllocator and GreedyPriorityAllocator on the SAME
    // candidate pool, capacities, and minimum-score threshold so results are directly
    // comparable. Never mutates DemoDataSet or any shared/stored capacities - capacity
    // overrides are request-local only. Results are deterministic recommendation
    // priorities, not actual registrations or reserved places.
    public sealed class DemoAllocationComparisonService
    {
        private readonly DemoRecommendationService recommendationService;
        private readonly MinCostMaxFlowPriorityAllocator minCostMaxFlowAllocator = new();
        private readonly GreedyPriorityAllocator greedyAllocator = new();

        public DemoAllocationComparisonService(DemoRecommendationService recommendationService)
        {
            this.recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        }

        // capacityOverrides: optional temporary remaining-places overrides, keyed by CauseId.
        // Causes not present keep the scenario's default remaining-places value. Overrides are
        // validated and applied only in-memory for this call.
        public DemoAllocationComparisonResult Compare(
            IReadOnlyDictionary<string, int>? capacityOverrides = null,
            double minimumScore = 0.50,
            DemoOptimizationScenario scenario = DemoOptimizationScenario.NeuralNetwork)
        {
            if (!double.IsFinite(minimumScore) || minimumScore < 0 || minimumScore > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumScore));
            }

            if (capacityOverrides != null)
            {
                foreach (var kvp in capacityOverrides)
                {
                    if (kvp.Value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(capacityOverrides), "Capacity override cannot be negative.");
                    }
                }
            }

            // Step 1: candidate pool and scores depend on the selected scenario only. Both
            // scenarios share the same 4 volunteer IDs / 3 cause IDs from DemoDataSet.
            var candidates = scenario == DemoOptimizationScenario.Educational
                ? BuildEducationalCandidates()
                : BuildNeuralNetworkCandidates();

            // Step 2: build capacities from the scenario's defaults, applying temporary
            // overrides only for this call. DemoDataSet.Causes is never mutated.
            var defaultRemainingPlaces = scenario == DemoOptimizationScenario.Educational
                ? DemoEducationalScenario.DefaultRemainingPlaces
                : DemoDataSet.Causes.ToDictionary(c => c.Id, c => c.RemainingPlaces, StringComparer.Ordinal);

            var capacities = DemoDataSet.Causes
                .Select(cause =>
                {
                    int remaining = capacityOverrides != null && capacityOverrides.TryGetValue(cause.Id, out var overridePlaces)
                        ? overridePlaces
                        : defaultRemainingPlaces[cause.Id];
                    return new CauseCapacity(cause.Id, remaining);
                })
                .ToList();

            // Step 3: run both allocators on the identical candidates/capacities/threshold.
            var minCostMaxFlowResult = minCostMaxFlowAllocator.CalculatePriorities(candidates, capacities, minimumScore);
            var greedyResult = greedyAllocator.CalculatePriorities(candidates, capacities, minimumScore);

            // Step 4: remaining capacity per cause is reported as-is (post-override input),
            // since these are recommendation priorities only - no places are reserved.
            var remainingCapacityByCauseId = capacities.ToDictionary(c => c.CauseId, c => c.RemainingPlaces, StringComparer.Ordinal);

            return new DemoAllocationComparisonResult
            {
                AllCandidates = candidates,
                MinCostMaxFlow = new AllocationAlgorithmSummary
                {
                    AlgorithmName = "Min-Cost Max-Flow",
                    SelectedPriorities = minCostMaxFlowResult.Priorities
                },
                Greedy = new AllocationAlgorithmSummary
                {
                    AlgorithmName = "Greedy",
                    SelectedPriorities = greedyResult.Priorities
                },
                RemainingCapacityByCauseId = remainingCapacityByCauseId
            };
        }

        // Real Algorithm 1 scores for every eligible volunteer-cause pair, mapped to
        // RecommendationCandidate without recalculating or hardcoding anything.
        private List<RecommendationCandidate> BuildNeuralNetworkCandidates()
        {
            var scoredResults = recommendationService.GenerateRankedRecommendations();

            return scoredResults
                .OrderBy(r => r.VolunteerId, StringComparer.Ordinal)
                .ThenBy(r => r.CauseId, StringComparer.Ordinal)
                .Select(r => new RecommendationCandidate(r.VolunteerId, r.CauseId, r.Score, isEligible: true))
                .ToList();
        }

        // Explicitly constructed educational scores (NOT neural network predictions) for the
        // same volunteer/cause IDs, used only to illustrate the Greedy vs Min-Cost Max-Flow
        // trade-off.
        private static List<RecommendationCandidate> BuildEducationalCandidates()
        {
            return DemoEducationalScenario.CompatibilityScores
                .SelectMany(v => v.Value.Select(c => new RecommendationCandidate(v.Key, c.Key, c.Value, isEligible: true)))
                .OrderBy(c => c.VolunteerId, StringComparer.Ordinal)
                .ThenBy(c => c.CauseId, StringComparer.Ordinal)
                .ToList();
        }

    }
}
