namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{ 
    public class RankedRecommendation
    {
        public string VolunteerId { get; set; }
        public string CauseId { get; set; }
        public double CompatibilityScore { get; set; }
        public int Rank { get; set; }
        public bool IsPriority { get; set; }

        public RankedRecommendation(string volunteerId, string causeId, double compatibilityScore, int rank, bool isPriority)
        {
            VolunteerId = volunteerId;
            CauseId = causeId;
            CompatibilityScore = compatibilityScore;
            Rank = rank;
            IsPriority = isPriority;
        }
    }

}
