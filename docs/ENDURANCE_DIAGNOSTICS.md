# Endurance Diagnostic Attempts

These short/private attempts cannot qualify 72-hour endurance or stable
publication. Originals are retained under their immutable run identities.

| Attempt | Source | Result |
| --- | --- | --- |
| `map-endurance-smoke-20261112-a` | `a03ca8e913e34769acf24ffba6f5c2a5f1805ba7` | Failed before SDK launch: fixed `/usr/bin/dotnet` did not exist on the SDK VM. Zero captured packets; no traffic pass claimed. Controller exit 1; all four approved unit definitions/enablement/states restored, only originally active units started, fallback guard cancelled after verification. |

The correction resolves installed executable paths and checks the declared
.NET runtime and all local capture/network tools before requesting maintenance.
This is a controller setup correction, not evidence of a transport throughput
or protocol defect. Failed raw bytes and exit receipts are not overwritten.
