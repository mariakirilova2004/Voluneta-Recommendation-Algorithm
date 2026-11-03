namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // How many places a cause currently has remaining
    public class CauseCapacity
    {
        public string CauseId { get; set; }
        public int RemainingPlaces { get; set; }

        public CauseCapacity(string causeId, int remainingPlaces)
        {
            CauseId = causeId;
            RemainingPlaces = remainingPlaces;
        }
    }
}
