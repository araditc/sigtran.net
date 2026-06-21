# Phase 33 - Performance And Resilience Evidence

Phase 33 turns the performance foundation into release-grade evidence contracts. It covers real peer traffic benchmarks, warmup/sustained/peak stages, resilience and failover evidence, latency P95/P99, CPU and memory metrics, and a publishable report model.

The phase does not manufacture benchmark evidence. Production performance claims remain blocked until retained peer-traffic artifacts are captured from a real environment and reviewed.

## Unit 1 - Benchmark Evidence Workload

`SigtranPerformanceEvidenceWorkload` now models benchmark evidence as measured stages rather than a single smoke result. It provides:

- Warmup, sustained, and peak stage kinds.
- Target and actual messages per second.
- Sent and received message counters.
- Message-loss detection.
- Stage-level pass/fail checks.
- Commercial peer-traffic workload coverage checks.

`SigtranPerformanceEvidenceWorkloads.CreateExpectedCommercialPeerTraffic()` maps the existing commercial load-test plan into the expected evidence workload. This gives later units a deterministic contract for real peer benchmark results.

## Unit 2 - Peer-Traffic Artifacts And Run Plan

`SigtranPerformanceEvidenceArtifactManifest` and `SigtranPerformanceEvidenceRunPlan` now define the retained artifact contract for a commercial peer benchmark. Required artifacts include:

- Packet capture.
- SDK trace.
- Peer log.
- Peer configuration.
- Structured metrics.
- Latency profile.
- Resource profile.
- Resilience log.
- Publishable benchmark report.

The manifest requires SHA-256 digest coverage for all retained artifacts before it can support commercial performance evidence. The run plan combines the peer-traffic workload and retained artifact manifest without naming any specific peer package.

## Unit 3 - Latency Percentile Evidence

`SigtranPerformanceLatencyEvidence` now captures measured latency percentiles for each SDK latency surface:

- Sample count.
- P50.
- P95.
- P99.
- Maximum latency.

`SigtranPerformanceLatencyEvidenceEvaluator` compares measured P95/P99 values against `SigtranLatencyBudgets`. The evaluator can report per-surface pass/fail results and verify that retained latency evidence covers every configured budget.

## Unit 4 - CPU Memory And Allocation Evidence

`SigtranPerformanceResourceEvidence` now captures runtime resource measurements for peer benchmark runs:

- Average CPU percentage.
- Peak CPU percentage.
- Peak working set in megabytes.
- Allocated bytes per message.
- Generation 2 garbage collection count.

`SigtranPerformanceResourceEvidenceEvaluator` compares the evidence against `SigtranResourceBudgets.CreateCommercialDefault()`. The report separates CPU, working set, and allocation budget checks so performance evidence can identify the specific resource area that needs correction.

## Unit 5 - Resilience And Failover Evidence

`SigtranPerformanceResilienceEvidence` now models failover evidence as an ordered event timeline:

- Failure detected.
- Recovery started.
- Failover completed.
- Traffic restored.

The evidence records recovery duration and lost messages, then gates commercial resilience claims on required event coverage, recovery within budget, and zero message loss. This gives peer benchmark reports a deterministic way to prove fault recovery behavior instead of only reporting throughput under healthy conditions.

## Unit 6 - Publishable Performance Report

`SigtranPerformanceEvidenceReport` now aggregates:

- Peer-traffic workload and retained artifacts.
- Latency P95/P99 budget reports.
- CPU, memory, and allocation budget report.
- Resilience and failover evidence.

The report exposes a single `Publishable` gate and can render a Markdown benchmark summary suitable for release evidence review. The report only becomes publishable when workload/artifacts, latency, resource, and resilience gates all pass.

## Unit 7 - Production Performance Evidence Gate

`SigtranPerformanceEvidenceGate` now connects publishable benchmark reports to production performance claims. The gate requires:

- Performance foundation readiness.
- A publishable performance evidence report.
- Wider commercial readiness.

This keeps retained benchmark evidence separate from production claims. A report can be complete and publishable while production performance still remains blocked by `commercial-readiness-required`.

## Unit 8 - Runner And CI Handoff

`SigtranPerformanceEvidenceRunnerPlan` now defines the command sequence for real peer benchmark execution:

- Prepare environment.
- Start capture.
- Run warmup.
- Run sustained.
- Run peak.
- Trigger failover.
- Collect metrics.
- Render report.

`SigtranPerformanceEvidenceCiHandoff` keeps the benchmark workflow manual and self-hosted, with artifact upload patterns under the retained artifact root. This prevents accidental CI publication of performance claims while still making the execution handoff repeatable.

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
