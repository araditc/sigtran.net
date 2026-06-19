# Phase 12 Operations

Phase 12 adds production operations and support foundations for SIGTRAN.NET.

## Capability Catalog

`SigtranOperations.GetCapabilities()` exposes the Phase 12 operations areas:

- Runbooks
- Incidents
- Health
- Recovery
- Support

## Runbook Catalog

`SigtranRunbooks.GetRunbooks()` exposes operational runbooks for transport outage, ASP recovery, interoperability evidence, and release rollback paths.

## Incident Response

`SigtranIncidentResponse.GetTargets()` defines operational severity targets.

Critical incidents should be acknowledged within 15 minutes and updated within 30 minutes. Lower severities have longer response windows.

## Health Checks

`SigtranHealthChecks.GetDefinitions()` defines operational health checks for transport, M3UA session, routing, interoperability evidence, and release readiness.

## Rollback Plan

`SigtranRollbackPlans.CreateDefaultPackageRollback()` defines package rollback actions for affected releases.

The plan stops publication, communicates the affected version, preserves artifacts and provenance, and publishes a corrected release only after gates pass.

## Maintenance Policy

`SigtranMaintenancePolicies.CreateDefault()` defines a 7-day minimum notice period, requires rollback plans, and requires lab validation for protocol and transport behavior changes.

## Support Handbook

`SigtranSupportHandbook.GetRules()` defines public GitHub issue support, private security disclosure, and commercial escalation channels.

## Operations Readiness

`SigtranOperationsReadiness.GetReport()` separates operations foundation readiness from production operations readiness.

Production operations remain blocked until wider commercial readiness is complete.
