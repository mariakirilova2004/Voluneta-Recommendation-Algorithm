using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Baselines
{
    public class MeanRatingBaselineTests
    {
        [Fact]
        public void Fit_WithValidSamples()
        {
            // Arrange
            var features = new RecommendationFeatureVector();

            var samples = new List<TrainingSample>
            {
                new TrainingSample(features, 1),
                new TrainingSample(features, 5),
                new TrainingSample(features, 10)
            };

            var baseline = new MeanRatingBaseline();

            // Act
            baseline.Fit(samples);
            double prediction = baseline.Predict();

            // Assert
            // Normalized targets:
            // 1 = 0
            // 5 = 4/9
            // 10 = 1
            double expected = (0.0 + 4.0 / 9.0 + 1.0) / 3.0;

            Assert.Equal(expected, prediction, 10);
        }

        [Fact]
        public void Predict_BeforeFit()
        {
            // Arrange
            var baseline = new MeanRatingBaseline();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => baseline.Predict());
        }

        [Fact]
        public void Fit_WithEmptySamplesn()
        {
            // Arrange
            var baseline = new MeanRatingBaseline();
            var samples = new List<TrainingSample>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => baseline.Fit(samples));
        }
    }
}
