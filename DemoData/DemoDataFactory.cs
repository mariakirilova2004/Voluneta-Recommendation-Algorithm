using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Integration;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;

namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Converts the static demo data into the existing Core pipeline input types.
    // This class performs no feature math and no allocation logic - it only maps demo
    // records onto RecommendationFeatureInput / VolunteerCauseInput / CauseCapacity.
    public static class DemoDataFactory
    {
        // When volunteerOverride is supplied, its field values replace the matching DemoDataSet
        // volunteer (by Id) for feature-input purposes only. DemoDataSet itself is never mutated,
        // and all other volunteers continue to use their original, unmodified profiles.
        public static IReadOnlyList<VolunteerCauseInput> BuildVolunteerCauseInputs(DemoVolunteer? volunteerOverride = null)
        {
            var registeredByCause = DemoDataSet.Registrations
                .GroupBy(r => r.CauseId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyCollection<ColleagueParticipant>)g
                        .Select(r => DemoDataSet.Volunteers.First(v => v.Id == r.VolunteerId))
                        .Select(v => new ColleagueParticipant(v.Id, v.CompanyId))
                        .ToList());

            var inputs = new List<VolunteerCauseInput>();

            foreach (var baseVolunteer in DemoDataSet.Volunteers)
            {
                var volunteer = volunteerOverride != null && volunteerOverride.Id == baseVolunteer.Id
                    ? volunteerOverride
                    : baseVolunteer;

                foreach (var cause in DemoDataSet.Causes)
                {
                    var features = new RecommendationFeatureInput
                    {
                        VolunteerInterests = volunteer.Interests,
                        CategoryName = cause.CategoryName,
                        CategoryId = cause.CategoryId,
                        CompletedCauses = volunteer.CompletedCauseCategoryIds
                            .Select(categoryId => new CompletedCause(categoryId))
                            .ToList(),
                        DaysAvailable = volunteer.DaysAvailable,
                        PreferredTimeOfDay = volunteer.PreferredTimeOfDay,
                        CauseStart = cause.StartDate,
                        UserLatitude = volunteer.Latitude,
                        UserLongitude = volunteer.Longitude,
                        CauseLatitude = cause.Latitude,
                        CauseLongitude = cause.Longitude,
                        UserSkills = volunteer.Skills,
                        RequiredSkills = cause.RequiredSkills,
                        HasPreviousExperience = volunteer.HasPreviousExperience,
                        DifficultyLevel = cause.DifficultyLevel,
                        UserId = volunteer.Id,
                        CompanyId = volunteer.CompanyId,
                        RegisteredParticipants = registeredByCause.TryGetValue(cause.Id, out var participants)
                            ? participants
                            : []
                    };

                    bool isEligible = cause.RemainingPlaces > 0;

                    inputs.Add(new VolunteerCauseInput(volunteer.Id, cause.Id, features, isEligible));
                }
            }

            return inputs;
        }

        public static IReadOnlyList<CauseCapacity> BuildCauseCapacities()
        {
            return DemoDataSet.Causes
                .Select(cause => new CauseCapacity(cause.Id, cause.RemainingPlaces))
                .ToList();
        }
    }
}
