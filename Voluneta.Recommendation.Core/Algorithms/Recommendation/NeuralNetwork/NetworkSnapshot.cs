namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork
{
    public sealed class NetworkSnapshot
    {
        public double[] Weights { get; } 

        public double[] Biases { get; }

        public NetworkSnapshot(double[] weights, double[] biases)
        {
            Weights = (double[])weights.Clone();
            Biases = (double[])biases.Clone();
        }
    }
}
