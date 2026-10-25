namespace Voluneta.Recommendation.Core.Algorithms.Allocation.PathFinding
{
    // One step of a path: go from node "From" using edge number "EdgeIndex".
    public class FlowPathStep
    {
        public int From { get; set; }
        public int EdgeIndex { get; set; }

        public FlowPathStep(int from, int edgeIndex)
        {
            From = from;
            EdgeIndex = edgeIndex;
        }
    }
}
