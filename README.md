# 🧠 Voluneta Recommendation Algorithm

### Senior Thesis — Intelligent Corporate Volunteering Platform with Recommendation and Allocation Algorithms

This repository contains the Senior Thesis implementation for **Voluneta**, a corporate volunteering recommendation and allocation system. It implements two algorithms **from scratch in C#**, with no external machine learning frameworks:

1. **Algorithm 1 — Neural Recommendation Model**: a custom feed-forward neural network that scores volunteer-cause compatibility.
2. **Algorithm 2 — Priority Allocation**: a capacity-constrained assignment algorithm (Min-Cost Max-Flow) compared against a Greedy baseline.

The project includes a working ASP.NET Core MVC demo, a console-based model training/evaluation tool, and an xUnit test suite (320 tests, all passing as of the last full validation pass — see [`ExperimentResults/final_validation_report.md`](ExperimentResults/final_validation_report.md)).

---

## 🎯 Goal

Recommend volunteering causes to employees based on their profile (interests, skills, availability, experience, location, etc.), then allocate a limited number of priority spots per cause fairly and optimally across all interested volunteers.

---

## 🔬 Algorithm 1 — Custom Neural Network (8-16-8-1)

A fully custom, from-scratch feed-forward neural network (no ML.NET, TensorFlow, or PyTorch):

```text
Volunteer + Cause
	   │
	   ▼
  8 Input Features
	   │
	   ▼
  Dense Layer (16, ReLU)
	   │
	   ▼
  Dense Layer (8, ReLU)
	   │
	   ▼
  Output Layer (1, Sigmoid)
	   │
	   ▼
Compatibility Score [0, 1]
```

**Input features** (`RecommendationFeatureCalculator`):

| # | Feature |
|---|---|
| 1 | Interest Match |
| 2 | Category History Match |
| 3 | Day Availability Match |
| 4 | Time Availability Match |
| 5 | Location Match |
| 6 | Skill Match |
| 7 | Experience Match |
| 8 | Colleague Match |

Implemented components: dense layers, ReLU/Sigmoid activations, mean-squared-error loss, backpropagation, SGD, early stopping, JSON model snapshot serialization (`ModelArtifacts/recommendation-model.json`), and training on a deterministic synthetic dataset (`SyntheticDatasetGenerator`).

**Baselines used for comparison:**
- **Mean Rating Baseline** — predicts the historical mean rating.
- **Linear Score Baseline** — a simple weighted linear combination of the same 8 features.

## 🔬 Algorithm 2 — Priority Allocation (Min-Cost Max-Flow vs Greedy)

Assigns a limited number of priority recommendations per cause, respecting each cause's remaining capacity and a minimum compatibility-score threshold:

```text
Recommendation Scores (Algorithm 1)
		│
		▼
 Volunteer-Cause Candidate Graph
		│
		▼
 Capacity Constraints + Minimum Score Threshold
		│
		▼
 Min-Cost Max-Flow (optimal)   vs   Greedy (baseline)
```

- **`MinCostMaxFlowPriorityAllocator`** — builds a flow network and repeatedly augments along shortest (Bellman-Ford) paths to find the maximum-cardinality, minimum-cost (i.e. maximum total compatibility) assignment, with at most one priority per volunteer.
- **`GreedyPriorityAllocator`** — sorts candidate pairs by score and greedily assigns the best-scoring pairs first until capacities run out. Simpler, but not guaranteed optimal.

Both allocators share the same `RecommendationCandidate` / `CauseCapacity` inputs and the same minimum-score threshold, so their results are directly comparable.

---

## 🏗️ Project Structure

```text
Voluneta-Recommendation-Algorithm/
├── Voluneta-Optimization.slnx                     # Solution file
├── Voluneta-Optimization.csproj                   # ASP.NET Core MVC demo (web app)
│   ├── Controllers/                               # HomeController, DemoController
│   ├── DemoData/                                  # Demo volunteers/causes, demo services (never mutated)
│   ├── Models/                                     # View models (Recommendations, Optimization)
│   ├── Views/Demo/                                 # Recommendations.cshtml, Optimization.cshtml
│   ├── ModelArtifacts/recommendation-model.json    # Trained model used by the live demo
│   └── ExperimentResults/                          # Evaluation/report artifacts (see below)
│
├── Voluneta.Recommendation.Core/                  # Core class library (no web/UI dependencies)
│   ├── Algorithms/Recommendation/                 # Algorithm 1: NeuralNetwork, Training, Evaluation, Baselines, Features, Reporting
│   └── Algorithms/Allocation/                      # Algorithm 2: Optimization (Min-Cost Max-Flow), Baselines (Greedy), Graph, PathFinding, Models, Reporting
│
├── Voluneta.Recommendation.ModelTraining/         # Console app: training + evaluation/benchmark CLI commands
│   └── Program.cs                                  # evaluate / allocation-evaluate / allocation-benchmark / predict / (default) train
│
└── Voluneta.Recommendation.Tests/                 # xUnit test suite (320 tests)
	├── Recommendation/                             # Algorithm 1 tests (NeuralNetwork, Training, Evaluation, Baselines, Features)
	├── Allocation/                                  # Algorithm 2 tests (allocators, graph, path finding, reporting)
	├── Controllers/                                 # DemoController tests (recommendations + optimization)
	└── DemoData/                                    # Demo service tests
```

---

## ⚙️ Technology & Requirements

`C#` · `.NET 10` · `ASP.NET Core MVC` · `xUnit`

- **.NET 10 SDK** is required to build and run this solution (`dotnet --version` should report a `10.x` SDK).
- The neural network and both allocation algorithms are implemented entirely from scratch — no ML.NET, TensorFlow, PyTorch, or other ML/optimization framework dependency.
- No database is used; all demo data is in-memory (see Limitations).

---

## 🛠️ Build & Test

From the repository root:

```powershell
# Build the whole solution in Release mode
dotnet build "Voluneta-Optimization.slnx" -c Release

# Run the full test suite (320 tests)
dotnet test "Voluneta-Optimization.slnx" -c Release
```

---

## 🚀 Launching the MVC Demo

From the repository root:

```powershell
cd Voluneta-Optimization
dotnet run
```

By default this launches at `http://localhost:5169` (and `https://localhost:7139` with the `https` profile; see `Properties/launchSettings.json`). Then open:

- **`/Demo/Recommendations`** — Algorithm 1 demo: pick a volunteer, see ranked causes with compatibility scores, edit the volunteer's profile, and compare original vs. edited recommendations.
- **`/Demo/Optimization`** — Algorithm 2 demo: choose a score source (real neural network predictions or the constructed educational example), edit cause capacities and the minimum match threshold, and compare Min-Cost Max-Flow vs. Greedy side by side.

---

## 🔁 Reproducing the Evaluations

All evaluation/benchmark commands are run from the `Voluneta.Recommendation.ModelTraining` project and write their artifacts to an `ExperimentResults` folder **relative to the current working directory** (pass an explicit output directory to control where they land):

```powershell
cd Voluneta.Recommendation.ModelTraining

# 1. Algorithm 1 evaluation: Mean Rating Baseline vs Linear Score Baseline vs Neural Network
dotnet run -c Release -- evaluate ../ExperimentResults

# 2. Algorithm 2 evaluation: Min-Cost Max-Flow vs Greedy across scenarios/thresholds
dotnet run -c Release -- allocation-evaluate ../ExperimentResults

# 3. Algorithm 2 performance/scalability benchmark (Small/Medium/Large/Extra Large scenarios)
dotnet run -c Release -- allocation-benchmark ../ExperimentResults
```

Omitting the output directory argument writes to `./ExperimentResults` relative to wherever `dotnet run` is invoked from (e.g. straight into `Voluneta.Recommendation.ModelTraining/ExperimentResults` if run from that project folder). **None of these three commands read, modify, or overwrite `ModelArtifacts/recommendation-model.json`.**

### Running a prediction with an explicit model path

The `predict` command loads a trained model and prints sample ranked recommendations. Because its default path is relative to the current working directory, pass the path to the deployed model explicitly when running from `Voluneta.Recommendation.ModelTraining`:

```powershell
cd Voluneta.Recommendation.ModelTraining
dotnet run -c Release -- predict "../ModelArtifacts/recommendation-model.json"
```

---

## 📊 Where to Find Generated Experiment Results

Two `ExperimentResults` folders exist in the repository, depending on where a command was run from. Nothing has been moved or duplicated on purpose — this reflects the two locations the CLI naturally writes to.

**Repository root — `ExperimentResults/`** (checked-in reference artifacts):
- [`neural_evaluation_runs.csv`](ExperimentResults/neural_evaluation_runs.csv) / [`neural_evaluation_summary.csv`](ExperimentResults/neural_evaluation_summary.csv) / [`neural_evaluation_report.md`](ExperimentResults/neural_evaluation_report.md) — Algorithm 1 evaluation (`evaluate`).
- [`allocation_evaluation.csv`](ExperimentResults/allocation_evaluation.csv) / [`allocation_evaluation_report.md`](ExperimentResults/allocation_evaluation_report.md) — Algorithm 2 evaluation (`allocation-evaluate`).
- [`final_validation_report.md`](ExperimentResults/final_validation_report.md) — Step 14.4 final technical validation report (build, tests, CLI verification, model-file integrity check).

**`Voluneta.Recommendation.ModelTraining/ExperimentResults/`** (produced when commands are run from that project folder):
- `neural_evaluation_runs.csv`, `neural_evaluation_summary.csv`, `neural_evaluation_report.md`
- `allocation_evaluation.csv`, `allocation_evaluation_report.md`
- `allocation_performance.csv`, `allocation_performance_report.md` — Algorithm 2 performance/scalability benchmark (`allocation-benchmark`).

Re-running any of the three evaluation commands regenerates the CSV/Markdown files for that command in whichever output directory you point it at.

---

## 🖱️ Demo Walkthrough

A suggested path through the live MVC demo:

1. **Select a volunteer and view neural recommendations** — open `/Demo/Recommendations`, pick a volunteer from the dropdown, and view their ranked causes with Algorithm 1 compatibility scores.
2. **Edit the profile and compare original vs. edited scores** — adjust interests, skills, available days, time of day, location, or experience, then recalculate; the page shows a side-by-side comparison of original vs. edited scores and ranks, matched by cause (not by position).
3. **Run graph optimization with neural scores** — open `/Demo/Optimization`, keep the "Neural Network Predictions" scenario selected, optionally adjust cause capacities and the minimum match threshold, and submit to see which volunteer-cause pairs become priorities.
4. **Switch between Min-Cost Max-Flow and Greedy** — on the same results page, compare the two algorithm panels side by side: differing picks are highlighted, along with each algorithm's total compatibility score and prioritized volunteer count.
5. **Demonstrate the educational Greedy vs Optimal case** — switch the scenario selector to "Greedy vs Optimal (Educational Example)" to see a small, hand-constructed scenario (not neural network output) specifically designed to make the cases where Greedy is suboptimal and Min-Cost Max-Flow finds the better total assignment clearly visible.

Use **Reset** on either page at any time to restore the original profile / default capacities and threshold without reloading the app.

---

## ⚠️ Limitations

- **Neural network evaluation uses synthetic training targets.** `SyntheticDatasetGenerator` / `SyntheticTargetGenerator` produce deterministic, seeded synthetic ratings for training and evaluation — not real historical volunteer feedback.
- **The educational allocation scenario's scores are explicitly constructed**, not neural network predictions. They exist solely to make the Greedy-vs-Optimal trade-off visible and are clearly labeled as such in the UI and reports.
- **Priority allocations are recommendations only, not actual registrations.** Neither allocator reserves real places or writes to any persistent store; "remaining places" are in-memory demo values.
- **Performance/benchmark timings are environment-specific.** Figures in `allocation_performance*.{csv,md}` depend on the machine, OS, .NET runtime, and current load at measurement time, and are not portable performance guarantees (see the benchmark report's own "Limitations" section).
- **No production database integration.** The demo uses in-memory `DemoDataSet` volunteers/causes only; there is no Azure SQL (or other) database wired up in this project despite earlier technology notes.

---

## 👩‍💻 Author

**Maria Kirilova**
