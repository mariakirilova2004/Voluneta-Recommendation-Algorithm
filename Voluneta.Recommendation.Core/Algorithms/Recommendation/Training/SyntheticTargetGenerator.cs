using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public static class SyntheticTargetGenerator
    {
        public static double CalculateScore(RecommendationFeatureVector features)
        {
            ArgumentNullException.ThrowIfNull(features);

            double score =
                0.10 * features.CategoryHistoryMatch +
                0.10 * features.DayAvailableMatch +
                0.10 * features.TimeAvailableMatch +
                0.15 * features.LocationMatch +
                0.15 * features.SkillMatch +
                0.10 * features.ExperienceMatch +
                0.10 * features.ColleagueMatch +
                0.20 * (features.InterestMatch * features.LocationMatch);

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
