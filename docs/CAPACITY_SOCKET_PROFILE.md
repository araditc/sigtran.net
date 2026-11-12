# Capacity Socket Profiles

The first approved dedicated-window rerun is retained as
[NOT QUALIFIED](CAPACITY_MEASUREMENT_DEDICATED_ATTEMPT_20261112.md):
26,575,568 completed transactions, two low seconds (9,944 at offset 320 and
781 at offset 949), zero terminal failures and a 1,047.529 ms maximum latency.
Kafka was temporarily stopped with explicit approval and restored, with unit
states, enablement and definition hashes verified. Dedicated hosts alone did
not eliminate the stalls. The successful earlier shared run remains unchanged.

## Observations, Not Assumptions

The severe second is bracketed by 16.286 ms cumulative GC pause and no Gen2
collection, about 44.2% normalized SDK CPU, one namespace T3 retransmission
and 45 packet discards. Peer samples show a fast retransmission, reduced CPU
and later duplicate DATA discards. The mild second is bracketed by 48.5 ms GC
pause and no new T3 retransmission. Samples span adjacent seconds; namespace
counters do not identify a particular lost packet or prove sole causation.
The bounded prefix PCAP does not cover these later events. The measurements
are consistent with a transport recovery stall, not proof of a particular
hypervisor, NIC or SDK defect.

## Explicit Per-Socket Experiment

The default remains `--socket-profile system-default`. The new opt-in
`--socket-profile lan-bounded-v1` uses the existing `INativeSctpSocketFactory`
injection contract in the workload application, not new SDK policy:

- Request 1 MiB send and receive buffers on each owned SDK and peer socket.
- Set SCTP RTO initial/minimum/maximum to 300/200/1,000 ms on those sockets.
- Read back both endpoints' effective kernel values after connection.
- Retain observed buffer values, since Linux may clamp requests to host limits.
- Reject mismatched profile names, missing observations or ineffective RTO bounds.

[RFC 6458 section 8.1.1](https://www.rfc-editor.org/rfc/rfc6458.html#section-8.1.1)
defines the millisecond RTO socket parameters. Shorter-than-standard default
recovery bounds are an explicitly declared low-latency private-LAN experiment,
not a public-Internet recommendation or an SDK production default. Delayed
paths may cause spurious retransmissions; requalify any different network.
No sysctl, firewall, priority of unrelated services or Kafka configuration is
changed. SDK public APIs and application worker defaults are unchanged.

This profile does not alter the workload, completion accounting, five-operation
mix, 10,000-TPS threshold or 1,800-second criterion. The subsequent full
[dedicated run](CAPACITY_MEASUREMENT_DEDICATED_20261112.md) now qualifies:
minimum 10,079 TPS, 26,599,474 completions and zero low seconds/errors. Both
kernels observed 425,984-byte send/receive buffers and the exact RTO bounds.
The 60-second smoke alone did not establish this result.

The prefix PCAP contains an identical startup retransmission after about
201.7 ms, outside the measured interval. Short RTO bounds may interact with
delayed acknowledgements and cause spurious retransmissions; do not infer that
all retransmissions disappeared. Extraction preserves duplicate bytes for
strict independent TSN/metadata comparison. The [exit report](CAPACITY_QUALIFICATION_EXIT.md)
records the exact source/profile and its narrow minimum margin. Neither this
pass nor the settings establish a general SLA, resilience, operator acceptance
or stable-release authorization.
