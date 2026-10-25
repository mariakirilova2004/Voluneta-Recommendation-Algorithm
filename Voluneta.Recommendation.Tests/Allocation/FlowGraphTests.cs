using Voluneta.Recommendation.Core.Algorithms.Allocation.Graph;

namespace Voluneta.Recommendation.Tests.Allocation
{
    public class FlowGraphTests
    {
        [Fact]
        public void AddEdge_CreatesForwardAndReverseEdges()
        {
            // Arrange
            var graph = new FlowGraph(2);

            // Act
            graph.AddEdge(0, 1, 1, 0.10);

            // Assert
            var forward = Assert.Single(graph.GetEdges(0));
            var reverse = Assert.Single(graph.GetEdges(1));
            Assert.Equal(1, forward.To);
            Assert.Equal(1, forward.ResidualCapacity);
            Assert.Equal(0.10, forward.Cost, 10);
            Assert.False(forward.IsReverse);
            Assert.Equal(0, reverse.To);
            Assert.Equal(0, reverse.ResidualCapacity);
            Assert.Equal(-0.10, reverse.Cost, 10);
            Assert.True(reverse.IsReverse);
        }

        [Fact]
        public void Augment_UpdatesForwardAndReverseCapacities()
        {
            // Arrange
            var graph = new FlowGraph(2);
            graph.AddEdge(0, 1, 1, 0.10);

            // Act
            graph.Augment(0, 0, 1);

            // Assert
            Assert.Equal(0, graph.GetEdges(0)[0].ResidualCapacity);
            Assert.Equal(1, graph.GetEdges(1)[0].ResidualCapacity);
        }

        [Fact]
        public void Augment_ReverseEdgeRestoresCapacity()
        {
            // Arrange
            var graph = new FlowGraph(2);
            graph.AddEdge(0, 1, 1, 0.10);
            graph.Augment(0, 0, 1);

            // Act
            graph.Augment(1, 0, 1);

            // Assert
            Assert.Equal(1, graph.GetEdges(0)[0].ResidualCapacity);
            Assert.Equal(0, graph.GetEdges(1)[0].ResidualCapacity);
        }

        [Fact]
        public void AddEdge_ReverseIndexPointsToCorrectEdge()
        {
            // Arrange
            var graph = new FlowGraph(3);
            graph.AddEdge(0, 1, 1, 0);
            graph.AddEdge(0, 2, 1, 0);
            graph.AddEdge(1, 2, 1, 0.40);

            // Act
            var edge = graph.GetEdges(1)[1];
            graph.Augment(1, 1, 1);

            // Assert
            Assert.Equal(1, edge.ReverseIndex);
            Assert.Equal(0, graph.GetEdges(1)[1].ResidualCapacity);
            Assert.Equal(1, graph.GetEdges(2)[1].ResidualCapacity);
        }

        [Fact]
        public void Augment_RejectsExceedingCapacity()
        {
            // Arrange
            var graph = new FlowGraph(2);
            graph.AddEdge(0, 1, 1, 0.10);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => graph.Augment(0, 0, 2));
        }

        [Fact]
        public void AddEdge_RejectsInvalidInputs()
        {
            // Arrange
            var graph = new FlowGraph(2);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => graph.AddEdge(0, 1, -1, 0.10));
            Assert.Throws<ArgumentOutOfRangeException>(() => graph.AddEdge(0, 1, 1, double.NaN));
            Assert.Throws<ArgumentException>(() => graph.AddEdge(0, 0, 1, 0.10));
        }
    }
}
