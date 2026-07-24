# Phase 48 - M2PA Production Path

Phase 48 adds a stateful RFC 4165 M2PA link that can be consumed by MTP3 through
`IMtp2Link`.

## Delivered Units

| Unit | Delivery |
| --- | --- |
| 1 | RFC 4165 common header and M2PA sequence header codec |
| 2 | User Data and Link Status message models |
| 3 | SCTP PPID `5`, ordered delivery, and stream policy |
| 4 | 24-bit BSN/FSN arithmetic and acknowledgement handling |
| 5 | Bounded changeover retrieval buffer |
| 6 | Alignment, normal/emergency proving, and Ready lifecycle |
| 7 | Busy and Busy Ended send backpressure |
| 8 | Processor Outage and Processor Recovered handshake |
| 9 | Transport replacement, state events, metrics, and graceful stop |
| 10 | Codec, policy, lifecycle, traffic, and readiness tests plus documentation |

## Public API

- `M2paMessage`
- `M2paProtocol`
- `M2paLink`
- `M2paLinkOptions`
- `M2paRetrievalBuffer`
- `M2paLinkMetrics`
- `M2paReadiness`

All public APIs have XML documentation and use protocol-domain naming rather
than project-plan terminology.

## Dependency Direction

```text
MTP3 implementation
        |
    IMtp2Link
        |
     M2paLink
        |
   ISctpTransport
```

The upper MTP3 layer does not depend on a concrete SCTP socket or on
`M2paLink`. A different MTP2 provider can be substituted behind the same
contract.

## Verification

The phase gate requires:

```powershell
dotnet build src\Sigtran.NET.sln
dotnet run --project src\Sigtran.NET.Tests\Sigtran.NET.Tests.csproj
dotnet pack src\Sigtran.NET\Sigtran.NET.csproj -c Release
```

The in-process peer test verifies the complete alignment and User Data path,
including SCTP metadata and flow-control behavior. External peer evidence is
deliberately tracked by the later end-to-end traffic lab instead of being
inferred from unit tests.
