namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // A possible volunteer-cause pair before we decide if it gets a priority recommendation.
    public class RecommendationCandidate
    {
        public string VolunteerId { get; set; }
        public string CauseId { get; set; }
        public double CompatibilityScore { get; set; }
        public bool IsEligible { get; set; }

        public RecommendationCandidate(string volunteerId, string causeId, double compatibilityScore, bool isEligible)
        {
            VolunteerId = volunteerId;
            CauseId = causeId;
            CompatibilityScore = compatibilityScore;
            IsEligible = isEligible;
        }
    }
}
