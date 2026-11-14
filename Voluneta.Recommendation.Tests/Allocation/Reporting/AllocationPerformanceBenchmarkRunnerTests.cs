using Voluneta.Recommendation.Core.Algorithms.Allocation.Reporting;

namespace Voluneta.Recommendation.Tests.Allocation.Reporting
{
    // Focused tests for the Algorithm 2 performance benchmark: scenario shape, deterministic
    // input generation, and capacity constraints. Intentionally avoids strict timing assertions
    // (e.g. "must complete under X ms") since measured durations are environment-specific. Does
    // not modify MinCostMaxFlowPriorityAllocator or GreedyPriorityAllocator.
    public class AllocationPerformanceBenchmarkRunnerTests
    {
        [Fact]
        public void BuildScenarios_ReturnsFourScenarios_WithExpectedSizes()
        {
            var scenarios = AllocationPerformanceBenchmarkRunner.BuildScenarios();

            Assert.Equal(4, scenarios.Count);

            Assert.Equal(10, scenarios[0].VolunteerCount);
            Assert.Equal(5, scenarios[0].CauseCount);

            Assert.Equal(25, scenarios[1].VolunteerCount);
            Assert.Equal(10, scenarios[1].CauseCount);

            Assert.Equal(50, scenarios[2].VolunteerCount);
            Assert.Equal(15, scenarios[2].CauseCount);

            Assert.Equal(100, scenarios[3].VolunteerCount);
            Assert.Equal(20, scenarios[3].CauseCount);
        }

        [Fact]
        public void BuildScenarios_IsDeterministic_AcrossRepeatedCalls()
        {
            var firstRun = AllocationPerformanceBenchmarkRunner.BuildScenarios();
            var secondRun = AllocationPerformanceBenchmarkRunner.BuildScenarios();

            Assert.Equal(firstRun.Count, secondRun.Count);

            for (int i = 0; i < firstRun.Count; i++)
            {
                Assert.Equal(firstRun[i].Name, secondRun[i].Name);
                Assert.Equal(firstRun[i].Seed, secondRun[i].Seed);
                Assert.Equal(firstRun[i].CandidateEdgeCount, secondRun[i].CandidateEdgeCount);

                Assert.Equal(firstRun[i].Candidates.Count, secondRun[i].Candidates.Count);
                for (int j = 0; j < firstRun[i].Candidates.Count; j++)
                {
                    Assert.Equal(firstRun[i].Candidates[j].VolunteerId, secondRun[i].Candidates[j].VolunteerId);
                    Assert.Equal(firstRun[i].Candidates[j].CauseId, secondRun[i].Candidates[j].CauseId);
                    Assert.Equal(firstRun[i].Candidates[j].CompatibilityScore, secondRun[i].Candidates[j].CompatibilityScore);
                }

                Assert.Equal(firstRun[i].Capacities.Count, secondRun[i].Capacities.Count);
                for (int j = 0; j < firstRun[i].Capacities.Count; j++)
                {
                    Assert.Equal(firstRun[i].Capacities[j].CauseId, secondRun[i].Capacities[j].CauseId);
                    Assert.Equal(firstRun[i].Capacities[j].RemainingPlaces, secondRun[i].Capacities[j].RemainingPlaces);
                }
            }
        }

        [Fact]
        public void BuildScenarios_EveryVolunteerHasAtLeastOneCandidate()
        {
            var scenarios = AllocationPerformanceBenchmarkRunner.BuildScenarios();

            foreach (var scenario in scenarios)
            {
                var volunteerIdsWithCandidates = scenario.Candidates.Select(c => c.VolunteerId).Distinct().Count();
                Assert.Equal(scenario.VolunteerCount, volunteerIdsWithCandidates);
            }
        }

        [Fact]
        public void Run_ProducesTwoRowsPerScenario_ForBothAlgorithms()
        {
            var scenarios = AllocationPerformanceBenchmarkRunner.BuildScenarios();
            var rows = AllocationPerformanceBenchmarkRunner.Run(scenarios);

            Assert.Equal(scenarios.Count * 2, rows.Count);

            foreach (var scenario in scenarios)
            {
                var scenarioRows = rows.Where(r => r.ScenarioName == scenario.Name).ToList();
                Assert.Equal(2, scenarioRows.Count);
                Assert.Contains(scenarioRows, r => r.AlgorithmName == "Min-Cost Max-Flow");
                Assert.Contains(scenarioRows, r => r.AlgorithmName == "Greedy");
            }
        }

        [Fact]
        public void Run_ReportsNonNegativeTimingsAndRespectsCapacityConstraints()
        {
            // Use only the smallest scenario to keep this test fast; still exercises the full
            // measured path (warm-up + 5 repetitions) for both algorithms.
            var smallScenario = AllocationPerformanceBenchmarkRunner.BuildScenarios()[0];
            var rows = AllocationPerformanceBenchmarkRunner.Run([smallScenario]);

            foreach (var row in rows)
            {
                // No strict timing thresholds asserted - only basic sanity checks that durations
                // are well-formed, since absolute timings are environment-specific.
                Assert.True(row.MedianMilliseconds >= 0);
                Assert.True(row.MeanMilliseconds >= 0);
                Assert.True(row.MinMilliseconds >= 0);
                Assert.True(row.MinMilliseconds <= row.MedianMilliseconds || row.Repetitions == 1);
                Assert.Equal(5, row.Repetitions);

                int totalCapacity = smallScenario.Capacities.Sum(c => c.RemainingPlaces);
                Assert.True(row.PrioritizedVolunteerCount <= totalCapacity);
                Assert.True(row.PrioritizedVolunteerCount <= smallScenario.VolunteerCount);
            }
        }
    }

    public class AllocationPerformanceReportGeneratorTests : IDisposable
    {
        private readonly string _outputDirectory;

        public AllocationPerformanceReportGeneratorTests()
        {
            _outputDirectory = Path.Combine(Path.GetTempPath(), "allocation_performance_tests_" + Guid.NewGuid());
        }

        [Fact]
        public void GenerateAndSave_WritesCsvAndMarkdownArtifacts()
        {
            var (scenarios, rows) = AllocationPerformanceReportGenerator.GenerateAndSave(_outputDirectory);

            string csvPath = Path.Combine(_outputDirectory, "allocation_performance.csv");
            string reportPath = Path.Combine(_outputDirectory, "allocation_performance_report.md");

            Assert.True(File.Exists(csvPath));
            Assert.True(File.Exists(reportPath));

            string csv = File.ReadAllText(csvPath);
            var csvLines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("scenario,seed,volunteer_count,cause_count,candidate_edge_count,threshold,algorithm,repetitions,median_ms,mean_ms,min_ms,prioritized_count", csvLines[0].TrimEnd('\r'));
            Assert.Equal(rows.Count, csvLines.Length - 1);

            string report = File.ReadAllText(reportPath);
            Assert.Contains("Min-Cost Max-Flow", report);
            Assert.Contains("Greedy", report);
            Assert.Contains("specific to this environment", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("does not claim", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".NET runtime", report);

            foreach (var scenario in scenarios)
            {
                Assert.Contains(scenario.Name, report);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_outputDirectory))
            {
                Directory.Delete(_outputDirectory, recursive: true);
            }
        }
    }
}
