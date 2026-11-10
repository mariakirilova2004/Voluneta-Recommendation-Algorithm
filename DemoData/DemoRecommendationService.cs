using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Demonstrates Algorithm 1 (feature calculation + neural network scoring) in isolation,
    // using the local demo volunteers/causes. Deliberately does NOT use
    // RecommendationPipelineService, since that service also runs Algorithm 2 (allocation
    // priorities / capacity-aware allocation), which is out of scope here.
    public sealed class DemoRecommendationService
    {
        private readonly DemoModelProvider modelProvider;

        public DemoRecommendationService(DemoModelProvider modelProvider)
        {
            this.modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
        }

        // Scores every eligible volunteer-cause pair and ranks causes per volunteer by
        // descending score. Ties are broken deterministically by ascending CauseId.
        // When volunteerOverride is supplied, it temporarily replaces the matching DemoDataSet
        // volunteer (by Id) for scoring purposes only - DemoDataSet is never mutated.
        public IReadOnlyList<DemoRecommendationResult> GenerateRankedRecommendations(DemoVolunteer? volunteerOverride = null)
        {
            var network = modelProvider.CreateSessionNetwork();
            var inputs = DemoDataFactory.BuildVolunteerCauseInputs(volunteerOverride);

            var scored = new List<(string VolunteerId, string CauseId, double[] Features, double Score)>();

            foreach (var input in inputs)
            {
                if (!input.IsEligible)
                {
                    continue;
                }

                var featureVector = RecommendationFeatureCalculator.Calculate(input.Features);
                double[] features = featureVector.ToArray();
                double score = network.Forward(features);

                scored.Add((input.VolunteerId, input.CauseId, features, score));
            }

            var results = new List<DemoRecommendationResult>();

            foreach (var group in scored.GroupBy(s => s.VolunteerId, StringComparer.Ordinal)
                                         .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var ranked = group
                    .OrderByDescending(s => s.Score)
                    .ThenBy(s => s.CauseId, StringComparer.Ordinal)
                    .ToList();

                for (int i = 0; i < ranked.Count; i++)
                {
                    var entry = ranked[i];
                    results.Add(new DemoRecommendationResult
                    {
                        VolunteerId = entry.VolunteerId,
                        CauseId = entry.CauseId,
                        Features = entry.Features,
                        Score = entry.Score,
                        Rank = i + 1
                    });
                }
            }

            return results;
        }
    }
}
