using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Evaluation
{
    public class ModelEvaluatorTests
    {
        private static List<TrainingSample> CreateSamples()
        {
            // Arrange + Act + Assert
            return new List<TrainingSample>
            {
                new TrainingSample(new RecommendationFeatureVector(), 1),

                new TrainingSample(new RecommendationFeatureVector(), 5),

                new TrainingSample(new RecommendationFeatureVector(), 10)
            };
        }

        [Fact]
        public void Evaluate_Perfect()
        {
            // Arrange
            var samples = CreateSamples();

            // Act
            var result = ModelEvaluator.Evaluate("Perfect Model", samples, sample => sample.Target);

            // Assert
            Assert.Equal("Perfect Model", result.ModelName);
            Assert.Equal(3, result.SampleCount);
            Assert.Equal(0.0, result.MAE);
            Assert.Equal(0.0, result.RMSE);
        }

        [Fact]
        public void Evaluate()
        {
            // Arrange
            var samples = CreateSamples();

            // Act
            var result = ModelEvaluator.Evaluate("Constant Model", samples, sample => 0.5);

            // Targets: 0, 4/9, 1
            // Predictions: 0.5, 0.5, 0.5

            double expectedMAE = (0.5 + 1.0 / 18.0 + 0.5) / 3.0;

            double expectedRMSE = Math.Sqrt((0.25 + Math.Pow(1.0 / 18.0, 2) + 0.25) / 3.0);

            // Assert
            Assert.Equal(expectedMAE, result.MAE, 10);
            Assert.Equal(expectedRMSE, result.RMSE, 10);
        }

        [Fact]
        public void Evaluate_Converts()
        {
            // Arrange
            var samples = CreateSamples();

            // Act
            var result = ModelEvaluator.Evaluate("Constant Model", samples, sample => 0.5);

            // Assert
            Assert.Equal(result.MAE * 9.0, result.MAEOnRatingScale, 10);

            Assert.Equal(result.RMSE * 9.0, result.RMSEOnRatingScale, 10);
        }

        [Fact]
        public void Evaluate_EmptyDataset()
        {
            // Arrange
            var samples = new List<TrainingSample>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ModelEvaluator.Evaluate("Test Model", samples, sample => 0.5));
        }

        [Fact]
        public void Evaluate_Invalid()
        {
            // Arrange
            var samples = CreateSamples();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ModelEvaluator.Evaluate("", samples, sample => 0.5));
        }

        [Fact]
        public void Evaluate_WithNull()
        {
            // Arrange
            var samples = CreateSamples();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ModelEvaluator.Evaluate("Test Model", samples, null!));
        }

        [Fact]
        public void Evaluate_OncePerSample()
        {
            // Arrange
            var samples = CreateSamples();

            int predictionCalls = 0;

            // Act
            var result = ModelEvaluator.Evaluate("Counting Model", samples, sample =>
                {
                    predictionCalls++;
                    return 0.5;
                });

            // Assert
            Assert.Equal(samples.Count, predictionCalls);
            Assert.Equal(samples.Count, result.SampleCount);
        }

        [Fact]
        public void Evaluate_NonFinitePrediction()
        {
            // Arrange
            var samples = CreateSamples();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ModelEvaluator.Evaluate("Invalid Model", samples, sample => double.NaN));
        }
    }
}
