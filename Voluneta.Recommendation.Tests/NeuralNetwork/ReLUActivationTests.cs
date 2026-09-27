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

    [Fact]
    public void Backward_ShouldReturnCorrectInputs()
    {
        // Arrange
        var relu = new ReLUActivation();
        double[] inputs = { -1, 0, 1, 2, -3 };

        relu.Forward(inputs);

        double[] dValues = { 1, 1, 1, 1, 1 };
        double[] resDInputs = { 0, 0, 1, 1, 0 };

        // Act
        double[] dInputs = relu.Backward(dValues);

        // Assert
        Assert.Equal(resDInputs, dInputs);
    }
}