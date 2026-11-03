using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Ranking;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Integration
{
    public class RecommendationPipelineService
    {
        private readonly RecommendationNeuralNetwork network;
        private readonly MinCostMaxFlowPriorityAllocator allocator = new();
        private readonly RecommendationRankingService ranking = new();

        public RecommendationPipelineService(RecommendationNeuralNetwork network)
        {
            this.network = network ?? throw new ArgumentNullException(nameof(network));
        }

        public IReadOnlyList<RankedRecommendation> Generate(
            IEnumerable<VolunteerCauseInput> inputs,
            IEnumerable<CauseCapacity> capacities,
            double minimumScore = 0.50)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(capacities);

            var causeList = capacities.ToList();
            var candidates = new List<RecommendationCandidate>();

            // Calculates scores using Algorithm 1
            foreach (var input in inputs)
            {
                if (input == null || input.Features == null || string.IsNullOrWhiteSpace(input.VolunteerId) || string.IsNullOrWhiteSpace(input.CauseId))
                {
                    throw new ArgumentException("Invalid volunteer-cause input.");
                }

                if (!input.IsEligible)
                {
                    continue;
                }

                var features = RecommendationFeatureCalculator.Calculate(input.Features);
                double score = network.Forward(features.ToArray());

                candidates.Add(new RecommendationCandidate(input.VolunteerId, input.CauseId, score, true));
            }

            // Calculate priorities for all volunteers together
            var priorities = allocator.CalculatePriorities(candidates, causeList, minimumScore);

            // Rank causes for each volunteer
            var result = new List<RankedRecommendation>();
            var volunteers = candidates.Select(c => c.VolunteerId).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal);

            foreach (var volunteer in volunteers)
            {
                result.AddRange(ranking.RankForVolunteer(volunteer, candidates, causeList, priorities));
            }

            return result.AsReadOnly();
        }
    }
}
