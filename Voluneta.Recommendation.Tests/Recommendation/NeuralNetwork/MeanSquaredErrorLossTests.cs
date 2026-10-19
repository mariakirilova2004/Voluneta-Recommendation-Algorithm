using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

namespace Voluneta.Recommendation.Tests.Recommendation.NeuralNetwork
{
    public class MeanSquaredErrorLossTests
    {
        [Fact]
        public void Forward_ShouldReturnCorrectLoss()
        {
            // Arrange
            var loss = new MeanSquaredErrorLoss();
            double actual = 0.8;
            double expected = 1.0;

            // Act
            double result = loss.Forward(actual, expected);

            // Assert
            Assert.Equal(0.04, result, 2);
        }

        [Fact]
        public void Backward_ShouldReturnCorrectGradient()
        {
            // Arrange
            var loss = new MeanSquaredErrorLoss();
            double actual = 0.8;
            double expected = 1.0;

            // Act
            double result = loss.Backward(actual, expected);

            // Assert
            Assert.Equal(-0.4, result, 2);
        }
    }
}
