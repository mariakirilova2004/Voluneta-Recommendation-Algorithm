namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public sealed class TrainingOptions
    {
        public int MaxEpochs { get; init; } = 100;

        public bool UseEarlyStopping { get; init; } = true;

        public int Patience { get; init; } = 10;

        public double MinDelta { get; init; } = 0.00001;
    }
}
