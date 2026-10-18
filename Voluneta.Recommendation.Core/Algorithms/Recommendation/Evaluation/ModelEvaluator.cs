using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation
{
    // This class is used to evaluate how good our model is doing
    // by comparing the predicted values to the actual values
    public class ModelEvaluator
    {
        public static EvaluationResult Evaluate(string modelName, IReadOnlyList<TrainingSample> testSamples, Func<TrainingSample, double> predict)
        {
            // check if the model name was given, if not throw an error
            if (modelName == null || modelName.Trim() == "")
            {
                throw new ArgumentException("Model name cannot be empty.", nameof(modelName));
            }

            // check that we actually got some test data and a predict function
            if (testSamples == null)
            {
                throw new ArgumentNullException(nameof(testSamples));
            }

            if (predict == null)
            {
                throw new ArgumentNullException(nameof(predict));
            }

            // make sure there is at least one sample to test on
            if (testSamples.Count == 0)
            {
                throw new ArgumentException("Test dataset cannot be empty.", nameof(testSamples));
            }

            // these lists will hold the real values and the predicted values
            var actual = new List<double>();
            var predicted = new List<double>();

            // go through every test sample one by one
            for (int i = 0; i < testSamples.Count; i++)
            {
                var sample = testSamples[i];

                // the real target value we are comparing against
                actual.Add(sample.Target);

                // what the model predicted for this sample
                double predictedValue = predict(sample);
                predicted.Add(predictedValue);
            }

            // calculate how far off our predictions were, using two different error measures
            double mae = RegressionMetrics.MAE(actual, predicted);
            double rmse = RegressionMetrics.RMSE(actual, predicted);

            // put everything together and return the result
            return new EvaluationResult(
                modelName,
                mae,
                rmse,
                testSamples.Count);
        }
    }
}

