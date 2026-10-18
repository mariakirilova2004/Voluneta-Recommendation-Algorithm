
using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Baselines;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Experiments
{
    public static class ModelComparisonExperiment
    {
        public static IReadOnlyList<EvaluationResult> Run(int sampleCount = 1000, int seed = 42)
        {
            // STEP 1: Generate synthetic dataset
            TrainingDataset dataset = SyntheticDatasetGenerator.Generate(sampleCount, seed: seed);

            // STEP 2: Split dataset
            DatasetSplit split = DatasetSplitter.Split(dataset, seed: seed);

            // STEP 3: Train mean rating baseline
            var meanBaseline = new MeanRatingBaseline();

            meanBaseline.Fit(split.Training);

            // STEP 4: Train linear regression baseline
            var linearBaseline = new LinearScoreBaseline();

            linearBaseline.Fit(split.Training, epochs: 500, learningrate: 0.01);

            // STEP 5: Initialize neural network
            var neuralNetwork = new RecommendationNeuralNetwork(new Random(seed));

            var trainer = new NeuralNetworkTrainer(neuralNetwork, learningRate: 0.01);

            // STEP 6: Train neural network
            var options = new TrainingOptions
            {
                MaxEpochs = 200,
                UseEarlyStopping = true,
                Patience = 15,
                MinDelta = 0.00001
            };

            trainer.Train(split, options);

            // STEP 7: Evaluate on the SAME test dataset
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
