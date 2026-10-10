using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.NeuralNetwork
{
    public class DatasetSplitterTests
    {
        private static TrainingDataset CreateDataset(int count)
        {
            var dataset = new TrainingDataset();

            for (int i = 0; i < count; i++)
            {
                var features = new RecommendationFeatureVector
                {
                    InterestMatch = i / (double)count,
                    CategoryHistoryMatch = 0.5,
                    DayAvailableMatch = 1.0,
                    TimeAvailableMatch = 1.0,
                    LocationMatch = 0.8,
                    SkillMatch = 0.5,
                    ExperienceMatch = 1.0,
                    ColleagueMatch = 0.0
                };

                dataset.AddSingle(new TrainingSample(features, 8));
            }

            return dataset;
        }

        [Fact]
        public void Split_ProducesCorrectProportions()
        {
            // Arrange
            var dataset = CreateDataset(100);

            // Act
            var split = DatasetSplitter.Split(dataset);

            // Assert
            Assert.Equal(70, split.Training.Count);
            Assert.Equal(15, split.Validation.Count);
            Assert.Equal(15, split.Testing.Count);
        }

        [Fact]
        public void Split_DoesNotLoseSamples()
        {
            // Arrange
            var dataset = CreateDataset(100);

            // Act
            var split = DatasetSplitter.Split(dataset);

            // Assert
            int total = split.Training.Count + split.Validation.Count + split.Testing.Count;

            Assert.Equal(dataset.Count, total);
        }

        [Fact]
        public void Split_Deterministic()
        {
            // Arrange
            var dataset = CreateDataset(100);

            // Act
            var a = DatasetSplitter.Split(dataset);
            var b = DatasetSplitter.Split(dataset);
            
            // Assert
            for (int i = 0; i < a.Training.Count; i++)
            {
                Assert.Same(a.Training[i], b.Training[i]);
            }
        }

        [Fact]
        public void Split_NoDuplicates()
        {
            // Arrange
            var dataset = CreateDataset(100);

            // Act
            var split = DatasetSplitter.Split(dataset);

            var samples = split.Training.Concat(split.Validation).Concat(split.Testing).ToList();

            // Assert
            Assert.Equal(dataset.Count, samples.Distinct().Count());
        }
    }
}
