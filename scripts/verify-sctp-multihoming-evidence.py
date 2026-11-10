"""Verify one native SCTP association across the lab's primary link cut."""

import argparse
import csv
from datetime import datetime, timezone
from decimal import Decimal
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess


FIELDS = (
    "frame.time_epoch", "ip.src", "ip.dst", "sctp.chunk_type", "sctp.verification_tag",
    "sctp.data_tsn", "sctp.data_sid", "sctp.data_payload_proto_id",
)
PATHS = (
    (("192.0.2.1", "192.0.2.2"), ("198.51.100.1", "198.51.100.2")),
    (("192.0.2.2", "192.0.2.1"), ("198.51.100.2", "198.51.100.1")),
)
ROLES = ("client", "server")
PRIMARY_PEERS = {"client": "192.0.2.2", "server": "192.0.2.1"}
PPIDS = {0x53475400, 0x53475401, 0x53475402}
RECORD_COUNT = 48
FAULT_SEQUENCE = 16
EPOCH = datetime(1970, 1, 1, tzinfo=timezone.utc)


def require(condition, message):
    """Stop promotion on an absent or inconsistent observation."""
    if not condition:
        raise ValueError(message)


def epoch(value):
    """Convert trace timestamps without losing microseconds to floating point."""
    # .NET trims trailing zeros; Python 3.10 only accepts three or six fractional digits.
    value = re.sub(r"\.\d+(?=[+-]\d{2}:\d{2}$)", lambda match: match[0].ljust(7, "0")[:7], value)
    delta = datetime.fromisoformat(value) - EPOCH
    return Decimal(delta.days * 86400 + delta.seconds) + Decimal(delta.microseconds) / 1_000_000


def verify_trace(events, role, cut):
    """Check application continuity and the kernel's local association identity."""
    records = [event for event in events if event["kind"] == "validated-record"]
    require(len(records) == RECORD_COUNT, f"{role}: incomplete workload")
    for sequence, event in enumerate(records):
        require(
            event["role"] == role and event["sequence"] == sequence
            and event["length"] == (17, 147, 8192, 63)[sequence % 4]
            and event["stream"] == sequence % 4 and event["ppid"] == 0x53475400 + sequence % 3
            and event["unordered"] is bool(sequence % 2),
            f"{role}: record or metadata mismatch at sequence {sequence}",
        )
    times = [epoch(record["timestampUtc"]) for record in records]
    last_before = FAULT_SEQUENCE - (role == "client")
    require(times == sorted(times) and times[last_before] < cut < times[last_before + 1],
            f"{role}: the cut did not straddle the expected record")

    summaries = [event for event in events if event["kind"] == "multihoming-summary"]
    require(len(summaries) == 1, f"{role}: missing or duplicate association summary")
    summary = summaries[0]
    require(summary["role"] == role and summary["attempts"] == 1 and summary["state"] == "Established",
            f"{role}: association did not stay established without reconnect")
    notices = summary["notices"]
    changes = [event for event in notices if event["type"] == "AssociationChange"]
    require(len(changes) == 1 and changes[0]["stateCode"] == 0
            and changes[0]["inbound"] == changes[0]["outbound"] == 4,
            f"{role}: expected one COMM_UP and no association loss or restart")
    association = changes[0]["associationId"]
    require(association > 0 and all(notice["associationId"] == association for notice in notices)
            and not any(notice["type"] == "Shutdown" for notice in notices),
            f"{role}: kernel notices do not belong to one active association")
    require(any(notice["type"] == "PeerAddressChange" and notice["stateCode"] == 1
                and notice["peer"] == PRIMARY_PEERS[role] for notice in notices),
            f"{role}: primary peer address was not reported unreachable")
    probe = "idle-receive-cancelled" if role == "client" else "oversize-rejected"
    require(sum(event["kind"] == probe and event["role"] == role for event in events) == 1
            and events[-1]["kind"] == "status" and events[-1]["status"] == "complete",
            f"{role}: missing probe or successful completion")
    return times


def integers(value):
    """Read a TShark field that may occur once per bundled SCTP chunk."""
    return [int(part, 16 if part.startswith("0x") else 10) for part in value.split(",")] if value else []


def verify_wire(table, cut):
    """Require a single handshake, unchanged tags and DATA over the alternate."""
    rows = csv.DictReader(io.StringIO(table), delimiter="\t")
    require(tuple(rows.fieldnames or ()) == FIELDS, "Unexpected TShark fields")
    tags = [set(), set()]
    chunks = [[0, 0], [0, 0]]
    alternate_metadata = [set(), set()]
    handshake = []
    packets = 0
    for row in rows:
        packets += 1
        path = (row["ip.src"], row["ip.dst"])
        timestamp = Decimal(row["frame.time_epoch"])
        types = integers(row["sctp.chunk_type"])
        require(timestamp.is_finite() and types, "Invalid SCTP frame timestamp or chunk types")
        for kind in types:
            if kind in {1, 2, 10, 11}:
                require(timestamp < cut, "Association handshake appeared after the cut")
                handshake.append((kind, path))
        if 0 not in types:
            continue
        direction = next((index for index, paths in enumerate(PATHS) if path in paths), None)
        require(direction is not None, "DATA appeared outside the two lab paths")
        post_cut = timestamp > cut
        require(timestamp != cut and path == PATHS[direction][post_cut], "DATA used the wrong path for the cut")
        tag = integers(row["sctp.verification_tag"])
        require(len(tag) == 1 and tag[0] > 0, "DATA is missing a nonzero verification tag")
        tags[direction].add(tag[0])
        tsns, streams, ppids = (integers(row[field]) for field in FIELDS[-3:])
        require(len(tsns) == len(streams) == len(ppids) == types.count(0), "Malformed DATA metadata")
        chunks[direction][post_cut] += len(tsns)
        if post_cut:
            alternate_metadata[direction].update(zip(streams, ppids))

    require(handshake == [(1, PATHS[0][0]), (2, PATHS[1][0]), (10, PATHS[0][0]), (11, PATHS[1][0])],
            "Expected a single primary-path INIT/COOKIE exchange")
    for direction, role in enumerate(ROLES):
        require(len(tags[direction]) == 1, f"{role}: verification tag changed or DATA was absent")
        require(all(chunks[direction]), f"{role}: missing pre-cut or post-cut DATA")
        require(alternate_metadata[direction] == {(stream, ppid) for stream in range(4) for ppid in PPIDS},
                f"{role}: alternate did not carry every workload stream and PPID combination")

    return {
        "sctpFrames": packets,
        "associationHandshakes": 1,
        "primaryDataChunks": dict(zip(ROLES, (counts[0] for counts in chunks))),
        "alternateDataChunks": dict(zip(ROLES, (counts[1] for counts in chunks))),
        "sameVerificationTagPerDirection": True,
    }


def verify_run(root):
    """Decode the actual capture, then bind the observations to input hashes."""
    def read(name):
        return (root / name).read_text(encoding="utf-8").strip()

    cut = Decimal(read("cut-epoch.txt"))
    require(cut.is_finite() and cut > 0, "Invalid cut timestamp")
    sha = read("source-commit.txt")
    require(re.fullmatch(r"[0-9a-f]{40}", sha), "Invalid source revision")
    capture = re.search(r"Packets received/dropped on interface 'any': ([1-9]\d*)/0\b", read("capture.log"))
    require(capture is not None, "Capture did not report nonzero packets and zero drops")
    times = {
        role: verify_trace([json.loads(line) for line in read(f"{role}.jsonl").splitlines()], role, cut)
        for role in ROLES
    }
    command = ["tshark", "-n", "-r", str(root / "traffic.pcap"), "-Y", "sctp", "-T", "fields", "-E", "header=y"]
    for field in FIELDS:
        command.extend(("-e", field))
    table = subprocess.run(command, check=True, capture_output=True, text=True, timeout=20).stdout
    wire = verify_wire(table, cut)
    (root / "wire-sctp.tsv").write_text(table, encoding="utf-8")
    return {
        "result": "pass",
        "evidenceKind": "isolated-linux-native-sctp-path-loss",
        "sdkSourceCommit": sha,
        "cutEpochSeconds": str(cut),
        "environment": {
            "kernel": read("kernel.txt"), "dotnetSdk": read("dotnet.txt"),
            "topology": "Two private network namespaces on one Linux kernel, two veth links, no default route",
        },
        "observations": {
            "roundTrips": RECORD_COUNT, "inFlightSequence": FAULT_SEQUENCE, "streams": 4,
            "ppids": sorted(PPIDS), "associationAttempts": 1, "reconnects": 0,
            "capturePackets": int(capture[1]), "captureDrops": 0,
            "primaryUnreachableObservedBy": list(ROLES),
            "inFlightReplyAfterCutMilliseconds": round(float(times["client"][FAULT_SEQUENCE] - cut) * 1000, 3),
            **wire,
        },
        "privateArtifactSha256": {
            name: hashlib.sha256((root / name).read_bytes()).hexdigest()
            for name in ("traffic.pcap", "wire-sctp.tsv", "client.jsonl", "server.jsonl",
                         "capture.log", "cut-epoch.txt", "topology.log")
        },
        "limitations": ["Synthetic workload; not a latency SLA or capacity test.",
                        "No separate-host, operator peer or release-candidate qualification."],
    }


def main():
    """Emit a JSON report only after every evidence check passes."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifacts", type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(verify_run(args.artifacts), indent=2))
    except (OSError, ValueError, KeyError, TypeError, ArithmeticError, subprocess.SubprocessError) as error:
        parser.exit(1, f"SCTP multihoming evidence rejected: {error}\n")


if __name__ == "__main__":
    main()
