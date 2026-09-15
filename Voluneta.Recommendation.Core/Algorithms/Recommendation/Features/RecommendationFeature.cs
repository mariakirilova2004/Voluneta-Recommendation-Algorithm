namespace Recommendation.Core.Algorithms.Recommendation.Features;

public sealed class RecommendationFeatureVector
{
    public const int Count = 8;
    public double InterestMatch { get; init; }

    public double CategoryHistoryMatch { get; init; }

    public double DayAvailableMatch { get; init; }

    public double TimeAvailableMatch { get; init; }

    public double LocationMatch { get; init; }
    public double SkillMatch { get; init; }

    public double ExperienceMatch { get; init; }

    public double ColleagueMatch { get; init; }

    public double[] ToArray()
    {
        return new double[]{
            InterestMatch,
            CategoryHistoryMatch,
            DayAvailableMatch,
            TimeAvailableMatch,
            LocationMatch,
            SkillMatch,
            ExperienceMatch,
            ColleagueMatch
        };
    }

}