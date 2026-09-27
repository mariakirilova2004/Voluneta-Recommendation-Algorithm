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

    [Fact]
    public void Backward_SingleNeuron()
    {
        //Arrange
        var rand = new Random(42);
        var layer = new DenseLayer(2, 1, rand);
        layer.Weights[0, 0] = 0.5;
        layer.Weights[0, 1] = 0.2;
        layer.Biases[0] = 0.1;
        double[] inputs = {2, 3};
        double[] dvalues = { 0.4 };

        //Act
        layer.Forward(inputs);
        double[] dinputs = layer.Backward(dvalues);

        //Assert
        Assert.Equal(0.8, layer.DWeights[0,0], 6);
        Assert.Equal(1.2, layer.DWeights[0,1], 6);
        Assert.Equal(0.4, layer.DBiases[0], 6);
        Assert.Equal(0.2, dinputs[0], 6);
        Assert.Equal(0.08, dinputs[1], 6);
    }

    [Fact]
    public void Backwards_MultipleNeurons()
    {
        //Arrange
        var layer = new DenseLayer(2, 2, new Random(42));

        layer.Weights[0, 0] = 0.5;
        layer.Weights[0, 1] = 0.2;
        layer.Weights[1, 0] = 0.4;
        layer.Weights[1, 1] = 0.6;

        //Act
        layer.Forward([2, 3]);

        double[] dInputs = layer.Backward([0.4, 0.3]);

        //Assert
        Assert.Equal(0.32, dInputs[0], 6);
        Assert.Equal(0.26, dInputs[1], 6);
    }
}