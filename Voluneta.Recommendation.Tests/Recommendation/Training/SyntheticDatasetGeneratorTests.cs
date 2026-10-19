using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Training
{
    public class SyntheticDatasetGeneratorTests
    {
        [Fact]
        public void Generate_Samples()
        {
            // Arrange + Act
            var dataset = SyntheticDatasetGenerator.Generate(1000);

            // Assert
            Assert.Equal(1000, dataset.Count);
        }

        [Fact]
        public void Generate_TargetsAndFeatures()
        {
            // Arrange + Act
            var dataset = SyntheticDatasetGenerator.Generate(1000);

            // Assert
            foreach (var sample in dataset.Dataset)
            {
                Assert.Equal(8, sample.Features.Length);

                Assert.All(sample.Features, feature => Assert.InRange(feature, 0.0, 1.0));

                Assert.InRange(sample.Target, 0.0, 1.0);
            }
        }

        [Fact]
        public void Generate_Deterministic()
        {
            // Arrange + Act
            var first = SyntheticDatasetGenerator.Generate(100, seed: 42);

            var second = SyntheticDatasetGenerator.Generate(100, seed: 42);

            // Assert
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first.Dataset[i].Target, second.Dataset[i].Target);

                Assert.Equal(first.Dataset[i].Features, second.Dataset[i].Features);
            }
        }

        [Fact]
        public void Generate_InvalidSampleCount()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticDatasetGenerator.Generate(0));
        }

        [Fact]
        public void Generate_RejectsNoise()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticDatasetGenerator.Generate(100, noise: -0.1));
        }
    }
}
