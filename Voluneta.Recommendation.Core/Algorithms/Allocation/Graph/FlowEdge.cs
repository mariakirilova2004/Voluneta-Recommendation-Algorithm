namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Graph
{
    // One edge in the flow graph. Every real edge has a matching
    // "reverse" edge used to undo flow when we augment paths.
    public class FlowEdge
    {
        public int To;
        public int ReverseIndex;
        public int ResidualCapacity;
        public int OriginalCapacity;
        public double Cost;
        public bool IsReverse;

        public FlowEdge(int to, int reverseIndex, int capacity, double cost, bool isReverse)
        {
            To = to;
            ReverseIndex = reverseIndex;
            ResidualCapacity = capacity;
            OriginalCapacity = capacity;
            Cost = cost;
            IsReverse = isReverse;
        }

        public void DecreaseCapacity(int amount)
        {
            ResidualCapacity = ResidualCapacity - amount;
        }

        public void IncreaseCapacity(int amount)
        {
            ResidualCapacity = ResidualCapacity + amount;
        }
    }
}
