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
| 6 | Structured dialogue APDUs | Pending |
| 7 | Dialogue negotiation at runtime | Pending |
| 8 | Connectionless SCCP wire checks | Pending |
| 9 | MAP SMS ASN.1 profile and vectors | Pending |
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
