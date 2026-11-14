# Allocation Algorithm Performance & Scalability Report (Algorithm 2)

## Environment

- OS: Microsoft Windows 10.0.26200 (X64)
- Process architecture: X64
- .NET runtime: .NET 10.0.9
- Logical processor count: 12
- Build configuration: Release

**Measured timings are specific to this environment (hardware, OS, .NET runtime, build
configuration, and current machine load) and are not portable performance guarantees.
Re-running this benchmark on different hardware or under load may produce different
absolute numbers.**

## Configuration

- Algorithms compared: `MinCostMaxFlowPriorityAllocator` (existing optimal allocator) vs `GreedyPriorityAllocator` (existing baseline)
- Threshold: 0.50 (same for both algorithms, all scenarios)
- Measured repetitions per scenario/algorithm: 5 (plus 1 untimed warm-up run)
- Scenario sizes: Small (10 volunteers x 5 causes), Medium (25 x 10), Large (50 x 15), Extra Large (100 x 20)
- Candidate data: deterministic synthetic pairs with bounded edge density (not a full dense matrix), generated from fixed random seeds per scenario; capacities are also seeded and deterministic.
- Both algorithms receive identical candidates, equivalent (freshly cloned) capacities, and the same threshold for each scenario.

## Methodology

For each scenario, candidate pairs and cause capacities are generated once, outside any timed
section, using a fixed seed per scenario. For each algorithm, a single untimed warm-up call is
made first (to absorb JIT compilation and any first-call setup), followed by 5 measured
repetitions. Each measured repetition uses a fresh clone of the capacities (since allocators
may mutate remaining-places bookkeeping) but the same candidate list, and only the call to
`CalculatePriorities` is wrapped in a `System.Diagnostics.Stopwatch` - input generation, cloning,
and result processing are not included in the timed interval. Median, mean, and minimum
elapsed time (milliseconds) are reported across the 5 measured repetitions, along with the
candidate edge count and prioritized volunteer count for context.

**Important:** all candidate scores and capacities are constructed synthetic test fixtures, not
real volunteer/cause data or neural network predictions. This benchmark measures allocation
(Algorithm 2) computation time only; it does not measure neural network inference or training.

## Results

| Scenario | Candidate Edges | Algorithm | Median (ms) | Mean (ms) | Min (ms) | Prioritized Count |
|---|---|---|---|---|---|---|
| Small (10 volunteers x 5 causes) | 27 | Min-Cost Max-Flow | 0.1793 | 0.2782 | 0.1768 | 5 |
| Small (10 volunteers x 5 causes) | 27 | Greedy | 0.0312 | 0.0358 | 0.0255 | 4 |
| Medium (25 volunteers x 10 causes) | 106 | Min-Cost Max-Flow | 3.6136 | 6.0704 | 2.3346 | 22 |
| Medium (25 volunteers x 10 causes) | 106 | Greedy | 0.1209 | 0.1588 | 0.1161 | 17 |
| Large (50 volunteers x 15 causes) | 287 | Min-Cost Max-Flow | 5.0294 | 5.4563 | 4.7761 | 41 |
| Large (50 volunteers x 15 causes) | 287 | Greedy | 0.1774 | 0.1772 | 0.1721 | 35 |
| Extra Large (100 volunteers x 20 causes) | 685 | Min-Cost Max-Flow | 13.0887 | 13.2411 | 10.3807 | 77 |
| Extra Large (100 volunteers x 20 causes) | 685 | Greedy | 0.4043 | 0.4047 | 0.3914 | 72 |

## Interpretation

`GreedyPriorityAllocator` performs a simpler, more localized selection process, while
`MinCostMaxFlowPriorityAllocator` solves a min-cost max-flow problem via repeated shortest-path
augmentation, which does more work per candidate edge to guarantee maximum assignment
cardinality and optimal total compatibility for that cardinality. On the scenario sizes measured
here, this report only states the timings actually observed above; it does not claim that either
algorithm is universally faster across all hardware, input shapes, or problem sizes. Relative
performance may change for different candidate densities, volunteer/cause ratios, or runtime
environments.

## Limitations

- Only 4 scenario sizes and 5 measured repetitions per scenario/algorithm were evaluated; variance estimates are based on this small sample.
- Timings reflect this specific machine, OS, .NET runtime, and build configuration at the time of measurement; they are not portable guarantees.
- Candidate data is synthetic with bounded density, not real volunteer/cause data or neural network output.
- No JIT tiering/server GC configuration changes were made beyond process defaults; results may differ under other hosting configurations.

