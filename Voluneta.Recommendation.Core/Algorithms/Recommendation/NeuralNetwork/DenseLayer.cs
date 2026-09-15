using System.Diagnostics.Contracts;

namespace WeVolunteer.Core.Algorithms.Recommendation
{
    public sealed class DenseLayer
    {
        public int InputN { get; }

        public int NeuronN { get; }

        public double[,] Weights { get; }

        public double[] Biases { get; }

        public double[] Outputs { get; private set; } = Array.Empty<double>();

        public DenseLayer(int inputN, int neuronN, Random rand)
        {
            InputN = inputN;
            NeuronN = neuronN;

            Weights = new double[NeuronN, InputN];
            Biases = new double[NeuronN];

            for (int n = 0; n < NeuronN; n++)
            {
                for (int i = 0; i < InputN; i++)
                {
                    // The aim here is to have values for the weights between -0.01 and 0.01, in this way the values will be small and easy to manage
                    Weights[n, i] = (rand.NextDouble() * 2.0 - 1.0) * 0.01;
                }
            }
        }

        public double[] Forward(double[] inputs)
        {
            if (inputs.Length != InputN)
            {
                throw new ArgumentException($"Input length should be {InputN}, but was {inputs.Length}");
            }

            Outputs = new double[NeuronN];

            // Going through all neurons and calculating the output according to the inputs given and weights for every single neuron
            for (int n = 0; n < NeuronN; n++)
            {
                double NeuronOutput = 0;

                for (int i = 0; i < InputN; i++)
                {
                    // using the formula: output = input * weight(for the neuron) + bias
                    NeuronOutput += inputs[i] * Weights[n, i];
                }

                NeuronOutput += Biases[n];

                Outputs[n] = NeuronOutput;
            }

            return Outputs;
        }
    }
}