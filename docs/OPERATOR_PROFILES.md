# Network And Operator Profiles

SIGTRAN.NET network profiles describe signalling policy that must be validated before a runtime is composed. A profile is configuration; it is not interoperability evidence and it is not an operator certification.

The first profile slice intentionally follows the implemented stable 1.0 protocol surface. It accepts only ITU-style 14-bit point codes, a two-bit MTP3 network indicator, the existing connectionless SCCP global-title translation rules, and the five MAP SMS operations already implemented by the SDK. Unsupported variants fail closed.

## Synthetic Example

```csharp
using Sigtran.NET.Core.Profiles;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;

SigtranNetworkProfileDefinition definition = new(
    name: "synthetic-lab",
    pointCodeFormat: SigtranPointCodeFormat.Itu14Bit,
    networkIndicator: 2,
    globalTitleTranslations:
    [
        new SccpGlobalTitleTranslationRule(
            name: "map-gateway",
            prefix: "44123",
            destinationPointCode: 1234,
            subsystemNumber: SubsystemNumber.MAP)
    ],
    allowedMapSmsOperations:
    [
        MapSmsOperationCode.SendRoutingInfoForShortMessage,
        MapSmsOperationCode.MtForwardShortMessage
    ]);

SigtranNetworkProfileValidationResult validation =
    SigtranNetworkProfileValidator.Validate(definition);

if (!validation.IsValid)
{
    foreach (SigtranNetworkProfileValidationIssue issue in validation.Issues)
    {
        Console.Error.WriteLine($"{issue.Code}: {issue.Message}");
    }

    return;
}

SigtranNetworkProfile profile = SigtranNetworkProfile.Create(definition);
SccpGlobalTitleTranslationTable translations =
    profile.CreateGlobalTitleTranslationTable();

MapSmsOperationProfile sriSm = profile.GetMapSmsOperationProfile(
    MapSmsOperationCode.SendRoutingInfoForShortMessage);
```

The profile reuses `SccpGlobalTitleTranslationRule`, `SccpGlobalTitleTranslationTable`, and `MapSmsOperationProfiles`; it does not introduce a competing SCCP or MAP policy model. Application-context and timeout values therefore remain sourced from the existing MAP SMS operation profiles.

## Fail-Closed Rules

Validation currently rejects:

- blank profile names;
- point-code formats other than `Itu14Bit`;
- network indicator values outside `0..3`;
- compatibility mode until a concrete deviation has its own reviewed implementation;
- duplicate SCCP translation rule names;
- conflicting rules for the same global-title prefix;
- an empty MAP SMS allowlist;
- duplicate or unknown MAP SMS operation codes.

Validation issues use stable codes and deterministic ordering so configuration tooling can surface the same failure set without opening SCTP associations or contacting a peer.

## Evidence Boundary

A locally valid profile proves only that the SDK can represent the selected settings. Closing the stable `operator-profile` gate additionally requires retained traffic against an authorized operator or vendor profile, including sanitized configuration, PCAP/trace comparison, peer observations, deviation classification, and digest coverage. Repository simulations or synthetic examples must never be relabeled as operator acceptance.
