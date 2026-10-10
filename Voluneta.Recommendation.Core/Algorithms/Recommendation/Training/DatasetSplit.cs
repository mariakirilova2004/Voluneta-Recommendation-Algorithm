namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public class DatasetSplit
    {
        public IReadOnlyList<TrainingSample> Training { get; }
        public IReadOnlyList<TrainingSample> Testing { get; }
        public IReadOnlyList<TrainingSample> Validation { get; }

        public DatasetSplit(IReadOnlyList<TrainingSample> training, IReadOnlyList<TrainingSample> testing, IReadOnlyList<TrainingSample> validation)
        {
            Training = training ?? throw new ArgumentNullException(nameof(training));
            Testing = testing ?? throw new ArgumentNullException(nameof(testing));
            Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        }
    }
}
