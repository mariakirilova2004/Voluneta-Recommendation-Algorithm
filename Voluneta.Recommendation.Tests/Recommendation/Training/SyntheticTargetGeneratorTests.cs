using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Training
{
    public class SyntheticTargetGeneratorTests
    {
        [Fact]
        public void CalculateScore0()
        {
            // Arrange
            var features = new RecommendationFeatureVector();
            
            // Act
            double score = SyntheticTargetGenerator.CalculateScore(features);

            // Assert
            Assert.Equal(0.0, score, 10);
        }

        [Fact]
        public void CalculateScore1()
        {
            // Arrange
            var features = new RecommendationFeatureVector
            {
                InterestMatch = 1.0,
                CategoryHistoryMatch = 1.0,
                DayAvailableMatch = 1.0,
                TimeAvailableMatch = 1.0,
                LocationMatch = 1.0,
                SkillMatch = 1.0,
                ExperienceMatch = 1.0,
                ColleagueMatch = 1.0
            };

            // Act
            double score = SyntheticTargetGenerator.CalculateScore(features);

            // Assert
            Assert.Equal(1.0, score, 10);
        }

        [Fact]
        public void CalculateScore_Nonlinear()
        {
            // Arrange
            var features = new RecommendationFeatureVector
            {
                InterestMatch = 0.8,
                LocationMatch = 0.5
            };

            // Act
            double score = SyntheticTargetGenerator.CalculateScore(features);

            // Assert
            // Linear location contribution: 0.15 * 0.5 = 0.075
            // Nonlinear interaction: 0.20 * 0.8 * 0.5 = 0.08
            // Expected total: 0.155

            Assert.Equal(0.155, score, 10);
        }

        [Fact]
        public void CalculateScore_LocationZero_InterestZero()
        {
            // Arrange
            var features = new RecommendationFeatureVector
            {
                InterestMatch = 1.0,
                LocationMatch = 0.0
            };

            // Act
            double score = SyntheticTargetGenerator.CalculateScore(features);

            // Assert
            Assert.Equal(0.0, score, 10);
        }
    }
}
