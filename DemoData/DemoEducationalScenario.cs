namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Explicitly constructed, hand-authored compatibility scores and default capacities for the
    // "Greedy vs Optimal" educational scenario. Uses the SAME 4 volunteer IDs and 3 cause IDs as
    // DemoDataSet so names/UI stay consistent, but the scores below are NOT neural network
    // predictions - they are deliberately chosen to make the Greedy vs Min-Cost Max-Flow
    // trade-off easy to see and explain. Never mutates DemoDataSet.
    public static class DemoEducationalScenario
    {
        // volunteerId -> (causeId -> compatibilityScore)
        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> CompatibilityScores { get; } =
            new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal)
            {
                ["V1"] = new Dictionary<string, double>(StringComparer.Ordinal) { ["C1"] = 0.95, ["C2"] = 0.92, ["C3"] = 0.10 },
                ["V2"] = new Dictionary<string, double>(StringComparer.Ordinal) { ["C1"] = 0.94, ["C2"] = 0.10, ["C3"] = 0.10 },
                ["V3"] = new Dictionary<string, double>(StringComparer.Ordinal) { ["C1"] = 0.10, ["C2"] = 0.10, ["C3"] = 0.90 },
                ["V4"] = new Dictionary<string, double>(StringComparer.Ordinal) { ["C1"] = 0.10, ["C2"] = 0.10, ["C3"] = 0.88 }
            };

        public static IReadOnlyDictionary<string, int> DefaultRemainingPlaces { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["C1"] = 1,
                ["C2"] = 1,
                ["C3"] = 2
            };

        public const double DefaultMinimumScore = 0.50;
    }
}
