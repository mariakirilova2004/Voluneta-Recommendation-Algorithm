using WeVolunteer.Core.Algorithms.Recommendation;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork
{
    public class RecommendationNeuralNetwork
    {
        private readonly DenseLayer layer1;
        private readonly DenseLayer layer2;

        private readonly ReLUActivation reLU1;
        private readonly ReLUActivation reLU2;

        private readonly DenseLayer outputLayer;
        private readonly SigmoidActivation sigmoid;

        public DenseLayer Layer1 => layer1;
        public DenseLayer Layer2 => layer2;
        public DenseLayer OutputLayer => outputLayer;

        public RecommendationNeuralNetwork(Random rand)
        {
            layer1 = new DenseLayer(8, 16, rand);
            reLU1 = new ReLUActivation();
            layer2 = new DenseLayer(16, 8, rand);
            reLU2 = new ReLUActivation();
            outputLayer = new DenseLayer(8, 1, rand);
            sigmoid = new SigmoidActivation();
        }

        public double Forward(double[] inputs)
        {
            if (inputs.Length != 8)
            {
                throw new ArgumentException("Input array must have a length of 8.");
            }

            double[] hidden1 = layer1.Forward(inputs);
            double[] activated1 = reLU1.Forward(hidden1);

            double[] hidden2 = layer2.Forward(activated1);
            double[] activated2 = reLU2.Forward(hidden2);

            double[] output = outputLayer.Forward(activated2);
            double[] res = sigmoid.Forward(output);

            return res[0];
        }

        public double[] Backward(double loss)
        {
            double[] dValues = { loss };
            dValues = sigmoid.Backward(dValues);
            dValues = outputLayer.Backward(dValues);
            dValues = reLU2.Backward(dValues);
            dValues = layer2.Backward(dValues);
            dValues = reLU1.Backward(dValues);
            dValues = layer1.Backward(dValues);
            return dValues;
        }

        public void Update(SGD sgd)
        {
            sgd.Update(layer1);
            sgd.Update(layer2);
            sgd.Update(outputLayer);
        }

        public NetworkSnapshot GetSnapshot()
        {
            var layers  = new[] {layer1, layer2, outputLayer };

            var weights = new List<double>();
            var biases = new List<double>();

            foreach (var layer in layers)
            {
                for (int i = 0; i < layer.NeuronN; i++)
                {
                    for (int j = 0; j < layer.InputN; j++)
                    {
                        weights.Add(layer.Weights[i, j]);
                    }
                    biases.Add(layer.Biases[i]);
                }
            }

            return new NetworkSnapshot(weights.ToArray(), biases.ToArray());
        }

        public void RestoreFromSnapshot(NetworkSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var layers = new[] { layer1, layer2, outputLayer };

            int expectedWeights = layers.Sum(layer => layer.NeuronN * layer.InputN);
            int expectedBiases = layers.Sum(layer => layer.NeuronN);

            if (snapshot.Weights.Length != expectedWeights || snapshot.Biases.Length != expectedBiases)
            {
                throw new ArgumentException("Snapshot dimensions do not match the network.");
            }

            int weightIndex = 0;
            int biasIndex = 0;

            foreach (var layer in layers)
            {
                for (int n = 0; n < layer.NeuronN; n++)
                {
                    for (int i = 0; i < layer.InputN; i++)
                    {
                        layer.Weights[n, i] = snapshot.Weights[weightIndex++];
                    }

                    layer.Biases[n] = snapshot.Biases[biasIndex++];
                }
            }
        }
    }
}
