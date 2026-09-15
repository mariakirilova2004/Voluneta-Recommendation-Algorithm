using Recommendation.Core.Algorithms.Recommendation.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    public class RecommendationFeatureVectorTests
    {
        [Fact]
        public void Count_ShouldBeEight()
        {
            // Arrange
            var vector = new RecommendationFeatureVector();

            // Act
            int count = RecommendationFeatureVector.Count;

            // Assert
            Assert.Equal(8, count);
        }

        [Fact]
        public void ToArray_ShouldReturnCorrectArray()
        {
            // Arrange
            var vector = new RecommendationFeatureVector()
            {
                InterestMatch = 0.1,
                CategoryHistoryMatch = 0.2,
                DayAvailableMatch = 0.3,
                TimeAvailableMatch = 0.4,
                LocationMatch = 0.5,
                SkillMatch = 0.6,
                ExperienceMatch = 0.7,
                ColleagueMatch = 0.8
            };

            // Act
            double[] res = vector.ToArray();

            // Assert
            Assert.Equal([0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8], res);
        }
    }
}
