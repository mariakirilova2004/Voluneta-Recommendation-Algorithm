namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Represents a current registration of a volunteer for a cause (used to derive colleague overlap).
    public sealed class DemoRegistration
    {
        public required string VolunteerId { get; init; }
        public required string CauseId { get; init; }
    }
}
