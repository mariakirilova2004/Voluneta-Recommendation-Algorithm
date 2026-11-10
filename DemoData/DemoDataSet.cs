namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Deterministic, hand-authored demo data: 4 volunteers, 3 causes, some completed-cause history
    // and some current registrations. No database access - everything is in-memory and fixed.
    public static class DemoDataSet
    {
        public static IReadOnlyList<DemoVolunteer> Volunteers { get; } =
        [
            new DemoVolunteer
            {
                Id = "V1",
                Name = "Maria Ivanova",
                Interests = "Animals,Environment",
                DaysAvailable = "Monday,Wednesday,Friday",
                PreferredTimeOfDay = "Morning (08:00-12:00)",
                Skills = "First Aid,Teamwork",
                HasPreviousExperience = true,
                CompanyId = 100,
                Latitude = 42.6977,
                Longitude = 23.3219,
                CompletedCauseCategoryIds = [1] // previously completed an Animals cause
            },
            new DemoVolunteer
            {
                Id = "V2",
                Name = "Georgi Petrov",
                Interests = "Education,Community",
                DaysAvailable = "Tuesday,Thursday",
                PreferredTimeOfDay = "Afternoon (12:00-17:00)",
                Skills = "Teaching,Communication",
                HasPreviousExperience = false,
                CompanyId = 200,
                Latitude = 42.1354,
                Longitude = 24.7453,
                CompletedCauseCategoryIds = []
            },
            new DemoVolunteer
            {
                Id = "V3",
                Name = "Elena Dimitrova",
                Interests = "Environment,Animals",
                DaysAvailable = "Saturday,Sunday",
                PreferredTimeOfDay = "Flexible",
                Skills = "Driving,Physical Labor",
                HasPreviousExperience = true,
                CompanyId = 100,
                Latitude = 43.2141,
                Longitude = 27.9147,
                CompletedCauseCategoryIds = [2] // previously completed an Environment cause
            },
            new DemoVolunteer
            {
                Id = "V4",
                Name = "Ivan Nikolov",
                Interests = "Community,Education",
                DaysAvailable = "Monday,Tuesday,Wednesday",
                PreferredTimeOfDay = "Evening (17:00+)",
                Skills = "Teamwork,Communication",
                HasPreviousExperience = false,
                CompanyId = 300,
                Latitude = 42.5048,
                Longitude = 27.4626,
                CompletedCauseCategoryIds = []
            }
        ];

        public static IReadOnlyList<DemoCause> Causes { get; } =
        [
            new DemoCause
            {
                Id = "C1",
                Name = "Animal Shelter Cleanup",
                CategoryName = "Animals",
                CategoryId = 1,
                StartDate = new DateTime(2025, 11, 15),
                Latitude = 42.6500,
                Longitude = 23.3800,
                RequiredSkills = "First Aid,Physical Labor",
                DifficultyLevel = 1,
                RemainingPlaces = 3,
                Place = "Sofia Animal Shelter, Sofia",
                Description = "Help clean enclosures, walk and socialize shelter animals, and assist staff with daily care routines.",
                MaxVolunteers = 10,
                ImagePath = "/images/causes/animal-shelter-cleanup.jpg"
            },
            new DemoCause
            {
                Id = "C2",
                Name = "Reforestation Day",
                CategoryName = "Environment",
                CategoryId = 2,
                StartDate = new DateTime(2025, 11, 22),
                Latitude = 43.2000,
                Longitude = 27.9000,
                RequiredSkills = "Physical Labor,Driving",
                DifficultyLevel = 2,
                RemainingPlaces = 2,
                Place = "Varna Coastal Forest, Varna",
                Description = "Plant saplings and restore woodland along the coastal reserve as part of a long-term reforestation effort.",
                MaxVolunteers = 6,
                ImagePath = "/images/causes/reforestation-day.jpg"
            },
            new DemoCause
            {
                Id = "C3",
                Name = "Community Tutoring",
                CategoryName = "Education",
                CategoryId = 3,
                StartDate = new DateTime(2025, 12, 1),
                Latitude = 42.1500,
                Longitude = 24.7500,
                RequiredSkills = "Teaching,Communication",
                DifficultyLevel = 0,
                RemainingPlaces = 1,
                Place = "Plovdiv Community Center, Plovdiv",
                Description = "Provide after-school tutoring in core subjects for local students needing extra academic support.",
                MaxVolunteers = 4,
                ImagePath = "/images/causes/community-tutoring.jpg"
            }
        ];

        // Current registrations (RegisteredParticipants) - used to compute ColleagueMatch.
        public static IReadOnlyList<DemoRegistration> Registrations { get; } =
        [
            new DemoRegistration { VolunteerId = "V3", CauseId = "C1" }, // same company as V1 (100)
            new DemoRegistration { VolunteerId = "V2", CauseId = "C3" },
            new DemoRegistration { VolunteerId = "V4", CauseId = "C3" }
        ];
    }
}
