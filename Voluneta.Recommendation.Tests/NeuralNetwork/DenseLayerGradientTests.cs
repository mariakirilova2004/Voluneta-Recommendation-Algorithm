using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    public class DenseLayerGradientTests
    {
        [Fact]
        public void Backward_WeightGradients()
        {
            // Arrange
            var layer = new DenseLayer(3, 2, new Random(42));

            double[] inputs = { 0.2, 0.5, 0.8 };
            double[] targets = { 0.3, 0.7 };

            var loss = new MeanSquaredErrorLoss();

            const double epsilon = 1e-5;
            const double tolerance = 1e-6;

            // Calculate analytical gradients
            double[] predictions = layer.Forward(inputs);

            double[] dvalues = new double[predictions.Length];

            for (int n = 0; n < predictions.Length; n++)
            {
                dvalues[n] = loss.Backward(predictions[n], targets[n]);
            }

            layer.Backward(dvalues);

            // Check every weight
            for (int n = 0; n < layer.NeuronN; n++)
            {
                for (int i = 0; i < layer.InputN; i++)
                {
                    double originalWeight = layer.Weights[n, i];

                    // Loss when weight increases
                    layer.Weights[n, i] = originalWeight + epsilon;
                    double lossPlus = CalculateTotalLoss(layer, inputs, targets, loss);

                    // Loss when weight decreases
                    layer.Weights[n, i] = originalWeight - epsilon;
                    double lossMinus = CalculateTotalLoss(layer, inputs, targets, loss);

                    // Restore original weight
                    layer.Weights[n, i] = originalWeight;

                    double numericalGradient =(lossPlus - lossMinus) / (2 * epsilon);

                    double analyticalGradient =layer.DWeights[n, i];

                    Assert.True(
                        Math.Abs(numericalGradient - analyticalGradient)
                        < tolerance,
                        $"Gradient mismatch at weight [{n},{i}]. " +
                        $"Numerical: {numericalGradient}, " +
                        $"Analytical: {analyticalGradient}");
                }
            }
        }

        private static double CalculateTotalLoss(DenseLayer layer, double[] inputs, double[] targets, MeanSquaredErrorLoss loss)
        {
            double[] predictions = layer.Forward(inputs);
            double totalLoss = 0;

            for (int n = 0; n < predictions.Length; n++)
            {
                totalLoss += loss.Forward(predictions[n], targets[n]);
            }

            return totalLoss;
        }

        [Fact]
        public void Backward_BiasGradients()
        {
            // Arrange
            var layer = new DenseLayer(3, 2, new Random(42));

            double[] inputs = { 0.2, 0.5, 0.8 };
            double[] targets = { 0.3, 0.7 };

            var loss = new MeanSquaredErrorLoss();

            const double epsilon = 1e-5;
            const double tolerance = 1e-6;

            // Step 1: Calculate analytical gradients
            double[] predictions = layer.Forward(inputs);

            double[] dvalues = new double[predictions.Length];

            for (int n = 0; n < predictions.Length; n++)
            {
                dvalues[n] = loss.Backward(predictions[n], targets[n]);
            }

            layer.Backward(dvalues);

            // Step 2: Check each bias
            for (int n = 0; n < layer.NeuronN; n++)
            {
                double originalBias = layer.Biases[n];

                // Loss when bias increases
                layer.Biases[n] = originalBias + epsilon;

                double lossPlus = CalculateTotalLoss(layer, inputs, targets, loss);

                // Loss when bias decreases
                layer.Biases[n] = originalBias - epsilon;

                double lossMinus = CalculateTotalLoss(layer, inputs, targets, loss);

                // Restore original bias
                layer.Biases[n] = originalBias;

                // Numerical gradient
                double numericalGradient = (lossPlus - lossMinus) / (2 * epsilon);

                // Analytical gradient
                double analyticalGradient = layer.DBiases[n];

                // Compare
                Assert.True(Math.Abs(numericalGradient - analyticalGradient) < tolerance,
                    $"Bias gradient mismatch at neuron {n}. " +
                    $"Numerical: {numericalGradient}, " +
                    $"Analytical: {analyticalGradient}");
            }
        }
    }
}
