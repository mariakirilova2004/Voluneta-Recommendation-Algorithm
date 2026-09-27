using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    public class SGDTests
    {
        [Fact]
        public void Update_ShouldUpdate()
        {
            // Arrange
            var layer = new DenseLayer(1,1,new Random(42));
            layer.Weights[0, 0] = 0.5;
            layer.Biases[0] = 0.1;
            layer.Forward(new double[] {2});
            layer.Backward(new double[] {0.4});
            var sgd = new SGD(learningRate: 0.01);

            // Act
            sgd.Update(layer);

            // Assert
            Assert.Equal(0.492, layer.Weights[0, 0], precision: 3);
            Assert.Equal(0.096, layer.Biases[0], precision: 3);
        }
    }
}
