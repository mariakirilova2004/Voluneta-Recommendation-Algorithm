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
            ArgumentNullException.ThrowIfNull(dataset, nameof(dataset));

            if (epochs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(epochs));
            }

            if (patience <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(patience));
            }

            if (!double.IsFinite(minDelta) || minDelta < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minDelta));
            }

            if (dataset.Training.Count == 0 || dataset.Validation.Count == 0)
            {
                throw new ArgumentException("Training and validation datasets cannot be empty.", nameof(dataset));
            }

            var history = new TrainingHistory();

            NetworkSnapshot? bestSnap = null;

            double bestValidationLoss = double.MaxValue;
            double referenceLoss = double.MaxValue;

            int epochsWithoutImprovement = 0;

            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                foreach (var sample in dataset.Training)
                {
                    // Forward pass
                    double prediction = _network.Forward(sample.Features);
                    
                    // Compute loss
                    double gradient = _loss.Backward(prediction, sample.Target);
                    
                    // Backward pass
                    _network.Backward(gradient);

                    // Update weights
                    _network.Update(_optimizer);
                }

                // Evaluate on validation set
                double trainingLoss = Evaluate(dataset.Training);
                double validationLoss = Evaluate(dataset.Validation);

                if (!double.IsFinite(trainingLoss) || !double.IsFinite(validationLoss))
                {
                    throw new InvalidOperationException("Training or validation loss is not finite.");
                }
                
                history.Add(epoch, trainingLoss, validationLoss);

                if (validationLoss < bestValidationLoss - minDelta)
                {
                    bestValidationLoss = validationLoss;
                    bestSnap = _network.GetSnapshot();
                    
                    history.BestEpoch = epoch;
                    history.BestValidationLoss = bestValidationLoss;
                }

                if (referenceLoss - validationLoss > minDelta)
                {
                    referenceLoss = validationLoss;
                    epochsWithoutImprovement = 0;
                }

                else
                {
                    epochsWithoutImprovement++;
                }

                if (epochsWithoutImprovement >= patience)
                {
                    history.StoppedEarly = true;
                    break;
                }
            }

            if (bestSnap != null)
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
