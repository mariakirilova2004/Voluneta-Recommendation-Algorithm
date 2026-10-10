namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public static class DatasetSplitter
    {
        public static DatasetSplit Split(TrainingDataset dataset, double trainingRatio = 0.7, double validationRatio = 0.15, int seed = 42)
        {
            ArgumentNullException.ThrowIfNull(dataset, nameof(dataset));
            
            var random = new Random(seed);

            if (trainingRatio <= 0 || validationRatio <= 0 || trainingRatio + validationRatio >= 1)
            {
                throw new ArgumentException("Ratios must be positive and their sum must be less than 1.");
            }

            var shuffled = dataset.Dataset.ToList();

            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int trainingCount = (int)Math.Floor(shuffled.Count * trainingRatio);
            int validationCount = (int)Math.Floor(shuffled.Count * validationRatio);
            int testingCount = shuffled.Count - trainingCount - validationCount;

            var trainingDataset = shuffled.Take(trainingCount).ToList();
            var validationDataset = shuffled.Skip(trainingCount).Take(validationCount).ToList();
            var testingDataset = shuffled.Skip(trainingCount + validationCount).Take(testingCount).ToList();

            return new DatasetSplit(trainingDataset, testingDataset, validationDataset);
        }
    }
}

