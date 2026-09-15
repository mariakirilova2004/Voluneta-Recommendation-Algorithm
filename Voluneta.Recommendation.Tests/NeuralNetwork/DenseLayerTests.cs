using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.NeuralNetwork;

public class DenseLayerTests
{
    [Fact]
    public void Forward_Testing_Results()
    {
        //Arrange
        var rand = new Random(42);

        var layer = new DenseLayer(3, 2, rand);

        layer.Weights[0, 0] = 0.4;
        layer.Weights[0, 1] = -0.2;
        layer.Weights[0, 2] = 0.7;

        layer.Weights[1, 0] = -0.5;
        layer.Weights[1, 1] = 0.6;
        layer.Weights[1, 2] = 0.2;

        layer.Biases[0] = 0.1;
        layer.Biases[1] = -0.05;

        double[] inputs = [0.8, 0.5, 1.0];

        //Act
        double[] outputs = layer.Forward(inputs);

        //Assert
        Assert.Equal(2, outputs.Length);
        Assert.Equal(0.4 * 0.8 + -0.2 * 0.5 + 0.7 * 1.0 + 0.1, outputs[0], 10);
        Assert.Equal(-0.5*0.8 + 0.6 * 0.5 + 0.2 * 1.0  + (-0.05), outputs[1], 10);
    }

    [Fact]
    public void Forward_WrongInput_Exception()
    {
        //Arrange
        var rand = new Random(42);

        var layer = new DenseLayer(3, 2, rand);

        // Wrong input length
        double[] inputs = [0.8, 0.5]; 

        //Act/Assert
        var exception = Assert.Throws<ArgumentException>(() => layer.Forward(inputs));
        Assert.Equal("Input length should be 3, but was 2", exception.Message);
    }
}