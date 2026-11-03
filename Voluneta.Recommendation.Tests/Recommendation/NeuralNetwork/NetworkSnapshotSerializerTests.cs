using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.NeuralNetwork
{
    public class NetworkSnapshotSerializerTests
    {
        [Fact]
        public void Serializer_RestoresTrainedModel()
        {
            //Arrange
            var dataset = SyntheticDatasetGenerator.Generate(120, seed: 42);
            var split = DatasetSplitter.Split(dataset, seed: 42);
            var network = new RecommendationNeuralNetwork(new Random(42));
            var trainer = new NeuralNetworkTrainer(network);

            trainer.Train(split, epochs: 25);

            double[] input = [0.8, 0.4, 0.7, 0.6, 0.9, 0.5, 0.3, 0.2];
            double original = network.Forward(input);

            //Act
            string json = NetworkSnapshotSerializer.ToJson(network.GetSnapshot());
            var snapshot = NetworkSnapshotSerializer.FromJson(json);

            var restoredNetwork = new RecommendationNeuralNetwork(new Random(123));
            restoredNetwork.RestoreFromSnapshot(snapshot);
            double restored = restoredNetwork.Forward(input);

            //Assert
            Assert.Equal(original, restored, 12);
            Assert.Equal(264, snapshot.Weights.Length);
            Assert.Equal(25, snapshot.Biases.Length);
        }

        [Fact]
        public void Serializer_RejectsInvalidSize()
        {
            //Arrange
            var snapshot = new NetworkSnapshot(new double[10], new double[5]);

            //Act & Assert
            Assert.Throws<ArgumentException>(() => NetworkSnapshotSerializer.ToJson(snapshot));
        }

        [Fact]
        public void Serializer_RejectsInvalidValues()
        {
            //Arrange
            var weights = new double[264];
            weights[0] = double.NaN;
            var snapshot = new NetworkSnapshot(weights, new double[25]);

            //Act & Assert
            Assert.Throws<ArgumentException>(() => NetworkSnapshotSerializer.ToJson(snapshot));
        }

        [Fact]
        public void Serializer_RejectsInvalidJson()
        {
            //Arrange
            string json = "{\"Weights\":[],\"Biases\":[]}";

            //Act & Assert
            Assert.Throws<ArgumentException>(() => NetworkSnapshotSerializer.FromJson(json));
        }
    }
}
