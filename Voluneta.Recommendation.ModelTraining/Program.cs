using Recommendation.Core.Algorithms.Recommendation.Features;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Integration;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Models;
using Voluneta.Recommendation.Core.Algorithms.Allocation.Reporting;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.NeuralNetwork;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Reporting;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Training;

// Reproducible evaluation: compares Mean Rating Baseline, Linear Score Baseline and the
// Custom Neural Network across fixed seeds on synthetic data, and exports CSV/Markdown
// artifacts into ExperimentResults/. Does not train or overwrite recommendation-model.json.
if (args.Length > 0 && args[0] == "evaluate")
{
    string outputDirectory = Path.GetFullPath(args.Length > 1 ? args[1] : "ExperimentResults");

    var (runs, summary) = NeuralEvaluationReportGenerator.GenerateAndSave(outputDirectory);

    Console.WriteLine($"Evaluated {runs.Count} runs across {summary.Count} models.");
    foreach (var model in summary)
    {
        Console.WriteLine($"{model.ModelName}: MeanMAE={model.MeanMAE:F4} (StdMAE={model.StdMAE:F4}), MeanRMSE={model.MeanRMSE:F4} (StdRMSE={model.StdRMSE:F4})");
    }
    Console.WriteLine($"Results written to: {outputDirectory}");
    return;
}

// Reproducible evaluation: compares MinCostMaxFlowPriorityAllocator and GreedyPriorityAllocator
// (Algorithm 2) on identical constructed candidate/capacity/threshold scenarios, and exports
// CSV/Markdown artifacts into ExperimentResults/. Does not modify either allocator.
if (args.Length > 0 && args[0] == "allocation-evaluate")
{
    string allocationOutputDirectory = Path.GetFullPath(args.Length > 1 ? args[1] : "ExperimentResults");

    var (scenarios, rows) = AllocationEvaluationReportGenerator.GenerateAndSave(allocationOutputDirectory);

    Console.WriteLine($"Evaluated {scenarios.Count} scenarios across {AllocationEvaluationRunner.Thresholds.Length} thresholds ({rows.Count} rows).");
    foreach (var row in rows)
    {
        Console.WriteLine($"{row.ScenarioName} | threshold={row.Threshold:F2} | {row.AlgorithmName}: count={row.PrioritizedVolunteerCount}, total={row.TotalCompatibilityScore:F4}, avg={row.AverageSelectedCompatibilityScore:F4}, util={row.CapacityUtilization:F4}");
    }
    Console.WriteLine($"Results written to: {allocationOutputDirectory}");
    return;
}

// Reproducible performance benchmark: measures MinCostMaxFlowPriorityAllocator and
// GreedyPriorityAllocator (Algorithm 2) on deterministic synthetic candidate pools of
// increasing size, using Stopwatch with a warm-up run and 5 measured repetitions per
// scenario/algorithm. Exports CSV/Markdown artifacts into ExperimentResults/. Does not
// modify either allocator.
if (args.Length > 0 && args[0] == "allocation-benchmark")
{
    string benchmarkOutputDirectory = Path.GetFullPath(args.Length > 1 ? args[1] : "ExperimentResults");

    var (scenarios, rows) = AllocationPerformanceReportGenerator.GenerateAndSave(benchmarkOutputDirectory);

    Console.WriteLine($"Benchmarked {scenarios.Count} scenarios across 2 algorithms ({rows.Count} rows).");
    foreach (var row in rows)
    {
        Console.WriteLine($"{row.ScenarioName} | {row.AlgorithmName}: edges={row.CandidateEdgeCount}, median={row.MedianMilliseconds:F4}ms, mean={row.MeanMilliseconds:F4}ms, min={row.MinMilliseconds:F4}ms, prioritized={row.PrioritizedVolunteerCount}");
    }
    Console.WriteLine($"Results written to: {benchmarkOutputDirectory}");
    return;
}

// Predict using the saved model
if (args.Length > 0 && args[0] == "predict")
{
    string modelPath = Path.GetFullPath(args.Length > 1 ? args[1] : "ModelArtifacts/recommendation-model.json");

    // Load the trained model
    string predictJson = File.ReadAllText(modelPath);
    var predictSnapshot = NetworkSnapshotSerializer.FromJson(predictJson);
    var predictNetwork = new RecommendationNeuralNetwork(new Random(42));
    predictNetwork.RestoreFromSnapshot(predictSnapshot);

    var pipeline = new RecommendationPipelineService(predictNetwork);

    // Sample volunteer-cause pairs
    VolunteerCauseInput[] inputs =
    [
        new("Maria", "Animals", new RecommendationFeatureInput { VolunteerInterests = "Animals", CategoryName = "Animals", DifficultyLevel = 0 }, true),
        new("Maria", "Environment", new RecommendationFeatureInput { VolunteerInterests = "Animals", CategoryName = "Environment", DifficultyLevel = 0 }, true),
        new("Ivan", "Animals", new RecommendationFeatureInput { VolunteerInterests = "Environment", CategoryName = "Animals", DifficultyLevel = 0 }, true),
        new("Ivan", "Environment", new RecommendationFeatureInput { VolunteerInterests = "Environment", CategoryName = "Environment", DifficultyLevel = 0 }, true)
    ];

    CauseCapacity[] capacities = [new("Animals", 1), new("Environment", 1)];

    // Generate recommendations
    var results = pipeline.Generate(inputs, capacities, 0.0);

    foreach (var group in results.GroupBy(r => r.VolunteerId).OrderBy(g => g.Key))
    {
        Console.WriteLine($"\nRecommendations for {group.Key}:");

        foreach (var r in group.OrderBy(r => r.Rank))
            Console.WriteLine($"{r.Rank}. {r.CauseId} | Score: {r.CompatibilityScore:F4} | Priority: {r.IsPriority}");
    }

    Console.WriteLine($"\nModel loaded from: {modelPath}");
    return;
}

// Generate the dataset
var dataset = SyntheticDatasetGenerator.Generate(1000, seed: 42);
var split = DatasetSplitter.Split(dataset, seed: 42);

// Train the network
var network = new RecommendationNeuralNetwork(new Random(42));
var trainer = new NeuralNetworkTrainer(network, learningRate: 0.01);
var history = trainer.Train(split, epochs: 100);

// Save the trained model
string filePath = Path.GetFullPath(args.Length > 0 ? args[0] : "ModelArtifacts/recommendation-model.json");

Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

string json = NetworkSnapshotSerializer.ToJson(network.GetSnapshot());
File.WriteAllText(filePath, json);

// Load the saved model
string savedJson = File.ReadAllText(filePath);
var snapshot = NetworkSnapshotSerializer.FromJson(savedJson);

var loadedNetwork = new RecommendationNeuralNetwork(new Random(123));
loadedNetwork.RestoreFromSnapshot(snapshot);

// Compare predictions
double[] sample = split.Testing[0].Features;
double original = network.Forward(sample);
double restored = loadedNetwork.Forward(sample);

if (Math.Abs(original - restored) > 1e-12)
    throw new Exception("The restored model gives a different prediction.");

double testLoss = trainer.Evaluate(split.Testing);

Console.WriteLine($"Epochs: {history.Epoch.Count}");
Console.WriteLine($"Best epoch: {history.BestEpoch}");
Console.WriteLine($"Validation loss: {history.BestValidationLoss:F6}");
Console.WriteLine($"Test loss: {testLoss:F6}");
Console.WriteLine($"Original prediction: {original:F6}");
Console.WriteLine($"Restored prediction: {restored:F6}");
Console.WriteLine($"Model saved to: {filePath}");
