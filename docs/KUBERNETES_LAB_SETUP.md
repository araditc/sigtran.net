# Protected Kubernetes Lab Setup

This is an operator provisioning checklist, not deployment or qualification
evidence. Do not use the Kafka hosts or any shared/production worker, and do
not use WSL or colocated virtual nodes as representative multi-host evidence.
No lab target is selected implicitly by the workstation or by CI.

## Verified GitHub Setup

On 2026-11-18, repository `araditc/sigtran.net` had a protected `main` branch.
The `kubernetes-sctp` environment was created with product owner
`ammarheidari` as required approver, self-approval blocked, and exactly one
deployment rule: the `main` branch, not a tag. No environment secret was
installed and `K8S_LAB_ENABLED` was left unset. Do not dispatch a live job yet.
The repository reported no visible self-hosted runners. The organization
runner-group API denied the current account, so organization-side runner
group existence, restrictions and membership were not verified.

The environment API used here does not verify the administrator bypass
setting. In [environment settings](https://github.com/araditc/sigtran.net/settings/environments),
turn off **Allow administrators to bypass configured protection rules** and
have an operator other than the required approver dispatch the eventual job.
Verify the control in the UI before installing credentials or enabling the
lab. Do not bypass approvals. See the [GitHub environment configuration guide](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments).

## Isolate The Runner And Hosts

1. Reserve a Linux control plane and at least two Ready, untainted Linux
   workers on dedicated, separate VMs. Confirm native kernel SCTP on both
   workers and a CNI that supports SCTP for the chosen NetworkPolicy slice.
   Keep the protected namespace exclusive; reserve a separately labelled
   disruption worker only when that scenario is approved.
2. Reserve a separate Linux peer VM outside Kubernetes. Approve the exact
   peer IPv4/port and allow SCTP only from the lab paths to that endpoint.
   Account for the actual CNI egress address/NAT behavior from both workers.
   Management SSH and Kubernetes API access stay on their private routes.
3. An organization admin creates runner group `sigtran-kubernetes-lab` with
   **Selected repositories** and only `araditc/sigtran.net`. Enable public
   repository use for this group only because this repository is public.
   **Selected workflows** must contain exactly:
   `araditc/sigtran.net/.github/workflows/phase56-kubernetes-sctp.yml@refs/heads/main`.
   Register an isolated Linux runner in that group with labels `self-hosted`,
   `linux` and `sigtran-kubernetes-lab`; never register a lab runner in the
   default group. Prefer a fresh runner per job, without ambient cluster
   credentials or routes to unrelated services. Both qualification and
   retention jobs are routed to this group; keep their private evidence
   storage accessible across those jobs without allowing public artifact reads.
   [Runner group access controls](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/manage-access)
   are required in addition to environment approval.

## Prepare Scoped Cluster Access

Use the approved kubeconfig/context on a private management system, not a
current-context guess. Create the lab namespace out of band and label it
`sigtran.net/qualification=enabled`. Record its UID and the approved
cluster's `kube-system` namespace UID from that context. Pin a named CNI
DaemonSet and validate the worker, namespace and CNI snapshots using the
existing [read-only preflight](KUBERNETES_QUALIFICATION.md#preflight-boundary).

Render and inspect a **new local** RBAC manifest before an administrator
applies anything on the selected context. Example identifiers below are
placeholders, not a target selection:

```text
python scripts/render-kubernetes-lab-rbac.py --namespace sigtran-lab --cni-daemonset approved-cni --output lab-rbac.json
```

The service account is dedicated and has no default token mount. The core
manifest allows namespaced workload operations and access to the named CNI
DaemonSet, with no cluster-wide write rule, secret access, token issuance or
RBAC administration. The namespaced permissions require an exclusive lab
namespace with suitable admission controls. For approved disruption only,
render with `--drain-node <exact-worker-name>`; this adds patch permission
for that single worker, read-only colocated pod inventory, and namespaced
PDB/eviction permissions. Do not grant cluster-admin to the runner. Use an
expiring lab credential with TLS verification and no embedded unrelated
contexts, then verify its effective grants with `kubectl auth can-i` against
the intended context before storing it privately. The renderer does not
apply the manifest, mint a credential or change a cluster.

## Prepare The Independent Peer

On the approved Linux peer use the independently implemented C/lksctp build,
not a second SDK instance. The reviewed source and binary can be rebuilt
with GCC and `libsctp-dev` on that host, or the matching Linux/architecture
CI build can be reviewed first. `linux-reference-peer-build` contains the
source snapshot, binary, self-test log, build binding and SHA-256 manifest;
verify `sha256sum -c sha256.txt` and the build's exact `sourceCommit`. A PR
merge-test SHA is not the eventual source on `main`. Rebuild from the exact
approved commit if the architecture or system libraries do not match:

```bash
bash scripts/build-kubernetes-lab-peer.sh "$APPROVED_FULL_SHA" "$NEW_PRIVATE_BUILD_DIR"
```

The build runs the protocol self-test only and starts no listener. After
peer host approval, verify that the host owns the approved IPv4 address,
select a dedicated unprivileged SCTP port, and run `reference-peer` with
arguments `<approved-ipv4> <approved-sctp-port> 0 quiet` as an unprivileged
lab identity. Use a dedicated supervisor/unit with a bounded restart policy
that also restarts after clean association closure: this C peer serves one
association per process. Do not reuse the endurance/MAP test service or edit
firewalls on Kafka hosts. Keep peer logs private, verify the SCTP listener
and its owning process on the peer, and retain the source/host/listener
receipt. A binary self-test is not evidence of an SCTP listener, association
from either worker, SCTP policy enforcement or peer traffic.

## Before Activation

- In the **environment**, not in repository-wide settings, set approved
  secrets `KUBECONFIG_B64`, `K8S_REMOTE_IP`, `K8S_REMOTE_PORT`; set variables
  `K8S_CLUSTER_UID`, `K8S_QUALIFICATION_NAMESPACE_UID`, `K8S_CNI_DAEMONSET`, and
  `K8S_RAW_EVIDENCE_ROOT`. Set `K8S_CNI_NAMESPACE` if not `kube-system`.
  Keep endpoints, kubeconfig, private storage locations and raw captures out
  of Git and public PRs. Review protocol settings with the peer owner.
- Inspect the GitHub metadata from an authorized org/repo account:
  `python scripts/check-kubernetes-lab-controls.py`. It reads metadata only,
  returns nonzero on missing/unavailable controls, and prints no secret values,
  variable values, runner names or API errors. A `githubMetadataConfigured`
  result is not an activation, live cluster check, peer check or qualification.
- Admit the reviewed workflow changes on `main`, verify administrator
  bypass is disabled, the runner is isolated, the raw evidence path is private
  and writable, both pinned workers are eligible, and the independent peer is
  reachable on the approved lab path. The operator must independently inspect
  each of these; the checker cannot infer them from GitHub metadata.
- Only then set environment variable `K8S_LAB_ENABLED=true`, approve a
  digest-pinned operations-host image built from the admitted workflow source,
  and request the relevant separately approved runtime slice. The workflow
  checks activation before it configures kubeconfig credentials. Missing
  activation is a failure rather than a pass.

The remaining access handoff is an approved dedicated cluster context or
non-Kafka host inventory, access to the separate peer, and a group provisioned
by an `araditc` organization administrator. No live cluster or peer was
verified, no worker or service was changed and no lab dispatch was made.
