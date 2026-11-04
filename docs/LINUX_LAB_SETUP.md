# Linux Lab Setup

## Existing Lab VM

The authorized VM was inspected on 2026-11-04: Ubuntu 22.04, kernel 5.15,
4 vCPUs and 16 GiB RAM. Native SCTP, lksctp, .NET 10, GCC, tcpdump and TShark
are available. It can remain the SDK endpoint. Its root filesystem is about
96% full; provide at least 30 GiB free before long-duration work. No files or
volumes were removed or resized by capture setup.

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

## Second Linux VM

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

Provide a non-root SSH account with sudo and SSH-key access. .NET 10, lksctp,
peer service, capture tools, protected artifact directories and restricted
automation privileges are configured after access is available.

Both VMs may run on the same VMware host. Record that shared physical host in
the benchmark topology: two guests support guest/association recovery tests,
but do not demonstrate survival of a physical-host failure. A container or
network namespace in the existing guest shares its kernel and does not satisfy
the separate-host gate. The inspected guest exposes neither `/dev/kvm` nor
nested virtualization CPU flags and has too little free disk for another VM.

References: [Ubuntu lifecycle](https://ubuntu.com/about/release-cycle),
[Wireshark capture privileges](https://wiki.wireshark.org/CaptureSetup/CapturePrivileges),
[dumpcap options](https://www.wireshark.org/docs/man-pages/dumpcap.html).
