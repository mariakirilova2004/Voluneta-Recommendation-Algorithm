using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;

namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public class NeuralNetworkTrainer
    {
        private readonly RecommendationNeuralNetwork _network;
        private readonly MeanSquaredErrorLoss _loss;
        private readonly SGD _optimizer;

        public NeuralNetworkTrainer(RecommendationNeuralNetwork network, double learningRate = 0.01)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _loss = new MeanSquaredErrorLoss();
            _optimizer = new SGD(learningRate);
        }

        public TrainingHistory Train(DatasetSplit dataset, int epochs = 100, int patience = 10, double minDelta = 0.00001)
        {
            return Train(dataset, new TrainingOptions{ MaxEpochs = epochs, UseEarlyStopping = true, Patience = patience, MinDelta = minDelta });
        }

        public TrainingHistory Train(DatasetSplit dataset, TrainingOptions options)
        {
            ArgumentNullException.ThrowIfNull(dataset);
            ArgumentNullException.ThrowIfNull(options);

            if (options.MaxEpochs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxEpochs));
            }

            if (options.Patience <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.Patience));
            }

            if (!double.IsFinite(options.MinDelta) || options.MinDelta < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MinDelta));
            }

            if (dataset.Training.Count == 0 || dataset.Validation.Count == 0)
            {
                throw new ArgumentException(
                    "Training and validation datasets cannot be empty.",
                    nameof(dataset));
            }

            var history = new TrainingHistory();

            NetworkSnapshot? bestSnap = null;

            double bestValidationLoss = double.MaxValue;
            double referenceLoss = double.MaxValue;

            int epochsWithoutImprovement = 0;

            for (int epoch = 1; epoch <= options.MaxEpochs; epoch++)
            {
                foreach (var sample in dataset.Training)
                {
                    // Forward pass
                    double prediction = _network.Forward(sample.Features);

                    // Compute loss gradient
                    double gradient = _loss.Backward(
                        prediction,
                        sample.Target);

                    // Backpropagation
                    _network.Backward(gradient);

                    // Update weights
                    _network.Update(_optimizer);
                }

                // Evaluate training and validation datasets
                double trainingLoss = Evaluate(dataset.Training);
                double validationLoss = Evaluate(dataset.Validation);

                if (!double.IsFinite(trainingLoss) ||
                    !double.IsFinite(validationLoss))
                {
                    throw new InvalidOperationException(
                        "Training or validation loss is not finite.");
                }

                history.Add(epoch, trainingLoss, validationLoss);

                // Track the absolute best validation result
                if (validationLoss < bestValidationLoss)
                {
                    bestValidationLoss = validationLoss;

                    history.BestEpoch = epoch;
                    history.BestValidationLoss = validationLoss;

                    if (options.UseEarlyStopping)
                    {
                        bestSnap = _network.GetSnapshot();
                    }
                }

                // Early stopping is optional
                if (options.UseEarlyStopping)
                {
                    if (referenceLoss - validationLoss > options.MinDelta)
                    {
                        referenceLoss = validationLoss;
                        epochsWithoutImprovement = 0;
                    }
                    else
                    {
                        epochsWithoutImprovement++;
                    }

                    if (epochsWithoutImprovement >= options.Patience)
                    {
                        history.StoppedEarly = true;
                        break;
                    }
                }
            }

            // Restore the best model only when early stopping is enabled
            if (options.UseEarlyStopping && bestSnap != null)
            {
                _network.RestoreFromSnapshot(bestSnap);
            }

            return history;
        }

        public double Evaluate(IReadOnlyList<TrainingSample> dataset)
        {
            ArgumentNullException.ThrowIfNull(dataset, nameof(dataset));

            if (dataset.Count == 0)
            {
                throw new ArgumentException("Dataset cannot be empty.", nameof(dataset));
            }

            double totalLoss = 0.0;

            foreach (var sample in dataset)
            {
                double prediction = _network.Forward(sample.Features);

                totalLoss += _loss.Forward(prediction, sample.Target);
            }

            return totalLoss / dataset.Count;
        }
    }
}
