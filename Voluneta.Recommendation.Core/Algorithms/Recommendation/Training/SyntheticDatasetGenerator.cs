using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public class SyntheticDatasetGenerator
    {
        public static TrainingDataset Generate(int count, int seed = 42, double noise = 0.05)
        {
            if(count <= 0)
            {
                throw new ArgumentOutOfRangeException("Count must be positive", nameof(count));
            }

            if(!double.IsFinite(noise) || noise < 0 || noise > 1)
            {
                throw new ArgumentOutOfRangeException("Noise must be between 0 and 1", nameof(noise));
            }

            TrainingDataset dataset = new TrainingDataset();

            Random random = new Random(seed);

            for (int i = 0; i < count; i++)
            {
                var features = new RecommendationFeatureVector
                {
                    InterestMatch = random.NextDouble(),
                    CategoryHistoryMatch = random.NextDouble(),
                    DayAvailableMatch = random.NextDouble(),
                    TimeAvailableMatch = random.NextDouble(),
                    LocationMatch = random.NextDouble(),
                    SkillMatch = random.NextDouble(),
                    ExperienceMatch = random.NextDouble(),
                    ColleagueMatch = random.NextDouble()
                };

                double score = SyntheticTargetGenerator.CalculateScore(features);

                double noiseValue = (random.NextDouble() * 2 - 1) * noise;

                double noiseScore = Math.Clamp(score + noiseValue, 0.0, 1.0);

                int rating = (int) Math.Round( 1.0 + noiseScore * 9.0, MidpointRounding.AwayFromZero);

                dataset.AddSingle(new TrainingSample(features, rating));

            }

            return dataset;
        }
    }
}
