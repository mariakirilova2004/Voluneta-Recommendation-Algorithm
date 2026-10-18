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
            var results = ModelComparisonExperiment.Run(
                sampleCount: 1000,
                seed: 42);

            // Arrange & Act
            Console.WriteLine();
            Console.WriteLine("MODEL COMPARISON RESULTS");
            Console.WriteLine(new string('-', 85));

            Console.WriteLine(
                $"{"Model",-25} {"MAE",12} {"RMSE",12} " +
                $"{"MAE (1-10)",15} {"RMSE (1-10)",15}");

            Console.WriteLine(new string('-', 85));

            foreach (var result in results)
            {
                Console.WriteLine(
                    $"{result.ModelName,-25} " +
                    $"{result.MAE,12:F6} " +
                    $"{result.RMSE,12:F6} " +
                    $"{result.MAEOnRatingScale,15:F4} " +
                    $"{result.RMSEOnRatingScale,15:F4}");
            }
        }


        [Fact]
        public void Run_PrintsMultiSeedStatistics()
        {
            // Run the experiment with five different seeds
            var results = MultiSeedExperiment.Run(sampleCount: 1000);

            // Calculate mean and standard deviation
            var statistics = StatisticsCalculator.Calculate(results);

            // Verify experiment structure
            Assert.Equal(15, results.Count);
            Assert.Equal(3, statistics.Count);

            Console.WriteLine();
            Console.WriteLine("MULTI-SEED MODEL COMPARISON");
            Console.WriteLine("Seeds: 1, 7, 42, 123, 2026");
            Console.WriteLine(new string('-', 95));

            Console.WriteLine(
                $"{"Model",-25} {"Mean MAE",12} {"Std MAE",12} " +
                $"{"Mean RMSE",12} {"Std RMSE",12} {"Runs",8}");

            Console.WriteLine(new string('-', 95));

            foreach (var model in statistics)
            {
                Assert.Equal(5, model.RunCount);

                Console.WriteLine(
                    $"{model.ModelName,-25} " +
                    $"{model.MeanMAE,12:F6} " +
                    $"{model.StdMAE,12:F6} " +
                    $"{model.MeanRMSE,12:F6} " +
                    $"{model.StdRMSE,12:F6} " +
                    $"{model.RunCount,8}");
            }
        }

    }
}
