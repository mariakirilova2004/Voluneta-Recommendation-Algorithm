using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Training
{
    public class TrainingSampleTests
    {
        [Theory]
        [InlineData(1, 0.0)]
        [InlineData(10, 1.0)]
        [InlineData(8, 7.0 / 9.0)]
        public void Normalize_ReturnExpectedValue(int rating, double expected)
        {
            // Act
            var result = RatingNormalizer.Normalize(rating);

            // Assert
            Assert.Equal(expected, result, 10);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public void Normalize_RejectsInvalidRatings(int rating)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RatingNormalizer.Normalize(rating));
        }

        [Fact]
        public void TrainingSample_StoresFeaturesAndTarget()
        {
            // Arrange
            var features = new RecommendationFeatureVector
            {
                InterestMatch = 1.0,
                CategoryHistoryMatch = 0.5,
                DayAvailableMatch = 1.0,
                TimeAvailableMatch = 1.0,
                LocationMatch = 0.8,
                SkillMatch = 0.5,
                ExperienceMatch = 1.0,
                ColleagueMatch = 0.0
            };

            // Act
            var sample = new TrainingSample(features, 8);


            // Assert
            Assert.Equal(8, sample.Features.Length);
            Assert.Equal(7.0 / 9.0, sample.Target, 10);
        }
    }
}
