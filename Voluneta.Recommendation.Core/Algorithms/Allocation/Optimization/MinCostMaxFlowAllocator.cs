using Voluneta.Recommendation.Core.Algorithms.Allocation.Graph;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.PathFinding;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Optimization
{
    public sealed class MinCostMaxFlowAllocator
    {
        public AllocationResult Allocate(IEnumerable<AllocationCandidate> candidates,
            IEnumerable<CauseCapacity> capacities, double minimumScore = 0.50)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(capacities);

            if (!double.IsFinite(minimumScore) || minimumScore < 0 || minimumScore > 1)
                throw new ArgumentOutOfRangeException(nameof(minimumScore));

            // Check cause capacities
            var places = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var cause in capacities)
            {
                if (string.IsNullOrWhiteSpace(cause.CauseId))
                    throw new ArgumentException("Invalid cause ID.");

                if (cause.AvailablePlaces < 0)
                    throw new ArgumentOutOfRangeException(nameof(capacities));

                if (!places.TryAdd(cause.CauseId, cause.AvailablePlaces))
                    throw new ArgumentException("Duplicate cause.");
            }

            // Check candidate data
            var candidateList = candidates.ToList();
            var pairs = new HashSet<(string, string)>();

            foreach (var candidate in candidateList)
            {
                if (string.IsNullOrWhiteSpace(candidate.VolunteerId) || string.IsNullOrWhiteSpace(candidate.CauseId))
                    throw new ArgumentException("Invalid candidate ID.");

                if (!double.IsFinite(candidate.Score) || candidate.Score < 0 || candidate.Score > 1)
                    throw new ArgumentOutOfRangeException(nameof(candidates));

                if (!places.ContainsKey(candidate.CauseId))
                    throw new ArgumentException("Cause not found.");

                if (!pairs.Add((candidate.VolunteerId, candidate.CauseId)))
                    throw new ArgumentException("Duplicate candidate.");
            }

            // Only keep valid matches with available places
            var valid = candidateList
                .Where(c => c.IsEligible && c.Score >= minimumScore && places[c.CauseId] > 0)
                .OrderBy(c => c.VolunteerId, StringComparer.Ordinal)
                .ThenBy(c => c.CauseId, StringComparer.Ordinal)
                .ToList();

            if (valid.Count == 0)
                return new AllocationResult(Array.Empty<AllocationAssignment>());

            var volunteers = valid.Select(c => c.VolunteerId).Distinct(StringComparer.Ordinal).ToArray();
            var causes = places.Where(c => c.Value > 0).Select(c => c.Key).OrderBy(c => c, StringComparer.Ordinal).ToArray();

            int source = 0;
            int sink = volunteers.Length + causes.Length + 1;
            var graph = new FlowGraph(sink + 1);

            var volunteerNodes = new Dictionary<string, int>(StringComparer.Ordinal);
            var causeNodes = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < volunteers.Length; i++)
                volunteerNodes[volunteers[i]] = i + 1;

            for (int i = 0; i < causes.Length; i++)
                causeNodes[causes[i]] = volunteers.Length + i + 1;

            // Source to volunteers
            foreach (var volunteer in volunteers)
                graph.AddEdge(source, volunteerNodes[volunteer], 1, 0);

            // Volunteer to cause edges
            var candidateEdges = new List<(AllocationCandidate Candidate, int From, int EdgeIndex)>();

            foreach (var candidate in valid)
            {
                int from = volunteerNodes[candidate.VolunteerId];
                int to = causeNodes[candidate.CauseId];
                int edgeIndex = graph.GetEdges(from).Count;

                graph.AddEdge(from, to, 1, 1 - candidate.Score);
                candidateEdges.Add((candidate, from, edgeIndex));
            }

            // Causes to sink
            foreach (var cause in causes)
                graph.AddEdge(causeNodes[cause], sink, places[cause], 0);

            // Keep adding flow while a path exists
            while (true)
            {
                var path = BellmanFordPathFinder.FindShortestPath(graph, source, sink);
                if (!path.Found) break;

                foreach (var step in path.Steps)
                    graph.Augment(step.From, step.EdgeIndex, 1);
            }

            // Read the final assignments from the graph
            var assignments = candidateEdges
                .Where(e => graph.GetEdges(e.From)[e.EdgeIndex].ResidualCapacity == 0)
                .Select(e => new AllocationAssignment(e.Candidate.VolunteerId, e.Candidate.CauseId, e.Candidate.Score))
                .ToList();

            return new AllocationResult(assignments.AsReadOnly());
        }
    }
}
