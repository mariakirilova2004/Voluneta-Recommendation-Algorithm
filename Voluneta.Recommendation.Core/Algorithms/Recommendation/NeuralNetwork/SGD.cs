using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork
{
    public class SGD
    {
        public double LearningRate { get; set; }

        public SGD(double learningRate = 0.01)
        {
            if(learningRate <= 0)
            {
                throw new ArgumentException("Learning rate must be positive.", nameof(learningRate));
            }

            LearningRate = learningRate;
        }

        public void Update(DenseLayer layer)
        { 
            for (int n = 0; n < layer.NeuronN; n++)
            {
                for (int i = 0; i < layer.InputN; i++)
                {
                    layer.Weights[n, i] -= LearningRate * layer.DWeights[n, i];
                }

                layer.Biases[n] -= LearningRate * layer.DBiases[n];
            }
        }
    }
}
