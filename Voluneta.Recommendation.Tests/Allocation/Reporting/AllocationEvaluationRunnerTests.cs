using Voluneta.Recommendation.Core.Algorithms.Allocation.Reporting;

namespace Voluneta.Recommendation.Tests.Allocation.Reporting
{
    // Focused tests for the Algorithm 2 evaluation runner/report generator: known expected
    // results for the educational scenario, deterministic/reproducible execution, and capacity
    // constraints. Does not modify MinCostMaxFlowPriorityAllocator or GreedyPriorityAllocator.
    public class AllocationEvaluationRunnerTests
    {
        [Fact]
        public void BuildScenarios_ReturnsEducationalScenarioFirst_WithExpectedShape()
        {
            var scenarios = AllocationEvaluationRunner.BuildScenarios();

            var educational = scenarios[0];
            Assert.Equal("Educational Greedy vs Optimal", educational.Name);
            Assert.Equal(4, educational.VolunteerCount);
            Assert.Equal(3, educational.CauseCount);
            Assert.Equal(4, educational.TotalCapacity);
            Assert.Null(educational.Seed);
            Assert.Contains("not neural network predictions", educational.ScoreSource);
        }

        [Fact]
        public void Run_EducationalScenario_AtDefaultThreshold_MatchesKnownExpectedResults()
        {
            var scenarios = AllocationEvaluationRunner.BuildScenarios();
            var educational = scenarios.Single(s => s.Name == "Educational Greedy vs Optimal");

            var rows = AllocationEvaluationRunner.Run([educational]);

            var greedyAt050 = rows.Single(r => r.AlgorithmName == "Greedy" && r.Threshold == 0.50);
            var minCostMaxFlowAt050 = rows.Single(r => r.AlgorithmName == "Min-Cost Max-Flow" && r.Threshold == 0.50);

            // Known expected results for this hand-authored educational matrix, matching the
            // existing Optimization demo's educational scenario at the 0.50 threshold.
            Assert.Equal(3, greedyAt050.PrioritizedVolunteerCount);
            Assert.Equal(2.73, greedyAt050.TotalCompatibilityScore, 2);

            Assert.Equal(4, minCostMaxFlowAt050.PrioritizedVolunteerCount);
            Assert.Equal(3.64, minCostMaxFlowAt050.TotalCompatibilityScore, 2);
        }

        [Fact]
        public void Run_IsDeterministic_AcrossRepeatedCalls()
        {
            var scenariosFirstRun = AllocationEvaluationRunner.BuildScenarios();
            var scenariosSecondRun = AllocationEvaluationRunner.BuildScenarios();

            var rowsFirstRun = AllocationEvaluationRunner.Run(scenariosFirstRun);
            var rowsSecondRun = AllocationEvaluationRunner.Run(scenariosSecondRun);

            Assert.Equal(rowsFirstRun.Count, rowsSecondRun.Count);

            for (int i = 0; i < rowsFirstRun.Count; i++)
            {
                Assert.Equal(rowsFirstRun[i].ScenarioName, rowsSecondRun[i].ScenarioName);
                Assert.Equal(rowsFirstRun[i].AlgorithmName, rowsSecondRun[i].AlgorithmName);
                Assert.Equal(rowsFirstRun[i].Threshold, rowsSecondRun[i].Threshold);
                Assert.Equal(rowsFirstRun[i].PrioritizedVolunteerCount, rowsSecondRun[i].PrioritizedVolunteerCount);
                Assert.Equal(rowsFirstRun[i].TotalCompatibilityScore, rowsSecondRun[i].TotalCompatibilityScore, 10);
            }
        }

        [Theory]
        [InlineData(0.30)]
        [InlineData(0.50)]
        [InlineData(0.70)]
        public void Run_NeverExceedsTotalCapacity_ForEitherAlgorithm(double threshold)
        {
            var scenarios = AllocationEvaluationRunner.BuildScenarios();
            var rows = AllocationEvaluationRunner.Run(scenarios).Where(r => r.Threshold == threshold);

            foreach (var row in rows)
            {
                Assert.True(row.PrioritizedVolunteerCount <= row.TotalCapacity,
                    $"{row.ScenarioName}/{row.AlgorithmName}/{threshold}: prioritized count {row.PrioritizedVolunteerCount} exceeded total capacity {row.TotalCapacity}.");

                Assert.InRange(row.CapacityUtilization, 0.0, 1.0);
            }
        }

        [Fact]
        public void Run_SyntheticScenarios_HaveExpectedSizesAndAreLabeledSynthetic()
        {
            var scenarios = AllocationEvaluationRunner.BuildScenarios();
            var syntheticScenarios = scenarios.Where(s => s.Seed.HasValue).ToList();

            Assert.Equal(3, syntheticScenarios.Count);

            foreach (var scenario in syntheticScenarios)
            {
                Assert.Contains("synthetic", scenario.ScoreSource, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("not neural network predictions", scenario.ScoreSource);
                Assert.True(scenario.VolunteerCount > 0);
                Assert.True(scenario.CauseCount > 0);
                Assert.True(scenario.TotalCapacity > 0);
            }
        }

        [Fact]
        public void HigherThreshold_NeverIncreasesPrioritizedCount_ForEitherAlgorithm()
        {
            var scenarios = AllocationEvaluationRunner.BuildScenarios();
            var rows = AllocationEvaluationRunner.Run(scenarios);

            foreach (var scenario in scenarios)
            {
                foreach (var algorithm in new[] { "Greedy", "Min-Cost Max-Flow" })
                {
                    var byThreshold = rows
                        .Where(r => r.ScenarioName == scenario.Name && r.AlgorithmName == algorithm)
                        .OrderBy(r => r.Threshold)
                        .Select(r => r.PrioritizedVolunteerCount)
                        .ToList();

                    for (int i = 1; i < byThreshold.Count; i++)
                    {
                        Assert.True(byThreshold[i] <= byThreshold[i - 1],
                            $"{scenario.Name}/{algorithm}: prioritized count increased as threshold increased.");
                    }
                }
            }
        }
    }

    public class AllocationEvaluationReportGeneratorTests : IDisposable
    {
        private readonly string _outputDirectory;

        public AllocationEvaluationReportGeneratorTests()
        {
            _outputDirectory = Path.Combine(Path.GetTempPath(), "allocation_evaluation_tests_" + Guid.NewGuid());
        }

        [Fact]
        public void GenerateAndSave_WritesCsvAndMarkdownArtifacts()
        {
            var (scenarios, rows) = AllocationEvaluationReportGenerator.GenerateAndSave(_outputDirectory);

            string csvPath = Path.Combine(_outputDirectory, "allocation_evaluation.csv");
            string reportPath = Path.Combine(_outputDirectory, "allocation_evaluation_report.md");

            Assert.True(File.Exists(csvPath));
            Assert.True(File.Exists(reportPath));

            string csv = File.ReadAllText(csvPath);
            var csvLines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("scenario,score_source,seed,threshold,volunteer_count,cause_count,total_capacity,algorithm,prioritized_count,total_score,average_selected_score,capacity_utilization", csvLines[0].TrimEnd('\r'));
            Assert.Equal(rows.Count, csvLines.Length - 1);

            string report = File.ReadAllText(reportPath);
            Assert.Contains("Min-Cost Max-Flow", report);
            Assert.Contains("Greedy", report);
            Assert.Contains("not neural network predictions", report);
            Assert.Contains("cardinality", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("does not claim", report, StringComparison.OrdinalIgnoreCase);

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
