namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

public sealed class ReLUActivation
{
    public double[] Outputs { get; private set; } = Array.Empty<double>();
    
    public double[] Forward(double[] inputs)
    {
        Outputs = new double[inputs.Length];

        for (int i = 0; i < inputs.Length; i++)
        {
            Outputs[i] = Math.Max(0, inputs[i]);
        }

        return Outputs;
    }
}