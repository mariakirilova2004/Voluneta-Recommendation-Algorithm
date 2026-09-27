namespace Voluneta.Recommendation.Core.Models
{
    public class RecommendationTraining
    {
        public double[] Features { get; set; }
        public double Target { get; set; }

        public RecommendationTraining(double[] features, double target)
        {
            if (features.Length != 8)
            {
                throw new ArgumentException("Features must be 8.", nameof(features));
            }
            
            if(target < 0 || target > 1)
            {
                throw new ArgumentException("Target must be between 0 and 1.", nameof(target));
            }
        
            Features = features;
            Target = target;
        }
    }
}
