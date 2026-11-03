using Recommendation.Core.Algorithms.Recommendation.Features;

namespace Voluneta.Recommendation.Core.Algorithms.Allocation.Integration
{
    public class VolunteerCauseInput
    {
        public string VolunteerId { get; set; }
        public string CauseId { get; set; }
        public RecommendationFeatureInput Features { get; set; }
        public bool IsEligible { get; set; }

        public VolunteerCauseInput(string volunteerId, string causeId, RecommendationFeatureInput features, bool isEligible)
        {
            VolunteerId = volunteerId;
            CauseId = causeId;
            Features = features;
            IsEligible = isEligible;
        }
    }
}
