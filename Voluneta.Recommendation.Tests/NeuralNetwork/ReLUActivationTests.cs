using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

namespace Voluneta.Recommendation.Tests.NeuralNetwork;

public class ReLUActivationTests
{
    [Fact]
    public void Forward_ShouldReturnCorrectOutputs()
    {
        // Arrange
        var relu = new ReLUActivation();
        double[] inputs = { -1.0, 0.0, 1.0, 2.0, -3.0 };
        double[] resOutputs = { 0.0, 0.0, 1.0, 2.0, 0.0 };

        // Act
        double[] outputs = relu.Forward(inputs);

        // Assert
        Assert.Equal(resOutputs, outputs);
    }
}