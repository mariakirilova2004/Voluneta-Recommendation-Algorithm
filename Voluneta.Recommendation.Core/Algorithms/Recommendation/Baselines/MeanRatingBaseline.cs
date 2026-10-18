using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Baselines
{
    public sealed class MeanRatingBaseline
    {
        private double _mean;
        private bool _isFitted;

        public void Fit(IReadOnlyList<TrainingSample> trainingSamples)
        {
            ArgumentNullException.ThrowIfNull(trainingSamples);

            if(trainingSamples.Count == 0)
            {
                throw new ArgumentException("Training samples cannot be empty.", nameof(trainingSamples));
            }

            _mean = trainingSamples.Average(sample => sample.Target);
            _isFitted = true;
        }

        public double Predict()
        {
            if (!_isFitted)
            {
                throw new InvalidOperationException("The model has not been fitted.");
            }

            return _mean;
        }
    }
}
