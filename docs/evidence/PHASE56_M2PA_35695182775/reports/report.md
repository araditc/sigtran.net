# Phase 56 Independent M2PA Interoperability Run

- Run ID: `phase56-m2pa-35695182775`
- Protocol: RFC 4165 M2PA over native Linux SCTP
- SDK implementation: Sigtran.NET `M2paLink`
- Peer implementation: independent C/lksctp reference peer
- SCTP PPID: 5
- Link-status stream: 0
- User-data stream: 1
- SDK exit: `0`
- Peer exit: `0`
- SDK validation: `true`
- Peer validation: `true`
- SCTP packets captured: `39`
- Result: **true**

## Exercised behavior

- Out-of-Service / Alignment / Proving / Ready handshake
- ordered SCTP metadata with PPID 5
- 24-bit M2PA BSN/FSN sequencing
- acknowledgement-only User Data
- two bidirectional User Data round trips
- Busy / Busy Ended signaling
- Processor Outage / Processor Recovered / Ready recovery handshake
- retrieval depth returning to zero
- PCAP, independent peer log, SDK trace, result summary, and SHA-256 retention
