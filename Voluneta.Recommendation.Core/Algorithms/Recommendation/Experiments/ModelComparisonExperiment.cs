using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Experiments
{
    public static class ModelComparisonExperiment
    {
        public static IReadOnlyList<EvaluationResult> Run(int sampleCount = 1000, int seed = 42, double neuralLearningRate = 0.01, int neuralMaxEpochs = 200, bool neuralUseEarlyStopping = true)
        {
            TrainingDataset dataset = SyntheticDatasetGenerator.Generate(sampleCount, seed: seed);
            DatasetSplit split = DatasetSplitter.Split(dataset, seed: seed);

            var meanBaseline = new MeanRatingBaseline();
            meanBaseline.Fit(split.Training);

            var linearBaseline = new LinearScoreBaseline();
            linearBaseline.Fit(split.Training, epochs: 500, learningrate: 0.01);

            var neuralNetwork = new RecommendationNeuralNetwork(new Random(seed));
            var trainer = new NeuralNetworkTrainer(neuralNetwork, learningRate: neuralLearningRate);
            var options = new TrainingOptions { MaxEpochs = neuralMaxEpochs, UseEarlyStopping = neuralUseEarlyStopping, Patience = 15, MinDelta = 0.00001 };
            trainer.Train(split, options);

            var results = new List<EvaluationResult>
            {
                ModelEvaluator.Evaluate("Mean Rating Baseline", split.Testing, sample => meanBaseline.Predict()),
                ModelEvaluator.Evaluate("Linear Score Baseline", split.Testing, sample => linearBaseline.Predict(ToFeatureVector(sample.Features))),
                ModelEvaluator.Evaluate("Neural Network", split.Testing, sample => neuralNetwork.Forward(sample.Features))
            };

            return results;
        }

        private static RecommendationFeatureVector ToFeatureVector(double[] features)
        {
            return new RecommendationFeatureVector
            {
                InterestMatch = features[0],
                CategoryHistoryMatch = features[1],
                DayAvailableMatch = features[2],
                TimeAvailableMatch = features[3],
                LocationMatch = features[4],
                SkillMatch = features[5],
                ExperienceMatch = features[6],
                ColleagueMatch = features[7]
            };
        }
    }
}
