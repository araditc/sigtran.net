# Phase 11 Developer Experience

Phase 11 makes the SDK easier to adopt by documenting quickstarts, samples, configuration profiles, troubleshooting paths, and enterprise adoption gates.

## Capability Catalog

`SigtranDeveloperExperience.GetCapabilities()` exposes the Phase 11 developer experience areas:

- Quickstart
- Samples
- Configuration
- Troubleshooting
- Adoption

## M3UA Quickstart

`SigtranQuickstarts.CreateM3uaAspToSg()` exposes the official M3UA ASP-to-SG quickstart sequence:

1. Create transport options.
2. Create an M3UA transport session.
3. Start the ASP lifecycle.
4. Send or receive DATA.
5. Inspect diagnostics.

## Sample Templates

`SigtranSampleTemplates.GetTemplates()` maps official sample ids to their intended environments.

| Sample | Environment |
| --- | --- |
| `local-tcp-m3ua` | Local development |
| `m3ua-asp-to-sg` | Interoperability lab |
| `sccp-map-sms` | Production-like validation |
