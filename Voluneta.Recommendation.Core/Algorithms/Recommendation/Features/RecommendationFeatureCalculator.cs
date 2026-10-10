namespace Recommendation.Core.Algorithms.Recommendation.Features;

public sealed record CompletedCause(int CategoryId);
public sealed record ColleagueParticipant(string UserId, int? CompanyId);

// This class just holds all the input data we need to calculate the features for the recommendation algorithm
public sealed class RecommendationFeatureInput
{
    public string? VolunteerInterests { get; init; }
    public string? CategoryName { get; init; }
    public int CategoryId { get; init; }
    public IReadOnlyCollection<CompletedCause>? CompletedCauses { get; init; }
    public string? DaysAvailable { get; init; }
    public string? PreferredTimeOfDay { get; init; }
    public DateTime CauseStart { get; init; }
    public double? UserLatitude { get; init; }
    public double? UserLongitude { get; init; }
    public double? CauseLatitude { get; init; }
    public double? CauseLongitude { get; init; }
    public string? UserSkills { get; init; }
    public string? RequiredSkills { get; init; }
    public bool? HasPreviousExperience { get; init; }
    public int DifficultyLevel { get; init; } // 0 = Easy, 1 = Medium, 2 = Hard
    public string? UserId { get; init; }
    public int? CompanyId { get; init; }
    public IReadOnlyCollection<ColleagueParticipant>? RegisteredParticipants { get; init; }
}

// Calculates all the features that we need for the recommendation algorithm (like a score for each thing we compare)
public static class RecommendationFeatureCalculator
{
    // when we don't have enough information to know, we just put 0.5 as a "neutral" value
    private const double UNKNOWN_VALUE = 0.5;

    // helper function that takes a string like "a, b, c" and turns it into a set of words, so it is easier to compare later
    private static HashSet<string> ParseToSet(string? text)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        string[] parts = text.Split(',');

        foreach (string part in parts)
        {
            string trimmedPart = part.Trim();

            if (trimmedPart.Length > 0)
            {
                result.Add(trimmedPart);
            }
        }

        return result;
    }

    public static RecommendationFeatureVector Calculate(RecommendationFeatureInput data)
    {
        // just making sure we didn't get null, otherwise everything below would crash
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        RecommendationFeatureVector featureVector = new RecommendationFeatureVector
        {
            InterestMatch = CalculateInterestMatch(data),
            CategoryHistoryMatch = CalculateCategoryHistoryMatch(data),
            DayAvailableMatch = CalculateDayAvailableMatch(data),
            TimeAvailableMatch = CalculateTimeAvailableMatch(data),
            LocationMatch = CalculateLocationMatch(data),
            SkillMatch = CalculateSkillMatch(data),
            ExperienceMatch = CalculateExperienceMatch(data),
            ColleagueMatch = CalculateColleagueMatch(data)
        };

        return featureVector;
    }

    // checks if the volunteer's interests contain the category name of the cause
    private static double CalculateInterestMatch(RecommendationFeatureInput data)
    {
        HashSet<string> interests = ParseToSet(data.VolunteerInterests);

        if (interests.Count == 0 || string.IsNullOrWhiteSpace(data.CategoryName))
        {
            return UNKNOWN_VALUE;
        }

        if (interests.Contains(data.CategoryName.Trim()))
        {
            return 1;
        }

        return 0;
    }

    // looks at the causes the user already completed and sees what percentage of them were in the same category
    private static double CalculateCategoryHistoryMatch(RecommendationFeatureInput data)
    {
        if (data.CompletedCauses == null || data.CompletedCauses.Count == 0)
        {
            return UNKNOWN_VALUE;
        }

        int matchingCount = 0;

        foreach (CompletedCause cause in data.CompletedCauses)
        {
            if (cause.CategoryId == data.CategoryId)
            {
                matchingCount++;
            }
        }

        return (double)matchingCount / data.CompletedCauses.Count;
    }

    // checks if the day of the week the cause happens on is one of the days the volunteer said they are available
    private static double CalculateDayAvailableMatch(RecommendationFeatureInput data)
    {
        HashSet<string> days = ParseToSet(data.DaysAvailable);

        if (days.Count == 0)
        {
            return UNKNOWN_VALUE;
        }

        if (days.Contains(data.CauseStart.DayOfWeek.ToString()))
        {
            return 1;
        }

        return 0;
    }

    // checks if the time of day matches what the volunteer prefers (morning/afternoon/evening or flexible)
    private static double CalculateTimeAvailableMatch(RecommendationFeatureInput data)
    {
        if (string.IsNullOrWhiteSpace(data.PreferredTimeOfDay))
        {
            return UNKNOWN_VALUE;
        }

        string preference = data.PreferredTimeOfDay.Trim();

        if (preference.Equals("Flexible", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        int hour = data.CauseStart.Hour;
        string period;

        // figure out what part of the day the cause starts in
        if (hour < 12)
        {
            period = "Morning";
        }
        else if (hour < 17)
        {
            period = "Afternoon";
        }
        else
        {
            period = "Evening";
        }

        if (preference.Equals(period, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }

    // uses the haversine formula to figure out how far the user is from the cause, then turns that distance into a score
    private static double CalculateLocationMatch(RecommendationFeatureInput data)
    {
        if (data.UserLatitude == null || data.UserLongitude == null || data.CauseLatitude == null || data.CauseLongitude == null)
        {
            return UNKNOWN_VALUE;
        }

        double lat1 = data.UserLatitude.Value;
        double lon1 = data.UserLongitude.Value;
        double lat2 = data.CauseLatitude.Value;
        double lon2 = data.CauseLongitude.Value;

        if (Math.Abs(lat1) > 90 || Math.Abs(lat2) > 90 || Math.Abs(lon1) > 180 || Math.Abs(lon2) > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Invalid geographical coordinates.");
        }

        double radians = Math.PI / 180.0;

        // this is the haversine formula from my notes, used to calculate distance between two points on a sphere (the earth)
        double deltaLatRad = (lat2 - lat1) * radians;
        double deltaLonRad = (lon2 - lon1) * radians;

        double a = Math.Pow(Math.Sin(deltaLatRad / 2), 2) + Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin(deltaLonRad / 2), 2);

        double earthRadiusKm = 6371.0;
        double distanceKm = earthRadiusKm * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));

        // closer causes get a higher score, the further away it is, the score drops off exponentially
        return Math.Exp(-distanceKm / 20.0);
    }

    // compares the skills the user has with the skills the cause requires
    private static double CalculateSkillMatch(RecommendationFeatureInput data)
    {
        HashSet<string> requiredSkills = ParseToSet(data.RequiredSkills);

        if (requiredSkills.Count == 0 || requiredSkills.Contains("none required"))
        {
            return 1;
        }

        if (string.IsNullOrWhiteSpace(data.UserSkills))
        {
            return UNKNOWN_VALUE;
        }

        HashSet<string> userSkills = ParseToSet(data.UserSkills);

        int matchingSkillsCount = 0;

        foreach (string skill in requiredSkills)
        {
            if (userSkills.Contains(skill))
            {
                matchingSkillsCount++;
            }
        }

        return (double)matchingSkillsCount / requiredSkills.Count;
    }

    // checks how well the difficulty of the cause matches with how much experience the volunteer has
    private static double CalculateExperienceMatch(RecommendationFeatureInput data)
    {
        if (data.DifficultyLevel < 0 || data.DifficultyLevel > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(data.DifficultyLevel));
        }

        // easy causes are fine for everyone, no experience needed
        if (data.DifficultyLevel == 0)
        {
            return 1;
        }

        if (data.HasPreviousExperience == null)
        {
            return UNKNOWN_VALUE;
        }

        if (data.HasPreviousExperience.Value)
        {
            return 1;
        }

        // no experience + medium difficulty = half score, no experience + hard difficulty = zero score
        if (data.DifficultyLevel == 1)
        {
            return 0.5;
        }

        return 0;
    }

    // checks how many of the volunteer's colleagues (same company) already signed up for this cause
    private static double CalculateColleagueMatch(RecommendationFeatureInput data)
    {
        if (data.CompanyId == null || data.RegisteredParticipants == null)
        {
            return UNKNOWN_VALUE;
        }

        List<ColleagueParticipant> otherParticipants = new List<ColleagueParticipant>();

        foreach (ColleagueParticipant participant in data.RegisteredParticipants)
        {
            if (participant.UserId != data.UserId)
            {
                otherParticipants.Add(participant);
            }
        }

        if (otherParticipants.Count == 0)
        {
            return 0;
        }

        int colleaguesCount = 0;

        foreach (ColleagueParticipant participant in otherParticipants)
        {
            if (participant.CompanyId == data.CompanyId)
            {
                colleaguesCount++;
            }
        }

        return (double)colleaguesCount / otherParticipants.Count;
    }
}
