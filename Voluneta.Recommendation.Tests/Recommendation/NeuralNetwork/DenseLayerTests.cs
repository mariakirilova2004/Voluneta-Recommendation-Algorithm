using System.Runtime.CompilerServices;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.Recommendation.NeuralNetwork;

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

    [Fact]
    public void Test_Seeding()
    {
        //Arrange
        var layer1 = new DenseLayer(3, 2, new Random(42));
        var layer2 = new DenseLayer(3, 2, new Random(42));

        //Assert
        for(int n = 0; n < layer1.NeuronN; n++)
        {
            for (int i = 0; i < layer1.InputN; i++)
            {
                Assert.Equal(layer1.Weights[n, i], layer2.Weights[n, i]);
            }
            Assert.Equal(layer1.Biases[n], layer2.Biases[n]);
        }
    }

    [Fact]
    public void Test_Weight_Gradient_Match()
    {
        //Arrange
        var layer = new DenseLayer(2,1, new Random(42));
        double[] input = { 0.4, 0.7 };
        double[] upstreamGradient = { 0.8 };

        double epsilon = 1e-5;

        //Act
        layer.Forward(input);
        layer.Backward(upstreamGradient);

        double originalWeight = layer.Weights[0, 0];

        layer.Weights[0, 0] = originalWeight + epsilon;

        double outputPlus = layer.Forward(input)[0];
        double lossPlus = outputPlus * upstreamGradient[0];

        layer.Weights[0, 0] = originalWeight - epsilon;

        double outputMinus = layer.Forward(input)[0];
        double lossMinus = outputMinus * upstreamGradient[0];

        double numericalGradient = (lossPlus - lossMinus) / (2 * epsilon);

        //Assert
        Assert.Equal(numericalGradient, layer.DWeights[0, 0], 5);
    }

    [Fact]
    public void Test_Bias_Gradient_Match()
    {
        //Arrange
        var layer = new DenseLayer(2, 1, new Random(42));
        double[] input = { 0.4, 0.7 };
        double[] upstreamGradient = { 0.8 };

        double epsilon = 1e-5;

        //Act
        layer.Forward(input);
        layer.Backward(upstreamGradient);

        double originalBias = layer.Biases[0];

        layer.Biases[0] = originalBias + epsilon;

        double outputPlus = layer.Forward(input)[0];
        double lossPlus = outputPlus * upstreamGradient[0];

        layer.Biases[0] = originalBias - epsilon;

        double outputMinus = layer.Forward(input)[0];
        double lossMinus = outputMinus * upstreamGradient[0];

        double numericalGradient = (lossPlus - lossMinus) / (2 * epsilon);

        //Assert
        Assert.Equal(numericalGradient, layer.DBiases[0], 5);
    }
}