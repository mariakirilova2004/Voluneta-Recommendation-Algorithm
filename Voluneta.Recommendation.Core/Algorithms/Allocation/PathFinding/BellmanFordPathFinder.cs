using Voluneta.Recommendation.Core.Algorithms.Allocation.Graph;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.PathFinding
{
    // Bellman-Ford shortest path over the residual graph. We use this
    // instead of Dijkstra because edge costs can become negative
    // (the reverse edges used to "undo" flow have negative cost).
    public static class BellmanFordPathFinder
    {
        public static ShortestPathResult FindShortestPath(FlowGraph graph, int source, int sink)
        {
            ArgumentNullException.ThrowIfNull(graph);

            int n = graph.NodeCount;

            if (source < 0 || source >= n)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            if (sink < 0 || sink >= n)
            {
                throw new ArgumentOutOfRangeException(nameof(sink));
            }

            if (source == sink)
            {
                throw new ArgumentException("Source and sink must be different.");
            }

            // Step 1: Initialize distances.
            var distance = new double[n];
            var previousNode = new int[n];
            var previousEdge = new int[n];

            for (int i = 0; i < n; i++)
            {
                distance[i] = double.PositiveInfinity;
                previousNode[i] = -1;
                previousEdge[i] = -1;
            }

            distance[source] = 0;

            // Step 2: Relax all residual edges, n-1 times.
            for (int iteration = 0; iteration < n - 1; iteration++)
            {
                bool updated = false;

                for (int from = 0; from < n; from++)
                {
                    if (double.IsPositiveInfinity(distance[from]))
                    {
                        continue;
                    }

                    var edges = graph.GetEdges(from);

                    for (int i = 0; i < edges.Count; i++)
                    {
                        var edge = edges[i];

                        if (edge.ResidualCapacity <= 0)
                        {
                            continue;
                        }

                        double newDistance = distance[from] + edge.Cost;

                        if (newDistance < distance[edge.To])
                        {
                            distance[edge.To] = newDistance;
                            previousNode[edge.To] = from;
                            previousEdge[edge.To] = i;
                            updated = true;
                        }
                    }
                }

                if (!updated)
                {
                    break;
                }
            }

            // Step 3: Detect reachable negative-cost cycles.
            for (int from = 0; from < n; from++)
            {
                if (double.IsPositiveInfinity(distance[from]))
                {
                    continue;
                }

                foreach (var edge in graph.GetEdges(from))
                {
                    if (edge.ResidualCapacity <= 0)
                    {
                        continue;
                    }

                    if (distance[from] + edge.Cost < distance[edge.To])
                    {
                        throw new InvalidOperationException("Reachable negative-cost cycle detected.");
                    }
                }
            }

            // Step 4: Check whether sink is reachable.
            if (double.IsPositiveInfinity(distance[sink]))
            {
                return new ShortestPathResult(Array.Empty<FlowPathStep>(), double.PositiveInfinity);
            }

            // Step 5: Reconstruct the shortest path by walking backwards from sink.
            var path = new List<FlowPathStep>();
            int current = sink;

            while (current != source)
            {
                int from = previousNode[current];
                int edgeIndex = previousEdge[current];

                if (from == -1 || edgeIndex == -1)
                {
                    throw new InvalidOperationException("Unable to reconstruct shortest path.");
                }

                path.Add(new FlowPathStep(from, edgeIndex));
                current = from;
            }

            path.Reverse();

            // Step 6: Return the path and its cost.
            return new ShortestPathResult(path.AsReadOnly(), distance[sink]);
        }
    }
}
