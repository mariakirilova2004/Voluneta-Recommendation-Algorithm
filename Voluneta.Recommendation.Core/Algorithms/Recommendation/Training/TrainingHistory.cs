namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public sealed record EpochMetrics(int EpochNumber, double TrainingLoss, double ValidationLoss);
    public class TrainingHistory
    {
        private readonly List<EpochMetrics> _epoch = new();

        public int BestEpoch { get; internal set; }

        public double BestValidationLoss { get; internal set; } = double.MaxValue;

        public bool StoppedEarly { get; internal set; } = false;

        public IReadOnlyList<EpochMetrics> Epoch => _epoch.AsReadOnly();

        public void Add(int epoch,  double TrainingLoss, double ValidationLoss)
        {
            _epoch.Add(new EpochMetrics(epoch, TrainingLoss, ValidationLoss));
        }
    }
}
