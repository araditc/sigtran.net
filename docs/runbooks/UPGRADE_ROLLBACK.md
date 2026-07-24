# Upgrade And Rollback Runbook

This runbook applies to a controlled SDK or operations-host deployment.

## Before Upgrade

1. Confirm package digest, SBOM, provenance, signature, release notes, API diff,
   and migration notes for the target version.
2. Preserve the current image digest, package version, configuration, and
   rollback command.
3. Confirm peer maintenance coordination and route capacity for draining one
   association.
4. Capture healthy baseline metrics and a readiness response.
5. Deploy to a canary association or non-production peer first.

## Upgrade

1. Stop new application work for the selected association.
2. Wait for bounded queues and active TCAP dialogues to drain according to the
   product policy.
3. Allow graceful ASP Inactive, ASP Down, and SCTP shutdown.
4. Deploy the immutable target image or package.
5. Verify SCTP association, ASP activation, heartbeat, DATA exchange, readiness,
   structured events, and application transaction success.
6. Continue one instance at a time only while service objectives remain met.

## Rollback

Rollback when startup, protocol compatibility, loss, duplicate handling,
latency, memory, or fault rate breaches the approved threshold.

1. Stop rollout and preserve failing evidence.
2. Drain and stop the affected instance.
3. Restore the previous immutable image digest and configuration.
4. Verify association, ASP activation, health, traffic, and application
   transactions.
5. Keep the failed release blocked until root cause, tests, migration guidance,
   and release evidence are corrected.

Never overwrite an existing package version or mutate an existing container
tag during rollback.
