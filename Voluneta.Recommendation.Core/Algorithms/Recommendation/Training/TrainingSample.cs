using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public sealed class TrainingSample
    {
        public double[] Features { get; }
        public double Target { get; }

        public TrainingSample(RecommendationFeatureVector features, int rating)
        {
            ArgumentNullException.ThrowIfNull(features, nameof(features));

            Features = features.ToArray();

            if (Features.Length != 8 || Features.Any(x => !double.IsFinite(x) || x < 0 || x > 1))
            {
                throw new ArgumentException("Features must contain 8 values between 0 and 1.", nameof(features));
            }

            Target = RatingNormalizer.Normalize(rating);
        }
    }
}
