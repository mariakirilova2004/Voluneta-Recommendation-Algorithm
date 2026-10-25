namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // A possible volunteer-cause pair before we decide if it gets picked.
    public class AllocationCandidate
    {
        public string VolunteerId { get; set; }
        public string CauseId { get; set; }
        public double Score { get; set; }
        public bool IsEligible { get; set; }

        public AllocationCandidate(string volunteerId, string causeId, double score, bool isEligible)
        {
            VolunteerId = volunteerId;
            CauseId = causeId;
            Score = score;
            IsEligible = isEligible;
        }
    }
}
