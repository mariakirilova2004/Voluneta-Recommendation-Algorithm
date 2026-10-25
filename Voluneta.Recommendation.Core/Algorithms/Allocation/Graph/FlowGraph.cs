namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Graph
{
    // Simple directed graph used for min-cost max-flow style allocation.
    // Nodes are just numbered 0..NodeCount-1, edges are stored as an
    // adjacency list (list of edges per node), like we learned in class.
    public class FlowGraph
    {
        private List<FlowEdge>[] _adjacency;

        public int NodeCount
        {
            get { return _adjacency.Length; }
        }

        public FlowGraph(int nodeCount)
        {
            if (nodeCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeCount));
            }

            _adjacency = new List<FlowEdge>[nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                _adjacency[i] = new List<FlowEdge>();
            }
        }

        public IReadOnlyList<FlowEdge> GetEdges(int node)
        {
            ValidateNode(node);
            return _adjacency[node].AsReadOnly();
        }

        public void AddEdge(int from, int to, int capacity, double cost)
        {
            ValidateNode(from);
            ValidateNode(to);

            if (from == to)
            {
                throw new ArgumentException("Self-loops are not supported.");
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (!double.IsFinite(cost))
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            // The forward edge is the "normal" edge, the reverse edge
            // with 0 capacity lets us cancel flow later on.
            var forward = new FlowEdge(to, _adjacency[to].Count, capacity, cost, false);
            var reverse = new FlowEdge(from, _adjacency[from].Count, 0, -cost, true);

            _adjacency[from].Add(forward);
            _adjacency[to].Add(reverse);
        }

        public void Augment(int from, int edgeIndex, int amount)
        {
            ValidateNode(from);

            if (edgeIndex < 0 || edgeIndex >= _adjacency[from].Count)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeIndex));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            var edge = _adjacency[from][edgeIndex];

            if (amount > edge.ResidualCapacity)
            {
                throw new InvalidOperationException("Insufficient residual capacity.");
            }

            var reverse = _adjacency[edge.To][edge.ReverseIndex];

            edge.DecreaseCapacity(amount);
            reverse.IncreaseCapacity(amount);
        }

        private void ValidateNode(int node)
        {
            if (node < 0 || node >= NodeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(node));
            }
        }
    }
}
