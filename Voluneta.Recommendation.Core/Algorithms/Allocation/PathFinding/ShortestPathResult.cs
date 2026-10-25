namespace Voluneta.Recommendation.Core.Algorithms.Allocation.PathFinding
{
    // Holds the path we found (if any) and its total cost.
    public class ShortestPathResult
    {
        public IReadOnlyList<FlowPathStep> Steps { get; set; }
        public double TotalCost { get; set; }

        public ShortestPathResult(IReadOnlyList<FlowPathStep> steps, double totalCost)
        {
            Steps = steps;
            TotalCost = totalCost;
        }

        public bool Found
        {
            get { return Steps.Count > 0; }
        }
    }
}
