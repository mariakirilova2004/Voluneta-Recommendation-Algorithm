using System.Transactions;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation
{
    public class RegressionMetrics
    {
        public static double MAE(IReadOnlyList<double> actual, IReadOnlyList<double> predicted)
        {
            Validate(actual, predicted);

            double sum = 0.0;

            for (int i = 0; i < actual.Count; i++)
            {
                sum += Math.Abs(actual[i] - predicted[i]);
            }

            return sum / actual.Count;
        }

        public static double RMSE(IReadOnlyList<double> actual, IReadOnlyList<double> predicted)
        {
            Validate(actual, predicted);

            double sum = 0.0;

            for (int i = 0; i < actual.Count; i++)
            {
                double error = actual[i] - predicted[i];
                sum += error * error;
            }

            return Math.Sqrt(sum / actual.Count);
        }

        public static double ToRatingScale(double normalizedError)
        {
            if(normalizedError < 0.0 || normalizedError > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedError), "Normalized error must be between 0 and 1.");
            }

            return normalizedError * 9.0;
        }

        private static void Validate(IReadOnlyList<double> actual, IReadOnlyList<double> predicted)
        {
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            if (predicted == null) throw new ArgumentNullException(nameof(predicted));

            if (actual.Count == 0)
            {
                throw new ArgumentException("The input collections cannot be empty.");
            }

            if (actual.Count != predicted.Count) throw new ArgumentException("Actual and predicted must have the same length.");
            
            for (int i = 0; i < actual.Count; i++)
            {
                if (!double.IsFinite(actual[i]) || !double.IsFinite(predicted[i]))
                {
                    throw new ArgumentException("All values must be finite.");
                }
            }
        }
    }
}
