namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation
{
    public sealed record ModelStatistics(
       string ModelName,
       int RunCount,
       double MeanMAE,
       double StdMAE,
       double MeanRMSE,
       double StdRMSE)
    {
        public double MeanMAEOnRatingScale => MeanMAE * 9.0;

        public double MeanRMSEOnRatingScale => MeanRMSE * 9.0;
    }
}
