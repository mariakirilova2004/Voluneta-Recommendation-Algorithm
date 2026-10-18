using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;

namespace Voluneta.Recommendation.Tests.Evaluation
{
    public class StatisticsCalculatorTests
    {
        [Fact]
        public void Calculate_WithValues()
        {
            // Arrange
            var results = new List<EvaluationResult>
            {
                new("Model A", 0.1, 0.2, 100),
                new("Model A", 0.2, 0.3, 100),
                new("Model A", 0.3, 0.4, 100)
            };

            // Act
            var statistics = StatisticsCalculator.Calculate(results);

            // Assert
            var model = Assert.Single(statistics);

            Assert.Equal("Model A", model.ModelName);
            Assert.Equal(3, model.RunCount);

            Assert.Equal(0.2, model.MeanMAE, 10);
            Assert.Equal(0.3, model.MeanRMSE, 10);

            Assert.Equal(0.1, model.StdMAE, 10);
            Assert.Equal(0.1, model.StdRMSE, 10);
        }

        [Fact]
        public void Calculate_Groups()
        {
            var results = new List<EvaluationResult>
            {
                new("Model A", 0.1, 0.2, 100),
                new("Model B", 0.3, 0.4, 100),
                new("Model A", 0.2, 0.3, 100),
                new("Model B", 0.4, 0.5, 100)
            };

            var statistics = StatisticsCalculator.Calculate(results);

            Assert.Equal(2, statistics.Count);

            var modelA = Assert.Single(statistics, s => s.ModelName == "Model A");

            var modelB = Assert.Single(statistics, s => s.ModelName == "Model B");

            Assert.Equal(2, modelA.RunCount);
            Assert.Equal(2, modelB.RunCount);

            Assert.Equal(0.15, modelA.MeanMAE, 10);
            Assert.Equal(0.35, modelB.MeanMAE, 10);
        }

        [Fact]
        public void Calculate_SingleRun()
        {
            var results = new List<EvaluationResult>
            {
                new("Model A", 0.15, 0.25, 100)
            };

            var statistics = StatisticsCalculator.Calculate(results);

            var model = Assert.Single(statistics);

            Assert.Equal(1, model.RunCount);
            Assert.Equal(0.0, model.StdMAE);
            Assert.Equal(0.0, model.StdRMSE);
        }

        [Fact]
        public void Calculate_IdenticalResults()
        {
            var results = new List<EvaluationResult>
            {
                new("Model A", 0.2, 0.3, 100),
                new("Model A", 0.2, 0.3, 100),
                new("Model A", 0.2, 0.3, 100)
            };

            var statistics = StatisticsCalculator.Calculate(results);

            var model = Assert.Single(statistics);

            Assert.Equal(0.0, model.StdMAE, 10);
            Assert.Equal(0.0, model.StdRMSE, 10);
        }

        [Fact]
        public void Calculate_Empty()
        {
            var results = new List<EvaluationResult>();

            Assert.Throws<ArgumentException>(() => StatisticsCalculator.Calculate(results));
        }

        [Fact]
        public void Calculate_Null()
        {
            Assert.Throws<ArgumentNullException>(() => StatisticsCalculator.Calculate(null!));
        }

        [Fact]
        public void ModelStatistics_Converts()
        {
            var model = new ModelStatistics(
                "Model A",
                5,
                0.1,
                0.02,
                0.15,
                0.03);

            Assert.Equal(0.9, model.MeanMAEOnRatingScale, 10);
            Assert.Equal(1.35, model.MeanRMSEOnRatingScale, 10);
        }
    }
}
