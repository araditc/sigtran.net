# Protocol Wire Conformance

This is Phase 57 of the [approved continuation](RELEASE_QUALIFICATION_ROADMAP.md),
not the earlier roadmap-execution checkpoint. It covers the declared ITU
connectionless SCCP, TCAP and five-operation MAP SMS profile. Offline byte
checks do not establish operator or release-candidate acceptance.

| Unit | Scope | State |
| --- | --- | --- |
| 1 | Q.773 transaction tag classes and opaque transaction IDs | Implemented |
| 2 | Transaction field validation and abort encoding | Implemented |
| 3 | Bounded BER and OID decoding | Implemented |
| 4 | Invoke and linked ID | Implemented |
| 5 | Result, error and reject | Implemented |
| 6 | Structured dialogue APDUs | Implemented |
| 7 | Dialogue negotiation at runtime | Implemented |
| 8 | Connectionless SCCP wire checks | Implemented |
| 9 | MAP SMS ASN.1 profile and vectors | Implemented |
| 10 | Integrated vectors, migration and exit report | Pending |

## Transaction Tags

[ITU-T Q.773 TCAPMessages](https://www.itu.int/ITU-T/formal-language/itu-t/q/q773/1997/TCAPMessages.html)
assigns application-class tags `48`/`49` to the opaque transaction IDs and
`6B`/`6C` to the constructed dialogue/component portions. The Continue vector
`650A480200FE4904A0FFFFFF` is independently encoded with
`System.Formats.Asn1.AsnWriter` and checked against fixed bytes.
The in-repository C peer follows the same tag change. Rebuild it from matching
source before retesting. Old captures retain their original wire-profile
identity; they are not evidence for the new encoding.

Transaction decode requires one complete package, ordered and unique fields,
correct primitive/constructed form, and 1-4 octets per ID. It rejects legacy
context tags, extra fields and data after the package. The provider abort
cause uses application tag `4A`; it cannot be combined with user abort
dialogue data or a component portion. Example: `67064901FF4A0101`.

## BER Bounds

The bounded TCAP reader accepts short-form tags and definite lengths of
at most two length octets (65,535 value octets). It rejects end-of-contents,
high-tag-number form and truncated elements. OID decoding checks base-128
termination and overflow, including a multibyte first subidentifier.

## Invoke Component

The [remote operations ASN.1](https://www.itu.int/ITU-T/formal-language/itu-t/x/x880/1994-tc1/Remote-Operations-Generic-ROS-PDUs.html)
uses implicit context tag `80` for a linked invoke ID. The byte-valued API
retains the octet of a signed -128..127 invoke ID. Local operation codes in
the supported byte range are encoded as non-negative INTEGERs. The optional
argument is a single, complete ASN.1 value. The MAP repository-profile
parameter set now supplies a SEQUENCE, with field-level mapping tracked
separately in unit 9.

## Component Outcomes

ReturnResultLast carries an invoke ID and, only when a result value exists,
a SEQUENCE containing the local opcode and encoded result. ReturnError
preserves an optional ASN.1 parameter. Reject uses the four context-tagged
problem families and supports NULL for an uncorrelatable invoke ID.
The manager does not complete an invoke with a mismatched result opcode.
Global operation/error codes and fragmented ReturnResultNotLast are outside
the declared MAP SMS profile.

## Structured Dialogue

The [Q.773 DialoguePDUs module](https://www.itu.int/ITU-T/formal-language/itu-t/q/q773/1997/DialoguePDUs.html)
defines AARQ and AARE. The SDK encodes the EXTERNAL direct-reference OID
`0.0.17.773.1.1.1`, single-ASN.1-type envelope, application context and
response result/diagnostic. The optional user-information field carries
encoded EXTERNAL values rather than arbitrary bytes. Fixed AARQ/AARE
vectors and malformed variants exercise the codec; runtime negotiation is
verified separately in unit 7.

The stateful dialogue manager sends one AARE on the first Continue or End
of an inbound structured dialogue. An outbound dialogue that sent AARQ
requires a matching AARE before components can complete invokes. Missing
or mismatched responses fail closed; an explicit refusal uses Abort with
rejected AARE per [Q.774](https://www.itu.int/rec/T-REC-Q.774/en). The MAP server checks the requested context against its operation
profile and refuses mismatches. Bare TCAP dialogues remain available to
callers who omit the dialogue portion.

## Connectionless SCCP

[Q.713](https://www.itu.int/rec/T-REC-Q.713-200103-I/en) sections 1.4, 2.3
and 4.20 define little-endian LUDT pointers relative to their second octet,
one-octet address lengths and a little-endian long-data length. UDT, UDTS,
XUDT and LUDT validate bounds, overlaps and unclaimed bytes while allowing
legal mandatory parameter reordering. Invalid XUDT segmentation lengths and
non-ITU point codes are rejected by the TryDecode APIs. Odd SCCP BCD global
titles use zero filler, unlike MAP TBCD. The LUDT codec remains scoped to
messages without optional parameters.

## MAP SMS Profile

The supported mandatory fields follow [3GPP TS 29.002 / ETSI TS 129 002](https://www.etsi.org/deliver/etsi_ts/129000_129099/129002/18.00.00_60/ts_129002v180000p.pdf)
and its [MAP-SM-DataTypes ASN.1 module](https://raw.githubusercontent.com/wireshark/wireshark/master/epan/dissectors/asn1/gsm_map/MAP-SM-DataTypes.asn).
AddressString combines MAP nature and numbering plan in one octet before TBCD;
IMSI has digits only. MO uses service-centre DA and MSISDN OA; MT uses IMSI
DA and service-centre OA. SignalInfo is 1..200 bytes. SRI-SM includes the
mandatory priority BOOLEAN and optional GPRS NULL, and requires an IMSI and
network-node number in its result. ReportSM maps the three ASN.1 delivery
outcomes without changing the SDK enum's existing numeric values.
AlertServiceCentre carries two AddressStrings and an ID-only success.

This is a bounded single-node profile. LMSI, alternate routing numbers,
additional delivery outcomes and extension negotiation are not implemented.
Unmodeled fields are rejected rather than silently dropped. Direct
`MapSmsAddress.Encode()` is a standalone SDK format; pass the address to an
operation model for wire encoding. The default MAP nature is 1 (international).
