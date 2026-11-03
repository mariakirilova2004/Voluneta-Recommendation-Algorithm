namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Models
{
    // Final output of a priority recommendation run.
    public class RecommendationPriorityResult
    {
        public IReadOnlyList<PriorityRecommendation> Priorities { get; set; }

        public RecommendationPriorityResult(IReadOnlyList<PriorityRecommendation> priorities)
        {
            Priorities = priorities;
        }

        public int PrioritizedCount
        {
            get { return Priorities.Count; }
        }

        public double TotalScore
        {
            get
            {
                double total = 0;
                foreach (var p in Priorities)
                {
                    total += p.CompatibilityScore;
                }
                return total;
            }
        }
    }
}
