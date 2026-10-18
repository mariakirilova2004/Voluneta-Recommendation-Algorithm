using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    // These tests check that the gradients our network calculates by hand (backpropagation)
    // are basically the same as the gradients we get if we just nudge each weight a tiny bit
    // and see how much the loss changes. This is called "gradient checking".
    public class FullNetworkGradientTests
    {
        private const double Epsilon = 1e-5;
        private const double Tolerance = 1e-5;

        [Fact]
        public void Backward_AllWeightGradients()
        {
            // Arrange - make a network and some fake input data
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();

            double[] inputs = new double[]
            {
                0.2, 0.5, 0.8, 0.3,
                0.7, 0.4, 0.6, 0.9
            };

            double target = 0.75;

            // Act - run forward then backward once to get the "real" gradients
            double prediction = network.Forward(inputs);
            double lossGradient = loss.Backward(prediction, target);
            network.Backward(lossGradient);

            // Go through every layer and compare the gradient we calculated
            // to the gradient we get from the slow/manual method.
            DenseLayer[] layers = new DenseLayer[] { network.Layer1, network.Layer2, network.OutputLayer };

            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                DenseLayer layer = layers[layerIndex];

                for (int n = 0; n < layer.NeuronN; n++)
                {
                    for (int i = 0; i < layer.InputN; i++)
                    {
                        double originalWeight = layer.Weights[n, i];

                        // Bump the weight up a little
                        layer.Weights[n, i] = originalWeight + Epsilon;
                        double lossPlus = CalculateLoss(network, inputs, target, loss);

                        // Bump the weight down a little
                        layer.Weights[n, i] = originalWeight - Epsilon;
                        double lossMinus = CalculateLoss(network, inputs, target, loss);

                        // Put the weight back the way it was
                        layer.Weights[n, i] = originalWeight;

                        // This is the slope of the loss at this weight (numerical gradient)
                        double numericalGradient = (lossPlus - lossMinus) / (2 * Epsilon);

                        // This is what backpropagation calculated for us
                        double analyticalGradient = layer.DWeights[n, i];

                        double difference = Math.Abs(numericalGradient - analyticalGradient);

                        string message = "Gradient mismatch at weight [" + n + "," + i + "]. " +
                            "Numerical: " + numericalGradient + ", Analytical: " + analyticalGradient;

                        Assert.True(difference < Tolerance, message);
                    }
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(42)]
        [InlineData(123)]
        [InlineData(2026)]
        public void Backward_MultipleSeeds_GradientsMatch(int seed)
        {
            // Try the same gradient check as above but with different random seeds,
            // so we are more confident it was not just a lucky result.
            var network = new RecommendationNeuralNetwork(new Random(seed));
            var loss = new MeanSquaredErrorLoss();
            var random = new Random(seed + 1000);

            // Build some random inputs between 0.1 and 0.9
            double[] inputs = new double[8];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = 0.1 + random.NextDouble() * 0.8;
            }

            double target = 0.75;

            double prediction = network.Forward(inputs);
            network.Backward(loss.Backward(prediction, target));

            DenseLayer[] layers = new DenseLayer[] { network.Layer1, network.Layer2, network.OutputLayer };

            int checkedParameters = 0;

            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                DenseLayer layer = layers[layerIndex];

                // Check every weight
                for (int n = 0; n < layer.NeuronN; n++)
                {
                    for (int i = 0; i < layer.InputN; i++)
                    {
                        double original = layer.Weights[n, i];

                        layer.Weights[n, i] = original + Epsilon;
                        double lossPlus = CalculateLoss(network, inputs, target, loss);

                        layer.Weights[n, i] = original - Epsilon;
                        double lossMinus = CalculateLoss(network, inputs, target, loss);

                        layer.Weights[n, i] = original;

                        double numerical = (lossPlus - lossMinus) / (2 * Epsilon);
                        double analytical = layer.DWeights[n, i];
                        double difference = Math.Abs(numerical - analytical);

                        string message = "Seed " + seed + ": weight [" + n + "," + i + "] mismatch. " +
                            "Numerical=" + numerical + ", Analytical=" + analytical;

                        Assert.True(difference < Tolerance, message);

                        checkedParameters++;
                    }
                }

                // Check every bias
                for (int n = 0; n < layer.NeuronN; n++)
                {
                    double original = layer.Biases[n];

                    layer.Biases[n] = original + Epsilon;
                    double lossPlus = CalculateLoss(network, inputs, target, loss);

                    layer.Biases[n] = original - Epsilon;
                    double lossMinus = CalculateLoss(network, inputs, target, loss);

                    layer.Biases[n] = original;

                    double numerical = (lossPlus - lossMinus) / (2 * Epsilon);
                    double analytical = layer.DBiases[n];
                    double difference = Math.Abs(numerical - analytical);

                    string message = "Seed " + seed + ": bias [" + n + "] mismatch. " +
                        "Numerical=" + numerical + ", Analytical=" + analytical;

                    Assert.True(difference < Tolerance, message);

                    checkedParameters++;
                }
            }

            // Just a sanity check that we actually looped through all the parameters we expect
            Assert.Equal(289, checkedParameters);
        }

        // Small helper so we don't repeat forward + loss calculation everywhere
        private static double CalculateLoss(RecommendationNeuralNetwork network, double[] inputs, double target, MeanSquaredErrorLoss loss)
        {
            double prediction = network.Forward(inputs);
            return loss.Forward(prediction, target);
        }
    }
}
