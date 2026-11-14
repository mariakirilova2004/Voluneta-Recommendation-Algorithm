# Final Technical Validation Report 


## 1. Build Result

- `dotnet build Voluneta-Optimization.slnx -c Release` — **Build succeeded** (0 errors, 0 warnings reported in output) for all 4 projects:
  - `Voluneta.Recommendation.Core`
  - `Voluneta.Recommendation.ModelTraining`
  - `Voluneta-Optimization` (Razor/MVC web app)
  - `Voluneta.Recommendation.Tests`

## 2. Test Count

- **320 tests total, 320 passed, 0 failed, 0 skipped** (xUnit, Release build).
- 312 pre-existing tests + 8 new tests added in this pass (see "Confirmed Issues and Fixes").
- Run twice during this validation (before and after adding the new test file); both runs green.

## 3. Algorithm 1 (Neural Network Recommendation) — Verified

| Check | Result | Evidence |
|---|---|---|
| Model loading from JSON | OK | `DemoModelProvider` loads `ModelArtifacts/recommendation-model.json` at startup (`Program.cs`); startup validation logs a sample prediction in `[0, 1]`. |
| Feature extraction | OK | `RecommendationFeatureCalculatorTests`, `RecommendationFeatureTests` pass; features flow through `DemoRecommendationService`. |
| Deterministic predictions | OK | `RecommendationNeuralNetworkTests`, `NetworkSnapshotSerializerTests` confirm restored model reproduces identical forward-pass output; CLI load/save/compare check in `Program.cs` (default run) asserts prediction match within `1e-12`. |
| Profile editing without mutating `DemoDataSet` | OK | `DemoRecommendationServiceTests.GenerateRankedRecommendations_WithVolunteerOverride_DoesNotMutateDemoDataSet` passes; additionally verified at controller level by new test `Optimization_Post_DoesNotMutateDemoDataSet`. |
| Before vs After comparison | OK | `DemoControllerComparisonTests` verifies matching strictly by `CauseId`, correct score/rank diffs, and that an initial GET / reset hides the comparison panel. |

## 4. Algorithm 2 (Priority Allocation) — Verified

| Check | Result | Evidence |
|---|---|---|
| Capacity constraints | OK | Both allocators reject negative remaining places and unknown/duplicate cause IDs (`GreedyPriorityAllocatorTests`, `MinCostMaxFlowPriorityAllocatorTests`); allocation never exceeds `RemainingPlaces` (`AllocationOptimalityTests`). |
| One priority per volunteer | OK | Covered by allocator tests and `AllocationOptimalityTests`; also checked structurally in `AllocationPerformanceBenchmarkRunnerTests` (`prioritized <= totalCapacity` and `<= volunteerCount`). |
| Minimum score threshold | OK | Both allocators validate `minimumScore` is finite and in `[0, 1]`, throwing `ArgumentOutOfRangeException` otherwise; filtering logic confirmed in allocator tests. |
| Greedy vs Min-Cost Max-Flow comparison | OK | `AllocationComparisonExperimentTests`, `AllocationOptimalityTests`, and `AllocationEvaluationRunnerTests` confirm Min-Cost Max-Flow total score is never lower than Greedy's for the same inputs. |
| Educational scenario expected results | OK | `DemoEducationalScenario` / `AllocationEvaluationRunner` educational case produces the documented Greedy-vs-Optimal divergence; exercised by `AllocationEvaluationRunnerTests` and `DemoAllocationComparisonServiceTests`. |

## 5. MVC Demo — Verified

| Check | Result | Evidence |
|---|---|---|
| `/Demo/Recommendations` | OK | `DemoController.Recommendations` (GET/POST) and `CalculateWithEditedProfile` exercised by `DemoControllerComparisonTests`. |
| `/Demo/Optimization` | OK | `DemoController.Optimization` (GET/POST) and `ResetOptimization` exercised by new `DemoControllerOptimizationTests`. |
| Dependency injection and routing | OK | `Program.cs` registers `DemoModelProvider`, `DemoRecommendationService`, `DemoAllocationComparisonService` as singletons; default route `{controller=Home}/{action=Index}/{id?}` confirmed in `Program.cs`; controller constructor-injects both demo services (verified by tests instantiating `DemoController` directly with the same services). |
| Invalid form inputs handled safely | OK (gap closed) | Negative cause capacity and out-of-range/NaN `minimumScore` return validation errors via the `Errors` list and `Result == null`, without throwing. Previously only covered at the service layer (`DemoAllocationComparisonServiceTests`); now also covered at the controller layer (`DemoControllerOptimizationTests`). Unknown cause IDs in posted capacity are silently ignored (by design) and do not throw. |
| Reset behavior | OK | `ResetOptimization` redirects to `Optimization` with the same scenario, restoring default capacities/threshold (`DemoControllerOptimizationTests.ResetOptimization_RedirectsToOptimization_WithSameScenario`); `Recommendations` reset path verified not to retain a stale comparison (`DemoControllerComparisonTests.Recommendations_Reset_HidesComparison`). |

## 6. Evaluation CLI Commands — Verified by Execution

All four commands were executed directly against the `Voluneta.Recommendation.ModelTraining` project in Release mode during this validation pass:

| Command | Result |
|---|---|
| `evaluate ExperimentResults` | Succeeded. Evaluated 15 runs across 3 models (Mean Rating Baseline, Linear Score Baseline, Neural Network). Wrote `neural_evaluation_runs.csv`, `neural_evaluation_summary.csv`, `neural_evaluation_report.md`. |
| `allocation-evaluate ExperimentResults` | Succeeded. Evaluated 4 scenarios × 3 thresholds (12 scenario/threshold combinations, 24 rows). Wrote `allocation_evaluation.csv`, `allocation_evaluation_report.md`. |
| `allocation-benchmark ExperimentResults` | Succeeded. Benchmarked 4 scenarios × 2 algorithms (8 rows), 5 measured repetitions each. Wrote `allocation_performance.csv`, `allocation_performance_report.md`. |
| `predict [modelPath]` | Succeeded when given an explicit model path (see Limitations below). Produced ranked recommendations for the two sample volunteer/cause pairs. |

## 7. `recommendation-model.json` Integrity — Confirmed Not Overwritten

- SHA-256 hash and `LastWriteTimeUtc` of `ModelArtifacts/recommendation-model.json` (the model used by the live web app) were captured **before** and **after** running `evaluate`, `allocation-evaluate`, `allocation-benchmark`, and `predict`.
- Hash: `71C4BF2C1254750A6E82EE20EC02468C65A13F1B0D3A576C543E997074AE53D2` — **identical** before and after.
- `LastWriteTimeUtc` — **unchanged**.
- Confirms none of the four evaluation/inference commands write to or mutate the deployed model file; only the default (no-argument) training run in `Program.cs` creates/overwrites a model file, and only at the path explicitly passed as `args[0]` (defaulting to `ModelArtifacts/recommendation-model.json` relative to the *current working directory*, not the deployed web app's copy unless explicitly pointed there).

## 8. Reproducibility / Validation Issues Found

1. **Confirmed gap (fixed): missing controller-level tests for invalid `/Demo/Optimization` form input.**
   Existing coverage validated input rules only at the `DemoAllocationComparisonService` layer. Added `Voluneta.Recommendation.Tests/Controllers/DemoControllerOptimizationTests.cs` (8 tests) to directly exercise `DemoController.Optimization` GET/POST, negative capacity, out-of-range/NaN threshold, unknown cause IDs, `ResetOptimization`, and non-mutation of `DemoDataSet`. All 8 pass.

2. **Non-defect, documented limitation: `predict`/default training commands use working-directory-relative default paths.**
   Running `dotnet run -- predict` from the `Voluneta.Recommendation.ModelTraining` directory without an explicit path fails, because the command's default (`ModelArtifacts/recommendation-model.json`) is relative to the current working directory, and the real trained model lives under the web app's `ModelArtifacts/` folder at the repository root. This is expected/by-design (mirrors the documented `[outputDir]`/`[modelPath]` argument pattern) rather than a defect — passing the correct relative/absolute path resolves it, as verified above. No code change was made for this since it does not violate any stated requirement and altering default path resolution risks unintended behavior changes outside this audit's scope.

3. No other defects were found in Algorithm 1, Algorithm 2, the MVC demo, or the CLI commands during this audit.

## 9. Final Technical Readiness Assessment

- **Build:** Release build succeeds cleanly across all projects.
- **Tests:** 320/320 passing, including newly added focused controller tests; no timing-based assertions are used anywhere in the test suite (performance benchmark tests assert only non-negative/well-formed durations, never fixed thresholds).
- **Algorithms:** Both Algorithm 1 (neural recommendation) and Algorithm 2 (priority allocation, Greedy vs Min-Cost Max-Flow) behave correctly and deterministically against their existing, unmodified implementations.
- **Demo (MVC/Razor-hosted) UI wiring:** Both demo pages are correctly wired with DI, routing, safe invalid-input handling, and reset behavior.
- **CLI tooling:** All four evaluation/inference commands run successfully and never mutate the deployed trained model.
- **Overall:** The implementation is technically ready for its stated educational/demo scope. No architecture, model weights, or algorithm objectives were changed as part of this validation.

## Changed Files (this validation pass)

- Added: `Voluneta.Recommendation.Tests/Controllers/DemoControllerOptimizationTests.cs` (8 new focused tests).
- Added: `ExperimentResults/final_validation_report.md` (this report).
- Generated/refreshed (not hand-edited): `ExperimentResults/neural_evaluation_*.{csv,md}`, `ExperimentResults/allocation_evaluation*.{csv,md}`, `ExperimentResults/allocation_performance*.{csv,md}` via CLI command execution.
- No existing production code, model architecture, trained weights, or algorithm objectives were modified.
