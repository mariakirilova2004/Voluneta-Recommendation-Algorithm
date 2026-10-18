using System.Diagnostics.Contracts;

namespace WeVolunteer.Core.Algorithms.Recommendation
{
    public sealed class DenseLayer
    {
        public int InputN { get; }

        public int NeuronN { get; }

        public double[,] Weights { get; }

        public double[] Biases { get; }

        public double[] Inputs { get; private set; } = Array.Empty<double>();

        public double[] Outputs { get; private set; } = Array.Empty<double>();

        public double[,] DWeights { get; private set; }

        public double[] DBiases { get; private set; }

        public double[] DInputs { get; private set; }

        public DenseLayer(int inputN, int neuronN, Random rand)
        {
            InputN = inputN;
            NeuronN = neuronN;

            Weights = new double[NeuronN, InputN];
            Biases = new double[NeuronN];

            DWeights = new double[NeuronN, InputN];
            DBiases = new double[NeuronN];
            DInputs = new double[InputN];

            for (int n = 0; n < NeuronN; n++)
            {
                for (int i = 0; i < InputN; i++)
                {
                    double limit = Math.Sqrt(6.0 / inputN);

                    Weights[n, i] = (rand.NextDouble() * 2.0 - 1.0) * limit;
                }
                Biases[n] = 0;
            }
        }

        public double[] Forward(double[] inputs)
        {
            if (inputs.Length != InputN)
            {
                throw new ArgumentException($"Input length should be {InputN}, but was {inputs.Length}");
            }

            Inputs = (double[])inputs.Clone();
            Outputs = new double[NeuronN];

            // Going through all neurons and calculating the output according to the inputs given and weights for every single neuron
            for (int n = 0; n < NeuronN; n++)
            {
                double sum = 0;

                for (int i = 0; i < InputN; i++)
                {
                    // using the formula: output = input * weight(for the neuron) + bias
                    sum += inputs[i] * Weights[n, i];
                }

                sum += Biases[n];

                Outputs[n] = sum;
            }

            return Outputs;
        }

        public double[] Backward(double[] dvalues)
        {

            if (dvalues.Length != NeuronN)
            {
                throw new ArgumentException($"DValues should be {NeuronN}, but was {dvalues.Length}");
            }

            DWeights = new double[NeuronN, InputN];
            DBiases = new double[NeuronN];
            DInputs = new double[InputN];

            for (int n = 0; n < NeuronN; n++)
            {
                for (int i = 0; i < InputN; i++)
                {
                    DWeights[n, i] = Inputs[i] * dvalues[n];
                }

                DBiases[n] = dvalues[n];
            }

            for (int i = 0; i < InputN; i++)
            {
                for (int n = 0; n < NeuronN; n++)
                {
                    DInputs[i] += Weights[n, i] * dvalues[n];
                }
            }
            return DInputs;
        }
    }
}