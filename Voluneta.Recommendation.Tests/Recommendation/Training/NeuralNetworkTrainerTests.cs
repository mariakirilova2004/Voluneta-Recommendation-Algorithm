using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

namespace Voluneta.Recommendation.Tests.Recommendation.Training
{
    // tests for the neural network trainer, just making sure training actually works and loss values make sense
    public class NeuralNetworkTrainerTests
    {
        [Fact]
        public void Train_TrainingHistory()
        {
            // step 1 - generate some fake data to train on (seed is fixed so results are the same every time we run it)
            var dataset = SyntheticDatasetGenerator.Generate(1000, seed: 42);

            // step 2 - split it into training and validation sets
            var split = DatasetSplitter.Split(dataset, seed: 42);

            // step 3 - make a new network, also with fixed seed so weights start the same every time
            var network = new RecommendationNeuralNetwork(new Random(42));

            // step 4 - create the trainer that will actually do the training
            var trainer = new NeuralNetworkTrainer(network, learningRate: 0.01);

            // step 5 - train it for 100 epochs
            var history = trainer.Train(split, epochs: 100);

            // step 6 - check that we actually got 100 epochs of history back
            Assert.InRange(history.Epoch.Count, 1, 100);

            Assert.Equal(history.StoppedEarly, history.Epoch.Count < 100);

            Assert.InRange(history.BestEpoch, 1, history.Epoch.Count);

            // and check that none of the loss values are weird (NaN, infinity, negative, etc)
            foreach (var epoch in history.Epoch)
            {
                Assert.True(double.IsFinite(epoch.TrainingLoss));
                Assert.True(double.IsFinite(epoch.ValidationLoss));
                Assert.True(epoch.TrainingLoss >= 0);
                Assert.True(epoch.ValidationLoss >= 0);
            }

            // just printing stuff out so I can see what's happening when I run the tests manually
            foreach (var epoch in history.Epoch)
            {
                Console.WriteLine("Epoch " + epoch.EpochNumber + ": Train=" + epoch.TrainingLoss.ToString("F6") + ", Validation=" + epoch.ValidationLoss.ToString("F6"));
            }

            Console.WriteLine("First 10 predictions:");

            int count = 0;

            foreach (var sample in split.Validation)
            {
                if (count >= 10)
                {
                    break;
                }

                double prediction = network.Forward(sample.Features);

                Console.WriteLine("Prediction=" + prediction.ToString("F6") + ", Target=" + sample.Target.ToString("F6"));

                count++;
            }
        }

        [Fact]
        public void Snapshot_Restores()
        {
            // Arrange - make a network and some sample input
            var network = new RecommendationNeuralNetwork(new Random(42));

            double[] input = { 0.8, 0.4, 0.7, 0.6, 0.9, 0.5, 0.3, 0.2 };

            // Act - get the original prediction and take a snapshot of the weights before changing anything
            double original = network.Forward(input);

            var snapshot = network.GetSnapshot();

            // now let's mess with the parameters a bit and see if the prediction actually changes
            var modified = network.GetSnapshot();

            modified.Biases[modified.Biases.Length - 1] += 2.0;

            network.RestoreFromSnapshot(modified);

            double changed = network.Forward(input);

            // Assert - prediction should be different now since we changed a bias
            Assert.NotEqual(original, changed);

            // now put back the original snapshot and make sure we get the exact same prediction as before
            network.RestoreFromSnapshot(snapshot);

            double restored = network.Forward(input);

            Assert.Equal(original, restored, 12);
        }

        [Fact]
        public void Train_StopsEarlyAndRestoresBestModel()
        {
            // same setup as before, generate data, split it, make a network
            var dataset = SyntheticDatasetGenerator.Generate(1000, seed: 42);

            var split = DatasetSplitter.Split(dataset, seed: 42);

            var network = new RecommendationNeuralNetwork(new Random(42));

            var trainer = new NeuralNetworkTrainer(network, learningRate: 0.01);

            // this time we give it patience and minDelta so it can stop early if it's not improving anymore
            var history = trainer.Train(split, epochs: 200, patience: 10, minDelta: 0.00001);

            // it should have run somewhere between 1 and 200 epochs (early stopping might kick in before 200)
            Assert.InRange(history.Epoch.Count, 1, 200);

            Assert.InRange(history.BestEpoch, 1, history.Epoch.Count);

            // after training, the network should be restored to the best epoch's weights
            // so if we evaluate again now, we should get basically the same loss as the recorded best one
            double restoredValidationLoss = trainer.Evaluate(split.Validation);

            Assert.Equal(history.BestValidationLoss, restoredValidationLoss, 10);

            // printing some debug info just to see what happened during training
            Console.WriteLine("Executed epochs: " + history.Epoch.Count);
            Console.WriteLine("Stopped early: " + history.StoppedEarly);
            Console.WriteLine("Best epoch: " + history.BestEpoch);
            Console.WriteLine("Best validation loss: " + history.BestValidationLoss.ToString("F8"));
            Console.WriteLine("Restored validation loss: " + restoredValidationLoss.ToString("F8"));
        }

        [Fact]
        public void Train_StoppingDisabled()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(100, seed: 42);

            var split = DatasetSplitter.Split(dataset, seed: 42);

            var network = new RecommendationNeuralNetwork(new Random(42));

            var trainer = new NeuralNetworkTrainer(network, 0.01);

            var options = new TrainingOptions
            {
                MaxEpochs = 100,
                UseEarlyStopping = false,
            };

            // Act
            // this time we disable early stopping
            var history = trainer.Train(split, options);

            // Assert
            // it should have run exactly 100 epochs since early stopping is disabled
            Assert.Equal(100, history.Epoch.Count);
            Assert.False(history.StoppedEarly);
            Assert.Equal(100, history.Epoch.Last().EpochNumber);
        }

        [Fact]
        public void Train_NotImprove()
        {
            // Arrange
            var dataset = SyntheticDatasetGenerator.Generate(100, seed: 42);

            var split = DatasetSplitter.Split(dataset, seed: 42);

            var network = new RecommendationNeuralNetwork(new Random(42));

            var trainer = new NeuralNetworkTrainer(network, 0.01);

            var options = new TrainingOptions
            {
                MaxEpochs = 100,
                UseEarlyStopping = true,
                Patience = 5,
                MinDelta = 1
            };

            // Act
            var history = trainer.Train(split, options);

            // Assert
            // it should have stopped early due to lack of improvement in validation loss
            Assert.True(history.StoppedEarly);
            Assert.Equal(6, history.Epoch.Count);
            Assert.InRange(history.BestEpoch, 1, 6);
        }

        [Fact]
        public void Train_WhenEarlyStoppingRestoresBestWeights_MatchesBestEpoch()
        {
            // Arrange
            var features = new RecommendationFeatureVector
            {
                InterestMatch = 0.5,
                CategoryHistoryMatch = 0.5,
                DayAvailableMatch = 0.5,
                TimeAvailableMatch = 0.5,
                LocationMatch = 0.5,
                SkillMatch = 0.5,
                ExperienceMatch = 0.5,
                ColleagueMatch = 0.5
            };

            var trainingSample = new TrainingSample(features, rating: 10); // Target = 1.0
            var validationSample = new TrainingSample(features, rating: 1); // Target = 0.0

            var split = new DatasetSplit(
                training: new List<TrainingSample> { trainingSample },
                testing: new List<TrainingSample>(),
                validation: new List<TrainingSample> { validationSample });

            var networkA = new RecommendationNeuralNetwork(new Random(42));
            var networkB = new RecommendationNeuralNetwork(new Random(42));

            var trainerA = new NeuralNetworkTrainer(networkA, 0.01);
            var trainerB = new NeuralNetworkTrainer(networkB, 0.01);

            var optionsB = new TrainingOptions
            {
                MaxEpochs = 100,
                UseEarlyStopping = true,
                Patience = 3,
                MinDelta = 0
            };

            // Act
            var historyB = trainerB.Train(split, optionsB);

            // Assert
            Assert.True(historyB.StoppedEarly);
            Assert.True(historyB.BestEpoch < historyB.Epoch.Count);

            // Arrange
            var optionsA = new TrainingOptions
            {
                MaxEpochs = historyB.BestEpoch,
                UseEarlyStopping = false
            };

            // Act
            trainerA.Train(split, optionsA);

            var expected = networkA.GetSnapshot();
            var actual = networkB.GetSnapshot();

            // Assert
            Assert.Equal(264, expected.Weights.Length);
            Assert.Equal(25, expected.Biases.Length);

            Assert.Equal(expected.Weights, actual.Weights); 
            Assert.Equal(expected.Biases, actual.Biases);
        }
    }
}
