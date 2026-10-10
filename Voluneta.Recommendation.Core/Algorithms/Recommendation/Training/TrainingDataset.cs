namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public sealed class TrainingDataset
    {
        private readonly List<TrainingSample> _dataset = new List<TrainingSample>();

        public int Count => _dataset.Count;

        public IReadOnlyList<TrainingSample> Dataset => _dataset.AsReadOnly();

        public void AddSingle(TrainingSample sample)
        {
            ArgumentNullException.ThrowIfNull(sample, nameof(sample));

            _dataset.Add(sample);
        }

        public void AddRange(IEnumerable<TrainingSample> samples)
        {
            ArgumentNullException.ThrowIfNull(samples, nameof(samples));

            _dataset.AddRange(samples);
        }
    }
}
