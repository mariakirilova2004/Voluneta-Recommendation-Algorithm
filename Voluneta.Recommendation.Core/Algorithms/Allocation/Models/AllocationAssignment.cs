namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // Represents one volunteer being matched to one cause.
    public class AllocationAssignment
    {
        public string VolunteerId { get; set; }
        public string CauseId { get; set; }
        public double Score { get; set; }

        public AllocationAssignment(string volunteerId, string causeId, double score)
        {
            VolunteerId = volunteerId;
            CauseId = causeId;
            Score = score;
        }
    }
}
