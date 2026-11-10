namespace Voluneta_Recommendation_Algorithm.Models
{
    // Dropdown option for selecting a demo volunteer.
    public sealed class VolunteerOptionViewModel
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
    }

    // A single ranked cause card shown on the recommendations page.
    public sealed class RankedCauseViewModel
    {
        public required string CauseId { get; init; }
        public required string CauseName { get; init; }
        public required string CategoryName { get; init; }
        public required DateTime StartDate { get; init; }
        public required int Rank { get; init; }
        public required double Score { get; init; }

        // Presentation-only fields, sourced from DemoDataSet, not used by scoring.
        public required string Place { get; init; }
        public required string Description { get; init; }
        public required string RequiredSkills { get; init; }
        public required int DifficultyLevel { get; init; }
        public required int RemainingPlaces { get; init; }
        public required int MaxVolunteers { get; init; }
        public required string ImagePath { get; init; }

        // ordered: InterestMatch, CategoryHistoryMatch, DayAvailableMatch, TimeAvailableMatch,
        // LocationMatch, SkillMatch, ExperienceMatch, ColleagueMatch
        public required double[] Features { get; init; }

        public double MatchScorePercent => Math.Round(Score * 100, 1);

        public string DifficultyLabel => DifficultyLevel switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };
    }

    // Predefined, selectable options shown in the profile-editing multi-selects/dropdown.
    // These must stay in sync with the demo data's comma-separated values and with the
    // category/skill names RecommendationFeatureCalculator understands.
    public static class DemoProfileOptions
    {
        public static readonly IReadOnlyList<string> Interests =
            ["Animals", "Environment", "Education", "Community", "Health"];

        public static readonly IReadOnlyList<string> Skills =
            ["First Aid", "Teamwork", "Teaching", "Communication", "Driving", "Physical Labor"];

        public static readonly IReadOnlyList<string> Days =
            ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

        public static readonly IReadOnlyList<string> TimesOfDay =
            ["Morning (08:00-12:00)", "Afternoon (12:00-17:00)", "Evening (17:00+)", "Flexible"];

        // "Original Location" means "keep the volunteer's own demo coordinates" (no override).
        public const string OriginalLocationLabel = "Original Location";

        public static readonly IReadOnlyList<string> Locations =
            [OriginalLocationLabel, "Sofia", "Plovdiv", "Varna"];

        // Fixed, accurate city-center coordinates (WGS84).
        public static readonly IReadOnlyDictionary<string, (double Latitude, double Longitude)> LocationCoordinates =
            new Dictionary<string, (double, double)>
            {
                ["Sofia"] = (42.6977, 23.3219),
                ["Plovdiv"] = (42.1354, 24.7453),
                ["Varna"] = (43.2141, 27.9147)
            };

        // "Unknown" means "keep the volunteer's own demo value" (no override).
        public const string UnknownExperienceLabel = "Unknown";

        public static readonly IReadOnlyList<string> ExperienceOptions =
            ["Yes", "No", UnknownExperienceLabel];
    }

    // Editable profile fields for the currently selected volunteer, prepopulated from
    // DemoDataSet and posted back by the form. Never written back to DemoDataSet.
    public sealed class VolunteerProfileEditViewModel
    {
        public required string VolunteerId { get; init; }
        public IReadOnlyList<string> SelectedInterests { get; init; } = [];
        public IReadOnlyList<string> SelectedSkills { get; init; } = [];
        public IReadOnlyList<string> SelectedDays { get; init; } = [];
        public string? PreferredTimeOfDay { get; init; }
        public string SelectedLocation { get; init; } = DemoProfileOptions.OriginalLocationLabel;
        public string SelectedExperience { get; init; } = DemoProfileOptions.UnknownExperienceLabel;

        // True when the posted values differ from the volunteer's original demo profile.
        public bool IsEdited { get; init; }
    }

    // A single cause's original-vs-edited comparison, matched by CauseId (never by rank
    // or list position). Only present after an edited-profile calculation.
    public sealed class CauseComparisonViewModel
    {
        public required string CauseId { get; init; }
        public required string CauseName { get; init; }
        public required string CategoryName { get; init; }

        public double? OriginalScore { get; init; }
        public int? OriginalRank { get; init; }
        public double[]? OriginalFeatures { get; init; }

        public double? EditedScore { get; init; }
        public int? EditedRank { get; init; }
        public double[]? EditedFeatures { get; init; }

        public bool WasEligibleBefore => OriginalScore.HasValue;
        public bool IsEligibleNow => EditedScore.HasValue;

        // Difference in percentage points (edited - original), null if not comparable
        // (e.g. cause was ineligible before or after the edit).
        public double? ScoreDifferencePercentPoints =>
            OriginalScore.HasValue && EditedScore.HasValue
                ? Math.Round((EditedScore.Value - OriginalScore.Value) * 100, 2)
                : null;

        public int? RankDifference =>
            OriginalRank.HasValue && EditedRank.HasValue
                ? OriginalRank.Value - EditedRank.Value
                : null;

        public string ScoreChangeLabel
        {
            get
            {
                var diff = ScoreDifferencePercentPoints;
                if (diff == null)
                {
                    return "N/A";
                }

                return diff.Value switch
                {
                    > 0 => $"+{diff.Value.ToString("0.00")} pp",
                    < 0 => $"{diff.Value.ToString("0.00")} pp",
                    _ => "0.00 pp"
                };
            }
        }

        public string RankChangeLabel
        {
            get
            {
                if (!WasEligibleBefore && IsEligibleNow)
                {
                    return "New";
                }
                if (WasEligibleBefore && !IsEligibleNow)
                {
                    return "No longer eligible";
                }

                var diff = RankDifference;
                if (diff == null)
                {
                    return "N/A";
                }

                return diff.Value switch
                {
                    > 0 => $"\u2191 Up {diff.Value}",
                    < 0 => $"\u2193 Down {Math.Abs(diff.Value)}",
                    _ => "No change"
                };
            }
        }
    }

    public sealed class RecommendationsViewModel
    {
        public required IReadOnlyList<VolunteerOptionViewModel> Volunteers { get; init; }
        public string? SelectedVolunteerId { get; init; }
        public IReadOnlyList<RankedCauseViewModel>? RankedCauses { get; init; }
        public VolunteerProfileEditViewModel? Profile { get; init; }

        // Only populated after a successful edited-profile calculation; hidden on reset.
        public IReadOnlyList<CauseComparisonViewModel>? Comparison { get; init; }
    }
}
