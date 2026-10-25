namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // How many volunteers a cause can still take.
    public class CauseCapacity
    {
        public string CauseId { get; set; }
        public int AvailablePlaces { get; set; }

        public CauseCapacity(string causeId, int availablePlaces)
        {
            CauseId = causeId;
            AvailablePlaces = availablePlaces;
        }
    }
}
