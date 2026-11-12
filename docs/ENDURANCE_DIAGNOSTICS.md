# Endurance Diagnostic Attempts

These short/private attempts cannot qualify 72-hour endurance or stable
publication. Originals are retained under their immutable run identities.

| Attempt | Source | Result |
| --- | --- | --- |
| `map-endurance-smoke-20261112-a` | `a03ca8e913e34769acf24ffba6f5c2a5f1805ba7` | Failed before SDK launch: fixed `/usr/bin/dotnet` did not exist on the SDK VM. Zero captured packets; no traffic pass claimed. Controller exit 1; all four approved unit definitions/enablement/states restored, only originally active units started, fallback guard cancelled after verification. |
| `map-endurance-smoke-20261112-b` | `de507af4cd77d6ce1ceaefaae375d1701ea2ef1d` | SDK completed 360 seconds: 4,626,812 measured successes, 256 drain successes, 768 explicitly classified fault terminals, zero unexpected failures/unresolved requests. SDK exit 0, controller exit 1: restoration/export did not complete on its initial attempt. A separate idempotent finish retry verified the original unit states/enablement/hashes and guard cancellation; no failed receipt was changed into success. Baseline and outage PCAP passed standalone decoding, but crash/partition captures ended before the probe windows despite zero reported drops. Overall result remains rejected. |
| `map-endurance-smoke-20261112-c` | `3740f6c70595a216b2b280ea526acac835fe684a` | Independent end-to-end diagnostic verified: 360 seconds, 4,251,513 in-window successes (11,809.76 TPS), 768 declared fault terminals, zero unexpected failures/unresolved work. All three incidents recovered; original Kafka states/enablement/unit hashes restored; scoped rules absent; SDK/controller/supervision exited successfully. Diagnostic only, never 72-hour qualification. |

The correction resolves installed executable paths and checks the declared
.NET runtime and all local capture/network tools before requesting maintenance.
This is a controller setup correction, not evidence of a transport throughput
or protocol defect. Failed raw bytes and exit receipts are not overwritten.

Restore polling now observes transient service states without accepting them as
restored; the final match against original states remains exact. Pre-stop
snapshots still reject transitioning units. The controller retries only this
idempotent restoration step, never application transactions or an SDK run.

Standalone replay also matched the second attempt's 361 resource observations
and exact journal/terminal accounting. This does not rescue incomplete wire or
collection evidence. The controller now leaves capture active for three seconds
after probe acknowledgement, with ordinary admission still paused, before
stopping the capture backend. Raw packet evidence must still independently
contain all five fresh correlated probes; the delay itself is not a pass.

The [verified sanitized diagnostic summary](evidence/ENDURANCE_DIAGNOSTIC_20261112.json)
contains original raw-artifact digests, replayed resource bounds and independent
wire/maintenance results. Short duration and the diagnostic marker explicitly
keep `qualificationPassed` and `stablePromotion` false. The actual 72-hour run
must qualify independently using its own source, process and original artifacts.
