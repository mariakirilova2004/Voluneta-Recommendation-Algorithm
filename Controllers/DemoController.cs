using Microsoft.AspNetCore.Mvc;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta_Recommendation_Algorithm.DemoData;
using Voluneta_Recommendation_Algorithm.Models;

namespace Voluneta_Recommendation_Algorithm.Controllers
{
    // Demo controller for Algorithm 1 (neural-network scoring + ranking) and Algorithm 2
    // (priority allocation comparison). Volunteer profiles and cause capacities can be
    // edited in-memory for a single request only; DemoDataSet and application singletons
    // are never mutated. No database or model retraining here.
    public class DemoController : Controller
    {
        private const double DefaultMinimumScore = 0.50;

        private readonly DemoRecommendationService recommendationService;
        private readonly DemoAllocationComparisonService comparisonService;

        public DemoController(
            DemoRecommendationService recommendationService,
            DemoAllocationComparisonService comparisonService)
        {
            this.recommendationService = recommendationService;
            this.comparisonService = comparisonService;
        }

        private static IReadOnlyList<VolunteerOptionViewModel> GetVolunteerOptions() =>
            DemoDataSet.Volunteers
                .Select(v => new VolunteerOptionViewModel { Id = v.Id, Name = v.Name })
                .ToList();

        [HttpGet]
        public IActionResult Recommendations(string? volunteerId)
        {
            var model = BuildViewModel(volunteerId, profileOverride: null);
            return View(model);
        }

        // Recalculates recommendations using the volunteer's original demo profile
        // (ignores any edited interests/skills/days/time posted alongside it).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Recommendations(string volunteerId, bool calculate)
        {
            var model = BuildViewModel(volunteerId, profileOverride: null);
            return View(model);
        }

        // Recalculates recommendations using the edited profile fields (temporary override).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculateWithEditedProfile(
            string volunteerId,
            List<string>? interests,
            List<string>? skills,
            List<string>? days,
            string? timeOfDay,
            string? location,
            string? experience)
        {
            var baseVolunteer = DemoDataSet.Volunteers.FirstOrDefault(v => v.Id == volunteerId);
            if (baseVolunteer == null)
            {
                return View("Recommendations", BuildViewModel(volunteerId, profileOverride: null));
            }

            var validInterests = (interests ?? []).Intersect(DemoProfileOptions.Interests, StringComparer.Ordinal).ToList();
            var validSkills = (skills ?? []).Intersect(DemoProfileOptions.Skills, StringComparer.Ordinal).ToList();
            var validDays = (days ?? []).Intersect(DemoProfileOptions.Days, StringComparer.Ordinal).ToList();
            var validTime = DemoProfileOptions.TimesOfDay.Contains(timeOfDay)
                ? timeOfDay!
                : baseVolunteer.PreferredTimeOfDay;

            string validLocation = DemoProfileOptions.Locations.Contains(location)
                ? location!
                : DemoProfileOptions.OriginalLocationLabel;
            (double Latitude, double Longitude) coordinates =
                DemoProfileOptions.LocationCoordinates.TryGetValue(validLocation, out var cityCoords)
                    ? cityCoords
                    : (baseVolunteer.Latitude, baseVolunteer.Longitude);

            string validExperience = DemoProfileOptions.ExperienceOptions.Contains(experience)
                ? experience!
                : DemoProfileOptions.UnknownExperienceLabel;
            bool hasPreviousExperience = validExperience switch
            {
                "Yes" => true,
                "No" => false,
                _ => baseVolunteer.HasPreviousExperience
            };

            var overrideVolunteer = new DemoVolunteer
            {
                Id = baseVolunteer.Id,
                Name = baseVolunteer.Name,
                Interests = string.Join(",", validInterests),
                DaysAvailable = string.Join(",", validDays),
                PreferredTimeOfDay = validTime,
                Skills = string.Join(",", validSkills),
                HasPreviousExperience = hasPreviousExperience,
                CompanyId = baseVolunteer.CompanyId,
                Latitude = coordinates.Latitude,
                Longitude = coordinates.Longitude,
                CompletedCauseCategoryIds = baseVolunteer.CompletedCauseCategoryIds
            };

            var model = BuildViewModel(volunteerId, overrideVolunteer, validLocation, validExperience);
            return View("Recommendations", model);
        }

        private RecommendationsViewModel BuildViewModel(
            string? volunteerId,
            DemoVolunteer? profileOverride,
            string? selectedLocation = null,
            string? selectedExperience = null)
        {
            var baseVolunteer = string.IsNullOrWhiteSpace(volunteerId)
                ? null
                : DemoDataSet.Volunteers.FirstOrDefault(v => v.Id == volunteerId);

            VolunteerProfileEditViewModel? profile = null;
            if (baseVolunteer != null)
            {
                var active = profileOverride ?? baseVolunteer;
                profile = new VolunteerProfileEditViewModel
                {
                    VolunteerId = baseVolunteer.Id,
                    SelectedInterests = SplitCsv(active.Interests),
                    SelectedSkills = SplitCsv(active.Skills),
                    SelectedDays = SplitCsv(active.DaysAvailable),
                    PreferredTimeOfDay = active.PreferredTimeOfDay,
                    SelectedLocation = selectedLocation ?? DemoProfileOptions.OriginalLocationLabel,
                    SelectedExperience = selectedExperience ?? DemoProfileOptions.UnknownExperienceLabel,
                    IsEdited = profileOverride != null
                };
            }

            IReadOnlyList<CauseComparisonViewModel>? comparison = null;
            if (baseVolunteer != null && profileOverride != null)
            {
                comparison = BuildComparison(baseVolunteer.Id, profileOverride);
            }

            return new RecommendationsViewModel
            {
                Volunteers = GetVolunteerOptions(),
                SelectedVolunteerId = volunteerId,
                RankedCauses = baseVolunteer != null ? BuildRankedCauses(baseVolunteer.Id, profileOverride) : null,
                Profile = profile,
                Comparison = comparison
            };
        }

        // Computes original vs. edited recommendations for the same volunteer, matching
        // causes by CauseId (never by rank or list position). Both calls reuse
        // DemoRecommendationService and never mutate DemoDataSet or shared state.
        private IReadOnlyList<CauseComparisonViewModel> BuildComparison(string volunteerId, DemoVolunteer profileOverride)
        {
            var originalResults = recommendationService.GenerateRankedRecommendations(volunteerOverride: null)
                .Where(r => r.VolunteerId == volunteerId)
                .ToDictionary(r => r.CauseId);

            var editedResults = recommendationService.GenerateRankedRecommendations(profileOverride)
                .Where(r => r.VolunteerId == volunteerId)
                .ToDictionary(r => r.CauseId);

            var causesById = DemoDataSet.Causes.ToDictionary(c => c.Id);

            var allCauseIds = originalResults.Keys.Union(editedResults.Keys, StringComparer.Ordinal);

            return allCauseIds
                .Select(causeId =>
                {
                    var cause = causesById[causeId];
                    originalResults.TryGetValue(causeId, out var original);
                    editedResults.TryGetValue(causeId, out var edited);

                    return new CauseComparisonViewModel
                    {
                        CauseId = causeId,
                        CauseName = cause.Name,
                        CategoryName = cause.CategoryName,
                        OriginalScore = original?.Score,
                        OriginalRank = original?.Rank,
                        OriginalFeatures = original?.Features,
                        EditedScore = edited?.Score,
                        EditedRank = edited?.Rank,
                        EditedFeatures = edited?.Features
                    };
                })
                .OrderBy(c => c.EditedRank ?? int.MaxValue)
                .ThenBy(c => c.OriginalRank ?? int.MaxValue)
                .ThenBy(c => c.CauseId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> SplitCsv(string value) =>
            value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private IReadOnlyList<RankedCauseViewModel> BuildRankedCauses(string volunteerId, DemoVolunteer? profileOverride)
        {
            var results = recommendationService.GenerateRankedRecommendations(profileOverride)
                .Where(r => r.VolunteerId == volunteerId)
                .OrderBy(r => r.Rank)
                .ToList();

            var causesById = DemoDataSet.Causes.ToDictionary(c => c.Id);

            return results
                .Select(r =>
                {
                    var cause = causesById[r.CauseId];
                    return new RankedCauseViewModel
                    {
                        CauseId = cause.Id,
                        CauseName = cause.Name,
                        CategoryName = cause.CategoryName,
                        StartDate = cause.StartDate,
                        Rank = r.Rank,
                        Score = r.Score,
                        Place = cause.Place,
                        Description = cause.Description,
                        RequiredSkills = cause.RequiredSkills,
                        DifficultyLevel = cause.DifficultyLevel,
                        RemainingPlaces = cause.RemainingPlaces,
                        MaxVolunteers = cause.MaxVolunteers,
                        ImagePath = cause.ImagePath,
                        Features = r.Features
                    };
                })
                .ToList();
        }

        // Algorithm 2 demo: lets the user pick a score scenario (real neural network predictions,
        // or an explicitly constructed educational example), tweak cause capacities and the
        // minimum match threshold, then runs the real Min-Cost Max-Flow and Greedy allocators on
        // the same scores for a side-by-side comparison.
        [HttpGet]
        public IActionResult Optimization(string? scenario)
        {
            var selectedScenario = ParseScenario(scenario);
            var model = BuildOptimizationViewModel(
                capacityOverrides: null,
                GetDefaultMinimumScore(selectedScenario),
                selectedScenario);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Optimization(Dictionary<string, int> capacity, double minimumScore, string? scenario)
        {
            var selectedScenario = ParseScenario(scenario);
            var errors = new List<string>();

            if (!double.IsFinite(minimumScore) || minimumScore < 0 || minimumScore > 1)
            {
                errors.Add("Minimum match threshold must be between 0 and 1.");
            }

            capacity ??= [];
            var validCapacity = new Dictionary<string, int>(StringComparer.Ordinal);
            var causeIds = DemoDataSet.Causes.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            foreach (var (causeId, remainingPlaces) in capacity)
            {
                if (!causeIds.Contains(causeId))
                {
                    continue;
                }

                if (remainingPlaces < 0)
                {
                    var causeName = DemoDataSet.Causes.First(c => c.Id == causeId).Name;
                    errors.Add($"Remaining places for \"{causeName}\" cannot be negative.");
                    continue;
                }

                validCapacity[causeId] = remainingPlaces;
            }

            if (errors.Count > 0)
            {
                var invalidModel = BuildOptimizationViewModel(capacity, minimumScore, selectedScenario, errors: errors);
                return View(invalidModel);
            }

            var model = BuildOptimizationViewModel(validCapacity, minimumScore, selectedScenario);
            return View(model);
        }

        // Restores the selected scenario's default capacities and default minimum score.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetOptimization(string? scenario)
        {
            return RedirectToAction(nameof(Optimization), new { scenario });
        }

        private static DemoOptimizationScenario ParseScenario(string? scenario) =>
            Enum.TryParse<DemoOptimizationScenario>(scenario, ignoreCase: true, out var parsed)
                ? parsed
                : DemoOptimizationScenario.NeuralNetwork;

        private static double GetDefaultMinimumScore(DemoOptimizationScenario scenario) =>
            scenario == DemoOptimizationScenario.Educational
                ? DemoEducationalScenario.DefaultMinimumScore
                : DefaultMinimumScore;

        private static IReadOnlyList<ScenarioOptionViewModel> BuildScenarioOptions(DemoOptimizationScenario selected) =>
        [
            new ScenarioOptionViewModel
            {
                Value = nameof(DemoOptimizationScenario.NeuralNetwork),
                Title = "Neural Network Predictions",
                Description = "Uses the real, trained Algorithm 1 compatibility scores for the current demo volunteers and causes.",
                ScoreSourceLabel = "Trained neural network predictions",
                DefaultMinimumScore = DefaultMinimumScore,
                IsSelected = selected == DemoOptimizationScenario.NeuralNetwork
            },
            new ScenarioOptionViewModel
            {
                Value = nameof(DemoOptimizationScenario.Educational),
                Title = "Greedy vs Optimal (Educational Example)",
                Description = "Explicitly constructed scores designed to make the Greedy vs Min-Cost Max-Flow trade-off easy to see.",
                ScoreSourceLabel = "Constructed educational scores (not neural network predictions)",
                DefaultMinimumScore = DemoEducationalScenario.DefaultMinimumScore,
                IsSelected = selected == DemoOptimizationScenario.Educational
            }
        ];

        private OptimizationViewModel BuildOptimizationViewModel(
            IReadOnlyDictionary<string, int>? capacityOverrides,
            double minimumScore,
            DemoOptimizationScenario scenario,
            IReadOnlyList<string>? errors = null)
        {
            var defaultRemainingPlaces = scenario == DemoOptimizationScenario.Educational
                ? DemoEducationalScenario.DefaultRemainingPlaces
                : DemoDataSet.Causes.ToDictionary(c => c.Id, c => c.RemainingPlaces, StringComparer.Ordinal);

            var causeCapacities = DemoDataSet.Causes
                .Select(cause => new CauseCapacityInputViewModel
                {
                    CauseId = cause.Id,
                    CauseName = cause.Name,
                    CategoryName = cause.CategoryName,
                    DefaultRemainingPlaces = defaultRemainingPlaces[cause.Id],
                    RemainingPlaces = capacityOverrides != null && capacityOverrides.TryGetValue(cause.Id, out var overridden)
                        ? overridden
                        : defaultRemainingPlaces[cause.Id]
                })
                .ToList();

            OptimizationResultViewModel? result = null;
            if (errors == null || errors.Count == 0)
            {
                result = BuildOptimizationResult(capacityOverrides, minimumScore, scenario);
            }

            var scenarioOptions = BuildScenarioOptions(scenario);

            return new OptimizationViewModel
            {
                Volunteers = GetVolunteerOptions(),
                CauseCapacities = causeCapacities,
                MinimumScore = minimumScore,
                ScenarioOptions = scenarioOptions,
                SelectedScenario = scenario.ToString(),
                ScoreSourceLabel = scenarioOptions.First(o => o.IsSelected).ScoreSourceLabel,
                Result = result,
                Errors = errors ?? []
            };
        }

        private OptimizationResultViewModel BuildOptimizationResult(
            IReadOnlyDictionary<string, int>? capacityOverrides,
            double minimumScore,
            DemoOptimizationScenario scenario)
        {
            var comparison = comparisonService.Compare(capacityOverrides, minimumScore, scenario);

            var volunteersById = DemoDataSet.Volunteers.ToDictionary(v => v.Id);
            var causesById = DemoDataSet.Causes.ToDictionary(c => c.Id);

            var minCostMaxFlowPairs = comparison.MinCostMaxFlow.SelectedPriorities
                .ToDictionary(p => p.VolunteerId, p => p.CauseId, StringComparer.Ordinal);
            var greedyPairs = comparison.Greedy.SelectedPriorities
                .ToDictionary(p => p.VolunteerId, p => p.CauseId, StringComparer.Ordinal);

            var allCandidates = comparison.AllCandidates
                .OrderByDescending(c => c.CompatibilityScore)
                .ThenBy(c => c.VolunteerId, StringComparer.Ordinal)
                .ThenBy(c => c.CauseId, StringComparer.Ordinal)
                .Select(c => new CandidatePairViewModel
                {
                    VolunteerId = c.VolunteerId,
                    VolunteerName = volunteersById[c.VolunteerId].Name,
                    CauseId = c.CauseId,
                    CauseName = causesById[c.CauseId].Name,
                    CompatibilityScore = c.CompatibilityScore,
                    IsAboveThreshold = c.CompatibilityScore >= minimumScore,
                    IsSelectedByMinCostMaxFlow = minCostMaxFlowPairs.TryGetValue(c.VolunteerId, out var mcmfCauseId) && mcmfCauseId == c.CauseId,
                    IsSelectedByGreedy = greedyPairs.TryGetValue(c.VolunteerId, out var greedyCauseId) && greedyCauseId == c.CauseId
                })
                .ToList();

            return new OptimizationResultViewModel
            {
                AllCandidates = allCandidates,
                MinCostMaxFlow = BuildAlgorithmSummary(
                    "Min-Cost Max-Flow",
                    "Finds the globally optimal set of priority recommendations for the given capacities and threshold.",
                    comparison.MinCostMaxFlow,
                    volunteersById,
                    causesById,
                    greedyPairs),
                Greedy = BuildAlgorithmSummary(
                    "Greedy",
                    "Assigns the best-scoring pairs first until capacities run out. Simple, but not always optimal.",
                    comparison.Greedy,
                    volunteersById,
                    causesById,
                    minCostMaxFlowPairs),
                RemainingCapacityByCauseId = comparison.RemainingCapacityByCauseId
            };
        }

        private static AlgorithmSummaryViewModel BuildAlgorithmSummary(
            string algorithmName,
            string description,
            AllocationAlgorithmSummary summary,
            IReadOnlyDictionary<string, DemoVolunteer> volunteersById,
            IReadOnlyDictionary<string, DemoCause> causesById,
            IReadOnlyDictionary<string, string> otherAlgorithmPairs)
        {
            var priorities = summary.SelectedPriorities
                .OrderByDescending(p => p.CompatibilityScore)
                .ThenBy(p => p.VolunteerId, StringComparer.Ordinal)
                .Select(p => new PriorityPairViewModel
                {
                    VolunteerId = p.VolunteerId,
                    VolunteerName = volunteersById[p.VolunteerId].Name,
                    CauseId = p.CauseId,
                    CauseName = causesById[p.CauseId].Name,
                    CompatibilityScore = p.CompatibilityScore,
                    DiffersFromOtherAlgorithm = !otherAlgorithmPairs.TryGetValue(p.VolunteerId, out var otherCauseId)
                        || otherCauseId != p.CauseId
                })
                .ToList();

            return new AlgorithmSummaryViewModel
            {
                AlgorithmName = algorithmName,
                Description = description,
                PrioritizedVolunteerCount = summary.PrioritizedVolunteerCount,
                TotalCompatibilityScore = summary.TotalCompatibilityScore,
                SelectedPriorities = priorities
            };
        }
    }
}
