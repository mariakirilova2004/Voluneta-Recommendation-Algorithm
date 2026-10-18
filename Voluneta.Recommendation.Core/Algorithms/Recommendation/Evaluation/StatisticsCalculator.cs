namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation
{
    public static class StatisticsCalculator
    {
        public static IReadOnlyList<ModelStatistics> Calculate(IReadOnlyList<EvaluationResult> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            if (results.Count == 0)
            {
                throw new ArgumentException("Results cannot be empty.", nameof(results));
            }

            var statistics = new List<ModelStatistics>();

            var groups = results.GroupBy(r => r.ModelName);

            foreach (var group in groups)
            {
                double[] maeValues = group.Select(r => r.MAE).ToArray();
                double[] rmseValues = group.Select(r => r.RMSE).ToArray();

                double meanMAE = maeValues.Average();
                double meanRMSE = rmseValues.Average();

                double stdMAE = StandardDeviation(maeValues);
                double stdRMSE = StandardDeviation(rmseValues);

                statistics.Add(new ModelStatistics(
                    group.Key,
                    group.Count(),
                    meanMAE,
                    stdMAE,
                    meanRMSE,
                    stdRMSE));
            }

            return statistics;
        }

        private static double StandardDeviation(IReadOnlyList<double> values)
        {
            if (values.Count < 2)
            {
                return 0.0;
            }

            double mean = values.Average();

            double sumSquaredDifferences = 0.0;

            foreach (double value in values)
            {
                double difference = value - mean;

                sumSquaredDifferences += difference * difference;
            }

            // Sample standard deviation (n - 1)
            double variance = sumSquaredDifferences / (values.Count - 1);

            return Math.Sqrt(variance);
        }
    }
}
