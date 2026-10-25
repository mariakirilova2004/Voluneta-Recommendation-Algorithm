using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Baselines
{
    // Simple baseline allocator: just picks the best scoring pairs first, one cause per volunteer, until capacities run out. Not optimal, but good enough as a comparison.
    public class GreedyAllocator
    {
        public AllocationResult Allocate(IEnumerable<AllocationCandidate> candidates, IEnumerable<CauseCapacity> capacities, double minimumScore = 0.50)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(capacities);

            if (!double.IsFinite(minimumScore) || minimumScore < 0 || minimumScore > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumScore));
            }

            // Step 1: Store the remaining capacity of each cause.
            var remainingPlaces = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var cause in capacities)
            {
                if (string.IsNullOrWhiteSpace(cause.CauseId))
                {
                    throw new ArgumentException("Invalid cause ID.");
                }

                if (cause.AvailablePlaces < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(capacities), "Available places cannot be negative.");
                }

                if (remainingPlaces.ContainsKey(cause.CauseId))
                {
                    throw new ArgumentException("Duplicate cause capacity.");
                }

                remainingPlaces.Add(cause.CauseId, cause.AvailablePlaces);
            }

            // Step 2: Validate candidate data.
            var candidateList = new List<AllocationCandidate>(candidates);
            var seenPairs = new HashSet<(string, string)>();

            foreach (var candidate in candidateList)
            {
                if (string.IsNullOrWhiteSpace(candidate.VolunteerId) || string.IsNullOrWhiteSpace(candidate.CauseId))
                {
                    throw new ArgumentException("Invalid candidate ID.");
                }

                if (!double.IsFinite(candidate.Score) || candidate.Score < 0 || candidate.Score > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(candidates), "Scores must be between 0 and 1.");
                }

                if (!remainingPlaces.ContainsKey(candidate.CauseId))
                {
                    throw new ArgumentException("Unknown cause ID.");
                }

                var pair = (candidate.VolunteerId, candidate.CauseId);
                if (seenPairs.Contains(pair))
                {
                    throw new ArgumentException("Duplicate volunteer-cause pair.");
                }
                seenPairs.Add(pair);
            }

            // Step 3: Keep only eligible candidates that pass the score bar.
            var filteredCandidates = new List<AllocationCandidate>();
            foreach (var candidate in candidateList)
            {
                if (candidate.IsEligible && candidate.Score >= minimumScore)
                {
                    filteredCandidates.Add(candidate);
                }
            }

            // Step 4: Sort by score (best first), then by IDs just so the
            // order is predictable when scores are tied.
            filteredCandidates.Sort((a, b) =>
            {
                int scoreCompare = b.Score.CompareTo(a.Score);
                if (scoreCompare != 0)
                {
                    return scoreCompare;
                }

                int volunteerCompare = string.CompareOrdinal(a.VolunteerId, b.VolunteerId);
                if (volunteerCompare != 0)
                {
                    return volunteerCompare;
                }

                return string.CompareOrdinal(a.CauseId, b.CauseId);
            });

            // Step 5: Track volunteers and selected assignments.
            var assignedVolunteers = new HashSet<string>(StringComparer.Ordinal);
            var assignments = new List<AllocationAssignment>();

            // Step 6: Greedily select the highest scoring pairs.
            foreach (var candidate in filteredCandidates)
            {
                if (assignedVolunteers.Contains(candidate.VolunteerId))
                {
                    continue;
                }

                if (remainingPlaces[candidate.CauseId] == 0)
                {
                    continue;
                }

                assignments.Add(new AllocationAssignment(candidate.VolunteerId, candidate.CauseId, candidate.Score));

                assignedVolunteers.Add(candidate.VolunteerId);
                remainingPlaces[candidate.CauseId] = remainingPlaces[candidate.CauseId] - 1;
            }

            // Step 7: Return the result.
            return new AllocationResult(assignments.AsReadOnly());
        }
    }
}
