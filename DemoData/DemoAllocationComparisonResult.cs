using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;

namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Per-algorithm summary of an allocation comparison run. These are recommendation
    // priorities only, not actual registrations or reserved places.
    public sealed class AllocationAlgorithmSummary
    {
        public required string AlgorithmName { get; init; }
        public required IReadOnlyList<PriorityRecommendation> SelectedPriorities { get; init; }
        public int PrioritizedVolunteerCount => SelectedPriorities.Count;
        public double TotalCompatibilityScore => SelectedPriorities.Sum(p => p.CompatibilityScore);
    }

    // Full output of DemoAllocationComparisonService.Compare: the shared candidate pool
    // plus each algorithm's selections, and remaining capacity per cause (unaffected by
    // the comparison - these priorities are never committed/reserved).
    public sealed class DemoAllocationComparisonResult
    {
        public required IReadOnlyList<RecommendationCandidate> AllCandidates { get; init; }
        public required AllocationAlgorithmSummary MinCostMaxFlow { get; init; }
        public required AllocationAlgorithmSummary Greedy { get; init; }
        public required IReadOnlyDictionary<string, int> RemainingCapacityByCauseId { get; init; }
    }
}
