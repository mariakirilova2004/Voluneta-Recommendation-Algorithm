using Microsoft.AspNetCore.Mvc;
using Voluneta_Recommendation_Algorithm.Controllers;
using Voluneta_Recommendation_Algorithm.DemoData;
using Voluneta_Recommendation_Algorithm.Models;

namespace Voluneta.Recommendation.Tests.Controllers
{
    // Algorithm 2 demo wiring in DemoController: valid comparison runs, invalid form input
    // handling (negative capacity, out-of-range threshold), and reset behavior. Never mutates
    // DemoDataSet or application singletons.
    public class DemoControllerOptimizationTests
    {
        private static DemoController CreateController()
        {
            var modelProvider = new DemoModelProvider(FindModelArtifactsRoot());
            var service = new DemoRecommendationService(modelProvider);
            var comparisonService = new DemoAllocationComparisonService(service);
            return new DemoController(service, comparisonService);
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

        [Fact]
        public void Optimization_Get_ReturnsResult_ForDefaultScenario()
        {
            var controller = CreateController();

            var result = controller.Optimization(scenario: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<OptimizationViewModel>(viewResult.Model);

            Assert.NotNull(model.Result);
            Assert.Empty(model.Errors);
        }

        [Fact]
        public void Optimization_Post_NegativeCapacity_ReturnsErrors_WithoutThrowing_AndNoResult()
        {
            var controller = CreateController();
            var causeId = DemoDataSet.Causes.First().Id;

            var result = controller.Optimization(
                capacity: new Dictionary<string, int> { [causeId] = -1 },
                minimumScore: 0.50,
                scenario: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<OptimizationViewModel>(viewResult.Model);

            Assert.NotEmpty(model.Errors);
            Assert.Null(model.Result);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(double.NaN)]
        public void Optimization_Post_InvalidMinimumScore_ReturnsErrors_WithoutThrowing_AndNoResult(double minimumScore)
        {
            var controller = CreateController();

            var result = controller.Optimization(
                capacity: [],
                minimumScore: minimumScore,
                scenario: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<OptimizationViewModel>(viewResult.Model);

            Assert.NotEmpty(model.Errors);
            Assert.Null(model.Result);
        }

        [Fact]
        public void Optimization_Post_UnknownCauseIdInCapacity_IsIgnored_AndDoesNotThrow()
        {
            var controller = CreateController();

            var result = controller.Optimization(
                capacity: new Dictionary<string, int> { ["does-not-exist"] = 5 },
                minimumScore: 0.50,
                scenario: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<OptimizationViewModel>(viewResult.Model);

            Assert.Empty(model.Errors);
            Assert.NotNull(model.Result);
        }

        [Fact]
        public void ResetOptimization_RedirectsToOptimization_WithSameScenario()
        {
            var controller = CreateController();

            var result = controller.ResetOptimization(scenario: "Educational");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(DemoController.Optimization), redirect.ActionName);
            Assert.Equal("Educational", redirect.RouteValues?["scenario"]);
        }

        [Fact]
        public void Optimization_Post_DoesNotMutateDemoDataSet()
        {
            var controller = CreateController();
            var causeId = DemoDataSet.Causes.First().Id;
            int originalRemainingPlaces = DemoDataSet.Causes.First().RemainingPlaces;

            controller.Optimization(
                capacity: new Dictionary<string, int> { [causeId] = originalRemainingPlaces + 100 },
                minimumScore: 0.30,
                scenario: null);

            Assert.Equal(originalRemainingPlaces, DemoDataSet.Causes.First().RemainingPlaces);
        }
    }
}
