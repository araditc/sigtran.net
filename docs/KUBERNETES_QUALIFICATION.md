# Kubernetes Deployment Qualification

Phase 63 qualifies an explicitly approved Linux Kubernetes profile for
native SCTP/M3UA. Repository CI checks tooling and fixtures; only a retained
run on a representative cluster with an independent SCTP peer can qualify
deployment. The `kubernetes-sctp` gate remains open.

## Ten Units

| Unit | Deliverable and exit criterion | Status |
| --- | --- | --- |
| 1 | Read-only target preflight: pin cluster and namespace, check worker and CNI state, refuse mutation on failure | Tooling and offline tests delivered; live preflight not run |
| 2 | Reconcile the PDB and voluntary-drain design from [PR #33](https://github.com/araditc/sigtran.net/pull/33) with pinned preflight and exact-run ownership | Integrated on the unit branch; offline verification only, review and representative execution open |
| 3 | Prepare isolated representative cluster, protected runner/environment, private evidence storage, approved peer and source-bound image | GitHub environment configured; preparation tooling delivered. Runner-group access, dedicated hosts, peer launch and live verification remain open |
| 4 | Deploy exact image and compare startup/liveness/readiness, management Service and real SCTP association/peer traffic | Existing tooling partial; representative run open |
| 5 | Verify a fresh association is blocked under SCTP NetworkPolicy deny and recovers under allow, with verified cleanup | Existing tooling admitted; representative run open |
| 6 | Verify graceful pod termination, replacement readiness, SCTP recovery and accounted in-flight work | Shutdown observation in drain tooling; separate timing and in-flight evidence open |
| 7 | Verify PDB behavior, approved worker drain and rescheduling to a distinct worker with restoration receipts | Unit 2 tooling verified offline; representative execution open |
| 8 | Verify bounded recovery after abrupt pod/node loss on an isolated target | Open; voluntary drain is not a substitute |
| 9 | Verify a real image upgrade and rollback, exact image identities and post-rollback signaling | Existing rollback checks partial; representative run open |
| 10 | Retain private originals and sanitized, source-bound receipts; independently validate matrix, cleanup and gate decision | Open; no gate promotion inferred from CI |

## Lab Prerequisites

- A dedicated non-production Kubernetes cluster with native SCTP-capable
  Linux nodes, chosen CNI and an independent authorized SCTP/M3UA peer. Plan for
  at least two eligible workers for drain/rescheduling and node-loss scenarios.
  Do not reuse a Kafka or production worker for disruption.
- A protected `kubernetes-sctp` GitHub environment restricted to trusted
  `main` deployments with a required independent reviewer, plus an isolated
  Linux runner labelled `sigtran-kubernetes-lab`. Prefer an ephemeral runner
  and restrict which repository/workflows can use it. The workflow's main-ref
  condition is not a replacement for environment and runner access controls.
- A trusted, minimally scoped kubeconfig in environment secret
  `KUBECONFIG_B64`, an authorized `K8S_REMOTE_IP`/`K8S_REMOTE_PORT`, and
  protected private `K8S_RAW_EVIDENCE_ROOT`. Do not put credentials, addresses
  or raw artifacts into Git. Authorize node-disruption permissions separately.
- The lab operator pre-creates and reserves an exclusive qualification
  namespace labelled `sigtran.net/qualification=enabled`. Pin its UID in
  `K8S_QUALIFICATION_NAMESPACE_UID` and the trusted cluster's `kube-system`
  namespace UID in `K8S_CLUSTER_UID`. Obtain both pins out of band from the
  approved cluster context; do not copy them from an unverified workflow run.
  Recreating either namespace requires explicit repinning. The workflow cannot
  create the qualification namespace and must not target a shared namespace.
- Configure `K8S_CNI_NAMESPACE`, `K8S_CNI_DAEMONSET` and a digest-pinned image
  built from the exact workflow source. The CNI must expose its current
  DaemonSet rollout state for this profile.

Repository inventory on 2026-11-18 showed zero repository-visible self-hosted
runners. The `kubernetes-sctp` environment now requires owner approval, prevents
self-review and admits only `main`; it is not activated. See the
[protected lab setup checklist](KUBERNETES_LAB_SETUP.md) for the runner-group
restriction, scoped access renderer, independent peer build and remaining
operator checks. This does not assert that no external cluster exists. No
workflow was dispatched and no lab, Kafka service or production namespace
was touched by this preparation.

## Preflight Boundary

The workflow reads the two pinned namespaces, nodes and CNI DaemonSet before
deployment. It excludes cordoned, terminating, control-plane, non-Linux,
not-Ready and blocking-tainted nodes from the worker count. The CNI check
requires its current generation to be observed with all desired pods
scheduled, updated, ready and available, and none misscheduled. Invalid,
missing or mismatched inventory stops execution. NetworkPolicy cleanup cannot
start before successful preflight and a recorded policy apply attempt.

Only the eligible-worker count is printed by the checker. The API objects
stay in private raw evidence; never attach them to public issues or PRs.
This is a snapshot and does not reserve a node, prove SCTP support, prove
NetworkPolicy enforcement, or authorize node operations. Keep the namespace
exclusive for the run and verify actual placement and wire behavior separately.
See [node status](https://kubernetes.io/docs/reference/node/node-status/),
[NetworkPolicy behavior](https://kubernetes.io/docs/concepts/services-networking/network-policies/)
and [environment protection rules](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments).

## Owned Disruption Profile

The manually approved `disruption` profile is CNI-only and requires the
additional confirmation `run kubernetes disruption <namespace>`. Preflight
requires two eligible workers. The lab operator must reserve the worker
labelled `sigtran.net/qualification-drain=true` for this drill. The workflow
rechecks the cluster/namespace pins, both eligible workers and existing
workloads before acting. It refuses an unrelated active non-DaemonSet pod on
the target worker; it does not move or evict other workloads.

The rendered pod template carries the workflow run/attempt label. A
run-owned `policy/v1` PDB selects that label and the signaling application:
`maxUnavailable: 1` allows disruption for this one-replica drill, not
zero downtime. The controller must observe the PDB's current generation
and report exactly one healthy pod and one permitted disruption. The workflow
cordons the one approved worker with an atomic node UID/resource-version
check and owner annotation, then uses a namespace-scoped
[Eviction API request](https://kubernetes.io/docs/concepts/scheduling-eviction/api-eviction/)
with the exact pod UID. It never falls back to forced deletion or tries to
drain every workload on a node. A budget rejection fails the drill.

The scenario looks for `m3ua.shutdown.completed` in the followed original
pod log, a Ready replacement from the same ReplicaSet on a distinct eligible
worker, and an SCTP association row. That row is not independently decoded
peer traffic, and this drill does not establish in-flight accounting or a
separate graceful-termination timing pass.

Before creating the PDB or claiming the worker, a private intent record binds
the run and target.
The workflow's `always()` cleanup rechecks both pins. It restores only the
same node UID with exact run ownership, clearing the annotation in the same
atomic update as restoring schedulability. It independently attempts to
delete only the owned PDB with UID/resource-version preconditions and verifies
absence. Ownership changes or cleanup failures fail the slice; preserve the
private intents and involve the lab operator. Runner loss is not covered by
`always()`; verify the state and restore the owned resources before retrying.
Node patch, cross-namespace pod inventory, namespaced PDB and `pods/eviction`
permissions must be explicitly scoped by the lab operator.

PDB, node rescheduling and restoration receipts must bind to the same run and
source and include their required observations. The disruption slice does not
produce NetworkPolicy, stand-alone graceful-termination or all peer evidence
required by the full matrix. A successful slice is not a stable-gate decision.

Run the offline checks from the repository root:

```text
python -m unittest discover -s scripts/tests -p "test_kubernetes*.py"
```

The [release qualification roadmap](RELEASE_QUALIFICATION_ROADMAP.md) and
[runtime operations](OPERATIONS_RUNTIME.md) remain the scope and operational
references. No preflight or offline test substitutes for the remaining matrix.
