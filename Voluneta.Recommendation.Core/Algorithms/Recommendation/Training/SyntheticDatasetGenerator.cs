using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public class SyntheticDatasetGenerator
    {
        public static TrainingDataset Generate(int count, int seed = 42, double noise = 0.05)
        {
            if(count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
            }

            if(!double.IsFinite(noise) || noise < 0 || noise > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(noise), "Noise must be between 0 and 1");
            }

            TrainingDataset dataset = new TrainingDataset();

            Random random = new Random(seed);

            for (int i = 0; i < count; i++)
            {
                var features = new RecommendationFeatureVector
                {
                    InterestMatch = GenerateBinaryMatch(random),

                    CategoryHistoryMatch = GenerateHistoryMatch(random),

                    DayAvailableMatch = GenerateBinaryMatch(random),

                    TimeAvailableMatch = GenerateBinaryMatch(random),

                    LocationMatch = GenerateLocationMatch(random),

                    SkillMatch = GenerateSkillMatch(random),

                    ExperienceMatch = GenerateExperienceMatch(random),

                    ColleagueMatch = GenerateColleagueMatch(random)
                };

                double score = SyntheticTargetGenerator.CalculateScore(features);

                double noiseValue = (random.NextDouble() * 2 - 1) * noise;

                double noiseScore = Math.Clamp(score + noiseValue, 0.0, 1.0);

                int rating = (int) Math.Round( 1.0 + noiseScore * 9.0, MidpointRounding.AwayFromZero);

                dataset.AddSingle(new TrainingSample(features, rating));

            }

            return dataset;
        }

        private static double GenerateBinaryMatch(Random random)
        {
            return random.Next(2);
        }

        private static double GenerateHistoryMatch(Random random)
        {
            int totalCompleted = random.Next(1, 11);

            int matchingCompleted =
                random.Next(0, totalCompleted + 1);

            return (double)matchingCompleted / totalCompleted;
        }

        private static double GenerateLocationMatch(Random random)
        {
            double distanceKm =
                random.NextDouble() * 100.0;

            return Math.Exp(-distanceKm / 20.0);
        }

        private static double GenerateSkillMatch(Random random)
        {
            int requiredSkills = random.Next(0, 6);

            if (requiredSkills == 0)
            {
                return 1.0;
            }

            int matchedSkills =
                random.Next(0, requiredSkills + 1);

            return (double)matchedSkills / requiredSkills;
        }

        private static double GenerateExperienceMatch(Random random)
        {
            return random.Next(3) switch
            {
                0 => 0.0,
                1 => 0.5,
                _ => 1.0
            };
        }

        private static double GenerateColleagueMatch(Random random)
        {
            int otherParticipants =
                random.Next(0, 11);

            if (otherParticipants == 0)
            {
                return 0.0;
            }

            int colleagues =
                random.Next(0, otherParticipants + 1);

            return (double)colleagues / otherParticipants;
        }
    }
}
