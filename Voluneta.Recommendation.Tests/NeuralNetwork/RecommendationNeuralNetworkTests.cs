using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Models;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    public class RecommendationNeuralNetworkTests
    {
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
    }
}