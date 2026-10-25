namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // Final output of an allocation run.
    public class AllocationResult
    {
        public IReadOnlyList<AllocationAssignment> Assignments { get; set; }

        public AllocationResult(IReadOnlyList<AllocationAssignment> assignments)
        {
            Assignments = assignments;
        }

        public int AllocatedCount
        {
            get { return Assignments.Count; }
        }

        public double TotalScore
        {
            get
            {
                double total = 0;
                foreach (var a in Assignments)
                {
                    total += a.Score;
                }
                return total;
            }
        }
    }
}
