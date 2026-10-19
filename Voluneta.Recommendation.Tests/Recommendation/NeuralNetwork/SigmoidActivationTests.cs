namespace Voluneta.Recommendation.Tests.Recommendation.NeuralNetwork
{
    public class SigmoidActivationTests
    {
        [Fact]
        public void Forward_ShouldReturnCorrectOutput()
        {
            // Arrange
            var activation = new Core.Algorithms.Recommendation.NeuralNetwork.SigmoidActivation();
            double[] inputs = { -1.0, 0.0, 1.0 };
            
            // Act
            double[] output = activation.Forward(inputs);
            
            // Assert
            Assert.Equal(3, output.Length);
            Assert.Equal(0.2689414213699951, output[0], 10);
            Assert.Equal(0.5, output[1], 10);
            Assert.Equal(0.7310585786300049, output[2], 10);
        }

        [Fact]
        public void Backward_ShouldReturnCorrectGradients()
        {
            // Arrange
            var activation = new Core.Algorithms.Recommendation.NeuralNetwork.SigmoidActivation();
            double[] inputs = {-1, 0, 1};
            activation.Forward(inputs);
            double[] dValues = {1, 1, 1};

            // Act
            double[] dInputs = activation.Backward(dValues);

            // Assert
            Assert.Equal(3, dInputs.Length);
            Assert.Equal(0.19661193324148185, dInputs[0], 10);
            Assert.Equal(0.25, dInputs[1], 10);
            Assert.Equal(0.19661193324148185, dInputs[2], 10);
        }
    }
}
