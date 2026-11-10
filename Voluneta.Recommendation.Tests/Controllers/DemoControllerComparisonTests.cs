using Microsoft.AspNetCore.Mvc;
using Voluneta_Recommendation_Algorithm.Controllers;
using Voluneta_Recommendation_Algorithm.DemoData;
using Voluneta_Recommendation_Algorithm.Models;

namespace Voluneta.Recommendation.Tests.Controllers
{
    // Before vs After comparison wiring in DemoController.
    // Verifies matching by CauseId, score/rank diff calculation, and reset-hides-comparison.
    public class DemoControllerComparisonTests
    {
        private static DemoController CreateController()
        {
            string contentRoot = AppDomain.CurrentDomain.BaseDirectory;
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
            var service = new DemoRecommendationService(modelProvider);
            var comparisonService = new DemoAllocationComparisonService(service);
            return new DemoController(service, comparisonService);
        }

        private const string VolunteerId = "V1";

        [Fact]
        public void CalculateWithEditedProfile_PopulatesComparison_MatchedByCauseId()
        {
            var controller = CreateController();

            var result = controller.CalculateWithEditedProfile(
                VolunteerId,
                interests: ["Education"],
                skills: ["Teaching", "Communication"],
                days: ["Saturday", "Sunday"],
                timeOfDay: "Evening (17:00+)",
                location: null,
                experience: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RecommendationsViewModel>(viewResult.Model);

            Assert.NotNull(model.Comparison);
            Assert.NotEmpty(model.Comparison);

            var originalCauseIds = DemoDataSet.Causes.Select(c => c.Id).ToHashSet();
            Assert.All(model.Comparison, c => Assert.Contains(c.CauseId, originalCauseIds));

            // No duplicate CauseIds - matching is strictly by CauseId.
            var distinctIds = model.Comparison.Select(c => c.CauseId).Distinct().Count();
            Assert.Equal(model.Comparison.Count, distinctIds);
        }

        [Fact]
        public void CalculateWithEditedProfile_ScoreDifference_MatchesIndependentCalculation()
        {
            var controller = CreateController();
            var service = new DemoRecommendationService(
                new DemoModelProvider(FindModelArtifactsRoot()));

            var originalResults = service.GenerateRankedRecommendations()
                .Where(r => r.VolunteerId == VolunteerId)
                .ToDictionary(r => r.CauseId);

            var result = controller.CalculateWithEditedProfile(
                VolunteerId,
                interests: ["Education"],
                skills: ["Teaching", "Communication"],
                days: ["Saturday", "Sunday"],
                timeOfDay: "Evening (17:00+)",
                location: null,
                experience: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RecommendationsViewModel>(viewResult.Model);

            Assert.NotNull(model.Comparison);
            foreach (var comparison in model.Comparison)
            {
                if (originalResults.TryGetValue(comparison.CauseId, out var original))
                {
                    Assert.Equal(original.Score, comparison.OriginalScore!.Value, precision: 10);
                    Assert.Equal(original.Rank, comparison.OriginalRank!.Value);

                    if (comparison.EditedScore.HasValue)
                    {
                        double expectedDiff = Math.Round((comparison.EditedScore.Value - original.Score) * 100, 2);
                        Assert.Equal(expectedDiff, comparison.ScoreDifferencePercentPoints!.Value, precision: 10);
                    }
                }
                else
                {
                    Assert.Null(comparison.OriginalScore);
                }
            }
        }

        [Fact]
        public void CalculateWithEditedProfile_AtLeastOneCause_HasNonZeroScoreDifference()
        {
            var controller = CreateController();

            var result = controller.CalculateWithEditedProfile(
                VolunteerId,
                interests: ["Education"],
                skills: ["Teaching", "Communication"],
                days: ["Saturday", "Sunday"],
                timeOfDay: "Evening (17:00+)",
                location: null,
                experience: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RecommendationsViewModel>(viewResult.Model);

            Assert.NotNull(model.Comparison);
            Assert.Contains(model.Comparison, c => c.ScoreDifferencePercentPoints is not (null or 0.0));
        }

        [Fact]
        public void Recommendations_Reset_HidesComparison()
        {
            var controller = CreateController();

            // Reset / original recalculation action: no profile override.
            var result = controller.Recommendations(VolunteerId, calculate: true);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RecommendationsViewModel>(viewResult.Model);

            Assert.Null(model.Comparison);
        }

        [Fact]
        public void Recommendations_InitialGet_HasNoComparison()
        {
            var controller = CreateController();

            var result = controller.Recommendations(VolunteerId);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RecommendationsViewModel>(viewResult.Model);

            Assert.Null(model.Comparison);
        }

        private static string FindModelArtifactsRoot()
        {
            string contentRoot = AppDomain.CurrentDomain.BaseDirectory;
            var directory = new DirectoryInfo(contentRoot);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ModelArtifacts", "recommendation-model.json")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Could not locate ModelArtifacts/recommendation-model.json from test output directory.");
            }

            return directory.FullName;
        }
    }
}
