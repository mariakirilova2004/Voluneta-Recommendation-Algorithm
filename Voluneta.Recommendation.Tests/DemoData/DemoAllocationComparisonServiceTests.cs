using Voluneta_Recommendation_Algorithm.DemoData;

namespace Voluneta.Recommendation.Tests.DemoData
{
    // Algorithm 2 demo comparison service.
    // Verifies capacity constraints, one priority per volunteer, determinism, that
    // candidate scores match the real Algorithm 1 output, and that demo data is untouched.
    public class DemoAllocationComparisonServiceTests
    {
        private static DemoRecommendationService CreateRecommendationService()
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
            return new DemoRecommendationService(modelProvider);
        }

        private static DemoAllocationComparisonService CreateService() =>
            new(CreateRecommendationService());

        [Fact]
        public void Compare_CandidateScores_MatchDemoRecommendationService()
        {
            var recommendationService = CreateRecommendationService();
            var service = new DemoAllocationComparisonService(recommendationService);

            var expectedScores = recommendationService.GenerateRankedRecommendations()
                .ToDictionary(r => (r.VolunteerId, r.CauseId), r => r.Score);

            var result = service.Compare();

            Assert.Equal(expectedScores.Count, result.AllCandidates.Count);
            foreach (var candidate in result.AllCandidates)
            {
                var expected = expectedScores[(candidate.VolunteerId, candidate.CauseId)];
                Assert.Equal(expected, candidate.CompatibilityScore, precision: 10);
                Assert.True(candidate.IsEligible);
            }
        }

        [Fact]
        public void Compare_BothAlgorithms_RespectCauseCapacity()
        {
            var service = CreateService();

            var result = service.Compare();

            foreach (var cause in DemoDataSet.Causes)
            {
                int minCostMaxFlowCount = result.MinCostMaxFlow.SelectedPriorities.Count(p => p.CauseId == cause.Id);
                int greedyCount = result.Greedy.SelectedPriorities.Count(p => p.CauseId == cause.Id);

                Assert.True(minCostMaxFlowCount <= cause.RemainingPlaces);
                Assert.True(greedyCount <= cause.RemainingPlaces);
            }
        }

        [Fact]
        public void Compare_BothAlgorithms_SelectAtMostOnePriorityPerVolunteer()
        {
            var service = CreateService();

            var result = service.Compare();

            var minCostMaxFlowVolunteerCounts = result.MinCostMaxFlow.SelectedPriorities
                .GroupBy(p => p.VolunteerId)
                .Select(g => g.Count());
            var greedyVolunteerCounts = result.Greedy.SelectedPriorities
                .GroupBy(p => p.VolunteerId)
                .Select(g => g.Count());

            Assert.All(minCostMaxFlowVolunteerCounts, count => Assert.Equal(1, count));
            Assert.All(greedyVolunteerCounts, count => Assert.Equal(1, count));
        }

        [Fact]
        public void Compare_IsDeterministicAcrossRuns()
        {
            var service = CreateService();

            var first = service.Compare();
            var second = service.Compare();

            Assert.Equal(first.MinCostMaxFlow.PrioritizedVolunteerCount, second.MinCostMaxFlow.PrioritizedVolunteerCount);
            Assert.Equal(first.MinCostMaxFlow.TotalCompatibilityScore, second.MinCostMaxFlow.TotalCompatibilityScore, precision: 10);
            Assert.Equal(first.Greedy.PrioritizedVolunteerCount, second.Greedy.PrioritizedVolunteerCount);
            Assert.Equal(first.Greedy.TotalCompatibilityScore, second.Greedy.TotalCompatibilityScore, precision: 10);

            var firstPairs = first.MinCostMaxFlow.SelectedPriorities.Select(p => (p.VolunteerId, p.CauseId)).OrderBy(p => p.VolunteerId).ToList();
            var secondPairs = second.MinCostMaxFlow.SelectedPriorities.Select(p => (p.VolunteerId, p.CauseId)).OrderBy(p => p.VolunteerId).ToList();
            Assert.Equal(firstPairs, secondPairs);
        }

        [Fact]
        public void Compare_DoesNotMutateDemoDataSet()
        {
            var service = CreateService();
            var originalRemainingPlaces = DemoDataSet.Causes.ToDictionary(c => c.Id, c => c.RemainingPlaces);

            var overrides = DemoDataSet.Causes.ToDictionary(c => c.Id, c => 0);
            service.Compare(overrides);

            foreach (var cause in DemoDataSet.Causes)
            {
                Assert.Equal(originalRemainingPlaces[cause.Id], cause.RemainingPlaces);
            }
        }

        [Fact]
        public void Compare_WithCapacityOverride_ZeroCapacity_ExcludesCauseFromPriorities()
        {
            var service = CreateService();
            var targetCause = DemoDataSet.Causes.First();

            var overrides = new Dictionary<string, int> { [targetCause.Id] = 0 };
            var result = service.Compare(overrides);

            Assert.DoesNotContain(result.MinCostMaxFlow.SelectedPriorities, p => p.CauseId == targetCause.Id);
            Assert.DoesNotContain(result.Greedy.SelectedPriorities, p => p.CauseId == targetCause.Id);
            Assert.Equal(0, result.RemainingCapacityByCauseId[targetCause.Id]);
        }

        [Fact]
        public void Compare_NegativeCapacityOverride_Throws()
        {
            var service = CreateService();
            var targetCause = DemoDataSet.Causes.First();
            var overrides = new Dictionary<string, int> { [targetCause.Id] = -1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => service.Compare(overrides));
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void Compare_InvalidMinimumScore_Throws(double minimumScore)
        {
            var service = CreateService();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.Compare(minimumScore: minimumScore));
        }

        [Fact]
        public void Compare_DefaultScenario_ProducesNonEmptyComparableResults()
        {
            var service = CreateService();

            var result = service.Compare();

            Assert.NotEmpty(result.AllCandidates);
            Assert.True(result.MinCostMaxFlow.PrioritizedVolunteerCount >= result.Greedy.PrioritizedVolunteerCount
                || result.MinCostMaxFlow.TotalCompatibilityScore >= result.Greedy.TotalCompatibilityScore - 1e-9);
            Assert.Equal(DemoDataSet.Causes.Count, result.RemainingCapacityByCauseId.Count);
        }

        [Fact]
        public void Compare_EducationalScenario_ProducesExpectedTotals()
        {
            var service = CreateService();

            var result = service.Compare(scenario: DemoOptimizationScenario.Educational);

            Assert.Equal(3, result.Greedy.PrioritizedVolunteerCount);
            Assert.Equal(2.73, result.Greedy.TotalCompatibilityScore, precision: 2);

            Assert.Equal(4, result.MinCostMaxFlow.PrioritizedVolunteerCount);
            Assert.Equal(3.64, result.MinCostMaxFlow.TotalCompatibilityScore, precision: 2);
        }

        [Fact]
        public void Compare_EducationalScenario_UsesConstructedScoresNotModelPredictions()
        {
            var recommendationService = CreateRecommendationService();
            var service = new DemoAllocationComparisonService(recommendationService);

            var result = service.Compare(scenario: DemoOptimizationScenario.Educational);

            foreach (var candidate in result.AllCandidates)
            {
                var expected = DemoEducationalScenario.CompatibilityScores[candidate.VolunteerId][candidate.CauseId];
                Assert.Equal(expected, candidate.CompatibilityScore, precision: 10);
            }
        }

        [Fact]
        public void Compare_EducationalScenario_BothAlgorithms_RespectDefaultCapacities()
        {
            var service = CreateService();

            var result = service.Compare(scenario: DemoOptimizationScenario.Educational);

            foreach (var (causeId, defaultCapacity) in DemoEducationalScenario.DefaultRemainingPlaces)
            {
                int minCostMaxFlowCount = result.MinCostMaxFlow.SelectedPriorities.Count(p => p.CauseId == causeId);
                int greedyCount = result.Greedy.SelectedPriorities.Count(p => p.CauseId == causeId);

                Assert.True(minCostMaxFlowCount <= defaultCapacity);
                Assert.True(greedyCount <= defaultCapacity);
            }
        }

        [Fact]
        public void Compare_EducationalScenario_DoesNotMutateDemoDataSet()
        {
            var service = CreateService();
            var originalRemainingPlaces = DemoDataSet.Causes.ToDictionary(c => c.Id, c => c.RemainingPlaces);

            service.Compare(scenario: DemoOptimizationScenario.Educational);

            foreach (var cause in DemoDataSet.Causes)
            {
                Assert.Equal(originalRemainingPlaces[cause.Id], cause.RemainingPlaces);
            }
        }

        [Fact]
        public void Compare_SwitchingScenarios_DoesNotMutateDemoDataSet()
        {
            var service = CreateService();
            var originalRemainingPlaces = DemoDataSet.Causes.ToDictionary(c => c.Id, c => c.RemainingPlaces);

            service.Compare(scenario: DemoOptimizationScenario.NeuralNetwork);
            service.Compare(scenario: DemoOptimizationScenario.Educational);
            service.Compare(scenario: DemoOptimizationScenario.NeuralNetwork);

            foreach (var cause in DemoDataSet.Causes)
            {
                Assert.Equal(originalRemainingPlaces[cause.Id], cause.RemainingPlaces);
            }
        }

        [Fact]
        public void Compare_NeuralNetworkScenario_StillUsesTrainedModelPredictions()
        {
            var recommendationService = CreateRecommendationService();
            var service = new DemoAllocationComparisonService(recommendationService);

            var expectedScores = recommendationService.GenerateRankedRecommendations()
                .ToDictionary(r => (r.VolunteerId, r.CauseId), r => r.Score);

            var result = service.Compare(scenario: DemoOptimizationScenario.NeuralNetwork);

            foreach (var candidate in result.AllCandidates)
            {
                var expected = expectedScores[(candidate.VolunteerId, candidate.CauseId)];
                Assert.Equal(expected, candidate.CompatibilityScore, precision: 10);
            }
        }
    }
}
