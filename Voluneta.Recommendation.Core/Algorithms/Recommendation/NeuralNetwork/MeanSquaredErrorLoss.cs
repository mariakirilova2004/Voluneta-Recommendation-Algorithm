namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork
{
    public class MeanSquaredErrorLoss
    {
        public double Forward(double actual, double expected) 
        {
            double error = actual - expected;
            return  error * error;
        }

        public double Backward(double actual, double expected)
        {
            return 2 * (actual - expected);
        }
    }
}
