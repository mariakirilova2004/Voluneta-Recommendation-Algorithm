using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Models;
using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
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
                    double originalWeight = layer.Weights[n,i];

                    layer.Weights[n,i] = originalWeight + epsilon;
                    double lossPlus = loss.Forward(network.Forward(input), target);

                    layer.Weights[n,i] = originalWeight - epsilon;
                    double lossMinus = loss.Forward(network.Forward(input), target);

                    layer.Weights[n,i] = originalWeight;

                    double gradient = (lossPlus - lossMinus) / (2 * epsilon);

                    Assert.Equal(gradient, layer.DWeights[n, i]);
                }
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
        public void Backward_Layer_Match()
        {
            //Arrange
            var network = new RecommendationNeuralNetwork(new Random(42));
            var loss = new MeanSquaredErrorLoss();
            double[] input = { 0.8, 0.6, 0.7, 0.5, 0.9, 0.4, 0.6, 0.0 };
            double target = 0.75;
            double epsilon = 1e-5;

            //Act
            double predict = network.Forward(input);
            double lossValue = loss.Backward(predict, target);

            network.Backward(lossValue);

            //original weight

            double gradient = network.Layer1.DWeights[0, 0];

            double original = network.Layer1.Weights[0, 0];

            //Pluss original weight
            network.Layer1.Weights[0, 0] = original + epsilon;
            double plusPredict = network.Forward(input);
            double plusLoss = loss.Forward(plusPredict, target);

            //Minus original weight
            network.Layer1.Weights[0, 0] = original - epsilon;
            double minusPredict = network.Forward(input);
            double minusLoss = loss.Forward(minusPredict, target);

            double res = (plusLoss - minusLoss) / (2 * epsilon);

            //Assert
            Assert.Equal(res, gradient, 5);
        }
    }
}