# Community Response Templates

Use these responses when people ask whether any .NET tooling exists for SS7, SIGTRAN, M3UA, SCTP, SCCP, TCAP, or MAP.

The project should be positioned as a public release-candidate SDK for controlled integrations and lab validation, not as a fully stable operator-grade stack.

## Short reply

```text
Yes — we are building SIGTRAN.NET, an open-source .NET 10 SDK for SIGTRAN and SS7-over-IP protocol engineering:

https://github.com/araditc/sigtran.net

It targets native Linux SCTP, M3UA, M2PA, SCCP, TCAP, and MAP SMS signaling applications. The project is currently in public release-candidate status: the latest public NuGet prerelease is `Sigtran.NET` `1.0.0-rc.1`, and source builds are now tracking the `1.0.0-rc.2` candidate.

It is not yet a fully stable operator-grade SS7/SIGTRAN stack, but it is intended for controlled integrations, lab traffic, protocol review, interoperability feedback, and contributor validation.

Contributions are welcome, especially around SCTP/M3UA/M2PA peer validation, SCCP/TCAP/MAP profile testing, Wireshark trace comparison, protocol vectors, and real-world telecom signaling feedback.
```

## Longer technical reply

```text
You are right that most serious SS7/SIGTRAN tooling has historically been C/C++ or Java based, and .NET developers have had very little native tooling for this niche.

We are working on SIGTRAN.NET:

https://github.com/araditc/sigtran.net

SIGTRAN.NET is an open-source .NET 10 SDK focused on SIGTRAN and SS7-over-IP protocol engineering. The current RC track covers native Linux SCTP direction, M3UA runtime support, M2PA foundations, stateful SCCP and TCAP services, and MAP SMS workflows such as SRI-SM, MO/MT ForwardSM, ReportSM-DeliveryStatus, and AlertServiceCentre.

The latest public NuGet prerelease is `Sigtran.NET` `1.0.0-rc.1`; source builds currently track the unpublished `1.0.0-rc.2` candidate.

It should still be treated as release-candidate infrastructure, not as a fully stable operator-grade SS7 stack. Stable production claims remain gated on independent M2PA evidence, operator/vendor-profile SCCP/TCAP/MAP interoperability, representative multi-host performance and soak evidence, Kubernetes SCTP validation, trusted signing, and final stable publication evidence.

If you are working with GSM signaling, TCAP/MAP, M3UA, SCTP, Wireshark traces, or .NET/C# telecom infrastructure, feedback and contributions would be very welcome.
```

## Contributor invitation

```text
If anyone here has real-world SS7/SIGTRAN experience, we would really value your input on SIGTRAN.NET:

https://github.com/araditc/sigtran.net

Useful contributions include protocol review, test vectors, Wireshark trace comparison, SCTP/M3UA/M2PA validation, SCCP/TCAP/MAP profile feedback, documentation, examples, performance review, and issue reports.
```
