using System.Text.Json;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork
{
    public static class NetworkSnapshotSerializer
    {
        public static string ToJson(NetworkSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            Validate(snapshot);
            return JsonSerializer.Serialize(snapshot);
        }

        public static NetworkSnapshot FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Invalid JSON.");

            var snapshot = JsonSerializer.Deserialize<NetworkSnapshot>(json);
            if (snapshot == null) throw new ArgumentException("Invalid snapshot.");

            Validate(snapshot);
            return snapshot;
        }

        private static void Validate(NetworkSnapshot snapshot)
        {
            if (snapshot.Weights.Length != 264 || snapshot.Biases.Length != 25)
                throw new ArgumentException("Invalid snapshot size.");

            if (snapshot.Weights.Any(w => !double.IsFinite(w)) || snapshot.Biases.Any(b => !double.IsFinite(b)))
                throw new ArgumentException("Invalid snapshot values.");
        }
    }
}
