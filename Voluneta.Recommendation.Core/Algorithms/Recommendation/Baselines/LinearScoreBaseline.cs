using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Baselines
{
    public sealed class LinearScoreBaseline
    {
        private readonly double[] _weights = new double[8];
        private double _bias;
        private bool _isFitted;

        public void Fit(IReadOnlyList<TrainingSample> trainingSamples, int epochs = 500, double learningrate = 0.01)
        {
            ArgumentNullException.ThrowIfNull(trainingSamples, nameof(trainingSamples));

            if (trainingSamples.Count == 0)
            {
                throw new ArgumentException("Training samples cannot be empty.", nameof(trainingSamples));
            }

            if (epochs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(epochs), "Epochs must be greater than zero.");
            }

            if (learningrate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(learningrate), "Learning rate must be greater than zero.");
            }

            Array.Clear(_weights, 0, _weights.Length);
            _bias = 0.0;
            _isFitted = false;

            for (int epoch = 0; epoch < epochs; epoch++)
            {
                foreach (var sample in trainingSamples)
                {
                    double[] features = sample.Features.ToArray();

                    double prediction = _bias;

                    for (int i = 0; i < _weights.Length; i++)
                    {
                        prediction += _weights[i] * features[i];
                    }

                    double error = prediction - sample.Target;

                    for (int i = 0; i < _weights.Length; i++)
                    {
                        double gradient = 2.0 * error * features[i];
                        _weights[i] -= learningrate * gradient;
                    }

                    _bias -= learningrate * 2.0 * error;
                }
            }

            _isFitted = true;
        }

        public double Predict(RecommendationFeatureVector features)
        {
            if (!_isFitted)
            {
                throw new InvalidOperationException("The model has not been fitted.");
            }

            double[] values = features.ToArray();
            double prediction = _bias;

            for (int i = 0; i < _weights.Length; i++)
            {
                prediction += _weights[i] * values[i];
            }

            return Math.Clamp(prediction, 0.0, 1.0);
        }
    }
}
