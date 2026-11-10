namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // A single volunteer-cause compatibility result produced by Algorithm 1 (neural network scoring)
    // for the local demo. This has no notion of capacity or allocation priority - it is a pure
    // per-volunteer ranking of all eligible causes by predicted compatibility score.
    public sealed class DemoRecommendationResult
    {
        public required string VolunteerId { get; init; }
        public required string CauseId { get; init; }

        // ordered: InterestMatch, CategoryHistoryMatch, DayAvailableMatch, TimeAvailableMatch,
        // LocationMatch, SkillMatch, ExperienceMatch, ColleagueMatch
        public required double[] Features { get; init; }

        public required double Score { get; init; }

        // 1-based rank of this cause among the volunteer's eligible causes, by descending score
        public required int Rank { get; init; }
    }
}
