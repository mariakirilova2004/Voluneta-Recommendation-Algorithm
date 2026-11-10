using Voluneta.Recommendation.Core.Algorithms.Recommendation.Reporting;

namespace Voluneta.Recommendation.Tests.Recommendation.Reporting
{
    public class NeuralEvaluationReportGeneratorTests
    {
        [Fact]
        public void GenerateAndSave_WritesAllThreeArtifacts()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), "NeuralEvaluationReportTests_" + Guid.NewGuid());

            try
            {
                var (runs, summary) = NeuralEvaluationReportGenerator.GenerateAndSave(outputDirectory);

                Assert.True(File.Exists(Path.Combine(outputDirectory, "neural_evaluation_runs.csv")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "neural_evaluation_summary.csv")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "neural_evaluation_report.md")));

                Assert.Equal(15, runs.Count);
                Assert.Equal(3, summary.Count);
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void GenerateAndSave_RunsCsv_HasExpectedHeaderAndRowCount()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), "NeuralEvaluationReportTests_" + Guid.NewGuid());

            try
            {
                NeuralEvaluationReportGenerator.GenerateAndSave(outputDirectory);

                var lines = File.ReadAllLines(Path.Combine(outputDirectory, "neural_evaluation_runs.csv"));

                Assert.Equal("seed,model,test_count,mae,rmse,mae_rating_scale,rmse_rating_scale", lines[0]);
                // 1 header + 5 seeds * 3 models = 16 lines
                Assert.Equal(16, lines.Length);
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void GenerateAndSave_SummaryCsv_HasExpectedHeaderAndRowCount()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), "NeuralEvaluationReportTests_" + Guid.NewGuid());

            try
            {
                NeuralEvaluationReportGenerator.GenerateAndSave(outputDirectory);

                var lines = File.ReadAllLines(Path.Combine(outputDirectory, "neural_evaluation_summary.csv"));

                Assert.Equal("model,runs,mean_mae,std_mae,mean_rmse,std_rmse,mean_mae_rating_scale,mean_rmse_rating_scale", lines[0]);
                // 1 header + 3 models
                Assert.Equal(4, lines.Length);
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void GenerateAndSave_Report_MentionsSyntheticDataAndSeeds()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), "NeuralEvaluationReportTests_" + Guid.NewGuid());

            try
            {
                NeuralEvaluationReportGenerator.GenerateAndSave(outputDirectory);

                string report = File.ReadAllText(Path.Combine(outputDirectory, "neural_evaluation_report.md"));

                Assert.Contains("synthetically generated", report, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("1, 7, 42, 123, 2026", report);
                Assert.Contains("Mean Rating Baseline", report);
                Assert.Contains("Linear Score Baseline", report);
                Assert.Contains("Neural Network", report);
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        }
    }
}
