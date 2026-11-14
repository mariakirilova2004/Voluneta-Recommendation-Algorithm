using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Reporting
{
    // Generates reproducible Algorithm 2 performance benchmark artifacts (CSV + Markdown report)
    // comparing MinCostMaxFlowPriorityAllocator and GreedyPriorityAllocator on deterministic
    // synthetic candidate pools of increasing size, using AllocationPerformanceBenchmarkRunner.
    // Never changes either allocator.
    public static class AllocationPerformanceReportGenerator
    {
        public static (IReadOnlyList<AllocationPerformanceScenario> Scenarios, IReadOnlyList<AllocationPerformanceRow> Rows) GenerateAndSave(string outputDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

            var scenarios = AllocationPerformanceBenchmarkRunner.BuildScenarios();
            var rows = AllocationPerformanceBenchmarkRunner.Run(scenarios);

            Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(Path.Combine(outputDirectory, "allocation_performance.csv"), BuildCsv(rows));
            File.WriteAllText(Path.Combine(outputDirectory, "allocation_performance_report.md"), BuildReport(scenarios, rows));

            return (scenarios, rows);
        }

        public static string BuildCsv(IReadOnlyList<AllocationPerformanceRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("scenario,seed,volunteer_count,cause_count,candidate_edge_count,threshold,algorithm,repetitions,median_ms,mean_ms,min_ms,prioritized_count");

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(row.ScenarioName),
                    row.Seed.ToString(CultureInfo.InvariantCulture),
                    row.VolunteerCount.ToString(CultureInfo.InvariantCulture),
                    row.CauseCount.ToString(CultureInfo.InvariantCulture),
                    row.CandidateEdgeCount.ToString(CultureInfo.InvariantCulture),
                    row.Threshold.ToString("F2", CultureInfo.InvariantCulture),
                    CsvEscape(row.AlgorithmName),
                    row.Repetitions.ToString(CultureInfo.InvariantCulture),
                    row.MedianMilliseconds.ToString("F4", CultureInfo.InvariantCulture),
                    row.MeanMilliseconds.ToString("F4", CultureInfo.InvariantCulture),
                    row.MinMilliseconds.ToString("F4", CultureInfo.InvariantCulture),
                    row.PrioritizedVolunteerCount.ToString(CultureInfo.InvariantCulture)));
            }

            return sb.ToString();
        }

        private static string BuildReport(IReadOnlyList<AllocationPerformanceScenario> scenarios, IReadOnlyList<AllocationPerformanceRow> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Allocation Algorithm Performance & Scalability Report (Algorithm 2)");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            sb.AppendLine("## Environment");
            sb.AppendLine();
            sb.AppendLine($"- OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            sb.AppendLine($"- Process architecture: {RuntimeInformation.ProcessArchitecture}");
            sb.AppendLine($"- .NET runtime: {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine($"- Logical processor count: {Environment.ProcessorCount}");
            sb.AppendLine($"- Build configuration: {(IsDebugBuild() ? "Debug" : "Release")}");
            sb.AppendLine();
            sb.AppendLine("**Measured timings are specific to this environment (hardware, OS, .NET runtime, build");
            sb.AppendLine("configuration, and current machine load) and are not portable performance guarantees.");
            sb.AppendLine("Re-running this benchmark on different hardware or under load may produce different");
            sb.AppendLine("absolute numbers.**");
            sb.AppendLine();

            sb.AppendLine("## Configuration");
            sb.AppendLine();
            sb.AppendLine("- Algorithms compared: `MinCostMaxFlowPriorityAllocator` (existing optimal allocator) vs `GreedyPriorityAllocator` (existing baseline)");
            sb.AppendLine($"- Threshold: {AllocationPerformanceBenchmarkRunner.Threshold:F2} (same for both algorithms, all scenarios)");
            sb.AppendLine($"- Measured repetitions per scenario/algorithm: {AllocationPerformanceBenchmarkRunner.MeasuredRepetitions} (plus 1 untimed warm-up run)");
            sb.AppendLine("- Scenario sizes: Small (10 volunteers x 5 causes), Medium (25 x 10), Large (50 x 15), Extra Large (100 x 20)");
            sb.AppendLine("- Candidate data: deterministic synthetic pairs with bounded edge density (not a full dense matrix), generated from fixed random seeds per scenario; capacities are also seeded and deterministic.");
            sb.AppendLine("- Both algorithms receive identical candidates, equivalent (freshly cloned) capacities, and the same threshold for each scenario.");
            sb.AppendLine();

            sb.AppendLine("## Methodology");
            sb.AppendLine();
            sb.AppendLine("For each scenario, candidate pairs and cause capacities are generated once, outside any timed");
            sb.AppendLine("section, using a fixed seed per scenario. For each algorithm, a single untimed warm-up call is");
            sb.AppendLine("made first (to absorb JIT compilation and any first-call setup), followed by 5 measured");
            sb.AppendLine("repetitions. Each measured repetition uses a fresh clone of the capacities (since allocators");
            sb.AppendLine("may mutate remaining-places bookkeeping) but the same candidate list, and only the call to");
            sb.AppendLine("`CalculatePriorities` is wrapped in a `System.Diagnostics.Stopwatch` - input generation, cloning,");
            sb.AppendLine("and result processing are not included in the timed interval. Median, mean, and minimum");
            sb.AppendLine("elapsed time (milliseconds) are reported across the 5 measured repetitions, along with the");
            sb.AppendLine("candidate edge count and prioritized volunteer count for context.");
            sb.AppendLine();
            sb.AppendLine("**Important:** all candidate scores and capacities are constructed synthetic test fixtures, not");
            sb.AppendLine("real volunteer/cause data or neural network predictions. This benchmark measures allocation");
            sb.AppendLine("(Algorithm 2) computation time only; it does not measure neural network inference or training.");
            sb.AppendLine();

            sb.AppendLine("## Results");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Candidate Edges | Algorithm | Median (ms) | Mean (ms) | Min (ms) | Prioritized Count |");
            sb.AppendLine("|---|---|---|---|---|---|---|");

            foreach (var scenario in scenarios)
            {
                foreach (var row in rows.Where(r => r.ScenarioName == scenario.Name))
                {
                    sb.AppendLine($"| {row.ScenarioName} | {row.CandidateEdgeCount} | {row.AlgorithmName} | {row.MedianMilliseconds:F4} | {row.MeanMilliseconds:F4} | {row.MinMilliseconds:F4} | {row.PrioritizedVolunteerCount} |");
                }
            }

            sb.AppendLine();

            sb.AppendLine("## Interpretation");
            sb.AppendLine();
            sb.AppendLine("`GreedyPriorityAllocator` performs a simpler, more localized selection process, while");
            sb.AppendLine("`MinCostMaxFlowPriorityAllocator` solves a min-cost max-flow problem via repeated shortest-path");
            sb.AppendLine("augmentation, which does more work per candidate edge to guarantee maximum assignment");
            sb.AppendLine("cardinality and optimal total compatibility for that cardinality. On the scenario sizes measured");
            sb.AppendLine("here, this report only states the timings actually observed above; it does not claim that either");
            sb.AppendLine("algorithm is universally faster across all hardware, input shapes, or problem sizes. Relative");
            sb.AppendLine("performance may change for different candidate densities, volunteer/cause ratios, or runtime");
            sb.AppendLine("environments.");
            sb.AppendLine();

            sb.AppendLine("## Limitations");
            sb.AppendLine();
            sb.AppendLine("- Only 4 scenario sizes and 5 measured repetitions per scenario/algorithm were evaluated; variance estimates are based on this small sample.");
            sb.AppendLine("- Timings reflect this specific machine, OS, .NET runtime, and build configuration at the time of measurement; they are not portable guarantees.");
            sb.AppendLine("- Candidate data is synthetic with bounded density, not real volunteer/cause data or neural network output.");
            sb.AppendLine("- No JIT tiering/server GC configuration changes were made beyond process defaults; results may differ under other hosting configurations.");
            sb.AppendLine();

            return sb.ToString();
        }

        private static bool IsDebugBuild()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
