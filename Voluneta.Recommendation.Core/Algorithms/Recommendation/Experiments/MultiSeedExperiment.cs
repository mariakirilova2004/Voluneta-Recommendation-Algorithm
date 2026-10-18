using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Experiments
{
    public class MultiSeedExperiment
    {
        public static IReadOnlyList<EvaluationResult> Run(int sampleCount = 1000)
        {
            int[] seeds = { 1, 7, 42, 123, 2026 };

            var allResults = new List<EvaluationResult>();

            foreach (int seed in seeds)
            {
                var results = ModelComparisonExperiment.Run(sampleCount: sampleCount, seed: seed);

                allResults.AddRange(results);
            }

            return allResults;
        }
    }
}
