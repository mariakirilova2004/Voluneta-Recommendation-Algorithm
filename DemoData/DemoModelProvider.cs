using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Loads the pre-trained recommendation model (ModelArtifacts/recommendation-model.json) for the
    // local demo. Does not retrain or overwrite the model file - it is strictly a read-only loader
    // that hands back a fresh, restored RecommendationNeuralNetwork instance for each prediction session,
    // since Forward() caches mutable state internally and must not be shared across concurrent sessions.
    public sealed class DemoModelProvider
    {
        private const string ModelRelativePath = "ModelArtifacts/recommendation-model.json";

        private readonly string modelFilePath;
        private readonly NetworkSnapshot snapshot;

        public DemoModelProvider(string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                throw new ArgumentException("Content root path must be provided.", nameof(contentRootPath));
            }

            modelFilePath = Path.Combine(contentRootPath, ModelRelativePath);

            if (!File.Exists(modelFilePath))
            {
                throw new FileNotFoundException(
                    $"Recommendation model file was not found at '{modelFilePath}'.", modelFilePath);
            }

            string json;
            try
            {
                json = File.ReadAllText(modelFilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException(
                    $"Failed to read recommendation model file at '{modelFilePath}'.", ex);
            }

            try
            {
                snapshot = NetworkSnapshotSerializer.FromJson(json);
            }
            catch (Exception ex) when (ex is ArgumentException or System.Text.Json.JsonException)
            {
                throw new InvalidOperationException(
                    $"Recommendation model file at '{modelFilePath}' is invalid or corrupted.", ex);
            }
        }

        // Creates a fresh network and restores the validated snapshot onto it. A new instance is
        // returned on every call so that Forward()'s cached intermediate values are never shared
        // between separate prediction sessions.
        public RecommendationNeuralNetwork CreateSessionNetwork()
        {
            var network = new RecommendationNeuralNetwork(new Random());
            network.RestoreFromSnapshot(snapshot);
            return network;
        }
    }
}
