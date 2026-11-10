# Protocol Wire Conformance

This is Phase 57 of the [approved continuation](RELEASE_QUALIFICATION_ROADMAP.md),
not the earlier roadmap-execution checkpoint. It covers the declared ITU
connectionless SCCP, TCAP and five-operation MAP SMS profile. Offline byte
checks do not establish operator or release-candidate acceptance.

| Unit | Scope | State |
| --- | --- | --- |
| 1 | Q.773 transaction tag classes and opaque transaction IDs | Implemented |
| 2 | Transaction field validation and abort encoding | Implemented |
| 3 | Bounded BER and OID decoding | Pending |
| 4 | Invoke and linked ID | Pending |
| 5 | Result, error and reject | Pending |
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
