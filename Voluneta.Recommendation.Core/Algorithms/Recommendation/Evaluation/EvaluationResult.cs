namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation
{
    public sealed record EvaluationResult(string ModelName, double MAE, double RMSE, int SampleCount)
    {
        public double MAEOnRatingScale => RegressionMetrics.ToRatingScale(MAE);

        public double RMSEOnRatingScale => RegressionMetrics.ToRatingScale(RMSE);
    }
}
