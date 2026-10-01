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
    }
}
