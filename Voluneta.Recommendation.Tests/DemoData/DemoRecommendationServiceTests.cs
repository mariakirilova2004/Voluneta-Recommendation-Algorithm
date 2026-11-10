using Voluneta_Recommendation_Algorithm.DemoData;

namespace Voluneta.Recommendation.Tests.DemoData
{
    public class DemoRecommendationServiceTests
    {
        private static DemoRecommendationService CreateService()
        {
            string contentRoot = AppDomain.CurrentDomain.BaseDirectory;

            // Walk up from the test output directory to the repo root, where ModelArtifacts lives.
            var directory = new DirectoryInfo(contentRoot);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ModelArtifacts", "recommendation-model.json")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Could not locate ModelArtifacts/recommendation-model.json from test output directory.");
            }

            var modelProvider = new DemoModelProvider(directory.FullName);
            return new DemoRecommendationService(modelProvider);
        }

        [Fact]
        public void GenerateRankedRecommendations_ReturnsExpectedResultCount()
        {
            var service = CreateService();

            var results = service.GenerateRankedRecommendations();

            // 4 demo volunteers x 3 demo causes, all eligible (RemainingPlaces > 0 for all causes).
            Assert.Equal(12, results.Count);
        }

        [Fact]
        public void GenerateRankedRecommendations_AllScoresAreFiniteAndInRange()
        {
            var service = CreateService();

            var results = service.GenerateRankedRecommendations();

            Assert.All(results, r =>
            {
                Assert.True(double.IsFinite(r.Score));
                Assert.InRange(r.Score, 0.0, 1.0);
                Assert.Equal(8, r.Features.Length);
                Assert.All(r.Features, f => Assert.True(double.IsFinite(f)));
            });
        }

        [Fact]
        public void GenerateRankedRecommendations_RanksEachVolunteersCausesByDescendingScore()
        {
            var service = CreateService();

            var results = service.GenerateRankedRecommendations();

            foreach (var group in results.GroupBy(r => r.VolunteerId))
            {
                var ordered = group.OrderBy(r => r.Rank).ToList();

                // Ranks are contiguous starting at 1.
                Assert.Equal(Enumerable.Range(1, ordered.Count), ordered.Select(r => r.Rank));

                // Scores are non-increasing as rank increases.
                for (int i = 1; i < ordered.Count; i++)
                {
                    Assert.True(ordered[i - 1].Score >= ordered[i].Score);
                }
            }
        }

        [Fact]
        public void GenerateRankedRecommendations_IsDeterministicAcrossRuns()
        {
            var service = CreateService();

            var first = service.GenerateRankedRecommendations();
            var second = service.GenerateRankedRecommendations();

            Assert.Equal(first.Count, second.Count);

            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i].VolunteerId, second[i].VolunteerId);
                Assert.Equal(first[i].CauseId, second[i].CauseId);
                Assert.Equal(first[i].Rank, second[i].Rank);
                Assert.Equal(first[i].Score, second[i].Score, precision: 10);
            }
        }

        [Fact]
        public void GenerateRankedRecommendations_WithVolunteerOverride_ChangesOnlyThatVolunteersScores()
        {
            var service = CreateService();
            var baseline = service.GenerateRankedRecommendations();

            var originalV1 = DemoDataSet.Volunteers.First(v => v.Id == "V1");
            var overrideVolunteer = new DemoVolunteer
            {
                Id = originalV1.Id,
                Name = originalV1.Name,
                Interests = "Education",
                DaysAvailable = "Saturday,Sunday",
                PreferredTimeOfDay = "Evening (17:00+)",
                Skills = "Teaching,Communication",
                HasPreviousExperience = originalV1.HasPreviousExperience,
                CompanyId = originalV1.CompanyId,
                Latitude = originalV1.Latitude,
                Longitude = originalV1.Longitude,
                CompletedCauseCategoryIds = originalV1.CompletedCauseCategoryIds
            };

            var edited = service.GenerateRankedRecommendations(overrideVolunteer);

            Assert.Equal(baseline.Count, edited.Count);

            var baselineOthers = baseline.Where(r => r.VolunteerId != "V1").OrderBy(r => r.VolunteerId).ThenBy(r => r.CauseId).ToList();
            var editedOthers = edited.Where(r => r.VolunteerId != "V1").OrderBy(r => r.VolunteerId).ThenBy(r => r.CauseId).ToList();

            Assert.Equal(baselineOthers.Count, editedOthers.Count);
            for (int i = 0; i < baselineOthers.Count; i++)
            {
                Assert.Equal(baselineOthers[i].Score, editedOthers[i].Score, precision: 10);
            }

            var baselineV1 = baseline.Where(r => r.VolunteerId == "V1").OrderBy(r => r.CauseId).Select(r => r.Score).ToList();
            var editedV1 = edited.Where(r => r.VolunteerId == "V1").OrderBy(r => r.CauseId).Select(r => r.Score).ToList();

            Assert.NotEqual(baselineV1, editedV1);
        }

        [Fact]
        public void GenerateRankedRecommendations_WithVolunteerOverride_DoesNotMutateDemoDataSet()
        {
            var service = CreateService();
            var originalV1 = DemoDataSet.Volunteers.First(v => v.Id == "V1");
            string originalInterests = originalV1.Interests;
            string originalSkills = originalV1.Skills;
            string originalDays = originalV1.DaysAvailable;
            string originalTime = originalV1.PreferredTimeOfDay;

            var overrideVolunteer = new DemoVolunteer
            {
                Id = originalV1.Id,
                Name = originalV1.Name,
                Interests = "Health",
                DaysAvailable = "Tuesday",
                PreferredTimeOfDay = "Flexible",
                Skills = "Driving",
                HasPreviousExperience = originalV1.HasPreviousExperience,
                CompanyId = originalV1.CompanyId,
                Latitude = originalV1.Latitude,
                Longitude = originalV1.Longitude,
                CompletedCauseCategoryIds = originalV1.CompletedCauseCategoryIds
            };

            service.GenerateRankedRecommendations(overrideVolunteer);

            var afterV1 = DemoDataSet.Volunteers.First(v => v.Id == "V1");
            Assert.Equal(originalInterests, afterV1.Interests);
            Assert.Equal(originalSkills, afterV1.Skills);
            Assert.Equal(originalDays, afterV1.DaysAvailable);
            Assert.Equal(originalTime, afterV1.PreferredTimeOfDay);
        }

        [Fact]
        public void GenerateRankedRecommendations_NullOverride_MatchesOriginalProfileScores()
        {
            var service = CreateService();

            var baseline = service.GenerateRankedRecommendations();
            var resetResults = service.GenerateRankedRecommendations(volunteerOverride: null);

            Assert.Equal(baseline.Count, resetResults.Count);
            for (int i = 0; i < baseline.Count; i++)
            {
                Assert.Equal(baseline[i].Score, resetResults[i].Score, precision: 10);
            }
        }

        [Fact]
        public void GenerateRankedRecommendations_WithLocationAndExperienceOverride_ChangesOnlyThatVolunteersScores()
        {
            var service = CreateService();
            var baseline = service.GenerateRankedRecommendations();

            var originalV1 = DemoDataSet.Volunteers.First(v => v.Id == "V1");
            var overrideVolunteer = new DemoVolunteer
            {
                Id = originalV1.Id,
                Name = originalV1.Name,
                Interests = originalV1.Interests,
                DaysAvailable = originalV1.DaysAvailable,
                PreferredTimeOfDay = originalV1.PreferredTimeOfDay,
                Skills = originalV1.Skills,
                HasPreviousExperience = !originalV1.HasPreviousExperience,
                CompanyId = originalV1.CompanyId,
                Latitude = 43.2141,
                Longitude = 27.9147,
                CompletedCauseCategoryIds = originalV1.CompletedCauseCategoryIds
            };

            var edited = service.GenerateRankedRecommendations(overrideVolunteer);

            Assert.Equal(baseline.Count, edited.Count);

            var baselineOthers = baseline.Where(r => r.VolunteerId != "V1").OrderBy(r => r.VolunteerId).ThenBy(r => r.CauseId).ToList();
            var editedOthers = edited.Where(r => r.VolunteerId != "V1").OrderBy(r => r.VolunteerId).ThenBy(r => r.CauseId).ToList();

            Assert.Equal(baselineOthers.Count, editedOthers.Count);
            for (int i = 0; i < baselineOthers.Count; i++)
            {
                Assert.Equal(baselineOthers[i].Score, editedOthers[i].Score, precision: 10);
            }

            var baselineV1 = baseline.Where(r => r.VolunteerId == "V1").OrderBy(r => r.CauseId).Select(r => r.Score).ToList();
            var editedV1 = edited.Where(r => r.VolunteerId == "V1").OrderBy(r => r.CauseId).Select(r => r.Score).ToList();

            Assert.NotEqual(baselineV1, editedV1);
        }
    }
}
