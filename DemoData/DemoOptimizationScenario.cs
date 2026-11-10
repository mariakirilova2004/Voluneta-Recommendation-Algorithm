namespace Voluneta_Recommendation_Algorithm.DemoData
{
    // Selects which compatibility scores feed the Algorithm 2 comparison (Min-Cost Max-Flow vs
    // Greedy). Both scenarios run the exact same Core allocators - only the input scores and
    // default capacities differ.
    public enum DemoOptimizationScenario
    {
        // Real Algorithm 1 (neural network) predictions from DemoRecommendationService. Default,
        // existing behavior - unchanged.
        NeuralNetwork,

        // Explicitly constructed, hand-authored scores for the same 4 volunteers / 3 causes,
        // designed to make the Min-Cost Max-Flow vs Greedy trade-off easy to see and explain.
        // These are NOT neural network predictions.
        Educational
    }
}
