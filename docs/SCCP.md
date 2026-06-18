# SCCP

Phase 3 replaces the early SCCP proof-of-concept with standards-shaped connectionless SCCP primitives.

## Protocol Vocabulary

`SccpMessageType` exposes the connectionless message type codes used by UDT, XUDT, LUDT, and their service-message forms.

```csharp
byte udt = (byte)SccpMessageType.Unitdata;
byte xudt = (byte)SccpMessageType.ExtendedUnitdata;
```

`SccpProtocolClass` models the protocol class octet and the return-message-on-error option.

```csharp
SccpProtocolClass protocolClass = new(
    SccpConnectionlessClass.Class1,
    returnMessageOnError: true);

byte encoded = protocolClass.Encode();
```

The current SCCP work is still a foundation for standards-based encode/decode. Interoperability-sensitive consumers should wait for the Phase 3 readiness report before treating SCCP APIs as stable.
