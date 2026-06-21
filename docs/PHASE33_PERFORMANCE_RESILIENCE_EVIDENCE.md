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

## Validation

Each unit in this phase is validated with:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```
