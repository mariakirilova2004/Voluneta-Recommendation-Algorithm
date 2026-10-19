using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Training
{
    public class TrainingConvergenceTests
    {
        [Fact]
        public void Train_ReducesTrainingLoss()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(count: 1000, seed: 42);
            var split = DatasetSplitter.Split(dataset, seed: 42);
            var network = new RecommendationNeuralNetwork(new Random(42));
            var trainer = new NeuralNetworkTrainer(network, learningRate: 0.01);
            double initialLoss = trainer.Evaluate(split.Training);
            var options = new TrainingOptions { MaxEpochs = 100, UseEarlyStopping = false };

            // Act
            var history = trainer.Train(split, options);
            double finalLoss = trainer.Evaluate(split.Training);

            // Assert
            Assert.True(finalLoss < initialLoss, "Training did not reduce loss. Initial: " + initialLoss + ", Final: " + finalLoss);
            Assert.True(double.IsFinite(finalLoss));

            Console.WriteLine("Initial Training Loss: " + initialLoss);
            Console.WriteLine("Final Training Loss: " + finalLoss);
            Console.WriteLine("Loss Reduction: " + (initialLoss - finalLoss) / initialLoss * 100 + "%");
        }

        [Fact]
        public void Train_RecordsLearningCurveAcrossEpochs()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(count: 1000, seed: 42);
            var split = DatasetSplitter.Split(dataset, seed: 42);
            var network = new RecommendationNeuralNetwork(new Random(42));
            var trainer = new NeuralNetworkTrainer(network, learningRate: 0.01);
            var options = new TrainingOptions { MaxEpochs = 100, UseEarlyStopping = false };

            // Act
            var history = trainer.Train(split, options);

            // Assert
            Assert.Equal(100, history.Epoch.Count);
            Assert.False(history.StoppedEarly);

            foreach (var epoch in history.Epoch)
            {
                Assert.True(double.IsFinite(epoch.TrainingLoss));
                Assert.True(double.IsFinite(epoch.ValidationLoss));
                Assert.True(epoch.TrainingLoss >= 0);
                Assert.True(epoch.ValidationLoss >= 0);
            }

            Console.WriteLine();
            Console.WriteLine("LEARNING CURVE");
            Console.WriteLine("Epoch Training Loss Validation Loss");
            Console.WriteLine(new string('-', 50));

            foreach (var epoch in history.Epoch)
            {
                if (epoch.EpochNumber == 1 || epoch.EpochNumber % 10 == 0)
                {
                    Console.WriteLine(epoch.EpochNumber + " " + epoch.TrainingLoss + " " + epoch.ValidationLoss);
                }
            }

            var firstEpoch = history.Epoch.First();
            var lastEpoch = history.Epoch.Last();

            Console.WriteLine();
            Console.WriteLine("First Training Loss: " + firstEpoch.TrainingLoss);
            Console.WriteLine("Final Training Loss: " + lastEpoch.TrainingLoss);
            Console.WriteLine("First Validation Loss: " + firstEpoch.ValidationLoss);
            Console.WriteLine("Final Validation Loss: " + lastEpoch.ValidationLoss);
            Console.WriteLine("Best Validation Epoch: " + history.BestEpoch);
            Console.WriteLine("Best Validation Loss: " + history.BestValidationLoss);

            var bestRecordedEpoch = history.Epoch.MinBy(e => e.ValidationLoss)!;

            Assert.Equal(bestRecordedEpoch.EpochNumber, history.BestEpoch);
            Assert.Equal(bestRecordedEpoch.ValidationLoss, history.BestValidationLoss, 10);
        }

        [Fact]
        public void Train_EarlyStopping_StopsAndRestoresBestModel()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(count: 1000, seed: 42);
            var split = DatasetSplitter.Split(dataset, seed: 42);
            var network = new RecommendationNeuralNetwork(new Random(42));
            var trainer = new NeuralNetworkTrainer(network, learningRate: 0.01);
            var options = new TrainingOptions { MaxEpochs = 100, UseEarlyStopping = true, Patience = 5, MinDelta = 1.0 };

            // Act
            var history = trainer.Train(split, options);
            double restoredValidationLoss = trainer.Evaluate(split.Validation);

            // Assert
            Assert.True(history.StoppedEarly);
            Assert.Equal(6, history.Epoch.Count);
            Assert.Equal(history.BestValidationLoss, restoredValidationLoss, 10);
            Assert.InRange(history.BestEpoch, 1, history.Epoch.Count);

            Console.WriteLine("Stopped at epoch: " + history.Epoch.Count);
            Console.WriteLine("Best epoch: " + history.BestEpoch);
            Console.WriteLine("Best validation loss: " + history.BestValidationLoss);
            Console.WriteLine("Restored validation loss: " + restoredValidationLoss);
        }

        [Fact]
        public void Train_CompareLearningRates()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(count: 1000, seed: 42);
            var split = DatasetSplitter.Split(dataset, seed: 42);
            double[] learningRates = { 0.001, 0.01, 0.1 };

            Console.WriteLine();
            Console.WriteLine("LEARNING RATE COMPARISON");
            Console.WriteLine(new string('-', 85));

            // Act & Assert
            foreach (double learningRate in learningRates)
            {
                var network = new RecommendationNeuralNetwork(new Random(42));
                var trainer = new NeuralNetworkTrainer(network, learningRate: learningRate);
                double initialLoss = trainer.Evaluate(split.Training);
                var options = new TrainingOptions { MaxEpochs = 100, UseEarlyStopping = false };

                var history = trainer.Train(split, options);
                double finalTrainingLoss = trainer.Evaluate(split.Training);
                double finalValidationLoss = trainer.Evaluate(split.Validation);

                Assert.Equal(100, history.Epoch.Count);
                Assert.False(history.StoppedEarly);
                Assert.True(double.IsFinite(finalTrainingLoss));
                Assert.True(double.IsFinite(finalValidationLoss));

                Console.WriteLine(learningRate + " " + initialLoss + " " + finalTrainingLoss + " " + finalValidationLoss + " " + history.BestEpoch);
            }
        }

        [Fact]
        public void Train_CompareLearningRatesAcrossSeeds()
        {
            // Arrange
            int[] seeds = { 1, 7, 42, 123, 2026 };
            double[] learningRates = { 0.001, 0.01, 0.1 };
            var results = new Dictionary<double, List<double>>();

            foreach (double rate in learningRates)
            {
                results[rate] = new List<double>();
            }

            // Act
            foreach (int seed in seeds)
            {
                var dataset = SyntheticDatasetGenerator.Generate(count: 1000, seed: seed);
                var split = DatasetSplitter.Split(dataset, seed: seed);

                foreach (double rate in learningRates)
                {
                    var network = new RecommendationNeuralNetwork(new Random(seed));
                    var trainer = new NeuralNetworkTrainer(network, learningRate: rate);
                    var options = new TrainingOptions { MaxEpochs = 100, UseEarlyStopping = false };

                    trainer.Train(split, options);
                    double validationLoss = trainer.Evaluate(split.Validation);

                    Assert.True(double.IsFinite(validationLoss));
                    Assert.True(validationLoss >= 0);

                    results[rate].Add(validationLoss);
                }
            }

            // Assert
            Console.WriteLine();
            Console.WriteLine("MULTI-SEED LEARNING RATE COMPARISON");
            Console.WriteLine(new string('-', 65));

            foreach (double rate in learningRates)
            {
                var values = results[rate];
                double mean = values.Average();
                double variance = values.Sum(x => Math.Pow(x - mean, 2)) / (values.Count - 1);
                double std = Math.Sqrt(variance);

                Assert.Equal(5, values.Count);

                Console.WriteLine(rate + " " + mean + " " + std + " " + values.Count);
            }
        }

        [Fact]
        public void Train_EvaluatesSelectedModelOnTestData()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(count: 1000, seed: 42);
            var split = DatasetSplitter.Split(dataset, seed: 42);
            const double selectedLearningRate = 0.1;
            var network = new RecommendationNeuralNetwork(new Random(42));
            var trainer = new NeuralNetworkTrainer(network, learningRate: selectedLearningRate);
            var options = new TrainingOptions { MaxEpochs = 100, UseEarlyStopping = false };

            // Act
            trainer.Train(split, options);
            double trainingLoss = trainer.Evaluate(split.Training);
            double validationLoss = trainer.Evaluate(split.Validation);
            double testLoss = trainer.Evaluate(split.Testing);
            var testResult = ModelEvaluator.Evaluate("Neural Network", split.Testing, sample => network.Forward(sample.Features));

            // Assert
            Assert.Equal(150, testResult.SampleCount);
            Assert.True(double.IsFinite(trainingLoss));
            Assert.True(double.IsFinite(validationLoss));
            Assert.True(double.IsFinite(testLoss));
            Assert.True(double.IsFinite(testResult.MAE));
            Assert.True(double.IsFinite(testResult.RMSE));

            Console.WriteLine();
            Console.WriteLine("SELECTED MODEL - TEST EVALUATION");
            Console.WriteLine("Learning Rate: " + selectedLearningRate);
            Console.WriteLine("Epochs: " + options.MaxEpochs);
            Console.WriteLine("Training MSE: " + trainingLoss);
            Console.WriteLine("Validation MSE: " + validationLoss);
            Console.WriteLine("Test MSE: " + testLoss);
            Console.WriteLine("Test MAE: " + testResult.MAE);
            Console.WriteLine("Test RMSE: " + testResult.RMSE);
            Console.WriteLine("Test MAE (1-10): " + testResult.MAEOnRatingScale);
            Console.WriteLine("Test RMSE (1-10): " + testResult.RMSEOnRatingScale);
        }
    }
}
