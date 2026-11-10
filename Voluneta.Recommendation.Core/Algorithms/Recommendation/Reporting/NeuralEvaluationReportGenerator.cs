using System.Globalization;
using System.Text;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Experiments;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Reporting
{
    // Generates reproducible evaluation artifacts (per-run CSV, summary CSV, and a Markdown
    // report) for the senior thesis. Reuses MultiSeedExperiment/StatisticsCalculator and never
    // changes network architecture, training logic, or the saved demo model.
    public static class NeuralEvaluationReportGenerator
    {
        private static readonly int[] Seeds = [1, 7, 42, 123, 2026];
        private const int SampleCount = 1000;

        // Runs the existing comparison once per seed and writes the three artifacts into
        // outputDirectory. Returns the per-run results and aggregated statistics for callers
        // that want to print a summary without re-running the experiment.
        public static (IReadOnlyList<EvaluationResult> Runs, IReadOnlyList<ModelStatistics> Summary) GenerateAndSave(string outputDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

            var seededRuns = RunAllSeedsWithSeed();
            var runs = seededRuns.Select(r => r.Result).ToList();
            var summary = StatisticsCalculator.Calculate(runs);

            Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(Path.Combine(outputDirectory, "neural_evaluation_runs.csv"), BuildRunsCsv(seededRuns));
            File.WriteAllText(Path.Combine(outputDirectory, "neural_evaluation_summary.csv"), BuildSummaryCsv(summary));
            File.WriteAllText(Path.Combine(outputDirectory, "neural_evaluation_report.md"), BuildReport(seededRuns, summary));

            return (runs, summary);
        }

        // Runs ModelComparisonExperiment once per fixed seed, tagging each result with its seed
        // so the per-run CSV can report (seed, model, test count, MAE, RMSE).
        private static IReadOnlyList<(int Seed, EvaluationResult Result)> RunAllSeedsWithSeed()
        {
            var runs = new List<(int Seed, EvaluationResult Result)>();

            foreach (int seed in Seeds)
            {
                var results = ModelComparisonExperiment.Run(sampleCount: SampleCount, seed: seed);

                foreach (var result in results)
                {
                    runs.Add((seed, result));
                }
            }

            return runs;
        }

        public static string BuildRunsCsv(IReadOnlyList<(int Seed, EvaluationResult Result)> runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("seed,model,test_count,mae,rmse,mae_rating_scale,rmse_rating_scale");

            foreach (var (seed, result) in runs)
            {
                sb.AppendLine(string.Join(",",
                    seed.ToString(CultureInfo.InvariantCulture),
                    CsvEscape(result.ModelName),
                    result.SampleCount.ToString(CultureInfo.InvariantCulture),
                    result.MAE.ToString("F6", CultureInfo.InvariantCulture),
                    result.RMSE.ToString("F6", CultureInfo.InvariantCulture),
                    result.MAEOnRatingScale.ToString("F6", CultureInfo.InvariantCulture),
                    result.RMSEOnRatingScale.ToString("F6", CultureInfo.InvariantCulture)));
            }

            return sb.ToString();
        }

        private static string BuildSummaryCsv(IReadOnlyList<ModelStatistics> summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("model,runs,mean_mae,std_mae,mean_rmse,std_rmse,mean_mae_rating_scale,mean_rmse_rating_scale");

            foreach (var model in summary)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(model.ModelName),
                    model.RunCount.ToString(CultureInfo.InvariantCulture),
                    model.MeanMAE.ToString("F6", CultureInfo.InvariantCulture),
                    model.StdMAE.ToString("F6", CultureInfo.InvariantCulture),
                    model.MeanRMSE.ToString("F6", CultureInfo.InvariantCulture),
                    model.StdRMSE.ToString("F6", CultureInfo.InvariantCulture),
                    model.MeanMAEOnRatingScale.ToString("F6", CultureInfo.InvariantCulture),
                    model.MeanRMSEOnRatingScale.ToString("F6", CultureInfo.InvariantCulture)));
            }

            return sb.ToString();
        }

        private static string BuildReport(IReadOnlyList<(int Seed, EvaluationResult Result)> runs, IReadOnlyList<ModelStatistics> summary)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Neural Network Evaluation Report");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            sb.AppendLine("## Configuration");
            sb.AppendLine();
            sb.AppendLine($"- Synthetic samples per run: {SampleCount}");
            sb.AppendLine($"- Seeds: {string.Join(", ", Seeds)}");
            sb.AppendLine("- Dataset split: 70% training / 15% validation / 15% testing (existing `DatasetSplitter` defaults)");
            sb.AppendLine("- Models compared: Mean Rating Baseline, Linear Score Baseline, Custom Neural Network");
            sb.AppendLine("- Neural network training defaults: existing `NeuralNetworkTrainer`/`TrainingOptions` defaults used by `ModelComparisonExperiment` (no hyperparameter tuning performed on test data)");
            sb.AppendLine();

            sb.AppendLine("## Methodology");
            sb.AppendLine();
            sb.AppendLine("For each seed, a fresh synthetic dataset is generated and split into training/validation/testing");
            sb.AppendLine("subsets using the existing `SyntheticDatasetGenerator` and `DatasetSplitter`. Each model is fit");
            sb.AppendLine("only on the training subset and evaluated once on the held-out testing subset via the existing");
            sb.AppendLine("`ModelEvaluator`. No model or hyperparameter selection is performed using the test subset.");
            sb.AppendLine("Per-seed results are aggregated with the existing `StatisticsCalculator` (mean and sample");
            sb.AppendLine("standard deviation across the 5 seeds).");
            sb.AppendLine();
            sb.AppendLine("**Important:** All rating targets used here are synthetically generated by");
            sb.AppendLine("`SyntheticTargetGenerator`/`SyntheticDatasetGenerator`, not collected from real volunteers or");
            sb.AppendLine("causes. Results describe how well each model fits this synthetic data-generating process, not");
            sb.AppendLine("real-world volunteer satisfaction.");
            sb.AppendLine();

            sb.AppendLine("## Results");
            sb.AppendLine();
            sb.AppendLine("### Per-run results");
            sb.AppendLine();
            sb.AppendLine("| Seed | Model | Test Count | MAE (normalized) | RMSE (normalized) | MAE (1-10 scale) | RMSE (1-10 scale) |");
            sb.AppendLine("|---|---|---|---|---|---|---|");

            foreach (var (seed, result) in runs)
            {
                sb.AppendLine($"| {seed} | {result.ModelName} | {result.SampleCount} | {result.MAE:F4} | {result.RMSE:F4} | {result.MAEOnRatingScale:F4} | {result.RMSEOnRatingScale:F4} |");
            }

            sb.AppendLine();
            sb.AppendLine("### Summary across seeds");
            sb.AppendLine();
            sb.AppendLine("| Model | Runs | Mean MAE (normalized) | Std MAE | Mean RMSE (normalized) | Std RMSE | Mean MAE (1-10 scale) | Mean RMSE (1-10 scale) |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            foreach (var model in summary)
            {
                sb.AppendLine($"| {model.ModelName} | {model.RunCount} | {model.MeanMAE:F4} | {model.StdMAE:F4} | {model.MeanRMSE:F4} | {model.StdRMSE:F4} | {model.MeanMAEOnRatingScale:F4} | {model.MeanRMSEOnRatingScale:F4} |");
            }

            sb.AppendLine();

            sb.AppendLine("## Interpretation");
            sb.AppendLine();
            var best = summary.OrderBy(m => m.MeanMAE).First();
            sb.AppendLine($"Across the {Seeds.Length} seeds evaluated, **{best.ModelName}** achieved the lowest mean MAE");
            sb.AppendLine($"({best.MeanMAE:F4} normalized, {best.MeanMAEOnRatingScale:F4} on the 1-10 scale). This report presents");
            sb.AppendLine("the measured results as-is; it does not assume in advance that the neural network outperforms");
            sb.AppendLine("the baselines, and in some configurations a baseline may perform comparably or better.");
            sb.AppendLine();

            sb.AppendLine("## Limitations");
            sb.AppendLine();
            sb.AppendLine("- Targets are synthetically generated (`SyntheticTargetGenerator`), not collected from real volunteers or causes.");
            sb.AppendLine("- Results reflect how well each model recovers the synthetic scoring function plus injected noise, not real-world recommendation quality.");
            sb.AppendLine("- No hyperparameter tuning was performed on the test subset; reported numbers use existing training defaults.");
            sb.AppendLine("- Only 5 seeds were evaluated; variance estimates are based on this small sample of runs.");
            sb.AppendLine();

            return sb.ToString();
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
