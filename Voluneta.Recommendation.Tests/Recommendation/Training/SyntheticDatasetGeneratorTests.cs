using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Training
{
    public class SyntheticDatasetGeneratorTests
    {
        [Fact]
        public void CreatesSamples()
        {
            // Arrange
            int sampleCount = 1000;

            // Act
            var dataset = SyntheticDatasetGenerator.Generate(sampleCount);

            // Assert
            Assert.Equal(1000, dataset.Count);
        }

        [Fact]
        public void HasValidFeatures()
        {
            // Arrange
            int sampleCount = 1000;

            // Act
            var dataset = SyntheticDatasetGenerator.Generate(sampleCount);

            // Assert
            foreach (var sample in dataset.Dataset)
            {
                Assert.Equal(8, sample.Features.Length);
                Assert.All(sample.Features, feature => Assert.InRange(feature, 0.0, 1.0));
                Assert.InRange(sample.Target, 0.0, 1.0);
            }
        }

        [Fact]
        public void IsDeterministic()
        {
            // Arrange
            int seed = 42;

            // Act
            var first = SyntheticDatasetGenerator.Generate(100, seed: seed);
            var second = SyntheticDatasetGenerator.Generate(100, seed: seed);

            // Assert
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first.Dataset[i].Target, second.Dataset[i].Target);
                Assert.Equal(first.Dataset[i].Features, second.Dataset[i].Features);
            }
        }

        [Fact]
        public void RejectsZeroCount()
        {
            // Arrange
            int sampleCount = 0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticDatasetGenerator.Generate(sampleCount));
        }

        [Fact]
        public void RejectsNegativeNoise()
        {
            // Arrange
            double noise = -0.1;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticDatasetGenerator.Generate(100, noise: noise));
        }

        [Fact]
        public void UsesDiscreteFeatures()
        {
            // Arrange
            double[] binaryValues = { 0.0, 1.0 };
            double[] experienceValues = { 0.0, 0.5, 1.0 };

            // Act
            var dataset = SyntheticDatasetGenerator.Generate(1000);

            // Assert
            foreach (var sample in dataset.Dataset)
            {
                Assert.Contains(sample.Features[0], binaryValues);
                Assert.Contains(sample.Features[2], binaryValues);
                Assert.Contains(sample.Features[3], binaryValues);
                Assert.Contains(sample.Features[6], experienceValues);
            }
        }
    }
}
