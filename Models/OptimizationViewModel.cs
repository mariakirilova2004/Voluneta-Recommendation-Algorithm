namespace Voluneta_Recommendation_Algorithm.Models
{
    // Editable remaining-capacity row for one demo cause, shown in the optimization settings panel.
    public sealed class CauseCapacityInputViewModel
    {
        public required string CauseId { get; init; }
        public required string CauseName { get; init; }
        public required string CategoryName { get; init; }
        public required int DefaultRemainingPlaces { get; init; }
        public required int RemainingPlaces { get; init; }
    }

    // One option in the scenario selector (Neural Network Predictions vs Greedy vs Optimal).
    public sealed class ScenarioOptionViewModel
    {
        public required string Value { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required string ScoreSourceLabel { get; init; }
        public required double DefaultMinimumScore { get; init; }
        public required bool IsSelected { get; init; }
    }

    // One volunteer-cause compatibility pair from Algorithm 1, before either allocator decides
    // whether it becomes a priority recommendation. Also carries each allocator's decision for
    // this exact pair so the graph/edge list can render without recalculating anything.
    public sealed class CandidatePairViewModel
    {
        public required string VolunteerId { get; init; }
        public required string VolunteerName { get; init; }
        public required string CauseId { get; init; }
        public required string CauseName { get; init; }
        public required double CompatibilityScore { get; init; }

        // Whether this pair meets the minimum match threshold used for this comparison run.
        public required bool IsAboveThreshold { get; init; }

        // Whether this exact volunteer-cause pair was selected as a priority by each allocator.
        public required bool IsSelectedByMinCostMaxFlow { get; init; }
        public required bool IsSelectedByGreedy { get; init; }

        public double ScorePercent => Math.Round(CompatibilityScore * 100, 1);

        public string PriorityStatusLabel => (IsSelectedByMinCostMaxFlow, IsSelectedByGreedy) switch
        {
            (true, true) => "Priority in both algorithms",
            (true, false) => "Priority in Min-Cost Max-Flow only",
            (false, true) => "Priority in Greedy only",
            _ => IsAboveThreshold ? "Candidate only (not selected)" : "Below minimum threshold"
        };
    }

    // One selected priority recommendation for a single algorithm. This is a recommendation
    // priority only, not an actual registration or reserved place.
    public sealed class PriorityPairViewModel
    {
        public required string VolunteerId { get; init; }
        public required string VolunteerName { get; init; }
        public required string CauseId { get; init; }
        public required string CauseName { get; init; }
        public required double CompatibilityScore { get; init; }

        // True when the other algorithm did not select this exact volunteer-cause pair
        // (either a different cause for the same volunteer, or no priority at all).
        public bool DiffersFromOtherAlgorithm { get; init; }

        public double ScorePercent => Math.Round(CompatibilityScore * 100, 1);
    }

    // Summary of one allocator's run (Min-Cost Max-Flow or Greedy) for side-by-side comparison.
    public sealed class AlgorithmSummaryViewModel
    {
        public required string AlgorithmName { get; init; }
        public required string Description { get; init; }
        public required int PrioritizedVolunteerCount { get; init; }
        public required double TotalCompatibilityScore { get; init; }
        public required IReadOnlyList<PriorityPairViewModel> SelectedPriorities { get; init; }
    }

    // Full result of a single Compare() run, ready for display.
    public sealed class OptimizationResultViewModel
    {
        public required IReadOnlyList<CandidatePairViewModel> AllCandidates { get; init; }
        public required AlgorithmSummaryViewModel MinCostMaxFlow { get; init; }
        public required AlgorithmSummaryViewModel Greedy { get; init; }
        public required IReadOnlyDictionary<string, int> RemainingCapacityByCauseId { get; init; }

        public bool AlgorithmsSelectDifferentPriorities =>
            MinCostMaxFlow.SelectedPriorities.Any(p => p.DiffersFromOtherAlgorithm) ||
            Greedy.SelectedPriorities.Any(p => p.DiffersFromOtherAlgorithm);
    }

    public sealed class OptimizationViewModel
    {
        public required IReadOnlyList<VolunteerOptionViewModel> Volunteers { get; init; }
        public required IReadOnlyList<CauseCapacityInputViewModel> CauseCapacities { get; init; }
        public required double MinimumScore { get; init; }
        public required IReadOnlyList<ScenarioOptionViewModel> ScenarioOptions { get; init; }
        public required string SelectedScenario { get; init; }
        public required string ScoreSourceLabel { get; init; }
        public OptimizationResultViewModel? Result { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = [];

        // True when Min-Cost Max-Flow found a strictly better set of priorities (higher total
        // score, or the same score with more volunteers prioritized) than Greedy.
        public bool MinCostMaxFlowImprovesResult =>
            Result != null &&
            (Result.MinCostMaxFlow.TotalCompatibilityScore > Result.Greedy.TotalCompatibilityScore + 1e-9 ||
             (Math.Abs(Result.MinCostMaxFlow.TotalCompatibilityScore - Result.Greedy.TotalCompatibilityScore) <= 1e-9 &&
              Result.MinCostMaxFlow.PrioritizedVolunteerCount > Result.Greedy.PrioritizedVolunteerCount));
    }
}
