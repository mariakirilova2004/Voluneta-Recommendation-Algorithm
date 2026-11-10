namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // A fictional volunteer profile used to exercise the recommendation pipeline without a database.
    public sealed class DemoVolunteer
    {
        public required string Id { get; init; }
        public required string Name { get; init; }

        // comma-separated, predefined volunteer interest labels (matches WeVolunteer's multi-select format)
        public required string Interests { get; init; }

        // comma-separated weekday names, e.g. "Monday,Wednesday,Friday"
        public required string DaysAvailable { get; init; }

        // one of: "Morning (08:00-12:00)", "Afternoon (12:00-17:00)", "Evening (17:00+)", "Flexible"
        public required string PreferredTimeOfDay { get; init; }

        // comma-separated, predefined skill labels
        public required string Skills { get; init; }

        public required bool HasPreviousExperience { get; init; }

        public required int CompanyId { get; init; }

        public required double Latitude { get; init; }
        public required double Longitude { get; init; }

        // category ids of causes this volunteer has verified-completed in the past
        // (VolunteerFeedback.Attended == true AND Cause.Status == Done, deduplicated by CauseId)
        public IReadOnlyCollection<int> CompletedCauseCategoryIds { get; init; } = [];
    }
}
