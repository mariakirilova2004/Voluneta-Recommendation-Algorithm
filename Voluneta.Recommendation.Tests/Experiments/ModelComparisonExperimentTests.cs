using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Experiments;

namespace Voluneta.Recommendation.Tests.Experiments
{
    public class ModelComparisonExperimentTests
    {
        [Fact]
        public void Run_Models()
        {
            // Arrange & Act
            var results = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.ModelName == "Mean Rating Baseline");
            Assert.Contains(results, r => r.ModelName == "Linear Score Baseline");
            Assert.Contains(results, r => r.ModelName == "Neural Network");
        }

        [Fact]
        public void Run_SampleCount()
        {
            // Arrange & Act
            var results = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42);

            // Assert
            foreach (var result in results)
            {
                Assert.Equal(150, result.SampleCount);
            }
        }

        [Fact]
        public void Run_ReturnsFinite()
        {
            // Arrange & Act
            var results = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42);

            // Assert
            foreach (var result in results)
            {
                Assert.True(double.IsFinite(result.MAE));
                Assert.True(double.IsFinite(result.RMSE));
                Assert.True(result.MAE >= 0);
                Assert.True(result.RMSE >= 0);
            }
        }

        [Fact]
        public void Run_Seed()
        {
            // Arrange & Act
            var first = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42);
            var second = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42);

            // Assert
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i].ModelName, second[i].ModelName);
                Assert.Equal(first[i].MAE, second[i].MAE, 10);
                Assert.Equal(first[i].RMSE, second[i].RMSE, 10);
            }
        }

        [Fact]
        public void Run_PrintsComparison()
        {
            // Arrange & Act
            var results = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42);

            // Assert
            Console.WriteLine();
            Console.WriteLine("MODEL COMPARISON RESULTS");
            Console.WriteLine(new string('-', 85));

            foreach (var result in results)
            {
                Console.WriteLine(result.ModelName + " " + result.MAE + " " + result.RMSE + " " + result.MAEOnRatingScale + " " + result.RMSEOnRatingScale);
            }
        }

        [Fact]
        public void Run_PrintsMultiSeedStatistics()
        {
            // Arrange & Act
            var results = MultiSeedExperiment.Run(sampleCount: 1000);
            var statistics = StatisticsCalculator.Calculate(results);

            // Assert
            Assert.Equal(15, results.Count);
            Assert.Equal(3, statistics.Count);

            Console.WriteLine();
            Console.WriteLine("MULTI-SEED MODEL COMPARISON");
            Console.WriteLine("Seeds: 1, 7, 42, 123, 2026");

            foreach (var model in statistics)
            {
                Assert.Equal(5, model.RunCount);
                Console.WriteLine(model.ModelName + " " + model.MeanMAE + " " + model.StdMAE + " " + model.MeanRMSE + " " + model.StdRMSE + " " + model.RunCount);
            }
        }

        [Fact]
        public void Run_ComparesSelectedNeuralConfiguration()
        {
            // Arrange & Act
            var results = ModelComparisonExperiment.Run(sampleCount: 1000, seed: 42, neuralLearningRate: 0.1, neuralMaxEpochs: 100, neuralUseEarlyStopping: false);

            // Assert
            Assert.Equal(3, results.Count);

            Console.WriteLine();
            Console.WriteLine("FINAL MODEL COMPARISON");
            Console.WriteLine("Neural Network: LR=0.1, Epochs=100");

            foreach (var result in results)
            {
                Assert.Equal(150, result.SampleCount);
                Assert.True(double.IsFinite(result.MAE));
                Assert.True(double.IsFinite(result.RMSE));
                Console.WriteLine(result.ModelName + " " + result.MAE + " " + result.RMSE + " " + result.MAEOnRatingScale + " " + result.RMSEOnRatingScale);
            }
        }

        [Fact]
        public void Run_FinalMultiSeedComparison()
        {
            // Arrange & Act
            int[] seeds = { 1, 7, 42, 123, 2026 };
            var allResults = new List<EvaluationResult>();

            foreach (int seed in seeds)
            {
                var results = ModelComparisonExperiment.Run(sampleCount: 1000, seed: seed, neuralLearningRate: 0.1, neuralMaxEpochs: 100, neuralUseEarlyStopping: false);
                allResults.AddRange(results);
            }

            var statistics = StatisticsCalculator.Calculate(allResults);

            // Assert
            Assert.Equal(15, allResults.Count);
            Assert.Equal(3, statistics.Count);

            Console.WriteLine();
            Console.WriteLine("FINAL MULTI-SEED COMPARISON");

            foreach (var model in statistics)
            {
                Assert.Equal(5, model.RunCount);
                Console.WriteLine(model.ModelName + " " + model.MeanMAE + " " + model.StdMAE + " " + model.MeanRMSE + " " + model.StdRMSE);
            }
        }
    }
}
