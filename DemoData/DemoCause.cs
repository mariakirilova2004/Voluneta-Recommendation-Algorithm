namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // A fictional cause/opportunity used to exercise the recommendation pipeline without a database.
    public sealed class DemoCause
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string CategoryName { get; init; }
        public required int CategoryId { get; init; }

        public required DateTime StartDate { get; init; }

        public required double Latitude { get; init; }
        public required double Longitude { get; init; }

        // comma-separated, predefined skill labels required for this cause
        public required string RequiredSkills { get; init; }

        public required int DifficultyLevel { get; init; } // 0 = Easy, 1 = Medium, 2 = Hard

        // places still open for registration (not total capacity)
        public required int RemainingPlaces { get; init; }

        // Presentation-only fields (not used by scoring): a human place name, a short
        // description, and total capacity, plus a local demo image path.
        public required string Place { get; init; }
        public required string Description { get; init; }
        public required int MaxVolunteers { get; init; }
        public required string ImagePath { get; init; }
    }
}
