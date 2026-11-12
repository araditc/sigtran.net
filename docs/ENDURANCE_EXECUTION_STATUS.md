# Continuous Endurance Execution Status

Phase 62 unit 10 is **in progress, not qualified**. The verified six-minute
[diagnostic](ENDURANCE_DIAGNOSTICS.md) closes only unit 9. The full attempt
must use one frozen committed source, one SDK process and 259,200 actual
measured seconds. No timer acceleration, concatenation or process restart
can substitute for this interval.

The full attempt will run on the same authorized SDK and peer VMs under
`completed-map-endurance-v1`, using the explicit LAN socket profile, .NET
10.0.12/server GC, four maximum workers, concurrency 256, queue capacity 8192
and the independent normal-priority CPU-3 C peer. Nine scheduled incidents
span peer crash, held outage and scoped SCTP partition. Owner approval covers
temporary Kafka/health-timer stop throughout this test with verified restoration.

Before launch, full build/test/API/pack must pass and source must be clean and
committed. The host-local systemd job owns execution and cleanup; a separate
supervisor records actual exits and its own verified-cleanup receipt before
hashing/verifying original artifacts. The raw root stays private. Only a
verified sanitized summary can be retrieved after successful stopped supervision.
Both VMs and their hypervisor must remain running without suspend/reboot for
the entire interval. Desktop/SSH disconnection does not stop host-local jobs;
VM power loss or an SDK restart cannot produce a qualifying continuation.

A local freeze-verification attempt hit workstation disk pressure; its failed
logs remain retained. After owner-authorized cleanup of stale tool downloads
and generated build caches, the complete `execution-freeze-retry` verification
passed build/tests/API/pack. Sources, credentials and lab evidence were not
removed. This preparation does not itself qualify the 72-hour interval.

The final report will record actual source/start/finish, duration, completed
work/TPS, classified fault terminals, zero unexpected/unresolved work, resource
replay, all nine recovery proofs, actual packet-drop/removal and original-unit
restoration. Until then, unit 10 and the 72-hour gate remain open. Stable release
is not authorized by this source/profile test; phases 63-65 remain separate.
