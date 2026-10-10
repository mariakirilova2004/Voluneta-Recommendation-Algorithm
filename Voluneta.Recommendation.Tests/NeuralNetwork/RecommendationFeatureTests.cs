using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    public class RecommendationFeatureTests
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

        [Fact]
        public void Calculate_Features()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                VolunteerInterests = "Environment,Education",
                CategoryName = "Environment",
                CategoryId = 3,
                CompletedCauses = new[] { new CompletedCause(3), new CompletedCause(2) },
                DaysAvailable = "Monday,Wednesday",
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0), // Monday morning
                PreferredTimeOfDay = "Morning",
                UserLatitude = 42.0,
                UserLongitude = 23.0,
                CauseLatitude = 42.0,
                CauseLongitude = 23.0,
                UserSkills = "Teaching,Planning",
                RequiredSkills = "Teaching,First Aid",
                HasPreviousExperience = true,
                DifficultyLevel = 2,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = new[] {
                new ColleagueParticipant("user2", 7),
                new ColleagueParticipant("user3", 8)
                }
            };

            // Act
            var values = RecommendationFeatureCalculator.Calculate(input).ToArray();

            // Assert
            Assert.Equal(8, values.Length);
            Assert.Equal(new double[] { 1, 0.5, 1, 1, 1, 0.5, 1, 0.5 }, values);
        }

        [Fact]
        public void Missing_Data()
        {
            // Arrange + Act
            var result = RecommendationFeatureCalculator.Calculate(new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                RequiredSkills = "First Aid",
                DifficultyLevel = 2
            });

            // Assert
            Assert.All(result.ToArray(), x => Assert.InRange(x, 0, 1));
            Assert.Equal(0.5, result.InterestMatch);
            Assert.Equal(0.5, result.CategoryHistoryMatch);
            Assert.Equal(0.5, result.LocationMatch);
            Assert.Equal(0.5, result.SkillMatch);
        }

        [Fact]
        public void Features_Normalized()
        {
            var result = RecommendationFeatureCalculator.Calculate(new RecommendationFeatureInput
            {
                VolunteerInterests = "Animals",
                CategoryName = "Environment",
                CategoryId = 2,
                CompletedCauses = new[] { new CompletedCause(1) },
                DaysAvailable = "Friday",
                CauseStart = new DateTime(2026, 10, 12, 19, 0, 0),
                PreferredTimeOfDay = "Morning",
                UserLatitude = 42.7,
                UserLongitude = 23.3,
                CauseLatitude = 42.1,
                CauseLongitude = 24.7,
                UserSkills = "Writing",
                RequiredSkills = "First Aid",
                HasPreviousExperience = false,
                DifficultyLevel = 2,
                CompanyId = 1,
                UserId = "1",
                RegisteredParticipants = new[] { new ColleagueParticipant("2", 2) }
            });
            Assert.All(result.ToArray(), x => Assert.InRange(x, 0, 1));
        }
    }
}
