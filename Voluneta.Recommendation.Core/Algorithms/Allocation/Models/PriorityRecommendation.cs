namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // A selected priority recommendation for one volunteer-cause pair.
    // This does not reserve a place, it only marks the top suggestion.
    public class PriorityRecommendation
    {
        public string VolunteerId { get; set; }
        public string CauseId { get; set; }
        public double CompatibilityScore { get; set; }

        public PriorityRecommendation(string volunteerId, string causeId, double compatibilityScore)
        {
            VolunteerId = volunteerId;
            CauseId = causeId;
            CompatibilityScore = compatibilityScore;
        }
    }
}
