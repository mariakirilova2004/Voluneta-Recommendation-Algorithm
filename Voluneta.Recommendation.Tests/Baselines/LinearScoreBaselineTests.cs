using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Baselines
{
    public class LinearScoreBaselineTests
    {
        [Fact]
        public void Predict_BeforeFit()
        {
            var baseline = new LinearScoreBaseline();

            Assert.Throws<InvalidOperationException>(() => baseline.Predict(new RecommendationFeatureVector()));
        }

        [Fact]
        public void Fit_EmptyDataset()
        {
            var baseline = new LinearScoreBaseline();

            var samples = new List<TrainingSample>();

            Assert.Throws<ArgumentException>(() => baseline.Fit(samples));
        }

        [Fact]
        public void Fit_InvalidLearningRate()
        {
            var baseline = new LinearScoreBaseline();

            var samples = new List<TrainingSample>
            {
                new TrainingSample(new RecommendationFeatureVector(), 5)
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => baseline.Fit(samples, learningrate: 0.0));
        }

        [Fact]
        public void Fit_LinearRelationship()
        {
            // Arrange
            var samples = new List<TrainingSample>
            {
                new TrainingSample(new RecommendationFeatureVector { InterestMatch = 0.0 }, 1),

                new TrainingSample(new RecommendationFeatureVector { InterestMatch = 0.25 }, 3),

                new TrainingSample(new RecommendationFeatureVector { InterestMatch = 0.5 }, 5),

                new TrainingSample(new RecommendationFeatureVector { InterestMatch = 0.75 }, 8),

                new TrainingSample(new RecommendationFeatureVector { InterestMatch = 1.0 }, 10)
            };

            var baseline = new LinearScoreBaseline();

            // Act
            baseline.Fit(samples, epochs: 2000, learningrate: 0.01);

            double lowPrediction = baseline.Predict(new RecommendationFeatureVector { InterestMatch = 0.1 });

            double highPrediction = baseline.Predict(new RecommendationFeatureVector { InterestMatch = 0.9 });

            // Assert
            Assert.True(highPrediction > lowPrediction);

            Assert.InRange(lowPrediction, 0.0, 1.0);
            Assert.InRange(highPrediction, 0.0, 1.0);
        }

        [Fact]
        public void Predict_AfterFit()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(100, seed: 42);

            var baseline = new LinearScoreBaseline();

            // Act
            baseline.Fit(dataset.Dataset);

            var features = new RecommendationFeatureVector
            {
                InterestMatch = 0.8,
                CategoryHistoryMatch = 0.6,
                DayAvailableMatch = 1.0,
                TimeAvailableMatch = 1.0,
                LocationMatch = 0.9,
                SkillMatch = 0.7,
                ExperienceMatch = 0.5,
                ColleagueMatch = 0.4
            };

            double prediction = baseline.Predict(features);

            // Assert
            Assert.True(double.IsFinite(prediction));
            Assert.InRange(prediction, 0.0, 1.0);
        }
    }
}
