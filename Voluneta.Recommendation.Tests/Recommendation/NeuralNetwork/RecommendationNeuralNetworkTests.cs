using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Models;
using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.Recommendation.NeuralNetwork
{
    public class RecommendationNeuralNetworkTests
    {
        private static void CheckWeightGradient(
            RecommendationNeuralNetwork network,
            DenseLayer layer,
            MeanSquaredErrorLoss loss,
            double[] input,
            double target,
            double epsilon = 1e-5)
        {
            for (int n = 0; n < layer.NeuronN; n++)
            {
                for (int i = 0; i < layer.InputN; i++)
                {
                    double originalWeight = layer.Weights[n, i];

                    layer.Weights[n, i] = originalWeight + epsilon;
                    double lossPlus = loss.Forward(network.Forward(input), target);

                    layer.Weights[n, i] = originalWeight - epsilon;
                    double lossMinus = loss.Forward(network.Forward(input), target);

                    layer.Weights[n, i] = originalWeight;

                    double gradient = (lossPlus - lossMinus) / (2 * epsilon);

                    Assert.Equal(gradient, layer.DWeights[n, i], 5);
                }
            }
        }

        private static void CheckBiasGradient(
            RecommendationNeuralNetwork network,
            DenseLayer layer,
            MeanSquaredErrorLoss loss,
            double[] input,
            double target,
            double epsilon = 1e-5)
        {
            for (int n = 0; n < layer.NeuronN; n++)
            {
                double originalBias = layer.Biases[n];
                layer.Biases[n] = originalBias + epsilon;
                double lossPlus = loss.Forward(network.Forward(input), target);
                layer.Biases[n] = originalBias - epsilon;
                double lossMinus = loss.Forward(network.Forward(input), target);
                layer.Biases[n] = originalBias;
                double gradient = (lossPlus - lossMinus) / (2 * epsilon);
                double analyticalGradient = layer.DBiases[n];

                Assert.True(
                    Math.Abs(gradient - analyticalGradient) < 1e-5,
                    $"Bias {n}: " +
                    $"Numerical = {gradient}, " +
                    $"Analytical = {analyticalGradient}, " +
                    $"Original bias = {originalBias}"
                );
                //Assert.Equal(gradient, layer.DBiases[n], 5);
            }
        }

        [Fact]
        public void Training_SingleData()
        {
            double[] features =
            {
                1.0, // InterestMatch
                0.8, // CategoryMatch
                1.0, // DayAvailableMatch
                0.9, // TimeAvailableMatch
                0.7, // LocationMatch
                0.9, // SkillMatch
                0.6, // ExperienceMatch
                0.8  // ColleagueMatch
            };

            double target = (9.0 - 1.0) / 9.0;

            var network = new RecommendationNeuralNetwork(new Random(42));

            var lossFunction = new MeanSquaredErrorLoss();

            var optimizer = new SGD(learningRate: 0.01);

            double predictionBefore = network.Forward(features);

            double lossBefore = lossFunction.Forward(predictionBefore, target);

            for (int i = 0; i < 1000; i++)
            {
                // Forward
                double prediction = network.Forward(features);

                // Calculate loss
                double lossGradient = lossFunction.Backward(prediction, target);

                // Backpropagation
                network.Backward(lossGradient);

                // Actually change 
                network.Update(optimizer);
            }

            // Prediction after the training
            double predictionAfter = network.Forward(features);

            double lossAfter = lossFunction.Forward(predictionAfter, target);

            Assert.True(lossAfter < lossBefore, $"Loss should decrease. " + $"Before: {lossBefore}, " + $"After: {lossAfter}");

            Assert.True(Math.Abs(target - predictionAfter) < Math.Abs(target - predictionBefore), "Prediction should move closer to the target.");
        }

        [Fact]
        public void Training_MultipleData()
        {
            var networks = new RecommendationNeuralNetwork(new Random(42));

            var loss = new MeanSquaredErrorLoss();

            var sgd = new SGD(learningRate: 0.01);

            var training = new List<RecommendationTraining> {
                new RecommendationTraining(new double[] {1,1,1,1,0.9,0.9,0.8,0.9}, (10-1)/9),
                new RecommendationTraining(new double[] {0.8,0.7,1,0.9,0.7,0.8,0.6,0.7}, (8-1)/9),
                new RecommendationTraining(new double[] {0.5,0.4,1.0,0.5,0.4,0.5,0.3,0.4}, (5-1)/9),
                new RecommendationTraining(new double[] {0.1,0.1,0.0,0.2,0.1,0.2,0.1,0.0}, (2-1)/9),
            };

            double lossBefore = 0;

            foreach (var data in training)
            {
                double prediction = networks.Forward(data.Features);
                lossBefore += loss.Forward(prediction, data.Target);
            }

            lossBefore /= training.Count;

            for (int i = 0; i < 1000; i++)
            {
                foreach (var data in training)
                {
                    double prediction = networks.Forward(data.Features);
                    double lossGradient = loss.Backward(prediction, data.Target);
                    networks.Backward(lossGradient);
                    networks.Update(sgd);
                }
            }

            double lossAfter = 0;
            foreach (var data in training)
            {
                double prediction = networks.Forward(data.Features);
                lossAfter += loss.Forward(prediction, data.Target);
            }
            lossAfter /= training.Count;

            Assert.True(lossAfter < lossBefore, $"Loss less. Before: {lossBefore}, After: {lossAfter}");
        }

        [Fact]
        public void Backward_Layer1_Weight_Gradient()
        {
            // Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;

            // Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);
            network.Backward(lossValue);

            // Assert
            CheckWeightGradient(network, network.Layer1, loss, input, target);
        }

        [Fact]
        public void Backward_Layer1_Bias_Gradient()
        {
            // Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;

            // Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);
            network.Backward(lossValue);

            // Assert
            CheckBiasGradient(network, network.Layer1, loss, input, target);
        }

        [Fact]
        public void Backward_Layer2_Weight_Gradient()
        {
            // Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;

            // Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);
            network.Backward(lossValue);

            // Assert
            CheckWeightGradient(network, network.Layer2, loss, input, target);
        }

        [Fact]
        public void Backward_Layer2_Bias_Gradient()
        {
            // Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;

            // Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);
            network.Backward(lossValue);

            // Assert
            CheckBiasGradient(network, network.Layer2, loss, input, target, 1e-6);
        }

        [Fact]
        public void Backward_Output_Bias_Gradient()
        {
            // Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;

            // Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);
            network.Backward(lossValue);

            // Assert
            CheckBiasGradient(network, network.OutputLayer, loss, input, target);
        }

        [Fact]
        public void Backward_Output_Weight_Gradient()
        {
            // Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;

            // Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);
            network.Backward(lossValue);

            // Assert
            CheckWeightGradient(network, network.OutputLayer, loss, input, target);
        }
    }
}