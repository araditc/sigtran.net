# Linux Lab Setup

## Existing Lab VM

The authorized VM was inspected on 2026-11-04: Ubuntu 22.04, kernel 5.15,
4 vCPUs and 16 GiB RAM. Native SCTP, lksctp, .NET 10, GCC, tcpdump and TShark
are available. This is the SDK endpoint. On 2026-11-10 the root filesystem
had 21 GiB free; provide at least 30 GiB free before long-duration work.
No files or volumes were removed or resized by capture setup.

Non-root capture is now configured with the Debian/Ubuntu `wireshark-common`
setting, group `wireshark`, and `cap_net_admin,cap_net_raw=eip` on `dumpcap`.
The executable is root-owned, group-restricted mode 0750 and has no setuid bit.
The lab account is a member of that group. General passwordless sudo was not
enabled. A new login is required after adding another account to the group.

Reproduce setup on a dedicated lab host with:

```bash
sudo bash scripts/configure-capture-access.sh <lab-user>
```

After reconnecting, verify access without sudo:

```bash
dumpcap -i lo -p -L
python3 scripts/test-capture-access.py "$HOME/sigtran-lab/artifacts/capture-proof-$(date -u +%Y%m%dT%H%M%SZ)"
```

The proof generates only its own loopback SCTP traffic, retains private PCAP
and emits sanitized observations. The observed run captured 11 SCTP frames as
UID 1000, with PCAP mode 0600 and directory mode 0700.
The [sanitized capture proof](evidence/NON_ROOT_CAPTURE_20261104.json) retains
the measured result and permission boundary.

`collect-host-inventory.py` records `captureAvailable` separately from
`sudoAvailable`. Validated lab plans select the dumpcap backend automatically
when it is observed. Direct runner invocations can select it explicitly:

```bash
SIGTRAN_CAPTURE_BACKEND=dumpcap bash scripts/run-multi-host-qualification.sh
```

Existing protected topology/SSH/source settings remain required. Capture starts
after the recovery handshake, keeps a bounded rotating PCAP, is stopped by its
own user, and produces caller-owned files. The historical sudo/tcpdump backend
remains available. Peer service restart and SDK firewall fault injection need
separately authorized privileges; capture capabilities do not grant them.

## Separate Peer VM

Use Ubuntu Server 24.04 LTS amd64 with normal security updates and the supported
distribution kernel. No kernel downgrade or OpenSS7 installation is required.
Recommended resources for the peer: 4 vCPUs, 8 GiB RAM, 80 GiB SSD-backed disk.
These are lab sizing choices, not a guaranteed throughput rating.

Connect the primary NIC to the same reachable lab network as the SDK VM; use
a distinct static IPv4 address. For multi-homing add a second NIC to both VMs
on a separate data subnet/virtual switch. Permit SSH TCP/22 and SCTP protocol
132 on configured ports 2905/2906 between the lab endpoints. Outbound HTTPS
is needed for package/source retrieval. Enable time synchronization and avoid
automatic suspend during long runs.

Use a non-root SSH account with pinned host-key verification. Install .NET,
GCC, lksctp and capture tools on the SDK host. A statically linked reference
peer needs a Linux kernel with SCTP, not an SDK or dynamic lksctp on the peer.
Build on an authorized lab build host:

```bash
gcc -static -std=c11 -D_POSIX_C_SOURCE=200809L -O2 -Wall -Wextra -Werror \
  tools/interop-peer/sigtran_reference_peer.c -o reference-peer -lsctp
sha256sum reference-peer
```

Transfer the installer and binary over SSH, then run the reviewed installer
once with administrator approval on the peer:

```bash
sudo bash provision-reference-peer.sh <lab-user> ./reference-peer <sha256> \
  <source-commit> <peer-ip> <sdk-ip> 2906
```

The installer manages only `sigtran-reference-peer.service`,
`/opt/sigtran-lab-peer`, and its dedicated sudoers file. The unit runs with a
dynamic identity, source-address restriction, half-core CPU quota and 96 MiB
memory limit. It is not enabled on boot. Set
`PEER_SERVICE=sigtran-reference-peer.service` and
`PEER_BUILD_METADATA_FILE=/opt/sigtran-lab-peer/build.json` for the protected
runner. The non-sensitive build metadata is readable without sudo; service
control is authorized only for the named lab unit. A successful `sudo -n true`
inventory probe is not proof of authorization for any other command.

The Ubuntu 24.04 peer VM shared with Kafka was verified on 2026-11-10 using
two bounded, synthetic five-operation runs and SDK-side non-root PCAP.
Kafka broker/controller state, process identities and restart counts were
unchanged. No packages, Kafka settings or host firewall rules were changed.
Use this shared VM for bounded smoke and scoped peer restart only; use
isolated hosts for load, peer-outage, partition or endurance runs. See
[the sanitized result](evidence/SEPARATE_HOST_LAB_SMOKE_20261110.json).

Both VMs may run on the same VMware host. Record that shared physical host in
the benchmark topology: two guests support guest/association recovery tests,
but do not demonstrate survival of a physical-host failure. A container or
network namespace in the existing guest shares its kernel and does not satisfy
the separate-host gate.

References: [Ubuntu lifecycle](https://ubuntu.com/about/release-cycle),
[Wireshark capture privileges](https://wiki.wireshark.org/CaptureSetup/CapturePrivileges),
[dumpcap options](https://www.wireshark.org/docs/man-pages/dumpcap.html).
