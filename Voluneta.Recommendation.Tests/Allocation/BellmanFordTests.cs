using Voluneta.Recommendation.Core.Algorithms.Allocation.Graph;
using Voluneta.Recommendation.Core.Algorithms.Allocation.PathFinding;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class BellmanFordTests
    {
        [Fact]
        public void FindShortestPath_SelectsLowestCostPath()
        {
            // Arrange
            var graph = new FlowGraph(4);
            graph.AddEdge(0, 1, 1, 0);
            graph.AddEdge(1, 3, 1, 0.10);
            graph.AddEdge(0, 2, 1, 0);
            graph.AddEdge(2, 3, 1, 0.30);

            // Act
            var result = BellmanFordPathFinder.FindShortestPath(graph, 0, 3);

            // Assert
            Assert.True(result.Found);
            Assert.Equal(0.10, result.TotalCost, 6);
            var destinations = result.Steps.Select(step => graph.GetEdges(step.From)[step.EdgeIndex].To).ToArray();
            Assert.Equal(new[] { 1, 3 }, destinations);
        }

        [Fact]
        public void FindShortestPath_IgnoresZeroCapacityEdges()
        {
            // Arrange
            var graph = new FlowGraph(4);
            graph.AddEdge(0, 1, 0, 0.01);
            graph.AddEdge(1, 3, 1, 0.01);
            graph.AddEdge(0, 2, 1, 0.10);
            graph.AddEdge(2, 3, 1, 0.10);

            // Act
            var result = BellmanFordPathFinder.FindShortestPath(graph, 0, 3);

            // Assert
            Assert.True(result.Found);
            Assert.Equal(0.20, result.TotalCost, 6);
        }

        [Fact]
        public void FindShortestPath_ReturnsNotFoundWhenDisconnected()
        {
            // Arrange
            var graph = new FlowGraph(3);
            graph.AddEdge(0, 1, 1, 0.10);

            // Act
            var result = BellmanFordPathFinder.FindShortestPath(graph, 0, 2);

            // Assert
            Assert.False(result.Found);
            Assert.Empty(result.Steps);
            Assert.True(double.IsPositiveInfinity(result.TotalCost));
        }

        [Fact]
        public void FindShortestPath_UsesReverseEdgeForRerouting()
        {
            // Arrange
            // Nodes: 0 = Source, 1 = Maria, 2 = Ivan, 3 = Cause A, 4 = Cause B, 5 = Sink
            var graph = new FlowGraph(6);
            graph.AddEdge(0, 1, 1, 0);
            graph.AddEdge(0, 2, 1, 0);
            graph.AddEdge(1, 3, 1, 0.10);
            graph.AddEdge(1, 4, 1, 0.20);
            graph.AddEdge(2, 3, 1, 0.15);
            graph.AddEdge(3, 5, 1, 0);
            graph.AddEdge(4, 5, 1, 0);

            // Act
            var first = BellmanFordPathFinder.FindShortestPath(graph, 0, 5);
            foreach (var step in first.Steps)
            {
                graph.Augment(step.From, step.EdgeIndex, 1);
            }
            var second = BellmanFordPathFinder.FindShortestPath(graph, 0, 5);

            // Assert
            Assert.True(first.Found);
            Assert.Equal(0.10, first.TotalCost, 6);
            Assert.True(second.Found);
            Assert.Equal(0.25, second.TotalCost, 6);
            var destinations = second.Steps.Select(step => graph.GetEdges(step.From)[step.EdgeIndex].To).ToArray();
            Assert.Equal(new[] { 2, 3, 1, 4, 5 }, destinations);
        }
    }
}
