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

    // cause categories can be phrased differently than the matching volunteer interest
    private static readonly Dictionary<string, string[]> CategoryInterestAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Environmental Work"] = new[] { "Environment & Nature", "Environment" },
        ["Animals"] = new[] { "Animal Welfare" },
        ["Social Impact"] = new[] { "Education & Youth", "Community Support", "Education", "Community" },
        ["Health Care"] = new[] { "Health & Elderly Care" }
    };

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

    // checks if the volunteer's interests contain the category name of the cause,
    // or one of the known aliases for that category
    private static double CalculateInterestMatch(RecommendationFeatureInput data)
    {
        HashSet<string> interests = ParseToSet(data.VolunteerInterests);

        if (interests.Count == 0 || string.IsNullOrWhiteSpace(data.CategoryName))
        {
            return UNKNOWN_VALUE;
        }

        string categoryName = data.CategoryName.Trim();

        if (interests.Contains(categoryName))
        {
            return 1;
        }

        if (CategoryInterestAliases.TryGetValue(categoryName, out string[]? aliases))
        {
            foreach (string alias in aliases)
            {
                if (interests.Contains(alias))
                {
                    return 1;
                }
            }

            return 0;
        }

        return UNKNOWN_VALUE;
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

    // the seven weekday names WeVolunteer actually allows volunteers to select
    private static readonly HashSet<string> RecognizedWeekdays = new(StringComparer.OrdinalIgnoreCase)
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };

    // checks if the day of the week the cause happens on is one of the days the volunteer said they are available
    private static double CalculateDayAvailableMatch(RecommendationFeatureInput data)
    {
        HashSet<string> days = ParseToSet(data.DaysAvailable);

        if (days.Count == 0)
        {
            return UNKNOWN_VALUE;
        }

        // only keep the recognized weekday names, ignoring any unexpected/garbage values
        HashSet<string> recognizedDays = new(StringComparer.OrdinalIgnoreCase);
        bool hasUnrecognizedValue = false;

        foreach (string day in days)
        {
            if (RecognizedWeekdays.Contains(day))
            {
                recognizedDays.Add(day);
            }
            else
            {
                hasUnrecognizedValue = true;
            }
        }

        if (recognizedDays.Count == 0)
        {
            return UNKNOWN_VALUE;
        }

        if (recognizedDays.Contains(data.CauseStart.DayOfWeek.ToString()))
        {
            return 1;
        }

        // a mix of recognized and unrecognized values is not a clear-cut mismatch, so stay neutral
        if (hasUnrecognizedValue)
        {
            return UNKNOWN_VALUE;
        }

        return 0;
    }

    // WeVolunteer stores the preferred time of day as a descriptive label, e.g.
    // "Morning (08:00-12:00)", "Afternoon (12:00-17:00)", "Evening (17:00+)" or "Flexible".
    // We only care about the leading keyword (Morning/Afternoon/Evening/Flexible); the
    // "(..)" time-range suffix is just a display hint and is ignored when matching.
    private static readonly string[] MorningLabels = { "Morning" };
    private static readonly string[] AfternoonLabels = { "Afternoon" };
    private static readonly string[] EveningLabels = { "Evening" };

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

        // figure out what part of the day the cause starts in, based on the thesis-defined
        // boundaries: Morning < 12:00, Afternoon 12:00-16:59, Evening >= 17:00
        int hour = data.CauseStart.Hour;
        string period;

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

        // labels can carry a trailing time-range, e.g. "Morning (08:00-12:00)", so we only
        // compare the leading keyword rather than requiring an exact match
        if (!TryGetTimePeriodKeyword(preference, out string? recognizedPeriod))
        {
            // unrecognized, non-empty preference - we can't know whether it matches, so stay neutral
            return UNKNOWN_VALUE;
        }

        return recognizedPeriod!.Equals(period, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    // extracts the leading Morning/Afternoon/Evening keyword from a preference label like
    // "Morning (08:00-12:00)", ignoring case and any trailing text
    private static bool TryGetTimePeriodKeyword(string preference, out string? keyword)
    {
        foreach (string label in MorningLabels)
        {
            if (preference.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                keyword = "Morning";
                return true;
            }
        }

        foreach (string label in AfternoonLabels)
        {
            if (preference.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                keyword = "Afternoon";
                return true;
            }
        }

        foreach (string label in EveningLabels)
        {
            if (preference.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                keyword = "Evening";
                return true;
            }
        }

        keyword = null;
        return false;
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

        // NaN and +/-Infinity must be rejected explicitly: comparisons like "Math.Abs(NaN) > 90"
        // are always false, so NaN would otherwise silently slip past a plain range check
        if (!double.IsFinite(lat1) || !double.IsFinite(lon1) || !double.IsFinite(lat2) || !double.IsFinite(lon2))
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Geographical coordinates must be finite numbers.");
        }

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

        // closer causes get a higher score, the further away it is, the score drops off exponentially;
        // distanceKm is always >= 0 so the result is always within (0,1], clamped defensively for safety
        return Math.Clamp(Math.Exp(-distanceKm / 20.0), 0, 1);
    }

    // Only explicit, unambiguous descriptions count as "no skills required".
    private static readonly HashSet<string> NoSkillRequirementPhrases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "none required",
            "no skills required",
            "no special skills required",
            "no specific skills required",
            "no experience required",
            "no experience needed",
            "no prior experience required",
            "no prior experience needed",
            "no special skills required. enthusiasm is enough"
        };

    // Comma-separated skill labels are supported. Free-form instructions must not
    // be misinterpreted as a reliable list of skill requirements.
    private static bool IsFreeTextSkillRequirement(string value)
    {
        return value.IndexOfAny(new[] { '.', '!', '?', ';', '\r', '\n' }) >= 0
            || value.StartsWith("Volunteers ", StringComparison.OrdinalIgnoreCase)
            || value.Contains(" should ", StringComparison.OrdinalIgnoreCase)
            || value.Contains(" must ", StringComparison.OrdinalIgnoreCase)
            || value.Contains(" need ", StringComparison.OrdinalIgnoreCase)
            || value.Contains(" and ", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(" required", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(" needed", StringComparison.OrdinalIgnoreCase);
    }

    // Compares explicitly listed skills without guessing from a volunteer's bio.
    private static double CalculateSkillMatch(RecommendationFeatureInput data)
    {
        if (string.IsNullOrWhiteSpace(data.RequiredSkills))
        {
            return 1.0;
        }

        string requirements = data.RequiredSkills.Trim().TrimEnd('.', '!', '?').Trim();

        if (NoSkillRequirementPhrases.Contains(requirements))
        {
            return 1.0;
        }

        HashSet<string> requiredSkills = ParseToSet(requirements);

        // Contradictory requirements such as "None required, First Aid" are unknown,
        // not proof that every volunteer is qualified.
        if (requiredSkills.Overlaps(NoSkillRequirementPhrases) ||
            IsFreeTextSkillRequirement(requirements))
        {
            return UNKNOWN_VALUE;
        }

        if (string.IsNullOrWhiteSpace(data.UserSkills))
        {
            return UNKNOWN_VALUE;
        }

        HashSet<string> userSkills = ParseToSet(data.UserSkills);
        int matchingSkillsCount = requiredSkills.Count(userSkills.Contains);

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
