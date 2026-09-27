namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork
{
    public class SigmoidActivation
    {
        public double[] Output { get; private set;  } = new double[0];

        public double[] Forward(double[] inputs)
        {
            Output = new double[inputs.Length];

            for (int i = 0; i < inputs.Length; i++)
            {
                Output[i] = 1.0 / (1.0 + Math.Exp(-inputs[i]));
            }

            return Output;
        }

        public double[] Backward(double[] dValues)
        {
            if(Output.Length != dValues.Length)
            {
                throw new ArgumentException("The length of dValues must match the length of the output.");
            }

            double[] dInputs = new double[dValues.Length];

            for (int i = 0; i < dValues.Length; i++)
            {
                double sigmoidDer = Output[i] * (1 - Output[i]);
                dInputs[i] = dValues[i] * sigmoidDer;
            }

            return dInputs;
        }
    }
}
