using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Tests.Recommendation.Features
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

        [Theory]
        [InlineData("Environment & Nature", "Environmental Work", 1.0)]
        [InlineData("Animal Welfare", "Animals", 1.0)]
        [InlineData("Education & Youth", "Social Impact", 1.0)]
        [InlineData("Community Support", "Social Impact", 1.0)]
        [InlineData("Health & Elderly Care", "Health Care", 1.0)]
        [InlineData("Environment & Nature", "Animals", 0.0)]
        [InlineData("Tourism", "Other", 0.5)]
        public void InterestMatch_UsesCategoryAliasMapping(string volunteerInterests, string categoryName, double expected)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                VolunteerInterests = volunteerInterests,
                CategoryName = categoryName,
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.InterestMatch);
        }

        [Fact]
        public void InterestMatch_MissingVolunteerInterests_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryName = "Animals",
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.InterestMatch);
        }

        [Fact]
        public void InterestMatch_IsCaseInsensitive()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                VolunteerInterests = "environment & nature",
                CategoryName = "ENVIRONMENTAL WORK",
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.InterestMatch);
        }

        [Fact]
        public void InterestMatch_IgnoresLeadingAndTrailingWhitespace()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                VolunteerInterests = "  Environment & Nature  ",
                CategoryName = "  Environmental Work  ",
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.InterestMatch);
        }

        [Fact]
        public void InterestMatch_MultipleSelectedInterests_MatchesAlias()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                VolunteerInterests = "Animal Welfare,Education & Youth,Tourism",
                CategoryName = "Social Impact",
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.InterestMatch);
        }

        [Fact]
        public void InterestMatch_DirectCategoryMatchStillWorks()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                VolunteerInterests = "Environment,Education",
                CategoryName = "Environment",
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.InterestMatch);
        }

        [Theory]
        [InlineData("Morning (08:00-12:00)", 9, 1.0)]
        [InlineData("Afternoon (12:00-17:00)", 14, 1.0)]
        [InlineData("Evening (17:00+)", 18, 1.0)]
        [InlineData("Morning (08:00-12:00)", 18, 0.0)]
        [InlineData("Flexible", 14, 1.0)]
        [InlineData("Morning", 9, 1.0)]
        [InlineData("Afternoon", 14, 1.0)]
        [InlineData("Evening", 9, 0.0)]
        public void TimeAvailableMatch_MatchesCauseStartPeriod(string preferredTimeOfDay, int causeHour, double expected)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, causeHour, 0, 0),
                PreferredTimeOfDay = preferredTimeOfDay,
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.TimeAvailableMatch);
        }

        [Fact]
        public void TimeAvailableMatch_MissingPreference_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 14, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.TimeAvailableMatch);
        }

        [Fact]
        public void TimeAvailableMatch_IsCaseInsensitive()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 14, 0, 0),
                PreferredTimeOfDay = "afternoon (12:00-17:00)",
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.TimeAvailableMatch);
        }

        [Fact]
        public void TimeAvailableMatch_IgnoresLeadingAndTrailingWhitespace()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 14, 0, 0),
                PreferredTimeOfDay = "  Afternoon (12:00-17:00)  ",
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.TimeAvailableMatch);
        }

        [Theory]
        [InlineData("Morning (08:00-12:00)", 11, 1.0)]
        [InlineData("Afternoon (12:00-17:00)", 12, 1.0)]
        [InlineData("Afternoon (12:00-17:00)", 16, 1.0)]
        [InlineData("Evening (17:00+)", 17, 1.0)]
        public void TimeAvailableMatch_BoundaryHours(string preferredTimeOfDay, int causeHour, double expected)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, causeHour, 0, 0),
                PreferredTimeOfDay = preferredTimeOfDay,
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.TimeAvailableMatch);
        }

        [Fact]
        public void TimeAvailableMatch_UnrecognizedPreference_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 14, 0, 0),
                PreferredTimeOfDay = "Whenever",
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.TimeAvailableMatch);
        }

        [Theory]
        [InlineData("Monday,Wednesday,Friday", 2026, 10, 14, 1.0)] // Wednesday
        [InlineData("Monday,Wednesday,Friday", 2026, 10, 13, 0.0)] // Tuesday
        [InlineData("Saturday,Sunday", 2026, 10, 18, 1.0)] // Sunday
        [InlineData("Monday", 2026, 10, 12, 1.0)] // Monday
        [InlineData("monday,WEDNESDAY", 2026, 10, 14, 1.0)] // Wednesday, case insensitive
        [InlineData("Monday, Wednesday, Friday", 2026, 10, 16, 1.0)] // Friday, with spaces
        [InlineData(null, 2026, 10, 12, 0.5)] // null availability
        [InlineData("", 2026, 10, 12, 0.5)] // empty availability
        [InlineData("   ", 2026, 10, 12, 0.5)] // whitespace-only availability
        [InlineData("Someday,Nonsense", 2026, 10, 12, 0.5)] // only unrecognized values
        [InlineData("Mon", 2026, 10, 12, 0.5)] // abbreviated/partial name is not a full match
        [InlineData("Monday,Nonsense", 2026, 10, 12, 1.0)] // recognized match wins over unrecognized value
        [InlineData("Tuesday,Nonsense", 2026, 10, 12, 0.5)] // recognized day present but no match, plus unrecognized -> neutral
        public void DayAvailableMatch_MatchesCauseWeekday(string? daysAvailable, int year, int month, int day, double expected)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                DaysAvailable = daysAvailable,
                CauseStart = new DateTime(year, month, day, 10, 0, 0),
                DifficultyLevel = 0
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.DayAvailableMatch);
        }

        private static double CalculateExpectedLocationMatch(double lat1, double lon1, double lat2, double lon2)
        {
            double radians = Math.PI / 180.0;
            double deltaLatRad = (lat2 - lat1) * radians;
            double deltaLonRad = (lon2 - lon1) * radians;

            double a = Math.Pow(Math.Sin(deltaLatRad / 2), 2) + Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin(deltaLonRad / 2), 2);

            double earthRadiusKm = 6371.0;
            double distanceKm = earthRadiusKm * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));

            return Math.Exp(-distanceKm / 20.0);
        }

        [Fact]
        public void LocationMatch_IdenticalCoordinates_ReturnsOne()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = 42.6977,
                UserLongitude = 23.3219,
                CauseLatitude = 42.6977,
                CauseLongitude = 23.3219
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.LocationMatch, 10);
        }

        [Theory]
        [InlineData(42.6977, 23.3219, 42.6977, 23.3219)] // Sofia to Sofia
        [InlineData(42.6977, 23.3219, 42.1354, 24.7453)] // Sofia to Plovdiv
        [InlineData(0.0, 0.0, 0.0, 1.0)] // one degree of longitude at the equator
        [InlineData(0.0, 0.0, 1.0, 0.0)] // one degree of latitude
        public void LocationMatch_MatchesIndependentHaversineCalculation(double userLat, double userLon, double causeLat, double causeLon)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = userLat,
                UserLongitude = userLon,
                CauseLatitude = causeLat,
                CauseLongitude = causeLon
            };

            double expected = CalculateExpectedLocationMatch(userLat, userLon, causeLat, causeLon);

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.LocationMatch, 6);
        }

        [Fact]
        public void LocationMatch_IsSymmetricWhenSwappingLocations()
        {
            // Arrange
            var forward = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = 42.6977,
                UserLongitude = 23.3219,
                CauseLatitude = 42.1354,
                CauseLongitude = 24.7453
            };

            var reversed = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = 42.1354,
                UserLongitude = 24.7453,
                CauseLatitude = 42.6977,
                CauseLongitude = 23.3219
            };

            // Act
            var forwardResult = RecommendationFeatureCalculator.Calculate(forward);
            var reversedResult = RecommendationFeatureCalculator.Calculate(reversed);

            // Assert
            Assert.Equal(forwardResult.LocationMatch, reversedResult.LocationMatch, 10);
        }

        [Fact]
        public void LocationMatch_IncreasingDistance_DecreasesScore()
        {
            // Arrange
            var near = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = 42.6977,
                UserLongitude = 23.3219,
                CauseLatitude = 42.70,
                CauseLongitude = 23.33
            };

            var far = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = 42.6977,
                UserLongitude = 23.3219,
                CauseLatitude = 43.5,
                CauseLongitude = 24.5
            };

            // Act
            var nearResult = RecommendationFeatureCalculator.Calculate(near);
            var farResult = RecommendationFeatureCalculator.Calculate(far);

            // Assert
            Assert.True(nearResult.LocationMatch > farResult.LocationMatch);
        }

        [Fact]
        public void LocationMatch_MissingUserCoordinates_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CauseLatitude = 42.6977,
                CauseLongitude = 23.3219
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.LocationMatch);
        }

        [Fact]
        public void LocationMatch_MissingCauseCoordinates_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = 42.6977,
                UserLongitude = 23.3219
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.LocationMatch);
        }

        [Theory]
        [InlineData(90.1, 0.0, 0.0, 0.0)]
        [InlineData(-90.1, 0.0, 0.0, 0.0)]
        [InlineData(0.0, 0.0, 90.1, 0.0)]
        [InlineData(0.0, 0.0, -90.1, 0.0)]
        public void LocationMatch_InvalidLatitude_ThrowsArgumentOutOfRangeException(double userLat, double userLon, double causeLat, double causeLon)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = userLat,
                UserLongitude = userLon,
                CauseLatitude = causeLat,
                CauseLongitude = causeLon
            };

            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationFeatureCalculator.Calculate(input));
        }

        [Theory]
        [InlineData(0.0, 180.1, 0.0, 0.0)]
        [InlineData(0.0, -180.1, 0.0, 0.0)]
        [InlineData(0.0, 0.0, 0.0, 180.1)]
        [InlineData(0.0, 0.0, 0.0, -180.1)]
        public void LocationMatch_InvalidLongitude_ThrowsArgumentOutOfRangeException(double userLat, double userLon, double causeLat, double causeLon)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = userLat,
                UserLongitude = userLon,
                CauseLatitude = causeLat,
                CauseLongitude = causeLon
            };

            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationFeatureCalculator.Calculate(input));
        }

        [Theory]
        [InlineData(double.NaN, 0.0, 0.0, 0.0)]
        [InlineData(0.0, double.NaN, 0.0, 0.0)]
        [InlineData(0.0, 0.0, double.NaN, 0.0)]
        [InlineData(0.0, 0.0, 0.0, double.NaN)]
        [InlineData(double.PositiveInfinity, 0.0, 0.0, 0.0)]
        [InlineData(double.NegativeInfinity, 0.0, 0.0, 0.0)]
        [InlineData(0.0, double.PositiveInfinity, 0.0, 0.0)]
        [InlineData(0.0, double.NegativeInfinity, 0.0, 0.0)]
        public void LocationMatch_NaNOrInfiniteCoordinates_ThrowsArgumentOutOfRangeException(double userLat, double userLon, double causeLat, double causeLon)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = userLat,
                UserLongitude = userLon,
                CauseLatitude = causeLat,
                CauseLongitude = causeLon
            };

            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationFeatureCalculator.Calculate(input));
        }

        [Theory]
        [InlineData(90.0, 180.0, -90.0, -180.0)]
        [InlineData(-90.0, -180.0, 90.0, 180.0)]
        [InlineData(90.0, 180.0, 90.0, 180.0)]
        public void LocationMatch_BoundaryCoordinates_AreAccepted(double userLat, double userLon, double causeLat, double causeLon)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = userLat,
                UserLongitude = userLon,
                CauseLatitude = causeLat,
                CauseLongitude = causeLon
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.InRange(result.LocationMatch, 0.0, 1.0);
        }

        [Fact]
        public void LocationMatch_OutputAlwaysWithinZeroToOne()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserLatitude = -33.8688,
                UserLongitude = 151.2093,
                CauseLatitude = 51.5074,
                CauseLongitude = -0.1278
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.InRange(result.LocationMatch, 0.0, 1.0);
        }

        [Theory]
        [InlineData("Teaching, First Aid", "Teaching, First Aid", 1.0)]
        [InlineData("Teaching", "Teaching, First Aid", 0.5)]
        [InlineData("Cooking", "Teaching, First Aid", 0.0)]
        [InlineData(null, "Teaching, First Aid", 0.5)]
        public void SkillMatch_ComparesCommaSeparatedSkills(string? userSkills, string requiredSkills, double expected)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserSkills = userSkills,
                RequiredSkills = requiredSkills
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.SkillMatch);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("None required")]
        [InlineData("No special skills required")]
        public void SkillMatch_MissingOrNoSkillsRequired_ReturnsOne(string? requiredSkills)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserSkills = null,
                RequiredSkills = requiredSkills
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.SkillMatch);
        }

        [Fact]
        public void SkillMatch_MissingUserSkills_WithRequiredSkills_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserSkills = null,
                RequiredSkills = "Teaching, First Aid"
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.SkillMatch);
        }

        [Theory]
        [InlineData("Must have reliable transportation and be comfortable around animals.")]
        [InlineData("Should enjoy working with children and be patient with beginners")]
        public void SkillMatch_AmbiguousFreeFormRequirements_ReturnsNeutral(string requiredSkills)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserSkills = "Teaching",
                RequiredSkills = requiredSkills
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.SkillMatch);
        }

        [Fact]
        public void SkillMatch_IsCaseInsensitiveAndTrimsWhitespace()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserSkills = "  teaching  ,  FIRST aid  ",
                RequiredSkills = "  Teaching , First Aid  "
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.SkillMatch);
        }

        [Fact]
        public void SkillMatch_DuplicateRequiredSkills_DoNotAffectResult()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserSkills = "Teaching, First Aid",
                RequiredSkills = "Teaching, Teaching, First Aid, First Aid"
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.SkillMatch);
        }

        [Fact]
        public void CategoryHistoryMatch_NullCompletedCauses_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CompletedCauses = null
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.CategoryHistoryMatch);
        }

        [Fact]
        public void CategoryHistoryMatch_EmptyCompletedCauses_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CompletedCauses = Array.Empty<CompletedCause>()
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.CategoryHistoryMatch);
        }

        [Fact]
        public void CategoryHistoryMatch_AllCompletedCausesMatch_ReturnsOne()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 3,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CompletedCauses = new[] { new CompletedCause(3), new CompletedCause(3) }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.CategoryHistoryMatch);
        }

        [Fact]
        public void CategoryHistoryMatch_NoCompletedCausesMatch_ReturnsZero()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 3,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CompletedCauses = new[] { new CompletedCause(1), new CompletedCause(2) }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.0, result.CategoryHistoryMatch);
        }

        [Fact]
        public void CategoryHistoryMatch_PartialMatch_ReturnsRatio()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 3,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CompletedCauses = new[] { new CompletedCause(3), new CompletedCause(2), new CompletedCause(1), new CompletedCause(3) }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.CategoryHistoryMatch);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CategoryHistoryMatch_InvalidCategoryId_ReturnsNeutral(int categoryId)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = categoryId,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                CompletedCauses = new[] { new CompletedCause(3), new CompletedCause(3) }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.CategoryHistoryMatch);
        }

        [Theory]
        [InlineData(0, null, 1.0)]
        [InlineData(0, true, 1.0)]
        [InlineData(0, false, 1.0)]
        [InlineData(1, true, 1.0)]
        [InlineData(1, false, 0.5)]
        [InlineData(1, null, 0.5)]
        [InlineData(2, true, 1.0)]
        [InlineData(2, false, 0.0)]
        [InlineData(2, null, 0.5)]
        public void ExperienceMatch_AllDifficultyAndExperienceCombinations(int difficultyLevel, bool? hasPreviousExperience, double expected)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = difficultyLevel,
                HasPreviousExperience = hasPreviousExperience
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(expected, result.ExperienceMatch);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(100)]
        public void ExperienceMatch_InvalidDifficultyLevel_Throws(int difficultyLevel)
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = difficultyLevel,
                HasPreviousExperience = true
            };

            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationFeatureCalculator.Calculate(input));
        }

        [Fact]
        public void ColleagueMatch_AllOtherParticipantsAreColleagues_ReturnsOne()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = new[]
                {
                    new ColleagueParticipant("user-2", 7),
                    new ColleagueParticipant("user-3", 7)
                }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(1.0, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_SomeOtherParticipantsAreColleagues_ReturnsRatio()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = new[]
                {
                    new ColleagueParticipant("user-2", 7),
                    new ColleagueParticipant("user-3", 8)
                }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_NoOtherParticipantsAreColleagues_ReturnsZero()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = new[]
                {
                    new ColleagueParticipant("user-2", 8),
                    new ColleagueParticipant("user-3", 9)
                }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.0, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_NoOtherParticipants_ReturnsZero()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = Array.Empty<ColleagueParticipant>()
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.0, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_MissingCompanyId_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = null,
                RegisteredParticipants = new[] { new ColleagueParticipant("user-2", 7) }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_MissingRegisteredParticipants_ReturnsNeutral()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = null
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_ExcludesCurrentUser()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = new[]
                {
                    new ColleagueParticipant("user-1", 7),
                    new ColleagueParticipant("user-2", 8)
                }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.0, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_MissingUserId_DoesNotExcludeAnyone()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = null,
                CompanyId = 7,
                RegisteredParticipants = new[]
                {
                    new ColleagueParticipant("user-2", 7),
                    new ColleagueParticipant("user-3", 8)
                }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.ColleagueMatch);
        }

        [Fact]
        public void ColleagueMatch_DuplicateParticipantIds_NotCountedTwice()
        {
            // Arrange
            var input = new RecommendationFeatureInput
            {
                CategoryId = 1,
                CauseStart = new DateTime(2026, 10, 12, 10, 0, 0),
                DifficultyLevel = 0,
                UserId = "user-1",
                CompanyId = 7,
                RegisteredParticipants = new[]
                {
                    new ColleagueParticipant("user-2", 7),
                    new ColleagueParticipant("user-2", 7),
                    new ColleagueParticipant("user-3", 8)
                }
            };

            // Act
            var result = RecommendationFeatureCalculator.Calculate(input);

            // Assert
            Assert.Equal(0.5, result.ColleagueMatch);
        }
    }
}
