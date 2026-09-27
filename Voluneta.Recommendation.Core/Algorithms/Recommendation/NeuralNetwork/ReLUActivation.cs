namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

public sealed class ReLUActivation
{
    public double[] Inputs { get; private set; } = Array.Empty<double>();
    public double[] Outputs { get; private set; } = Array.Empty<double>();
    
    public double[] Forward(double[] inputs)
    {
        Inputs = (double[])inputs.Clone();
        Outputs = new double[inputs.Length];

        for (int i = 0; i < inputs.Length; i++)
        {
            Outputs[i] = Math.Max(0, inputs[i]);
        }

        return Outputs;
    }

    public double[] Backward(double[] dValues)
    {

        if (dValues.Length != Inputs.Length)
        {
            throw new ArgumentException("dValues must match inputs.");
        }

        double[] dInputs = new double[dValues.Length];

        for (int i = 0; i < dValues.Length; i++)
        {
            dInputs[i] = Inputs[i] > 0 ? dValues[i] : 0;
        }

        return dInputs;
    }
}