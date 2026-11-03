using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Ranking
{
    public class RecommendationRankingService
    {
        public IReadOnlyList<RankedRecommendation> RankForVolunteer(
            string volunteerId, 
            IEnumerable<RecommendationCandidate> candidates,
            IEnumerable<CauseCapacity> capacities, 
            RecommendationPriorityResult priorityResult)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(volunteerId);
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(capacities);
            ArgumentNullException.ThrowIfNull(priorityResult);

            var places = capacities.ToDictionary(c => c.CauseId, c => c.RemainingPlaces, StringComparer.Ordinal);
            var matches = candidates.Where(c => c.VolunteerId == volunteerId).ToList();

            if (places.Values.Any(p => p < 0)) throw new ArgumentException("Invalid capacity.");

            
            if (matches.Any(c => !places.ContainsKey(c.CauseId))) throw new ArgumentException("Cause not found.");
            
            if (matches.Any(c => !double.IsFinite(c.CompatibilityScore) || c.CompatibilityScore < 0 || c.CompatibilityScore > 1))
                throw new ArgumentException("Invalid score.");
            
            if (matches.Select(c => c.CauseId).Distinct(StringComparer.Ordinal).Count() != matches.Count)
                throw new ArgumentException("Duplicate candidate.");

            // Get the selected priority for this volunteer
            var selected = priorityResult.Priorities.Where(p => p.VolunteerId == volunteerId).ToList();
            
            if (selected.Count > 1) throw new ArgumentException("Multiple priorities for one volunteer.");

            string? priorityCause = selected.FirstOrDefault()?.CauseId;

            // Put the priority first and sort the rest by score
            var ordered = matches.Where(c => c.IsEligible && places[c.CauseId] > 0)
                .OrderByDescending(c => c.CauseId == priorityCause)
                .ThenByDescending(c => c.CompatibilityScore)
                .ThenBy(c => c.CauseId, StringComparer.Ordinal).ToList();

            var result = new List<RankedRecommendation>();

            for (int i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i];
                result.Add(new RankedRecommendation(volunteerId, c.CauseId, c.CompatibilityScore, i + 1, c.CauseId == priorityCause));
            }

            return result.AsReadOnly();
        }
    }
}
